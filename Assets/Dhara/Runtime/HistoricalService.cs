using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
namespace Dhara
{
    public sealed class HistoricalService : MonoBehaviour
    {
        public readonly List<WeatherData> Records=new List<WeatherData>();
        public readonly DataSourceStatus Status=new DataSourceStatus();
        public int City {get;private set;}=-1;
        public string Date {get;private set;}
        public Func<Location,string,string> RequestUrl=HistoricalWeather.Url;
        UnityWebRequest active;Coroutine job;
        public void Load(int city,string date)
        {
            Cancel();
            if(City==city&&Date==date&&Records.Count>0){Status.Error=null;return;}
            City=city;Date=date;Records.Clear();Status.RetrievedUtc=null;Status.Error=null;
            string url;try{url=RequestUrl(Location.All[city],date);}catch(Exception e){Status.Error=e.Message;return;}
            Status.Loading=true;job=StartCoroutine(Fetch(url));
        }
        IEnumerator Fetch(string url)
        {
            for(int attempt=0;attempt<2;attempt++)
            {
                active=UnityWebRequest.Get(url);active.timeout=20;yield return active.SendWebRequest();
                if(active.result==UnityWebRequest.Result.Success)
                {try{var parsed=HistoricalWeather.ParseValidated(active.downloadHandler.text,Date);Records.AddRange(parsed);Status.RetrievedUtc=DateTime.UtcNow;Status.Error=null;}catch(Exception e){Status.Error="Historical response: "+e.Message;}}
                else Status.Error=active.error;
                double retryDelay=ApiRetry.Delay(attempt+1,active.responseCode,active.GetResponseHeader("Retry-After"),DateTime.UtcNow);
                active.Dispose();active=null;if(Records.Count>0)break;
                if(attempt==0)yield return new WaitForSecondsRealtime((float)retryDelay);
            }
            Status.Loading=false;job=null;
        }
        public void Cancel(){if(job!=null)StopCoroutine(job);job=null;if(active!=null){active.Abort();active.Dispose();active=null;}Status.Loading=false;}
        void OnDestroy(){Cancel();}
    }
}
