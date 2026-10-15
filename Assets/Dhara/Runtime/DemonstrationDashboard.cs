using TMPro;
using UnityEngine;
namespace Dhara
{
    public sealed partial class EnvironmentalDashboard
    {
        RectTransform guidePanel;TMP_Text guideText;
        bool demoDeterministic;
        public bool DemoRunning {get;private set;}
        public int DemoStage {get;private set;}
        void OpenDemonstration()
        {
            var panel=Tool("REPRODUCIBLE GUIDED DEMONSTRATION");
            Text(panel,"Guide","Choose a genuine live connection or a separate deterministic demonstration. The guide runs the actual district model, inspects a roof, calculates an intervention, and exposes its causal trace.\n\nDeterministic inputs: 50 mm/h rain, 30 km/h wind, 25 °C, 85% cloud. The displayed time is a synthetic demonstration clock; pollutant, solar, humidity and precipitation totals remain unavailable.",18,.04f,.37f,.92f,.48f,ink);
            Button(panel,"Start with live data",.04f,.22f,.92f,.10f,()=>StartDemo(false));
            Button(panel,"Start deterministic demonstration",.04f,.08f,.92f,.10f,()=>StartDemo(true));
        }
        public void StartDemo(bool deterministic)
        {
            CloseTool();studioPanel.gameObject.SetActive(false);CloseExperiment();
            demoDeterministic=deterministic;DemoRunning=true;DemoStage=0;
            if(deterministic)
            {app.SetMode(WorldMode.Demonstration);app.Simulation.Demonstration=new WeatherData{Time="2000-01-01T12:00",RainMmPerHour=50,WindKmh=30,Temperature=25,CloudPercent=85,WindDegrees=90};app.ApplyInputs();}
            else app.SelectLocation(0);
            ResetStudio();app.Spatial.ShowComparison(null);
            if(guidePanel==null)
            {
                guidePanel=Region("Guided demonstration",studioPanel.parent,.755f,.075f,.235f,.405f,card,true);guidePanel.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
                Text(guidePanel,"Heading","GUIDED DEMONSTRATION",16,.03f,.87f,.94f,.11f,ink,true);
                guideText=Text(guidePanel,"Actual step","",14,.03f,.25f,.94f,.61f,ink);
                Button(guidePanel,"Next step",.03f,.13f,.60f,.09f,()=>AdvanceDemo());
                Button(guidePanel,"Reset",.65f,.13f,.32f,.09f,()=>StartDemo(demoDeterministic));
                Button(guidePanel,"Stop / return live",.03f,.025f,.94f,.09f,StopDemo);
            }
            guidePanel.gameObject.SetActive(true);UpdateGuide();
        }
        public bool AdvanceDemo()
        {
            if(!DemoRunning||app.Simulation.Active==null||!app.Simulation.Active.HasSimulationInputs){if(guideText!=null)guideText.text="Waiting for complete live weather. Refresh weather or stop and choose deterministic demonstration.";return false;}
            DemoStage++;
            switch(DemoStage)
            {
                case 1:
                    app.EvaluateActiveHour();CaptureStudio();break;
                case 2:
                    app.Spatial.ShowComparison(null);app.Spatial.SetOverlay(DistrictOverlay.Surfaces);app.Spatial.SelectObject("roof-1");break;
                case 3:
                    roofCoverage.SetValueWithoutNotify(50);pavingCoverage.SetValueWithoutNotify(50);studioDrainage.SetValueWithoutNotify(12);gardenCapacity.SetValueWithoutNotify(10);foreach(var update in sliderLabels)update();RunComparison();break;
                case 4:
                    app.Spatial.Highlight("green-roof");app.Spatial.SetOverlay(DistrictOverlay.Intervention);break;
                default:
                    ResetStudio();app.SetMode(WorldMode.Live);DemoRunning=false;break;
            }
            UpdateGuide();return true;
        }
        void UpdateGuide()
        {
            if(guideText==null)return;
            string intro=demoDeterministic?"DETERMINISTIC · NOT LIVE\nSynthetic clock: 2000-01-01 12:00\n":"LIVE PROVIDER INPUT\n";
            string step=DemoStage==0?"1 / 6 · Inspect source weather, then advance.\nRain "+Value(app.Simulation.Active?.RainMmPerHour,"mm/h"):
                DemoStage==1?"2 / 6 · Calculated a dry-start one-hour baseline.\nCollection "+Comparison.Baseline.StorageM3.ToString("0.000")+" m³":
                DemoStage==2?"3 / 6 · Selected roof-1.\n"+app.Spatial.InspectSelected():
                DemoStage==3?"4 / 6 · Intervention calculated.\n50% green roof + 50% permeable paving\n12 mm/h extra drainage + 10 m³ garden\nCollection "+Comparison.Baseline.StorageM3.ToString("0.00")+" → "+Comparison.Intervention.StorageM3.ToString("0.00")+" m³":
                DemoStage==4?"5 / 6 · Surface response highlighted.\n"+CurrentTrace.Stages[2].Explanation:
                "6 / 6 · Interventions reset; returned to live mode. The live request status remains visible in the header.";
            guideText.text=intro+step;
        }
        public void StopDemo(){DemoRunning=false;ResetStudio();app.SetMode(WorldMode.Live);if(guidePanel!=null)guidePanel.gameObject.SetActive(false);}
    }
}
