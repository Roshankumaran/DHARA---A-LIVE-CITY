using System;
using NUnit.Framework;
namespace Dhara.Tests {
public sealed class ExplorationModelTests {
static DistrictHydrology Model(double drainage=0)=>new DistrictHydrology(new InterventionConfig(),drainage);
static WeatherData Weather()=>new WeatherData{Time="2026-01-01T12:00",RainMmPerHour=20,Temperature=25,WindKmh=10,WindDegrees=90,CloudPercent=30,SolarWm2=600};
[Test] public void A01_ZeroRainAddsNoVolume(){var m=Model();m.Step(0,60);Assert.That(m.Result.RainM3,Is.Zero);}
[Test] public void A02_PositiveRainConvertsDepthToVolume(){var m=Model();m.Step(10,3600);Assert.That(m.Result.RainM3,Is.EqualTo(46.52).Within(1e-8));}
[Test] public void A03_StrongerRainIncreasesInput(){var a=Model();var b=Model();a.Step(5,60);b.Step(40,60);Assert.That(b.Result.RainM3,Is.GreaterThan(a.Result.RainM3));}
[Test] public void A04_LowDrainRetainsMore(){Assert.That(DistrictHydrology.Run(40,new InterventionConfig(),0).StorageM3,Is.GreaterThan(DistrictHydrology.Run(40,new InterventionConfig(),30).StorageM3));}
[Test] public void A05_HighDrainRemovesMore(){var a=Model(2);var b=Model(50);a.Step(80,600);b.Step(80,600);Assert.That(b.Result.DrainedM3,Is.GreaterThan(a.Result.DrainedM3));}
[Test] public void A06_ZeroDrainRemovesNothing(){var m=Model();m.Step(40,600);Assert.That(m.LastDrainedM3,Is.Zero);}
[Test] public void A07_RemovalLimitedToAvailableVolume(){var m=Model(100);m.Step(1,6);Assert.That(m.Result.DrainedM3,Is.LessThanOrEqualTo(m.Result.RainM3));}
[Test] public void A08_EmptyStoreNeverNegative(){var m=Model(60);m.Step(0,3600);Assert.That(m.Result.StorageM3,Is.Zero);}
[Test] public void A09_StorageBoundReportsOverflow(){var m=Model();m.Step(200,3600);Assert.That(m.Result.StorageM3,Is.EqualTo(150));Assert.That(m.Result.OverflowM3,Is.GreaterThan(0));}
[Test] public void A10_NegativeRainClamped(){var m=Model();m.Step(-10,60);Assert.That(m.Result.RainM3,Is.Zero);}
[Test] public void A11_NegativeDrainClamped(){var m=Model(-20);m.Step(40,60);Assert.That(m.LastDrainedM3,Is.Zero);}
[Test] public void A12_NanRainPauses(){var m=Model();m.Step(double.NaN,60);Assert.That(m.Result.Seconds,Is.Zero);}
[Test] public void A13_InfiniteRainPauses(){var m=Model();m.Step(double.PositiveInfinity,60);Assert.That(m.Result.RainM3,Is.Zero);}
[Test] public void A14_ZeroAndNegativeTimeDoNotIntegrate(){var m=Model();m.Step(40,0);m.Step(40,-60);Assert.That(m.Result.Seconds,Is.Zero);}
[Test] public void A15_ReplayIsDeterministic(){var a=DistrictHydrology.Run(37,new InterventionConfig{GardenCapacityM3=12},8);var b=DistrictHydrology.Run(37,new InterventionConfig{GardenCapacityM3=12},8);Assert.That(a.StorageM3,Is.EqualTo(b.StorageM3));Assert.That(a.DrainedM3,Is.EqualTo(b.DrainedM3));}
[Test] public void A16_StoppingRainPreservesUndrainedStorage(){var m=Model();m.Step(40,600);double before=m.Result.StorageM3;m.Step(0,600);Assert.That(m.Result.StorageM3,Is.EqualTo(before));}
[Test] public void A17_StoppingRainAllowsDrainage(){var m=Model();m.Step(40,600);double before=m.Result.StorageM3;m.Step(0,600,12);Assert.That(m.Result.StorageM3,Is.LessThan(before));}
[Test] public void A18_DrainRemovalUsesAreaEfficiencyAndTime(){var m=Model();m.Step(100,600);m.Step(0,60,10);Assert.That(m.LastDrainedM3,Is.EqualTo(10*.8*4652/1000/60).Within(1e-8));}
[Test] public void A19_WhatIfRainDoesNotAlterLive(){var w=Weather();var s=new ScenarioOverrides{RainMmPerHour=90};s.Apply(w);Assert.That(w.RainMmPerHour,Is.EqualTo(20));}
[Test] public void A20_WhatIfDrainIsSeparateFromLive(){var s=new SimulationState{Live=Weather(),Mode=WorldMode.WhatIf};s.Scenario.DrainageMmPerHour=0;Assert.That(s.Drainage,Is.Zero);Assert.That(s.Live.RainMmPerHour,Is.EqualTo(20));}
[Test] public void A21_ResetUsesLiveBaseline(){var s=new SimulationState{Live=Weather(),Mode=WorldMode.WhatIf};s.Scenario.RainMmPerHour=190;s.ResetScenario();Assert.That(s.Scenario.RainMmPerHour,Is.EqualTo(20));Assert.That(s.Scenario.DrainageMmPerHour,Is.EqualTo(12));}
[Test] public void A22_EngineResetClearsAccumulation(){var e=new CausalEnvironmentEngine();e.Step(Weather(),0,600);e.Reset();Assert.That(e.State.CollectionM3,Is.Zero);Assert.That(e.District.Result.StorageM3,Is.Zero);}
[Test] public void A23_LongRunConservesMass(){var m=Model(5);for(int i=0;i<10000;i++)m.Step(i%4==0?200:0,6);Assert.That(m.Result.BalanceError,Is.EqualTo(0).Within(1e-5));}
[Test] public void A24_ExtremeFiniteInputsAreClamped(){var m=Model(999);m.Step(999,99999);Assert.That(m.Result.RainM3,Is.EqualTo(930.4).Within(1e-8));Assert.That(m.Result.StorageM3,Is.InRange(0,150));}
[Test] public void A25_SubdivisionsPreserveUnsaturatedBalance(){var a=Model(4);var b=Model(4);a.Step(20,600);for(int i=0;i<100;i++)b.Step(20,6);Assert.That(a.Result.StorageM3,Is.EqualTo(b.Result.StorageM3).Within(1e-7));}
[Test] public void B01_ParsesCurrentTimestamp(){var r=WeatherJson.Parse("{\"current\":{\"time\":\"2026-01-01T12:00\",\"temperature_2m\":25}}");Assert.That(r.current.Convert().Time,Is.EqualTo("2026-01-01T12:00"));}
[Test] public void B02_Missing_Temperature(){var r=WeatherJson.Parse("{\"current\":{\"time\":\"2026-01-01T12:00\",\"temperature_2m\":null}}");Assert.That(r.current.Convert().Temperature,Is.Null);}
[Test] public void B03_Missing_PrecipitationMm(){var r=WeatherJson.Parse("{\"current\":{\"time\":\"2026-01-01T12:00\",\"precipitation\":null}}");Assert.That(r.current.Convert().PrecipitationMm,Is.Null);}
[Test] public void B04_Missing_RainMmPerHour(){var r=WeatherJson.Parse("{\"current\":{\"time\":\"2026-01-01T12:00\",\"rain\":null}}");Assert.That(r.current.Convert().RainMmPerHour,Is.Null);}
[Test] public void B05_Missing_WindKmh(){var r=WeatherJson.Parse("{\"current\":{\"time\":\"2026-01-01T12:00\",\"wind_speed_10m\":null}}");Assert.That(r.current.Convert().WindKmh,Is.Null);}
[Test] public void B06_Missing_WindDegrees(){var r=WeatherJson.Parse("{\"current\":{\"time\":\"2026-01-01T12:00\",\"wind_direction_10m\":null}}");Assert.That(r.current.Convert().WindDegrees,Is.Null);}
[Test] public void B07_Missing_CloudPercent(){var r=WeatherJson.Parse("{\"current\":{\"time\":\"2026-01-01T12:00\",\"cloud_cover\":null}}");Assert.That(r.current.Convert().CloudPercent,Is.Null);}
[Test] public void B08_Missing_SolarWm2(){var r=WeatherJson.Parse("{\"current\":{\"time\":\"2026-01-01T12:00\",\"shortwave_radiation\":null}}");Assert.That(r.current.Convert().SolarWm2,Is.Null);}
[Test] public void B09_MalformedJsonRejected(){Assert.Catch(()=>WeatherJson.Parse("{broken"));}
[Test] public void B10_EmptyBodyRejected(){Assert.Catch(()=>WeatherJson.Parse(""));}
[Test] public void B18_MissingIsNotZero(){Assert.That(new ApiValues{rain=0,interval=null}.Convert().RainMmPerHour,Is.Null);}
[Test] public void B19_CurrentIntervalAndHourlyUnitsRemainDistinct(){Assert.That(new ApiValues{rain=2,interval=900}.Convert().RainMmPerHour,Is.EqualTo(8));Assert.That(new ApiHourly{time=new[]{"2026-01-01T13:00"},rain=new double?[]{2}}.At(0).RainMmPerHour,Is.EqualTo(2));}
[Test] public void B20_FailureDoesNotCreateFreshLive(){var s=new WeatherDataStore();s.BeginRequest(true);s.Fail("offline");Assert.That(s.Status.RetrievedUtc,Is.Null);Assert.That(s.Current,Is.Null);Assert.That(s.Status.Label,Does.Contain("failed"));}
}}