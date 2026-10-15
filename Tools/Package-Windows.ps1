param([string]$BuildDirectory='Builds/Windows',[string]$ReportPath='Verification/windows-package.json')
$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$buildRoot=(Resolve-Path $BuildDirectory).Path
foreach($required in @('DHARA.exe','UnityPlayer.dll','DHARA_Data','MonoBleedingEdge')) {
    if(-not(Test-Path -LiteralPath (Join-Path $buildRoot $required))) {throw "Build companion missing: $required"}
}
$executable=Join-Path $buildRoot 'DHARA.exe'
$pe=[IO.File]::ReadAllBytes($executable)
$offset=[BitConverter]::ToInt32($pe,0x3c)
if([BitConverter]::ToUInt32($pe,$offset) -ne 0x4550 -or [BitConverter]::ToUInt16($pe,$offset+4) -ne 0x8664) {throw 'Expected an AMD64 Windows executable'}
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$stage=Join-Path (Get-Location) "TestResults/Package-$stamp/DHARA"
New-Item -ItemType Directory -Path $stage -Force | Out-Null
Get-ChildItem -LiteralPath $buildRoot | Where-Object Name -NotLike '*DoNotShip*' | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $stage -Recurse
}
@'
DHARA — A LIVING CITY
Windows 64-bit / Unity 6000.3.26f1 / Mono / URP

Extract this complete folder, then double-click DHARA.exe.
Keep DHARA_Data, UnityPlayer.dll, MonoBleedingEdge and all companion files together.
No Unity installation is needed to run this player.
Internet access supplies live and forecast weather from Open-Meteo.

Select a city in the header. WHAT-IF LAB controls rainfall, wind, temperature
and drainage. Reset scenario restores the available live baseline. Return to
live clears hypothetical overrides. FORECAST PREVIEW selects actual returned
hourly records. Right drag: orbit; middle drag/WASD: pan; wheel: zoom; Home: overview.

Urban Intervention Lab captures the current inputs as a frozen baseline. Adjust
extra drainage to compare one simulated hour from zero storage; reset returns
the added capacity to zero. It does not modify live weather or the running scenario.
Resilience Studio evaluates green roofs, permeable paving, drainage and garden storage.
City tools provide trace, planner, ERA5 history, forecast impacts, air quality,
five-city comparison, local snapshots/JSON/CSV reports, guided demonstration and sensitivity.
District water balance: depth * area / 1000 = volume; all runoff routes to collection.
Collection capacity 150 m3; green roof retention 20 mm; permeable infiltration 15 mm/h;
green strips 8 mm/h; drainage removal 0.8 * nominal capacity * catchment area / 1000.
Initially dry, simplified conceptual catchment; inspect full assumptions in Studio.

Failures and stale data are labelled. The hypothetical lab is available offline,
with a stated cloud assumption and unavailable solar/humidity/direction.
Weather-model estimates are not physical sensor readings. Water accumulation
and solar output use simplified rules, not validated flood/energy predictions.
The district is generic and does not reconstruct the selected city.

Weather source: Open-Meteo (https://open-meteo.com), CC BY 4.0.
License: https://creativecommons.org/licenses/by/4.0/
Uses temperature, rain, precipitation, wind, cloud, humidity and solar radiation.
'@ | Set-Content -LiteralPath (Join-Path $stage 'RUN-DHARA.txt') -Encoding utf8
$manifest=@(Get-ChildItem -LiteralPath $stage -File -Recurse | ForEach-Object {
    @{path=[IO.Path]::GetRelativePath($stage,$_.FullName);bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
})
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $stage 'SHA256SUMS.json')
$archive=Join-Path (Get-Location) "Builds/DHARA-LivingCity-Windows-x64-$stamp.zip"
Compress-Archive -LiteralPath $stage -DestinationPath $archive
$zip=[IO.Compression.ZipFile]::OpenRead($archive)
try {
    if($zip.Entries.Count -lt $manifest.Count) {throw 'Package entries are incomplete'}
    foreach($file in $manifest) {
        $entry=$zip.GetEntry('DHARA/'+$file.path.Replace('\','/'))
        if($null -eq $entry -or $entry.Length -ne $file.bytes) {throw "Package size mismatch: $($file.path)"}
        $stream=$entry.Open();$hash=[Security.Cryptography.SHA256]::Create()
        try {$actual=[Convert]::ToHexString($hash.ComputeHash($stream))} finally {$stream.Dispose();$hash.Dispose()}
        if($actual -ne $file.sha256) {throw "Package hash mismatch: $($file.path)"}
    }
} finally {$zip.Dispose()}
@{createdUtc=[DateTime]::UtcNow.ToString('o');archive=$archive;archiveSha256=(Get-FileHash -LiteralPath $archive).Hash;architecture='AMD64';filesVerified=$manifest.Count;allEntryHashesVerified=$true;stage=$stage;bytes=(Get-Item -LiteralPath $archive).Length} | ConvertTo-Json | Set-Content -LiteralPath $ReportPath
Write-Output "PASS Windows AMD64 package: $archive ($($manifest.Count) verified file hashes)"
