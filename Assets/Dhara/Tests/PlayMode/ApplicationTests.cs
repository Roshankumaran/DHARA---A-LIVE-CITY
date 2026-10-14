using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dhara.Tests
{
    public sealed class ApplicationTests
    {
        DharaApplication runningApp;
        GameObject requestObject;
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if(runningApp!=null) Object.Destroy(runningApp.gameObject);
            if(requestObject!=null) Object.Destroy(requestObject);
            yield return null;
        }
        [UnityTest]
        public IEnumerator SceneBootsAndScenarioDrivesWaterRainAndWind()
        {
            yield return SceneManager.LoadSceneAsync("Dhara"); yield return null;
            var app=Object.FindFirstObjectByType<DharaApplication>(); Assert.That(app,Is.Not.Null);
            runningApp=app;
            Assert.That(app.World.ViewCamera,Is.Not.Null); Assert.That(app.World.Trees.Count,Is.GreaterThan(0));
            Assert.That(Object.FindFirstObjectByType<Canvas>(),Is.Not.Null);
            app.SetMode(WorldMode.WhatIf);app.Simulation.Scenario.RainMmPerHour=80;app.Simulation.Scenario.WindKmh=80;app.Simulation.Scenario.DrainageMmPerHour=0;app.ApplyInputs();
            var lab=app.transform.Find("Environmental monitoring dashboard/Scenario controls/What-if controls");
            Assert.That(lab,Is.Not.Null); Assert.That(lab.gameObject.activeInHierarchy,Is.True);
            yield return new WaitForSecondsRealtime(2);
            Assert.That(app.Engine.State.WaterMm,Is.GreaterThan(0));
            Assert.That(app.World.GetComponentInChildren<ParticleSystem>().emission.rateOverTime.constant,Is.GreaterThan(0));
            Assert.That(app.World.Trees[0].localRotation,Is.Not.EqualTo(Quaternion.identity));
            app.SelectLocation(1);Assert.That(app.Engine.State.WaterMm,Is.Zero);
            Assert.That(app.Simulation.Mode,Is.EqualTo(WorldMode.Live)); Assert.That(lab.gameObject.activeInHierarchy,Is.False);
            Assert.That(app.Simulation.Live,Is.Null); Assert.That(app.Weather.Current,Is.Null);
            Assert.That(app.World.ViewCamera.transform.IsChildOf(app.transform),Is.True);
        }
        [UnityTest]
        public IEnumerator FailedWeatherRequestLeavesApplicationUsable()
        {
            var obj=new GameObject("Failed weather request test");requestObject=obj;var service=obj.AddComponent<WeatherService>();
            service.RequestUrl=_=>"https://dhara-unavailable.invalid/forecast";
            service.Select(0);float deadline=Time.realtimeSinceStartup+25;
            while(service.Status.Loading && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(service.Status.Loading,Is.False,"Request exceeded its configured timeout");
            Assert.That(service.Status.Error,Is.Not.Null);Assert.That(service.Current,Is.Null);
            var simulation=new SimulationState { Mode=WorldMode.WhatIf };simulation.ResetScenario();
            var engine=new CausalEnvironmentEngine();Assert.DoesNotThrow(()=>engine.Step(simulation.Active,12,6));
            Object.Destroy(obj);yield return null;
        }
    }
}
