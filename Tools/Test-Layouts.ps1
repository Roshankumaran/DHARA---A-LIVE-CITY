param([int]$Width=1600,[int]$Height=900)
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Force TestResults | Out-Null
function Command([string]$name,[string[]]$arguments){$raw=& unity command $name --project-path . --caller plugin --skill unity-cli --format json -- @arguments; $r=$raw|ConvertFrom-Json;if(-not $r.success -or ($r.data.result.success -eq $false)){throw $raw};return $r.data.result}
$code=(Get-Content Tools/QA/SetResolution.cs -Raw).Replace('1600',$Width.ToString()).Replace('900',$Height.ToString());Set-Content TestResults/CityResolution.cs $code
Command 'eval_file' @('--file','TestResults/CityResolution.cs')|Out-Null
$records=@();$views=@('Live','Forecast','WhatIf','Studio','Assumptions','Trace','Planner','Timeline','DataSources','CityComparison','ScenarioFiles','Demo','Sensitivity','ObjectInspector')
foreach($view in $views){
$code=@'
var app=UnityEngine.Object.FindFirstObjectByType<Dhara.DharaApplication>();var ui=app.GetComponent<Dhara.EnvironmentalDashboard>();
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
System.Action<string> invoke=name=>typeof(Dhara.EnvironmentalDashboard).GetMethod(name,flags).Invoke(ui,null);
ui.StopDemo();invoke("CloseTool");invoke("CloseStudioDetail");invoke("CloseExperiment");
app.transform.Find("Environmental monitoring dashboard/Neighborhood Resilience Studio").gameObject.SetActive(false);
app.SetMode(Dhara.WorldMode.WhatIf);app.Simulation.Scenario.RainMmPerHour=50;app.Simulation.Scenario.WindKmh=30;app.Simulation.Scenario.Temperature=25;app.ApplyInputs();
'@
switch($view){
'Live'{$code+='app.SetMode(Dhara.WorldMode.Live);'}
'Forecast'{$code+='app.SetMode(Dhara.WorldMode.Forecast);app.SelectForecast(2);'}
'WhatIf'{}
'Studio'{$code+='ui.OpenStudio();app.transform.Find("Environmental monitoring dashboard/Neighborhood Resilience Studio/GREEN ROOFS/Track").GetComponent<UnityEngine.UI.Slider>().value=50;app.transform.Find("Environmental monitoring dashboard/Neighborhood Resilience Studio/PERMEABLE PAVING/Track").GetComponent<UnityEngine.UI.Slider>().value=50;'}
'Assumptions'{$code+='ui.OpenStudio();typeof(Dhara.EnvironmentalDashboard).GetMethod("ShowStudioInformation",flags).Invoke(ui,new object[]{Dhara.DistrictHydrology.Assumptions});'}
'Trace'{$code+='ui.OpenTrace();'}
'Planner'{$code+='invoke("OpenPlanner");app.transform.Find("Environmental monitoring dashboard/INTERVENTION PLANNER/Evaluate").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();'}
'Timeline'{$code+='invoke("OpenTimeline");'}
'DataSources'{$code+='invoke("OpenDataSources");'}
'CityComparison'{$code+='invoke("OpenCityComparison");'}
'ScenarioFiles'{$code+='invoke("OpenScenarioFiles");'}
'Demo'{$code+='ui.StartDemo(true);ui.AdvanceDemo();ui.AdvanceDemo();ui.AdvanceDemo();'}
'Sensitivity'{$code+='invoke("OpenSensitivity");app.transform.Find("Environmental monitoring dashboard/ENVIRONMENTAL SENSITIVITY ANALYSIS/Evaluate range").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();'}
'ObjectInspector'{$code+='app.Spatial.SelectObject("roof-1");'}
}
$code+='UnityEngine.Canvas.ForceUpdateCanvases();return true;'
Command 'eval' @('--code',$code)|Out-Null
if($view -eq 'Planner'){Start-Sleep -Seconds 5}
$audit=Command 'eval_file' @('--file','Tools/QA/LayoutAudit.cs');$records+=@{view=$view;layout=$audit.result}
Command 'capture_game_view' @('--source','screen','--save_path',"TestResults/city-$view-$Width.png",'--width',"$Width",'--height',"$Height")|Out-Null
Write-Output "$Width $view : overflow=$($audit.result.overflow.Count), outside=$($audit.result.controlsOutside.Count)"
}
$records|ConvertTo-Json -Depth 15|Set-Content "Verification/city-layout-$Width.json"



