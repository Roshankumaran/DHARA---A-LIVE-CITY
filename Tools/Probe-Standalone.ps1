param([int]$PlayerId,[string]$OutputPath)
$ErrorActionPreference='Stop'
$playerProbe=Get-Process -Id $PlayerId
$playerProbe.Refresh()
[ordered]@{processId=$PlayerId;responding=$playerProbe.Responding;hasWindow=($playerProbe.MainWindowHandle -ne 0);title=$playerProbe.MainWindowTitle;checkedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath $OutputPath -Encoding utf8
