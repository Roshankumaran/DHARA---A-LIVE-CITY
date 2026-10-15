var app=UnityEngine.Object.FindFirstObjectByType<Dhara.DharaApplication>();
var canvas=app.GetComponentInChildren<UnityEngine.Canvas>();var renderWidth=canvas.pixelRect.width;var renderHeight=canvas.pixelRect.height;var texts=UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(UnityEngine.FindObjectsSortMode.None);
var outside=new System.Collections.Generic.List<string>();
foreach(var button in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Selectable>(UnityEngine.FindObjectsSortMode.None)) {
 var corners=new UnityEngine.Vector3[4];((UnityEngine.RectTransform)button.transform).GetWorldCorners(corners);
 if(corners.Any(p=>p.x<-.5f||p.x>renderWidth+.5f||p.y<-.5f||p.y>renderHeight+.5f))outside.Add(button.name);
}
return new {width=renderWidth,height=renderHeight,frame=UnityEngine.Time.frameCount,mode=app.Simulation.Mode.ToString(),weatherValid=app.Weather.Current?.Time,loading=app.Weather.Status.Loading,error=app.Weather.Status.Error,viewport=app.World.ViewCamera.pixelRect.ToString(),overflow=texts.Where(t=>t.isTextOverflowing).Select(t=>new{t.name,t.text}).ToArray(),controlsOutside=outside.ToArray()};

