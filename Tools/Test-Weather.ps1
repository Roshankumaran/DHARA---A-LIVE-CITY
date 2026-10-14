$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
Add-Type -Path (Get-ChildItem 'Assets/Dhara/Core/*.cs').FullName
New-Item -ItemType Directory -Path 'TestResults/Weather' -Force | Out-Null
$results=@()
foreach($location in [Dhara.Location]::All) {
    try {
        $url=[Dhara.WeatherRequest]::Url($location)
        $response=Invoke-WebRequest -Uri $url -TimeoutSec 25
        $response.Content | Set-Content "TestResults/Weather/$($location.Name).json"
        $data=[Dhara.WeatherJson]::Parse($response.Content)
        $current=$data.current.Convert()
        if(-not $current.HasSimulationInputs -or $data.hourly.time.Length -lt 24) { throw 'Weather response lacks required fields' }
        if($response.Content.Contains('"error":true')) { throw 'API returned error' }
        $searchName=if($location.Name -eq 'Delhi') {'New Delhi'} else {$location.Name}
        $geo=Invoke-RestMethod -Uri ("https://geocoding-api.open-meteo.com/v1/search?name="+[Uri]::EscapeDataString($searchName)+'&count=10&language=en&format=json') -TimeoutSec 25
        $place=$geo.results | Where-Object country_code -eq 'IN' | Select-Object -First 1
        if(-not $place) { throw 'No matching Indian geocoding result' }
        $geo | ConvertTo-Json -Depth 12 | Set-Content "TestResults/Weather/$($location.Name)-geocoding.json"
        $results+=@{location=$location.Name;passed=$true;timestamp=$current.Time;intervalSeconds=$data.current.interval;forecastHours=$data.hourly.time.Length;geocodedLatitude=$place.latitude;geocodedLongitude=$place.longitude;requestedLatitude=$location.Latitude;requestedLongitude=$location.Longitude}
        Write-Output "PASS $($location.Name): valid $($current.Time), $($data.hourly.time.Length) forecast hours; geocode $($place.latitude), $($place.longitude)"
    } catch { $results+=@{location=$location.Name;passed=$false;error=$_.Exception.Message};Write-Output "FAIL $($location.Name): $_" }
}
@{executedUtc=[DateTime]::UtcNow.ToString('o');scope='Real HTTPS requests and production C# parser; not UnityWebRequest execution';results=$results} | ConvertTo-Json -Depth 8 | Set-Content 'TestResults/weather-results.json'
if(($results | Where-Object {-not $_.passed}).Count -gt 0) {exit 1}
