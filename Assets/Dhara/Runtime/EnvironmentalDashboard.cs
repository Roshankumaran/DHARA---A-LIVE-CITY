using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Dhara
{
    public sealed partial class EnvironmentalDashboard : MonoBehaviour
    {
        DharaApplication app;
        readonly Color paper=new Color(.965f,.980f,.990f), card=Color.white, tint=new Color(.925f,.962f,.978f);
        readonly Color ink=new Color(.055f,.14f,.26f), muted=new Color(.32f,.42f,.51f), accent=new Color(.02f,.46f,.42f), line=new Color(.84f,.90f,.94f);
        TMP_Text navigationStatus;
        RectTransform tourControls;
        TMP_Text status,cityTitle,mode,source,forecastLabel,scenarioNote,insights,experimentInput,experimentResult;
        readonly TMP_Text[] metrics=new TMP_Text[6], outputs=new TMP_Text[5], eventRows=new TMP_Text[3];
        RectTransform lab,forecast,live,experimentPanel,waterBar,wetBar;
        UnityEngine.UI.Slider rain,wind,temperature,drainage,timeline,extraDrainage;
        UnityEngine.UI.Button compareButton;
        readonly List<UnityEngine.UI.Button> tabs=new List<UnityEngine.UI.Button>(), locations=new List<UnityEngine.UI.Button>();
        readonly List<Action> sliderLabels=new List<Action>();
        readonly DrainageExperiment experiment=new DrainageExperiment();
        string capturedContext;
        float nextRefresh;
        bool syncing;
        Sprite rounded;
        Texture2D roundedTexture;
        public static readonly Rect WorldViewport=new Rect(.205f,.255f,.54f,.565f);

        void CreateStyle()
        {
            roundedTexture=new Texture2D(32,32,TextureFormat.RGBA32,false);
            roundedTexture.wrapMode=TextureWrapMode.Clamp;
            for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {
                float dx=Mathf.Max(7-x,0,x-24),dy=Mathf.Max(7-y,0,y-24);
                roundedTexture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(7.5f-Mathf.Sqrt(dx*dx+dy*dy))));
            }
            roundedTexture.Apply();
            rounded=Sprite.Create(roundedTexture,new Rect(0,0,32,32),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(8,8,8,8));
        }
        RectTransform Region(string name,Transform parent,float x,float y,float w,float h,Color? background=null,bool soft=false)
        {
            var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);
            var rect=obj.GetComponent<RectTransform>();rect.anchorMin=new Vector2(x,y);rect.anchorMax=new Vector2(x+w,y+h);rect.offsetMin=rect.offsetMax=Vector2.zero;
            if(background.HasValue)
            {
                var image=obj.AddComponent<UnityEngine.UI.Image>();image.color=background.Value;image.raycastTarget=false;
                if(soft){image.sprite=rounded;image.type=UnityEngine.UI.Image.Type.Sliced;}
            }
            return rect;
        }
        TMP_Text Text(Transform parent,string name,string value,float size,float x,float y,float w,float h,Color? color=null,bool bold=false)
        {
            var rect=Region(name,parent,x,y,w,h);
            var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.text=value;label.fontSize=size;label.color=color??ink;
            label.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;label.margin=new Vector4(5,3,5,3);
            label.alignment=TextAlignmentOptions.MidlineLeft;label.raycastTarget=false;label.textWrappingMode=TextWrappingModes.Normal;
            label.overflowMode=TextOverflowModes.Truncate;return label;
        }
        void Icon(Transform parent,int kind,Color color,float x=.035f,float y=.28f,float w=.12f,float h=.44f)
        {
            var rect=Region("Symbol",parent,x,y,w,h);var graphic=rect.gameObject.AddComponent<DashboardSymbol>();graphic.Kind=kind;graphic.color=color;graphic.raycastTarget=false;
        }
        UnityEngine.UI.Button Button(Transform parent,string name,float x,float y,float w,float h,Action action)
        {
            var rect=Region(name,parent,x,y,w,h,tint,true);rect.offsetMin=new Vector2(3,3);rect.offsetMax=new Vector2(-3,-3);
            var image=rect.GetComponent<UnityEngine.UI.Image>();image.raycastTarget=true;
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
            var colors=button.colors;colors.highlightedColor=new Color(.78f,.91f,.90f);colors.pressedColor=new Color(.62f,.82f,.80f);colors.selectedColor=Color.white;button.colors=colors;
            var label=Text(rect,"Label",name,15,0,0,1,1);label.alignment=TextAlignmentOptions.Center;
            button.onClick.AddListener(()=>action());return button;
        }
        UnityEngine.UI.Slider Slider(Transform parent,string name,float min,float max,float x,float y,float w,float h,Action<float> changed,string unit)
        {
            var group=Region(name,parent,x,y,w,h);
            var label=Text(group,"Label",name,13,0,.44f,1,.56f,ink,true);
            var track=Region("Track",group,.07f,.18f,.85f,.075f,line,true);track.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            var handle=Region("Handle",track,0,.5f,0,0,accent,true);handle.sizeDelta=new Vector2(15,22);handle.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            var slider=track.gameObject.AddComponent<UnityEngine.UI.Slider>();slider.minValue=min;slider.maxValue=max;slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<UnityEngine.UI.Image>();
            Action update=()=>{if(label!=null&&slider!=null)label.text=name+"\n<color=#08786D>"+slider.value.ToString("0.0")+" "+unit+"</color>";};
            slider.onValueChanged.AddListener(value=>{update();if(!syncing)changed(value);});sliderLabels.Add(update);
            return slider;
        }
        TMP_Text MetricCard(Transform parent,string title,int kind,float bottom,float height,bool small=false)
        {
            var box=Region(title,parent,.04f,bottom,.92f,height,tint,true);
            Icon(box,kind,kind==5?new Color(.80f,.49f,.05f):accent);
            Text(box,"Caption",title,small?12:13,.19f,.53f,.78f,.40f,muted);
            return Text(box,"Value","Unavailable",small?20:24,.19f,.07f,.78f,.49f,ink,true);
        }
        RectTransform Progress(Transform parent,string name,float y)
        {
            var rail=Region(name,parent,.23f,y,.69f,.012f,line,true);
            return Region("Amount",rail,0,0,0,1,accent,true);
        }
        public void Initialize(DharaApplication application)
        {
            app=application;CreateStyle();app.World.ViewCamera.rect=WorldViewport;
            var canvasObj=new GameObject("Environmental monitoring dashboard",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObj.transform.SetParent(app.transform,false);canvasObj.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasObj.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
            var input=new GameObject("Dashboard input",typeof(EventSystem),typeof(InputSystemUIInputModule));input.transform.SetParent(app.transform,false);
            var root=canvasObj.transform;
            // Surround the live camera viewport without placing an opaque overlay over it.
            Region("Left background",root,0,0,.205f,1,paper);Region("Right background",root,.745f,0,.255f,1,paper);
            Region("Top background",root,.205f,.82f,.54f,.18f,paper);Region("Bottom background",root,.205f,0,.54f,.255f,paper);
            var header=Region("Header",root,0,.91f,1,.09f,card);
            var logo=Region("DHARA logo",header,.010f,.06f,.048f,.88f).gameObject.AddComponent<UnityEngine.UI.Image>();
            logo.sprite=Resources.Load<Sprite>("Branding/DharaLogo");
            logo.preserveAspect=true;logo.raycastTarget=false;
            Text(header,"Identity","DHARA",30,.063f,.40f,.13f,.52f,ink,true);
            Text(header,"Tagline","A LIVING CITY",13,.065f,.10f,.14f,.30f,muted);
            for(int i=0;i<Location.All.Length;i++){int index=i;locations.Add(Button(header,Location.All[i].Name,.215f+i*.102f,.15f,.097f,.7f,()=>{CloseExperiment();app.SelectLocation(index);}));}
            status=Text(header,"Connection status","CONNECTING",12,.733f,.12f,.14f,.78f,muted);
            Button(header,"Refresh weather",.875f,.16f,.115f,.68f,()=>{app.Engine.Record("Weather refresh requested.");app.Weather.Refresh();});
            var left=Region("Weather data",root,.01f,.075f,.185f,.82f,card,true);
            Text(left,"Heading","WEATHER DATA",17,.035f,.935f,.93f,.052f,ink,true);
            string[] names={"TEMPERATURE","RAINFALL RATE","WIND / DIRECTION","CLOUD / HUMIDITY","SOLAR RADIATION","PRECIPITATION TOTAL"};
            int[] symbols={1,2,3,4,5,2};
            for(int i=0;i<6;i++)metrics[i]=MetricCard(left,names[i],symbols[i],.81f-i*.12f,.108f);
            var sourceBox=Region("Source card",left,.04f,.025f,.92f,.17f,tint,true);
            source=Text(sourceBox,"Source","",12,.025f,.03f,.95f,.94f,muted);
            var cityHeader=Region("City heading",root,.205f,.82f,.54f,.075f,card,true);
            cityTitle=Text(cityHeader,"Selected city","",24,.025f,.40f,.68f,.55f,ink,true);
            mode=Text(cityHeader,"Simulation mode","Awaiting weather",13,.027f,.03f,.68f,.38f,accent);
            Button(cityHeader,"Reset camera",.72f,.20f,.085f,.63f,()=>app.World.ViewCamera.GetComponent<OrbitCamera>().Home()).GetComponentInChildren<TMP_Text>().text="Reset";
            Button(cityHeader,"Walk",.8125f,.20f,.085f,.63f,()=>app.World.ViewCamera.GetComponent<OrbitCamera>().ToStreet());
            Button(cityHeader,"Tour / cancel",.905f,.20f,.085f,.63f,()=>{var camera=app.World.ViewCamera.GetComponent<OrbitCamera>();if(camera.TourRunning)camera.CancelTour();else camera.StartTour();}).GetComponentInChildren<TMP_Text>().text="Tour";
            foreach(var cameraButton in cityHeader.GetComponentsInChildren<UnityEngine.UI.Button>())
                cameraButton.GetComponentInChildren<TMP_Text>().fontSize=12;
            var right=Region("Environment status",root,.755f,.49f,.235f,.405f,card,true);
            Text(right,"Heading","ENVIRONMENT STATUS",17,.035f,.88f,.94f,.10f,ink,true);
            outputs[0]=MetricCard(right,"SIMULATED STANDING WATER",4,.65f,.22f,true);waterBar=Progress(right,"Storage progress",.667f);
            outputs[1]=MetricCard(right,"SURFACE WETNESS",0,.43f,.20f,true);wetBar=Progress(right,"Wetness progress",.443f);
            string[] rows={"DRAINAGE CAPACITY","SOLAR ESTIMATE","MODEL TIME"};
            for(int i=0;i<3;i++)
            {
                var row=Region(rows[i],right,.04f,.035f+(2-i)*.12f,.92f,.11f,tint,true);
                Icon(row,i==1?8:i==2?6:7,accent,.03f,.18f,.08f,.64f);
                Text(row,"Caption",rows[i],12,.14f,0,.52f,1,muted);
                outputs[i+2]=Text(row,"Value","",17,.65f,0,.34f,1,ink,true);
            }
            var log=Region("Activity log",root,.755f,.265f,.235f,.21f,card,true);
            Text(log,"Heading","ENVIRONMENTAL CHANGE LOG",13,.035f,.77f,.65f,.20f,ink,true);
            Button(log,"Clear log",.69f,.78f,.28f,.19f,()=>app.Engine.Events.Clear());
            for(int i=0;i<3;i++)
            {
                Region("Timeline marker "+i,log,.045f,.64f-i*.225f,.009f,.04f,i==0?accent:line,true);
                eventRows[i]=Text(log,"Event "+i,"",12,.08f,.49f-i*.225f,.88f,.23f,muted);
                eventRows[i].enableAutoSizing=true;eventRows[i].fontSizeMin=10;eventRows[i].fontSizeMax=12;
            }
            var insight=Region("Environmental insights",root,.755f,.075f,.235f,.175f,card,true);
            Text(insight,"Heading","ENVIRONMENTAL INSIGHTS",14,.035f,.79f,.93f,.19f,ink,true);
            insights=Text(insight,"Explanation","",12,.035f,.25f,.93f,.53f,muted);
            compareButton=Button(insight,"Urban Intervention Lab",.035f,.025f,.93f,.23f,OpenExperiment);

            var footer=Region("Scenario controls",root,.205f,.075f,.54f,.18f,card,true);
            string[] tabNames={"LIVE WORLD","FORECAST PREVIEW","WHAT-IF LAB"};
            for(int i=0;i<3;i++){int index=i;tabs.Add(Button(footer,tabNames[i],i/3f,.72f,1/3f,.28f,()=>{CloseExperiment();app.SetMode((WorldMode)index);}));}
            lab=Region("What-if controls",footer,0,0,1,.72f);
            rain=Slider(lab,"RAINFALL",0,200,.015f,.22f,.19f,.78f,v=>{app.Simulation.Scenario.RainMmPerHour=v;app.ApplyInputs();},"mm/h");
            wind=Slider(lab,"WIND SPEED",0,180,.21f,.22f,.19f,.78f,v=>{app.Simulation.Scenario.WindKmh=v;app.ApplyInputs();},"km/h");
            temperature=Slider(lab,"TEMPERATURE",-30,60,.405f,.22f,.19f,.78f,v=>{app.Simulation.Scenario.Temperature=v;app.ApplyInputs();},"°C");
            drainage=Slider(lab,"DRAINAGE CAPACITY",0,60,.60f,.22f,.19f,.78f,v=>{app.Simulation.Scenario.DrainageMmPerHour=v;app.ApplyInputs();},"mm/h");
            Button(lab,"Reset scenario",.80f,.51f,.19f,.42f,app.ResetScenario);
            Button(lab,"Return to live",.80f,.07f,.19f,.42f,()=>app.SetMode(WorldMode.Live));
            scenarioNote=Text(lab,"Scenario note","",10,.02f,.01f,.77f,.22f,muted);
            forecast=Region("Forecast controls",footer,.015f,0,.97f,.72f);
            forecastLabel=Text(forecast,"Forecast timestamp","",14,0,.58f,.99f,.40f,ink,true);
            timeline=Slider(forecast,"FORECAST STEP",0,1,0,.02f,.75f,.58f,v=>app.SelectForecast(Mathf.RoundToInt(v)),"");timeline.wholeNumbers=true;
            Button(forecast,"Return to live",.78f,.12f,.21f,.40f,()=>app.SetMode(WorldMode.Live));
            live=Region("Live controls",footer,.025f,.03f,.95f,.66f);
            Text(live,"Live explanation","A district responding to weather.\nExplore forecast hours or test rainfall, wind, heat and drainage in the What-if Lab.",16,0,.12f,.97f,.79f,muted);
            var footnote=Region("Footnote",root,0,0,1,.062f,card);
            Text(footnote,"Navigation","Walk: WASD / arrows; Shift faster; right drag: 360° look\n1: overview · 2: street · 3: roam · Esc: release input · click city: resume",11,.02f,0,.40f,1,muted);
            Text(footnote,"Model disclosure","SIMULATED · 60× model clock\nGeneric district · simplified water models",10,.76f,0,.23f,1,muted);
            tourControls=Region("Tour controls",root,.22f,.26f,.51f,.09f,card,true);
            navigationStatus=Text(tourControls,"Tour status","",12,.02f,.58f,.96f,.40f,accent,true);
            Button(tourControls,"Pause",0,0,.2f,.58f,()=>app.World.ViewCamera.GetComponent<OrbitCamera>().PauseTour());
            Button(tourControls,"Resume",.2f,0,.2f,.58f,()=>app.World.ViewCamera.GetComponent<OrbitCamera>().ResumeTour());
            Button(tourControls,"Restart",.4f,0,.2f,.58f,()=>app.World.ViewCamera.GetComponent<OrbitCamera>().RestartTour());
            Button(tourControls,"Exit",.6f,0,.2f,.58f,()=>app.World.ViewCamera.GetComponent<OrbitCamera>().CancelTour());
            Button(tourControls,"Overview",.8f,0,.2f,.58f,()=>app.World.ViewCamera.GetComponent<OrbitCamera>().ToOverview());
            tourControls.gameObject.SetActive(false);
            CreateExperiment(root);
            CreateStudio(root);
            app.ControlsChanged+=Sync;Sync();
        }
        void CreateExperiment(Transform root)
        {
            experimentPanel=Region("Urban Intervention Lab",root,.218f,.29f,.514f,.50f,card,true);
            experimentPanel.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            Text(experimentPanel,"Heading","Drainage depth comparison",22,.035f,.85f,.75f,.12f,ink,true);
            Button(experimentPanel,"Open spatial intervention model",.035f,.76f,.75f,.09f,()=>{CloseExperiment();OpenStudio();});
            Button(experimentPanel,"Close",.82f,.87f,.15f,.09f,CloseExperiment);
            experimentInput=Text(experimentPanel,"Captured baseline","",14,.035f,.65f,.93f,.19f,muted);
            extraDrainage=Slider(experimentPanel,"EXTRA DRAINAGE",0,60,.035f,.46f,.93f,.18f,v=>RefreshExperiment(),"mm/h");
            experimentResult=Text(experimentPanel,"Comparison","",17,.035f,.16f,.93f,.29f,ink);
            Button(experimentPanel,"Capture current baseline",.035f,.035f,.47f,.10f,CaptureExperiment);
            Button(experimentPanel,"Reset experiment",.53f,.035f,.43f,.10f,()=>{extraDrainage.value=0;RefreshExperiment();});
            experimentPanel.gameObject.SetActive(false);
        }
        public void OpenExperiment()
        {
            if(app.Simulation.Active==null || !app.Simulation.Active.HasSimulationInputs)return;
            CaptureExperiment();experimentPanel.gameObject.SetActive(true);
        }
        void CloseExperiment(){if(experimentPanel!=null)experimentPanel.gameObject.SetActive(false);}
        void CaptureExperiment()
        {
            if(!experiment.Capture(app.Simulation.Active,app.Simulation.Drainage))return;
            capturedContext=Location.All[app.Weather.Selected].Name+" · "+app.Simulation.Mode+" · "+(app.Simulation.Active.Time??"hypothetical time");
            extraDrainage.SetValueWithoutNotify(0);foreach(var update in sliderLabels)update();RefreshExperiment();
            app.Engine.Record("Intervention baseline captured; one-hour drainage comparison is separate from the running world.");
        }
        void RefreshExperiment()
        {
            var result=experiment.Compare(extraDrainage.value);
            if(result==null)return;
            experimentInput.text=capturedContext+"\nFROZEN INPUT  "+result.RainMmPerHour.ToString("0.0")+" mm/h rain · "+result.TemperatureC.ToString("0.0")+" °C\n60 model minutes, both starting at zero storage.";
            experimentResult.text="<b>BASELINE</b>  "+result.BaselineWaterMm.ToString("0.0")+" mm    →    <b>INTERVENTION</b>  "+result.InterventionWaterMm.ToString("0.0")+" mm\n"
                +"Drainage capacity "+result.BaselineDrainage.ToString("0.0")+" → "+result.InterventionDrainage.ToString("0.0")+" mm/h.\n"
                +"<size=14>MODEL RESPONSE  "+result.ReductionMm.ToString("0.0")+" mm less retained water.\nRemoval = 0.8 × capacity; identical rain and drying. Simplified comparison, not an engineering prediction.</size>";
        }
        void Sync()
        {
            syncing=true;var s=app.Simulation.Scenario;
            rain.SetValueWithoutNotify((float)s.RainMmPerHour);wind.SetValueWithoutNotify((float)s.WindKmh);temperature.SetValueWithoutNotify((float)s.Temperature);drainage.SetValueWithoutNotify((float)s.DrainageMmPerHour);
            foreach(var update in sliderLabels)update();syncing=false;
            var active=app.Simulation.Mode;live.gameObject.SetActive(active==WorldMode.Live);forecast.gameObject.SetActive(active==WorldMode.Forecast);lab.gameObject.SetActive(active==WorldMode.WhatIf);
            timeline.maxValue=Mathf.Max(1,app.Weather.Forecast.Count-1);
            timeline.interactable=app.Weather.Forecast.Count>0;
            for(int i=0;i<tabs.Count;i++)StyleButton(tabs[i],i==(int)active);
            nextRefresh=0;
        }
        void StyleButton(UnityEngine.UI.Button button,bool selected)
        {
            button.GetComponent<UnityEngine.UI.Image>().color=selected?accent:tint;
            var label=button.GetComponentInChildren<TMP_Text>();label.color=selected?Color.white:ink;label.fontStyle=selected?FontStyles.Bold:FontStyles.Normal;
        }
        static string Value(double? value,string unit,string format="0.0") => Numbers.IsFinite(value)?value.Value.ToString(format)+" "+unit:"Unavailable";
        static void Fill(RectTransform bar,double ratio){bar.anchorMax=new Vector2((float)Numbers.Clamp(ratio,0,1),1);}
        void Update()
        {
            if(app==null || Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.25f;
            var cameraControl=app.World.ViewCamera.GetComponent<OrbitCamera>();
            tourControls.gameObject.SetActive(cameraControl.TourRunning);navigationStatus.text=cameraControl.NavigationStatus;
            for(int i=0;i<viewButtons.Count;i++)StyleButton(viewButtons[i],app.Spatial.Overlay==viewModes[i]);
            var simulation=app.Simulation;var weather=simulation.Active;var state=app.Engine.State;var connection=app.Weather.Status;
            if(overlayLegend!=null)overlayLegend.text=(app.Spatial.Preview!=null?"FROZEN INTERVENTION · ":"RUNNING WORLD · ")+app.Spatial.Legend;
            if(objectPanel!=null&&objectPanel.gameObject.activeSelf)objectDetails.text=app.Spatial.InspectSelected();
            bool valid=weather!=null && weather.HasSimulationInputs;
            string connectionText=connection.Loading?"UPDATING":connection.Error!=null?"REQUEST FAILED":connection.Stale?"STALE DATA":app.Weather.Current==null?"NO LIVE DATA":!app.Weather.Current.HasSimulationInputs?"PARTIAL DATA":"WEATHER CONNECTED";
            status.text=connectionText+"\n"+(connection.RetrievedUtc.HasValue?"Retrieved "+connection.RetrievedUtc.Value.ToString("HH:mm:ss")+" UTC":"Awaiting response");
            status.color=connection.Error!=null || connection.Stale?new Color(.65f,.28f,.09f):muted;
            cityTitle.text=Location.All[app.Weather.Selected].Name+" <size=17>· Urban simulation</size>";
            mode.text=simulation.Mode==WorldMode.WhatIf?"WHAT-IF · HYPOTHETICAL":simulation.Mode!=WorldMode.Live?simulation.Mode.ToString().ToUpperInvariant()+" · "+(weather?.Time??"Unavailable")+" IST":valid?"LIVE INPUTS · "+weather.Time+" IST":"LIVE WORLD · Waiting for complete data";
            for(int i=0;i<locations.Count;i++)StyleButton(locations[i],i==app.Weather.Selected);
            metrics[0].text=Value(weather?.Temperature,"°C");metrics[1].text=Value(weather?.RainMmPerHour,"mm/h");
            metrics[2].text=!Numbers.IsFinite(weather?.WindKmh)&&!Numbers.IsFinite(weather?.WindDegrees)?"Unavailable":(Numbers.IsFinite(weather?.WindKmh)?Value(weather.WindKmh,"km/h"):"N/A")+" <size=12>"+(Numbers.IsFinite(weather?.WindDegrees)?Value(weather.WindDegrees,"°","0"):"Dir: N/A")+"</size>";
            metrics[3].text=!Numbers.IsFinite(weather?.CloudPercent)&&!Numbers.IsFinite(weather?.HumidityPercent)?"Unavailable":(Numbers.IsFinite(weather?.CloudPercent)?Value(weather.CloudPercent,"%","0"):"N/A")+" / "+(Numbers.IsFinite(weather?.HumidityPercent)?Value(weather.HumidityPercent,"%","0"):"N/A");
            metrics[4].text=Value(weather?.SolarWm2,"W/m²","0");metrics[5].text=simulation.Mode==WorldMode.WhatIf?"<size=18>Live total not used</size>":Value(weather?.PrecipitationMm,"mm");
            source.text="<b>OPEN-METEO · CC BY 4.0</b>\nModel estimates, not sensors.\nVALID  "+(weather?.Time??"Unavailable")+" IST\nRETRIEVED  "+(connection.RetrievedUtc?.ToString("HH:mm:ss 'UTC'")??"Unavailable")+"\nRain is an interval-derived rate.\nPrecipitation is a separate total.";
            if(simulation.Mode==WorldMode.Historical)source.text="<b>OPEN-METEO · ERA5 REANALYSIS</b>\nVALID "+weather?.Time+" IST\nRETRIEVED "+app.History.Status.RetrievedUtc?.ToString("HH:mm:ss 'UTC'")+"\nHistorical modeled data · CC BY 4.0\nPreceding-hour rain as mm/h.";
            if(simulation.Mode==WorldMode.Demonstration)source.text="<b>DETERMINISTIC DEMONSTRATION</b>\nExplicit synthetic input values.\nClock: "+weather?.Time+"\nNo live source / retrieval time.\nNot current weather.";
            if(simulation.Mode==WorldMode.Recorded)source.text="<b>RECORDED SCENARIO</b>\nSource time "+weather?.Time+" IST\nLoaded snapshot; not current live data.\nInspect provenance in scenario reports.";
            outputs[0].text=valid?state.CollectionM3.ToString("0.0")+" <size=14>/ 150 m³</size>":"<size=17>Simulation paused</size>";
            outputs[1].text=valid?(state.Wetness*100).ToString("0")+"%":"Unavailable";
            outputs[2].text=simulation.Drainage.ToString("0.0")+" mm/h";
            outputs[3].text=valid && Numbers.IsFinite(weather.SolarWm2)?state.SolarKw.ToString("0.00")+" kW":"Unavailable";
            outputs[4].text=(state.ElapsedSimulatedSeconds/60).ToString("0.0")+" min";Fill(waterBar,valid?state.CollectionM3/150:0);Fill(wetBar,valid?state.Wetness:0);
            for(int i=0;i<eventRows.Length;i++)
            {if(i>=app.Engine.Events.Count){eventRows[i].text="";continue;}var item=app.Engine.Events[i];string message=item.Message.Replace('\n',' ');eventRows[i].text="<color=#08786D>"+item.Timestamp.ToString("HH:mm:ss")+"</color>  "+message;}
            if(valid)
            {
                var r=app.Engine.District.Result;
                insights.text="<b>INPUT</b> "+weather.RainMmPerHour.Value.ToString("0.0")+" mm/h rain\n<b>DISTRICT</b> "+r.RainM3.ToString("0.0")+" m³ rain · "+r.InfiltratedM3.ToString("0.0")+" m³ infiltrated\n<b>REMOVAL</b> "+r.DrainedM3.ToString("0.0")+" m³ drained";
            }
            else insights.text="<b>INPUT</b> Required weather unavailable.\n<b>MODEL</b> Integration paused.\n<b>MEANING</b> Use the What-if Lab offline.";
            compareButton.interactable=valid;
            forecastLabel.text=simulation.Forecast!=null?"FORECAST VALID  "+simulation.Forecast.Time+" IST":"No forecast available. Refresh weather to retry.";
            timeline.maxValue=Mathf.Max(1,app.Weather.Forecast.Count-1);timeline.interactable=app.Weather.Forecast.Count>0;
            if(simulation.Mode==WorldMode.Forecast && simulation.Forecast!=null){int index=app.Weather.Forecast.IndexOf(simulation.Forecast);if(index>=0)timeline.SetValueWithoutNotify(index);}
            scenarioNote.text=app.Weather.Current==null?"Offline: assumed 50% cloud; solar / humidity / direction unavailable.":"Hypothetical inputs. Other variables retain the live baseline.";
        }
        void OnDestroy(){if(app!=null)app.ControlsChanged-=Sync;if(rounded!=null)Destroy(rounded);if(roundedTexture!=null)Destroy(roundedTexture);}
    }

    // Small code-drawn line symbols: no font glyph dependence or external asset package.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DashboardSymbol : UnityEngine.UI.MaskableGraphic
    {
        public int Kind;
        UnityEngine.UI.VertexHelper mesh;
        void Line(float x,float y,float a,float b)
        {
            var r=GetPixelAdjustedRect();float size=Mathf.Min(r.width,r.height);
            Vector2 p=r.center+new Vector2(x-.5f,y-.5f)*size,q=r.center+new Vector2(a-.5f,b-.5f)*size;
            Vector2 n=(q-p).normalized;n=new Vector2(-n.y,n.x)*Mathf.Max(1,size*.027f);
            int start=mesh.currentVertCount;mesh.AddVert(p-n,color,Vector2.zero);mesh.AddVert(p+n,color,Vector2.zero);mesh.AddVert(q+n,color,Vector2.zero);mesh.AddVert(q-n,color,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
        }
        void Circle(float x,float y,float radius){for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Line(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius);}}
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            mesh=vh;mesh.Clear();
            switch(Kind)
            {
                case 1:Circle(.5f,.25f,.16f);Line(.43f,.39f,.43f,.85f);Line(.57f,.39f,.57f,.85f);Line(.43f,.85f,.57f,.85f);Line(.5f,.27f,.5f,.72f);break;
                case 2:Circle(.40f,.64f,.19f);Circle(.62f,.59f,.16f);Line(.2f,.43f,.8f,.43f);for(int i=0;i<3;i++)Line(.3f+i*.2f,.31f,.24f+i*.2f,.15f);break;
                case 3:for(int i=0;i<3;i++){Line(.12f,.3f+i*.2f,.7f,.3f+i*.2f);Line(.7f,.3f+i*.2f,.83f,.38f+i*.2f);}break;
                case 4:Line(.5f,.9f,.22f,.42f);Line(.22f,.42f,.24f,.24f);Line(.24f,.24f,.5f,.12f);Line(.5f,.12f,.76f,.24f);Line(.76f,.24f,.78f,.42f);Line(.78f,.42f,.5f,.9f);break;
                case 5:Circle(.5f,.5f,.21f);for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Line(.5f+Mathf.Cos(a)*.30f,.5f+Mathf.Sin(a)*.30f,.5f+Mathf.Cos(a)*.44f,.5f+Mathf.Sin(a)*.44f);}break;
                case 6:Circle(.5f,.5f,.36f);Line(.5f,.5f,.5f,.76f);Line(.5f,.5f,.7f,.5f);break;
                case 7:Line(.2f,.8f,.4f,.8f);Line(.4f,.8f,.4f,.3f);Line(.4f,.3f,.8f,.3f);Line(.2f,.8f,.2f,.15f);Line(.2f,.15f,.8f,.15f);break;
                case 8:for(int i=0;i<3;i++){Line(.2f,.25f+i*.25f,.8f,.25f+i*.25f);Line(.2f+i*.3f,.25f,.2f+i*.3f,.75f);}Line(.5f,.25f,.5f,.10f);break;
                default:for(int row=0;row<3;row++)for(int i=0;i<18;i++){float x=.06f+i*.049f,a=x+.049f;Line(x,.25f+row*.23f+Mathf.Sin(x*7)*.06f,a,.25f+row*.23f+Mathf.Sin(a*7)*.06f);}break;
            }
        }
    }
}
