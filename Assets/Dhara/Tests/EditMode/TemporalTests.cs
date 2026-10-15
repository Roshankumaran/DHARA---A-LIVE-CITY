using System;
using NUnit.Framework;
namespace Dhara.Tests
{
    public class TemporalTests
    {
        [Test] public void FrozenForecastSurvivesRefreshWithoutMislabelingNewLiveData()
        {var frozen=new WeatherData{Time="2026-10-01T12:00",RainMmPerHour=10};var s=new SimulationState{Mode=WorldMode.Forecast,Forecast=frozen,FrozenPreview=frozen.Copy()};s.UpdateWeather(new WeatherData{Temperature=25},new[]{new WeatherData{Time=frozen.Time,RainMmPerHour=50}});Assert.That(s.Active.RainMmPerHour,Is.EqualTo(10));Assert.That(s.Live.Temperature,Is.EqualTo(25));s.ChangeMode(WorldMode.Live,null);Assert.That(s.FrozenPreview,Is.Null);}
        [Test] public void HistoricalParserKeepsMissingValuesAndRejectsWrongTimeZone()
        {var records=HistoricalWeather.Parse("{\"utc_offset_seconds\":19800,\"hourly\":{\"time\":[\"2026-10-01T00:00\"],\"rain\":[null]}}");Assert.That(records[0].RainMmPerHour,Is.Null);Assert.Throws<FormatException>(()=>HistoricalWeather.Parse("{\"utc_offset_seconds\":0}"));}
        [Test] public void HistoricalStateCannotOverwriteLiveAndFuture()
        {var s=new SimulationState{Live=new WeatherData{Temperature=10},Forecast=new WeatherData{Temperature=20},Historical=new WeatherData{Temperature=30},Mode=WorldMode.Historical};Assert.That(s.Active.Temperature,Is.EqualTo(30));s.UpdateWeather(new WeatherData{Temperature=11},null);Assert.That(s.Active.Temperature,Is.EqualTo(30));s.ChangeMode(WorldMode.Live,null);Assert.That(s.Active.Temperature,Is.EqualTo(11));}
        [Test] public void HistoricalDateIsValidatedBeforeRequest()
        {Assert.Throws<ArgumentException>(()=>HistoricalWeather.Url(Location.All[0],"bad"));Assert.Throws<ArgumentException>(()=>HistoricalWeather.Url(Location.All[0],"2099-01-01"));Assert.That(HistoricalWeather.Url(Location.All[0],"2020-01-01"),Does.Contain("models=era5"));}
    }
}
