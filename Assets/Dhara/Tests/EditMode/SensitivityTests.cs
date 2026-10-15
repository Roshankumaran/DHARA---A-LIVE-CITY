using System;
using NUnit.Framework;
namespace Dhara.Tests
{
    public class SensitivityTests
    {
        [Test] public void SweepChangesOneVariableAndMatchesIndependentCalculation()
        {var config=new InterventionConfig{GreenRoofFraction=.3,PermeableFraction=.5,ExtraDrainage=10};var cases=SensitivityAnalysis.Evaluate(20,12,config,SensitivityVariable.RainMmHour,0,100,20);Assert.That(cases.Count,Is.EqualTo(6));for(int i=0;i<cases.Count;i++){var expected=DistrictHydrology.Run(i*20,config,12);Assert.That(cases[i].Result.StorageM3,Is.EqualTo(expected.StorageM3));}Assert.That(config.GreenRoofFraction,Is.EqualTo(.3));}
        [Test] public void ExcessiveOrInvalidSweepIsRejected()
        {Assert.Throws<ArgumentException>(()=>SensitivityAnalysis.Evaluate(20,12,new InterventionConfig(),SensitivityVariable.RainMmHour,0,200,.1));Assert.Throws<ArgumentException>(()=>SensitivityAnalysis.Evaluate(20,12,new InterventionConfig(),SensitivityVariable.RainMmHour,0,100,0));}
    }
}
