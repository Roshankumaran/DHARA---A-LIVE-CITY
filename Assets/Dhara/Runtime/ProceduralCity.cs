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
        ParticleSystem rain;
        float visualWetness;
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
            var ground = Material("Earth",new Color(.33f,.37f,.31f));
            asphalt = Material("Asphalt",new Color(.18f,.20f,.21f));
            var pavement = Material("Limestone pavement",new Color(.63f,.64f,.59f));
            var concrete = Material("Drain concrete",new Color(.32f,.36f,.35f));
            var marking = Material("Road markings",new Color(.84f,.82f,.66f));
            var bark = Material("Bark",new Color(.27f,.21f,.15f));
            var leaves = Material("Neem canopy",new Color(.22f,.37f,.23f));
            var metal = Material("Street furniture",new Color(.18f,.24f,.25f),.45f);
            var glass = Material("Window glazing",new Color(.22f,.37f,.42f),.65f);
            var solar = Material("Photovoltaic cells",new Color(.055f,.13f,.21f),.5f);
            water = Material("Detention water",new Color(.20f,.40f,.44f),.92f);
            Box("Urban district plinth",new Vector3(0,-.6f,0),new Vector3(106,1,94),ground);
            Box("East west avenue",new Vector3(0,0,0),new Vector3(104,.12f,12),asphalt);
            Box("North south street",new Vector3(0,.01f,0),new Vector3(11,.12f,92),asphalt);
            foreach (int side in new[]{-1,1})
            {
                Box("Avenue pavement",new Vector3(0,.13f,side*7.5f),new Vector3(104,.25f,2.7f),pavement);
                Box("Street pavement",new Vector3(side*7,.13f,0),new Vector3(2.4f,.25f,92),pavement);
                Box("Open stormwater channel",new Vector3(0,.1f,side*9.1f),new Vector3(104,.14f,.55f),concrete);
                for (int x=-46;x<=46;x+=12)
                {
                    var post = Box("Streetlight",new Vector3(x,3.4f,side*7.4f),new Vector3(.13f,6.4f,.13f),metal);
                    Box("Streetlight arm",new Vector3(x,6.55f,side*6.5f),new Vector3(.13f,.15f,1.8f),metal);
                    Box("Luminaire",new Vector3(x,6.45f,side*5.7f),new Vector3(.5f,.13f,.8f),marking);
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
                for (int j=0;j<3;j++)
                {
                    float x=sx*(16+j*13), z=sz*23 + ((float)random.NextDouble()-.5f)*Settings.PlacementJitter;
                    float h=Mathf.Lerp(Settings.MinimumHeight,Settings.MaximumHeight,(float)random.NextDouble());
                    var facade=Material("Mineral facade "+buildingIndex,new Color(.56f+(float)random.NextDouble()*.16f,.54f+(float)random.NextDouble()*.15f,.47f+(float)random.NextDouble()*.17f));
                    Box("Building "+(++buildingIndex),new Vector3(x,h/2+.24f,z),new Vector3(9,h,13),facade);
                    Box("Roof coping",new Vector3(x,h+.35f,z),new Vector3(9.4f,.3f,13.4f),pavement);
                    Box("Entrance canopy",new Vector3(x,2.6f,z-sz*7),new Vector3(3,.16f,1.4f),metal);
                    Box("Entrance",new Vector3(x,1.35f,z-sz*6.52f),new Vector3(1.5f,2.2f,.08f),glass);
                    for (float y=3.7f;y<h-1;y+=3) for (int w=-1;w<=1;w++)
                    {
                        Box("Recessed window",new Vector3(x+w*2.5f,y,z-sz*6.53f),new Vector3(1.35f,1.65f,.08f),glass);
                        Box("Window sill",new Vector3(x+w*2.5f,y-.88f,z-sz*6.6f),new Vector3(1.6f,.12f,.26f),pavement);
                        Box("Side window",new Vector3(x+sx*4.52f,y,z+w*3.2f),new Vector3(.08f,1.65f,1.5f),glass);
                    }
                    if (j==0) for(int p=0;p<4;p++)
                    {
                        var panel=Box("Solar panel 2 × 3 m",new Vector3(x+(p%2==0?-1.6f:1.6f),h+.8f,z+(p<2?-2.2f:2.2f)),new Vector3(2,.12f,3),solar);
                        panel.transform.rotation=Quaternion.Euler(12,0,0);
                        for(int cell=0;cell<3;cell++) Box("Panel cell seam",new Vector3(x+(p%2==0?-1.6f:1.6f),h+.9f,z+(p<2?-2.2f:2.2f)+(cell-1)*.8f),new Vector3(2.01f,.02f,.025f),metal);
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
            ViewCamera.clearFlags=CameraClearFlags.SolidColor; ViewCamera.backgroundColor=new Color(.64f,.72f,.74f); ViewCamera.fieldOfView=43; ViewCamera.nearClipPlane=.3f; ViewCamera.farClipPlane=450;
            cameraObj.AddComponent<AudioListener>(); cameraObj.AddComponent<OrbitCamera>();
            var cameraData=ViewCamera.GetUniversalAdditionalCameraData(); cameraData.renderPostProcessing=true; cameraData.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var volumeObj=new GameObject("Daylight tonemapping"); volumeObj.transform.SetParent(transform);
            var volume=volumeObj.AddComponent<Volume>(); volume.isGlobal=true; volume.profile=ScriptableObject.CreateInstance<VolumeProfile>();
            var tonemapping=volume.profile.Add<Tonemapping>(); tonemapping.mode.Override(TonemappingMode.ACES);
            CreateRain();
        }
        void CreateRain()
        {
            var obj=new GameObject("Rainfall particles"); obj.transform.SetParent(transform,false); obj.transform.position=new Vector3(0,34,0);
            rain=obj.AddComponent<ParticleSystem>(); rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=rain.main; main.loop=true; main.startLifetime=2.8f; main.startSpeed=0; main.startSize=.08f;
            main.maxParticles=12000; main.simulationSpace=ParticleSystemSimulationSpace.World; main.startColor=new Color(.64f,.79f,.82f,.65f);
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
            waterSurface.localScale=new Vector3(Mathf.Lerp(.01f,21,(float)(state.WaterMm/150)),.02f,4.5f);
            float daylight=1;
            if (DateTime.TryParse(weather?.Time,out var timestamp)) daylight=timestamp.Hour<6 || timestamp.Hour>=19 ? .16f : 1;
            sunlight.intensity=Mathf.Lerp(sunlight.intensity,(float)state.LightIntensity*daylight,blend);
            var emission=rain.emission; emission.rateOverTime=Mathf.Lerp(emission.rateOverTime.constant,(float)state.RainIntensity*3500,blend);
            float direction=(float)(weather?.WindDegrees ?? 0)*Mathf.Deg2Rad;
            // Meteorological direction is where wind comes FROM.
            float drift=(float)state.WindIntensity*9;
            var velocity=rain.velocityOverLifetime; velocity.x=-Mathf.Sin(direction)*drift; velocity.z=-Mathf.Cos(direction)*drift;
            for(int i=0;i<Trees.Count;i++) Trees[i].localRotation=Quaternion.Euler(Mathf.Sin(Time.time*1.7f+i)*2.5f*(float)state.WindIntensity,0,Mathf.Cos(Time.time*1.3f+i)*3.5f*(float)state.WindIntensity);
        }
        void OnDestroy() { foreach(var material in generatedMaterials) Destroy(material); }
    }
}
