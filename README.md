# DHARA — A LIVING CITY

REAL DATA. CONNECTED SYSTEMS. LIVING ENVIRONMENTS.

A Unity environmental simulation for **Innovation Mission 4 — Procedural Environments: Real-Time API Integration**. The existing Unity 6.3 LTS / URP project is extended with connected surface-water modeling, reproducible urban interventions, and transparent data provenance.

## Browser dashboard and Vercel

The web companion is in [Web](Web/README.md). It displays real weather/AQI for five cities, hourly forecasts and downloadable CSVs. Run `cd Web`, `npm ci --ignore-scripts`, then `npm run dev`. The current local preview is http://127.0.0.1:5174.

For Vercel, import this GitHub repository with **Root Directory `Web`**, **Framework `Other`**, **Node.js `24.x`**, **Build Command `npm run build`** and **Output Directory `dist`**. No API keys or environment variables are needed. See the [complete deployment and Hostinger domain instructions](Web/README.md#vercel-deployment).

Unity works locally and remains the interactive 3D client. The browser companion is suitable for Vercel; the Windows executable is distributed through [GitHub Releases](https://github.com/Roshankumaran/DHARA---A-LIVE-CITY/releases/tag/dhara-api-20261016), rather than executed on Vercel. Unity Web Build Support is absent, so browser-based 3D playback is not claimed.

Latest logo update: [official artwork integration and verification](docs/BRANDING.md). The supplied logo appears in the Unity header and Windows icons, plus the web header/favicon. Run `Builds/Windows-Branded/DHARA.exe`, or download the [branded Windows release](https://github.com/Roshankumaran/DHARA---A-LIVE-CITY/releases/tag/dhara-branding-20261016). Earlier builds remain preserved.

Latest API release: [validated weather, AQI, pollutants and 275 passing Unity tests](docs/API-ACCURACY.md). Run `Builds/Windows-API/DHARA.exe`, or extract `Builds/DHARA-LivingCity-Windows-x64-20261016-040756.zip`. Previous releases remain preserved.

Previous exploration release: [city exploration, standing-water and 234-case verification report](docs/EXPLORATION-RELEASE.md). The current executable is `Builds/Windows-Exploration/DHARA.exe`; earlier releases remain preserved.

Read the [implementation, verification and model report](docs/LIVING-CITY.md) for feature details, actual test evidence, provider access limits and delivery paths. Earlier work is preserved in the [historical redesign report](docs/REFERENCE-REDESIGN.md).

## Run the project

Open this folder in **Unity 6000.3.26f1** with Windows desktop support. Let package imports finish, open `Assets/Scenes/Dhara.unity`, then press Play. The city and dashboard are generated at runtime. If setup is missing, use **DHARA > Install or repair dependencies**, then **DHARA > Prepare main scene**. Preparation preserves the existing scene and GUID references.

The application uses URP 17.3.0, Input System 1.20.1, uGUI 2.0.0 and Test Framework 1.6.0. No additional package was required for the Living City upgrade. Source namespaces and the main scene remain intact.

## Explore

- Select Bengaluru, Chennai, Mumbai, Delhi or Hyderabad. Current and forecast weather comes from Open-Meteo; missing, failed and stale data is labeled.
- **Live World / Forecast Preview / What-if Lab** retain their existing functions. In overview, right-drag orbits, middle-drag / WASD pans and wheel zooms. Walk enters street exploration: WASD/arrows, Shift faster, right-drag 360-degree look; click city to resume input, Escape releases it, 1 returns to overview and Home resets. Tour traverses the district with pause/resume/restart/exit controls.
- **Resilience Studio** freezes the selected forcing and compares a dry-start hour with green-roof coverage, permeable road coverage, additional drainage and garden storage. Reset clears interventions. Assumptions explains every supported rule. World coverage colors identify the affected categories.
- **City tools** opens impact trace, deterministic planner, historical/live/future timeline, forecast-to-impact comparison, air quality/advisory status, five-city weather comparison, scenario save/export, guided demonstration and sensitivity analysis.
- Click a modeled roof, road, green strip or collection area to inspect its actual assigned area, parameters, connection and computed state. Values are conceptual model contributions using shared weather, not object sensors.
- Time Machine playback evaluates independent one-hour cases from real source timestamps. It does not claim continuous hydraulic replay. Frozen temporal inputs remain independent of background live refreshes.
- Scenario files are written only when requested, to the local folder shown in the UI. One versioned snapshot slot (`scenario.json`) and JSON/CSV report slots are supported. Saving/exporting replaces those named files. Loading validates and recalculates a snapshot, marks it RECORDED, and retains its source provenance.
- The deterministic demonstration is explicitly synthetic and separate from live weather. Start, Next, Reset and Stop exercise real calculations and selection actions.

## Model

The primary water display and basin use a **4,652 m² conceptual catchment**: 1,404 m² roofs, 2,128 m² road union and 1,120 m² green strips. Rain depth × area / 1000 gives m³. Green roofs retain up to 20 mm; permeable paving infiltrates up to 15 mm/h; green strips infiltrate up to 8 mm/h. Runoff routes immediately to a shared collection node. Garden storage retains up to its configured capacity. Drainage removes 0.8 × nominal mm/h × catchment area / 1000, limited by available water. Collection is bounded at 150 m³ and excess is reported as overflow.

The Studio, planner, sensitivity, reports and live district use these same rules. Comparisons start dry for 60 minutes with fixed six-second steps. The runtime model clock advances 60 simulated seconds per real second. The retained legacy drainage-depth comparator uses its separately disclosed effective-surface equation; it does not supply the new district volume results.

This is a simplified, uncalibrated model with uniform category coverage, no hydraulic network, soil saturation, travel time or groundwater model. It does not predict real street flood depths, infrastructure failure or damage. The procedural district is generic. Solar output and atmospheric effects remain illustrative.

## Sources

Weather and forecast: [Open-Meteo](https://open-meteo.com/en/docs), CC BY 4.0. ERA5 history: [Open-Meteo Historical Weather API](https://open-meteo.com/en/docs/historical-weather-api). PM2.5 / PM10: [CAMS global via Open-Meteo](https://open-meteo.com/en/docs/air-quality-api), with source attribution in the application. Values are model estimates, not physical sensor readings. Units, source times and retrieval times are retained separately.

**IMD weather advisories remain unavailable:** the documented district-warning endpoint returned HTTP 401 during verification. No warnings or credentials are fabricated. Details and actual probe evidence are in the implementation report.

## Test and build

Use PowerShell 7 from this directory. Close the interactive Editor before batch tests/builds.

```powershell
pwsh -NoProfile -File Tools/Test-Core.ps1
pwsh -NoProfile -File Tools/Test-Source.ps1
pwsh -NoProfile -File Tools/Test-Weather.ps1
unity test . --editor-version 6000.3.26f1 --mode EditMode --report-format nunit --output TestResults/city-editmode.xml --timeout 600 --format json
unity test . --editor-version 6000.3.26f1 --mode PlayMode --report-format nunit --output TestResults/city-playmode.xml --timeout 600 --format json
unity build . --editor-version 6000.3.26f1 --target StandaloneWindows64 --execute-method Dhara.Editor.DharaProject.BuildLivingCityWindows --format json
pwsh -NoProfile -File Tools/Package-Windows.ps1 -BuildDirectory Builds/Windows-LivingCity -ReportPath Verification/city-windows-package.json
```

The Living City build target is `Builds/Windows-LivingCity/DHARA.exe`. Keep all companion files together. Older verified releases in `Builds/Windows` and `Builds/Windows-Visual` are preserved. Final package paths, hashes, executed test counts and extracted-player verification are recorded in `docs/LIVING-CITY.md` and `Verification/city-*`.

## Download the Windows app

The verified Windows build is included in [the downloadable ZIP](Builds/DHARA-LivingCity-Windows-x64-20261016-034523.zip). Download the ZIP, extract the entire folder, and run DHARA.exe. Keep its accompanying data and runtime folders together.


Earlier animation release: see [implementation and verification report](docs/ENVIRONMENTAL-ANIMATION.md). Camera controls include Reset, Street and Tour buttons.

