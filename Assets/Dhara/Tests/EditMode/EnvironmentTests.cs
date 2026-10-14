using System;
using NUnit.Framework;

namespace Dhara.Tests
{
    public sealed class EnvironmentTests
    {
        static WeatherData Weather(double rain=10,double wind=12,double cloud=20) => new WeatherData { RainMmPerHour=rain,WindKmh=wind,CloudPercent=cloud,Temperature=25,Time="2026-01-01T12:00" };
        [Test] public void RainIncreasesEmission() { var a=new CausalEnvironmentEngine(); var b=new CausalEnvironmentEngine(); a.Step(Weather(2),12,6); b.Step(Weather(40),12,6); Assert.That(b.State.RainIntensity,Is.GreaterThan(a.State.RainIntensity)); }
        [Test] public void RainCannotReduceWater() { Assert.That(WaterAccumulationModel.Step(20,40,12,25,600),Is.GreaterThanOrEqualTo(WaterAccumulationModel.Step(20,10,12,25,600))); }
        [Test] public void LowerDrainageRetainsWater() { Assert.That(WaterAccumulationModel.Step(20,40,0,25,600),Is.GreaterThan(WaterAccumulationModel.Step(20,40,20,25,600))); }
        [Test] public void WindIncreasesSway() { var engine=new CausalEnvironmentEngine(); engine.Step(Weather(0,5),12,6); double before=engine.State.WindIntensity; engine.Step(Weather(0,70),12,6); Assert.That(engine.State.WindIntensity,Is.GreaterThan(before)); }
        [Test] public void CloudsReduceDaylight() { var engine=new CausalEnvironmentEngine(); engine.Step(Weather(0,0,0),12,6); double before=engine.State.LightIntensity; engine.Step(Weather(0,0,100),12,6); Assert.That(engine.State.LightIntensity,Is.LessThan(before)); }
        [Test] public void ScenarioResetRestoresBaseline() { var state=new SimulationState { Live=Weather(8,22),Mode=WorldMode.WhatIf }; state.Scenario.RainMmPerHour=90; state.ResetScenario(); Assert.That(state.Active.RainMmPerHour,Is.EqualTo(8)); Assert.That(state.Active.WindKmh,Is.EqualTo(22)); Assert.That(state.Drainage,Is.EqualTo(12)); }
        [Test] public void MissingDataPausesSafely() { var engine=new CausalEnvironmentEngine(); Assert.DoesNotThrow(()=>{ engine.Step(null,12,6);engine.Step(new WeatherData(),12,6); }); Assert.That(engine.State.ElapsedSimulatedSeconds,Is.Zero); }
        [Test] public void WaterStaysBounded() { double water=0;for(int i=0;i<10000;i++)water=WaterAccumulationModel.Step(water,200,0,50,3600);Assert.That(water,Is.InRange(0,150)); Assert.That(WaterAccumulationModel.Step(double.NaN,double.PositiveInfinity,0,25,6),Is.InRange(0,150)); }
        [Test] public void DryWeatherRemovesWater() { Assert.That(WaterAccumulationModel.Step(20,0,12,25,600),Is.LessThan(20)); }
        [Test] public void RepeatedInputsAreDeterministic() { var a=new CausalEnvironmentEngine();var b=new CausalEnvironmentEngine();for(int i=0;i<100;i++){a.Step(Weather(),12,6);b.Step(Weather(),12,6);}Assert.That(a.State.WaterMm,Is.EqualTo(b.State.WaterMm)); }
        [Test] public void CurrentRainIsConvertedFromIntervalTotal() { var values=new ApiValues { rain=2,interval=900 };Assert.That(values.Convert().RainMmPerHour,Is.EqualTo(8)); }
        [Test] public void NullAndAbsentValuesRemainMissing() { var response=WeatherJson.Parse("{\"current\":{\"time\":\"2026-01-01T12:00\",\"rain\":null}}");Assert.That(response.current.Convert().RainMmPerHour,Is.Null);Assert.That(response.current.Convert().WindKmh,Is.Null); }
        [Test] public void ForecastIndexPreservesTimestamp() { var hourly=new ApiHourly { time=new[]{"2026-01-01T12:00","2026-01-01T13:00"},rain=new double?[]{1,4} };Assert.That(hourly.At(1).Time,Is.EqualTo("2026-01-01T13:00"));Assert.That(hourly.At(1).RainMmPerHour,Is.EqualTo(4));Assert.That(hourly.At(2),Is.Null); }
        [Test] public void FailedConnectionStatusIsSafe() { var status=new DataSourceStatus { Error="Network unavailable" };Assert.That(status.Label,Does.Contain("Request failed"));Assert.That(status.Stale,Is.False); }
        [Test] public void StaleDataIsIdentified() { var status=new DataSourceStatus { RetrievedUtc=DateTime.UtcNow.AddMinutes(-11) };Assert.That(status.Stale,Is.True); }
        [Test] public void LogIsBoundedAndDeduplicated() { var engine=new CausalEnvironmentEngine();for(int i=0;i<100;i++)engine.Step(Weather(),12,6);Assert.That(engine.Events.Count,Is.Zero);for(int i=0;i<100;i++)engine.Record(i.ToString());Assert.That(engine.Events.Count,Is.EqualTo(30)); }
        [Test] public void SmallSliderMovementsEventuallyLog() { var engine=new CausalEnvironmentEngine();for(int i=0;i<20;i++)engine.Step(Weather(i*.1),12,0);Assert.That(engine.Events.Count,Is.GreaterThan(0)); }
        [Test] public void OfflineScenarioIsExplicitAndUsable() { var state=new SimulationState { Mode=WorldMode.WhatIf };state.ResetScenario();Assert.That(state.Active.HasSimulationInputs,Is.True);Assert.That(state.Active.SolarWm2,Is.Null);Assert.That(state.Active.CloudPercent,Is.EqualTo(50)); }
        [Test] public void RefreshRetainsDataButLocationChangeClearsIt()
        {
            var store=new WeatherDataStore();store.Accept(WeatherJson.Parse("{\"current\":{\"time\":\"2026-01-01T12:00\"}}"),DateTime.UtcNow);
            var original=store.Current;var retrieved=store.Status.RetrievedUtc;
            store.BeginRequest(false);store.Fail("Network unavailable");
            Assert.That(store.Current,Is.SameAs(original));Assert.That(store.Status.RetrievedUtc,Is.EqualTo(retrieved));
            store.BeginRequest(true);Assert.That(store.Current,Is.Null);Assert.That(store.Status.RetrievedUtc,Is.Null);
        }
        [Test] public void ForecastRefreshPreservesSelectedHourAndDetectsExpiry()
        {
            var a=Weather();a.Time="2026-01-01T13:00";var b=Weather();b.Time="2026-01-01T14:00";
            var state=new SimulationState { Mode=WorldMode.Forecast,Forecast=b };
            var changed=state.UpdateWeather(Weather(),new[]{a,b});Assert.That(changed,Is.False);Assert.That(state.Active,Is.SameAs(b));
            changed=state.UpdateWeather(Weather(),new[]{a});Assert.That(changed,Is.True);Assert.That(state.Active,Is.SameAs(a));
        }
        [Test] public void ReturnToLiveClearsHypotheticalOverrides()
        {
            var state=new SimulationState { Mode=WorldMode.WhatIf,Live=Weather(8,22) };
            state.Scenario.RainMmPerHour=99;state.Scenario.DrainageMmPerHour=0;
            state.ChangeMode(WorldMode.Live,null);Assert.That(state.Active,Is.SameAs(state.Live));
            Assert.That(state.Scenario.RainMmPerHour,Is.EqualTo(8));Assert.That(state.Scenario.DrainageMmPerHour,Is.EqualTo(12));
        }
    }
}
