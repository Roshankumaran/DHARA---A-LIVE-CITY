using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
namespace Dhara
{
    public sealed class ScenarioSnapshot
    {
        public string Name,CreatedUtc,OriginalClassification;
        public DistrictComparison Comparison;
    }
    public static class ScenarioReport
    {
        static readonly CultureInfo Invariant=CultureInfo.InvariantCulture;
        public static string Serialize(DistrictComparison c,string name)
        {
            if(c==null||c.Input==null||!c.Input.HasSimulationInputs)throw new ArgumentException("No calculated comparison to save");
            c.Configuration.Validate();if(string.IsNullOrWhiteSpace(name)||name.Length>80)throw new ArgumentException("Scenario name must contain 1–80 characters");
            var root=new Dictionary<string,object>{["schemaVersion"]=1,["modelVersion"]=DistrictHydrology.Version,["product"]="DHARA — A LIVING CITY",["name"]=name,["createdUtc"]=DateTime.UtcNow.ToString("o"),["classification"]=c.Classification,["originalClassification"]=c.OriginClassification??c.Classification,["location"]=c.Location,["sourceTimeIst"]=c.Input.Time,["retrievedUtc"]=c.RetrievedUtc,
                ["source"]=c.Classification=="Historical"?"Open-Meteo ERA5 reanalysis":c.Classification=="Demonstration"?"Deterministic demonstration":c.Classification=="WhatIf"?"Hypothetical inputs with available live baseline":c.Classification=="Recorded"?"Recorded snapshot":"Open-Meteo weather model",
                ["assumptions"]=DistrictHydrology.Assumptions,["durationSeconds"]=3600,["stepSeconds"]=6,["initialStorageM3"]=0,["nominalDrainageMmHour"]=c.Drainage,
                ["input"]=new Dictionary<string,object>{["temperatureC"]=c.Input.Temperature,["rainMmHour"]=c.Input.RainMmPerHour,["precipitationMm"]=c.Input.PrecipitationMm,["windKmh"]=c.Input.WindKmh,["windDegrees"]=c.Input.WindDegrees,["cloudPercent"]=c.Input.CloudPercent,["humidityPercent"]=c.Input.HumidityPercent,["solarWm2"]=c.Input.SolarWm2,["weatherCode"]=c.Input.WeatherCode,["intervalSeconds"]=c.Input.IntervalSeconds>0?(double?)c.Input.IntervalSeconds:null},
                ["configuration"]=new Dictionary<string,object>{["greenRoofFraction"]=c.Configuration.GreenRoofFraction,["permeableFraction"]=c.Configuration.PermeableFraction,["extraDrainageMmHour"]=c.Configuration.ExtraDrainage,["gardenCapacityM3"]=c.Configuration.GardenCapacityM3},
                ["baselineConfiguration"]=new Dictionary<string,object>{["greenRoofFraction"]=0,["permeableFraction"]=0,["extraDrainageMmHour"]=0,["gardenCapacityM3"]=0},
                ["zones"]=new Dictionary<string,object>{["roofM2"]=1404,["roadM2"]=2128,["greenM2"]=1120,["downstream"]="shared collection",["collectionCapacityM3"]=150},
                ["baseline"]=Metrics(c.Baseline),["intervention"]=Metrics(c.Intervention)};
            var differences=new Dictionary<string,object>();var a=Metrics(c.Baseline);var b=Metrics(c.Intervention);
            foreach(var key in a.Keys){double x=(double)a[key],y=(double)b[key];differences[key]=new Dictionary<string,object>{["absolute"]=y-x,["percent"]=DistrictComparison.Percent(x,y)};}root["differences"]=differences;
            var text=new StringBuilder();Write(text,root);return text.ToString();
        }
        public static ScenarioSnapshot Parse(string json)
        {
            var root=WeatherJson.Object(json);
            if(Number(root,"schemaVersion")!=1||String(root,"modelVersion")!=DistrictHydrology.Version)throw new FormatException("Incompatible scenario schema or model version");
            string name=String(root,"name"),created=String(root,"createdUtc"),city=String(root,"location"),classification=String(root,"classification");
            if(string.IsNullOrWhiteSpace(name)||name.Length>80||!DateTimeOffset.TryParse(created,Invariant,DateTimeStyles.None,out _)||!Array.Exists(Location.All,l=>l.Name==city)||!Enum.TryParse<WorldMode>(classification,out var state)||!Enum.IsDefined(typeof(WorldMode),state))throw new FormatException("Invalid scenario metadata");
            if(Number(root,"durationSeconds")!=3600||Number(root,"stepSeconds")!=6||Number(root,"initialStorageM3")!=0)throw new FormatException("Unsupported experiment conditions");
            var values=Object(root,"input");var config=Object(root,"configuration");
            var input=new WeatherData{Time=String(root,"sourceTimeIst",false),Temperature=Nullable(values,"temperatureC"),RainMmPerHour=Nullable(values,"rainMmHour"),PrecipitationMm=Nullable(values,"precipitationMm"),WindKmh=Nullable(values,"windKmh"),WindDegrees=Nullable(values,"windDegrees"),CloudPercent=Nullable(values,"cloudPercent"),HumidityPercent=Nullable(values,"humidityPercent"),SolarWm2=Nullable(values,"solarWm2"),WeatherCode=Nullable(values,"weatherCode"),IntervalSeconds=Nullable(values,"intervalSeconds")??0};
            if(!input.HasSimulationInputs||input.Temperature< -30||input.Temperature>60||input.RainMmPerHour<0||input.RainMmPerHour>200||input.WindKmh<0||input.WindKmh>180||input.CloudPercent<0||input.CloudPercent>100)throw new FormatException("Invalid environmental inputs");
            if(input.Time!=null&&!DateTime.TryParseExact(input.Time,"yyyy-MM-dd'T'HH:mm",Invariant,DateTimeStyles.None,out _))throw new FormatException("Invalid source timestamp");
            if(input.WindDegrees<0||input.WindDegrees>360||input.HumidityPercent<0||input.HumidityPercent>100||input.PrecipitationMm<0||input.SolarWm2<0||input.SolarWm2>1500)throw new FormatException("Invalid optional environmental values");
            if(input.WeatherCode.HasValue&&(input.WeatherCode!=Math.Truncate(input.WeatherCode.Value)||!WeatherCondition.Known((int)input.WeatherCode.Value))||input.IntervalSeconds<0||input.IntervalSeconds>3600)throw new FormatException("Invalid weather metadata");
            var c=new InterventionConfig{GreenRoofFraction=Number(config,"greenRoofFraction"),PermeableFraction=Number(config,"permeableFraction"),ExtraDrainage=Number(config,"extraDrainageMmHour"),GardenCapacityM3=Number(config,"gardenCapacityM3")};c.Validate();
            double drainage=Number(root,"nominalDrainageMmHour");if(drainage<0||drainage>100)throw new FormatException("Invalid drainage");
            var experiment=new DistrictExperiment();experiment.Capture(input,drainage,"Recorded",city,String(root,"retrievedUtc",false));var comparison=experiment.Compare(c);
            CheckResults(Object(root,"baseline"),comparison.Baseline);CheckResults(Object(root,"intervention"),comparison.Intervention);
            comparison.OriginClassification=String(root,"originalClassification",false)??classification;
            return new ScenarioSnapshot{Name=name,CreatedUtc=created,OriginalClassification=classification,Comparison=comparison};
        }
        public static string Csv(DistrictComparison c)
        {
            var text=new StringBuilder("field,baseline,intervention,absolute_difference,percent_difference\r\n");
            void Metadata(string key,string value){text.Append(Quote(key)).Append(',').Append(Quote(value)).Append(",,,\r\n");}
            Metadata("product","DHARA — A LIVING CITY");Metadata("createdUtc",DateTime.UtcNow.ToString("o"));Metadata("location",c.Location);Metadata("classification",c.Classification);Metadata("sourceTimeIst",c.Input.Time);Metadata("retrievedUtc",c.RetrievedUtc);Metadata("modelVersion",DistrictHydrology.Version);Metadata("assumptions",DistrictHydrology.Assumptions);
            Metadata("rainMmHour",Format(c.Input.RainMmPerHour));Metadata("temperatureC",Format(c.Input.Temperature));Metadata("windKmh",Format(c.Input.WindKmh));Metadata("cloudPercent",Format(c.Input.CloudPercent));Metadata("precipitationMm",Format(c.Input.PrecipitationMm));Metadata("solarWm2",Format(c.Input.SolarWm2));Metadata("windDegrees",Format(c.Input.WindDegrees));Metadata("humidityPercent",Format(c.Input.HumidityPercent));Metadata("weatherCode",Format(c.Input.WeatherCode));Metadata("intervalSeconds",Format(c.Input.IntervalSeconds>0?(double?)c.Input.IntervalSeconds:null));
            Metadata("nominalDrainageMmHour",Format(c.Drainage));Metadata("greenRoofFraction",Format(c.Configuration.GreenRoofFraction));Metadata("permeableFraction",Format(c.Configuration.PermeableFraction));Metadata("extraDrainageMmHour",Format(c.Configuration.ExtraDrainage));Metadata("gardenCapacityM3",Format(c.Configuration.GardenCapacityM3));Metadata("baselineConfiguration","zero roof/paving coverage, zero extra drainage, zero garden");Metadata("durationSeconds","3600; initially dry; 6-second steps");
            var a=Metrics(c.Baseline);var b=Metrics(c.Intervention);foreach(var key in a.Keys){double x=(double)a[key],y=(double)b[key];text.Append(Quote(key)).Append(',').Append(Format(x)).Append(',').Append(Format(y)).Append(',').Append(Format(y-x)).Append(',').Append(Format(DistrictComparison.Percent(x,y))).Append("\r\n");}return text.ToString();
        }
        static Dictionary<string,object> Metrics(DistrictResult r)=>new Dictionary<string,object>{["rainM3"]=r.RainM3,["retainedM3"]=r.RetainedM3,["infiltratedM3"]=r.InfiltratedM3,["drainedM3"]=r.DrainedM3,["overflowM3"]=r.OverflowM3,["finalStorageM3"]=r.StorageM3,["maximumStorageM3"]=r.MaximumStorageM3,["peakRunoffM3Hour"]=r.PeakRunoffM3Hour};
        static void CheckResults(Dictionary<string,object> saved,DistrictResult actual){foreach(var pair in Metrics(actual))if(Math.Abs(Number(saved,pair.Key)-(double)pair.Value)>1e-6)throw new FormatException("Recorded outputs disagree with current model");}
        static Dictionary<string,object> Object(Dictionary<string,object> d,string key)=>d.TryGetValue(key,out var v)&&v is Dictionary<string,object> value?value:throw new FormatException("Missing object: "+key);
        static string String(Dictionary<string,object> d,string key,bool required=true){if(d.TryGetValue(key,out var v)&&v is string s)return s;if(!required)return null;throw new FormatException("Missing string: "+key);}
        static double? Nullable(Dictionary<string,object> d,string key){if(!d.TryGetValue(key,out var v)||v==null)return null;if(v is double n&&Numbers.IsFinite(n))return n;throw new FormatException("Invalid number: "+key);}
        static double Number(Dictionary<string,object> d,string key)=>Nullable(d,key)??throw new FormatException("Missing number: "+key);
        static string Format(double? value)=>Numbers.IsFinite(value)?value.Value.ToString("R",Invariant):"Unavailable";
        static string Quote(string s)=>"\""+(s??"Unavailable").Replace("\"","\"\"")+"\"";
        static void Write(StringBuilder b,object value)
        {
            if(value==null){b.Append("null");return;}if(value is string text){b.Append('"');foreach(char c in text){if(c=='"'||c=='\\')b.Append('\\').Append(c);else if(c<32)b.Append("\\u").Append(((int)c).ToString("x4"));else b.Append(c);}b.Append('"');return;}
            if(value is IDictionary dictionary){b.Append('{');bool first=true;foreach(DictionaryEntry entry in dictionary){if(!first)b.Append(',');first=false;Write(b,(string)entry.Key);b.Append(':');Write(b,entry.Value);}b.Append('}');return;}
            if(value is double n){if(!Numbers.IsFinite(n))throw new FormatException("Nonfinite export value");b.Append(n.ToString("R",Invariant));return;}
            if(value is int integer){b.Append(integer.ToString(Invariant));return;}throw new FormatException("Unsupported export type");
        }
    }
}
