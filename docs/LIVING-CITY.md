# DHARA — A LIVING CITY

REAL DATA. CONNECTED SYSTEMS. LIVING ENVIRONMENTS.

## Preservation and foundation

This extends the existing Unity 6000.3.26f1 / URP 17.3 project. The main scene, namespaces, GUIDs, weather service, five locations, light uGUI dashboard, forecast, What-if controls, rain/wind/lighting/wetness effects, event log and prior Windows releases are preserved. No new Unity packages, website, backend or database were introduced for this upgrade.

The starting Git HEAD was `d194b8f532e6ef1432014eca8b121f34fcebc5a2`. The working tree already contained substantial changes; none were reset. The pre-upgrade checkpoint is `TestResults/Checkpoints/before-living-city-20261015-091528.zip`. Earlier verified builds remain in `Builds/Windows` and `Builds/Windows-Visual`.

## Implemented workflows

- **Resilience Studio:** freezes the active inputs and compares initially dry, 60-minute baseline/intervention cases with 6-second steps. Green roofs, permeable roads, extra drainage and garden storage are calculated. Results include peak surface runoff, maximum collection, retained/infiltrated/drained water and overflow, with absolute and meaningful percentage differences. Zero baselines display N/A. Storage bars use actual calculated values. Main files: `Core/DistrictHydrology.cs`, `Core/DistrictExperiment.cs`, `Runtime/ResilienceDashboard.cs`.
- **Impact trace:** seven inspectable stages contain actual input, computed volume, rule, timestamp and model residual. Selecting a stage highlights its modeled category; Show highlight in 3D exposes the world. Only the current trace is retained. Main file: `Core/ImpactTrace.cs`.
- **Spatial district / object explorer:** 12 roofs, two road catchments, four green strips and a shared collection object are associated with the model. Click a modeled surface to select it; click empty world space or Deselect to clear it. Object values are area-weighted model contributions. Normal, water, runoff, categories, connections and intervention overlays have legends. Cyan connections are schematic immediate routing, not engineered pipe geometry. Main file: `Runtime/SpatialDistrict.cs`.
- **Planner:** evaluates at most 36 cases (0/50/100% of roof/paving area constraints; zero/maximum drainage and garden options). Objective is peak runoff or maximum collection. Cases are inspectable in pages; best evaluated configuration can be opened in the same comparison. Search yields between cases and stops on panel close. Ties favor less treated area, then deterministic enumeration order. Main files: `Core/InterventionPlanner.cs`, `Runtime/AnalysisDashboard.cs`.
- **Time Machine / forecast impacts:** an ERA5 date retrieves up to 24 actual hourly records. Historical, present and future selections remain distinct. Previous/next/play/pause/resume and 1×/2×/4× playback select actual records. Each selected record is an independent dry-start one-hour constant-forcing case; playback does not claim continuous hydraulic replay. The selected source forecast or history can be used directly in Studio. Main files: `Core/HistoricalWeather.cs`, `Runtime/HistoricalService.cs`, `Runtime/TimelineDashboard.cs`.
- **Air quality:** PM2.5 / PM10 concentrations, individual missing values, units, validity and retrieval times, failed/stale states, source attribution and a user-enabled qualitative haze effect. Haze is live-mode only and disabled when data fails or is stale. Main files: `Core/AirQualityData.cs`, `Runtime/AirQualityService.cs`, `Runtime/DataDashboard.cs`.
- **Five-city comparison:** compares eight supported metrics using actual independently timestamped returns. Fresh comparison data is reused for five minutes; explicit Refresh all re-fetches. Selecting a city uses the existing main weather pipeline. Main file: `Runtime/CityComparisonService.cs`.
- **Scenario files / exports:** one versioned local snapshot slot, with source classification, timestamps, nullable input values, configuration, model version, assumptions and recorded outputs. Load validates the schema and ranges and recalculates outputs before accepting them. Loaded state is RECORDED. JSON and CSV comparison reports use selected calculated values and represent unavailable values explicitly. Files are in `Application.persistentDataPath/Scenarios`; the UI displays the exact path. The user explicitly saves/replaces `scenario.json` and exports/replaces `comparison.json` / `comparison.csv`. Main files: `Core/ScenarioReport.cs`, `Runtime/ScenarioDashboard.cs`.
- **Guided demonstration:** live-data path or an explicitly separate deterministic demonstration. Next runs the actual baseline, object selection, intervention and trace steps. Reset repeats the sequence; Stop returns to live. The deterministic case uses synthetic 50 mm/h rain, 30 km/h wind, 25 °C and 85% cloud, and labels its synthetic clock. Main file: `Runtime/DemonstrationDashboard.cs`.
- **Sensitivity:** varies only rainfall, nominal drainage, roof coverage or paving coverage while holding the other selected conditions fixed. Explicit minimum/maximum/step; at most 21 cases, all initially dry for one hour. Actual output tables are paginated. Main files: `Core/SensitivityAnalysis.cs`, `Runtime/SensitivityDashboard.cs`.

Runtime source paths above are relative to `Assets/Dhara/`. All workflows extend the existing CausalEnvironmentEngine / SimulationState. The district model runs on the existing fixed model clock. The primary storage card and basin use district collection volume. The previous effective-depth equation remains available in the legacy drainage comparison and for the existing wetness response; it is not interpreted as a measured district flood depth.

## Model and limitations

The conceptual catchment comprises 1,404 m² roofs (12 × 9 × 13 m), 2,128 m² road union (104 × 12 + 11 × 92 − 11 × 12), and 1,120 m² green strips (4 × 35 × 8 m). Other decorative land is excluded. Road object areas allocate the crossing once. Roof coping geometry includes a parapet overhang; modeled roof catchment area follows the underlying 9 × 13 m footprint.

Rain input is `rain_mm_per_hour × hours × area_m2 / 1000`. Green roofs hold up to 20 mm over their configured area. Permeable road fraction infiltrates up to 15 mm/h, green strips up to 8 mm/h, limited by available input. The remaining water routes immediately to a common collection node. A garden captures up to its configured 0–50 m³ capacity without release during a case. The collection outlet removes `0.8 × nominal_mm_per_hour × 4652 / 1000`, capped by available water; combined nominal and extra drainage is capped at 100 mm/h. Collection stores at most 150 m³; excess is explicitly overflow.

The conservation check is `rain − retained − infiltrated − drained − overflow − collection = residual`. Retained includes green-roof and garden water. Peak runoff is upstream of garden and collection drainage. This matters when comparing planner objectives. Coverage is uniformly distributed within a surface category. No soil saturation, groundwater, travel time, hydraulic head, evaporation in the district model, real pipe network, damage model or geographic city reconstruction is claimed. Green-roof and garden storage do not drain during an experiment. Time-to-clear is not implemented. Vegetation sway and green coverage appearance are supported; quantitative shade/cooling benefits are not modeled. Solar output remains an illustrative 96 m² × 20% efficiency calculation.

The legacy drainage-depth comparator uses a separate effective impervious-surface equation (0.85 runoff coefficient, 0.8 drainage efficiency, temperature-dependent drying, 150 mm bound), explicitly independent of the new area/volume model. It is retained for compatibility and does not provide the new district results.

## Data sources and access

- **Open-Meteo weather:** `https://api.open-meteo.com/v1/forecast`, existing current and hourly fields, explicit Celsius/km/h/mm and Asia/Kolkata. Current rain interval is converted to mm/h; hourly rain is the preceding-hour mm total interpreted over one hour. Current precipitation total stays separate. Source [documentation](https://open-meteo.com/en/docs). Five genuine weather/geocoding checks are recorded in `Verification/city-weather-results.json`.
- **Historical:** `https://archive-api.open-meteo.com/v1/archive`, `models=era5`, one selected date, the same hourly weather fields, Asia/Kolkata. UI supports 1940 through seven days before the request date to allow publication lag. [Official documentation](https://open-meteo.com/en/docs/historical-weather-api). A genuine 1 Oct 2026 Bengaluru probe returned 24 hourly records, correct units and IST offset.
- **Air quality:** `https://air-quality-api.open-meteo.com/v1/air-quality`, current `pm2_5,pm10`, `domains=cams_global`, Asia/Kolkata. Concentrations are μg/m³. [Official documentation and attribution](https://open-meteo.com/en/docs/air-quality-api). The verified Bengaluru probe returned PM2.5 9.0 and PM10 9.1 μg/m³ valid 15 Oct 2026 16:30 IST; these are probe evidence, not constants in the application. CAMS via Open-Meteo model estimates, not physical station readings. Haze maps concentration to a small capped visual effect and is not calibrated visibility.
- **IMD advisory blocker:** the documented district-warning endpoint `https://mausam.imd.gov.in/api/warnings_district_api.php?id=573` returned HTTP 401 on 15 Oct 2026. [IMD API document](https://mausam.imd.gov.in/imd_latest/contents/api.pdf). No official warning integration is claimed; the UI marks it unavailable. No credentials were embedded or invented. Authorized provider access and validated district mapping are required before implementing it.

Probe URLs, retrieval times and errors are in `Verification/city-provider-probes.json`. Weather retains its established 20-second timeout, cancellation and exponential retry. History has a 20-second timeout and one delayed retry; date/location changes cancel and discard obsolete requests. Air quality has separate timeout/cancellation/retry and retained-data labeling. Comparison fetches are sequential, cancelable and timestamped per city. Optional provider failures do not replace weather input. No requests run per simulation frame.

## Verification and delivery status

Baseline: 33 core checks and 26 Edit Mode tests passed before model changes. Incremental phase evidence is in `Verification/city-phase*-*.json`. Clean final batch test reports, visual inspection, performance measurements, executable and package verification are documented below once completed. Interactive test-runner interruptions are not counted as passing tests.

## Rebuild and test

From the project root with Unity CLI installed:

```powershell
pwsh -NoProfile -File Tools/Test-Core.ps1
pwsh -NoProfile -File Tools/Test-Source.ps1
pwsh -NoProfile -File Tools/Test-Weather.ps1
unity test . --editor-version 6000.3.26f1 --mode EditMode --report-format nunit --output TestResults/city-editmode.xml --timeout 600 --format json
unity test . --editor-version 6000.3.26f1 --mode PlayMode --report-format nunit --output TestResults/city-playmode.xml --timeout 600 --format json
```

Close the interactive Editor before batch test/build commands. To build from a connected stopped Editor, run `Dhara.Editor.DharaProject.BuildLivingCityWindows()` via the Unity CLI, or use DHARA > Build Living City Windows release. Packaging: `pwsh -NoProfile -File Tools/Package-Windows.ps1 -BuildDirectory Builds/Windows-LivingCity -ReportPath Verification/city-windows-package.json`.

## Competition alignment

Stage 1 is the existing genuine weather integration, now extended with verified ERA5 and CAMS data. Stage 2 connects active inputs to rain, wind, time/cloud lighting, wetness and area-based storage. Stage 3 adds connected runoff/retention/infiltration/drainage, causal trace, reproducible intervention comparisons, bounded search and sensitivity. Data integration is explicit about sources/timestamps; functionality is supported by executed checks; spatial overlays and inspection explain the visual model; innovation comes from these connected and reproducible workflows. This is an implementation mapping, not a claim of competition certification or calibrated engineering accuracy.

## Final verification evidence

Final source checks passed: **33 core checks**, syntax/reference/GUID/meta checks over **43 C# source files and five assembly definitions**, and **five genuine weather/geocoding API checks**. The final clean Edit Mode run passed **49 / 49** tests (`Verification/city-final-editmode.xml`, `city-final-batch-editmode.json`). The additional frozen-forecast test verifies that background live refresh does not relabel captured preview values.

The final clean Play Mode run passed **15 / 15** tests in **54.45 seconds** (`Verification/city-final-playmode.xml`, `city-final-batch-playmode.json`). The full suite covers the original nine application regressions plus spatial mapping/selection, exact forecast-to-impact transfer, actual five-city/cache behavior, repeatable guide/reset, real ERA5 selection with optional AQ failure isolation, and scenario file/planner controls. Network failures (including real timeout, HTTP/malformed responses and retry recovery) are tested in Unity, not injected into the release player.

All **42 layout inspections** (fourteen screens × 1280×800 / 1600×900 / 1920×1080) found zero active TMP text overflows and zero controls outside the rendered Canvas. Screens included Live, Forecast, What-if, Studio, assumptions, impact trace, evaluated planner, timeline, sources/advisory status, five-city comparison, scenario files, demonstration, calculated sensitivity and object inspection. See `Verification/city-layout-summary.json` and `Verification/Screenshots/LivingCity/`. Representative rendered screenshots were manually inspected; this does not imply that every possible content/error string or operating-system DPI setting was exhaustively inspected.

Warm Unity Editor benchmark: ten complete 36-case searches averaged **3.896 ms**, range **3.517–5.992 ms**, on this machine. There were 19 modeled objects, 18 schematic connection renderers, 988 city mesh renderers and a 12,000-particle maximum. Editor total allocated memory at that sample was 365,951,104 bytes, which includes Editor overhead. The Mono per-thread allocation counter returned zero despite constructing objects, so no zero-allocation claim is made. These measurements do not establish a universal frame rate or performance on a different laptop. Search is bounded and normally spread over frames; sensitivity is bounded at 21 cases; events retain at most 30 entries; only one tool panel and one Studio detail panel are retained.

Visual inspection corrected a short inspector button, separated comparison bars, added their scale/legend, bounded visible event-row summaries (the full bounded event messages remain in the engine), and made planner search progress explicit. Earlier interactive runner interruptions and one fixed initialization failure are preserved as historical phase evidence; clean batch results provide the final regression verdict.


## Windows delivery and extracted-player verification

- Executable: `C:\Users\Roshan\OneDrive\Desktop\DHARA_CODEX\Builds\Windows-LivingCity\DHARA.exe`.
- Complete distributable: `C:\Users\Roshan\OneDrive\Desktop\DHARA_CODEX\Builds\DHARA-LivingCity-Windows-x64-20261015-175316.zip` (40,052,729 bytes).
- ZIP SHA256: `F3EDB0AFBE4FEA725FC3C5B0F23E94C843A6CF2DDCCDB661ECC2BABD04AECFE2`.
- Fresh extraction tested: `C:\Users\Roshan\OneDrive\Desktop\DHARA_CODEX\TestResults\Extracted-LivingCity-20261015-175433\DHARA\DHARA.exe`.
- All **174** package file hashes and all **174** extracted file hashes matched. The built/extracted runtime DLL matched SHA256 `F1A62ED555331C053F699CC0CEE94C1B8CE7096C9751D2239EC7B05FD584B0EF`.
- Unity CLI build returned success; its full log is `Verification/city-windows-build.log`. Build used Unity 6000.3.26f1, Windows x64, Mono, URP and the existing main scene. Non-blocking licensing-client validation/token messages were logged, plus the expected warning that Pipeline control is disabled in the player because no runtime configuration is supplied. This is not a zero-warning build claim. The native player starts and operates normally without a Unity account dialog.

The extracted player was launched at 1600×900 and operated through native UI input. The exact product window title, genuine Bengaluru weather, Chennai selection and genuine response, City tools, clearly labeled deterministic guide, baseline calculation, roof inspection, intervention coverage and calculated results were observed. The guide produced baseline collection **150.00 m³** and intervention collection **99.46 m³**. Direct clicking on a roof also opened the actual object inspector.

The packaged player exported JSON and CSV. Their calculated final intervention storage agreed exactly at **99.46467333333133 m³**, and rounded to the visible **99.46 m³**. The export was correctly classified Demonstration, used the synthetic 2000-01-01T12:00 clock, and had no live retrieval timestamp. The snapshot saved and reloaded as RECORDED with recalculation. Stop/reset returned to live Chennai weather. No existing files were found in the three snapshot/report slots before this test; the explicit demonstration sample is left there for inspection.

Normal Alt-F4 shutdown was requested and confirmed by process termination and input/physics cleanup in the player log. No exception/shader/error matches were found in that log. This does not claim an independently observed process exit code. Native screenshots use the operating system's display scaling; the separate Editor Canvas audits establish the true three requested render resolutions.

Evidence: `Verification/city-windows-package.json`, `city-final-player-launch.json`, `city-packaged-player-smoke.json`, `city-final-player.log`, `city-standalone-export-check.json`, and the exported JSON/CSV copies. `city-delivery-integrity.json` confirms 104 source/assembly/meta hashes were unchanged since building and that earlier releases remain present. The main scene has no diff from the starting Git HEAD. No Git reset or replacement project was used.

To run: extract the **entire** ZIP and double-click `DHARA.exe`; keep its data folder, UnityPlayer.dll, Mono runtime and all companion files together. Unity is not needed for the player. Internet is needed for genuine live/forecast/history/air-quality data. What-if and the explicitly deterministic guide remain available without a successful provider response.

For reproducible visual inspection in a running main-scene Editor preview, use `pwsh -NoProfile -File Tools/Test-Layouts.ps1 -Width 1280 -Height 800` (and repeat for 1600×900 / 1920×1080). Supporting QA scripts are in `Tools/QA/`. The final source checkpoint is under `TestResults/Checkpoints/living-city-before-final-validation-*`; previous verified executables and ZIPs are preserved.

## Remaining work / practical boundaries

The requested official IMD advisory integration is **not completed** because the tested endpoint rejects anonymous access with HTTP 401. It remains visibly unavailable. Provider authorization and validated city/district mapping are the actual prerequisites; no fake advisory is substituted.

The other implemented workflows have the evidence described above. Practical boundaries remain explicit: independent-hour temporal playback instead of continuous hydraulic replay, a bounded grid search instead of global optimization, a single local scenario slot instead of a scenario library, one current comparison trace instead of a historical archive of every event, conceptual uniform surface coverage, and an uncalibrated catchment model. Time-to-clear, physical vegetation shade/cooling, real terrain flow and real building sensors are not claimed. No endurance run, exhaustive operating-system DPI matrix or separate low-end laptop benchmark was performed.
