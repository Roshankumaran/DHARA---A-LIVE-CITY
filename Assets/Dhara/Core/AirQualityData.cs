using System;
using System.Globalization;
namespace Dhara
{
    public sealed class AirQualityData
    {
        public string Time;
        public double? Pm25,Pm10,UsAqi,EuropeanAqi,NitrogenDioxide,Ozone,SulphurDioxide,CarbonMonoxide;
        public static string Url(Location l)=>"https://air-quality-api.open-meteo.com/v1/air-quality?latitude="+l.Latitude.ToString(CultureInfo.InvariantCulture)+"&longitude="+l.Longitude.ToString(CultureInfo.InvariantCulture)+"&current=pm2_5,pm10,us_aqi,european_aqi,nitrogen_dioxide,ozone,sulphur_dioxide,carbon_monoxide&domains=cams_global&timezone=Asia%2FKolkata";
        public static AirQualityData Parse(string json)
        {
            var root=WeatherJson.Object(json);ApiValidation.Offset(root);
            var data=ApiValidation.Section(root,"current");var units=ApiValidation.Section(root,"current_units");
            string time=data.TryGetValue("time",out var t)?t as string:null;ApiValidation.Time(time);
            double? Read(string key,string unit="μg/m³")=>ApiValidation.Number(data,units,key,unit);
            return new AirQualityData {Time=time,Pm25=Read("pm2_5"),Pm10=Read("pm10"),
                UsAqi=Read("us_aqi","USAQI"),EuropeanAqi=Read("european_aqi","EAQI"),
                NitrogenDioxide=Read("nitrogen_dioxide"),Ozone=Read("ozone"),SulphurDioxide=Read("sulphur_dioxide"),CarbonMonoxide=Read("carbon_monoxide")};
        }
    }
}
