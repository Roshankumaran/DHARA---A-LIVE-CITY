using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Dhara.Tests
{
    public class ResilienceTests
    {
        [UnityTest] public IEnumerator HistoricalAndOptionalFailureRemainSeparateFromLive()
        {
            yield return SceneManager.LoadSceneAsync("Dhara");yield return null;var app=Object.FindFirstObjectByType<DharaApplication>();
            app.History.Load(0,"2020-01-01");float deadline=Time.realtimeSinceStartup+50;
            while(app.History.Status.Loading&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(app.History.Records.Count,Is.EqualTo(24),app.History.Status.Error);
            var live=app.Simulation.Live;var historical=app.History.Records[6];app.SelectTemporal(historical,true);
            Assert.That(app.Simulation.Active.Time,Is.EqualTo("2020-01-01T06:00"));Assert.That(app.Simulation.Live,Is.SameAs(live));
            app.AirQuality.RequestUrl=l=>"https://dhara-unavailable.invalid/air";app.AirQuality.Select(0);deadline=Time.realtimeSinceStartup+25;
            while(app.AirQuality.Status.Loading&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(app.AirQuality.Status.Error,Is.Not.Null);app.SetMode(WorldMode.WhatIf);app.Simulation.Scenario.RainMmPerHour=50;app.ApplyInputs();app.Engine.Step(app.Simulation.Active,12,6);
            Assert.That(app.Engine.District.Result.RainM3,Is.GreaterThan(0));Object.Destroy(app.gameObject);yield return null;
        }
        [UnityTest] public IEnumerator ScenarioFileRoundTripAndPlannerControlsWork()
        {
            yield return SceneManager.LoadSceneAsync("Dhara");yield return null;var app=Object.FindFirstObjectByType<DharaApplication>();var ui=app.GetComponent<EnvironmentalDashboard>();
            ui.StartDemo(true);ui.AdvanceDemo();ui.AdvanceDemo();ui.AdvanceDemo();double expected=ui.Comparison.Intervention.StorageM3;
            ui.ScenarioStorageRoot=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../TestResults/ScenarioIntegration"));ui.SaveSelectedScenario("Integration test");ui.LoadSelectedScenario();
            Assert.That(ui.Comparison.Intervention.StorageM3,Is.EqualTo(expected));Assert.That(app.Simulation.Mode,Is.EqualTo(WorldMode.Recorded));
            ui.StopDemo();ui.StartDemo(true);ui.AdvanceDemo();
            var root=app.transform.Find("Environmental monitoring dashboard");root.Find("City tools").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            root.Find("CITY TOOLS/Intervention planner").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            root.Find("INTERVENTION PLANNER/Evaluate").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            for(int i=0;i<45;i++)yield return null;Assert.That(ui.PlannerBest,Is.Not.Null);
            root.Find("INTERVENTION PLANNER/Inspect best comparison").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(ui.Comparison.Input.RainMmPerHour,Is.EqualTo(50));Assert.That(ui.Comparison.Intervention.PeakRunoffM3Hour,Is.EqualTo(ui.PlannerBest.Result.PeakRunoffM3Hour));
            ui.OpenStudio();ui.ResetStudio();Object.Destroy(app.gameObject);yield return null;
        }
        [UnityTest] public IEnumerator DeterministicGuideIsRepeatableAndResetsToLive()
        {
            yield return SceneManager.LoadSceneAsync("Dhara");yield return null;var app=Object.FindFirstObjectByType<DharaApplication>();var ui=app.GetComponent<EnvironmentalDashboard>();
            ui.StartDemo(true);Assert.That(app.Simulation.Mode,Is.EqualTo(WorldMode.Demonstration));Assert.That(ui.AdvanceDemo(),Is.True);double baseline=ui.Comparison.Baseline.StorageM3;
            ui.AdvanceDemo();Assert.That(app.Spatial.Selected.Id,Is.EqualTo("roof-1"));ui.AdvanceDemo();double intervention=ui.Comparison.Intervention.StorageM3;
            ui.StartDemo(true);ui.AdvanceDemo();ui.AdvanceDemo();ui.AdvanceDemo();Assert.That(ui.Comparison.Baseline.StorageM3,Is.EqualTo(baseline));Assert.That(ui.Comparison.Intervention.StorageM3,Is.EqualTo(intervention));
            ui.AdvanceDemo();ui.AdvanceDemo();Assert.That(ui.DemoRunning,Is.False);Assert.That(app.Simulation.Mode,Is.EqualTo(WorldMode.Live));Assert.That(app.Spatial.Preview,Is.Null);
            ui.StopDemo();Object.Destroy(app.gameObject);yield return null;
        }
        [UnityTest] public IEnumerator CityComparisonUsesReturnedDataAndReusesFreshCache()
        {
            yield return SceneManager.LoadSceneAsync("Dhara");yield return null;var app=Object.FindFirstObjectByType<DharaApplication>();
            app.CityComparison.Refresh();float deadline=Time.realtimeSinceStartup+110;
            while(app.CityComparison.Loading&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(app.CityComparison.Loading,Is.False);
            foreach(var cached in app.Weather.CityCache){Assert.That(cached.Current,Is.Not.Null,cached.Status.Error);Assert.That(cached.Status.RetrievedUtc.HasValue,Is.True);Assert.That(cached.Current.HasSimulationInputs,Is.True);}
            var retrieved=app.Weather.CityCache[2].Status.RetrievedUtc;app.CityComparison.Refresh();yield return null;
            Assert.That(app.Weather.CityCache[2].Status.RetrievedUtc,Is.EqualTo(retrieved));
            Object.Destroy(app.gameObject);yield return null;
        }
        [UnityTest] public IEnumerator ForecastImpactPreservesExactTimestampAndLiveInput()
        {
            yield return SceneManager.LoadSceneAsync("Dhara");yield return null;
            var app=Object.FindFirstObjectByType<DharaApplication>();float deadline=Time.realtimeSinceStartup+25;
            while(app.Weather.Status.Loading&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(app.Weather.Forecast.Count,Is.GreaterThan(2));var live=app.Simulation.Live;
            app.SetMode(WorldMode.Forecast);app.SelectForecast(2);var expected=app.Simulation.Forecast;
            var ui=app.GetComponent<EnvironmentalDashboard>();ui.OpenForecastImpact();
            Assert.That(ui.Comparison.Input.Time,Is.EqualTo(expected.Time));Assert.That(ui.Comparison.Classification,Is.EqualTo("Forecast"));
            Assert.That(app.TemporalPreviewFrozen,Is.True);Assert.That(app.Engine.District.Result.Seconds,Is.EqualTo(3600).Within(1e-6));
            app.SetMode(WorldMode.Live);Assert.That(app.Simulation.Live,Is.SameAs(live));Assert.That(app.Spatial.Preview,Is.Null);
            Object.Destroy(app.gameObject);yield return null;
        }
        [UnityTest] public IEnumerator SpatialObjectsMapToCatchmentAndSelection()
        {
            yield return SceneManager.LoadSceneAsync("Dhara");yield return null;
            var app=Object.FindFirstObjectByType<DharaApplication>();
            Assert.That(app.Spatial.Objects.Count,Is.EqualTo(19));
            double area=0;foreach(var obj in app.Spatial.Objects)area+=obj.Area;
            Assert.That(area,Is.EqualTo(DistrictHydrology.TotalArea));
            Assert.That(app.Spatial.SelectObject("roof-1"),Is.True);
            Assert.That(app.Spatial.InspectSelected(),Does.Contain("117 m²"));
            app.Spatial.SetOverlay(DistrictOverlay.Connections);Assert.That(app.Spatial.Legend,Does.Contain("shared collection"));
            app.Spatial.Deselect();Assert.That(app.Spatial.Selected,Is.Null);
            Object.Destroy(app.gameObject);yield return null;
        }
    }
}
