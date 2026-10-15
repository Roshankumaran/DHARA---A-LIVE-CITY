using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dhara
{
    [Serializable] public sealed class CitySettings
    {
        public int Seed = 47;
        [Range(4,12)] public float MinimumHeight = 6;
        [Range(12,26)] public float MaximumHeight = 19;
        [Range(0,1)] public float TreeDensity = .8f;
        [Range(0,2)] public float PlacementJitter = .75f;
    }
    public sealed class ProceduralCity : MonoBehaviour
    {
        public CitySettings Settings = new CitySettings();
        public Material SurfaceTemplate;
        public Material ParticleTemplate;
        public readonly List<Transform> Trees = new List<Transform>();
        readonly List<Material> generatedMaterials = new List<Material>();
        Material asphalt, water;
        Transform waterSurface;
        Light sunlight;
        Material windowGlass, lampMaterial;
        VolumeProfile generatedProfile;
        readonly List<Transform> channelWater=new List<Transform>();
        ParticleSystem rain;
        float visualWetness;
        float visualWind;
        Vector2 visualDrift;
        public Camera ViewCamera { get; private set; }
        Material Material(string name, Color color, float smoothness = .1f)
        {
            var template = SurfaceTemplate != null ? SurfaceTemplate : GraphicsSettings.currentRenderPipeline.defaultMaterial;
            var material = new Material(template) { name = name, color = color, enableInstancing = true };
            material.SetFloat("_Smoothness", smoothness); generatedMaterials.Add(material); return material;
        }
        GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent = null)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = name; obj.transform.SetParent(parent != null ? parent : transform, false);
            obj.transform.localPosition = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(obj.GetComponent<Collider>()); obj.isStatic = parent == null;
            return obj;
        }
        GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent = null) => Shape(name,PrimitiveType.Cube,position,scale,material,parent);
        public void Generate()
        {
            var random = new System.Random(Settings.Seed);
            var ground = Material("Earth",new Color(.43f,.57f,.39f));
            asphalt = Material("Asphalt",new Color(.18f,.20f,.21f));
            var pavement = Material("Limestone pavement",new Color(.81f,.83f,.80f));
            var concrete = Material("Drain concrete",new Color(.52f,.60f,.62f));
            var marking = Material("Road markings",new Color(.84f,.82f,.66f));
            var bark = Material("Bark",new Color(.27f,.21f,.15f));
            var leaves = Material("Neem canopy",new Color(.24f,.50f,.25f));
            var metal = Material("Street furniture",new Color(.18f,.24f,.25f),.45f);
            var glass = Material("Window glazing",new Color(.17f,.33f,.43f),.65f);
            windowGlass=glass; glass.EnableKeyword("_EMISSION");
            lampMaterial=Material("Warm street luminaires",new Color(.9f,.83f,.64f)); lampMaterial.EnableKeyword("_EMISSION");
            var solar = Material("Photovoltaic cells",new Color(.07f,.22f,.36f),.5f);
            water = Material("Detention water",new Color(.20f,.40f,.44f),.92f);
            Box("Urban district plinth",new Vector3(0,-.8f,0),new Vector3(106,1.4f,94),concrete);
            Box("Landscaped district surface",new Vector3(0,-.05f,0),new Vector3(104,.16f,92),ground);
            Box("East west avenue",new Vector3(0,0,0),new Vector3(104,.12f,12),asphalt);
            Box("North south street",new Vector3(0,.01f,0),new Vector3(11,.12f,92),asphalt);
            foreach (int side in new[]{-1,1})
            {
                Box("Avenue pavement",new Vector3(0,.13f,side*7.5f),new Vector3(104,.25f,2.7f),pavement);
                Box("Street pavement",new Vector3(side*7,.13f,0),new Vector3(2.4f,.25f,92),pavement);
                Box("Open stormwater channel",new Vector3(0,.1f,side*9.1f),new Vector3(104,.14f,.85f),metal);
                for(int bank=-1;bank<=1;bank+=2) Box("Channel bank",new Vector3(0,.23f,side*9.1f+bank*.47f),new Vector3(104,.28f,.13f),concrete);
                channelWater.Add(Box("Simulated channel storage",new Vector3(0,.18f,side*9.1f),new Vector3(104,.02f,.65f),water).transform);
                for (int x=-46;x<=46;x+=12)
                {
                    var post = Box("Streetlight",new Vector3(x,3.4f,side*7.4f),new Vector3(.13f,6.4f,.13f),metal);
                    Box("Streetlight arm",new Vector3(x,6.55f,side*6.5f),new Vector3(.13f,.15f,1.8f),metal);
                    Box("Luminaire",new Vector3(x,6.45f,side*5.7f),new Vector3(.5f,.13f,.8f),lampMaterial);
                    Box("Drain grate",new Vector3(x,.2f,side*9.1f),new Vector3(1.2f,.06f,.6f),metal);
                }
            }
            for (int x=-48;x<=48;x+=5) if (Mathf.Abs(x)>8) Box("Lane divider",new Vector3(x,.08f,0),new Vector3(2.4f,.015f,.14f),marking);
            for (int z=-42;z<=42;z+=5) if (Mathf.Abs(z)>8) Box("Lane divider",new Vector3(0,.09f,z),new Vector3(.14f,.015f,2.4f),marking);
            foreach (int side in new[]{-1,1}) for (int stripe=-4;stripe<=4;stripe++)
                Box("Pedestrian crossing",new Vector3(stripe*.85f,.09f,side*10.3f),new Vector3(.45f,.02f,2.3f),pavement);
            int buildingIndex=0;
            foreach (int sx in new[]{-1,1}) foreach (int sz in new[]{-1,1})
            {
                Box("Block footpath",new Vector3(sx*29,.12f,sz*27),new Vector3(38,.22f,31),pavement);
                Box("Community green strip",new Vector3(sx*29,.25f,sz*36),new Vector3(35,.12f,8),ground);
                for(int bed=0;bed<5;bed++) Box("Planted verge",new Vector3(sx*(12+bed*8),.26f,sz*12),new Vector3(5,.18f,1.8f),leaves);
                for (int j=0;j<3;j++)
                {
                    float x=sx*(16+j*13), z=sz*23 + ((float)random.NextDouble()-.5f)*Settings.PlacementJitter;
                    float h=Mathf.Lerp(Settings.MinimumHeight,Settings.MaximumHeight,(float)random.NextDouble());
                    Color[] palette={new Color(.91f,.89f,.82f),new Color(.84f,.90f,.92f),new Color(.94f,.91f,.84f),new Color(.85f,.89f,.86f)};
                    var facade=Material("Mineral facade "+buildingIndex,Color.Lerp(palette[buildingIndex%palette.Length],Color.white,(float)random.NextDouble()*.12f));
                    Box("Building "+(++buildingIndex),new Vector3(x,h/2+.24f,z),new Vector3(9,h,13),facade);
                    Box("Stone ground floor",new Vector3(x,1.5f,z),new Vector3(9.05f,2.5f,13.05f),concrete);
                    Box("Roof coping",new Vector3(x,h+.35f,z),new Vector3(9.4f,.3f,13.4f),pavement);
                    foreach(int edge in new[]{-1,1}) { Box("Roof parapet",new Vector3(x+edge*4.55f,h+.7f,z),new Vector3(.2f,.5f,13.2f),facade); Box("Roof parapet",new Vector3(x,h+.7f,z+edge*6.55f),new Vector3(9.2f,.5f,.2f),facade); }
                    Box("Entrance canopy",new Vector3(x,2.6f,z-sz*7),new Vector3(3,.16f,1.4f),metal);
                    Box("Entrance",new Vector3(x,1.35f,z-sz*6.52f),new Vector3(1.5f,2.2f,.08f),glass);
                    for (float y=3.7f;y<h-1;y+=3) for (int w=-1;w<=1;w++)
                    {
                        Box("Recessed window",new Vector3(x+w*2.5f,y,z-sz*6.53f),new Vector3(1.35f,1.65f,.08f),glass);
                        Box("Window sill",new Vector3(x+w*2.5f,y-.88f,z-sz*6.6f),new Vector3(1.6f,.12f,.26f),pavement);
                        Box("Side window",new Vector3(x+sx*4.52f,y,z+w*3.2f),new Vector3(.08f,1.65f,1.5f),glass);
                        Box("Rear window",new Vector3(x+w*2.5f,y,z+sz*6.53f),new Vector3(1.35f,1.65f,.08f),glass);
                        Box("Opposite side window",new Vector3(x-sx*4.52f,y,z+w*3.2f),new Vector3(.08f,1.65f,1.5f),glass);
                    }
                    if (j==0) for(int p=0;p<4;p++)
                    {
                        var panel=Box("Solar panel 2 × 3 m",new Vector3(x+(p%2==0?-1.6f:1.6f),h+.8f,z+(p<2?-2.2f:2.2f)),new Vector3(2,.12f,3),solar);
                        panel.transform.rotation=Quaternion.Euler(12,0,0);
                        for(int cell=0;cell<3;cell++) Box("Panel cell seam",new Vector3(0,.55f,(cell-1)*.28f),new Vector3(1.002f,.15f,.0083f),metal,panel.transform);
                    }
                    Box("Roof service housing",new Vector3(x+2,h+1,z+3),new Vector3(1.8f,1.5f,2),concrete);
                }
                for (int t=0;t<7;t++) if (random.NextDouble()<Settings.TreeDensity)
                {
                    float x=sx*(12+t*5.5f),z=sz*37;
                    Box("Tree bed",new Vector3(x,.29f,z),new Vector3(2.4f,.18f,2.4f),ground);
                    var pivot=new GameObject("Tree sway pivot").transform; pivot.SetParent(transform,false); pivot.localPosition=new Vector3(x,.35f,z); Trees.Add(pivot);
                    Shape("Trunk",PrimitiveType.Cylinder,new Vector3(0,1.7f,0),new Vector3(.28f,1.7f,.28f),bark,pivot);
                    Shape("Canopy",PrimitiveType.Sphere,new Vector3(0,4.6f,0),new Vector3(3.5f,4.4f,3.2f),leaves,pivot);
                    Shape("Canopy branch",PrimitiveType.Sphere,new Vector3(1.1f,4.1f,.3f),new Vector3(2.4f,2.8f,2.5f),leaves,pivot);
                }
                Box("Park bench seat",new Vector3(sx*26,.8f,sz*37),new Vector3(3,.2f,.65f),bark);
                for(int leg=-1;leg<=1;leg+=2) Box("Bench leg",new Vector3(sx*26+leg,.45f,sz*37),new Vector3(.15f,.6f,.65f),metal);
            }
            Box("Detention basin rim",new Vector3(34,.25f,42),new Vector3(22,.32f,5.5f),concrete);
            waterSurface=Box("Simulated detention storage",new Vector3(34,.43f,42),new Vector3(21,.02f,4.5f),water).transform;
            var lightObj=new GameObject("Daylight"); lightObj.transform.SetParent(transform); sunlight=lightObj.AddComponent<Light>();
            sunlight.type=LightType.Directional; sunlight.shadows=LightShadows.Soft; sunlight.intensity=1.2f; sunlight.color=new Color(1,.95f,.84f); sunlight.transform.rotation=Quaternion.Euler(48,-35,0);
            RenderSettings.ambientMode=AmbientMode.Trilight; RenderSettings.ambientSkyColor=new Color(.60f,.69f,.73f);
            RenderSettings.ambientEquatorColor=new Color(.42f,.47f,.46f); RenderSettings.ambientGroundColor=new Color(.22f,.23f,.21f);
            RenderSettings.fog=true; RenderSettings.fogMode=FogMode.ExponentialSquared; RenderSettings.fogDensity=.0018f; RenderSettings.fogColor=new Color(.64f,.72f,.74f);
            var cameraObj=new GameObject("Overview camera"); ViewCamera=cameraObj.AddComponent<Camera>(); cameraObj.tag="MainCamera";
            cameraObj.transform.SetParent(transform,false);
            ViewCamera.clearFlags=CameraClearFlags.SolidColor; ViewCamera.backgroundColor=new Color(.64f,.72f,.74f); ViewCamera.fieldOfView=43; ViewCamera.nearClipPlane=.3f; ViewCamera.farClipPlane=450;
            cameraObj.AddComponent<AudioListener>(); cameraObj.AddComponent<OrbitCamera>();
            var cameraData=ViewCamera.GetUniversalAdditionalCameraData(); cameraData.renderPostProcessing=true; cameraData.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var volumeObj=new GameObject("Daylight tonemapping"); volumeObj.transform.SetParent(transform);
            var volume=volumeObj.AddComponent<Volume>(); volume.isGlobal=true; generatedProfile=ScriptableObject.CreateInstance<VolumeProfile>(); volume.sharedProfile=generatedProfile;
            var tonemapping=generatedProfile.Add<Tonemapping>(); tonemapping.mode.Override(TonemappingMode.ACES);
            var exposure=generatedProfile.Add<ColorAdjustments>(); exposure.postExposure.Override(.25f);exposure.contrast.Override(12f);
            CreateRain();
        }
        void CreateRain()
        {
            var obj=new GameObject("Rainfall particles"); obj.transform.SetParent(transform,false); obj.transform.position=new Vector3(0,34,0);
            rain=obj.AddComponent<ParticleSystem>(); rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=rain.main; main.loop=true; main.startLifetime=2.8f; main.startSpeed=0; main.startSize=.12f;
            main.maxParticles=12000; main.simulationSpace=ParticleSystemSimulationSpace.World; main.startColor=new Color(.26f,.49f,.64f,.75f);
            var shape=rain.shape; shape.shapeType=ParticleSystemShapeType.Box; shape.scale=new Vector3(102,1,90);
            var velocity=rain.velocityOverLifetime; velocity.enabled=true; velocity.space=ParticleSystemSimulationSpace.World; velocity.y=-16;
            var emission=rain.emission; emission.rateOverTime=0;
            var renderer=rain.GetComponent<ParticleSystemRenderer>(); renderer.renderMode=ParticleSystemRenderMode.Stretch; renderer.lengthScale=5; renderer.velocityScale=.06f; renderer.sharedMaterial=ParticleTemplate;
            rain.useAutoRandomSeed=false; rain.randomSeed=(uint)Math.Max(1,Settings.Seed); rain.Play();
        }
        public void Render(EnvironmentState state, WeatherData weather, float seconds)
        {
            float blend=1-Mathf.Exp(-seconds*2);
            visualWetness=Mathf.Lerp(visualWetness,(float)state.Wetness,blend);
            asphalt.color=Color.Lerp(new Color(.18f,.20f,.21f),new Color(.085f,.115f,.13f),visualWetness);
            asphalt.SetFloat("_Smoothness",Mathf.Lerp(.12f,.84f,visualWetness));
            waterSurface.localScale=new Vector3(Mathf.Lerp(.01f,21,(float)(state.CollectionM3/150)),.02f,4.5f);
            float daylight=1;
            if (DateTime.TryParse(weather?.Time,out var timestamp)) daylight=timestamp.Hour<6 || timestamp.Hour>=19 ? .70f : 1;
            sunlight.intensity=Mathf.Lerp(sunlight.intensity,(float)state.LightIntensity*daylight,blend);
            float cloud=(float)Numbers.Clamp(weather?.CloudPercent ?? 50,0,100)/100;
            var sky=Color.Lerp(new Color(.90f,.96f,.99f),new Color(.83f,.89f,.94f),cloud);
            sky=Color.Lerp(new Color(.62f,.71f,.82f),sky,daylight);
            ViewCamera.backgroundColor=Color.Lerp(ViewCamera.backgroundColor,sky,blend);
            RenderSettings.fogColor=ViewCamera.backgroundColor;
            RenderSettings.ambientSkyColor=Color.Lerp(new Color(.83f,.89f,.94f),new Color(.72f,.79f,.85f),cloud);
            windowGlass.SetColor("_EmissionColor",new Color(.40f,.31f,.18f)*(1-daylight)*.5f);
            lampMaterial.SetColor("_EmissionColor",new Color(1,.78f,.40f)*(1-daylight)*1.5f);
            foreach(var channel in channelWater) channel.GetComponent<Renderer>().enabled=state.CollectionM3>.1;
            var emission=rain.emission; emission.rateOverTime=Mathf.Lerp(emission.rateOverTime.constant,(float)state.RainIntensity*3500,blend);
            float direction=(float)(weather?.WindDegrees ?? 0)*Mathf.Deg2Rad;
            // Meteorological direction is where wind comes FROM.
            visualWind=Mathf.Lerp(visualWind,(float)state.WindIntensity,blend);
            float drift=Numbers.IsFinite(weather?.WindDegrees) ? visualWind*9 : 0;
            visualDrift=Vector2.Lerp(visualDrift,new Vector2(-Mathf.Sin(direction)*drift,-Mathf.Cos(direction)*drift),blend);
            var velocity=rain.velocityOverLifetime; velocity.x=visualDrift.x; velocity.z=visualDrift.y;
            for(int i=0;i<Trees.Count;i++) Trees[i].localRotation=Quaternion.Euler(Mathf.Sin(Time.time*1.7f+i)*2.5f*visualWind,0,Mathf.Cos(Time.time*1.3f+i)*3.5f*visualWind);
        }
        void OnDestroy()
        {
            foreach(var material in generatedMaterials) Destroy(material);
            if(generatedProfile!=null) { foreach(var component in generatedProfile.components) Destroy(component); Destroy(generatedProfile); }
        }
    }
}
