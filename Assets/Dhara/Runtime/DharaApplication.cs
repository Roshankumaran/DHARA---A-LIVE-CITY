using UnityEngine;

namespace Dhara
{
    public sealed class DharaApplication : MonoBehaviour
    {
        public Material SurfaceTemplate, ParticleTemplate;
        public CitySettings City = new CitySettings();
        public readonly SimulationState Simulation = new SimulationState();
        public readonly CausalEnvironmentEngine Engine = new CausalEnvironmentEngine();
        public WeatherService Weather { get; private set; }
        public ProceduralCity World { get; private set; }
        public SpatialDistrict Spatial { get; private set; }
        public HistoricalService History {get;private set;}
        public AirQualityService AirQuality {get;private set;}
        public CityComparisonService CityComparison {get;private set;}
        public bool TemporalPreviewFrozen {get;private set;}
        public string RecordedRetrievedUtc;
        public string RecordedOriginClassification;
        public string ActiveRetrievedUtc => Simulation.FrozenPreview!=null?Simulation.FrozenPreviewRetrievedUtc:Simulation.Mode==WorldMode.Historical?History.Status.RetrievedUtc?.ToString("o"):Simulation.Mode==WorldMode.Demonstration?null:Simulation.Mode==WorldMode.Recorded?RecordedRetrievedUtc:Weather.Status.RetrievedUtc?.ToString("o");
        public const float SimulatedSecondsPerSecond = 60;
        public event System.Action ControlsChanged;
        float accumulated;
        int waterBand;
        void Start()
        {
            Application.targetFrameRate=60; Application.runInBackground=true;
            World=new GameObject("Procedural urban district").AddComponent<ProceduralCity>();
            World.transform.SetParent(transform,false);
            World.SurfaceTemplate=SurfaceTemplate; World.ParticleTemplate=ParticleTemplate; World.Settings=City; World.Generate();
            Spatial=World.gameObject.AddComponent<SpatialDistrict>();Spatial.Initialize(this);
            // Match the central viewport between the persistent monitoring panels.
            World.ViewCamera.rect=EnvironmentalDashboard.WorldViewport;
            Weather=gameObject.AddComponent<WeatherService>(); Weather.Updated+=ReceiveWeather;
            History=gameObject.AddComponent<HistoricalService>();
            AirQuality=gameObject.AddComponent<AirQualityService>();AirQuality.Select(0);
            CityComparison=gameObject.AddComponent<CityComparisonService>();CityComparison.Initialize(Weather);
            var dashboard=gameObject.AddComponent<EnvironmentalDashboard>(); dashboard.Initialize(this);
            Weather.Select(0);
        }
        void ReceiveWeather()
        {
            if(Simulation.UpdateWeather(Weather.Current,Weather.Forecast))
            {
                Engine.Reset(); accumulated=0; waterBand=0;
                Engine.Record("Selected forecast hour is no longer available; preview selection and simulated storage reset.");
            }
            if(Weather.Status.Error!=null) Engine.Record("Weather request failed; retained data is labelled and simulation remains available. " + Weather.Status.Error);
            else if(Weather.Current!=null) Engine.Record("Weather model updated for " + Location.All[Weather.Selected].Name + " · valid " + Weather.Current.Time + " IST.");
            if(TemporalPreviewFrozen&&Simulation.Mode==WorldMode.Live)EvaluateActiveHour();
        }
        public void SelectLocation(int index)
        {
            History.Cancel();Simulation.Historical=null;
            index=Mathf.Clamp(index,0,Location.All.Length-1);
            AirQuality.Select(index);
            SetMode(WorldMode.Live); Simulation.Live=null; Simulation.Forecast=null; Weather.Select(index); Engine.Reset(); waterBand=0;
            Engine.Record("Location changed to " + Location.All[index].Name + "; awaiting weather. Water storage reset.");
        }
        public void SetMode(WorldMode mode)
        {
            TemporalPreviewFrozen=false;
            if(Spatial!=null){Spatial.ShowComparison(null);Spatial.Deselect();}
            Simulation.ChangeMode(mode,Weather.Forecast);
            Engine.Reset(); accumulated=0; waterBand=0;
            Engine.Record("Mode changed to " + (mode==WorldMode.WhatIf ? "WHAT-IF" : mode==WorldMode.Forecast ? "FORECAST PREVIEW" : "LIVE CONDITIONS") + "; simulated storage reset.");
            ApplyInputs(); ControlsChanged?.Invoke();
        }
        public void SelectForecast(int index)
        {
            if(index<0 || index>=Weather.Forecast.Count) return;
            TemporalPreviewFrozen=false;Simulation.FrozenPreview=null;Simulation.FrozenPreviewRetrievedUtc=null;
            Simulation.Forecast=Weather.Forecast[index]; Engine.Reset(); waterBand=0;
            Engine.Record("Forecast selected: " + Simulation.Forecast.Time + " IST; storage reset for this preview."); ApplyInputs();
        }
        public void SelectTemporal(WeatherData record,bool historical)
        {
            SetMode(historical?WorldMode.Historical:WorldMode.Forecast);
            if(historical)Simulation.Historical=record;else Simulation.Forecast=record;
            Simulation.FrozenPreview=record.Copy();Simulation.FrozenPreviewRetrievedUtc=historical?History.Status.RetrievedUtc?.ToString("o"):Weather.Status.RetrievedUtc?.ToString("o");
            Engine.Reset();for(int i=0;i<600;i++)Engine.Step(record,Simulation.Drainage,6);
            TemporalPreviewFrozen=true;ApplyInputs();ControlsChanged?.Invoke();
        }
        public void EvaluateActiveHour()
        {var input=Simulation.Active;Engine.Reset();for(int i=0;i<600;i++)Engine.Step(input,Simulation.Drainage,6);TemporalPreviewFrozen=true;}
        public void ResetScenario()
        {
            TemporalPreviewFrozen=false;
            Simulation.ResetScenario(); Engine.Reset(); accumulated=0; waterBand=0;
            Engine.Record("Scenario reset to available live inputs and 12 mm/h drainage; storage reset."); ApplyInputs(); ControlsChanged?.Invoke();
        }
        public void ApplyInputs() => Engine.Step(Simulation.Active,Simulation.Drainage,0);
        void Update()
        {
            accumulated+=Time.unscaledDeltaTime;
            // Fixed simulation steps: render frame rate does not change model integration.
            int iterations=0;
            while(accumulated>=.1f && iterations++<20)
            {
                accumulated-=.1f;
                if(!TemporalPreviewFrozen)Engine.Step(Simulation.Active,Simulation.Drainage,6);
            }
            if(iterations>=20) accumulated=0; // Suspend/resume never creates an unbounded catch-up loop.
            int band=(int)(Engine.State.CollectionM3/5);
            if(band!=waterBand) { Engine.Record($"District collection {Engine.State.CollectionM3:0.0} m³ · {Engine.State.ElapsedSimulatedSeconds/60:0.0} model min."); waterBand=band; }
            World.Render(Engine.State,Simulation.Active,Time.unscaledDeltaTime);
            RenderSettings.fogDensity=Simulation.Mode==WorldMode.Live?AirQuality.HazeDensity:.0018f;
        }
        void OnDestroy() { if(Weather!=null) Weather.Updated-=ReceiveWeather; }
    }
}
