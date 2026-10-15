using System;
namespace Dhara
{
    public sealed class DistrictComparison
    {
        public WeatherData Input;
        public string Classification, Location, RetrievedUtc;
        public string OriginClassification;
        public double Drainage;
        public InterventionConfig Configuration;
        public DistrictResult Baseline, Intervention;
        public static double? Percent(double baseline,double intervention) => Math.Abs(baseline)>1e-9 ? (intervention-baseline)/baseline*100 : (double?)null;
    }
    public sealed class DistrictExperiment
    {
        WeatherData input;
        string classification,location,retrieved,origin;
        double drainage;
        public bool Capture(WeatherData weather,double nominalDrainage,string state,string city,string retrievedUtc,string originalClassification=null)
        {
            if(weather==null||!weather.HasSimulationInputs)return false;
            input=weather.Copy();drainage=Numbers.Clamp(nominalDrainage,0,100);classification=state;location=city;retrieved=retrievedUtc;origin=originalClassification;return true;
        }
        public DistrictComparison Compare(InterventionConfig configuration)
        {
            if(input==null)return null;configuration.Validate();
            return new DistrictComparison{Input=input.Copy(),Classification=classification,OriginClassification=origin,Location=location,RetrievedUtc=retrieved,Drainage=drainage,Configuration=configuration.Copy(),
                Baseline=DistrictHydrology.Run(input.RainMmPerHour.Value,new InterventionConfig(),drainage),Intervention=DistrictHydrology.Run(input.RainMmPerHour.Value,configuration,drainage)};
        }
    }
}
