> Latest API release: [weather/AQI validation, actual 275-case results and Windows delivery](API-ACCURACY.md).

> Latest exploration release: [standing water, city navigation, tour and actual 234-case results](EXPLORATION-RELEASE.md).

> Latest validation: [16 October 2026 results and final Windows release](VALIDATION-20261016.md). Historical evidence below is retained.

> Historical verification record. See [Living City implementation and current evidence](LIVING-CITY.md) for the current source and release.

# DHARA completion and verification — 14 October 2026

This is the earlier release record. The current light dashboard, intervention experiment and 15 October Windows delivery are documented in [REFERENCE-REDESIGN.md](REFERENCE-REDESIGN.md).

Project: `C:/Users/Roshan/OneDrive/Desktop/DHARA_CODEX`. Branch: `master`. HEAD: `d194b8f532e6ef1432014eca8b121f34fcebc5a2`; its parent is `4aaec67`. Subsequent runtime refinements and the current completion changes are uncommitted. Existing changes were preserved, not reset. Source/configuration checkpoint: `TestResults/Checkpoints/before-completion-pass.zip`.

## Acceptance status

The Windows application is built, packaged, and observed running from a freshly extracted ZIP. Required core functionality passed actual Unity tests. This is a verified demonstration prototype; it is not declared exhaustively production-ready. Release-player forced network-failure testing, low-end hardware benchmarks, full retry-backoff progression and extended endurance remain unverified. No current compilation, licensing, dependency or deployment blocker was observed.

Initial checklist: live data, forecast, What-if/reset, deterministic model, procedural effects and build automation were implemented; earlier runtime evidence covered 22 Edit Mode and five Play Mode tests. New gaps were HTTP/invalid/missing responses through UnityWebRequest, timeout, rapid switching/refresh, scene reload and delivery integrity. These now passed. No additional application-code defect was reproduced during this pass. Machine-readable checklist: `Verification/completion-checklist.json`.

## Inspection and changes

Inspected README, prior verification/setup/build instructions, version, manifest/lockfile, scenes/build settings, all 15 DHARA C# files, five assembly definitions, standalone scripts and Git state/history. Runtime responsibilities remain `DharaApplication` (ownership/startup), `WeatherService`/`WeatherDataStore` (requests/cache/status), `ProceduralCity` (world/effects), `CausalEnvironmentEngine`/`SimulationState`/`WaterAccumulationModel` (rules/modes/log), `EnvironmentalDashboard` (controls), and `OrbitCamera` (navigation). Existing `DharaProject.BuildWindows()` was used.

This completion pass modified:

- `Assets/Dhara/Tests/PlayMode/ApplicationTests.cs`: added real timeout/cleanup, loopback failure injection, rapid switching/refresh and scene-reload coverage. Eight tests now exist and ran. Loopback fixtures provide only explicit error/malformed/missing responses, never successful fabricated weather; they are excluded from release builds.
- `Tools/Test-Weather.ps1`: records HTTP status, parsing, retrieval/data timestamps, complete weather values, missing fields and coordinates in addition to its existing unit checks.
- `Tools/Package-Windows.ps1`: new repeatable AMD64 validation, staging, launch notes, ZIP creation, per-file hashes and ZIP-entry hash verification. It excludes DoNotShip debug artifacts and deletes no source or build files.
- README, this report and `Verification/`: current results and delivery evidence.

Earlier runtime improvements remain: overview bounds/aspect fitting; readable dashboard spacing/hierarchy; clearer rain and channels; mineral building palette, solar-cell seams, emissive lamps/windows and fuller canopies; smooth sky/fog/light transitions and readable night exposure; generated Volume cleanup; preview-scene test isolation; retained Resources directory to prevent OneDrive post-build cleanup failure. Files: `OrbitCamera.cs`, `EnvironmentalDashboard.cs`, `ProceduralCity.cs`, `ScenePreparationTests.cs`, `Assets/Resources/README.txt`, `Tools/Test-Source.ps1`. The saved main scene/GUID and user assets/configuration were preserved. Unity-generated upgrades/imports remain in the working tree, including TMP and URP settings; they were not discarded or silently represented as solely agent-authored.

## Environment and actual execution

Unity CLI is current at `1.0.0-beta.13`. Connected Editor is **6000.3.26f1**, with resolved URP **17.3.0**, Input System **1.20.1**, uGUI **2.0.0**, Test Framework **1.6.0** and official Pipeline **0.8.0-exp.1**. Windows standalone support exists. Editor inspection confirmed `PC_RPAsset`, LiberationSans SDF, enabled main scene `Assets/Scenes/Dhara.unity`, target `StandaloneWindows64` and Mono2x backend. Actual Test Runner/build execution confirms licensing allowed execution, rather than assuming account sign-in means a valid license. Evidence: `unity-environment.json`, `unity-compilation.json`.

Executed in this pass:

- **33/33 standalone core checks passed** through PowerShell Add-Type against production parser/store/engine. `core-results.json`.
- **5/5 genuine weather HTTPS checks passed**, HTTP 200, valid units/coordinates, no missing requested current fields. All returned 48 hourly / 26 future records at retrieval. `weather-results.json`.
- **15 C# files / five assembly definitions passed source checks** (syntax, metadata, GUID references and visible strings); this is separate from Unity compilation. `source-results.json`.
- **22/22 actual Unity Edit Mode tests passed**, approximately 0.38 seconds. `unity-editmode.json`.
- **8/8 actual Unity Play Mode tests passed**, approximately 44.93 seconds. `unity-playmode.json`.
- Unity compilation completed with zero errors/warnings. Final cached Windows rebuild succeeded in approximately 6.41 seconds. Final Console: **zero errors, one warning**. `windows-build-job.json`, `unity-console-final.json`, `windows-build-console.json`.

All evidence filenames above are under `Verification/`. No authored-only tests are counted as executed.

Play Mode coverage: scene/bootstrap/weather initialization; supported shaders and complete overview framing; actual sliders and modes; particles, canopy motion/direction, asphalt smoothness, basin footprint, cloud-driven light/sky; low/high drainage comparisons, drying, reset and live restoration; genuine requests from all five location buttons and identity of the object delivered to live simulation state; exact forecast timestamp/object; actual failed `.invalid` request; retained data/stale label and first automatic 15-second retry recovery; HTTP 503, malformed JSON and missing current data over real loopback HTTP; actual 20-second timeout (observed 20.07 seconds); cancelling a pending request on destruction; rapid location changes and refresh; main-scene reload and removal of old camera/world. Rejected responses never became successful live data.

Earlier corrections included an Edit Mode fixture failing when creating an additive untitled scene; preview-scene isolation fixed it before successful runs. The first Windows build's nonfatal OneDrive Resources cleanup IOException was fixed by retaining that directory; subsequent builds completed without it. Initial diagnostics remain in `windows-build-initial-diagnostics.json`. A previous URP Core DebugOccluder shader warning did not recur in the final cached rebuild. The sole final warning says Pipeline has no runtime config and is disabled in release players; no release control server is configured.

## Genuine API observations

Production endpoint: `https://api.open-meteo.com/v1/forecast`, requesting current/hourly temperature, humidity, precipitation, rain, wind speed/direction, cloud cover and shortwave radiation; Celsius, km/h, mm and Asia/Kolkata. Geocoding checks use `https://geocoding-api.open-meteo.com/v1/search`. Retrieval and model-valid timestamps are separate.

At model-valid **2026-10-14 21:45 IST**, all five returned HTTP 200 and parsed successfully:

- Bengaluru (12.97194, 77.59369): 22.1°C, precipitation 0 mm, rain 0 mm/h, wind 11.5 km/h from 73°, cloud 100%.
- Chennai (13.08784, 80.27847): 26.4°C, precipitation 0.6 mm, rain 0.4 mm/h, wind 3.1 km/h from 350°, cloud 91%.
- Mumbai (19.07283, 72.88261): 31.6°C, precipitation 0 mm, rain 0 mm/h, wind 9.4 km/h from 2°, cloud 0%.
- Delhi (28.62137, 77.2148): 26.8°C, precipitation 0 mm, rain 0 mm/h, wind 4.7 km/h from 97°, cloud 0%.
- Hyderabad (17.38405, 78.45636): 27.9°C, precipitation 0 mm, rain 0 mm/h, wind 7.7 km/h from 49°, cloud 95%.

Per-city UTC retrieval timestamps, field units and coordinates are preserved in `weather-results.json`. Direct UnityWebRequest city observations are in `unity-live-locations.txt`. These are Open-Meteo model estimates, not physical-sensor measurements. Current rainfall is normalized from the returned interval; precipitation total remains separate.

## Windows delivery and manual evidence

Release executable: `C:/Users/Roshan/OneDrive/Desktop/DHARA_CODEX/Builds/Windows/DHARA.exe`.

Delivery ZIP: `Builds/DHARA-Windows-x64-20261014-220013.zip` (approximately 40 MB). `windows-package.json` records SHA256, AMD64 PE-header verification and **174/174 ZIP file hashes verified**. Includes executable, data, UnityPlayer, Mono runtime, companions, launch notes and checksum manifest; excludes Burst DoNotShip artifacts. No installed Unity Editor is needed to run it.

The ZIP was extracted to a fresh `TestResults/Extracted-*` directory and that executable was launched at 1600×900. The district and dashboard rendered. Bengaluru retrieval succeeded; clicking Chennai showed loading/unavailable/paused state, then genuine Chennai 22:00 IST conditions (26.3°C, 0.4 mm/h, 2.9 km/h from 7°, cloud 94%). Light rain was visible and wetness reached 8%. What-if mode opened with the correct baseline and hypothetical label. Screenshot: `Screenshots/packaged-chennai-live.png`.

Prior direct player interaction also verified all four scenario sliders, visible heavy rain and wind drift, wetness/storage response, forecast step 14 with its real timestamp/solar values, return to live and normal Alt+F4 exit. Prior screenshots `standalone-live.png`, `standalone-what-if.png`, `standalone-forecast.png` and `standalone-final.png` remain valid evidence for that build. The final release has the same application runtime assembly hash; this pass changed tests/tools/docs. The packaged player's log was inspected for fatal startup exceptions: none observed. The device reports a D3D12 debug-info-queue query failure but rendered successfully. Forced offline/timeout/error behavior was exercised in Unity tests, not manually forced in this release player.

## Security, limits and reproduction

Targeted scan reviewed 45 application/tool/config files with zero credential-pattern hits. Manual review found no application secret, API-key header, destructive deletion or unbounded busy retry; public forecast needs no key. Timeout/backoff and cancellation were actually tested. Packaging copies data without deleting originals. Evidence/scope: `security-review.json`; this is not a comprehensive dependency audit. No credentials are included in delivery notes.

Run: double-click `Builds/Windows/DHARA.exe`, or extract the complete ZIP and double-click its `DHARA/DHARA.exe`. Keep companion files together. Editor: open the existing project in 6000.3.26f1 and run `Assets/Scenes/Dhara.unity`. Rebuild through **DHARA > Build Windows application**. Package with `.\Tools\Package-Windows.ps1`. Test commands and controls are in README.

Limits: water is aggregate effective storage, not terrain/hydraulic flood prediction; solar is illustrative radiation×96 m²×20% with omitted losses; the district is generic. Forecast applies one returned hour as constant input, not intervening-hour integration. Missing inputs pause live integration; offline What-if explicitly assumes 50% cloud and leaves missing solar/humidity/direction unavailable. No fake weather fallback or inference feature was added. Full five-minute-refresh soak/backoff sequence, long endurance, broad resolutions/hardware and student-laptop performance remain unverified. Earlier RTX 4070 observation near 61 FPS is not a general hardware claim. Historical replay, air quality and scenario comparison remain unimplemented and optional.

