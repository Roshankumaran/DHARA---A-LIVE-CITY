using System;
using System.Collections.Generic;
namespace Dhara
{
    public enum SensitivityVariable { RainMmHour, DrainageMmHour, GreenRoofPercent, PermeablePercent }
    public sealed class SensitivityCase
    {
        public double Value;
        public DistrictResult Result;
    }
    public static class SensitivityAnalysis
    {
        public static List<SensitivityCase> Evaluate(double rain,double drainage,InterventionConfig configuration,SensitivityVariable variable,double min,double max,double step)
        {
            configuration.Validate();double limit=variable==SensitivityVariable.RainMmHour?200:100;
            if(!Enum.IsDefined(typeof(SensitivityVariable),variable)||!Numbers.IsFinite(min)||!Numbers.IsFinite(max)||!Numbers.IsFinite(step)||step<=0||min<0||max>limit||min>max||(max-min)/step>20)throw new ArgumentException("Choose a valid range and positive step yielding at most 21 cases.");
            var cases=new List<SensitivityCase>();int count=(int)Math.Floor((max-min)/step+1e-9)+1;
            for(int i=0;i<count;i++)
            {
                double value=min+i*step;var c=configuration.Copy();double r=rain,d=drainage;
                switch(variable){case SensitivityVariable.RainMmHour:r=value;break;case SensitivityVariable.DrainageMmHour:d=value;break;case SensitivityVariable.GreenRoofPercent:c.GreenRoofFraction=value/100;break;case SensitivityVariable.PermeablePercent:c.PermeableFraction=value/100;break;}
                cases.Add(new SensitivityCase{Value=value,Result=DistrictHydrology.Run(r,c,d)});
            }
            return cases;
        }
    }
}
