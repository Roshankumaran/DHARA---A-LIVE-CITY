using System;
using System.Collections.Generic;
namespace Dhara
{
    public sealed class ImpactStage
    {
        public string Title,ZoneId,Explanation;
        public ImpactStage(string title,string zone,string explanation){Title=title;ZoneId=zone;Explanation=explanation;}
    }
    public sealed class ImpactTrace
    {
        public string SourceTime,Classification;
        public DateTime CalculatedUtc;
        public readonly List<ImpactStage> Stages=new List<ImpactStage>();
        public static ImpactTrace From(DistrictComparison comparison)
        {
            if(comparison==null)return null;
            var r=comparison.Intervention;var c=comparison.Configuration;
            var trace=new ImpactTrace{SourceTime=comparison.Input.Time,Classification=comparison.Classification,CalculatedUtc=DateTime.UtcNow};
            trace.Stages.Add(new ImpactStage("Weather forcing","roof",$"{comparison.Input.RainMmPerHour:0.00} mm/h held constant for {r.Seconds/60:0} minutes. Source valid: {comparison.Input.Time ?? "hypothetical"} IST. {comparison.Classification}."));
            trace.Stages.Add(new ImpactStage("Rain contribution","road",$"Rain × 4,652 m² × 1 hour / 1000 = {r.RainM3:0.000} m³. Shared weather input across conceptual zones."));
            trace.Stages.Add(new ImpactStage("Surface response","green-roof",$"Roof coverage {c.GreenRoofFraction*100:0}% holds up to 20 mm. Paving coverage {c.PermeableFraction*100:0}% infiltrates up to 15 mm/h. Total infiltration {r.InfiltratedM3:0.000} m³; retained including garden {r.RetainedM3:0.000} m³."));
            trace.Stages.Add(new ImpactStage("Runoff routing","road",$"Unretained, uninfiltrated surface water routes instantly to collection. Peak surface runoff {r.PeakRunoffM3Hour:0.000} m³/h; this peak is upstream of garden and drainage."));
            trace.Stages.Add(new ImpactStage("Drainage response","collection",$"0.8 × {Math.Min(100,comparison.Drainage+c.ExtraDrainage):0.0} mm/h × catchment area, limited by water available. Removed {r.DrainedM3:0.000} m³."));
            trace.Stages.Add(new ImpactStage("Storage and overflow","collection",$"Final collection {r.StorageM3:0.000} m³; maximum {r.MaximumStorageM3:0.000} m³; overflow {r.OverflowM3:0.000} m³. Mass-balance residual {r.BalanceError:0.000000} m³."));
            trace.Stages.Add(new ImpactStage("World response","collection","Coverage overlay identifies changed roof and road categories. Water overlay encodes simulated collection volume and retained surface water, not measured flood depths."));
            return trace;
        }
    }
}
