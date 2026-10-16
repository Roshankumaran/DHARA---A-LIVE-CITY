using System;
using System.Collections.Generic;
using System.Globalization;
namespace Dhara
{
    public static class HistoricalWeather
    {
        public const string Fields="temperature_2m,relative_humidity_2m,precipitation,rain,wind_speed_10m,wind_direction_10m,cloud_cover,shortwave_radiation,weather_code";
        public static string Url(Location location,string date)
        {
            if(!DateTime.TryParseExact(date,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var day)||day<new DateTime(1940,1,1)||day>DateTime.UtcNow.Date.AddDays(-7))throw new ArgumentException("Choose a date from 1940 through seven days before today (ERA5 publication delay).");
            return "https://archive-api.open-meteo.com/v1/archive?latitude="+location.Latitude.ToString(CultureInfo.InvariantCulture)+"&longitude="+location.Longitude.ToString(CultureInfo.InvariantCulture)+"&start_date="+date+"&end_date="+date+"&models=era5&hourly="+Fields+"&timezone=Asia%2FKolkata&wind_speed_unit=kmh&temperature_unit=celsius&precipitation_unit=mm";
        }
        public static List<WeatherData> ParseValidated(string json,string requestedDate)
        {
            var root=WeatherJson.Object(json);ApiValidation.Offset(root);ApiValidation.Section(root,"hourly");ApiValidation.Hourly(root);
            var records=Parse(json);
            foreach(var record in records)if(!record.Time.StartsWith(requestedDate+"T",StringComparison.Ordinal))throw new FormatException("Historical response does not match requested date");
            return records;
        }
        public static List<WeatherData> Parse(string json)
        {
            var root=WeatherJson.Object(json);if(!root.TryGetValue("utc_offset_seconds",out var offset)||!(offset is double value)||value!=19800)throw new FormatException("Expected IST offset");
            var hourly=WeatherJson.Parse(json).hourly;if(hourly?.time==null||hourly.time.Length>24)throw new FormatException("Expected one day of hourly reanalysis");
            var result=new List<WeatherData>();DateTime previous=DateTime.MinValue;
            for(int i=0;i<hourly.time.Length;i++)
            {var item=hourly.At(i);if(!DateTime.TryParseExact(item.Time,"yyyy-MM-dd'T'HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out var time)||time<=previous)throw new FormatException("Invalid or unordered hourly time");previous=time;result.Add(item);}
            if(result.Count==0)throw new FormatException("No historical records");return result;
        }
    }
}
