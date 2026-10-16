# DHARA — city exploration upgrade, 16 October 2026

## Project and preservation

Work continued in `C:/Users/Roshan/Downloads/DHARA_CODEX`, the active project containing the latest verified changes. The older Desktop copy was inspected for the purchased asset; no replacement project was created. Git HEAD was a6fd5e8cc344011053a033e374f1f05ad98dd24b. Existing modified files were retained. A recoverable Assets/Packages/ProjectSettings archive and working-tree patch are in `TestResults/Checkpoints/before-city-exploration-20261016`. Previous Windows, visual, animation, explorer and validated releases remain available.

Unity 6000.3.26f1 / URP 17.3.0 is retained. No package changes were required. The PlayMode test assembly references the already installed URP Core/Universal runtime assemblies for its post-processing regression check. The existing main scene and its GUID are retained.

## A. Water model

The primary display is district collection volume in m³ over a conceptual 4,652 m² catchment (1,404 m² roofs, 2,128 m² road union, 1,120 m² green strips). For each fixed six-second model step:

- Incoming volume = rainfall rate in mm/h × elapsed hours × area in m² / 1000.
- Supported surface retention and infiltration are applied before routing runoff to the shared collection node.
- Optional garden storage intercepts inflow up to its configured capacity.
- Drainage removal = min(available water, 0.8 × nominal drainage in mm/h × elapsed hours × catchment area / 1000), including configured extra drainage.
- Remaining collection storage is clamped to 0–150 m³; excess is separately recorded as overflow.

Zero rainfall adds no new rain volume. Zero drainage removes no collection volume; without another removal term, existing collection water persists. Higher drainage increases removal, but two cases can still have equal retained storage if both reach the 150 m³ cap. Comparison tests distinguish unsaturated retention from overflow conditions.

The primary district model has no evaporation, travel time, hydraulic gradient, soil saturation or calibrated street depth. The retained legacy depth comparator and surface-wetness drying rule are separate and disclosed; their mm output does not replace district collection m³. Current API rain is a preceding-interval total normalized using its actual interval; hourly forecast rain is a preceding-hour total applied as a constant hourly forcing. Model time runs at 60 simulated seconds per real second.

Road patches and basin coverage consume actual collection volume and shrink as drainage removes it. Puddle coverage spans the full 0–150 m³ range instead of saturating at 30 m³. The default spatial view now clears its material property block so it cannot repeatedly repaint wet roads with a saved dry color. Spatial patches are illustrative allocations of shared storage, not calculated local flood depths.

## B. Navigation

The existing OrbitCamera owns overview, street/free-roam and tour movement, preventing competing controllers. Walk transitions to the central north/south road. WASD or arrow keys move relative to horizontal heading; Shift moves faster; right-drag supports unrestricted 360-degree horizontal look, with pitch limited to ±65 degrees. The custom outdoor controller uses acceleration/deceleration, substepped axis sliding, cached building/trunk/lamp/bench/basin footprints and a bounded district ground plane. It does not perform full mesh-based locomotion, stair climbing, jumping or building interiors. Eye height is fixed at 2.2 m above the district origin, appropriate to this flat generated environment; an imported terrain city requires collision/layout adaptation.

1 returns to overview, 2 enters street view, 3 enters roam, Home resets the camera, T toggles a tour. Escape exits a tour or releases walking input; clicking the city resumes it. UI input blocks walking/look, and the viewport click clears slider focus. Network failures and visualization changes do not reset the camera or mutate weather inputs.

## C. City tour

The repeatable route visits the actual currently generated district: overview → elevated southern approach → south road entry → central intersection → western avenue → southwest frontage → southeast frontage → eastern avenue → northeast frontage → northern frontage → northwest frontage → western avenue → central intersection → north road → elevated return → overview. Ground route points use the central roads and open frontage lanes at x ±50 / z ±32; north/south road positions remain inside the district. The route is not an imported-city route, because that geometry is absent.

Tour supports Start, Pause, Resume, Restart, Exit and Overview. Status reports running/paused. Position uses eased segments, orientation blends toward segment headings, input is suppressed during autonomous travel, and completion restores overview controls. Checkpoint and interpolated ground-route samples are tested against known solid footprints.

## D. Visualization menu

- City View: normal weather-driven materials, water and atmospheric effects.
- Standing Water: shared collection volume with a pale-to-blue road/collection overlay and illustrative puddles; no claim of per-street depth.
- Drainage: highlights the existing open stormwater channels and collection area; legend reports nominal capacity and actual last-step removal.
- Surface Wetness: uniform modeled road wetness, labeled as shared rather than object sensor readings.

Every view has a legend and selected state. Choosing a main view clears any frozen intervention preview, so its legend and the world use the running model. Roof retention is not colored as street standing water. Confusing Normal, Water, Runoff, Surfaces, Connections and Coverage buttons are removed from the main menu. Internal schematic/analysis functionality remains available to its existing tools. The real frozen baseline/intervention comparison remains in Resilience Studio; no placeholder comparison view was added.

## E. Environmental effects

Live/forecast forcing remains Open-Meteo through UnityWebRequest, with timestamps, status, units, timeout, cancellation and bounded retries. What-if sliders modify a separate hypothetical state. Rain streaks, wind drift, splashes/ripples, wet-road smoothness/darkening, collection patches, vegetation, sky and lighting consume the shared simulation state. Valid meteorological wind directions are FROM bearings; +Z north and +X east. Unknown direction permits speed-driven non-directional vegetation rocking, with no fabricated directional rain drift. Solar radiation now contributes a bounded lighting factor, alongside time, clouds and rain. Particle counts/lifetimes remain bounded. The rain emitter offsets upwind by the fall time to street height, keeping the ground-level footprint over the accessible city instead of letting strong wind leave an edge dry. An additional particle-position regression checks the upwind city edge. Screenshot inspection found an excessive wet-weather bloom glare that parameter checks had missed. An initial inactive-component fix proved insufficient because URP fell back to its default bloom profile. The final fix explicitly overrides bloom intensity to zero while material-based wet reflections remain. The rendered comparison and effective blended stack were both inspected. A strengthened regression case checks the blended stack rather than only the local component, separate from visual-quality inspection.

Existing optional CAMS/Open-Meteo particulate estimates and restrained illustrative haze remain separate from weather. No new gas clouds, dust measurements, lightning, volumetric cloud package or exact visibility estimate was invented. Unsupported sources remain unavailable.

## Asset status and exact import steps

The actual VENCREATIONS SimplePoly City 1.2.3 meshes/prefabs/materials were not found in either project's Assets. The supplied folder .meta files are identifiers and contain no geometry. The procedural city was preserved; purchased-asset integration is not claimed.

1. Open this existing project with Unity 6000.3.26f1.
2. Sign in to the Unity account/organization that owns the purchase.
3. Open Window → Package Manager, select My Assets, search SimplePoly City by VENCREATIONS, select the purchased version, Download and Import. Import the real Models, Prefab, Materials, Textures and Demo content with their metadata.
4. If authentication remains unavailable, transfer the complete legally obtained asset folder or .unitypackage from the other system. Use Assets → Import Package → Custom Package for .unitypackage, or copy the complete asset folder under Assets while preserving .meta files. Five standalone folder .meta files are insufficient.
5. Inspect the actual demo/prefab and render pipeline before replacing the primary city. Preserve original materials and adapt project-owned overrides if required by URP. Do not duplicate the procedural city underneath it. Camera route, collision and model-area mapping must be adapted to its real layout before claiming integration.

## F–I. Execution, stress results and delivery

Final runner results, per-case identifiers, stress outcomes, screenshots, performance samples and release paths are recorded below after execution. Failed earlier runs remain in Verification rather than being discarded.

### Final automated execution

Baseline before modification: 49/49 EditMode and 17/17 PlayMode passed. Final model/parser regression: 87/87 EditMode passed. The final full runtime regression passed 146/146. After the final rain-footprint correction, the affected runtime fixture passed 130/130; 129 overlap the full runtime suite and one is the additional edge test. Therefore **234 distinct cases were executed with passing latest results**, including **168 new cases**. There are zero unresolved failed, blocked or unexecuted cases in that reconciled set. Assertions and repeated runs are not counted as separate tests.

New groups: A water 25; B weather/state 20; C navigation 20; D tour 15; E effects 20; F UI/integration 15; G lifecycle/resilience 15; H end-to-end/smoke 5; S hard scenarios and additional regressions 33. The required A–H groups total 135, including the explicitly requested G group; S cases are additional. Exact input/expectation code, identifiers, source lines, result, duration and evidence file are in `Verification/exploration-test-manifest.json` and `exploration-test-cases.csv`.

Separately: 33/33 PowerShell core checks passed; five genuine HTTPS city checks passed, including units, geocoded coordinates, production parsing and forecast availability. These are not added to the 234 Unity-test count. `exploration-weather-initial.json` retains two earlier network timeouts; the successful retry is `exploration-weather.json`. Real UnityWebRequest five-city and forecast tests ran, rather than relying solely on PowerShell HTTPS.

Earlier failed runs are retained. They exposed the broadened boundary regression, standalone probe timing/Mono readiness and file-sharing issues, an incorrectly saturated comparison fixture, transient public API timeouts/HTTP 503, test-assembly references missing for URP, and an uninitialized volume-stack fixture in batch mode. The standalone probe now checks Windows readiness through PowerShell/.NET, reads the active player log with shared access and still asserts a real window and responding process. Isolated component fixtures disable automatic startup requests and use controlled failure endpoints; explicit real-API cases still contact Open-Meteo. Product startup fetching remains enabled by default. No successful live fallback responses were fabricated.

### Hard scenarios

All S01–S33 passed. This includes heavy rain/low and high drainage before storage saturation, retained water after rain stops, zero-rain/empty and stored-water cases, rapid rain/drain changes, input extremes, missing inputs, rapid city refresh, controlled API failure during transitions/What-if, reset under accumulation, return to live during camera movement, pause/resume/exit, visualization changes during a tour, repeated roam/tour cycles, complete horizontal rotation, continuous navigation through all four frontage regions, movement near solids/bounds, rain particles at street height, maximum wind/rain, repeated wet/dry and emission transitions, reload/shutdown, optional data absence, exiting frozen comparison, effective bloom override and wind-compensated rain at the upwind city edge.

These are component/integration assertions against the actual application, not 33 independent manual playthroughs. Visual-state tests inspect rendering parameters, material/state values and particles; screenshot inspection is separate. H01 launched the previously available validated Windows build; the new exploration release is checked separately below after packaging.

### Visual and performance observations

Eight views (Live, Forecast, What-if, Standing Water, Drainage, Wetness, Tour and Roam) were captured and layout-audited at 1280×800, 1600×900 and 1920×1080: all 24 checks reported zero text overflow and zero off-screen controls. Actual final screenshots in `Verification/Screenshots/Exploration` were inspected, including ground-level rain, water overlay and paused-tour controls. The final legend has a pale backing strip for readable contrast. This final cosmetic adjustment was compiled and recaptured; it does not alter simulation or navigation behavior. Interactive Console inspection reported zero errors/warnings; the build log is reported separately.

A 600-frame Editor sample at 1920×1080 with 100 mm/h hypothetical rain, 80 km/h wind and a moving tour measured mean 16.713 ms, median 16.647 ms, p95 18.303 ms and maximum 57.704 ms on an NVIDIA RTX 4070 Laptop GPU. App hierarchy objects remained 1,283 and referenced world materials remained 88. Rain had 9,800/12,000 particles, splashes 212/4,000, wind debris 300/300 and ripples 292/600. The Editor managed-heap snapshots rose from 847,785,984 to 910,401,536 bytes during the sample. This is an Editor-wide short sample, not a retained-memory or standalone/endurance measurement; it does not prove a leak or memory stability. Raw scope and measurements are in `exploration-performance.json`.

### Remaining limitations

The purchased SimplePoly environment is still missing, so imported-city acceptance remains blocked on the real geometry/prefabs/materials. This release uses the existing procedural environment and its actual layout. Third-person navigation, interiors, stairs and terrain-following physics are deferred; the custom outdoor controller uses a flat ground plane and cached solid footprints. Atmospheric haze/sky effects are illustrative and do not claim measured visibility or visible pollutant gases. Long endurance, low-end/student-laptop benchmarking, exhaustive standalone network-failure injection and every possible user workflow are not verified. No finite suite proves absence of all bugs. Public API availability can vary, as the retained failures demonstrate.

### Windows release and actual standalone verification

Build succeeded: `Builds/Windows-Exploration/DHARA.exe`. The previous releases were not overwritten. Two nonfatal build warnings remain: Unity's internal DebugOccluder shader truncation warning and the expected notice that the Editor-control Pipeline is disabled in release players. The native player also logs the known nonfatal D3D12 debug-info-queue query failure; it rendered successfully. No matched startup/runtime exceptions were found in the inspected player logs.

Final ZIP: `Builds/DHARA-LivingCity-Windows-x64-20261016-034523.zip` (40060570 bytes). SHA256: `7B5FD1FCDFC4DEA66F5D05F4BC77B4572AC6339D77F818B5CD4D53185A92287A`. Packaging verified 174 archive file hashes; fresh extraction verified 175 files including the generated checksum manifest. The first manually tested extraction was repackaged only to correct the Escape instructions in RUN-DHARA.txt. All 173 runtime files match byte-for-byte between the manually tested and final packages; only launch notes and their hash manifest changed. Final extraction: `TestResults/Extracted-Exploration-20261016-034523/DHARA/DHARA.exe`. It was launched and visually observed rendering the city with genuine Bengaluru conditions valid 03:45 IST. It remains open.

Actual native UI checks on the identical runtime binaries: live Bengaluru weather; What-if mode/sliders; heavy-rain collection reaching 150 m³; Standing Water road coloration; higher drainage around 55.9 mm/h reducing collection through 148.9, 136.6, 121.8, 112.0, 98.7, 84.9 and 50.3 m³ while rain around 22.3 mm/h continued; tour start and visible movement from aerial approach to street level; pause holding the camera while drainage continued; resume; exit restoring overview; scenario reset clearing rain/storage and restoring 12 mm/h drainage; Walk completing the street descent; object selection showing actual modeled infrastructure; return to live; camera reset. Full four-direction, 360-degree, collision and complete route checks are automated component/integration coverage, not a claimed exhaustive manual standalone traversal. Native UI automation briefly required re-observation/refocus; it did not cause an application failure.

The manually inspected player remained responsive. One post-workflow snapshot reported approximately 949 MB private bytes and 688 MB working set; no standalone memory trend or endurance stability claim is made. Evidence: exploration-player-manual-tested.json, exploration-player-smoke.json, exploration-runtime-identity.json, exploration-extraction.json and exploration-package.json.

### Launch and reproduce

Extract the complete ZIP, then double-click `DHARA/DHARA.exe`. Keep DHARA_Data, UnityPlayer.dll, MonoBleedingEdge and companion files together. Unity is not required to run the Windows player. The direct build executable is `C:/Users/Roshan/Downloads/DHARA_CODEX/Builds/Windows-Exploration/DHARA.exe`.

Choose a city and wait for its real weather status. Open What-if Lab to adjust rainfall/wind/temperature/drainage. Click Walk for street entry, then click inside the city to focus movement. Hold WASD/arrows to move; Shift moves faster; right-drag looks around through 360 degrees. 1 returns to overview; Home resets; Escape releases walking input or exits the tour. Tour exposes Pause/Resume/Restart/Exit/Overview controls. Main views are City View, Standing Water, Drainage and Surface Wetness. Reset scenario clears accumulated scenario storage; Return to live clears hypothetical overrides.

Editor: open the existing Downloads project in Unity 6000.3.26f1 and run Assets/Scenes/Dhara.unity. Batch tests require closing the interactive Editor: use Unity.exe -batchmode -projectPath <project> -runTests -testPlatform EditMode (or PlayMode) -testResults <xml> -logFile <log>. The full runtime suite and affected fixture reports preserve actual runner counts. `python Tools/Record-ExplorationTests.py` reconciles unique cases. The standalone smoke adapter uses the bundled PowerShell 7 runtime configured in H01; adapt that test path on another machine. This is a test-harness dependency, not a player dependency.

Build: Unity.exe -batchmode -quit -projectPath <project> -executeMethod Dhara.Editor.DharaProject.BuildExplorationWindows -logFile Verification/exploration-build.log. Package: pwsh -NoProfile -File Tools/Package-Windows.ps1 -BuildDirectory Builds/Windows-Exploration -ReportPath Verification/exploration-package.json.

Public hosting/domain configuration was not changed in this Windows-release pass. The missing purchased asset and the limitations above remain explicit; no claim of complete imported-city acceptance or universal hardware compatibility is made.
