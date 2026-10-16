using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Dhara.Tests
{
    public sealed class ApiIntegrationTests
    {
        GameObject host;AirQualityService service;
        [SetUp] public void Setup(){host=new GameObject("Air quality integration checks");service=host.AddComponent<AirQualityService>();}
        [UnityTearDown] public IEnumerator Cleanup(){if(host!=null)Object.Destroy(host);yield return null;}
        IEnumerator Wait()
        {float deadline=Time.realtimeSinceStartup+26;while(service.Status.Loading&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(service.Status.Loading,Is.False,"Request exceeded its timeout");}
        [UnityTest] public IEnumerator RealAirQualityAllFiveCities()
        {
            var results=new System.Collections.Generic.List<string>();
            for(int i=0;i<Location.All.Length;i++)
            {
                service.Select(i);yield return Wait();Assert.That(service.Status.Error,Is.Null,Location.All[i].Name+": "+service.Status.Error);
                Assert.That(service.Data,Is.Not.Null);var fields=EnvironmentalParameters.AirQuality(service.Data,service.Status).ToArray();
                Assert.That(fields.Length,Is.EqualTo(8));Assert.That(fields.All(p=>p.Availability=="Available"),Is.True,"Actual response did not supply all requested fields; inspect provider availability");
                Assert.That(service.Data.UsAqi,Is.GreaterThanOrEqualTo(0));Assert.That(service.Data.EuropeanAqi,Is.GreaterThanOrEqualTo(0));
                results.Add(Location.All[i].Name+" | "+service.Data.Time+" IST | US AQI "+service.Data.UsAqi+" | European AQI "+service.Data.EuropeanAqi+" | "+string.Join("; ",fields.Select(p=>p.Name+"="+p.Value+" "+p.Unit)));
            }
            System.IO.Directory.CreateDirectory("Verification");System.IO.File.WriteAllLines("Verification/api-unity-air-quality.txt",results);
        }
        [UnityTest] public IEnumerator FailedAirQualityRefreshRetainsTimestampAndValues()
        {
            service.Select(0);yield return Wait();Assert.That(service.Status.Error,Is.Null);
            var original=service.Data;var retrieved=service.Status.RetrievedUtc;
            using(var endpoint=new ApplicationTests.FaultEndpoint(500,"{}"))
            {service.RequestUrl=_=>endpoint.Url;service.Select(0);yield return Wait();Assert.That(service.Status.Error,Is.Not.Null);Assert.That(service.Data,Is.SameAs(original));Assert.That(service.Status.RetrievedUtc,Is.EqualTo(retrieved));Assert.That(service.Status.RetryUtc,Is.GreaterThan(DateTime.UtcNow));}
        }
        [UnityTest] public IEnumerator RateLimitedManualRefreshWaitsForRetryAfter()
        {
            using(var endpoint=new ApplicationTests.FaultEndpoint(429,"{}",false,"120"))
            {
                service.RequestUrl=_=>endpoint.Url;service.Select(0);yield return Wait();
                Assert.That(service.Status.Error,Does.Contain("429"));Assert.That(service.Status.RetryUtc,Is.GreaterThan(DateTime.UtcNow.AddSeconds(110)));
                var retry=service.Status.RetryUtc;service.RequestUrl=_=>throw new InvalidOperationException("Request sent before Retry-After");service.Select(0);
                yield return new WaitForSecondsRealtime(.5f);Assert.That(service.Status.Error,Does.Not.Contain("Request sent"));Assert.That(service.Status.RetryUtc,Is.EqualTo(retry));service.Cancel();
            }
        }
        [UnityTest] public IEnumerator AirQualityCityChangeClearsRetainedValuesAndCancels()
        {
            service.Select(0);yield return Wait();Assert.That(service.Status.Error,Is.Null);
            using(var endpoint=new ApplicationTests.FaultEndpoint(200,"{}",true))
            {service.RequestUrl=_=>endpoint.Url;service.Select(3);Assert.That(service.Data,Is.Null);Assert.That(service.Status.RetrievedUtc,Is.Null);service.Cancel();Assert.That(service.Status.Loading,Is.False);yield return null;Assert.That(service.Data,Is.Null);}
        }
    }
}
