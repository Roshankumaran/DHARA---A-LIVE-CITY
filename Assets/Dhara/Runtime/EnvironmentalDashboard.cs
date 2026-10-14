using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Dhara
{
    public sealed class EnvironmentalDashboard : MonoBehaviour
    {
        DharaApplication app;
        readonly Color panel=new Color(.065f,.094f,.11f), raised=new Color(.105f,.15f,.17f), ink=new Color(.89f,.93f,.91f), muted=new Color(.59f,.69f,.70f), accent=new Color(.25f,.66f,.57f);
        TMP_Text status,mode,metrics,source,environment,events,forecastLabel,scenarioNote;
        RectTransform lab,forecast,live;
        UnityEngine.UI.Slider rain,wind,temperature,drainage,timeline;
        readonly List<UnityEngine.UI.Button> tabs=new List<UnityEngine.UI.Button>();
        readonly List<UnityEngine.UI.Button> locations=new List<UnityEngine.UI.Button>();
        readonly List<Action> sliderLabels=new List<Action>();
        float nextRefresh;
        bool syncing;
        RectTransform Region(string name, Transform parent, Vector2 min, Vector2 max, Color? background=null)
        {
            var obj=new GameObject(name,typeof(RectTransform)); obj.transform.SetParent(parent,false);
            var rect=obj.GetComponent<RectTransform>(); rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=Vector2.zero; rect.offsetMax=Vector2.zero;
            if(background.HasValue) { var image=obj.AddComponent<UnityEngine.UI.Image>(); image.color=background.Value; image.raycastTarget=false; }
            return rect;
        }
        TMP_Text Text(Transform parent,string text,float size,Color color,Vector2 min,Vector2 max)
        {
            var rect=Region(text,parent,min,max);
            var label=rect.gameObject.AddComponent<TextMeshProUGUI>(); label.text=text; label.fontSize=size; label.color=color;
            label.margin=new Vector4(16,8,16,6); label.alignment=TextAlignmentOptions.TopLeft; label.raycastTarget=false;
            label.enableWordWrapping=true; label.overflowMode=TextOverflowModes.Ellipsis;
            return label;
        }
        UnityEngine.UI.Button Button(Transform parent,string text,Vector2 min,Vector2 max,Action action)
        {
            var rect=Region(text,parent,min,max,raised); rect.offsetMin=new Vector2(4,4); rect.offsetMax=new Vector2(-4,-4);
            rect.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic=rect.GetComponent<UnityEngine.UI.Image>();
            var colors=button.colors; colors.highlightedColor=new Color(.48f,.66f,.62f); colors.selectedColor=new Color(.38f,.58f,.53f); button.colors=colors;
            var label=Text(rect,text,16,ink,Vector2.zero,Vector2.one); label.alignment=TextAlignmentOptions.Center; label.margin=Vector4.zero;
            button.onClick.AddListener(()=>action()); return button;
        }
        UnityEngine.UI.Slider Slider(Transform parent,string title,float min,float max,Vector2 a,Vector2 b,Action<float> changed,string unit)
        {
            var group=Region(title,parent,a,b);
            var label=Text(group,title,16,ink,new Vector2(0,.55f),Vector2.one);
            var track=Region("Track",group,new Vector2(.08f,.19f),new Vector2(.91f,.28f),raised);
            track.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            var handle=Region("Handle",track,new Vector2(0,.5f),new Vector2(0,.5f),accent); handle.sizeDelta=new Vector2(16,23);
            handle.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            var slider=track.gameObject.AddComponent<UnityEngine.UI.Slider>(); slider.minValue=min; slider.maxValue=max; slider.handleRect=handle; slider.targetGraphic=handle.GetComponent<UnityEngine.UI.Image>();
            slider.onValueChanged.AddListener(value=> { label.text=title+"  "+value.ToString("0.0")+" "+unit; if(!syncing) changed(value); });
            sliderLabels.Add(()=>label.text=title+"  "+slider.value.ToString("0.0")+" "+unit);
            return slider;
        }
        public void Initialize(DharaApplication application)
        {
            app=application;
            var canvasObj=new GameObject("Environmental monitoring dashboard",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObj.transform.SetParent(app.transform,false);
            var canvas=canvasObj.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasObj.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1600,1000); scaler.matchWidthOrHeight=.5f;
            var eventSystem=new GameObject("Dashboard input",typeof(EventSystem),typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(app.transform,false);
            var root=canvasObj.transform;
            var header=Region("Header",root,new Vector2(0,.90f),Vector2.one,panel);
            Text(header,"DHARA",32,ink,new Vector2(0,.35f),new Vector2(.14f,1));
            Text(header,"A Living World",13,muted,Vector2.zero,new Vector2(.14f,.4f));
            for(int i=0;i<Location.All.Length;i++) { int index=i; locations.Add(Button(header,Location.All[i].Name,new Vector2(.15f+i*.115f,.42f),new Vector2(.265f+i*.115f,.94f),()=>app.SelectLocation(index))); }
            status=Text(header,"Retrieving weather…",14,muted,new Vector2(.16f,0),new Vector2(.75f,.4f));
            mode=Text(header,"LIVE CONDITIONS",17,accent,new Vector2(.75f,.44f),Vector2.one);
            Button(header,"Refresh weather",new Vector2(.79f,.02f),new Vector2(.99f,.44f),()=>app.Weather.Refresh());
            var left=Region("Weather data",root,new Vector2(0,.25f),new Vector2(.19f,.90f),panel);
            Text(left,"WEATHER DATA",17,accent,new Vector2(0,.91f),Vector2.one);
            metrics=Text(left,"Awaiting data",16,ink,new Vector2(0,.30f),new Vector2(1,.91f));
            source=Text(left,"",11,muted,Vector2.zero,new Vector2(1,.30f));
            var right=Region("Environment status",root,new Vector2(.78f,.25f),new Vector2(1,.90f),panel);
            Text(right,"ENVIRONMENT STATUS",17,accent,new Vector2(0,.91f),Vector2.one);
            environment=Text(right,"",16,ink,new Vector2(0,.56f),new Vector2(1,.91f));
            Text(right,"ENVIRONMENTAL CHANGE LOG",14,accent,new Vector2(0,.48f),new Vector2(1,.55f));
            events=Text(right,"",13,muted,new Vector2(0,0),new Vector2(1,.48f));
            var footer=Region("Scenario controls",root,Vector2.zero,new Vector2(1,.25f),panel);
            string[] tabNames={"LIVE WORLD","FORECAST PREVIEW","WHAT-IF LAB"};
            for(int i=0;i<3;i++) { int index=i; tabs.Add(Button(footer,tabNames[i],new Vector2(i*.23f,.77f),new Vector2((i+1)*.23f,.99f),()=> { app.SetMode((WorldMode)index); Sync(); })); }
            Button(footer,"Reset camera",new Vector2(.82f,.77f),new Vector2(.99f,.99f),()=>app.World.ViewCamera.GetComponent<OrbitCamera>().Home());
            lab=Region("What-if controls",footer,new Vector2(0,.12f),new Vector2(1,.77f));
            rain=Slider(lab,"RAINFALL",0,200,new Vector2(.01f,.30f),new Vector2(.20f,1),value=>{app.Simulation.Scenario.RainMmPerHour=value;app.ApplyInputs();},"mm/h");
            wind=Slider(lab,"WIND SPEED",0,180,new Vector2(.21f,.30f),new Vector2(.40f,1),value=>{app.Simulation.Scenario.WindKmh=value;app.ApplyInputs();},"km/h");
            temperature=Slider(lab,"TEMPERATURE",-30,60,new Vector2(.41f,.30f),new Vector2(.60f,1),value=>{app.Simulation.Scenario.Temperature=value;app.ApplyInputs();},"°C");
            drainage=Slider(lab,"DRAINAGE CAPACITY",0,60,new Vector2(.61f,.30f),new Vector2(.80f,1),value=>{app.Simulation.Scenario.DrainageMmPerHour=value;app.ApplyInputs();},"mm/h");
            Button(lab,"Reset scenario",new Vector2(.82f,.55f),new Vector2(.99f,.98f),()=>{app.ResetScenario();Sync();});
            Button(lab,"Return to live",new Vector2(.82f,.08f),new Vector2(.99f,.51f),()=>{app.SetMode(WorldMode.Live);Sync();});
            scenarioNote=Text(lab,"Hypothetical inputs",13,muted,Vector2.zero,new Vector2(.81f,.30f));
            forecast=Region("Forecast controls",footer,new Vector2(.01f,.13f),new Vector2(.99f,.77f));
            forecastLabel=Text(forecast,"No forecast available",17,ink,new Vector2(0,.55f),new Vector2(.7f,1));
            timeline=Slider(forecast,"HOUR OFFSET",0,1,new Vector2(0,0),new Vector2(.74f,.58f),value=>app.SelectForecast(Mathf.RoundToInt(value)),"h"); timeline.wholeNumbers=true;
            Button(forecast,"Return to live",new Vector2(.80f,.15f),new Vector2(.99f,.68f),()=>{app.SetMode(WorldMode.Live);Sync();});
            live=Region("Live controls",footer,new Vector2(.01f,.13f),new Vector2(.99f,.77f));
            Text(live,"A compact urban environment responding to weather-model inputs.\nOpen WHAT-IF LAB to explore rainfall, wind, temperature and drainage.\nRight drag: orbit  ·  Middle drag / WASD: pan  ·  Scroll: zoom  ·  Home: reset view",17,ink,Vector2.zero,new Vector2(.8f,1));
            Text(footer,"SIMULATED OUTPUTS · 60× water-model clock · effective surface storage, not a calibrated flood forecast · generic district, not a city reconstruction",12,muted,Vector2.zero,new Vector2(1,.13f));
            app.ControlsChanged+=Sync; Sync();
        }
        void Sync()
        {
            syncing=true;
            var scenario=app.Simulation.Scenario;
            rain.SetValueWithoutNotify((float)scenario.RainMmPerHour); wind.SetValueWithoutNotify((float)scenario.WindKmh);
            temperature.SetValueWithoutNotify((float)scenario.Temperature); drainage.SetValueWithoutNotify((float)scenario.DrainageMmPerHour);
            timeline.maxValue=Mathf.Max(1,app.Weather.Forecast.Count-1); timeline.SetValueWithoutNotify(0);
            timeline.interactable=app.Weather.Forecast.Count>0;
            foreach(var update in sliderLabels) update();
            syncing=false;
            var active=app.Simulation.Mode;
            live.gameObject.SetActive(active==WorldMode.Live); forecast.gameObject.SetActive(active==WorldMode.Forecast); lab.gameObject.SetActive(active==WorldMode.WhatIf);
            for(int i=0;i<tabs.Count;i++) tabs[i].GetComponent<UnityEngine.UI.Image>().color=i==(int)active ? new Color(.15f,.32f,.30f) : raised;
        }
        static string Value(double? value,string unit,string format="0.0") => value.HasValue && !double.IsNaN(value.Value) && !double.IsInfinity(value.Value) ? value.Value.ToString(format)+" "+unit : "Unavailable";
        void OnDestroy() { if(app!=null) app.ControlsChanged-=Sync; }
        void Update()
        {
            if(app==null || Time.unscaledTime<nextRefresh) return; nextRefresh=Time.unscaledTime+.25f;
            var simulation=app.Simulation; var weather=simulation.Active; var state=app.Engine.State;
            status.text=Location.All[app.Weather.Selected].Name+"  ·  "+app.Weather.Status.Label;
            mode.text=simulation.Mode==WorldMode.WhatIf ? "WHAT-IF · HYPOTHETICAL" : simulation.Mode==WorldMode.Forecast ? "FORECAST PREVIEW" : "LIVE CONDITIONS";
            for(int i=0;i<locations.Count;i++) locations[i].GetComponent<UnityEngine.UI.Image>().color=i==app.Weather.Selected ? new Color(.15f,.32f,.30f) : raised;
            string rainfallLabel=simulation.Mode==WorldMode.WhatIf ? "RAINFALL INPUT" : "RAINFALL RATE¹";
            metrics.text="TEMPERATURE\n"+Value(weather?.Temperature,"°C")+"\n\n"+rainfallLabel+"\n"+Value(weather?.RainMmPerHour,"mm/h")
                +"\n\nWIND SPEED / DIRECTION\n"+Value(weather?.WindKmh,"km/h")+" · "+Value(weather?.WindDegrees,"°","0")
                +"\n\nCLOUD COVER / HUMIDITY\n"+Value(weather?.CloudPercent,"%","0")+" / "+Value(weather?.HumidityPercent,"%","0")
                +"\n\nSOLAR RADIATION\n"+Value(weather?.SolarWm2,"W/m²","0")
                +"\n\nPRECIPITATION TOTAL¹\n"+(simulation.Mode==WorldMode.WhatIf ? "Live total not used" : Value(weather?.PrecipitationMm,"mm"));
            source.text="DATA SOURCE: Open-Meteo\nWeather-model estimates, not sensors\nVALID: "+(weather?.Time ?? "Unavailable")+" IST\nRETRIEVED: "+(app.Weather.Status.RetrievedUtc?.ToString("HH:mm:ss 'UTC'") ?? "Unavailable")
                +"\n¹ Preceding 15-min current / 1-h forecast interval; rain converted to mm/h.\nCC BY 4.0 · open-meteo.com";
            bool valid=weather!=null && weather.HasSimulationInputs;
            environment.text=valid ? $"SIMULATED WATER\n{state.WaterMm:0.0} / 150 mm\nSURFACE WETNESS  {state.Wetness*100:0}%\nDRAINAGE  {simulation.Drainage:0.0} mm/h\nROOFTOP SOLAR  "+(weather.SolarWm2.HasValue ? $"{state.SolarKw:0.00} kW" : "Unavailable")+$"\nMODEL TIME  {state.ElapsedSimulatedSeconds/60:0.0} min" : "Simulation paused\nAwaiting complete weather inputs.\nWHAT-IF LAB remains available.\nNo measured water levels.";
            events.text=""; for(int i=0;i<Math.Min(3,app.Engine.Events.Count);i++) { var entry=app.Engine.Events[i]; events.text+=entry.Timestamp.ToString("HH:mm:ss")+"  "+entry.Message+"\n\n"; }
            forecastLabel.text=simulation.Forecast!=null ? "FORECAST VALID: "+simulation.Forecast.Time+" IST · storage starts at zero per selection" : "No forecast data available. Retrieve weather and retry.";
            timeline.maxValue=Mathf.Max(1,app.Weather.Forecast.Count-1); timeline.interactable=app.Weather.Forecast.Count>0;
            if(simulation.Mode==WorldMode.Forecast && simulation.Forecast!=null)
            {
                int index=app.Weather.Forecast.IndexOf(simulation.Forecast);
                if(index>=0) timeline.SetValueWithoutNotify(index);
            }
            scenarioNote.text=app.Weather.Current==null ? "HYPOTHETICAL · Offline baseline: cloud cover 50%; solar / humidity / direction unavailable." : "HYPOTHETICAL · Unmodified cloud cover, solar, humidity and direction use the latest live baseline.";
        }
    }
}
