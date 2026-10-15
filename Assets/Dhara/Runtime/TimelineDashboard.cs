using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace Dhara
{
    public sealed partial class EnvironmentalDashboard
    {
        TMP_InputField Input(Transform parent,string name,string initial,float x,float y,float w,float h)
        {
            var rect=Region(name,parent,x,y,w,h,tint,true);rect.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            var text=Text(rect,"Input text",initial,16,.02f,0,.96f,1,ink);text.raycastTarget=true;
            var input=rect.gameObject.AddComponent<TMP_InputField>();input.textViewport=rect;input.textComponent=(TextMeshProUGUI)text;input.text=initial;input.characterLimit=80;input.lineType=TMP_InputField.LineType.SingleLine;return input;
        }
        void OpenTimeline()
        {
            var panel=Tool("ENVIRONMENTAL TIME MACHINE");
            var date=Input(panel,"Historical date",DateTime.UtcNow.Date.AddDays(-14).ToString("yyyy-MM-dd"),.03f,.78f,.47f,.08f);
            Button(panel,"Load ERA5 date",.52f,.78f,.45f,.08f,()=>app.History.Load(app.Weather.Selected,date.text.Trim()));
            bool historical=true,playing=false;int selected=0;float speed=1,nextStep=0;string lastTime=null;
            var detail=Text(panel,"Timeline state","",16,.03f,.35f,.94f,.40f,ink);
            List<WeatherData> Records()=>historical&&app.History.City==app.Weather.Selected?app.History.Records:!historical?app.Weather.Forecast:new List<WeatherData>();
            Action select=()=>{var records=Records();if(records.Count==0)return;selected=Mathf.Clamp(selected,0,records.Count-1);app.SelectTemporal(records[selected],historical);lastTime=records[selected].Time;};
            Button(panel,"Historical",.03f,.67f,.30f,.08f,()=>{historical=true;playing=false;selected=0;select();});
            Button(panel,"Future",.35f,.67f,.30f,.08f,()=>{historical=false;playing=false;selected=0;select();});
            Button(panel,"Present / live",.67f,.67f,.30f,.08f,()=>{playing=false;app.SetMode(WorldMode.Live);CloseTool();});
            Button(panel,"Previous",.03f,.24f,.23f,.08f,()=>{playing=false;selected--;select();});
            Button(panel,"Next",.27f,.24f,.23f,.08f,()=>{playing=false;selected++;select();});
            var play=Button(panel,"Play / pause",.51f,.24f,.23f,.08f,()=>{playing=!playing;nextStep=Time.unscaledTime;});
            Button(panel,"Speed",.75f,.24f,.22f,.08f,()=>speed=speed==1?2:speed==2?4:1);
            Button(panel,"Simulate selected impact",.03f,.13f,.94f,.08f,()=>{select();if(lastTime!=null)OpenStudio();});
            Text(panel,"Temporal assumptions","ERA5 reanalysis / source forecast, not sensors. Each selected record is an independent, initially dry, one-hour constant-forcing simulation. Playback repeats these cases; missing inputs pause integration. Rain is the preceding-hour total interpreted as mm/h.",13,.03f,.015f,.94f,.10f,muted);
            analysisJob=StartCoroutine(TimelineLoop());
            IEnumerator TimelineLoop()
            {
                while(panel!=null)
                {
                    var records=Records();
                    if(playing&&Time.unscaledTime>=nextStep&&records.Count>0){select();selected++;if(selected>=records.Count){selected=records.Count-1;playing=false;}nextStep=Time.unscaledTime+2/speed;}
                    var active=app.Simulation.Active;
                    detail.text=(historical?"HISTORICAL · OPEN-METEO ERA5 REANALYSIS":"FUTURE · OPEN-METEO FORECAST")+"\nAvailable records: "+records.Count+" · Playback "+(playing?"playing":"paused")+" · "+speed+"×\nSelected source time: "+(lastTime??"None")+" IST\n"+(historical?(app.History.Status.Loading?"Loading…":app.History.Status.Error??"Retrieved "+app.History.Status.RetrievedUtc?.ToString("o")):app.Weather.Status.Label)+"\nActive mode: "+app.Simulation.Mode+" · rain "+Value(active?.RainMmPerHour,"mm/h")+"\nCalculated collection: "+app.Engine.District.Result.StorageM3.ToString("0.000")+" m³";
                    yield return null;
                }
            }
        }
    }
}
