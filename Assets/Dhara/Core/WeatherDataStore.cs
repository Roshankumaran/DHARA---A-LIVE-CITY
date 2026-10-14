using System;
using System.Collections.Generic;
using System.Globalization;

namespace Dhara
{
    // Shared by the runtime service and standalone regression tests. A response
    // is validated before replacing any retained same-location data.
    public sealed class WeatherDataStore
    {
        public readonly DataSourceStatus Status=new DataSourceStatus();
        public WeatherData Current { get; private set; }
        public readonly List<WeatherData> Forecast=new List<WeatherData>();
        public void BeginRequest(bool locationChanged)
        {
            if(locationChanged) { Current=null; Forecast.Clear(); Status.RetrievedUtc=null; Status.Error=null; }
            Status.Loading=true;
        }
        public void Fail(string error) { Status.Loading=false; Status.Error=error; }
        public void Accept(WeatherResponse response, DateTime retrievedUtc)
        {
            var current=response?.current?.Convert();
            if(current==null || !DateTime.TryParseExact(current.Time,"yyyy-MM-dd'T'HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out var currentTime))
                throw new FormatException("Missing or invalid current weather timestamp");
            var forecast=new List<WeatherData>();
            if(response.hourly?.time!=null)
                for(int i=0;i<response.hourly.time.Length;i++)
                {
                    var item=response.hourly.At(i);
                    if(DateTime.TryParseExact(item.Time,"yyyy-MM-dd'T'HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out var time) && time>=currentTime)
                        forecast.Add(item);
                }
            Current=current; Forecast.Clear(); Forecast.AddRange(forecast);
            Status.RetrievedUtc=retrievedUtc; Status.Loading=false; Status.Error=null;
        }
    }
}
