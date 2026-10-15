using System;
using System.Collections.Generic;

namespace Dhara
{
    [Serializable] public sealed class InterventionConfig
    {
        public double GreenRoofFraction, PermeableFraction, ExtraDrainage, GardenCapacityM3;
        public InterventionConfig Copy() => (InterventionConfig)MemberwiseClone();
        public void Validate()
        {
            Check(GreenRoofFraction,0,1); Check(PermeableFraction,0,1);
            Check(ExtraDrainage,0,60); Check(GardenCapacityM3,0,50);
        }
        static void Check(double v,double min,double max)
        { if(!Numbers.IsFinite(v)||v<min||v>max)throw new ArgumentException("Intervention parameter outside supported range"); }
    }
    public sealed class SurfaceZone
    {
        public readonly string Id, Category, Downstream;
        public readonly double AreaM2, RetentionMm, InfiltrationMmHour;
        public double RetainedM3, InfiltratedM3, RainM3, RunoffM3, LastRunoffM3;
        public SurfaceZone(string id,string category,double area,double retention,double infiltration)
        { Id=id;Category=category;AreaM2=area;RetentionMm=retention;InfiltrationMmHour=infiltration;Downstream="collection"; }
    }
    public sealed class DistrictResult
    {
        public double RainM3, RetainedM3, InfiltratedM3, DrainedM3, OverflowM3, StorageM3, MaximumStorageM3, PeakRunoffM3Hour, Seconds;
        public double BalanceError => RainM3-RetainedM3-InfiltratedM3-DrainedM3-OverflowM3-StorageM3;
        public DistrictResult Copy() => (DistrictResult)MemberwiseClone();
    }
    // Explicit conceptual catchment; excludes unmodelled land. 1 mm over 1 m² = .001 m³.
    // Surface runoff is routed immediately to one shared collection node, not a hydraulic solver.
    public sealed class DistrictHydrology
    {
        public const string Version="district-water-1";
        public const double RoofArea=1404, PavedArea=2128, GreenArea=1120, TotalArea=4652;
        public const double CollectionCapacityM3=150;
        public const string Assumptions="Conceptual 4,652 m² catchment: roofs 1,404 m², road union 2,128 m², green strips 1,120 m². " +
            "Rain depth × area / 1000 gives m³. Green roofs hold up to 20 mm with no release during an experiment. " +
            "Permeable paving infiltrates up to 15 mm/h; green strips 8 mm/h. Remaining surface water routes instantly to collection. " +
            "Optional garden captures collection inflow up to its configured m³ capacity; no release. Drain removes 0.8 × nominal mm/h × catchment area / 1000, limited by available water. " +
            "Collection holds 150 m³; excess is reported as overflow. Initially dry; no evaporation, travel time, soil saturation, groundwater, or calibrated flood depth. Coverage is uniformly distributed across each category.";
        public readonly List<SurfaceZone> Zones=new List<SurfaceZone>();
        public readonly DistrictResult Result=new DistrictResult();
        public readonly InterventionConfig Configuration;
        public readonly double NominalDrainage;
        public double GardenStoredM3 {get;private set;}
        public double LastRunoffM3 {get;private set;}
        public double LastDrainedM3 {get;private set;}
        public DistrictHydrology(InterventionConfig config,double drainage=12)
        {
            if(config==null)throw new ArgumentNullException(nameof(config));config.Validate();Configuration=config.Copy();
            NominalDrainage=Numbers.Clamp(drainage,0,100);
            Zones.Add(new SurfaceZone("roof","Roof",RoofArea*(1-config.GreenRoofFraction),0,0));
            Zones.Add(new SurfaceZone("green-roof","Green roof",RoofArea*config.GreenRoofFraction,20,0));
            Zones.Add(new SurfaceZone("road","Road",PavedArea*(1-config.PermeableFraction),0,0));
            Zones.Add(new SurfaceZone("permeable","Permeable paving",PavedArea*config.PermeableFraction,0,15));
            Zones.Add(new SurfaceZone("green","Green strips",GreenArea,0,8));
        }
        public void Step(double rainMmHour,double seconds,double? drainageOverride=null)
        {
            if(!Numbers.IsFinite(rainMmHour)||!Numbers.IsFinite(seconds)||seconds<=0)return;
            double hours=Numbers.Clamp(seconds,0,3600)/3600, rain=Numbers.Clamp(rainMmHour,0,200);
            double runoff=0;
            foreach(var z in Zones)
            {
                double input=rain*hours*z.AreaM2/1000;
                double retained=Math.Min(input,Math.Max(0,z.RetentionMm*z.AreaM2/1000-z.RetainedM3));
                double infiltrated=Math.Min(input-retained,z.InfiltrationMmHour*hours*z.AreaM2/1000);
                z.LastRunoffM3=Math.Max(0,input-retained-infiltrated);
                z.RainM3+=input;z.RetainedM3+=retained;z.InfiltratedM3+=infiltrated;z.RunoffM3+=z.LastRunoffM3;
                Result.RainM3+=input;Result.RetainedM3+=retained;Result.InfiltratedM3+=infiltrated;runoff+=z.LastRunoffM3;
            }
            LastRunoffM3=runoff;Result.PeakRunoffM3Hour=Math.Max(Result.PeakRunoffM3Hour,runoff/hours);
            double garden=Math.Min(runoff,Math.Max(0,Configuration.GardenCapacityM3-GardenStoredM3));
            GardenStoredM3+=garden;Result.RetainedM3+=garden;
            double available=Result.StorageM3+runoff-garden;
            LastDrainedM3=Math.Min(available,Numbers.Clamp((drainageOverride??NominalDrainage)+Configuration.ExtraDrainage,0,100)*.8*hours*TotalArea/1000);
            Result.DrainedM3+=LastDrainedM3;available-=LastDrainedM3;
            Result.OverflowM3+=Math.Max(0,available-CollectionCapacityM3);
            Result.StorageM3=Math.Min(CollectionCapacityM3,Math.Max(0,available));
            Result.MaximumStorageM3=Math.Max(Result.MaximumStorageM3,Result.StorageM3);Result.Seconds+=hours*3600;
        }
        public static DistrictResult Run(double rain,InterventionConfig config,double drainage=12,int steps=600)
        {
            if(steps<1||steps>14400)throw new ArgumentOutOfRangeException(nameof(steps));
            var model=new DistrictHydrology(config,drainage);for(int i=0;i<steps;i++)model.Step(rain,6);return model.Result.Copy();
        }
    }
}
