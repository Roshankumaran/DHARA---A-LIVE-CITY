using System;
using TMPro;
using UnityEngine;
namespace Dhara
{
    public sealed partial class EnvironmentalDashboard
    {
        RectTransform studioPanel,comparisonBars;
        TMP_Text studioContext,studioResults,studioMessage;
        TMP_Text objectDetails;
        RectTransform objectPanel;
        RectTransform studioDetail;
        TMP_Text overlayLegend;
        UnityEngine.UI.Slider roofCoverage,pavingCoverage,studioDrainage,gardenCapacity;
        readonly DistrictExperiment districtExperiment=new DistrictExperiment();
        public DistrictComparison Comparison {get;private set;}
        public ImpactTrace CurrentTrace {get;private set;}
        public InterventionConfig StudioConfiguration => new InterventionConfig{GreenRoofFraction=roofCoverage.value/100,PermeableFraction=pavingCoverage.value/100,ExtraDrainage=studioDrainage.value,GardenCapacityM3=gardenCapacity.value};
        void CreateStudio(Transform root)
        {
            var overlays=Region("Spatial overlays",root,.21f,.765f,.53f,.05f,card,true);
            string[] overlayNames={"Normal","Water","Runoff","Surfaces","Connections","Coverage"};
            overlayLegend=Text(root,"Overlay legend","",11,.21f,.725f,.53f,.04f,muted);
            for(int i=0;i<overlayNames.Length;i++){int index=i;Button(overlays,overlayNames[i],i/6f,0,1/6f,1,()=>app.Spatial.SetOverlay((DistrictOverlay)index));}
            objectPanel=Region("Infrastructure explorer",root,.755f,.075f,.235f,.405f,card,true);objectPanel.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            Text(objectPanel,"Title","INFRASTRUCTURE EXPLORER",16,.03f,.88f,.94f,.10f,ink,true);
            objectDetails=Text(objectPanel,"Actual object properties","",14,.03f,.20f,.94f,.67f,ink);
            Button(objectPanel,"Edit interventions",.03f,.10f,.64f,.09f,OpenStudio);
            Button(objectPanel,"Deselect",.68f,.10f,.29f,.09f,()=>app.Spatial.Deselect());
            Button(objectPanel,"Running world",.03f,.005f,.94f,.09f,()=>{app.Spatial.ShowComparison(null);objectDetails.text=app.Spatial.InspectSelected();});
            objectPanel.gameObject.SetActive(false);
            app.Spatial.SelectionChanged+=()=>{objectPanel.gameObject.SetActive(app.Spatial.Selected!=null);objectDetails.text=app.Spatial.InspectSelected();};
            Button(root,"Resilience Studio",.58f,.012f,.16f,.045f,OpenStudio);
            Button(root,"City tools",.43f,.012f,.14f,.045f,OpenTools);
            studioPanel=Region("Neighborhood Resilience Studio",root,.208f,.08f,.534f,.735f,card,true);
            studioPanel.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            Text(studioPanel,"Title","NEIGHBORHOOD RESILIENCE STUDIO",20,.025f,.92f,.78f,.07f,ink,true);
            Button(studioPanel,"Close",.83f,.93f,.15f,.055f,()=>studioPanel.gameObject.SetActive(false));
            studioContext=Text(studioPanel,"Frozen inputs","",13,.025f,.79f,.95f,.12f,muted);
            roofCoverage=Slider(studioPanel,"GREEN ROOFS",0,100,.025f,.64f,.46f,.14f,v=>RunComparison(),"% roofs");
            pavingCoverage=Slider(studioPanel,"PERMEABLE PAVING",0,100,.51f,.64f,.46f,.14f,v=>RunComparison(),"% roads");
            studioDrainage=Slider(studioPanel,"EXTRA DRAINAGE",0,60,.025f,.50f,.46f,.14f,v=>RunComparison(),"mm/h");
            gardenCapacity=Slider(studioPanel,"GARDEN STORAGE",0,50,.51f,.50f,.46f,.14f,v=>RunComparison(),"m³");
            studioResults=Text(studioPanel,"Calculated comparison","",14,.025f,.20f,.95f,.29f,ink);
            comparisonBars=Region("Maximum storage comparison",studioPanel,.04f,.15f,.92f,.04f,tint);
            Region("Baseline",comparisonBars,0,.55f,0,.35f,new Color(.32f,.53f,.72f));
            Region("Intervention",comparisonBars,0,.05f,0,.35f,accent);
            Text(studioPanel,"Chart legend","MAX COLLECTION · Blue: baseline · Teal: intervention · scale 0–150 m³",10,.04f,.115f,.92f,.03f,muted);
            Button(studioPanel,"Capture inputs",.025f,.055f,.30f,.06f,CaptureStudio);
            Button(studioPanel,"Reset interventions",.34f,.055f,.33f,.06f,ResetStudio);
            Button(studioPanel,"Assumptions",.68f,.055f,.29f,.06f,()=>ShowStudioInformation(DistrictHydrology.Assumptions));
            studioMessage=Text(studioPanel,"Status","SIMPLIFIED SIMULATION · Initially dry · 60 minutes · 6-second steps",11,.025f,.006f,.95f,.043f,muted);
            studioPanel.gameObject.SetActive(false);
        }
        public void OpenStudio(){CloseTool();CloseStudioDetail();CaptureStudio();studioPanel.gameObject.SetActive(true);}
        void CloseStudioDetail(){if(studioDetail!=null)Destroy(studioDetail.gameObject);studioDetail=null;}
        void CaptureStudio()
        {
            if(!districtExperiment.Capture(app.Simulation.Active,app.Simulation.Drainage,app.Simulation.Mode.ToString(),Location.All[app.Weather.Selected].Name,app.ActiveRetrievedUtc,app.Simulation.Mode==WorldMode.Recorded?app.RecordedOriginClassification:null))
            {studioContext.text="Complete inputs unavailable. Select What-if Lab to define a hypothetical scenario.";return;}
            RunComparison();
        }
        public void ResetStudio()
        {roofCoverage.SetValueWithoutNotify(0);pavingCoverage.SetValueWithoutNotify(0);studioDrainage.SetValueWithoutNotify(0);gardenCapacity.SetValueWithoutNotify(0);foreach(var update in sliderLabels)update();RunComparison();}
        public void RunComparison()
        {
            Comparison=districtExperiment.Compare(StudioConfiguration);if(Comparison==null)return;
            CurrentTrace=ImpactTrace.From(Comparison);
            var c=Comparison;studioContext.text=c.Location+" · "+c.Classification+" derived experiment\nValid "+(c.Input.Time??"Hypothetical")+" IST · rain "+c.Input.RainMmPerHour.Value.ToString("0.0")+" mm/h\nFrozen forcing; baseline and intervention both start dry for 60 minutes.";
            studioResults.text="<b>METRIC                       BASELINE → INTERVENTION    Δ</b>\n"+
                CompareRow("Peak runoff m³/h",c.Baseline.PeakRunoffM3Hour,c.Intervention.PeakRunoffM3Hour)+
                CompareRow("Max collection m³",c.Baseline.MaximumStorageM3,c.Intervention.MaximumStorageM3)+
                CompareRow("Retained m³",c.Baseline.RetainedM3,c.Intervention.RetainedM3)+
                CompareRow("Infiltrated m³",c.Baseline.InfiltratedM3,c.Intervention.InfiltratedM3)+
                CompareRow("Drained m³",c.Baseline.DrainedM3,c.Intervention.DrainedM3)+
                CompareRow("Overflow m³",c.Baseline.OverflowM3,c.Intervention.OverflowM3);
            var before=(RectTransform)comparisonBars.Find("Baseline");var after=(RectTransform)comparisonBars.Find("Intervention");
            before.anchorMax=new Vector2((float)Numbers.Clamp(c.Baseline.MaximumStorageM3/150,0,1),before.anchorMax.y);
            after.anchorMax=new Vector2((float)Numbers.Clamp(c.Intervention.MaximumStorageM3/150,0,1),after.anchorMax.y);
            app.Spatial.ShowComparison(c);
        }
        static string CompareRow(string name,double a,double b)
        {double? p=DistrictComparison.Percent(a,b);return name+"    "+a.ToString("0.00")+" → "+b.ToString("0.00")+"    "+(b-a).ToString("+0.00;-0.00;0.00")+" ("+(p.HasValue?p.Value.ToString("+0.0;-0.0;0.0")+"%":"N/A")+")\n";}
        void ShowStudioInformation(string message)
        {
            CloseStudioDetail();var panel=Region("Model information",studioPanel,.02f,.18f,.96f,.72f,card,true);studioDetail=panel;panel.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            Text(panel,"Information",message,16,.035f,.14f,.93f,.82f,ink);
            Button(panel,"Back to comparison",.2f,.02f,.6f,.10f,CloseStudioDetail);
        }
        public void OpenTrace()
        {
            CloseTool();
            CloseStudioDetail();
            if(CurrentTrace==null)CaptureStudio();if(CurrentTrace==null)return;
            studioPanel.gameObject.SetActive(true);
            var panel=Region("Environmental impact trace",studioPanel,.02f,.18f,.96f,.72f,card,true);studioDetail=panel;panel.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            Text(panel,"Trace heading","ACTUAL INTERVENTION CALCULATION · "+CurrentTrace.CalculatedUtc.ToString("HH:mm:ss 'UTC'"),14,.025f,.87f,.95f,.10f,ink,true);
            var detail=Text(panel,"Stage detail",CurrentTrace.Stages[0].Explanation,17,.035f,.14f,.93f,.35f,ink);
            for(int i=0;i<CurrentTrace.Stages.Count;i++)
            {int stage=i;Button(panel,CurrentTrace.Stages[i].Title,.025f+(i%2)*.48f,.78f-(i/2)*.095f,.46f,.085f,()=>{detail.text=CurrentTrace.Stages[stage].Explanation;app.Spatial.Highlight(CurrentTrace.Stages[stage].ZoneId);});}
            Button(panel,"Back",.03f,.02f,.44f,.10f,CloseStudioDetail);
            Button(panel,"Show highlight in 3D",.51f,.02f,.46f,.10f,()=>studioPanel.gameObject.SetActive(false));
        }
    }
}
