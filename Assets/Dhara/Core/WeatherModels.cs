using System;
namespace Dhara {
    [Serializable] public sealed class Location
    {
        public string Name; public double Latitude, Longitude;
        public Location(string name, double latitude, double longitude) { Name = name; Latitude = latitude; Longitude = longitude; }
        public static readonly Location[] All = {
            new Location("Bengaluru",12.97194,77.59369), new Location("Chennai",13.08784,80.27847),
            new Location("Mumbai",19.07283,72.88261), new Location("Delhi",28.62137,77.2148), new Location("Hyderabad",17.38405,78.45636)
        };
    }
    public sealed class DataSourceStatus
    {
        public bool Loading; public string Error; public DateTime? RetrievedUtc, RetryUtc;
        public bool Stale => RetrievedUtc.HasValue && DateTime.UtcNow - RetrievedUtc.Value > TimeSpan.FromMinutes(10);
        public string Label => Loading ? (Stale ? "Stale data · " : "") + "Retrieving weather…" : Error != null ? (Stale ? "Stale data · " : "") + "Request failed · " + Error : !RetrievedUtc.HasValue ? "No weather data" : Stale ? "Stale weather data" : "Weather model connected";
    }
    public sealed class ApiValues
    {
        public string time; public double? interval, temperature_2m, precipitation, rain, wind_speed_10m, wind_direction_10m, cloud_cover, relative_humidity_2m, shortwave_radiation, weather_code;
        public WeatherData Convert() => new WeatherData { Time = time, IntervalSeconds = interval ?? 0, Temperature = temperature_2m,
            // Current rain is a preceding-interval sum; convert it to a rate for simulation.
            RainMmPerHour = rain.HasValue && interval > 0 ? rain * 3600 / interval : null,
            PrecipitationMm = precipitation, WindKmh = wind_speed_10m, WindDegrees = wind_direction_10m,
            CloudPercent = cloud_cover, HumidityPercent = relative_humidity_2m, SolarWm2 = shortwave_radiation, WeatherCode = weather_code };
    }
    public sealed class ApiHourly
    {
        public string[] time;
        public double?[] temperature_2m, rain, precipitation, wind_speed_10m, wind_direction_10m, cloud_cover, relative_humidity_2m, shortwave_radiation, weather_code;
        static double? At(double?[] data, int i) => data != null && i >= 0 && i < data.Length ? data[i] : null;
        public WeatherData At(int i)
        {
            if (time == null || i < 0 || i >= time.Length) return null;
            return new WeatherData { Time = time[i], IsForecast = true, IntervalSeconds = 3600, Temperature = At(temperature_2m,i), RainMmPerHour = At(rain,i),
                PrecipitationMm = At(precipitation,i), WindKmh = At(wind_speed_10m,i), WindDegrees = At(wind_direction_10m,i),
                CloudPercent = At(cloud_cover,i), HumidityPercent = At(relative_humidity_2m,i), SolarWm2 = At(shortwave_radiation,i), WeatherCode = At(weather_code,i) };
        }
    }
    public sealed class WeatherResponse { public ApiValues current; public ApiHourly hourly; }
}
