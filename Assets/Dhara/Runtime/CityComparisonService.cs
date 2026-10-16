using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
namespace Dhara
{
    public sealed class CityComparisonService : MonoBehaviour
    {
        WeatherService weather;UnityWebRequest active;Coroutine job;
        public bool Loading {get;private set;}
        public void Initialize(WeatherService source){weather=source;}
        public void Refresh(bool force=false){Cancel();job=StartCoroutine(Fetch(force));}
        IEnumerator Fetch(bool force)
        {
            Loading=true;
            for(int i=0;i<Location.All.Length;i++)
            {
                var data=weather.CityCache[i];if(!force&&data.Status.RetrievedUtc.HasValue&&DateTime.UtcNow-data.Status.RetrievedUtc.Value<TimeSpan.FromMinutes(5))continue;
                data.BeginRequest(false);active=UnityWebRequest.Get(weather.RequestUrl(Location.All[i]));active.timeout=20;yield return active.SendWebRequest();
                if(active.result==UnityWebRequest.Result.Success){try{data.Accept(ApiValidation.Forecast(active.downloadHandler.text,DateTime.UtcNow),DateTime.UtcNow);}catch(Exception e){data.Fail(e.Message);}}
                else data.Fail(active.error);
                active.Dispose();active=null;
            }
            Loading=false;job=null;
        }
        public void Cancel(){if(job!=null)StopCoroutine(job);job=null;if(active!=null){active.Abort();active.Dispose();active=null;}if(weather!=null)foreach(var item in weather.CityCache)item.Status.Loading=false;Loading=false;}
        void OnDestroy(){Cancel();}
    }
}
