using System;
using System.Collections.Generic;
using System.Globalization;

namespace Dhara
{
    // Validation at the external-data boundary. Low-level WeatherJson.Parse also
    // remains available for archived records and partial-data model tests.
    public static class ApiValidation
    {
        public static readonly Dictionary<string,string> WeatherUnits=new Dictionary<string,string> {
            {"temperature_2m","°C"},{"relative_humidity_2m","%"},{"precipitation","mm"},{"rain","mm"},
            {"wind_speed_10m","km/h"},{"wind_direction_10m","°"},{"cloud_cover","%"},
            {"shortwave_radiation","W/m²"},{"weather_code","wmo code"}
        };
        public static Dictionary<string,object> Section(Dictionary<string,object> root,string name)
        {if(!root.TryGetValue(name,out var value)||!(value is Dictionary<string,object> result))throw new FormatException("Missing "+name);return result;}
        public static void Offset(Dictionary<string,object> root)
        {if(!root.TryGetValue("utc_offset_seconds",out var value)||!(value is double n)||n!=19800)throw new FormatException("Expected Asia/Kolkata UTC offset (+05:30)");}
        public static DateTime Time(string text)
        {if(!DateTime.TryParseExact(text,"yyyy-MM-dd'T'HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out var value))throw new FormatException("Invalid API timestamp");return DateTime.SpecifyKind(value,DateTimeKind.Unspecified);}
        public static void Fresh(string text,DateTime nowUtc,TimeSpan maximumAge)
        {var utc=Time(text).AddSeconds(-19800);if(utc>nowUtc.AddMinutes(5)||nowUtc-utc>maximumAge)throw new FormatException("API valid time is outdated or in the future");}
        public static void Unit(Dictionary<string,object> units,string key,string expected)
        {if(!units.TryGetValue(key,out var value)||!(value is string text)||text.Replace('µ','μ')!=expected.Replace('µ','μ'))throw new FormatException("Unexpected unit for "+key);}
        public static double? Number(Dictionary<string,object> data,Dictionary<string,object> units,string key,string unit,double minimum=0,double maximum=double.MaxValue)
        {
            if(!data.TryGetValue(key,out var value)||value==null)return null;
            Unit(units,key,unit);
            if(!(value is double n)||!Numbers.IsFinite(n)||n<minimum||n>maximum)throw new FormatException("Invalid "+key);
            if(key=="weather_code"&&(n!=Math.Truncate(n)||!WeatherCondition.Known((int)n)))throw new FormatException("Unknown WMO weather code");
            return n;
        }
        static void WeatherFields(Dictionary<string,object> data,Dictionary<string,object> units)
        {
            foreach(var field in WeatherUnits)
            {
                double min=field.Key=="temperature_2m"?-100:0;
                double max=field.Key=="relative_humidity_2m"||field.Key=="cloud_cover"?100:field.Key=="wind_direction_10m"?360:field.Key=="temperature_2m"?100:double.MaxValue;
                Number(data,units,field.Key,field.Value,min,max);
            }
        }
        public static WeatherResponse Forecast(string json,DateTime nowUtc)
        {
            var root=WeatherJson.Object(json);Offset(root);
            var current=Section(root,"current");var cu=Section(root,"current_units");
            Fresh(current.TryGetValue("time",out var time)?time as string:null,nowUtc,TimeSpan.FromHours(2));
            Unit(cu,"time","iso8601");Unit(cu,"interval","seconds");
            if(!current.TryGetValue("interval",out var interval)||!(interval is double seconds)||seconds<=0||seconds>3600)throw new FormatException("Invalid current interval");
            WeatherFields(current,cu);
            Hourly(root);
            return WeatherJson.Parse(json);
        }
        public static void Hourly(Dictionary<string,object> root)
        {
            if(root.TryGetValue("hourly",out var hourly))
            {
                var h=Section(root,"hourly");var hu=Section(root,"hourly_units");Unit(hu,"time","iso8601");
                if(!h.TryGetValue("time",out var ts)||!(ts is List<object> times)||times.Count==0)throw new FormatException("Missing hourly timestamps");
                foreach(var field in WeatherUnits)
                    if(h.TryGetValue(field.Key,out var array)&&(!(array is List<object> list)||list.Count!=times.Count))throw new FormatException("Misaligned hourly "+field.Key);
                DateTime previous=DateTime.MinValue;
                for(int i=0;i<times.Count;i++)
                {
                    var stamp=Time(times[i] as string);if(stamp<=previous)throw new FormatException("Unordered hourly timestamps");previous=stamp;
                    var row=new Dictionary<string,object>();foreach(var field in WeatherUnits)if(h.TryGetValue(field.Key,out var array))row[field.Key]=((List<object>)array)[i];
                    WeatherFields(row,hu);
                }
            }
        }
    }
    public static class WeatherCondition
    {
        static readonly Dictionary<int,string> Codes=new Dictionary<int,string> {
            {0,"Clear sky"},{1,"Mainly clear"},{2,"Partly cloudy"},{3,"Overcast"},{45,"Fog"},{48,"Depositing rime fog"},
            {51,"Light drizzle"},{53,"Moderate drizzle"},{55,"Dense drizzle"},{56,"Light freezing drizzle"},{57,"Dense freezing drizzle"},
            {61,"Slight rain"},{63,"Moderate rain"},{65,"Heavy rain"},{66,"Light freezing rain"},{67,"Heavy freezing rain"},
            {71,"Slight snowfall"},{73,"Moderate snowfall"},{75,"Heavy snowfall"},{77,"Snow grains"},
            {80,"Slight rain showers"},{81,"Moderate rain showers"},{82,"Violent rain showers"},{85,"Slight snow showers"},{86,"Heavy snow showers"},
            {95,"Thunderstorm"},{96,"Thunderstorm with slight hail"},{99,"Thunderstorm with heavy hail"}
        };
        public static bool Known(int code)=>Codes.ContainsKey(code);
        public static string Describe(double? code)=>Numbers.IsFinite(code)&&code==Math.Truncate(code.Value)&&Codes.TryGetValue((int)code.Value,out var text)?text:"Unavailable";
    }
}
