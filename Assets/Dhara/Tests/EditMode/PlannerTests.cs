using NUnit.Framework;
using System.Linq;
namespace Dhara.Tests
{
    public class PlannerTests
    {
        [Test] public void SearchIsBoundedAndReproducibleWithinConstraints()
        {var max=new InterventionConfig{GreenRoofFraction=.5,PermeableFraction=.3,ExtraDrainage=20,GardenCapacityM3=10};var a=InterventionPlanner.Evaluate(50,12,max).ToArray();var b=InterventionPlanner.Evaluate(50,12,max).ToArray();Assert.That(a.Length,Is.EqualTo(36));for(int i=0;i<a.Length;i++){Assert.That(a[i].Configuration.GreenRoofFraction,Is.LessThanOrEqualTo(.5));Assert.That(a[i].Configuration.PermeableFraction,Is.LessThanOrEqualTo(.3));Assert.That(a[i].Result.StorageM3,Is.EqualTo(b[i].Result.StorageM3));Assert.That(a[i].Result.BalanceError,Is.EqualTo(0).Within(1e-7));}}
        [Test] public void DisabledOptionsYieldOnlyBaseline()
        {Assert.That(InterventionPlanner.Evaluate(20,12,new InterventionConfig()).Count(),Is.EqualTo(1));}
    }
}
