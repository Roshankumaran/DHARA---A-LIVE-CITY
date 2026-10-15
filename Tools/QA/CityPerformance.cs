var app=UnityEngine.Object.FindFirstObjectByType<Dhara.DharaApplication>();
var elapsed=new System.Collections.Generic.List<double>();long allocated=0;int count=0;
var config=new Dhara.InterventionConfig{GreenRoofFraction=1,PermeableFraction=1,ExtraDrainage=20,GardenCapacityM3=20};
foreach(var warmup in Dhara.InterventionPlanner.Evaluate(50,12,config)){}
for(int run=0;run<10;run++){
long before=System.GC.GetAllocatedBytesForCurrentThread();var timer=System.Diagnostics.Stopwatch.StartNew();count=0;
foreach(var candidate in Dhara.InterventionPlanner.Evaluate(50,12,config))count++;
timer.Stop();elapsed.Add(timer.Elapsed.TotalMilliseconds);allocated+=System.GC.GetAllocatedBytesForCurrentThread()-before;
}
return new {scope="Unity Editor warm benchmark; ten complete searches, not an FPS or laptop guarantee",casesPerSearch=count,minimumMs=elapsed.Min(),meanMs=elapsed.Average(),maximumMs=elapsed.Max(),meanAllocatedBytes=allocated/10,spatialObjects=app.Spatial.Objects.Count,meshRenderers=app.World.GetComponentsInChildren<UnityEngine.MeshRenderer>().Length,connectionLines=app.World.GetComponentsInChildren<UnityEngine.LineRenderer>().Length,maximumRainParticles=app.World.GetComponentInChildren<UnityEngine.ParticleSystem>().main.maxParticles,unityAllocatedBytes=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()};
