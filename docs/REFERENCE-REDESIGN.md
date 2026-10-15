> Historical delivery record. The current Living City upgrade supersedes feature limitations below; see [current implementation report](LIVING-CITY.md).

# DHARA reference redesign — 15 October 2026

This report covers the light dashboard redesign. The earlier [verification report](VERIFICATION.md) retains the previous release's evidence. Existing uncommitted work, scene, GUIDs, framework, API and prior Windows build were preserved. Checkpoint: `TestResults/Checkpoints/before-reference-redesign-20261015.zip`.

## Reference and implementation

The first supplied image guided the white/pale-blue surface palette, navy typography, teal selection, compact header, six weather cards, central district, right-hand output cards and timestamped activity list. Supporting images informed city framing, roof detail and landscaped blocks. No decorative charts, invented building measurements or fake LIVE badge were added. A three-line causal insight panel and a drainage comparison replace unsupported analytics. The geometry remains a procedural district rather than the rendered reference's bespoke models or any named city's reconstruction.

Changed in this pass:
- `EnvironmentalDashboard.cs`: existing uGUI/TMP dashboard redesigned; vector icons, rounded cards, source timestamps, explicit loading/failure/stale status, storage/wetness bars, mode controls, causal insights and intervention overlay. Forecast slider range now synchronizes immediately on mode entry.
- `OrbitCamera.cs`: home framing uses individual mesh bounds and projected vertical centering.
- `ProceduralCity.cs`: paler mineral facades, landscaped blocks, roof parapets, plinth, sky/exposure/contrast adjustments; existing rain, wind, wetness, water and solar behavior retained.
- `DharaApplication.cs`: central viewport uses the dashboard's shared rectangle.
- `DrainageExperiment.cs`: frozen-input, independent baseline/intervention comparison using the production water model.
- `DrainageExperimentTests.cs`, `ApplicationTests.cs`, Play Mode asmdef: quantitative model and actual control/isolation tests.
- `DharaProject.cs`, `Package-Windows.ps1`: separate `Builds/Windows-Visual` release path and parameterized packaging, preserving `Builds/Windows`.

## Intervention assumptions

Capture copies the active weather and nominal drainage. Each comparison starts both cases at zero storage, holds inputs constant for one model hour and executes 600 six-second steps:

`delta mm/hour = 0.85 × rain − 0.8 × nominal drainage − max(0, temperature − 5) × 0.02`

Production input clamps and the 0–150 mm storage bound apply every step. Added nominal drainage is capped at 100 mm/h total. Reset sets added drainage to zero; live weather and the running scenario are never changed. At 80 mm/h rain, 25°C and drainage 12 → 32 mm/h, results are 58 → 42 mm retained water (16 mm reduction). Dry conditions can yield no benefit. This is an illustrative equation, not calibrated engineering performance.

Permeability, rain gardens, green roofs and vegetation interventions are not offered because the existing model has no defensible coefficients for them. Object-level impact explorer is deferred: the model reports district-level storage and solar, not per-building observations.

## Verification

Final execution results and artifact paths are recorded below after runtime and package verification.

Executed checks:
- Source validation: 17 C# files and five assembly definitions; syntax, asset GUIDs/metas and visible strings passed. This is separate from Unity compilation.
- Unity compilation: zero errors and zero warnings (`Verification/visual-compilation.json`).
- Core checks: 33/33 passed (`visual-core-results.json`).
- Edit Mode: 26/26 passed, zero skipped (`visual-editmode.json`).
- Final Play Mode: 9/9 passed, zero skipped, 45.66 seconds (`visual-playmode.json`). Includes genuine five-city UnityWebRequest, exact forecast record selection, sliders/effects/water/drainage, intervention capture/reset/isolation, malformed/missing/HTTP failures, actual 20.08-second timeout, cancellation, rapid selection/refresh, retry recovery and scene cleanup.
- Live HTTPS/parser/geocoding checks: 5/5 passed; HTTP 200, eight requested fields available, correct units and coordinates, 48 hourly / 47 future records. Separate from UnityWebRequest execution (`visual-weather-results.json`).
- Layout: actual rendered Canvas sizes 1920×1080, 1600×900 and 1280×800; Live, Forecast, What-if and intervention overlay checked at each. No TMP overflow or controls outside the Canvas. Capture images inspected for hierarchy, scene framing, readability and control alignment. The Editor's global Screen dimensions can reflect another view during command evaluation; audits use the actual Canvas pixel rectangle and confirm the corresponding camera pixel rectangle. See `visual-layout-*.json` and `Verification/Screenshots/ReferenceRedesign/`.

The initial runtime suite found a forecast slider range initialization failure (8/9 passed). Synchronizing the range immediately fixed it; both subsequent full runs passed 9/9. The initial diagnostic remains in `visual-playmode-initial.json`. Visual review also found missing CanvasRenderer on custom icons and loading-state combined labels that could truncate; both were corrected before release. No failed check is hidden or counted as passing.

## Windows delivery and actual player observations

Final executable: `C:/Users/Roshan/OneDrive/Desktop/DHARA_CODEX/Builds/Windows-Visual/DHARA.exe`.
Final distributable: `C:/Users/Roshan/OneDrive/Desktop/DHARA_CODEX/Builds/DHARA-Windows-x64-20261015-084804.zip` (40,018,006 bytes, SHA-256 `647029422D4D07A556AE6A938F3DF3AF922E40581FAFB8AC7F9AD87497505468`).

The first build attempt failed while Unity cleaned a read-only OneDrive temporary Burst directory. Reopening the Editor with a fresh Temp directory resolved it; the next build succeeded in 38.02 seconds with zero errors and one expected Pipeline warning (release control server disabled). The initial diagnostics remain in `visual-build-initial-console.json`.

The first packaged launch exposed TMP's unavailable ellipsis fallback. The final one-line presentation correction uses explicit Truncate overflow mode; the prior layout audits had no overflowing text. Recompilation passed with zero errors/warnings. A subsequent Windows build succeeded in 22.177 seconds, recorded in the saved Editor build log. The Editor closed after successful completion, so a later detached-job query was unavailable; the saved Build Successful / Result Success and PlayerBuildInfo records are the final build evidence (`visual-final-build-log.txt`, `visual-final-build-summary.json`). The full 9/9 runtime suite preceded this final text-overflow setting change; final player controls and rendering were then checked directly.

The final ZIP was expanded into `TestResults/Extracted-Visual-Final-20261015-084838/DHARA`. All 174 ZIP file hashes and all 174 extracted file hashes passed. Extracted and build runtime DLL hashes match: `D67F5ABBF7572FEB91BCF6543A3FD3EF649C7B90D2C647ED07B647AC403E59DD`. Previous build DLL hash remains `9DFB47303E0060DD42107E5CCA227782D8DD9378CEB45172D1A2BA2CF16D2606`.

The actual extracted Windows player was observed with native window captures and clicked controls:
- Startup, city and dashboard rendered; model time advanced.
- Genuine current weather reached all five city selections. At model-valid 2026-10-15T08:45 IST, observed temperatures were Bengaluru 25.6°C, Chennai 27.4°C, Mumbai 30.4°C, Delhi 23.1°C and Hyderabad 27.2°C. Mumbai refresh advanced retrieval time with a success event. Hyderabad loading showed unavailable values and paused outputs before the response.
- Forecast changed from 09:00 IST to 18:00 IST at step 9, updating rain, wind, temperature, total precipitation and solar. Return to live restored current inputs.
- What-if rainfall 98.3 mm/h caused visible rain, 100% wetness and rising simulated storage; scenario reset restored baseline rainfall and cleared storage.
- Independent experiment at that rain rate showed 73.5 mm baseline versus 57.7 mm with 19.7 mm/h added drainage, rounded displayed reduction 15.8 mm. Running drainage remained 12 mm/h. Experiment reset restored equal baseline/intervention results.
- Normal Alt+F4 shutdown completed, the final process was absent and the log recorded physics/input cleanup. No exception, error or ellipsis warning appeared in the final player log.

Evidence: `Verification/visual-packaged-player-smoke.json`, `visual-final-player.log`, `visual-windows-package.json` and `Screenshots/ReferenceRedesign/standalone-*.jpg`. Native captures use Windows logical pixels under display scaling; exact three-resolution audits were performed on the Editor's rendered Canvas. The release was launched with `-screen-width 1600 -screen-height 900 -screen-fullscreen 0`.

Rebuild: **DHARA > Build Windows visual release** (or evaluate `Dhara.Editor.DharaProject.BuildVisualWindows()` through Unity CLI). Package: `pwsh -NoProfile -File Tools/Package-Windows.ps1 -BuildDirectory Builds/Windows-Visual -ReportPath Verification/visual-windows-package.json`. Run: double-click `Builds/Windows-Visual/DHARA.exe`, or extract the complete ZIP and run its `DHARA/DHARA.exe`.

Remaining limits: the optional object-level explorer and unsupported green-infrastructure interventions are deferred. Forced network-fault injection was executed in Unity Play Mode, not in the release player. No low-end hardware benchmark, exhaustive retry-backoff progression or extended endurance run was performed. The city uses simple procedural geometry and illustrative lighting; it does not reproduce the reference's bespoke art or a real geographic district. Previous verified build/ZIP and prior working changes remain preserved. No external blocker remains for this delivery.

