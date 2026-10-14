using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

namespace Dhara
{
    public sealed class WeatherService : MonoBehaviour
    {
        public readonly DataSourceStatus Status = new DataSourceStatus();
        public WeatherData Current { get; private set; }
        public readonly List<WeatherData> Forecast = new List<WeatherData>();
        public event Action Updated;
        public int Selected { get; private set; }
        public Func<Location,string> RequestUrl = WeatherRequest.Url;
        Coroutine loop;
        UnityWebRequest active;
        public static string Url(Location location) => WeatherRequest.Url(location);
        public void Select(int index)
        {
            Selected = Mathf.Clamp(index,0,Location.All.Length-1);
            if (loop != null) StopCoroutine(loop);
            if (active != null) { active.Abort(); active.Dispose(); active = null; }
            Current = null; Forecast.Clear(); Status.RetrievedUtc = null; Status.Error = null;
            Updated?.Invoke(); loop = StartCoroutine(RefreshLoop());
        }
        public void Refresh() => Select(Selected);
        IEnumerator RefreshLoop()
        {
            int failures = 0;
            while (true)
            {
                Status.Loading = true;
                active = UnityWebRequest.Get(RequestUrl(Location.All[Selected])); active.timeout = 20;
                yield return active.SendWebRequest();
                if (active.result != UnityWebRequest.Result.Success) { Status.Error = active.error; failures++; }
                else
                {
                    try
                    {
                        var response = WeatherJson.Parse(active.downloadHandler.text);
                        var current = response?.current?.Convert();
                        if (current == null || string.IsNullOrEmpty(current.Time)) throw new FormatException("Missing current weather timestamp");
                        Current = current; Forecast.Clear();
                        if (response.hourly?.time != null)
                            for (int i=0; i<response.hourly.time.Length; i++)
                            {
                                var item = response.hourly.At(i);
                                if (string.CompareOrdinal(item.Time, current.Time) >= 0) Forecast.Add(item);
                            }
                        Status.RetrievedUtc = DateTime.UtcNow; Status.Error = null; failures = 0;
                    }
                    catch (Exception ex) { Status.Error = "Invalid weather response: " + ex.Message; failures++; }
                }
                active.Dispose(); active = null; Status.Loading = false; Updated?.Invoke();
                yield return new WaitForSecondsRealtime(failures == 0 ? 300 : Mathf.Min(300, 15 * Mathf.Pow(2, Mathf.Min(failures-1,5))));
            }
        }
        void OnDestroy() { if (active != null) { active.Abort(); active.Dispose(); } }
    }
}

