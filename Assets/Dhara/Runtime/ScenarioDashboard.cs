using System;
using System.IO;
using UnityEngine;
namespace Dhara
{
    public sealed partial class EnvironmentalDashboard
    {
        public string ScenarioStorageRoot;
        public string ScenarioDirectory => Path.Combine(ScenarioStorageRoot??Application.persistentDataPath,"Scenarios");
        public string SaveSelectedScenario(string name)
        {
            string json=ScenarioReport.Serialize(Comparison,name);Directory.CreateDirectory(ScenarioDirectory);
            string path=Path.Combine(ScenarioDirectory,"scenario.json");File.WriteAllText(path,json);return path;
        }
        public void LoadSelectedScenario()
        {
            string path=Path.Combine(ScenarioDirectory,"scenario.json");if(new FileInfo(path).Length>2000000)throw new FormatException("Scenario file too large");
            var snapshot=ScenarioReport.Parse(File.ReadAllText(path));var c=snapshot.Comparison;
            districtExperiment.Capture(c.Input,c.Drainage,"Recorded",c.Location,c.RetrievedUtc,c.OriginClassification);
            roofCoverage.SetValueWithoutNotify((float)c.Configuration.GreenRoofFraction*100);pavingCoverage.SetValueWithoutNotify((float)c.Configuration.PermeableFraction*100);studioDrainage.SetValueWithoutNotify((float)c.Configuration.ExtraDrainage);gardenCapacity.SetValueWithoutNotify((float)c.Configuration.GardenCapacityM3);
            int city=Array.FindIndex(Location.All,l=>l.Name==c.Location);if(city!=app.Weather.Selected)app.SelectLocation(city);
            app.SetMode(WorldMode.Recorded);app.RecordedRetrievedUtc=c.RetrievedUtc;app.RecordedOriginClassification=c.OriginClassification;app.Simulation.Recorded=c.Input.Copy();app.ApplyInputs();foreach(var update in sliderLabels)update();RunComparison();
        }
        void OpenScenarioFiles()
        {
            if(Comparison==null)CaptureStudio();var panel=Tool("SCENARIO SNAPSHOT / REPORTS");
            var name=Input(panel,"Scenario name","District experiment",.03f,.76f,.94f,.09f);
            var message=Text(panel,"File status","Local folder: "+ScenarioDirectory+"\nOne named snapshot slot; saving replaces scenario.json. JSON and CSV reports replace comparison.json / comparison.csv. Loaded data is always RECORDED, never current live data.",16,.03f,.25f,.94f,.34f,ink);
            void Attempt(Action action){try{action();}catch(Exception e){message.text="Operation failed safely: "+e.Message;}}
            Button(panel,"Save selected comparison",.03f,.64f,.46f,.09f,()=>Attempt(()=>message.text="Saved "+SaveSelectedScenario(name.text)));
            Button(panel,"Load recorded snapshot",.51f,.64f,.46f,.09f,()=>Attempt(()=>{LoadSelectedScenario();message.text="Loaded and recalculated recorded snapshot. Live weather retained.\nSource valid "+Comparison.Input.Time+" IST; collection "+Comparison.Intervention.StorageM3.ToString("0.000")+" m³.";}));
            Button(panel,"Export JSON + CSV",.03f,.13f,.46f,.09f,()=>Attempt(()=>{if(Comparison==null)throw new InvalidOperationException("No comparison available");Directory.CreateDirectory(ScenarioDirectory);File.WriteAllText(Path.Combine(ScenarioDirectory,"comparison.json"),ScenarioReport.Serialize(Comparison,name.text));File.WriteAllText(Path.Combine(ScenarioDirectory,"comparison.csv"),ScenarioReport.Csv(Comparison));message.text="Exported actual comparison to:\n"+Path.Combine(ScenarioDirectory,"comparison.json")+"\n"+Path.Combine(ScenarioDirectory,"comparison.csv");}));
            Button(panel,"Inspect loaded comparison",.51f,.13f,.46f,.09f,()=>{CloseTool();studioPanel.gameObject.SetActive(true);});
            Button(panel,"Return to live",.03f,.025f,.94f,.08f,()=>{app.SetMode(WorldMode.Live);CloseTool();});
        }
    }
}
