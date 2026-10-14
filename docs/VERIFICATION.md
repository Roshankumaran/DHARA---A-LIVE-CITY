# Verification and remaining work

## Executed on 14 October 2026

- Production core compiled through PowerShell 7 `Add-Type`.
- **21/21** standalone core checks passed. Coverage includes causal rain/wind/cloud rules, drainage sensitivity, bounds under 10,000 extreme updates, nonfinite input sanitization, deterministic state, scenario reset, missing data, interval conversion, null-preserving JSON, exact forecast indexing, error/stale status, event deduplication, offline scenarios, and malformed JSON rejection.
- **5/5** real Open-Meteo HTTPS request/parser/geocoding checks passed, including a second run after applying the verified coordinates. All five responses contained required current fields and 48 hourly forecast records. This proves the request contract and production parser against real responses; it does not execute UnityWebRequest.
- C# syntax, assembly-definition names/JSON, required local asset GUIDs, metadata coverage, and runtime string-branding checks are covered by `Tools/Test-Source.ps1`. The saved result states the actual outcome. These checks do not resolve all Unity API types or compile the Unity assemblies.
- Git initialized locally. There was no existing project to preserve at the start.

Machine-readable snapshots of the executed checks are in `Verification/`. Raw API responses and repeat-run reports are under ignored `TestResults/`. Neither directory is used by the running application as a weather source.

## Environment blocker

The official Unity plugin `unity@unity-agent-plugin` **0.1.8-beta** was installed through its documented marketplace. Unity CLI **1.0.0-beta.13** was installed and confirmed current. The release catalog reported Unity **6000.3.26f1** as a Unity 6.3 LTS release.

There was no pre-existing Editor, open Unity process, signed-in Unity CLI account, or connected Pipeline instance. The first installer exited with **code 2**, and its command reported **INSTALL_FAILED / INSTALL_ERROR**. A no-elevation retry diagnosed **ELEVATION_REQUIRED** for Program Files. User-profile overrides were rejected as nonexistent by the CLI. A final writable workspace install path was accepted, but the installer again reported **INSTALL_FAILED / INSTALL_ERROR**. No successful Editor installation was detected. License status reported **LICENSING_CLIENT_UNAVAILABLE**. Pipeline connection status reported **STATUS_NO_INSTANCES**.

Further automatic installation retries were stopped. Install the Editor through Unity Hub and activate its license to unblock execution. The CLI's saved Editor installation root was changed during the final attempt to this workspace's ignored `.bootstrap/Editors` folder; choose the desired permanent location in Hub, or use `unity install-path --set <existing-directory>` to change it. The original default was `C:/Program Files/Unity/Hub/Editor`.

The project uses the downloaded **official Universal 3D 17.1.0 template** settings/assets as a baseline. Required package resolution, TMP import, scene preparation, and build commands are implemented but have not been executed in an Editor.

## Authored, not executed

- 18 Edit Mode NUnit tests in `Assets/Dhara/Tests/EditMode`.
- Two Play Mode tests in `Assets/Dhara/Tests/PlayMode`: scene/scenario responses and a genuine failing UnityWebRequest to a reserved `.invalid` host.
- URP scene preparation and font/material setup.
- Unity compilation, renderer/shader variants, UI layout, input controls, network retry coroutine, visual transitions, and performance.
- Windows build and standalone smoke test. **No executable exists.**

## Required manual acceptance pass

After opening in licensed Unity 6.3 LTS and completing first-open setup:

1. Confirm no Console compilation errors or runtime exceptions. Run both Test Runner suites.
2. Enter Play Mode and confirm the district, readable dashboard, natural materials, rain system, and useful overview camera. Inspect all three modes at 1600 × 1000 and a smaller landscape window. Check that all visible controls receive input and text does not clip.
3. Select each city. Verify the displayed selected location, source, IST valid time, UTC retrieval time, null/unavailable fields, and actual returned values. Confirm that switching locations never displays the previous location's weather.
4. Verify five-minute refresh, the refresh button, failure status, retry/backoff, preserved same-location data, and stale status. Disconnect/reconnect networking for this check; confirm the scene remains navigable and hypothetical mode remains available.
5. In WHAT-IF LAB set rain to 80 mm/h and drainage to zero. Watch rain emission, wetness, storage depth, basin footprint, and the actual change log. Raise drainage and compare storage after the same elapsed model time from a reset. Increase wind and confirm sway and particle drift.
6. Reset the scenario, return to live, and verify the mode indicator, baseline values, and zeroed storage. Confirm hypothetical rain/wind never overwrites the retrieved weather object.
7. Select several forecast hours. Compare the displayed valid timestamp and values to the corresponding actual API response entries. Confirm forecast data is labelled, preview storage resets, and controls make no extra requests.
8. Review the full visible experience for consistent environmental identity, accurate simulated/model/missing-data labels, and absence of prohibited branding or decorative statistics. The automated string scan is only part of this review.
9. Build Windows through the DHARA menu, inspect the build report, and run the executable with its companion data/runtime files. Repeat camera, location, scenario, forecast, and network-failure smoke checks in the standalone player.

Do not call the application ready until these checks have evidence. Historical replay, air quality, scenario comparison, and advanced hydrology are outside the implemented scope.
