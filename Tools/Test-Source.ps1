$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
Add-Type 'public class DharaSourceValidationBootstrap {}'
$issues=[Collections.Generic.List[string]]::new()
$sourceCount=0
Get-ChildItem 'Assets' -Recurse -Filter '*.cs' | ForEach-Object {
    $sourceCount++
    $tree=[Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText((Get-Content -LiteralPath $_.FullName -Raw))
    foreach($diagnostic in $tree.GetDiagnostics()) {if($diagnostic.Severity -eq 'Error') {$issues.Add($_.Name+': '+$diagnostic.ToString())}}
    if($_.FullName.Contains('Dhara\Runtime')) {
        foreach($token in $tree.GetRoot().DescendantTokens()) {
            if($token.ValueText -match '\b(AI|LLM|Claude|neural|chatbot|copilot)\b' -and $token.RawKind -in @(8511,8517)) {$issues.Add('Prohibited visible branding: '+$_.Name+': '+$token.ValueText)}
        }
    }
}
$assemblyNames=@('Unity.TextMeshPro','UnityEngine.UI','Unity.InputSystem','Unity.RenderPipelines.Core.Runtime','Unity.RenderPipelines.Universal.Runtime')
$assemblies=@(Get-ChildItem 'Assets' -Recurse -Filter '*.asmdef' | ForEach-Object {Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json})
$assemblyNames+=@($assemblies | ForEach-Object name)
foreach($assembly in $assemblies) {foreach($reference in $assembly.references) {if($reference -notin $assemblyNames) {$issues.Add('Unknown assembly reference: '+$reference)}}}
$guidMap=@{}
Get-ChildItem 'Assets' -Recurse -Filter '*.meta' | ForEach-Object {
    $match=[regex]::Match((Get-Content -LiteralPath $_.FullName -Raw),'(?m)^guid: ([a-f0-9]{32})\s*$')
    if(-not $match.Success) {$issues.Add('Missing GUID: '+$_.FullName);return}
    $guid=$match.Groups[1].Value
    if($guidMap.ContainsKey($guid)) {$issues.Add('Duplicate GUID: '+$guid)}else{$guidMap[$guid]=$_.FullName}
}
foreach($guid in @('0471249ae03741b7ac613be5c482d908','70f8de0b65bc4baaa160a524371e6912','6e6adc7696864ee68f80353cc0d5a9e5','a49efddf165f4773bcce7c7c71ac836e','4b83569d67af61e458304325a23e5dfd')) {if(-not $guidMap.ContainsKey($guid)) {$issues.Add('Missing required scene/material/pipeline GUID: '+$guid)}}
Get-ChildItem 'Assets' -Recurse | Where-Object Extension -ne '.meta' | ForEach-Object {if(-not(Test-Path -LiteralPath ($_.FullName+'.meta'))) {$issues.Add('Missing meta: '+$_.FullName)}}
$scene=Get-Content 'Assets/Scenes/Dhara.unity' -Raw
foreach($guid in @('0471249ae03741b7ac613be5c482d908','70f8de0b65bc4baaa160a524371e6912','6e6adc7696864ee68f80353cc0d5a9e5')) {if(-not $scene.Contains($guid)) {$issues.Add('Scene reference absent: '+$guid)}}
New-Item -ItemType Directory -Path 'TestResults' -Force | Out-Null
@{executedUtc=[DateTime]::UtcNow.ToString('o');scope='Syntax and source checks, not Unity compilation or rendering';sourceFiles=$sourceCount;assemblies=$assemblies.Count;issues=$issues.ToArray();passed=($issues.Count -eq 0)} | ConvertTo-Json -Depth 6 | Set-Content 'TestResults/source-results.json'
if($issues.Count) {$issues | Write-Output;exit 1}
Write-Output "PASS $sourceCount C# files: syntax, $($assemblies.Count) assembly definitions, asset GUIDs/metas, runtime branding strings. Unity compilation remains unverified."
