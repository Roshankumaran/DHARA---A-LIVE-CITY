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

        // Enhanced environmental effects.
        ParticleSystem rainSplashes;
        ParticleSystem windDebris;
        readonly List<Transform> puddles = new List<Transform>();
        readonly List<Vector3> puddleSizes = new List<Vector3>();
        readonly List<Material> puddleMaterials = new List<Material>();
        Material puddleWater;
        Texture2D streakTexture, rippleTexture;
        ParticleSystem ripples;
        public float WeatherFogDensity { get; private set; }
        public float DisplayedRain => visualRainIntensity;
        public Vector2 DisplayedWind => visualDrift;
        float visualRainIntensity;
        float visualCloudCover;
        float visualDaylight = 1;
        ColorAdjustments colorAdjustments;
        Bloom bloom;
        Vignette vignette;

        // Tree canopy materials for wind-driven color variation.
        readonly List<Material> canopyMaterials = new List<Material>();

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
            puddleWater = Material("Puddle water",new Color(.15f,.28f,.34f),.95f);

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

            // Procedural puddle areas on road surfaces.
            CreatePuddles(random);

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
                    // Create individual canopy materials for wind color variation.
                    var canopy1 = Material("Canopy "+Trees.Count+"a", new Color(.24f,.50f,.25f));
                    var canopy2 = Material("Canopy "+Trees.Count+"b", new Color(.22f,.48f,.23f));
                    canopyMaterials.Add(canopy1); canopyMaterials.Add(canopy2);
                    Shape("Canopy",PrimitiveType.Sphere,new Vector3(0,4.6f,0),new Vector3(3.5f,4.4f,3.2f),canopy1,pivot);
                    Shape("Canopy branch",PrimitiveType.Sphere,new Vector3(1.1f,4.1f,.3f),new Vector3(2.4f,2.8f,2.5f),canopy2,pivot);
                }
                Box("Park bench seat",new Vector3(sx*26,.8f,sz*37),new Vector3(3,.2f,.65f),bark);
                for(int leg=-1;leg<=1;leg+=2) Box("Bench leg",new Vector3(sx*26+leg,.45f,sz*37),new Vector3(.15f,.6f,.65f),metal);
            }
            Box("Detention basin rim",new Vector3(34,.25f,42),new Vector3(22,.32f,5.5f),concrete);
            waterSurface=Box("Simulated detention storage",new Vector3(34,.43f,42),new Vector3(21,.02f,4.5f),water).transform;

            // Enhanced directional light with better shadows.
            var lightObj=new GameObject("Daylight"); lightObj.transform.SetParent(transform); sunlight=lightObj.AddComponent<Light>();
            sunlight.type=LightType.Directional; sunlight.shadows=LightShadows.Soft; sunlight.intensity=1.2f; sunlight.color=new Color(1,.95f,.84f); sunlight.transform.rotation=Quaternion.Euler(48,-35,0);
            sunlight.shadowStrength = .85f;

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
            colorAdjustments=generatedProfile.Add<ColorAdjustments>(); colorAdjustments.postExposure.Override(.25f);colorAdjustments.contrast.Override(12f);

            // Keep post-processing restrained; wet reflections come from the surface materials.
            bloom = generatedProfile.Add<Bloom>();
            // Wet-road highlights are rendered by the material; bloom can obscure the city in heavy rain.
            bloom.active=true;
            bloom.threshold.Override(1.1f);
            bloom.intensity.Override(0);
            bloom.scatter.Override(.65f);

            vignette = generatedProfile.Add<Vignette>();
            vignette.intensity.Override(.18f);
            vignette.smoothness.Override(.45f);
            vignette.color.Override(new Color(.18f,.22f,.28f));

            CreateRain();
            CreateRainSplashes();
            CreateWindDebris();
            CreateRipples();
        }

        void CreatePuddles(System.Random random)
        {
            // Create puddle patches on road surfaces at deterministic positions.
            Vector3[] puddlePositions = {
                new Vector3(-20, .07f, 1), new Vector3(15, .07f, -2), new Vector3(-35, .07f, 3),
                new Vector3(30, .07f, -1), new Vector3(-8, .07f, 4), new Vector3(42, .07f, 0),
                new Vector3(-2, .105f, -15), new Vector3(3, .105f, 20), new Vector3(-3, .105f, -30),
                new Vector3(1, .105f, 35), new Vector3(-2, .105f, -40), new Vector3(2, .105f, 25)
            };
            foreach (var pos in puddlePositions)
            {
                float width = 1.5f + (float)random.NextDouble() * 3f;
                float depth = 1.2f + (float)random.NextDouble() * 2.5f;
                var mat = Material("Road puddle", new Color(.12f, .22f, .30f), .94f);
                puddleMaterials.Add(mat);
                var puddle = Shape("Puddle", PrimitiveType.Cylinder, pos, new Vector3(width, .005f, depth), mat);
                puddle.transform.localRotation = Quaternion.Euler(0, (float)random.NextDouble() * 360f, 0);
                puddles.Add(puddle.transform); puddleSizes.Add(puddle.transform.localScale);
                puddle.GetComponent<Renderer>().enabled = false;
            }
        }

        Material EffectMaterial(bool ring)
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { name = ring ? "Ripple alpha" : "Rain alpha", wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                float u = (x + .5f) / 16 - 1, v = (y + .5f) / 16 - 1;
                float alpha = ring ? Mathf.Clamp01(1 - Mathf.Abs(Mathf.Sqrt(u*u+v*v)-.76f)*18) : Mathf.Pow(Mathf.Clamp01(1-Mathf.Abs(u)), 3) * Mathf.Clamp01(1-Mathf.Abs(v));
                texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
            texture.Apply();
            if (ring) rippleTexture = texture; else { if (streakTexture != null) Destroy(streakTexture); streakTexture = texture; }
            var material = new Material(ParticleTemplate) { name = ring ? "Ripple particles" : "Rain streak particles" };
            material.mainTexture = texture; generatedMaterials.Add(material); return material;
        }
        void CreateRipples()
        {
            var obj = new GameObject("Road rain ripples"); obj.transform.SetParent(transform, false); obj.transform.localPosition = new Vector3(0, .11f, 0);
            ripples = obj.AddComponent<ParticleSystem>(); ripples.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ripples.main; main.startLifetime = .65f; main.startSpeed = 0; main.startSize = .8f; main.maxParticles = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startColor = new Color(.65f,.8f,.85f,.5f);
            var shape = ripples.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(100, .001f, 9);
            var size = ripples.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0,.05f,1,1));
            var fade = ripples.colorOverLifetime; fade.enabled = true; var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white,0), new GradientColorKey(Color.white,1) }, new[] { new GradientAlphaKey(.7f,0),new GradientAlphaKey(0,1) }); fade.color = gradient;
            var emission = ripples.emission; emission.rateOverTime = 0;
            var renderer = ripples.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard; renderer.sharedMaterial = EffectMaterial(true);
            ripples.useAutoRandomSeed = false; ripples.randomSeed = (uint)Math.Max(1, Settings.Seed+3); ripples.Play();
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
            var renderer=rain.GetComponent<ParticleSystemRenderer>(); renderer.renderMode=ParticleSystemRenderMode.Stretch; renderer.lengthScale=2.8f; renderer.velocityScale=.025f; renderer.sharedMaterial=EffectMaterial(false);
            rain.useAutoRandomSeed=false; rain.randomSeed=(uint)Math.Max(1,Settings.Seed); rain.Play();
        }

        void CreateRainSplashes()
        {
            // Rain impact particles — splash effects on surfaces.
            var obj = new GameObject("Rain splash impacts"); obj.transform.SetParent(transform, false); obj.transform.position = new Vector3(0, .3f, 0);
            rainSplashes = obj.AddComponent<ParticleSystem>(); rainSplashes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = rainSplashes.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.12f, .35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(.02f, .08f);
            main.maxParticles = 4000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(.45f, .58f, .68f, .6f);
            main.gravityModifier = 2.5f;

            var shape = rainSplashes.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(100, .01f, 9);

            var emission = rainSplashes.emission;
            emission.rateOverTime = 0;

            var fade = rainSplashes.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(.65f, 0), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
            // Size over lifetime: shrink to nothing.
            var sizeOverLife = rainSplashes.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

            var renderer = rainSplashes.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = rain.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            rainSplashes.useAutoRandomSeed = false;
            rainSplashes.randomSeed = (uint)Math.Max(1, Settings.Seed + 1);
            rainSplashes.Play();
        }

        void CreateWindDebris()
        {
            // Wind-driven debris / leaf particles.
            var obj = new GameObject("Wind debris"); obj.transform.SetParent(transform, false); obj.transform.position = new Vector3(0, 3f, 0);
            windDebris = obj.AddComponent<ParticleSystem>(); windDebris.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = windDebris.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startSpeed = 0;
            main.startSize = new ParticleSystem.MinMaxCurve(.06f, .15f);
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(.35f, .48f, .30f, .7f);
            main.gravityModifier = .08f;
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);

            var shape = windDebris.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(80, 8, 70);

            var emission = windDebris.emission;
            emission.rateOverTime = 0;

            // Rotation over lifetime for tumbling.
            var rotation = windDebris.rotationOverLifetime;
            rotation.enabled = true;
            rotation.separateAxes = true;
            rotation.x = new ParticleSystem.MinMaxCurve(-2f, 2f);
            rotation.y = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
            rotation.z = new ParticleSystem.MinMaxCurve(-2f, 2f);

            var renderer = windDebris.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = ParticleTemplate;
            windDebris.useAutoRandomSeed = false;
            windDebris.randomSeed = (uint)Math.Max(1, Settings.Seed + 2);
            windDebris.Play();
        }

        public void Render(EnvironmentState state, WeatherData weather, float seconds)
        {
            float blend = 1 - Mathf.Exp(-seconds * 2);

            // Rain intensity smoothing.
            visualRainIntensity = Mathf.Lerp(visualRainIntensity, (float)state.RainIntensity, blend);

            // Wetness with hysteresis — surfaces stay wet longer after rain stops.
            float wetTarget = (float)state.Wetness;
            float wetBlend = wetTarget > visualWetness ? blend : blend * .3f; // Slower drying.
            visualWetness = Mathf.Lerp(visualWetness, wetTarget, wetBlend);

            // Asphalt wetness: darker color and higher reflectivity when wet.
            asphalt.color = Color.Lerp(new Color(.18f, .20f, .21f), new Color(.075f, .10f, .12f), visualWetness);
            asphalt.SetFloat("_Smoothness", Mathf.Lerp(.12f, .88f, visualWetness));

            // Water accumulation in detention basin.
            waterSurface.localScale = new Vector3(Mathf.Lerp(.01f, 21, (float)(state.CollectionM3 / 150)), .02f, 4.5f);

            // Puddle visibility — controlled by rain and water accumulation.
            float puddleScale = Mathf.Clamp01((float)(state.CollectionM3 / 150));
            for (int i = 0; i < puddles.Count; i++)
            {
                var puddle = puddles[i];
                var originalScale = puddleSizes[i];
                float targetY = puddleScale > .05f ? .005f : 0;
                puddle.localScale = new Vector3(originalScale.x * Mathf.Sqrt(puddleScale), targetY, originalScale.z * Mathf.Sqrt(puddleScale));
                puddle.GetComponent<Renderer>().enabled = puddleScale > .02f;
            }
            foreach (var mat in puddleMaterials)
            {
                mat.SetFloat("_Smoothness", Mathf.Lerp(.6f, .96f, puddleScale));
                mat.color = Color.Lerp(new Color(.12f, .22f, .30f, .3f), new Color(.10f, .20f, .28f, .85f), puddleScale);
            }

            // Daylight calculation.
            float daylight = 1;
            if (DateTime.TryParse(weather?.Time, out var timestamp))
            {
                float hour = timestamp.Hour + timestamp.Minute / 60f;
                if (hour < 6) daylight = .55f;
                else if (hour < 7) daylight = Mathf.Lerp(.55f, .85f, hour - 6);
                else if (hour >= 19) daylight = Mathf.Lerp(.85f, .55f, Mathf.Clamp01(hour - 19));
                else if (hour >= 18) daylight = Mathf.Lerp(1f, .85f, hour - 18);
            }
            visualDaylight = Mathf.Lerp(visualDaylight, daylight, blend);

            // Cloud cover smoothing.
            float cloud = (float)Numbers.Clamp(weather?.CloudPercent ?? 50, 0, 100) / 100;
            visualCloudCover = Mathf.Lerp(visualCloudCover, cloud, blend);

            // Sunlight: dimmer in rain, warmer at golden hours, cooler in overcast.
            float lightReduction = visualRainIntensity * .25f;
            float solarFactor=Numbers.IsFinite(weather?.SolarWm2)?Mathf.Lerp(.65f,1,(float)Numbers.Clamp(weather.SolarWm2.Value/800,0,1)):1;
            sunlight.intensity = Mathf.Lerp(sunlight.intensity, (float)state.LightIntensity * visualDaylight * solarFactor * (1 - lightReduction), blend);
            sunlight.color = Color.Lerp(
                Color.Lerp(new Color(1, .95f, .84f), new Color(.88f, .90f, .94f), visualCloudCover),
                new Color(1, .80f, .55f), (1 - visualDaylight) * .3f);
            sunlight.shadowStrength = Mathf.Lerp(.4f, .85f, visualDaylight * (1 - visualCloudCover * .5f));

            // Sky / background color.
            var clearSky = Color.Lerp(new Color(.90f, .96f, .99f), new Color(.62f, .71f, .82f), 1 - visualDaylight);
            var overcastSky = Color.Lerp(new Color(.72f, .76f, .80f), new Color(.50f, .54f, .58f), 1 - visualDaylight);
            var rainSky = new Color(.52f, .56f, .60f);
            var sky = Color.Lerp(clearSky, overcastSky, visualCloudCover);
            sky = Color.Lerp(sky, rainSky, visualRainIntensity * .6f);
            ViewCamera.backgroundColor = Color.Lerp(ViewCamera.backgroundColor, sky, blend);
            RenderSettings.fogColor = ViewCamera.backgroundColor;

            // Fog density increases with rain.
            float baseFog = Mathf.Lerp(.0012f, .0025f, visualCloudCover);
            float rainFog = visualRainIntensity * .003f;
            WeatherFogDensity = baseFog + rainFog;

            // Ambient lighting.
            RenderSettings.ambientSkyColor = Color.Lerp(
                new Color(.83f, .89f, .94f),
                new Color(.58f, .62f, .66f),
                Mathf.Max(visualCloudCover, visualRainIntensity));
            RenderSettings.ambientEquatorColor = Color.Lerp(
                new Color(.42f, .47f, .46f),
                new Color(.35f, .38f, .40f),
                visualRainIntensity);

            // Window glow at night.
            float nightFactor = 1 - visualDaylight;
            windowGlass.SetColor("_EmissionColor", new Color(.40f, .31f, .18f) * nightFactor * .5f);
            lampMaterial.SetColor("_EmissionColor", new Color(1, .78f, .40f) * nightFactor * 1.5f);

            // Explicitly suppress bloom fallback; retain a restrained weather vignette.
            if (bloom != null)
            {
                bloom.intensity.Override(0); // Explicit override prevents default-profile bloom from returning.
                bloom.threshold.Override(Mathf.Lerp(1.2f, .9f, visualRainIntensity));
            }
            if (vignette != null)
            {
                vignette.intensity.Override(Mathf.Lerp(.15f, .35f, visualRainIntensity * .6f + (1 - visualDaylight) * .3f));
            }
            if (colorAdjustments != null)
            {
                // Desaturate slightly in heavy rain; boost contrast in clear conditions.
                colorAdjustments.saturation.Override(Mathf.Lerp(5f, -12f, visualRainIntensity));
                colorAdjustments.contrast.Override(Mathf.Lerp(12f, 18f, visualRainIntensity * .5f));
                colorAdjustments.postExposure.Override(Mathf.Lerp(.25f, -.1f, visualRainIntensity * .3f));
            }

            // Channel water visibility.
            foreach (var channel in channelWater) channel.GetComponent<Renderer>().enabled = state.CollectionM3 > .1;

            // Rain particles.
            var emission = rain.emission;
            emission.rateOverTime = state.RainIntensity <= 0 ? 0 : visualRainIntensity * 3500;

            // Rain particle sizing: bigger drops in heavy rain.
            var rainMain = rain.main;
            rainMain.startSize = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(.06f, .10f, visualRainIntensity),
                Mathf.Lerp(.12f, .20f, visualRainIntensity));

            // Rain splash impacts.
            if (rainSplashes != null)
            {
                var splashEmit = rainSplashes.emission;
                splashEmit.rateOverTime = state.RainIntensity <= 0 ? 0 : 900 * visualRainIntensity;
                var rippleEmission = ripples.emission; rippleEmission.rateOverTime = state.RainIntensity <= 0 ? 0 : 450 * visualRainIntensity * visualWetness;
            }

            // Wind debris particles.
            if (windDebris != null)
            {
                var debrisEmit = windDebris.emission;
                debrisEmit.rateOverTime = Mathf.Lerp(0, 80, visualWind * visualWind);
                var debrisVel = windDebris.velocityOverLifetime;
                debrisVel.enabled = true;
                debrisVel.space = ParticleSystemSimulationSpace.World;
                debrisVel.x = visualDrift.x * .8f;
                debrisVel.y = .1f; // Match the constant X/Z curve modes required by Unity.
                debrisVel.z = visualDrift.y * .8f;
            }

            // Wind direction and intensity.
            float direction = Numbers.IsFinite(weather?.WindDegrees) ? (float)weather.WindDegrees.Value * Mathf.Deg2Rad : 0;
            visualWind = Mathf.Lerp(visualWind, (float)state.WindIntensity, blend);
            float drift = Numbers.IsFinite(weather?.WindDegrees) ? visualWind * 9 : 0;
            visualDrift = Vector2.Lerp(visualDrift, new Vector2(-Mathf.Sin(direction) * drift, -Mathf.Cos(direction) * drift), blend);
            var velocity = rain.velocityOverLifetime; velocity.x = visualDrift.x; velocity.z = visualDrift.y;
            // Offset the emitter upwind so falling particles still cover the accessible city at street height.
            float fallTime=(rain.transform.position.y-2.2f)/16f;
            rain.transform.position=new Vector3(-visualDrift.x*fallTime,34,-visualDrift.y*fallTime);

            // Enhanced tree animation: multi-frequency sway with individual character.
            float time = Time.time;
            for (int i = 0; i < Trees.Count; i++)
            {
                float phase = i * 1.37f; // Golden-ratio phase offset for natural variation.
                float swayAmount = visualWind;

                // Meteorological direction is where wind comes FROM; +Z is north, +X east.
                // A root pivot and absolute rotations prevent cumulative drift.
                float oscillation = .55f + .3f * Mathf.Sin(time * 1.1f + phase) + .15f * Mathf.Sin(time * 3.7f + phase);
                // Unknown direction still permits non-directional, speed-driven rocking in an offline scenario.
                // Do not invent a compass bearing or directional particle drift.
                Vector2 heading = Numbers.IsFinite(weather?.WindDegrees)
                    ? (visualDrift.sqrMagnitude > .0001f ? visualDrift.normalized : Vector2.zero)
                    : new Vector2(Mathf.Sin(time * .7f + phase), Mathf.Cos(time * .7f + phase));
                Trees[i].localRotation = Quaternion.Euler(heading.y * 7 * swayAmount * oscillation, 0, -heading.x * 7 * swayAmount * oscillation);

                // Canopy color variation in wind — slight lightening as leaves flip.
                if (i * 2 + 1 < canopyMaterials.Count)
                {
                    float colorShift = Mathf.Abs(Mathf.Sin(time * 2.5f + phase)) * swayAmount * .08f;
                    canopyMaterials[i * 2].color = Color.Lerp(new Color(.24f, .50f, .25f), new Color(.30f, .58f, .32f), colorShift);
                    canopyMaterials[i * 2 + 1].color = Color.Lerp(new Color(.22f, .48f, .23f), new Color(.28f, .55f, .30f), colorShift);
                }
            }
        }

        /// <summary>Set the fog density externally, used for air quality haze integration.</summary>
        public void SetFogOverride(float density)
        {
            RenderSettings.fogDensity = density;
        }

        void OnDestroy()
        {
            foreach (var material in generatedMaterials) Destroy(material);
            if (streakTexture != null) Destroy(streakTexture);
            if (rippleTexture != null) Destroy(rippleTexture);
            if (generatedProfile != null) { foreach (var component in generatedProfile.components) Destroy(component); Destroy(generatedProfile); }
        }
    }
}
