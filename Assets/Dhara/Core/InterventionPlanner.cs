using System;
using System.Collections.Generic;
namespace Dhara
{
    public enum PlanningObjective { PeakRunoff, MaximumStorage }
    public sealed class EvaluatedIntervention
    {
        public int Index;
        public InterventionConfig Configuration;
        public DistrictResult Result;
        public double Area => Configuration.GreenRoofFraction*DistrictHydrology.RoofArea+Configuration.PermeableFraction*DistrictHydrology.PavedArea;
    }
    public static class InterventionPlanner
    {
        // Fixed grid: 3 roof × 3 paving × 2 drainage × 2 garden = at most 36 cases.
        public static IEnumerable<EvaluatedIntervention> Evaluate(double rain,double drainage,InterventionConfig maximum)
        {
            maximum.Validate();int index=0;
            for(int roof=0;roof<3;roof++)for(int paving=0;paving<3;paving++)for(int drain=0;drain<2;drain++)for(int garden=0;garden<2;garden++)
            {
                if((maximum.GreenRoofFraction==0&&roof>0)||(maximum.PermeableFraction==0&&paving>0)||(maximum.ExtraDrainage==0&&drain>0)||(maximum.GardenCapacityM3==0&&garden>0))continue;
                var c=new InterventionConfig{GreenRoofFraction=maximum.GreenRoofFraction*roof/2,PermeableFraction=maximum.PermeableFraction*paving/2,ExtraDrainage=maximum.ExtraDrainage*drain,GardenCapacityM3=maximum.GardenCapacityM3*garden};
                yield return new EvaluatedIntervention{Index=++index,Configuration=c,Result=DistrictHydrology.Run(rain,c,drainage)};
            }
        }
        public static bool Better(EvaluatedIntervention candidate,EvaluatedIntervention best,PlanningObjective objective)
        {
            if(best==null)return true;
            double a=objective==PlanningObjective.PeakRunoff?candidate.Result.PeakRunoffM3Hour:candidate.Result.MaximumStorageM3;
            double b=objective==PlanningObjective.PeakRunoff?best.Result.PeakRunoffM3Hour:best.Result.MaximumStorageM3;
            return a<b-1e-9||(Math.Abs(a-b)<1e-9&&candidate.Area<best.Area-1e-9);
        }
    }
}
