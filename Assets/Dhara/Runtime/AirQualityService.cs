using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
namespace Dhara
{
    public sealed class AirQualityService : MonoBehaviour
    {
        public AirQualityData Data {get;private set;}
        public readonly DataSourceStatus Status=new DataSourceStatus();
        public bool HazeEnabled;
        public Func<Location,string> RequestUrl=AirQualityData.Url;
        public int City {get;private set;}=-1;
        Coroutine job;UnityWebRequest active;
        bool enforceRetry;
        public void Select(int city)
        {
            city=Mathf.Clamp(city,0,Location.All.Length-1);bool changed=City!=city;Cancel();City=city;
            if(changed){Data=null;Status.RetrievedUtc=null;Status.Error=null;}
            job=StartCoroutine(Fetch());
        }
        IEnumerator Fetch()
        {
            int failures=0;
            while(true)
            {
                if(enforceRetry&&Status.RetryUtc>DateTime.UtcNow)yield return new WaitForSecondsRealtime((float)(Status.RetryUtc.Value-DateTime.UtcNow).TotalSeconds);
                Status.Loading=true;UnityWebRequestAsyncOperation operation=null;
                try{active=UnityWebRequest.Get(RequestUrl(Location.All[City]));active.timeout=20;operation=active.SendWebRequest();}catch(Exception e){Status.Error=e.Message;}
                if(operation!=null)yield return operation;
                if(operation!=null&&active.result==UnityWebRequest.Result.Success)
                {try{var candidate=AirQualityData.Parse(active.downloadHandler.text);ApiValidation.Fresh(candidate.Time,DateTime.UtcNow,TimeSpan.FromHours(3));if(Data!=null&&ApiValidation.Time(candidate.Time)<ApiValidation.Time(Data.Time))throw new FormatException("Older air-quality response rejected");Data=candidate;Status.RetrievedUtc=DateTime.UtcNow;Status.Error=null;failures=0;}catch(Exception e){Status.Error=e.Message;failures++;}}
                else{if(active!=null)Status.Error="HTTP "+active.responseCode+" · "+active.error;failures++;}
                double delay=ApiRetry.Delay(failures,active?.responseCode??0,active?.GetResponseHeader("Retry-After"),DateTime.UtcNow);
                enforceRetry=active!=null&&(active.responseCode==429||active.responseCode==503&&!string.IsNullOrWhiteSpace(active.GetResponseHeader("Retry-After")));
                Status.RetryUtc=failures>0?DateTime.UtcNow.AddSeconds(delay):(DateTime?)null;
                active?.Dispose();active=null;Status.Loading=false;
                yield return new WaitForSecondsRealtime((float)delay);
            }
        }
        public float HazeDensity => HazeEnabled&&Data!=null&&Numbers.IsFinite(Data.Pm25)&&Status.Error==null&&!Status.Stale ? .0018f+(float)Numbers.Clamp(Data.Pm25.Value/100,0,1)*.0015f:.0018f;
        public void Cancel(){if(job!=null)StopCoroutine(job);job=null;if(active!=null){active.Abort();active.Dispose();active=null;}Status.Loading=false;}
        void OnDestroy(){Cancel();}
    }
}
