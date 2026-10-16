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
        $data=[Dhara.ApiValidation]::Forecast($response.Content,[DateTime]::UtcNow)
        $current=$data.current.Convert()
        if(-not $current.HasSimulationInputs -or $data.hourly.time.Length -lt 24) { throw 'Weather response lacks required fields' }
        $store=[Dhara.WeatherDataStore]::new();$store.Accept($data,[DateTime]::UtcNow)
        if($store.Forecast.Count -eq 0 -or $store.Forecast[0].Time -lt $current.Time) {throw 'No valid future forecast after production filtering'}
        $parsed=$response.Content | ConvertFrom-Json
        $expectedUnits=@{temperature_2m='°C';wind_speed_10m='km/h';wind_direction_10m='°';precipitation='mm';rain='mm';cloud_cover='%';relative_humidity_2m='%';shortwave_radiation='W/m²';weather_code='wmo code'}
        foreach($key in $expectedUnits.Keys) {if($parsed.current_units.$key -ne $expectedUnits[$key] -or $parsed.hourly_units.$key -ne $expectedUnits[$key]) {throw "Incorrect or missing units for $key"}}
        if($response.Content.Contains('"error":true')) { throw 'API returned error' }
        $searchName=if($location.Name -eq 'Delhi') {'New Delhi'} else {$location.Name}
        $geo=Invoke-RestMethod -Uri ("https://geocoding-api.open-meteo.com/v1/search?name="+[Uri]::EscapeDataString($searchName)+'&count=10&language=en&format=json') -TimeoutSec 25
        $place=$geo.results | Where-Object country_code -eq 'IN' | Select-Object -First 1
        if(-not $place) { throw 'No matching Indian geocoding result' }
        if([Math]::Abs($place.latitude-$location.Latitude) -gt .01 -or [Math]::Abs($place.longitude-$location.Longitude) -gt .01) {throw 'Configured coordinates differ from geocoded location by more than 0.01 degrees'}
        $geo | ConvertTo-Json -Depth 12 | Set-Content "TestResults/Weather/$($location.Name)-geocoding.json"
        $results+=@{location=$location.Name;passed=$true;httpStatus=[int]$response.StatusCode;parsed=$true;retrievedUtc=$store.Status.RetrievedUtc.ToString('o');timestamp=$current.Time;temperatureC=$current.Temperature;precipitationMm=$current.PrecipitationMm;rainRateMmPerHour=$current.RainMmPerHour;windKmh=$current.WindKmh;windDegrees=$current.WindDegrees;cloudPercent=$current.CloudPercent;missingFields=@($expectedUnits.Keys | Where-Object { $null -eq $parsed.current.$_ });intervalSeconds=$data.current.interval;forecastHours=$data.hourly.time.Length;futureForecastHours=$store.Forecast.Count;unitsVerified=$true;coordinatesVerified=$true;geocodedLatitude=$place.latitude;geocodedLongitude=$place.longitude;requestedLatitude=$location.Latitude;requestedLongitude=$location.Longitude}
        Write-Output "PASS $($location.Name): valid $($current.Time), $($data.hourly.time.Length) hourly / $($store.Forecast.Count) future records; units and coordinates verified"
    } catch { $results+=@{location=$location.Name;passed=$false;error=$_.Exception.Message};Write-Output "FAIL $($location.Name): $_" }
}
@{executedUtc=[DateTime]::UtcNow.ToString('o');scope='Real HTTPS requests, production C# parser/store, units and coordinates; not UnityWebRequest execution';results=$results} | ConvertTo-Json -Depth 8 | Set-Content 'TestResults/weather-results.json'
if(($results | Where-Object {-not $_.passed}).Count -gt 0) {exit 1}
