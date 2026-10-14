# DHARA — A Living World

A Unity environmental simulation prototype for Innovation Mission 4: Procedural Environments — Real-Time API Integration.

**Status:** the existing implementation has been inspected and corrected without replacing its scene, assets, GUIDs, or package configuration. The production C# core passed **33 standalone checks**; real Open-Meteo requests, parsing, response units, coordinates, and future-forecast filtering passed for all five locations. Unity compilation, Play Mode, visual quality, and Windows deployment remain **unverified** because no Editor is installed. No Windows executable has been generated. See [verification details](docs/VERIFICATION.md).

## Open and run

1. Install **Unity 6.3 LTS** using Unity Hub; the project records `6000.3.26f1`. Sign in and activate an appropriate Unity license. Include Windows desktop support. A Mono Windows build avoids an IL2CPP toolchain requirement.
2. Add this folder as a project in Hub and open it. Keep internet access available during the first package import.
3. `DharaDependencies` resolves required packages through Unity's Package Manager API. Once successful, it adds `DHARA_READY` to Standalone scripting defines. Runtime/editor/test assemblies are gated until dependencies resolve.
4. First-open preparation imports the official TextMeshPro essentials, assigns the URP asset, prepares `Assets/Scenes/Dhara.unity`, and configures Windows build settings. Wait for package/font imports and script recompilation to finish. A successful preparation writes `Assets/Dhara/SetupComplete.txt`.
5. If preparation reports an error, resolve it in the Console and choose **DHARA > Install or repair dependencies**, then **DHARA > Prepare main scene**. TMP essentials can also be imported through **Window > TextMeshPro > Import TMP Essential Resources**. Preparation preserves the existing scene and bootstrap configuration, fills missing material references, and keeps other build scenes. A new bootstrap or scene is created only if it is missing. Multiple bootstraps are reported as an error instead of being deleted.
6. Open `Assets/Scenes/Dhara.unity` and press **Play**. The bootstrap component generates the district, camera, effects, Canvas dashboard, and weather service at runtime. The 3D world is therefore visible in Play Mode, not in the saved scene hierarchy before Play.

Use a landscape Game view, preferably **1600 × 1000**. Package/setup automation is provided but could not be executed here; address any Console compilation errors before continuing.

## Use the simulation

- Select **Bengaluru, Chennai, Mumbai, Delhi, or Hyderabad** in the header. Changing location clears the previous location's data and simulated storage.
- **LIVE WORLD** uses current weather-model estimates. The dashboard reports source, valid time in IST, retrieval time in UTC, missing fields, request failures, and stale data after ten minutes, including during refresh. These are model estimates rather than physical-sensor readings. Requests refresh every five minutes, time out after 20 seconds, and retry after 15/30/60/120/240/300 seconds on failure. **Refresh weather** retries immediately while retaining the current location's last successful data.
- **WHAT-IF LAB** changes rainfall, wind speed, temperature, and drainage without extra network requests. Inputs are hypothetical. Other available weather values remain from the latest live baseline. Offline scenarios explicitly assume 50% cloud cover and leave unavailable solar radiation, humidity, and wind direction unreported.
- **Reset scenario** restores available live rainfall/wind/temperature and 12 mm/h nominal drainage, then clears simulated storage. **Return to live** clears hypothetical overrides and restores current model inputs. Location changes also switch the visible controls back to live mode. Each mode change starts water storage at zero, so scenario water cannot leak into live output.
- **FORECAST PREVIEW** selects actual returned hourly timestamps. Hour offset zero is the first available future hourly value; the selected valid time is displayed. Refresh preserves the selected timestamp while it remains available. If that hour expires, the preview advances to the first available hour and resets storage with a log entry. Each manual selection also resets storage and applies that hour's weather as a constant input. This previews conditions at a timestamp; it does not integrate the intervening forecast hours.
- Right mouse drag orbits, middle drag or WASD/arrows pans, wheel zooms, and **Home** or **Reset camera** restores the overview. Camera mouse navigation yields to dashboard controls.

## Environment and model

The seeded procedural district includes twelve mineral-facade buildings, road markings and crossings, pavements, trees, open stormwater channels, grates, streetlights, benches, rooftop service housings, and sixteen 2 × 3 m solar panels. City seed, height range, tree density, and placement jitter are Inspector settings on the bootstrap component. This is a generic district, not a reconstruction of the selected city.

Weather retrieval, typed data, deterministic calculations, and visual rendering are separate components. Rain drives particle emission, surface wetness, and effective water storage. Wind drives canopy sway and particle drift using meteorological direction. Cloud cover changes directional lighting. Available shortwave radiation drives an illustrative solar output using **96 m² × 20% efficiency**.

Water depth is an aggregate **effective surface-storage depth in millimetres**, bounded to 0–150 mm. Per model hour:

`storage change = 0.85 × rainfall − 0.8 × nominal drainage − max(0, temperature − 5) × 0.02`

Storage is clamped after each step. The model uses fixed six-second simulation steps at ten updates per real second: **60 simulated seconds per real second**. The basin's visible water footprint and dashboard depth represent this aggregate storage. Wetness follows rainfall/storage with an exponential response. There is no terrain flow, catchment calibration, hydraulic network, infiltration map, measured water level, or calibrated flood forecast. Solar output omits shading, temperature losses, panel orientation losses, and inverter losses. Wind effects are visual, not structural physics.

The environmental change log records actual input changes, water-depth bands, mode changes, and retrieval outcomes. It retains 30 entries and displays the latest three; small input changes accumulate toward logging thresholds. No fabricated alerts or decorative data charts are used.

## Weather source

[Open-Meteo Forecast API documentation](https://open-meteo.com/en/docs). Coordinates were verified against [Open-Meteo's geocoding service](https://open-meteo.com/en/docs/geocoding-api). Data attribution: Open-Meteo, CC BY 4.0. Review the provider's terms for deployment beyond a noncommercial demonstration.

Requests use `temperature_2m`, `relative_humidity_2m`, `precipitation`, `rain`, `wind_speed_10m`, `wind_direction_10m`, `cloud_cover`, and `shortwave_radiation` for current and hourly data, with °C, km/h, mm, and Asia/Kolkata timestamps. Current rainfall is a preceding-interval sum; `rain × 3600 / interval` converts it to a simulation rate in mm/h. Hourly rain is a preceding-hour sum and is used as that hour's mean rate. Precipitation totals remain separate from rain rates. Forecasts use returned timestamps, never invented timeline values.

JSON parsing preserves missing/null numbers as nullable values. Malformed JSON, oversized/deep responses, nonfinite numbers, and duplicate properties are rejected. No successful API response is bundled as an application fallback. Network failure preserves previously retrieved data for the same location; the UI reports the failure and any staleness. Missing required simulation inputs pause integration; the hypothetical lab remains available.

## Dependencies

The baseline settings/assets come from Unity's official **Universal 3D** template `com.unity.template.urp-blank` **17.1.0**. Its original manifest/lockfile are preserved. First-open setup resolves compatible versions through the Package Manager API rather than manually changing package dependencies:

- `com.unity.render-pipelines.universal` — URP/Core renderer and Volume framework.
- `com.unity.ugui` — Canvas UI and TextMeshPro.
- `com.unity.inputsystem` — camera and event-system input.
- `com.unity.test-framework` — Edit Mode and Play Mode tests.
- Built-in modules: `com.unity.modules.unitywebrequest`, `com.unity.modules.particlesystem`, `com.unity.modules.audio`, `com.unity.modules.physics`.

ProBuilder, Cinemachine, downloaded art, a backend, and external JSON libraries are not required. The template also includes development packages not used by DHARA's application code. Final resolved versions are recorded by Unity in `Packages/packages-lock.json` after import.

## Tests

From PowerShell 7 in this directory:

```powershell
.\Tools\Test-Core.ps1
.\Tools\Test-Weather.ps1
.\Tools\Test-Source.ps1
```

The first compiles the production engine/parser/data store with PowerShell's C# compiler and runs 33 checks without Unity. The second makes real HTTPS requests for all five locations using the production request builder/parser/store, and checks units and geocoded coordinates; it needs network access. The third parses C# syntax, validates assembly JSON and local asset GUID references, and checks visible string literals for prohibited branding. These are narrower than compiling or rendering in Unity. Reports are written under ignored `TestResults/`.

In Unity, open **Window > General > Test Runner**. Run the 22 **EditMode** tests and the two **PlayMode** tests after preparation. The original counts were 18 and two; four Edit Mode regression tests have been added. Play Mode tests cover scene startup, scenario-driven water/rain/wind, control-panel synchronization, location clearing, and a real failed network request. They were authored but **not executed**. Perform the manual checks in [verification details](docs/VERIFICATION.md) before presenting the application.

## Windows build

After successful preparation and tests, choose **DHARA > Build Windows application**. Output: `Builds/Windows/DHARA.exe` plus its accompanying data/runtime files. Distribute the entire Windows output folder. The command uses the saved main scene and throws on build failure. A build has **not** been created here.

With the Editor installed and first-open setup already completed, the official CLI can also run:

```powershell
unity test . --editor-version 6000.3.26f1 --mode EditMode --output TestResults/editmode.xml
unity test . --editor-version 6000.3.26f1 --mode PlayMode --output TestResults/playmode.xml
unity build . --editor-version 6000.3.26f1 --target StandaloneWindows64 --execute-method Dhara.Editor.DharaProject.BuildWindows
```

Editor integration is optional for running DHARA. To enable it, close the Editor after saving, run `unity pipeline install --project-path <project-directory>`, reopen, and verify `unity status`. The official Unity plugin and CLI were installed during development; no connected Editor or Pipeline integration was verified.

Optional historical weather, air-quality integration, scenario comparison, and advanced hydrology are not implemented.
