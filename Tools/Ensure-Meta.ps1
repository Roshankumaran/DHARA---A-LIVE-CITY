$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets') -Recurse | Where-Object Extension -ne '.meta' | ForEach-Object {
    $metaPath=$_.FullName+'.meta'
    if(Test-Path -LiteralPath $metaPath) {return}
    $relative=[IO.Path]::GetRelativePath($projectRoot,$_.FullName).Replace('\','/')
    $hash=[Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes('DHARA/'+$relative))
    $guid=([Convert]::ToHexString($hash)).ToLowerInvariant().Substring(0,32)
    $lines=@('fileFormatVersion: 2',"guid: $guid")
    if($_.PSIsContainer) {$lines+=@('folderAsset: yes','DefaultImporter:','  externalObjects: {}')}
    elseif($_.Extension -eq '.cs') {$lines+=@('MonoImporter:','  externalObjects: {}','  serializedVersion: 2','  defaultReferences: []','  executionOrder: 0','  icon: {instanceID: 0}')}
    elseif($_.Extension -eq '.asmdef') {$lines+=@('AssemblyDefinitionImporter:','  externalObjects: {}')}
    else {$lines+=@('DefaultImporter:','  externalObjects: {}')}
    Set-Content -LiteralPath $metaPath -Value $lines
}
