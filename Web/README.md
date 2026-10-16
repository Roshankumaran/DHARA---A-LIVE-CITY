# DHARA browser companion

This directory is the deployable website. The repository root remains the Unity project. The browser companion displays live weather, an hourly forecast selector, eight AQI/pollutant fields, provenance, error/retry states and a real CSV download. The 3D simulation, What-if Lab, tour and walk controls remain in the verified Unity Windows application. This is not a Unity WebGL build.

## Local run

Requires Node.js 24.x. No third-party npm dependencies or API keys are required.

```sh
cd Web
npm ci --ignore-scripts
npm run dev
```

Open http://127.0.0.1:5173. Set PORT to use another port. The validation session used http://127.0.0.1:5174 and left that server running.

```sh
npm run build
npm run preview
```

The build runs 27 data/CSV regression tests, then creates `dist/`. Preview serves that production directory. `scripts/serve.mjs` is only a local server; Vercel serves the static output directly.

## Vercel deployment

1. Import `Roshankumaran/DHARA---A-LIVE-CITY` from GitHub into Vercel.
2. Name the project `dhara` (or another available project name).
3. For the existing `dhara` project imported at the repository root, keep **Root Directory: .**. The root `vercel.json` installs and builds inside `Web` and publishes only `Web/dist`, preventing an empty deployment/404. Alternatively, a project rooted at **Web** uses `Web/vercel.json` directly; do not select `Hosting/dhara`.
4. Choose **Framework Preset: Other**, **Node.js: 24.x**. Leave command/output overrides disabled so the selected root's `vercel.json` supplies them. At the repository root these are **Install Command: npm --prefix Web ci --ignore-scripts**, **Build Command: npm --prefix Web run build**, **Output Directory: Web/dist**. With Root Directory `Web`, they are `npm ci --ignore-scripts`, `npm run build`, and `dist`. Both configurations include the response security headers.
5. No environment variables or secrets are required. Choose production branch `main`, then Deploy.
6. Verify the supplied Vercel URL: switch cities, select a forecast hour, download a CSV, and follow the Windows release link.
7. After the deployment is ready, add `dhara.aftanfaiz.in` in Vercel Project Settings > Domains. Copy the exact DNS record Vercel shows into Hostinger's DNS panel. Use `dhara` as the subdomain host where Hostinger requests a relative name. Resolve conflicting existing records for that same host only when intentionally switching it to the new deployment. Keep unrelated apex/domain/email records intact. Wait for Vercel to confirm DNS and HTTPS.

GitHub pushes to the connected production branch trigger Vercel builds. Verify the deployment status and public URL after each release; a successful build alone does not confirm the served page or DNS. DNS changes require access to the domain provider.

The project has one document with section anchors, so no SPA catch-all rewrite is needed. Unknown asset paths should remain 404. `vercel.json` permits only the two required API origins for fetches. The browser connects directly to public Open-Meteo endpoints; no proxy is needed for the verified workflow. Provider outages and rate limits are surfaced, not hidden behind fabricated data. Browser network access is required.

## API meaning

The nine weather fields and eight air-quality fields match the supported Unity API integration. `data.js` handles transport/schema validation for presentation; it does not duplicate the district hydrology model. Current rain is converted from the returned aggregation interval to mm/h. Hourly rain is the preceding-hour amount expressed as a mean rate. Precipitation remains its separate total; shortwave radiation is an interval average. Wind direction is FROM. All selected cities use IST. Valid and successful retrieval times remain distinct.

Air quality is CAMS global model output. US and European AQI are different scales, and neither is Indian NAQI. CSV exports label a selected hourly forecast separately from current air quality, retaining each group's original valid/retrieval times. Null values do not become zero. Ten-minute stale indicators describe time since retrieval. Outdated/future current responses, invalid units/types/ranges, malformed arrays and unknown WMO codes are rejected. Same-city request failures retain prior values; switching cities clears them and aborts superseded requests. Automatic retries back off from 15 seconds up to five minutes and honor server Retry-After, capped at one day. Browser requests time out at 20 seconds.

## Windows release

`release.json` points to the tagged GitHub release ZIP and publishes its SHA-256 and byte size. Keep Windows binaries in GitHub Releases rather than committing Unity build folders to Git or trying to execute Windows binaries on Vercel. Update that manifest only after verifying the new downloadable release.

## Tests and evidence

`npm test` uses genuine previously captured responses as offline test fixtures, plus explicitly mutated invalid test cases. Fixtures are never loaded by the live dashboard and are excluded from `dist/`. No live API success is fabricated.

The local browser loaded real weather/AQI for all five cities, selected an actual future hour and downloaded a 17-field CSV. An initial genuine weather HTTP 503 was displayed and recovered automatically. Mobile 390px and desktop 1280px viewport checks showed no horizontal page overflow. Evidence and screenshots are under the repository's `Verification/` directory. The existing Unity results remain 124 Edit Mode + 151 Play Mode = 275 passed cases; Unity source has not been changed during this web preparation.

References: [Vercel build settings](https://vercel.com/docs/builds/configure-a-build), [Vercel Node.js versions](https://vercel.com/docs/functions/runtimes/node-js/node-js-versions), [custom domains](https://vercel.com/docs/domains/working-with-domains/add-a-domain), [Open-Meteo weather](https://open-meteo.com/en/docs), [air quality](https://open-meteo.com/en/docs/air-quality-api).
