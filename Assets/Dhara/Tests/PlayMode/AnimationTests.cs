using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Dhara.Tests
{
    public sealed class AnimationTests
    {
        DharaApplication app;
        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Dhara"); yield return null;
            app = Object.FindFirstObjectByType<DharaApplication>();
            app.enabled = false;
        }
        [UnityTearDown] public IEnumerator Cleanup() { if(app != null) Object.Destroy(app.gameObject); yield return null; }
        [UnityTest] public IEnumerator CameraTravelsToStreetAndResetCancelsTour()
        {
            yield return Load(); var camera = app.World.ViewCamera; var orbit = camera.GetComponent<OrbitCamera>();
            orbit.Home(); yield return null; Vector3 start = camera.transform.position;
            orbit.ToStreet(); yield return new WaitForSecondsRealtime(2.5f);
            Assert.That(Vector3.Distance(start, camera.transform.position), Is.GreaterThan(5));
            Assert.That(camera.transform.position.y, Is.GreaterThan(25));
            yield return new WaitForSecondsRealtime(2.8f);
            Assert.That(orbit.IsTransitioning, Is.False);
            Assert.That(camera.transform.position.y, Is.EqualTo(2.2f).Within(.1f));
            Assert.That(Mathf.Abs(camera.transform.position.x), Is.LessThan(.1f));
            Assert.That(orbit.IsExploring, Is.True);
            Vector3 entrance = camera.transform.position;
            orbit.MoveExplorer(Vector2.up, .1f);
            Assert.That(camera.transform.position.z, Is.GreaterThan(entrance.z));
            // Move along the road, then sideways into a building: stop at its footprint.
            for (int i = 0; i < 21; i++) orbit.MoveExplorer(Vector2.up, .1f);
            for (int i = 0; i < 300; i++) orbit.MoveExplorer(Vector2.right, .1f);
            Assert.That(camera.transform.position.x, Is.LessThan(12));
            Assert.That(camera.transform.position.y, Is.EqualTo(2.2f).Within(.01f));
            for (int i = 0; i < 500; i++) orbit.MoveExplorer(Vector2.down, .1f, true);
            Assert.That(camera.transform.position.z, Is.GreaterThanOrEqualTo(-44));
            orbit.StartTour(); Assert.That(orbit.TourRunning, Is.True);
            orbit.CancelTour(); yield return null;
            Assert.That(orbit.TourRunning || orbit.IsTransitioning, Is.False);
            Assert.That(camera.transform.position.y, Is.GreaterThan(20));
        }
        [UnityTest] public IEnumerator WeatherEffectsFollowStateAndStopNewRainWhenDry()
        {
            yield return Load(); var world = app.World;
            var state = new EnvironmentState { RainIntensity=1, WindIntensity=1, Wetness=1, CollectionM3=30, LightIntensity=1 };
            var weather = new WeatherData { Time="2000-01-01T12:00", WindDegrees=90, CloudPercent=90 };
            world.Render(state, weather, 5);
            var rain = world.transform.Find("Rainfall particles").GetComponent<ParticleSystem>();
            Assert.That(rain.emission.rateOverTime.constant, Is.GreaterThan(3000));
            Assert.That(rain.GetComponent<ParticleSystemRenderer>().renderMode, Is.EqualTo(ParticleSystemRenderMode.Stretch));
            Assert.That(rain.GetComponent<ParticleSystemRenderer>().sharedMaterial.mainTexture, Is.Not.Null);
            var debrisVelocity = world.transform.Find("Wind debris").GetComponent<ParticleSystem>().velocityOverLifetime;
            Assert.That(debrisVelocity.x.mode, Is.EqualTo(debrisVelocity.y.mode));
            Assert.That(debrisVelocity.z.mode, Is.EqualTo(debrisVelocity.y.mode));
            Assert.That(world.DisplayedWind.x, Is.LessThan(-8));
            Assert.That(Mathf.Abs(world.DisplayedWind.y), Is.LessThan(.01f));
            Assert.That(world.transform.Find("Road rain ripples").GetComponent<ParticleSystem>().emission.rateOverTime.constant, Is.GreaterThan(400));
            var tree = world.Trees[0]; Assert.That(Quaternion.Angle(tree.localRotation, Quaternion.identity), Is.LessThanOrEqualTo(7.01f));
            weather.WindDegrees=null; world.Render(state,weather,5);
            Assert.That(Quaternion.Angle(tree.localRotation, Quaternion.identity), Is.GreaterThan(.01f), "Offline wind speed must still move vegetation without inventing a bearing");
            Assert.That(world.DisplayedWind.magnitude, Is.LessThan(.01f));
            var puddle = world.transform.Find("Puddle"); float wetSize=puddle.localScale.x;
            state.RainIntensity=state.WindIntensity=state.Wetness=state.CollectionM3=0;
            world.Render(state, weather, 10);
            Assert.That(rain.emission.rateOverTime.constant, Is.Zero);
            Assert.That(world.transform.Find("Rain splash impacts").GetComponent<ParticleSystem>().emission.rateOverTime.constant, Is.Zero);
            Assert.That(puddle.localScale.x, Is.LessThan(wetSize));
            Assert.That(Quaternion.Angle(tree.localRotation, Quaternion.identity), Is.LessThan(.01f));
            weather.WindDegrees=double.NaN; world.Render(state,weather,.1f);
            Assert.That(float.IsNaN(world.DisplayedWind.x), Is.False);
        }
    }
}

