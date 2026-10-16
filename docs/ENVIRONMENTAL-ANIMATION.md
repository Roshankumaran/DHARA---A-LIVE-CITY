# DHARA environmental animation upgrade — 15 October 2026

Working project: `C:\Users\Roshan\Downloads\DHARA_CODEX`. This extends the existing Unity 6000.3.26f1 / URP project. The separate running Desktop Editor was not modified. Starting HEAD: a6fd5e8. The pre-existing camera/city edits, working-tree patch and HEAD are preserved in `Verification/animation-checkpoint-20261015`. The nested project and previous releases remain intact.

## Implementation

- **Camera:** preserves the existing mesh-based overview framing, mouse orbit/pan/zoom and presets. Street travel now follows explicit spatial segments: rise above roofs, cross at safe altitude, descend inside the central north/south road. Position and look direction both change. The street endpoint is (0, 2.2, -34) looking along +Z. Street manual movement is constrained to this corridor. Clickable Reset, Street and Tour controls are also available. `1` returns to overview; `2` enters street view; `T` starts/cancels the short overview/street/overview tour; Escape, Home and Reset camera cancel and restore control. The tour uses the running simulation and never changes weather.
- **Rain:** the existing bounded 12,000-particle world-space system now uses a generated tapered alpha texture and stretched rendering. Shared rain intensity maps to at most 3,500 emissions/second. Dry input immediately stops new emissions while existing drops finish their lifetime. Wind drift follows meteorological FROM direction: +Z north, +X east; easterly wind moves rain west. Missing/nonfinite direction produces no invented heading.
- **Wet surfaces:** the existing central wetness drives asphalt darkness/smoothness with slower visual drying. Fixed road puddle sites now expand/shrink horizontally with central collection volume, reaching full illustrative coverage at 30 m3. These depict district storage, not measured or separately solved road depths. Basin/channel visuals continue using the same model storage. No new hydrological calculation is invented in the renderer.
- **Impacts:** splashes are restricted to the clear east/west road rather than the entire building footprint. A capped 600-particle horizontal ripple system uses a generated ring texture, size-over-lifetime expansion and alpha fade. Rain and wetness govern emission; zero rainfall stops new splashes/ripples. All textures/materials are created once and disposed with the city.
- **Vegetation:** existing root pivots remain attached. Absolute rotations use a shared downwind heading, individual phase variation and a maximum seven-degree lean. Lower wind restores rest without accumulating rotation. The existing tree collection is used; no per-frame scene search was introduced.
- **Atmosphere:** preserves the existing weather/time lighting and postprocessing improvements. Rain/cloud fog now survives the application update and combines with optional air-quality haze using the larger density. No additional weather controller or provider was added.

The broad Living City functionality was already present at the starting HEAD: spatial water balance, green-roof retention, permeable infiltration, garden storage, planner, trace, history, comparison, export, guide and sensitivity. It is preserved, with existing limitations in `docs/LIVING-CITY.md`. This upgrade changes presentation rather than replacing the simulation or API architecture. The current task supplied textual descriptions of four visual references, but no four image files were attached for inspection.

## Executed checks

- PowerShell core checks: **33 passed**. Evidence: `Verification/animation-core-results.json`.
- Syntax, assembly-reference and asset GUID/meta validation: **44 C# files / five assembly definitions passed**. This check alone is not Unity compilation proof.
- Unity Edit Mode: **49 passed, zero failed/skipped**, Editor exit 0. Evidence: `Verification/animation-editmode.xml` and `.log`.
- Unity Play Mode with graphics enabled: **17 passed, zero failed/skipped**, Editor exit 0. Evidence: `Verification/animation-playmode.xml` and `.log`. Includes two new animation tests covering the actual street camera route/end height, tour cancellation, wind heading/bounds, rain renderer/texture, ripple emission, shrinking puddles, dry emission shutdown and nonfinite direction. Existing application and resilience regressions also ran.

The CLI update check could not resolve public-cdn.cloud.unity3d.com; installed Unity was used. The first CLI batch attempt stalled around Hub IPC. A direct batch launch then exposed stale copied Bee compiler pipe references. The old Bee cache was preserved as `Library/Bee.pre-animation-20261015`; rebuilding it allowed compilation/tests to complete. Interrupted attempts are not counted as passing tests. Logs retain non-blocking Unity licensing diagnostics.

## Important files

`Assets/Dhara/Runtime/OrbitCamera.cs`, `ProceduralCity.cs`, `DharaApplication.cs`, `EnvironmentalDashboard.cs`; `Assets/Dhara/Editor/DharaProject.cs`; `Assets/Dhara/Tests/PlayMode/AnimationTests.cs` and its meta.

## Demonstration

Open City tools > guided demonstration > deterministic demonstration. The UI identifies synthetic conditions. Let modeled wetness/storage develop. Press `2` over the world viewport to descend; inspect rain streaks, road water and expanding ripples. Press `1` to return and observe trees. Press `T` to run the short camera tour; Escape cancels. Switch to What-if, set rainfall and wind to zero, and observe emission stop, calmer trees and model-driven drainage/drying. Return to Live World to restore provider forcing. Missing live data remains labeled.

## Delivery and visual inspection

Final Windows build succeeded (Editor exit 0): `Builds/Windows-Animation/DHARA.exe`. Final build log: `Verification/animation-build-release.log`.

Distributable: `C:\Users\Roshan\Downloads\DHARA_CODEX\Builds\DHARA-LivingCity-Windows-x64-20261015-222711.zip` (40057415 bytes). SHA256: `8410AE30B30C57EF0386DB6BF541344833311BA2D93FA203D1241B88E890DB94`. All 174 archive file hashes and all 174 freshly extracted file hashes matched. Evidence: `Verification/animation-package-release.json` and `animation-extraction-release.json`.

The extracted release was launched as a native Windows application. Its product title, genuine weather connection, complete overview and readable Reset/Street/Tour controls were inspected. Street travel was observed in the packaged app. Earlier package inspection also exercised the explicitly labeled deterministic rain demonstration at street level. This is not a claim that every particle or visual detail was exhaustively inspected at all resolutions. Keyboard injection was unreliable during inspection; direct camera buttons provide the verified interaction path.

An observed particle-velocity warning was fixed by using matching constant curve modes for wind debris. The final animation regression run passed **2/2**, including new assertions for these modes (`Verification/animation-release-tests.xml`; Editor exit 0). These are a targeted rerun of the animation tests, not two additional distinct tests beyond the earlier 17. Final player log checks found no Particle Velocity, Exception, NullReference or C# error matches during the inspected run. Reset restored the overview in the final extracted release. Normal window-close shutdown and process exit were also verified for that final release; evidence is in `Verification/animation-release-smoke.json`.

Remaining visual verification boundaries: no exhaustive three-resolution rerun, endurance benchmark or independently observed full wet/dry recovery sequence was completed for this release. The existing model remains simplified and uncalibrated; the pre-existing IMD provider-access limitation remains documented in `docs/LIVING-CITY.md`.


