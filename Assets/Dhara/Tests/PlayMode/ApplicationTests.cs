using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace Dhara.Tests
{
    public sealed class ApplicationTests
    {
        DharaApplication runningApp;
        GameObject requestObject;
        IEnumerator LoadApplication()
        {
            yield return SceneManager.LoadSceneAsync("Dhara"); yield return null;
            runningApp=UnityEngine.Object.FindFirstObjectByType<DharaApplication>(); Assert.That(runningApp,Is.Not.Null);
        }
        Slider Control(string name) => runningApp.transform.Find("Environmental monitoring dashboard/Scenario controls/What-if controls/"+name+"/Track").GetComponent<Slider>();
        IEnumerator WaitForWeather(WeatherService service,float seconds=25)
        {
            float deadline=Time.realtimeSinceStartup+seconds;
            while(service.Status.Loading && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.That(service.Status.Loading,Is.False,"Weather request did not finish");
        }
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
            foreach(var mesh in app.World.GetComponentsInChildren<MeshRenderer>())
            {
                Assert.That(mesh.sharedMaterial.shader.isSupported,Is.True,mesh.name+" shader is unsupported");
                var bounds=mesh.bounds;
                foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
                {
                    var point=app.World.ViewCamera.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)));
                    Assert.That(point.x,Is.InRange(0f,1f),mesh.name+" outside horizontal framing");
                    Assert.That(point.y,Is.InRange(0f,1f),mesh.name+" outside vertical framing");
                }
            }
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
            var camera=app.World.ViewCamera;var world=app.World;var canvas=app.GetComponentInChildren<Canvas>();
            UnityEngine.Object.Destroy(app.gameObject);yield return null;
            Assert.That(camera==null && world==null && canvas==null,Is.True,"Runtime hierarchy was not cleaned up");
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
        [UnityTest]
        public IEnumerator ActualSlidersDriveEffectsDrainageCloudLightingAndReset()
        {
            yield return LoadApplication();var app=runningApp;app.SetMode(WorldMode.WhatIf);
            Control("RAINFALL").value=80;Control("WIND SPEED").value=80;Control("TEMPERATURE").value=45;Control("DRAINAGE CAPACITY").value=0;
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.That(app.Simulation.Active.Temperature,Is.EqualTo(45));Assert.That(app.Engine.State.Wetness,Is.GreaterThan(.5));Assert.That(app.Engine.State.WaterMm,Is.GreaterThan(0));
            var particles=app.World.GetComponentInChildren<ParticleSystem>();Assert.That(particles.emission.rateOverTime.constant,Is.GreaterThan(100));
            Assert.That(app.World.Trees[0].localRotation,Is.Not.EqualTo(Quaternion.identity));
            // Hold the real loop while rendering controlled hypothetical inputs for equal model durations.
            app.enabled=false;var scenario=app.Simulation.Active;scenario.Time="2026-01-01T12:00";scenario.WindDegrees=90;
            app.Engine.Reset();app.Engine.Step(scenario,0,600);app.World.Render(app.Engine.State,scenario,5);
            var basin=app.World.transform.Find("Simulated detention storage");float retainedWidth=basin.localScale.x;double retained=app.Engine.State.WaterMm;
            Assert.That(particles.velocityOverLifetime.x.constant,Is.LessThan(0));Assert.That(Mathf.Abs(particles.velocityOverLifetime.z.constant),Is.LessThan(.01f));
            var asphalt=app.World.transform.Find("East west avenue").GetComponent<Renderer>().sharedMaterial;Assert.That(asphalt.GetFloat("_Smoothness"),Is.GreaterThan(.7f));
            app.Engine.Reset();app.Engine.Step(scenario,60,600);app.World.Render(app.Engine.State,scenario,5);
            Assert.That(app.Engine.State.WaterMm,Is.LessThan(retained));Assert.That(basin.localScale.x,Is.LessThan(retainedWidth));
            scenario.RainMmPerHour=0;double before=app.Engine.State.WaterMm;app.Engine.Step(scenario,60,600);Assert.That(app.Engine.State.WaterMm,Is.LessThanOrEqualTo(before));
            scenario.CloudPercent=0;app.Engine.Step(scenario,12,0);app.World.Render(app.Engine.State,scenario,5);
            var daylight=app.World.transform.Find("Daylight").GetComponent<Light>();float bright=daylight.intensity;var clearSky=app.World.ViewCamera.backgroundColor;
            scenario.CloudPercent=100;app.Engine.Step(scenario,12,0);app.World.Render(app.Engine.State,scenario,5);
            Assert.That(daylight.intensity,Is.LessThan(bright));Assert.That(app.World.ViewCamera.backgroundColor,Is.Not.EqualTo(clearSky));
            app.enabled=true;
            app.transform.Find("Environmental monitoring dashboard/Scenario controls/What-if controls/Reset scenario").GetComponent<Button>().onClick.Invoke();
            Assert.That(app.Engine.State.WaterMm,Is.Zero);Assert.That(app.Simulation.Scenario.DrainageMmPerHour,Is.EqualTo(12));
            app.transform.Find("Environmental monitoring dashboard/Scenario controls/What-if controls/Return to live").GetComponent<Button>().onClick.Invoke();
            Assert.That(app.Simulation.Mode,Is.EqualTo(WorldMode.Live));Assert.That(app.Simulation.Active,Is.SameAs(app.Weather.Current));
        }
        [UnityTest]
        public IEnumerator RealWeatherForAllLocationButtonsAndExactForecastSelection()
        {
            yield return LoadApplication();var app=runningApp;var records=new List<string>();
            for(int i=0;i<Location.All.Length;i++)
            {
                app.transform.Find("Environmental monitoring dashboard/Header/"+Location.All[i].Name).GetComponent<Button>().onClick.Invoke();
                Assert.That(app.Weather.Selected,Is.EqualTo(i));Assert.That(app.Weather.Current,Is.Null);
                yield return WaitForWeather(app.Weather);
                Assert.That(app.Weather.Status.Error,Is.Null,Location.All[i].Name+": "+app.Weather.Status.Error);
                Assert.That(app.Weather.Current.HasSimulationInputs,Is.True);Assert.That(app.Simulation.Live,Is.SameAs(app.Weather.Current));
                records.Add(Location.All[i].Name+" | "+app.Weather.Current.Time+" IST | "+app.Weather.Current.Temperature+" C | "+app.Weather.Current.WindKmh+" km/h | "+app.Weather.Forecast.Count+" future hours");
            }
            Directory.CreateDirectory("TestResults");File.WriteAllLines("TestResults/unity-live-locations.txt",records);
            app.SetMode(WorldMode.Forecast);Assert.That(app.Weather.Forecast.Count,Is.GreaterThan(2));
            var expected=app.Weather.Forecast[2];var timeline=app.transform.Find("Environmental monitoring dashboard/Scenario controls/Forecast controls/FORECAST STEP/Track").GetComponent<Slider>();
            timeline.value=2;Assert.That(app.Simulation.Active,Is.SameAs(expected));Assert.That(app.Simulation.Active.Time,Is.EqualTo(expected.Time));
            var live=app.Weather.Current;app.SetMode(WorldMode.WhatIf);Control("RAINFALL").value=120;
            Assert.That(app.Weather.Current,Is.SameAs(live));app.SetMode(WorldMode.Live);Assert.That(app.Simulation.Forecast,Is.Null);Assert.That(app.Simulation.Active,Is.SameAs(live));
        }
        [UnityTest]
        public IEnumerator RapidSwitchRefreshAndSceneReloadPreserveLatestLocation()
        {
            yield return LoadApplication();var app=runningApp;
            app.SelectLocation(1);app.Weather.Refresh();app.SelectLocation(3);app.SelectLocation(4);app.Weather.Refresh();
            Assert.That(app.Weather.Current,Is.Null);yield return WaitForWeather(app.Weather);
            Assert.That(app.Weather.Selected,Is.EqualTo(4));Assert.That(app.Weather.Status.Error,Is.Null);
            Assert.That(app.Simulation.Live,Is.SameAs(app.Weather.Current));
            var oldCamera=app.World.ViewCamera;var oldWorld=app.World;
            yield return LoadApplication();
            Assert.That(oldCamera==null && oldWorld==null,Is.True,"Scene reload leaked old runtime objects");
            Assert.That(Object.FindObjectsByType<DharaApplication>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            Assert.That(runningApp.Weather.Selected,Is.Zero);yield return WaitForWeather(runningApp.Weather);
            Assert.That(runningApp.Weather.Status.Error,Is.Null);Assert.That(runningApp.Simulation.Live,Is.SameAs(runningApp.Weather.Current));
        }
        [UnityTest]
        public IEnumerator InterventionControlsPreserveFrozenBaselineAndLiveData()
        {
            yield return LoadApplication();var app=runningApp;app.SetMode(WorldMode.WhatIf);
            Control("RAINFALL").value=80;Control("TEMPERATURE").value=25;Control("DRAINAGE CAPACITY").value=12;
            var dashboard=app.GetComponent<EnvironmentalDashboard>();dashboard.OpenExperiment();
            var panel=app.transform.Find("Environmental monitoring dashboard/Urban Intervention Lab");
            Assert.That(panel.gameObject.activeInHierarchy,Is.True);
            var extra=panel.Find("EXTRA DRAINAGE/Track").GetComponent<Slider>();extra.value=20;
            var result=panel.Find("Comparison").GetComponent<TMPro.TMP_Text>();
            Assert.That(result.text,Does.Contain("58.0"));Assert.That(result.text,Does.Contain("42.0"));Assert.That(result.text,Does.Contain("16.0 mm less"));
            Control("RAINFALL").value=20;extra.value=21;Assert.That(result.text,Does.Contain("58.0"),"Captured baseline changed with running inputs");
            var original=app.Weather.Current;panel.Find("Reset experiment").GetComponent<Button>().onClick.Invoke();
            Assert.That(extra.value,Is.Zero);Assert.That(result.text,Does.Contain("0.0 mm less"));Assert.That(app.Weather.Current,Is.SameAs(original));
            panel.Find("Close").GetComponent<Button>().onClick.Invoke();Assert.That(panel.gameObject.activeInHierarchy,Is.False);
            app.transform.Find("Environmental monitoring dashboard/Activity log/Clear log").GetComponent<Button>().onClick.Invoke();Assert.That(app.Engine.Events,Is.Empty);
        }
        // These loopback endpoints only inject failures. They never provide successful weather data.
        sealed class FaultEndpoint : IDisposable
        {
            readonly System.Net.Sockets.TcpListener listener;
            readonly System.Threading.CancellationTokenSource stop=new System.Threading.CancellationTokenSource();
            System.Net.Sockets.TcpClient client;
            public string Url { get; }
            public FaultEndpoint(int status,string body,bool stall=false)
            {
                listener=new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback,0);listener.Start();
                Url="http://127.0.0.1:"+((System.Net.IPEndPoint)listener.LocalEndpoint).Port+"/failure";
                _=Serve(status,body,stall);
            }
            async System.Threading.Tasks.Task Serve(int status,string body,bool stall)
            {
                try
                {
                    client=await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    using(var stream=client.GetStream())
                    {
                        var request=new byte[8192];await stream.ReadAsync(request,0,request.Length,stop.Token).ConfigureAwait(false);
                        if(stall) await System.Threading.Tasks.Task.Delay(30000,stop.Token).ConfigureAwait(false);
                        else
                        {
                            var payload=System.Text.Encoding.UTF8.GetBytes(body);
                            var header=System.Text.Encoding.ASCII.GetBytes("HTTP/1.1 "+status+" Test fault\r\nContent-Type: application/json\r\nContent-Length: "+payload.Length+"\r\nConnection: close\r\n\r\n");
                            await stream.WriteAsync(header,0,header.Length,stop.Token).ConfigureAwait(false);
                            await stream.WriteAsync(payload,0,payload.Length,stop.Token).ConfigureAwait(false);
                        }
                    }
                }
                catch(Exception error) when(error is OperationCanceledException || error is ObjectDisposedException || error is System.IO.IOException || error is System.Net.Sockets.SocketException) { }
            }
            public void Dispose() { stop.Cancel();listener.Stop();client?.Dispose(); }
        }
        [UnityTest]
        public IEnumerator HttpMalformedAndMissingResponsesFailHonestly()
        {
            requestObject=new GameObject("HTTP failure verification");var service=requestObject.AddComponent<WeatherService>();
            foreach(var fault in new[]{Tuple.Create(503,"{\"error\":true}"),Tuple.Create(200,"not json"),Tuple.Create(200,"{}")})
            {
                using(var endpoint=new FaultEndpoint(fault.Item1,fault.Item2))
                {
                    service.RequestUrl=_=>endpoint.Url;service.Select(0);yield return WaitForWeather(service,10);
                    Assert.That(service.Status.Error,Is.Not.Null,"Rejected response was presented as live weather");
                    Assert.That(service.Current,Is.Null);Assert.That(service.Forecast,Is.Empty);
                }
            }
        }
        [UnityTest]
        public IEnumerator RealRequestTimeoutIsBoundedAndCleanupCancelsPendingRequest()
        {
            requestObject=new GameObject("Timeout verification");var service=requestObject.AddComponent<WeatherService>();
            using(var endpoint=new FaultEndpoint(200,"",true))
            {
                service.RequestUrl=_=>endpoint.Url;float started=Time.realtimeSinceStartup;service.Select(0);
                yield return WaitForWeather(service,26);
                Assert.That(Time.realtimeSinceStartup-started,Is.InRange(18f,26f));
                Assert.That(service.Status.Error,Is.Not.Null);Assert.That(service.Current,Is.Null);
                service.Refresh();Assert.That(service.Status.Loading,Is.True);
                Object.Destroy(requestObject);yield return null;Assert.That(service==null,Is.True);
            }
        }
        [UnityTest]
        public IEnumerator RealRefreshFailureRetainsDataAndAutomaticRetryRecovers()
        {
            var obj=new GameObject("Refresh recovery test");requestObject=obj;var service=obj.AddComponent<WeatherService>();service.Select(0);
            yield return WaitForWeather(service);Assert.That(service.Status.Error,Is.Null);var original=service.Current;Assert.That(original,Is.Not.Null);
            var retrieved=service.Status.RetrievedUtc;service.Status.RetrievedUtc=DateTime.UtcNow.AddMinutes(-11);
            service.RequestUrl=_=>"https://dhara-unavailable.invalid/forecast";service.Refresh();
            Assert.That(service.Current,Is.SameAs(original));Assert.That(service.Status.Label,Does.Contain("Stale"));
            yield return WaitForWeather(service);Assert.That(service.Status.Error,Is.Not.Null);Assert.That(service.Current,Is.SameAs(original));
            service.RequestUrl=WeatherRequest.Url;float deadline=Time.realtimeSinceStartup+45;
            while((service.Status.Error!=null || service.Status.Loading) && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(service.Status.Error,Is.Null,"Automatic retry did not recover");Assert.That(service.Status.RetrievedUtc,Is.GreaterThanOrEqualTo(retrieved));Assert.That(service.Status.Stale,Is.False);
        }
    }
}
