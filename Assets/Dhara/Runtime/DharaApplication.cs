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
        public const float SimulatedSecondsPerSecond = 60;
        float accumulated;
        int waterBand;
        void Start()
        {
            Application.targetFrameRate=60; Application.runInBackground=true;
            World=new GameObject("Procedural urban district").AddComponent<ProceduralCity>();
            World.SurfaceTemplate=SurfaceTemplate; World.ParticleTemplate=ParticleTemplate; World.Settings=City; World.Generate();
            // Match the central viewport between the persistent monitoring panels.
            World.ViewCamera.rect=new Rect(.19f,.25f,.59f,.65f);
            Weather=gameObject.AddComponent<WeatherService>(); Weather.Updated+=ReceiveWeather;
            var dashboard=gameObject.AddComponent<EnvironmentalDashboard>(); dashboard.Initialize(this);
            Weather.Select(0);
        }
        void ReceiveWeather()
        {
            Simulation.Live=Weather.Current;
            if(Simulation.Mode==WorldMode.Forecast)
                Simulation.Forecast=Weather.Forecast.Count>0 ? Weather.Forecast[0] : null;
            if(Weather.Status.Error!=null) Engine.Record("Weather request failed; retained data is labelled and simulation remains available. " + Weather.Status.Error);
            else if(Weather.Current!=null) Engine.Record("Weather model updated for " + Location.All[Weather.Selected].Name + " · valid " + Weather.Current.Time + " IST.");
        }
        public void SelectLocation(int index)
        {
            SetMode(WorldMode.Live); Simulation.Live=null; Simulation.Forecast=null; Weather.Select(index); Engine.Reset(); waterBand=0;
            Engine.Record("Location changed to " + Location.All[index].Name + "; awaiting weather. Water storage reset.");
        }
        public void SetMode(WorldMode mode)
        {
            if(mode==WorldMode.WhatIf) Simulation.ResetScenario();
            if(mode==WorldMode.Forecast) Simulation.Forecast=Weather.Forecast.Count>0 ? Weather.Forecast[0] : null;
            Simulation.Mode=mode; Engine.Reset(); accumulated=0; waterBand=0;
            Engine.Record("Mode changed to " + (mode==WorldMode.WhatIf ? "WHAT-IF" : mode==WorldMode.Forecast ? "FORECAST PREVIEW" : "LIVE CONDITIONS") + "; simulated storage reset.");
            ApplyInputs();
        }
        public void SelectForecast(int index)
        {
            if(index<0 || index>=Weather.Forecast.Count) return;
            Simulation.Forecast=Weather.Forecast[index]; Engine.Reset(); waterBand=0;
            Engine.Record("Forecast selected: " + Simulation.Forecast.Time + " IST; storage reset for this preview."); ApplyInputs();
        }
        public void ResetScenario()
        {
            Simulation.ResetScenario(); Engine.Reset(); waterBand=0;
            Engine.Record("Scenario reset to available live inputs and 12 mm/h drainage; storage reset."); ApplyInputs();
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
                Engine.Step(Simulation.Active,Simulation.Drainage,6);
            }
            if(iterations>=20) accumulated=0; // Suspend/resume never creates an unbounded catch-up loop.
            int band=(int)(Engine.State.WaterMm/5);
            if(band!=waterBand) { Engine.Record($"Simulated storage {Engine.State.WaterMm:0.0} mm after {Engine.State.ElapsedSimulatedSeconds/60:0.0} model minutes; rainfall, drainage and drying applied."); waterBand=band; }
            World.Render(Engine.State,Simulation.Active,Time.unscaledDeltaTime);
        }
        void OnDestroy() { if(Weather!=null) Weather.Updated-=ReceiveWeather; }
    }
}
