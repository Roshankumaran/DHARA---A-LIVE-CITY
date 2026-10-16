using System;
using System.Collections.Generic;
namespace Dhara
{
    public sealed class EnvironmentalParameter
    {
        public string Name,Unit,Source,ValidTime,Availability,Meaning;
        public double? Value;
        public DateTime? RetrievedUtc;
    }
    public static class EnvironmentalParameters
    {
        public static string Csv(string city,IEnumerable<EnvironmentalParameter> fields)
        {
            string Quote(string text)=>"\""+(text??"Unavailable").Replace("\"","\"\"")+"\"";
            var text=new System.Text.StringBuilder("city,field,value,unit,source,valid_time_IST,retrieved_UTC,availability,meaning\r\n");
            foreach(var field in fields)
            {
                string value=Numbers.IsFinite(field.Value)?field.Value.Value.ToString("R",System.Globalization.CultureInfo.InvariantCulture):"Unavailable";
                text.Append(string.Join(",",new[]{Quote(city),Quote(field.Name),Quote(value),Quote(field.Unit),Quote(field.Source),Quote(field.ValidTime),Quote(field.RetrievedUtc?.ToString("o")),Quote(field.Availability),Quote(field.Meaning)})).Append("\r\n");
            }
            return text.ToString();
        }
        static EnvironmentalParameter Field(string name,double? value,string unit,string source,string time,DataSourceStatus status,string meaning)
            =>new EnvironmentalParameter {Name=name,Value=value,Unit=unit,Source=source,ValidTime=time,RetrievedUtc=status.RetrievedUtc,
                Meaning=meaning,Availability=!Numbers.IsFinite(value)?"Unavailable":status.Stale?"Stale":status.Error!=null?"Retained after request failure":"Available"};
        public static IEnumerable<EnvironmentalParameter> Weather(WeatherData data,DataSourceStatus status)
        {
            const string source="Open-Meteo Forecast API";string time=data?.Time;
            string kind=data?.IsForecast==true?"Hourly forecast":"Current model estimate";
            yield return Field("Temperature",data?.Temperature,"°C",source,time,status,kind+"; 2 m air temperature");
            yield return Field("Relative humidity",data?.HumidityPercent,"%",source,time,status,kind+"; not visible vapour concentration");
            yield return Field("Rain rate",data?.RainMmPerHour,"mm/h",source,time,status,kind+"; preceding interval rain / interval hours; excludes separate showers/snow components");
            yield return Field("Precipitation",data?.PrecipitationMm,"mm",source,time,status,kind+"; preceding "+(data?.IntervalSeconds.ToString()??"unknown")+" s total (rain, showers and snow)");
            yield return Field("Wind speed",data?.WindKmh,"km/h",source,time,status,kind+"; 10 m wind");
            yield return Field("Wind direction",data?.WindDegrees,"°",source,time,status,kind+"; meteorological direction FROM, clockwise from north");
            yield return Field("Cloud cover",data?.CloudPercent,"%",source,time,status,kind+"; area fraction");
            yield return Field("Shortwave radiation",data?.SolarWm2,"W/m²",source,time,status,kind+"; preceding interval mean");
            yield return Field("Weather code",data?.WeatherCode,"WMO",source,time,status,kind+"; "+WeatherCondition.Describe(data?.WeatherCode));
        }
        public static IEnumerable<EnvironmentalParameter> AirQuality(AirQualityData data,DataSourceStatus status)
        {
            const string source="Open-Meteo Air Quality API / CAMS global";
            const string meaning="Current model estimate, not a local sensor measurement";
            yield return Field("US AQI",data?.UsAqi,"USAQI",source,data?.Time,status,meaning+"; US scale, not Indian NAQI");
            yield return Field("European AQI",data?.EuropeanAqi,"EAQI",source,data?.Time,status,meaning+"; European scale");
            yield return Field("PM2.5",data?.Pm25,"μg/m³",source,data?.Time,status,meaning);
            yield return Field("PM10",data?.Pm10,"μg/m³",source,data?.Time,status,meaning);
            yield return Field("Nitrogen dioxide",data?.NitrogenDioxide,"μg/m³",source,data?.Time,status,meaning);
            yield return Field("Ozone",data?.Ozone,"μg/m³",source,data?.Time,status,meaning);
            yield return Field("Sulphur dioxide",data?.SulphurDioxide,"μg/m³",source,data?.Time,status,meaning);
            yield return Field("Carbon monoxide",data?.CarbonMonoxide,"μg/m³",source,data?.Time,status,meaning);
        }
    }
}
