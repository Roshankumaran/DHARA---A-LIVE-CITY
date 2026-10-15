using System;

namespace Dhara
{
    // A frozen one-hour comparison using the same production water equation.
    // Only additional nominal drainage is supported; no arbitrary green-infrastructure benefits.
    public sealed class DrainageExperiment
    {
        WeatherData baseline;
        double drainage;
        public bool Capture(WeatherData input,double nominalDrainage)
        {
            if(input==null || !input.HasSimulationInputs)return false;
            baseline=input.Copy();drainage=Numbers.Clamp(nominalDrainage,0,100);return true;
        }
        public DrainageComparison Compare(double extraDrainage)
        {
            if(baseline==null)return null;
            double improved=Numbers.Clamp(drainage+Numbers.Clamp(extraDrainage,0,100),0,100);
            double before=0,after=0;
            for(int step=0;step<600;step++)
            {
                before=WaterAccumulationModel.Step(before,baseline.RainMmPerHour.Value,drainage,baseline.Temperature.Value,6);
                after=WaterAccumulationModel.Step(after,baseline.RainMmPerHour.Value,improved,baseline.Temperature.Value,6);
            }
            return new DrainageComparison { RainMmPerHour=baseline.RainMmPerHour.Value,TemperatureC=baseline.Temperature.Value,
                BaselineDrainage=drainage,InterventionDrainage=improved,BaselineWaterMm=before,InterventionWaterMm=after };
        }
    }
    public sealed class DrainageComparison
    {
        public double RainMmPerHour,TemperatureC,BaselineDrainage,InterventionDrainage,BaselineWaterMm,InterventionWaterMm;
        public double ReductionMm => Math.Max(0,BaselineWaterMm-InterventionWaterMm);
    }
}
