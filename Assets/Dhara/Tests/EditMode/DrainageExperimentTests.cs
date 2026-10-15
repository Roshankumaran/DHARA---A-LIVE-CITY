using NUnit.Framework;

namespace Dhara.Tests
{
    public sealed class DrainageExperimentTests
    {
        static WeatherData Input(double rain=80) => new WeatherData { RainMmPerHour=rain,Temperature=25,WindKmh=10,CloudPercent=50 };
        [Test] public void OneHourComparisonMatchesProductionEquation()
        {
            var experiment=new DrainageExperiment();Assert.That(experiment.Capture(Input(),12),Is.True);
            var result=experiment.Compare(20);
            Assert.That(result.BaselineWaterMm,Is.EqualTo(58).Within(.00001));
            Assert.That(result.InterventionWaterMm,Is.EqualTo(42).Within(.00001));
            Assert.That(result.ReductionMm,Is.EqualTo(16).Within(.00001));
        }
        [Test] public void CapturedBaselineIsIndependentAndZeroUpgradeResetsComparison()
        {
            var input=Input();var experiment=new DrainageExperiment();experiment.Capture(input,12);input.RainMmPerHour=0;
            var first=experiment.Compare(20);var reset=experiment.Compare(0);
            Assert.That(first.RainMmPerHour,Is.EqualTo(80));Assert.That(reset.BaselineWaterMm,Is.EqualTo(reset.InterventionWaterMm));
            Assert.That(input.RainMmPerHour,Is.Zero);Assert.That(experiment.Compare(20).InterventionWaterMm,Is.EqualTo(first.InterventionWaterMm));
        }
        [Test] public void DryConditionsCannotCreateInventedBenefitsOrNegativeWater()
        {
            var experiment=new DrainageExperiment();experiment.Capture(Input(0),12);var result=experiment.Compare(1000);
            Assert.That(result.InterventionDrainage,Is.EqualTo(100));Assert.That(result.ReductionMm,Is.Zero);Assert.That(result.InterventionWaterMm,Is.Zero);
        }
        [Test] public void IncompleteInputsCannotStartAnExperiment()
        {
            var experiment=new DrainageExperiment();Assert.That(experiment.Capture(new WeatherData(),12),Is.False);Assert.That(experiment.Compare(20),Is.Null);
        }
    }
}
