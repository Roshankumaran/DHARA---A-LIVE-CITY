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
        public void Select(int city)
        {
            bool changed=City!=city;Cancel();City=city;
            if(changed){Data=null;Status.RetrievedUtc=null;Status.Error=null;}
            job=StartCoroutine(Fetch());
        }
        IEnumerator Fetch()
        {
            int failures=0;
            while(true)
            {
                Status.Loading=true;UnityWebRequestAsyncOperation operation=null;
                try{active=UnityWebRequest.Get(RequestUrl(Location.All[City]));active.timeout=20;operation=active.SendWebRequest();}catch(Exception e){Status.Error=e.Message;}
                if(operation!=null)yield return operation;
                if(operation!=null&&active.result==UnityWebRequest.Result.Success)
                {try{Data=AirQualityData.Parse(active.downloadHandler.text);Status.RetrievedUtc=DateTime.UtcNow;Status.Error=null;failures=0;}catch(Exception e){Status.Error=e.Message;failures++;}}
                else{if(active!=null)Status.Error=active.error;failures++;}
                active?.Dispose();active=null;Status.Loading=false;
                yield return new WaitForSecondsRealtime(failures==0?300:Mathf.Min(300,15*Mathf.Pow(2,Mathf.Min(failures-1,5))));
            }
        }
        public float HazeDensity => HazeEnabled&&Data!=null&&Numbers.IsFinite(Data.Pm25)&&Status.Error==null&&!Status.Stale ? .0018f+(float)Numbers.Clamp(Data.Pm25.Value/100,0,1)*.0015f:.0018f;
        void Cancel(){if(job!=null)StopCoroutine(job);job=null;if(active!=null){active.Abort();active.Dispose();active=null;}Status.Loading=false;}
        void OnDestroy(){Cancel();}
    }
}
