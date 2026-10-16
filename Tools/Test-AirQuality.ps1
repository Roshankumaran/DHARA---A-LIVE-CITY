$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
Add-Type -Path (Get-ChildItem 'Assets/Dhara/Core/*.cs').FullName
New-Item -ItemType Directory -Force 'TestResults/AirQuality' | Out-Null
$results=@()
foreach($location in [Dhara.Location]::All) {
    try {
        $response=Invoke-WebRequest -Uri ([Dhara.AirQualityData]::Url($location)) -TimeoutSec 25
        $response.Content | Set-Content -Encoding utf8 "TestResults/AirQuality/$($location.Name).json"
        $data=[Dhara.AirQualityData]::Parse($response.Content)
        [Dhara.ApiValidation]::Fresh($data.Time,[DateTime]::UtcNow,[TimeSpan]::FromHours(3))
        $status=[Dhara.DataSourceStatus]::new();$status.RetrievedUtc=[DateTime]::UtcNow
        $fields=@([Dhara.EnvironmentalParameters]::AirQuality($data,$status))
        if(($fields|Where-Object {$_.Availability -eq 'Available'}).Count -eq 0){throw 'No air-quality model estimates returned'}
        $results+=@{city=$location.Name;passed=$true;httpStatus=[int]$response.StatusCode;fields=$fields}
        Write-Output "PASS $($location.Name): $($data.Time) IST; US AQI $($data.UsAqi), European AQI $($data.EuropeanAqi)"
    }catch{$results+=@{city=$location.Name;passed=$false;error=$_.Exception.Message};Write-Output "FAIL $($location.Name): $_"}
}
@{executedUtc=[DateTime]::UtcNow.ToString('o');scope='Real HTTPS and production C# parser; CAMS model estimates, not sensors';results=$results}|ConvertTo-Json -Depth 9|Set-Content -Encoding utf8 'Verification/api-air-quality.json'
if(($results|Where-Object {-not $_.passed}).Count -gt 0){exit 1}
