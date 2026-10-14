$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
Add-Type -Path 'Assets/Dhara/Core/EnvironmentModel.cs','Assets/Dhara/Core/WeatherModels.cs','Assets/Dhara/Core/WeatherJson.cs'
$results = [System.Collections.Generic.List[object]]::new()
function Check([string]$name,[scriptblock]$test) {
    try { & $test; $results.Add([pscustomobject]@{name=$name;passed=$true}); Write-Output "PASS $name" }
    catch { $results.Add([pscustomobject]@{name=$name;passed=$false;error=$_.Exception.Message}); Write-Output "FAIL $name : $_" }
}
function Assert([bool]$condition,[string]$message='Assertion failed') { if(-not $condition) { throw $message } }
function Weather([double]$rain=10,[double]$wind=12,[double]$cloud=20) {
    $data=[Dhara.WeatherData]::new(); $data.RainMmPerHour=$rain; $data.WindKmh=$wind; $data.CloudPercent=$cloud; $data.Temperature=25; return $data
}
Check 'Rain increases emission' { $a=[Dhara.CausalEnvironmentEngine]::new();$b=[Dhara.CausalEnvironmentEngine]::new();$a.Step((Weather 2),12,6);$b.Step((Weather 40),12,6);Assert ($b.State.RainIntensity -gt $a.State.RainIntensity) }
Check 'Rain cannot reduce water' { Assert ([Dhara.WaterAccumulationModel]::Step(20,40,12,25,600) -ge [Dhara.WaterAccumulationModel]::Step(20,10,12,25,600)) }
Check 'Lower drainage retains more water' { Assert ([Dhara.WaterAccumulationModel]::Step(20,40,0,25,600) -gt [Dhara.WaterAccumulationModel]::Step(20,40,20,25,600)) }
Check 'Wind increases effect intensity' { $a=[Dhara.CausalEnvironmentEngine]::new();$a.Step((Weather 0 5),12,6);$before=$a.State.WindIntensity;$a.Step((Weather 0 70),12,6);Assert ($a.State.WindIntensity -gt $before) }
Check 'Clouds reduce lighting' { $a=[Dhara.CausalEnvironmentEngine]::new();$a.Step((Weather 0 0 0),12,6);$before=$a.State.LightIntensity;$a.Step((Weather 0 0 100),12,6);Assert ($a.State.LightIntensity -lt $before) }
Check 'Scenario reset restores live baseline' { $s=[Dhara.SimulationState]::new();$s.Live=Weather 8 22;$s.Mode=[Dhara.WorldMode]::WhatIf;$s.Scenario.RainMmPerHour=90;$s.ResetScenario();Assert ($s.Active.RainMmPerHour -eq 8 -and $s.Active.WindKmh -eq 22 -and $s.Drainage -eq 12) }
Check 'Missing input pauses safely' { $e=[Dhara.CausalEnvironmentEngine]::new();$e.Step($null,12,6);$e.Step([Dhara.WeatherData]::new(),12,6);Assert ($e.State.ElapsedSimulatedSeconds -eq 0) }
Check 'Water bounded under 10000 extreme updates' { $water=0;for($i=0;$i -lt 10000;$i++) {$water=[Dhara.WaterAccumulationModel]::Step($water,200,0,50,3600)};Assert ($water -ge 0 -and $water -le 150) }
Check 'Nonfinite inputs are sanitized' { $water=[Dhara.WaterAccumulationModel]::Step([double]::NaN,[double]::PositiveInfinity,0,25,6);Assert (-not [double]::IsNaN($water) -and $water -ge 0 -and $water -le 150) }
Check 'Dry weather removes water' { Assert ([Dhara.WaterAccumulationModel]::Step(20,0,12,25,600) -lt 20) }
Check 'Identical inputs produce deterministic state' { $a=[Dhara.CausalEnvironmentEngine]::new();$b=[Dhara.CausalEnvironmentEngine]::new();for($i=0;$i -lt 100;$i++) {$a.Step((Weather),12,6);$b.Step((Weather),12,6)};Assert ($a.State.WaterMm -eq $b.State.WaterMm -and $a.State.Wetness -eq $b.State.Wetness) }
Check 'Current rainfall interval conversion' { $r=[Dhara.WeatherJson]::Parse('{"current":{"time":"2026-01-01T12:00","interval":900,"rain":2}}');Assert ($r.current.Convert().RainMmPerHour -eq 8) }
Check 'Missing and null are not zero' { $r=[Dhara.WeatherJson]::Parse('{"current":{"time":"2026-01-01T12:00","rain":null}}');Assert ($null -eq $r.current.Convert().RainMmPerHour -and $null -eq $r.current.Convert().WindKmh) }
Check 'Forecast index preserves value and timestamp' { $r=[Dhara.WeatherJson]::Parse('{"hourly":{"time":["2026-01-01T12:00","2026-01-01T13:00"],"rain":[1,4]}}');Assert ($r.hourly.At(1).Time -eq '2026-01-01T13:00' -and $r.hourly.At(1).RainMmPerHour -eq 4 -and $null -eq $r.hourly.At(2)) }
Check 'Failed request status remains available' { $s=[Dhara.DataSourceStatus]::new();$s.Error='Network unavailable';Assert ($s.Label.Contains('Request failed') -and -not $s.Stale) }
Check 'Stale retrieval detected' { $s=[Dhara.DataSourceStatus]::new();$s.RetrievedUtc=[DateTime]::UtcNow.AddMinutes(-11);Assert $s.Stale }
Check 'Unchanged inputs do not produce duplicate events' { $e=[Dhara.CausalEnvironmentEngine]::new();for($i=0;$i -lt 100;$i++) {$e.Step((Weather),12,6)};Assert ($e.Events.Count -eq 0);for($i=0;$i -lt 100;$i++) {$e.Record($i.ToString())};Assert ($e.Events.Count -eq 30) }
Check 'Small slider changes eventually log' { $e=[Dhara.CausalEnvironmentEngine]::new();for($i=0;$i -lt 20;$i++) {$e.Step((Weather ($i*.1)),12,0)};Assert ($e.Events.Count -gt 0) }
Check 'Offline scenario has explicit cloud baseline' { $s=[Dhara.SimulationState]::new();$s.Mode=[Dhara.WorldMode]::WhatIf;$s.ResetScenario();Assert ($s.Active.HasSimulationInputs -and $null -eq $s.Active.SolarWm2 -and $s.Active.CloudPercent -eq 50) }
Check 'Strict JSON rejects malformed responses' { foreach($json in @('{','{"x":01}','{"x":NaN}','{"x":1,}','[] trailing','{"x":1,"x":2}')) { $failed=$false;try {[void][Dhara.WeatherJson]::Parse($json)}catch {$failed=$true};Assert $failed $json } }
Check 'JSON escapes and numeric exponents' { $r=[Dhara.WeatherJson]::Parse('{"current":{"time":"line\n\u0041","rain":2e-1,"interval":900}}');Assert ($r.current.time -eq "line`nA" -and [Math]::Abs($r.current.Convert().RainMmPerHour-.8) -lt .000001) }
New-Item -ItemType Directory -Path 'TestResults' -Force | Out-Null
@{ executedUtc=[DateTime]::UtcNow.ToString('o');scope='Compiled production core via PowerShell Add-Type; not Unity Play Mode';results=$results.ToArray() } | ConvertTo-Json -Depth 6 | Set-Content 'TestResults/core-results.json'
if(($results | Where-Object { -not $_.passed }).Count -gt 0) { exit 1 }
Write-Output "$($results.Count) core checks passed."
