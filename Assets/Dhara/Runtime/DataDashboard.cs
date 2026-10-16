using System;
using System.Collections;
using UnityEngine;
namespace Dhara
{
    public sealed partial class EnvironmentalDashboard
    {
        void OpenCityComparison()
        {
            var panel=Tool("FIVE-CITY WEATHER COMPARISON");app.CityComparison.Refresh();int metric=0;
            string[] names={"Temperature °C","Rain rate mm/h","Precipitation mm","Wind km/h","Wind direction °","Cloud %","Humidity %","Solar W/m²"};
            Func<WeatherData,double?>[] fields={d=>d?.Temperature,d=>d?.RainMmPerHour,d=>d?.PrecipitationMm,d=>d?.WindKmh,d=>d?.WindDegrees,d=>d?.CloudPercent,d=>d?.HumidityPercent,d=>d?.SolarWm2};
            var metricButton=Button(panel,names[0],.03f,.80f,.61f,.08f,()=>{});metricButton.onClick.AddListener(()=>{metric=(metric+1)%names.Length;metricButton.GetComponentInChildren<TMPro.TMP_Text>().text=names[metric];});
            Button(panel,"Refresh all",.66f,.80f,.31f,.08f,()=>app.CityComparison.Refresh(true));
            var rows=new TMPro.TMP_Text[5];var bars=new RectTransform[5];
            for(int i=0;i<5;i++){int city=i;var row=Region(Location.All[i].Name,panel,.03f,.65f-i*.12f,.94f,.11f,tint,true);Button(row,Location.All[i].Name,0,.04f,.25f,.92f,()=>{app.CityComparison.Cancel();CloseTool();app.SelectLocation(city);});rows[i]=Text(row,"Source value","",13,.26f,.1f,.73f,.84f,ink);bars[i]=Region("Metric bar",row,.27f,.025f,0,.045f,accent);}
            Text(panel,"Attribution","Open-Meteo model estimates · CC BY 4.0. Each row retains its own valid and retrieval time. Missing values are unavailable. Bars show magnitude relative to the largest absolute returned value; wind direction is circular, not a ranking.",12,.03f,.025f,.94f,.11f,muted);
            analysisJob=StartCoroutine(UpdateCities());
            IEnumerator UpdateCities()
            {
                while(panel!=null)
                {
                    double max=0;foreach(var d in app.Weather.CityCache){var value=fields[metric](d.Current);if(Numbers.IsFinite(value))max=Math.Max(max,Math.Abs(value.Value));}
                    for(int i=0;i<5;i++){var data=app.Weather.CityCache[i];var value=fields[metric](data.Current);rows[i].text=(Numbers.IsFinite(value)?value.Value.ToString("0.0"):"Unavailable")+" · valid "+(data.Current?.Time??"N/A")+" IST\nRetrieved "+(data.Status.RetrievedUtc?.ToString("HH:mm:ss 'UTC'")??"N/A")+" · "+(data.Status.Loading?"loading":data.Status.Error!=null?"request failed; retained":data.Status.Stale?"STALE":"model estimate");bars[i].anchorMax=new Vector2(.27f+(max>0&&Numbers.IsFinite(value)?(float)(Math.Abs(value.Value)/max)*.70f:0),.07f);}
                    yield return new WaitForSecondsRealtime(.25f);
                }
            }
        }
        void OpenDataSources()
        {
            var panel=Tool("ENVIRONMENTAL DATA SOURCES");
            bool showWeather=false;
            var values=Text(panel,"Air quality state","",14,.03f,.28f,.94f,.60f,ink);
            Button(panel,"Refresh air quality",.03f,.18f,.30f,.08f,()=>app.AirQuality.Select(app.Weather.Selected));
            Button(panel,"Weather / air quality",.35f,.18f,.31f,.08f,()=>showWeather=!showWeather);
            Button(panel,"Toggle optional haze",.68f,.18f,.29f,.08f,()=>app.AirQuality.HazeEnabled=!app.AirQuality.HazeEnabled);
            Text(panel,"Weather advisory","Simulation: DHARA model, not measured water levels. IMD advisories are unavailable; no authorized feed is configured.",12,.03f,.125f,.94f,.05f,muted);
            Button(panel,"Open-Meteo / CAMS documentation",.03f,.04f,.61f,.08f,()=>Application.OpenURL("https://open-meteo.com/en/docs/air-quality-api"));
            string exportStatus="";
            var feedback=Text(panel,"Export status","",11,.03f,.005f,.94f,.035f,muted);
            Button(panel,"Export data CSV",.66f,.04f,.31f,.08f,()=>{
                try{
                    var fields=new System.Collections.Generic.List<EnvironmentalParameter>(EnvironmentalParameters.Weather(app.Weather.Current,app.Weather.Status));
                    fields.AddRange(EnvironmentalParameters.AirQuality(app.AirQuality.Data,app.AirQuality.Status));
                    string city=Location.All[app.Weather.Selected].Name;
                    string directory=System.IO.Path.Combine(Application.persistentDataPath,"EnvironmentalData");System.IO.Directory.CreateDirectory(directory);
                    string path=System.IO.Path.Combine(directory,"DHARA_Environment_"+city+"_"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".csv");
                    System.IO.File.WriteAllText(path,EnvironmentalParameters.Csv(city,fields),new System.Text.UTF8Encoding(true));
                    exportStatus="CSV saved. Folder: "+directory;
                    Application.OpenURL(new Uri(directory+System.IO.Path.DirectorySeparatorChar).AbsoluteUri);
                }catch(Exception e){exportStatus="Export failed: "+e.Message;}
                feedback.text=exportStatus;
            });
            analysisJob=StartCoroutine(UpdateSources());
            IEnumerator UpdateSources()
            {
                while(panel!=null)
                {
                    var s=app.AirQuality;var d=s.Data;
                    var weather=app.Weather.Current;var status=showWeather?app.Weather.Status:s.Status;
                    values.text=(showWeather?"WEATHER · Open-Meteo Forecast API":"AIR QUALITY · Open-Meteo / CAMS global")+" · "+Location.All[app.Weather.Selected].Name+"\n";
                    var parameters=showWeather?EnvironmentalParameters.Weather(weather,status):EnvironmentalParameters.AirQuality(d,status);
                    foreach(var field in parameters)values.text+=field.Name+"  "+Value(field.Value,field.Unit)+"\n";
                    values.text+="VALID "+(showWeather?weather?.Time:d?.Time)+" IST · retrieved "+(status.RetrievedUtc?.ToString("HH:mm:ss 'UTC'")??"Unavailable")+"\n"+(status.Loading?"Loading…":status.Error!=null?"REQUEST FAILED: "+status.Error:status.Stale?"STALE DATA":status.RetrievedUtc.HasValue?"Model estimates received":"Unavailable")+"\n"+(showWeather?WeatherCondition.Describe(weather?.WeatherCode)+" · precipitation: preceding "+(weather?.IntervalSeconds.ToString()??"unknown")+" s total":"US and European AQI scales; neither is Indian NAQI")+"\nModel estimates, not sensors. Missing values remain unavailable.\n"+(showWeather?"Rain rate = interval rain / hours. Solar = interval mean.":"Haze "+(s.HazeEnabled?"enabled":"disabled")+": illustrative capped effect, live mode only.");
                    yield return new WaitForSecondsRealtime(.25f);
                }
            }
        }
    }
}
