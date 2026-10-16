# DHARA validation and refinement — 16 October 2026

The existing Downloads project was validated in Unity 6000.3.26f1. Earlier Windows, visual, Living City, animation and explorer builds were preserved. The user confirmed that the light dashboard should be retained. No new reference image was attached to this request; the existing light layout and written requirements guided the refinements.

## Inspection and changes

Inspected README, historical verification reports, attached requirements, version/manifest/lockfile, saved Dhara scene, bootstrap and dependency/build automation, runtime weather/state/UI/city/camera code, test assemblies and standalone test tools. The main scene references its existing bootstrap, surface and rain materials. URP 17.3.0, Input System 1.20.1, uGUI 2.0.0, Test Framework 1.6.0 and Pipeline 0.8.0-exp.1 resolve successfully. No package changes or scene regeneration were needed. Editor inspection confirmed PC_RPAsset.

Targeted changes:
- Retained the light/pale-blue dashboard, navy typography and teal selection.
- Renamed the visible activity heading to Environmental Change Log, removed the fixed 72-character message cut-off and allowed bounded text sizing within existing rows.
- Reduced camera-button text size for the compact header.
- Fixed offline wind-speed scenarios: vegetation now rocks without requiring a compass direction. Unknown direction does not create directional rain drift; known directions still use the meteorological FROM convention. Added an actual runtime regression assertion.
- Added BuildValidatedWindows targeting a separate output directory.

Existing architecture, purchased-asset metadata, scene GUIDs and prior user changes were preserved. A pre-edit UI/build-script checkpoint is in TestResults/Checkpoints/before-validation-20261016.

## Checks actually executed

- PowerShell core: 33/33 passed.
- Source: 44 C# files, five assemblies, metadata/GUID and runtime-branding checks passed. This is separate from Unity compilation.
- Real HTTPS API: 5/5 cities passed: Bengaluru, Chennai, Mumbai, Delhi and Hyderabad. HTTP 200, current/hourly units, geocoded coordinates and 48 forecast records per city were checked using the production parser/store. No successful responses were fabricated.
- Unity EditMode: 49/49 passed.
- Unity PlayMode: initial run 10/17 passed; two offline-wind assertions failed and five live-data-dependent checks failed during network timeouts. The original results remain in validation-playmode-initial.xml/log. After the wind fix and restoration of connectivity, the full rerun passed 17/17 in 79.63 seconds.
- Actual runtime coverage: procedural generation and supported shaders; overview framing and camera transitions/walking/collision; five real city button requests and shared live-state identity; exact forecast slider timestamp; What-if sliders, reset and return to live; wetness, rainfall, wind direction, lighting, accumulation/drainage/drying; malformed/503/missing data, bounded timeout, stale retention, automatic retry recovery, rapid selection and scene cleanup; existing resilience controls.
- Editor Play Mode frame counter was observed advancing. Live/Forecast/What-if layout audits passed at 1280×720 and 1920×1080 with zero reported text overflow or off-screen controls. Actual screenshots were captured; Live, Forecast and settled What-if views were visually inspected. Source values, hypothetical labels and slider values were consistent.
- Unity compilation and Console evidence: validation-compilation.json and validation-console-final.json. No runtime Console errors or warnings were reported during the interactive inspection.

Evidence is under Verification/validation-*; screenshots are under Verification/Screenshots/Validation. Unity live city observations are in validation-live-locations.txt. The first failed run is retained rather than hidden.

## Remaining limits

The original dashboard reference image was unavailable in this message; an exact image match is not claimed. The procedural city remains the primary environment. VENCREATIONS SimplePoly City geometry is still absent: folder .meta files cannot supply models, materials or prefabs. No purchased-asset integration is claimed.

The water model is conceptual and uncalibrated. Unknown-direction wind rocking is illustrative, not a reconstructed wind bearing. Long endurance, full retry-backoff progression, broad hardware benchmarking and forced network failure in the standalone release remain unverified. Network-failure/recovery checks were executed in Unity tests. Web deployment and Hostinger DNS were outside this validation pass.

## Launch and rebuild

Final executable: Builds/Windows-Validated/DHARA.exe. Keep all companion files together. No Unity installation is needed to run the Windows package.

Use the five city selectors, then Live World, Forecast Preview or What-if Lab. What-if exposes rainfall, wind, temperature and drainage; Reset scenario restores the available baseline and Return to live clears overrides. Walk enters street exploration; click the city, use WASD, Shift for faster movement and right-drag to look. Press 1 for overview or Esc/Home to reset.

Editor: open this project in Unity 6000.3.26f1, open Assets/Scenes/Dhara.unity and press Play. Batch rebuild (with the interactive Editor closed): Unity.exe -batchmode -quit -projectPath C:/Users/Roshan/Downloads/DHARA_CODEX -executeMethod Dhara.Editor.DharaProject.BuildValidatedWindows -logFile Verification/validation-build.log.

## Final delivery result

Windows build succeeded (validation-build.log). The build emitted a nonfatal Unity DebugOccluder shader truncation warning and the expected notice that the Editor-control Pipeline is disabled in release players. Interactive Editor inspection had zero Console errors/warnings; this does not mean the build log had no warnings.

ZIP: `Builds/DHARA-LivingCity-Windows-x64-20261016-005227.zip` (40,058,509 bytes). SHA256: `1A79D96CF7293C04DB76D31F2284CF3DBB4BE9638A255181A1F1931A57055C59`. All 174 packaged file hashes passed archive verification. After fresh extraction, 175 files (including the generated hash manifest) matched the staged delivery. The extracted executable was launched at 1600×900, remained responsive and had no matched startup exceptions. See validation-package.json, validation-extraction.json and validation-player-smoke.json. Standalone controls and forced network failures were not manually retested; full control/network tests and screenshot inspection ran in Unity.

Extract the complete ZIP and launch `DHARA/DHARA.exe`. The extracted verification copy is `TestResults/Extracted-Validated-20261016-005227/DHARA/DHARA.exe`; the direct build remains `Builds/Windows-Validated/DHARA.exe`. The final extracted player was left open.
