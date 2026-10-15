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
            var values=Text(panel,"Air quality state","",17,.03f,.53f,.94f,.35f,ink);
            Button(panel,"Refresh air quality",.03f,.43f,.47f,.08f,()=>app.AirQuality.Select(app.Weather.Selected));
            Button(panel,"Toggle optional haze",.51f,.43f,.46f,.08f,()=>app.AirQuality.HazeEnabled=!app.AirQuality.HazeEnabled);
            Text(panel,"Weather advisory","WEATHER ADVISORY · UNAVAILABLE\nIndia Meteorological Department district-warning endpoint returned HTTP 401 during verification on 15 Oct 2026. Authorized access is required before integration. No advisory data is loaded.",16,.03f,.16f,.94f,.25f,muted);
            Button(panel,"Open-Meteo / CAMS documentation",.03f,.04f,.94f,.08f,()=>Application.OpenURL("https://open-meteo.com/en/docs/air-quality-api"));
            analysisJob=StartCoroutine(UpdateSources());
            IEnumerator UpdateSources()
            {
                while(panel!=null)
                {
                    var s=app.AirQuality;var d=s.Data;
                    values.text="AIR QUALITY · "+Location.All[app.Weather.Selected].Name+"\nPM2.5  "+Value(d?.Pm25,"μg/m³")+"    PM10  "+Value(d?.Pm10,"μg/m³")+"\nVALID "+(d?.Time??"Unavailable")+" IST\nRETRIEVED "+(s.Status.RetrievedUtc?.ToString("o")??"Unavailable")+"\n"+(s.Status.Loading?"Loading…":s.Status.Error!=null?"REQUEST FAILED · retained data: "+s.Status.Error:s.Status.Stale?"STALE DATA":d==null?"Unavailable":"Model data received")+"\nCAMS global via Open-Meteo · modeled concentrations, not sensors\nHaze "+(s.HazeEnabled?"enabled":"disabled")+"; qualitative capped effect, live mode only.";
                    yield return new WaitForSecondsRealtime(.25f);
                }
            }
        }
    }
}
