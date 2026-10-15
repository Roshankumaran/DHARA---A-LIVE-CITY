using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace Dhara
{
    public sealed partial class EnvironmentalDashboard
    {
        RectTransform toolPanel;
        Coroutine analysisJob;
        int toolLabelStart=-1;
        readonly List<EvaluatedIntervention> evaluated=new List<EvaluatedIntervention>();
        public EvaluatedIntervention PlannerBest {get;private set;}
        public bool PlannerSearching {get;private set;}
        void CloseTool()
        {
            if(analysisJob!=null){StopCoroutine(analysisJob);analysisJob=null;}
            PlannerSearching=false;
            if(toolLabelStart>=0&&toolLabelStart<=sliderLabels.Count)sliderLabels.RemoveRange(toolLabelStart,sliderLabels.Count-toolLabelStart);
            toolLabelStart=-1;if(toolPanel!=null)Destroy(toolPanel.gameObject);toolPanel=null;
        }
        RectTransform Tool(string title)
        {
            CloseTool();toolLabelStart=sliderLabels.Count;
            studioPanel.gameObject.SetActive(false);
            toolPanel=Region(title,studioPanel.parent,.208f,.08f,.534f,.735f,card,true);toolPanel.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            Text(toolPanel,"Heading",title,22,.025f,.91f,.78f,.08f,ink,true);
            Button(toolPanel,"Close",.83f,.925f,.15f,.06f,CloseTool);return toolPanel;
        }
        void OpenTools()
        {
            var panel=Tool("CITY TOOLS");
            Button(panel,"Impact trace",.03f,.75f,.46f,.12f,OpenTrace);
            Button(panel,"Intervention planner",.51f,.75f,.46f,.12f,OpenPlanner);
            Button(panel,"Environmental timeline",.03f,.59f,.46f,.12f,OpenTimeline);
            Button(panel,"Forecast-to-impact",.51f,.59f,.46f,.12f,OpenForecastImpact);
            Button(panel,"Air quality / advisories",.03f,.43f,.46f,.12f,OpenDataSources);
            Button(panel,"Five-city comparison",.51f,.43f,.46f,.12f,OpenCityComparison);
            Button(panel,"Scenario save / reports",.03f,.27f,.46f,.12f,OpenScenarioFiles);
            Button(panel,"Guided demonstration",.51f,.27f,.46f,.12f,OpenDemonstration);
            Button(panel,"Sensitivity analysis",.03f,.11f,.94f,.12f,OpenSensitivity);
            Text(panel,"Scope","Shared weather · documented catchment · reproducible model outputs",12,.04f,.02f,.92f,.06f,muted);
        }
        public void OpenForecastImpact()
        {
            if(app.Weather.Forecast.Count==0){var p=Tool("FORECAST IMPACT UNAVAILABLE");Text(p,"Reason","No source forecast is available. Refresh weather or return to live conditions.",18,.04f,.4f,.92f,.35f,ink);return;}
            var input=app.Simulation.Forecast??app.Weather.Forecast[0];
            app.SelectTemporal(input,false);OpenStudio();
        }
        void OpenPlanner()
        {
            if(Comparison==null)CaptureStudio();if(Comparison==null)return;
            var frozen=Comparison;var panel=Tool("INTERVENTION PLANNER");
            Text(panel,"Forcing",frozen.Location+" · "+frozen.Classification+" · "+frozen.Input.Time+" IST\nFrozen rain "+frozen.Input.RainMmPerHour.Value.ToString("0.0")+" mm/h · initially dry · 60 minutes",14,.03f,.80f,.94f,.10f,muted);
            var maxRoof=Slider(panel,"MAX GREEN ROOF AREA",0,1404,.03f,.66f,.46f,.13f,v=>{},"m²");maxRoof.SetValueWithoutNotify(1404);
            var maxPaving=Slider(panel,"MAX PERMEABLE AREA",0,2128,.51f,.66f,.46f,.13f,v=>{},"m²");maxPaving.SetValueWithoutNotify(2128);
            var maxDrain=Slider(panel,"MAX EXTRA DRAINAGE",0,60,.03f,.53f,.46f,.13f,v=>{},"mm/h");maxDrain.SetValueWithoutNotify(20);
            var maxGarden=Slider(panel,"MAX GARDEN STORAGE",0,50,.51f,.53f,.46f,.13f,v=>{},"m³");maxGarden.SetValueWithoutNotify(20);
            foreach(var label in sliderLabels)label();
            PlanningObjective objective=PlanningObjective.PeakRunoff;
            var objectiveButton=Button(panel,"Objective: peak runoff",.03f,.45f,.60f,.07f,()=>{});
            objectiveButton.onClick.AddListener(()=>{objective=objective==PlanningObjective.PeakRunoff?PlanningObjective.MaximumStorage:PlanningObjective.PeakRunoff;objectiveButton.GetComponentInChildren<TMP_Text>().text=objective==PlanningObjective.PeakRunoff?"Objective: peak runoff":"Objective: maximum storage";});
            var result=Text(panel,"Evaluated configurations","Choose limits, then evaluate. Zero limits disable that option.\nGrid levels: 0 / 50 / 100% of area limits; 0 / maximum for drainage and garden.",14,.03f,.13f,.94f,.30f,ink);
            int page=0;
            Action draw=()=>{string rows="CASE · roof m² / paving m² / extra drain / garden → peak m³/h / max m³\n";for(int i=page*5;i<Math.Min(evaluated.Count,page*5+5);i++){var e=evaluated[i];rows+=$"{e.Index}: {e.Configuration.GreenRoofFraction*1404:0} / {e.Configuration.PermeableFraction*2128:0} / {e.Configuration.ExtraDrainage:0} / {e.Configuration.GardenCapacityM3:0} → {e.Result.PeakRunoffM3Hour:0.00} / {e.Result.MaximumStorageM3:0.00}\n";}result.text=rows+$"{evaluated.Count} cases evaluated · "+(PlannerSearching?"SEARCH RUNNING\n":"SEARCH COMPLETE\n")+(PlannerBest==null?"":$"Best evaluated case {PlannerBest.Index}; baseline peak {frozen.Baseline.PeakRunoffM3Hour:0.00} → {PlannerBest.Result.PeakRunoffM3Hour:0.00} m³/h. More treated area / storage required.");};
            Button(panel,"Evaluate",.66f,.45f,.31f,.07f,()=>{if(analysisJob!=null)StopCoroutine(analysisJob);evaluated.Clear();PlannerBest=null;page=0;var max=new InterventionConfig{GreenRoofFraction=maxRoof.value/1404,PermeableFraction=maxPaving.value/2128,ExtraDrainage=maxDrain.value,GardenCapacityM3=maxGarden.value};analysisJob=StartCoroutine(Search(frozen,max,objective,draw));});
            Button(panel,"Previous",.03f,.055f,.20f,.06f,()=>{page=Math.Max(0,page-1);draw();});
            Button(panel,"Next",.24f,.055f,.20f,.06f,()=>{page=Math.Min(Math.Max(0,(evaluated.Count-1)/5),page+1);draw();});
            Button(panel,"Inspect best comparison",.46f,.055f,.51f,.06f,()=>{if(PlannerBest==null)return;var c=PlannerBest.Configuration;districtExperiment.Capture(frozen.Input,frozen.Drainage,frozen.Classification,frozen.Location,frozen.RetrievedUtc);roofCoverage.SetValueWithoutNotify((float)c.GreenRoofFraction*100);pavingCoverage.SetValueWithoutNotify((float)c.PermeableFraction*100);studioDrainage.SetValueWithoutNotify((float)c.ExtraDrainage);gardenCapacity.SetValueWithoutNotify((float)c.GardenCapacityM3);CloseTool();studioPanel.gameObject.SetActive(true);foreach(var update in sliderLabels)update();RunComparison();});
            Text(panel,"Limit","BEST EVALUATED CONFIGURATION WITHIN THE SELECTED MODEL AND SEARCH SPACE.",10,.03f,.005f,.94f,.04f,muted);
        }
        IEnumerator Search(DistrictComparison frozen,InterventionConfig maximum,PlanningObjective objective,Action draw)
        {
            PlannerSearching=true;
            foreach(var candidate in InterventionPlanner.Evaluate(frozen.Input.RainMmPerHour.Value,frozen.Drainage,maximum))
            {evaluated.Add(candidate);if(InterventionPlanner.Better(candidate,PlannerBest,objective))PlannerBest=candidate;draw();yield return null;}
            analysisJob=null;PlannerSearching=false;draw();
        }
    }
}
