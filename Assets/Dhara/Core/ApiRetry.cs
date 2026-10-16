using System;
using System.Globalization;
namespace Dhara
{
    public static class ApiRetry
    {
        public static double Delay(int failures,long httpStatus,string retryAfter,DateTime nowUtc)
        {
            double backoff=failures==0?300:Math.Min(300,15*Math.Pow(2,Math.Min(failures-1,5)));
            if(httpStatus!=429&&httpStatus!=503)return backoff;
            if(double.TryParse(retryAfter,NumberStyles.None,CultureInfo.InvariantCulture,out var seconds)&&seconds>=0)
                return Math.Max(backoff,Math.Min(86400,seconds));
            if(DateTimeOffset.TryParse(retryAfter,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var date))
                return Math.Max(backoff,Math.Min(86400,(date.UtcDateTime-nowUtc).TotalSeconds));
            return backoff;
        }
    }
}
