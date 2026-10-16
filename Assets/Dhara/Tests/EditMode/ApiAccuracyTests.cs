using System;
using System.Linq;
using NUnit.Framework;
namespace Dhara.Tests
{
    public sealed class ApiAccuracyTests
    {
        const string Valid="{\"utc_offset_seconds\":19800,\"current_units\":{\"time\":\"iso8601\",\"interval\":\"seconds\",\"rain\":\"mm\",\"temperature_2m\":\"°C\",\"weather_code\":\"wmo code\"},\"current\":{\"time\":\"2026-10-16T12:00\",\"interval\":900,\"rain\":2,\"temperature_2m\":25,\"weather_code\":61},\"hourly_units\":{\"time\":\"iso8601\",\"rain\":\"mm\",\"weather_code\":\"wmo code\"},\"hourly\":{\"time\":[\"2026-10-16T12:00\",\"2026-10-16T13:00\"],\"rain\":[0,null],\"weather_code\":[3,61]}}";
        static readonly DateTime Now=new DateTime(2026,10,16,6,30,0,DateTimeKind.Utc);
        [Test] public void CurrentAndHourlyHaveDifferentRainIntervals()
        {var r=ApiValidation.Forecast(Valid,Now);Assert.That(r.current.Convert().RainMmPerHour,Is.EqualTo(8));Assert.That(r.current.Convert().IntervalSeconds,Is.EqualTo(900));Assert.That(r.hourly.At(0).RainMmPerHour,Is.Zero);Assert.That(r.hourly.At(1).RainMmPerHour,Is.Null);Assert.That(r.hourly.At(0).IntervalSeconds,Is.EqualTo(3600));Assert.That(r.hourly.At(0).IsForecast,Is.True);}
        [TestCase("19800","0")]
        [TestCase("iso8601","unixtime")]
        [TestCase("seconds","hours")]
        [TestCase("\"interval\":900","\"interval\":0")]
        [TestCase("\"interval\":900","\"interval\":null")]
        [TestCase("\"rain\":2","\"rain\":-1")]
        [TestCase("\"rain\":2","\"rain\":\"2\"")]
        [TestCase("\"rain\":\"mm\"","\"rain\":\"inch\"")]
        [TestCase("\"temperature_2m\":25","\"temperature_2m\":200")]
        [TestCase("\"weather_code\":61","\"weather_code\":62")]
        [TestCase("\"weather_code\":61","\"weather_code\":61.5")]
        [TestCase("[0,null]","[0]")]
        [TestCase("2026-10-16T13:00","2026-10-16T11:00")]
        [TestCase("2026-10-16T12:00","bad time")]
        public void RejectsInvalidExternalData(string before,string after)
        {Assert.Throws<FormatException>(()=>ApiValidation.Forecast(Valid.Replace(before,after),Now));}
        [Test] public void RejectsOldResponseEvenWhenRetrievedNow()
        {Assert.Throws<FormatException>(()=>ApiValidation.Forecast(Valid,Now.AddHours(3)));}
        [Test] public void RejectsFutureResponse()
        {Assert.Throws<FormatException>(()=>ApiValidation.Forecast(Valid,Now.AddMinutes(-10)));}
        [Test] public void MissingAndZeroHaveDifferentAvailability()
        {var status=new DataSourceStatus{RetrievedUtc=DateTime.UtcNow};var fields=EnvironmentalParameters.Weather(new WeatherData{RainMmPerHour=0},status).ToArray();Assert.That(fields.Single(x=>x.Name=="Rain rate").Availability,Is.EqualTo("Available"));Assert.That(fields.Single(x=>x.Name=="Temperature").Availability,Is.EqualTo("Unavailable"));}
        [Test] public void RetainedFailurePreservesProvenance()
        {var status=new DataSourceStatus{RetrievedUtc=DateTime.UtcNow,Error="HTTP 429"};var field=EnvironmentalParameters.Weather(new WeatherData{Temperature=25,Time="valid"},status).First();Assert.That(field.Availability,Is.EqualTo("Retained after request failure"));Assert.That(field.ValidTime,Is.EqualTo("valid"));Assert.That(field.Source,Does.Contain("Forecast API"));}
        [Test] public void StaleMetadataRemainsStale()
        {var fields=EnvironmentalParameters.AirQuality(new AirQualityData{UsAqi=0},new DataSourceStatus{RetrievedUtc=DateTime.UtcNow.AddMinutes(-11)});Assert.That(fields.First().Availability,Is.EqualTo("Stale"));}
        [TestCase(1,429,"120",120)]
        [TestCase(1,503,"60",60)]
        [TestCase(1,429,"bad",15)]
        [TestCase(8,500,null,300)]
        [TestCase(1,429,"999999",86400)]
        public void RetryBackoffHonorsServerLimits(int failures,long code,string header,double expected)
        {Assert.That(ApiRetry.Delay(failures,code,header,Now),Is.EqualTo(expected));}
        [Test] public void RetryAfterHttpDateIsRespected()
        {Assert.That(ApiRetry.Delay(1,429,Now.AddSeconds(120).ToString("R"),Now),Is.EqualTo(120));}
        [Test] public void OlderResponseCannotOverwriteSameLocation()
        {var store=new WeatherDataStore();store.Accept(ApiValidation.Forecast(Valid,Now),Now);var old=store.Current;var r=ApiValidation.Forecast(Valid.Replace("T12:00","T11:00"),Now);Assert.Throws<FormatException>(()=>store.Accept(r,Now));Assert.That(store.Current,Is.SameAs(old));}
        [Test] public void HistoricalRequestDateMustMatch()
        {Assert.Throws<FormatException>(()=>HistoricalWeather.ParseValidated(Valid,"2026-10-15"));}
        [Test] public void ParameterCsvEscapesTextAndRetainsMissingValues()
        {
            var fields=EnvironmentalParameters.Weather(new WeatherData{Temperature=0,Time="2026-10-16T12:00"},new DataSourceStatus{RetrievedUtc=Now});
            string csv=EnvironmentalParameters.Csv("City, \"name\"",fields);
            Assert.That(csv,Does.Contain("\"City, \"\"name\"\"\""));Assert.That(csv,Does.Contain("Unavailable"));Assert.That(csv,Does.Contain("\"0\""));Assert.That(csv,Does.Contain("valid_time_IST,retrieved_UTC"));
        }
        [Test] public void EmptyParameterCsvHasValidHeader()
        {Assert.That(EnvironmentalParameters.Csv("Bengaluru",new EnvironmentalParameter[0]).Split('\n').Length,Is.EqualTo(2));}
        const string Aq="{\"utc_offset_seconds\":19800,\"current_units\":{\"us_aqi\":\"USAQI\",\"european_aqi\":\"EAQI\",\"pm2_5\":\"μg/m³\",\"carbon_monoxide\":\"μg/m³\"},\"current\":{\"time\":\"2026-10-16T12:00\",\"us_aqi\":0,\"european_aqi\":30,\"pm2_5\":null,\"carbon_monoxide\":200}}";
        [Test] public void AirQualityRetainsScalesConcentrationsAndMissing()
        {var d=AirQualityData.Parse(Aq);Assert.That(d.UsAqi,Is.Zero);Assert.That(d.EuropeanAqi,Is.EqualTo(30));Assert.That(d.Pm25,Is.Null);Assert.That(d.Ozone,Is.Null);Assert.That(d.CarbonMonoxide,Is.EqualTo(200));}
        [TestCase("USAQI","EAQI")]
        [TestCase("μg/m³","mg/m³")]
        [TestCase("\"us_aqi\":0","\"us_aqi\":-1")]
        [TestCase("\"carbon_monoxide\":200","\"carbon_monoxide\":true")]
        public void RejectsInvalidAirQuality(string before,string after)
        {Assert.Throws<FormatException>(()=>AirQualityData.Parse(Aq.Replace(before,after)));}
        [Test] public void WeatherCodeDoesNotInventUnknownConditions()
        {Assert.That(WeatherCondition.Describe(61),Is.EqualTo("Slight rain"));Assert.That(WeatherCondition.Describe(null),Is.EqualTo("Unavailable"));Assert.That(WeatherCondition.Describe(62),Is.EqualTo("Unavailable"));}
    }
}
