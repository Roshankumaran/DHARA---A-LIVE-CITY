using NUnit.Framework;
using System;
namespace Dhara.Tests
{
    public class DistrictHydrologyTests
    {
        [Test] public void TraceReportsCalculatedResultsAndProvenance()
        {var e=new DistrictExperiment();e.Capture(new WeatherData{Time="2026-10-01T12:00",RainMmPerHour=10,Temperature=25,WindKmh=10,CloudPercent=50},12,"Forecast","Delhi",null);var c=e.Compare(new InterventionConfig());var t=ImpactTrace.From(c);Assert.That(t.Stages.Count,Is.EqualTo(7));Assert.That(t.SourceTime,Is.EqualTo(c.Input.Time));Assert.That(t.Stages[1].Explanation,Does.Contain("46.520"));}
        [Test] public void ComparisonFreezesInputAndUsesIdenticalConditions()
        {var w=new WeatherData{RainMmPerHour=30,Temperature=25,WindKmh=10,CloudPercent=50};var e=new DistrictExperiment();Assert.That(e.Capture(w,12,"WhatIf","Bengaluru",null),Is.True);w.RainMmPerHour=100;var c=e.Compare(new InterventionConfig());Assert.That(c.Input.RainMmPerHour,Is.EqualTo(30));Assert.That(c.Baseline.StorageM3,Is.EqualTo(c.Intervention.StorageM3));Assert.That(c.Baseline.Seconds,Is.EqualTo(3600).Within(1e-8));Assert.That(DistrictComparison.Percent(0,10),Is.Null);}
        [Test] public void RainDepthUsesCorrectAreaAndMassIsConserved()
        { var r=DistrictHydrology.Run(10,new InterventionConfig());Assert.That(r.RainM3,Is.EqualTo(46.52).Within(1e-8));Assert.That(r.BalanceError,Is.EqualTo(0).Within(1e-8)); }
        [Test] public void GreenRoofRetentionHasFiniteCapacity()
        { var m=new DistrictHydrology(new InterventionConfig{GreenRoofFraction=1});for(int i=0;i<1200;i++)m.Step(100,6);Assert.That(m.Zones[1].RetainedM3,Is.EqualTo(28.08).Within(1e-8));Assert.That(m.Result.BalanceError,Is.EqualTo(0).Within(1e-7)); }
        [Test] public void PermeablePavingInfiltratesAtConfiguredRate()
        {var r=DistrictHydrology.Run(50,new InterventionConfig{PermeableFraction=1});Assert.That(r.InfiltratedM3,Is.EqualTo(2128*.015+1120*.008).Within(1e-8));}
        [Test] public void AllInterventionsConserveMassDuringOverflow()
        {var r=DistrictHydrology.Run(200,new InterventionConfig{GreenRoofFraction=.7,PermeableFraction=.3,GardenCapacityM3=20},0,14400);Assert.That(r.OverflowM3,Is.GreaterThan(0));Assert.That(r.StorageM3,Is.LessThanOrEqualTo(150));Assert.That(r.BalanceError,Is.EqualTo(0).Within(1e-5));}
        [Test] public void DrainageCannotRemoveUnavailableWater()
        {var r=DistrictHydrology.Run(0,new InterventionConfig{ExtraDrainage=60});Assert.That(r.DrainedM3+r.StorageM3+r.RainM3,Is.Zero);}
        [Test] public void DrainageReducesCollectionStorage()
        {Assert.That(DistrictHydrology.Run(40,new InterventionConfig{ExtraDrainage=20}).StorageM3,Is.LessThan(DistrictHydrology.Run(40,new InterventionConfig()).StorageM3));}
        [Test] public void InvalidConfigurationRejectedAndInputsRemainFinite()
        {Assert.Throws<ArgumentException>(()=>new DistrictHydrology(new InterventionConfig{GreenRoofFraction=double.NaN}));var m=new DistrictHydrology(new InterventionConfig());m.Step(double.PositiveInfinity,6);Assert.That(m.Result.RainM3,Is.Zero);}
        [Test] public void ModelCopiesConfigurationAndIsReproducible()
        {var c=new InterventionConfig{GreenRoofFraction=.5};var a=new DistrictHydrology(c);c.GreenRoofFraction=1;var b=new DistrictHydrology(new InterventionConfig{GreenRoofFraction=.5});for(int i=0;i<100;i++){a.Step(50,6);b.Step(50,6);}Assert.That(a.Result.StorageM3,Is.EqualTo(b.Result.StorageM3));}
    }
}
