# Verification and remaining work

## Existing-project validation pass — 14 October 2026

Project root confirmed: `C:/Users/Roshan/OneDrive/Desktop/DHARA_CODEX`. Inspected the README and this report before changes, then checked `ProjectVersion.txt`, the full manifest/lockfile, scene/bootstrap/material references, all C# sources, five assembly definitions, dependency automation, build settings, and all four scripts under `Tools/`. The recorded Editor is `6000.3.26f1`; the template manifest pins URP `17.0.4`. Package compatibility remains an Editor-resolution check; package versions were not manually replaced.

Executed results:

- Initial standalone baseline: **21 passed, zero failed**. After corrections and regression coverage: **33 passed, zero failed**, compiling the production core through PowerShell `Add-Type`.
- Initial source check: **13 C# files and five assembly definitions passed**. After adding the data store and editor regression fixture: **15 C# files and five assembly definitions passed**, with zero reported syntax, source-branding, required-reference, or metadata issues. This does not resolve Unity API types.
- Initial weather run inside the restricted network sandbox: **five requests failed with `No such host is known`**; none was counted as a pass. An allowed network-enabled rerun passed all five locations. The strengthened integration checks then also passed **five of five**: Bengaluru, Chennai, Mumbai, Delhi, and Hyderabad. Each returned **48 hourly records**, of which **28** were future records after production filtering at the tested 19:30 IST valid time. Current and hourly units, interval conversion inputs, and configured coordinates were verified against real responses/geocoding. No fake API response was used as a fallback.
- Unity CLI reported **zero installed Editors**, **zero running Editor instances**, and `LICENSING_CLIENT_UNAVAILABLE`. Unity Hub was not found in the standard Program Files/user Programs paths or the checked Windows uninstall registry. Windows player support cannot be verified without an Editor. No additional installer retries were made during this validation pass.
- Unity compilation, Console inspection, scene rendering, manual interaction checks, and Unity Test Runner execution remain **blocked**, not passed. No Windows build was attempted because compilation and scene execution are prerequisites.

Source defects corrected:

- Scene preparation previously recreated the main scene and replaced the build-scene list. It now reuses the existing bootstrap, preserves configuration/other objects and build entries, and fills only missing references. The saved scene and its GUID were not regenerated during this pass.
- Manual refresh previously cleared the successful same-location weather, forecast, and retrieval timestamp. The shared production data store now retains those through refresh/failure, clears them for a new location, and validates responses before replacement.
- Forecast refresh previously silently selected the first hour while retaining storage from the old selection. Refresh now retains the selected timestamp, or reports expiry and resets preview storage when the hour disappears.
- Changing locations could switch simulation mode while leaving What-if controls visible. Controls now subscribe to actual mode/reset changes.
- Returning to live now clears stored hypothetical overrides; live refresh keeps active hypothetical overrides separate from the live object.
- Nonfinite required weather inputs pause integration. Stale data stays labelled during refresh. Temperature changes now generate actual drying-rule events; bounded inputs no longer create spurious repeated input-change events.
- Generated camera/world/dashboard/input objects now share application ownership for cleanup. Wind amplitude and particle drift transition smoothly in source; missing direction does not imply a fabricated north wind. Visual behavior still requires Play Mode verification.

The original **18 Edit Mode and two Play Mode tests** were confirmed in source. The updated suite has **22 Edit Mode tests** (including scene-preservation, cache, forecast-refresh, and mode-reset regressions) and **two expanded Play Mode tests**. **Zero Unity tests were executed**. Standalone tests do not substitute for these runs. No rendered visual defects can be confirmed or declared fixed without the Editor.

Snapshots in `Verification/core-results.json`, `source-results.json`, and `weather-results.json` reflect this pass. Historical notes below describe the initial implementation, not additional successful Unity verification.

Minimum remaining setup and launch:

1. Install Unity Hub, sign in, and install **Unity 6000.3.26f1** for Windows with Windows 64-bit player support; activate the appropriate license. The project uses the Mono scripting backend, so an IL2CPP toolchain is optional.
2. Add the existing root folder to Hub and open it with that Editor. Let Package Manager resolve; inspect any compatibility errors before changing package versions. Required package setup uses `DHARA > Install or repair dependencies`.
3. Allow TMP essentials preparation, then run `DHARA > Prepare main scene` if needed. Confirm no Console compilation errors; open `Assets/Scenes/Dhara.unity` and press Play.
4. Run the 22 Edit Mode and two Play Mode tests, then the manual acceptance pass below. Only after successful scene execution use `DHARA > Build Windows application`.
5. Expected output is `C:/Users/Roshan/OneDrive/Desktop/DHARA_CODEX/Builds/Windows/DHARA.exe`. This file does **not** currently exist. Distribute its entire output folder after a verified build and standalone smoke test.

## Initial implementation checks — 14 October 2026

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
