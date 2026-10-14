using System.Globalization;

namespace Dhara
{
    public static class WeatherRequest
    {
        const string Fields="temperature_2m,relative_humidity_2m,precipitation,rain,wind_speed_10m,wind_direction_10m,cloud_cover,shortwave_radiation";
        public static string Url(Location location) => "https://api.open-meteo.com/v1/forecast?latitude="+location.Latitude.ToString(CultureInfo.InvariantCulture)
            +"&longitude="+location.Longitude.ToString(CultureInfo.InvariantCulture)+"&current="+Fields+"&hourly="+Fields
            +"&wind_speed_unit=kmh&temperature_unit=celsius&precipitation_unit=mm&timezone=Asia%2FKolkata&forecast_days=2";
    }
}
