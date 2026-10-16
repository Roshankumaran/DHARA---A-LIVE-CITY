using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Dhara
{
    public sealed class WeatherService : MonoBehaviour
    {
        readonly WeatherDataStore data=new WeatherDataStore();
        public DataSourceStatus Status => data.Status;
        public WeatherData Current => data.Current;
        public List<WeatherData> Forecast => data.Forecast;
        public event Action Updated;
        public int Selected { get; private set; }
        public Func<Location,string> RequestUrl = WeatherRequest.Url;
        public readonly WeatherDataStore[] CityCache={new WeatherDataStore(),new WeatherDataStore(),new WeatherDataStore(),new WeatherDataStore(),new WeatherDataStore()};
        Coroutine loop;
        bool enforceRetry;
        UnityWebRequest active;
        public static string Url(Location location) => WeatherRequest.Url(location);
        public void Select(int index)
        {
            Selected = Mathf.Clamp(index,0,Location.All.Length-1);
            Restart(true);
        }
        void Restart(bool locationChanged)
        {
            if (loop != null) StopCoroutine(loop);
            if (active != null) { active.Abort(); active.Dispose(); active = null; }
            data.BeginRequest(locationChanged);
            if(locationChanged) Updated?.Invoke();
            loop = StartCoroutine(RefreshLoop());
        }
        public void Refresh() => Restart(false);
        public void CancelRequests()
        {if(loop!=null)StopCoroutine(loop);loop=null;if(active!=null){active.Abort();active.Dispose();active=null;}data.Status.Loading=false;}
        IEnumerator RefreshLoop()
        {
            int failures = 0;
            while (true)
            {
                if(enforceRetry&&Status.RetryUtc>DateTime.UtcNow)yield return new WaitForSecondsRealtime((float)(Status.RetryUtc.Value-DateTime.UtcNow).TotalSeconds);
                data.BeginRequest(false);
                UnityWebRequestAsyncOperation operation=null;
                string startupError=null;
                try
                {
                    active = UnityWebRequest.Get(RequestUrl(Location.All[Selected])); active.timeout = 20;
                    operation=active.SendWebRequest();
                }
                catch(Exception error) { startupError=error.Message; }
                if(operation!=null) yield return operation;
                if(startupError!=null) { data.Fail(startupError); failures++; }
                else if (active.result != UnityWebRequest.Result.Success) { data.Fail("HTTP "+active.responseCode+" · "+active.error); failures++; }
                else
                {
                    try
                    {
                        var response = ApiValidation.Forecast(active.downloadHandler.text,DateTime.UtcNow);
                        var retrievedUtc=DateTime.UtcNow;
                        CityCache[Selected].Accept(response,retrievedUtc);
                        data.Accept(response,retrievedUtc); failures = 0;
                    }
                    catch (Exception ex) { data.Fail("Invalid weather response: " + ex.Message); failures++; }
                }
                double delay=ApiRetry.Delay(failures,active?.responseCode??0,active?.GetResponseHeader("Retry-After"),DateTime.UtcNow);
                enforceRetry=active!=null&&(active.responseCode==429||active.responseCode==503&&!string.IsNullOrWhiteSpace(active.GetResponseHeader("Retry-After")));
                Status.RetryUtc=failures>0?DateTime.UtcNow.AddSeconds(delay):(DateTime?)null;
                active?.Dispose(); active = null; Updated?.Invoke();
                yield return new WaitForSecondsRealtime((float)delay);
            }
        }
        void OnDestroy() { if (active != null) { active.Abort(); active.Dispose(); } }
    }
}
