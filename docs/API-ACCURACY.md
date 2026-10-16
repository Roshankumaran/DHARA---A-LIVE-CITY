# API accuracy upgrade — 16 October 2026

The active project is `C:/Users/Roshan/Downloads/DHARA_CODEX`, HEAD `a6fd5e8cc344011053a033e374f1f05ad98dd24b`, with existing uncommitted work retained. The older Desktop path in the requirements is not the current verified checkout. Before changes, Assets, Packages and ProjectSettings were archived to `TestResults/Checkpoints/before-api-accuracy-20261016/project-source.zip`, with the existing Git diff alongside it. Existing Windows releases remain preserved.

## Architecture

Keep the working Unity client. Unity 6000.3.26f1 compiles and executes tests with the existing installation; an account sign-in workaround or replacement browser application is unnecessary for this API task. No packages were added, and the main scene, GUIDs and simulation controller were preserved. This update does not claim a new web deployment or completion of every independent navigation/export/web request in the broader attachment.

## Implemented data

- Weather: temperature, relative humidity, rain, precipitation, wind speed/direction, cloud cover, shortwave radiation and WMO weather code, for current model estimates and hourly forecast records.
- Air quality: US AQI, European AQI, PM2.5, PM10, nitrogen dioxide, ozone, sulphur dioxide and carbon monoxide, from the separate Open-Meteo Air Quality endpoint with CAMS global selected.
- History: the existing ERA5 endpoint now also requests weather codes; its production boundary validates units, ordered arrays and the exact requested date.
- Geocoding: all five configured Indian city coordinates were checked against real geocoding results. There is no new arbitrary-location selector.

`EnvironmentalParameters` exposes field name, nullable value, unit, source, valid time, retrieval time, availability and semantic description. Current and hourly records carry their aggregation interval and forecast classification. Zero remains a valid value; absent/null remains unavailable. Invalid response values are rejected; retained prior data keeps its successful retrieval time and is labeled after failure.

The existing light dashboard is retained. City tools > Air quality / advisories opens Environmental Data Sources. Weather / air quality switches between actual weather details and the eight AQI/pollutant values. Source groups, timestamps, missing fields and failures are shown. The two AQI scales are explicitly distinguished from Indian NAQI.

## Semantics and safeguards

The official forecast API defines current `interval` as the duration of backward-looking totals/means. DHARA divides current `rain` in mm by `interval / 3600` to obtain mm/h. Hourly rain represents the preceding hour and has the same numerical value when expressed as mean mm/h. `precipitation` remains a separate accumulation including rain, showers and snow; it is not an instantaneous rain rate. Rain-only forcing does not model separate snowfall or convective shower components. Shortwave radiation is an interval mean, not a fabricated instantaneous measurement. Wind direction is meteorological FROM, clockwise from north.

External forecast responses now validate JSON, IST offset, ISO time, interval, supported field units, numeric types/ranges, WMO codes, hourly array lengths and timestamp ordering. Current valid time must be within two hours of retrieval and not more than five minutes ahead. Air-quality valid time has a three-hour age limit. These are DHARA acceptance limits, not provider accuracy guarantees. Older same-city weather/AQ responses cannot replace newer data. Missing simulation inputs still pause integration rather than silently becoming zero.

The retained ten-minute freshness indicator describes time since retrieval. Failed requests never advance successful retrieval time. Requests retain existing timeouts, cancellation, same-city cache retention and clearing on location changes. Weather/AQ automatic retries use bounded exponential delays; HTTP 429/503 Retry-After delta-seconds or HTTP-date is respected up to a one-day safety cap. Manual refresh also waits for that pending delay. Historical requests retain their two-attempt limit and respect the same server delay.

CAMS values and AQIs are model estimates, not local sensors. No specific gas clouds, measured vapour, physical visibility or calibrated flood prediction are claimed. Optional haze remains a capped illustrative PM2.5 effect. Rain, wind, water and lighting continue using the existing shared simulation state; What-if values do not overwrite the live API cache. IMD advisories remain unavailable without an authorized feed. Missing licensed SimplePoly source assets were not fabricated or downloaded from substitutes.

## Sources

- [Open-Meteo forecast definitions and WMO codes](https://open-meteo.com/en/docs)
- [Open-Meteo air quality fields, units and AQI scales](https://open-meteo.com/en/docs/air-quality-api)
- [Open-Meteo historical weather](https://open-meteo.com/en/docs/historical-weather-api)
- [Open-Meteo geocoding](https://open-meteo.com/en/docs/geocoding-api)

Real response bodies are retained locally in `TestResults/Weather`, `TestResults/AirQuality` and `Verification/api-history-response.json`. Public API availability and model accuracy are outside DHARA's control. No hard-coded success values or API credentials were introduced.

## Executed checks

- Unity 6000.3.26f1 compilation succeeded as part of the real test runs.
- Final Edit Mode: 124/124 passed (`Verification/api-editmode-release.xml`).
- Final full Play Mode: 151/151 passed (`Verification/api-playmode-release.xml`). Total: 275 distinct Unity cases. Reruns are not counted as extra cases.
- Earlier runtime run: 149/150 passed; the failed multi-failure fixture revealed that the first retry change delayed ordinary manual retries. This was repaired; manual throttling is now limited to HTTP 429 or HTTP 503 with an explicit Retry-After. The final full suite passed. Original failure evidence is retained in `Verification/api-playmode.xml`.
- Real HTTPS weather, units and geocoded coordinates: 5/5 (`Verification/api-weather.json`).
- Real HTTPS AQI/all six pollutant concentrations: 5/5 (`Verification/api-air-quality.json`). The separate UnityWebRequest suite also verified all eight returned AQ fields in all five cities (`Verification/api-unity-air-quality.txt`).
- ERA5: 24/24 ordered hourly records for the requested Bengaluru historical day (`Verification/api-history.json`). This is one day/city, not exhaustive archive coverage.
- Standalone core: 33/33 (`Verification/api-core.json`).
- Source validation: 51 C# files, five assembly definitions, required GUIDs/metas checked (`Verification/api-source.json`). This script is a syntax/configuration check, separate from actual Unity compilation.
- Actual-response CSV: 17 weather/air-quality fields round-tripped through a CSV reader, with original retrieval timestamps (`Verification/api-csv.json`, `Verification/DHARA_Environment_Bengaluru_API.csv`).

The CSV export writes a real UTF-8 CSV under the player's EnvironmentalData directory and opens that folder. It exports stored current API estimates, including missing/stale labels, independently of hypothetical scenario overrides. Existing scenario reports additionally retain optional weather code and interval metadata; old snapshots remain readable. PDF and browser-download claims are not made for this native CSV operation.

## Dashboard and Windows delivery

Both Air Quality and Weather pages were inspected at 1280×800, 1600×900 and 1920×1080. All six layout audits reported zero overflowing text and zero off-screen controls (`Verification/api-layouts.json`). Screenshots are in `Verification/Screenshots/API`. Actual advancing frame count was checked before inspection. The interactive Unity console reported zero errors and warnings (`Verification/api-console.json`).

The actual UI CSV button wrote a nonempty 17-field file; the copied result parsed correctly with units and source/valid/retrieval metadata (`Verification/api-ui-export.json`, `api-ui-csv-validation.json`, `api-ui-export.csv`). The screen-size QA script initially used Transform.Find on a button name containing a slash; that QA lookup was corrected to select the actual Button component, then all six audits completed. This was a verification-script lookup error, not a failed application button.

Separate final build: `Builds/Windows-API/DHARA.exe`.

Distributable ZIP: `Builds/DHARA-LivingCity-Windows-x64-20261016-040756.zip` (40,067,151 bytes). SHA-256: `44ACD258E10503E4034408242B677D16050B8E11FD6F9DE55F507C88090A0C12`.

Unity build exited 0 (`Verification/api-build.log`). Two nonfatal build warnings remain: the internal URP DebugOccluder vector truncation warning and Pipeline's expected disabled-in-player warning because no RuntimePipelineConfig is configured. No runtime automation server is shipped as enabled.

All 174 package-entry hashes and extracted companion files were verified (`Verification/api-package.json`, `api-extraction.json`). The new player was launched from `TestResults/Extracted-API-20261016-040756/DHARA/DHARA.exe`, not from the build staging directory. It displayed real Bengaluru weather and all eight AQI/pollutant values. The native Weather / air quality button switched pages correctly. The native Export data CSV button produced a second genuine 17-field CSV (`Verification/api-player-export.csv`, `api-player-smoke.json`). The process responded, exposed a real window, and its log contained no matched startup/runtime exceptions during this check. This is a smoke check, not an endurance or low-end performance guarantee. The older exploration build/package remains intact.

To launch: extract the complete ZIP and run `DHARA/DHARA.exe`. Keep all companion folders/files together. Internet access is required for API data; hypothetical simulation remains usable when requests fail. No Unity installation or sign-in is needed to run the Windows player.

To inspect/export values: City tools > Air quality / advisories > Weather / air quality. Export data CSV saves under `C:/Users/Roshan/AppData/LocalLow/DHARA/DHARA — A LIVING CITY/EnvironmentalData`. The current player was left open on the weather details page.

Remaining limits: provider outages/rate limits, coarse model grids and model uncertainty remain external constraints. Only the requested supported weather/AQ fields are integrated, not every variable in Open-Meteo's entire catalog. IMD advisories still need an authorized feed. The exact purchased asset files remain absent. This release does not add a web client, redeploy hosting, or add PDF/screenshot downloads; those broader attachment items are separate from the API integration delivered here.
