using System;
using System.Collections.Generic;

namespace Dhara
{
    public enum WorldMode { Live, Forecast, WhatIf }
    public static class Numbers
    {
        public static double Clamp(double value, double min, double max) => double.IsNaN(value) || double.IsInfinity(value) ? min : Math.Max(min, Math.Min(max, value));
    }
    [Serializable]
    public sealed class WeatherData
    {
        public string Time;
        public double? Temperature, RainMmPerHour, PrecipitationMm, WindKmh, WindDegrees, CloudPercent, HumidityPercent, SolarWm2;
        public WeatherData Copy() => (WeatherData)MemberwiseClone();
        public bool HasSimulationInputs => Temperature.HasValue && RainMmPerHour.HasValue && WindKmh.HasValue && CloudPercent.HasValue;
    }
    public sealed class EnvironmentState
    {
        public double WaterMm, Wetness, RainIntensity, WindIntensity, LightIntensity, SolarKw;
        public double ElapsedSimulatedSeconds;
    }
    public sealed class ScenarioOverrides
    {
        public double RainMmPerHour, WindKmh, Temperature, DrainageMmPerHour = 12;
        public WeatherData Apply(WeatherData baseline)
        {
            var data = (baseline ?? new WeatherData()).Copy();
            data.RainMmPerHour = RainMmPerHour; data.WindKmh = WindKmh; data.Temperature = Temperature;
            if (!data.CloudPercent.HasValue) data.CloudPercent = 50; // Explicit hypothetical baseline when offline.
            return data;
        }
    }
    public sealed class EnvironmentalEvent
    {
        public readonly DateTime Timestamp = DateTime.Now;
        public readonly string Message;
        public EnvironmentalEvent(string message) { Message = message; }
    }
    public static class WaterAccumulationModel
    {
        // Effective impervious-surface water depth, mm. Runoff coefficient 0.85;
        // drainage efficiency 0.8; drying 0.02 mm/h/degree above 5 C. Not a flood forecast.
        public const double MaximumMm = 150;
        public static double Step(double previous, double rain, double drainage, double temperature, double seconds)
        {
            double hours = Numbers.Clamp(seconds, 0, 3600) / 3600;
            double inflow = Numbers.Clamp(rain, 0, 200) * 0.85;
            double removal = Numbers.Clamp(drainage, 0, 100) * 0.8;
            double drying = Math.Max(0, Numbers.Clamp(temperature, -30, 60) - 5) * 0.02;
            return Numbers.Clamp(Numbers.Clamp(previous, 0, MaximumMm) + (inflow - removal - drying) * hours, 0, MaximumMm);
        }
    }
    public sealed class CausalEnvironmentEngine
    {
        public EnvironmentState State { get; private set; } = new EnvironmentState();
        public readonly List<EnvironmentalEvent> Events = new List<EnvironmentalEvent>();
        WeatherData previous;
        double previousDrainage = 12;
        public void Reset() { State = new EnvironmentState(); previous = null; }
        public void Record(string message)
        {
            Events.Insert(0, new EnvironmentalEvent(message));
            if (Events.Count > 30) Events.RemoveAt(Events.Count - 1);
        }
        public void Step(WeatherData input, double drainage, double seconds)
        {
            if (input == null || !input.HasSimulationInputs) return;
            double rain = Numbers.Clamp(input.RainMmPerHour.Value, 0, 200);
            double wind = Numbers.Clamp(input.WindKmh.Value, 0, 180);
            double cloud = Numbers.Clamp(input.CloudPercent.Value, 0, 100);
            if (previous != null)
            {
                if (Math.Abs(rain - previous.RainMmPerHour.Value) >= 0.5) { Record($"Rainfall {previous.RainMmPerHour:0.0} → {rain:0.0} mm/h; rain emission and wetness target changed."); previous.RainMmPerHour=rain; }
                if (Math.Abs(wind - previous.WindKmh.Value) >= 2) { Record($"Wind {previous.WindKmh:0} → {wind:0} km/h; vegetation sway and particle drift changed."); previous.WindKmh=wind; }
                if (Math.Abs(cloud - previous.CloudPercent.Value) >= 5) { Record($"Cloud cover {previous.CloudPercent:0} → {cloud:0}%; daylight target changed."); previous.CloudPercent=cloud; }
                if (Math.Abs(drainage - previousDrainage) >= 0.1) { Record($"Drainage {previousDrainage:0.0} → {drainage:0.0} mm/h; removal rate changed to {drainage * .8:0.0} mm/h."); previousDrainage=drainage; }
            }
            if (previous == null) { previous = input.Copy(); previousDrainage = drainage; }
            double dt = Numbers.Clamp(seconds, 0, 3600);
            State.WaterMm = WaterAccumulationModel.Step(State.WaterMm, rain, drainage, input.Temperature.Value, dt);
            double wetTarget = Math.Min(1, rain / 5 + State.WaterMm / 20);
            double blend = 1 - Math.Exp(-dt / 45);
            State.Wetness += (wetTarget - State.Wetness) * blend;
            State.RainIntensity = Math.Min(1, rain / 40);
            State.WindIntensity = Math.Min(1, wind / 80);
            State.LightIntensity = 1.35 - cloud / 100 * 0.9;
            State.SolarKw = Numbers.Clamp(input.SolarWm2 ?? 0, 0, 1500) * 96 * .2 / 1000;
            State.ElapsedSimulatedSeconds += dt;
        }
    }
    public sealed class SimulationState
    {
        public WorldMode Mode = WorldMode.Live;
        public WeatherData Live;
        public WeatherData Forecast;
        public ScenarioOverrides Scenario = new ScenarioOverrides();
        public double Drainage => Mode == WorldMode.WhatIf ? Scenario.DrainageMmPerHour : 12;
        public WeatherData Active => Mode == WorldMode.WhatIf ? Scenario.Apply(Live) : Mode == WorldMode.Forecast ? Forecast : Live;
        public void ResetScenario()
        {
            Scenario = new ScenarioOverrides { RainMmPerHour = Live?.RainMmPerHour ?? 0, WindKmh = Live?.WindKmh ?? 0, Temperature = Live?.Temperature ?? 25 };
        }
    }
}
