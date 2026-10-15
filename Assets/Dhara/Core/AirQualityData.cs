using System;
using System.Collections.Generic;
using System.Globalization;
namespace Dhara
{
    public sealed class AirQualityData
    {
        public string Time;
        public double? Pm25,Pm10;
        public static string Url(Location l)=>"https://air-quality-api.open-meteo.com/v1/air-quality?latitude="+l.Latitude.ToString(CultureInfo.InvariantCulture)+"&longitude="+l.Longitude.ToString(CultureInfo.InvariantCulture)+"&current=pm2_5,pm10&domains=cams_global&timezone=Asia%2FKolkata";
        public static AirQualityData Parse(string json)
        {
            var root=WeatherJson.Object(json);
            if(!root.TryGetValue("utc_offset_seconds",out var offset)||!(offset is double value)||value!=19800||!root.TryGetValue("current",out var current)||!(current is Dictionary<string,object> c)||!root.TryGetValue("current_units",out var units)||!(units is Dictionary<string,object> u))throw new FormatException("Missing air-quality fields / IST offset");
            string time=c.TryGetValue("time",out var t)?t as string:null;
            if(!DateTime.TryParseExact(time,"yyyy-MM-dd'T'HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out _))throw new FormatException("Invalid air-quality timestamp");
            double? Read(string key)
            {
                if(!c.TryGetValue(key,out var x)||x==null)return null;
                if(!(x is double n)||!Numbers.IsFinite(n)||n<0)throw new FormatException("Invalid pollutant concentration");
                if(!u.TryGetValue(key,out var unit)||!(unit is string text)||text.Replace('µ','μ')!="μg/m³")throw new FormatException("Unexpected concentration unit");return n;
            }
            return new AirQualityData{Time=time,Pm25=Read("pm2_5"),Pm10=Read("pm10")};
        }
    }
}
