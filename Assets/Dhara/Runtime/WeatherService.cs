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
        IEnumerator RefreshLoop()
        {
            int failures = 0;
            while (true)
            {
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
                else if (active.result != UnityWebRequest.Result.Success) { data.Fail(active.error); failures++; }
                else
                {
                    try
                    {
                        var response = WeatherJson.Parse(active.downloadHandler.text);
                        data.Accept(response,DateTime.UtcNow); failures = 0;
                        CityCache[Selected].Accept(response,data.Status.RetrievedUtc.Value);
                    }
                    catch (Exception ex) { data.Fail("Invalid weather response: " + ex.Message); failures++; }
                }
                active?.Dispose(); active = null; Updated?.Invoke();
                yield return new WaitForSecondsRealtime(failures == 0 ? 300 : Mathf.Min(300, 15 * Mathf.Pow(2, Mathf.Min(failures-1,5))));
            }
        }
        void OnDestroy() { if (active != null) { active.Abort(); active.Dispose(); } }
    }
}
