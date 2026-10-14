using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dhara.Tests
{
    public sealed class ApplicationTests
    {
        [UnityTest]
        public IEnumerator SceneBootsAndScenarioDrivesWaterRainAndWind()
        {
            yield return SceneManager.LoadSceneAsync("Dhara"); yield return null;
            var app=Object.FindFirstObjectByType<DharaApplication>(); Assert.That(app,Is.Not.Null);
            Assert.That(app.World.ViewCamera,Is.Not.Null); Assert.That(app.World.Trees.Count,Is.GreaterThan(0));
            Assert.That(Object.FindFirstObjectByType<Canvas>(),Is.Not.Null);
            app.SetMode(WorldMode.WhatIf);app.Simulation.Scenario.RainMmPerHour=80;app.Simulation.Scenario.WindKmh=80;app.Simulation.Scenario.DrainageMmPerHour=0;app.ApplyInputs();
            yield return new WaitForSecondsRealtime(2);
            Assert.That(app.Engine.State.WaterMm,Is.GreaterThan(0));
            Assert.That(app.World.GetComponentInChildren<ParticleSystem>().emission.rateOverTime.constant,Is.GreaterThan(0));
            Assert.That(app.World.Trees[0].localRotation,Is.Not.EqualTo(Quaternion.identity));
            app.SetMode(WorldMode.Live);Assert.That(app.Engine.State.WaterMm,Is.Zero);
        }
        [UnityTest]
        public IEnumerator FailedWeatherRequestLeavesApplicationUsable()
        {
            var obj=new GameObject("Failed weather request test");var service=obj.AddComponent<WeatherService>();
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
