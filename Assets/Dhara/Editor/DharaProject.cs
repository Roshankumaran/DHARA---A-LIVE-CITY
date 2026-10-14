using System;
using System.IO;
using System.Linq;
using Dhara;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dhara.Editor
{
    public static class DharaProject
    {
        const string ScenePath="Assets/Scenes/Dhara.unity";
        [InitializeOnLoadMethod]
        static void FirstOpen()
        {
            if(!File.Exists("Assets/Dhara/SetupComplete.txt")) EditorApplication.delayCall+=() => {
                if(EditorApplication.isPlayingOrWillChangePlaymode) return;
                try { Prepare(); } catch(Exception error) { Debug.LogError("DHARA first-open preparation: "+error.Message+". Retry DHARA > Prepare main scene after resolving this issue."); }
            };
        }
        [MenuItem("DHARA/Prepare main scene")]
        public static void Prepare()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            if(pipeline==null) throw new InvalidOperationException("Official URP template asset missing: Assets/Settings/PC_RPAsset.asset");
            GraphicsSettings.defaultRenderPipeline=pipeline; QualitySettings.renderPipeline=pipeline; pipeline.supportsHDR=true;
            var settings=AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset");
            if(settings==null)
            {
                // Locate the official bundled essentials instead of assuming a package layout.
                var candidates=Directory.GetFiles("Library/PackageCache","TMP Essential Resources.unitypackage",SearchOption.AllDirectories);
                if(candidates.Length==0) throw new InvalidOperationException("Import TMP Essential Resources from Window > TextMeshPro, then run Prepare again.");
                AssetDatabase.importPackageCompleted-=EssentialsImported;
                AssetDatabase.importPackageCompleted+=EssentialsImported;
                AssetDatabase.ImportPackage(candidates[0],false);
                return; // Resume after importing the official fonts and TMP settings.
            }
            if(TMP_Settings.defaultFontAsset==null)
                throw new InvalidOperationException("TMP default font is unavailable. Wait for essentials to import, then run Prepare main scene again.");
            Directory.CreateDirectory("Assets/Dhara/Materials"); Directory.CreateDirectory("Assets/Scenes");
            var surface=GetMaterial("Assets/Dhara/Materials/Surface.mat","Universal Render Pipeline/Lit");
            var particles=GetMaterial("Assets/Dhara/Materials/Rain.mat","Universal Render Pipeline/Particles/Unlit");
            particles.SetFloat("_Surface",1); particles.SetFloat("_Blend",0);
            particles.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); particles.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            particles.SetFloat("_ZWrite",0); particles.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); particles.renderQueue=(int)RenderQueue.Transparent;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var app=new GameObject("DHARA · A Living World").AddComponent<DharaApplication>(); app.SurfaceTemplate=surface; app.ParticleTemplate=particles;
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            PlayerSettings.companyName="DHARA"; PlayerSettings.productName="DHARA — A Living World";
            PlayerSettings.defaultScreenWidth=1600; PlayerSettings.defaultScreenHeight=1000;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed; PlayerSettings.resizableWindow=true;
            PlayerSettings.colorSpace=ColorSpace.Linear; PlayerSettings.runInBackground=true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            File.WriteAllText("Assets/Dhara/SetupComplete.txt","Prepared by Unity Editor "+Application.unityVersion);
            AssetDatabase.SaveAssets(); Debug.Log("DHARA scene prepared. Enter Play Mode to generate the district and retrieve weather.");
        }
        static Material GetMaterial(string path,string shaderName)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material!=null) return material;
            var shader=Shader.Find(shaderName); if(shader==null) throw new InvalidOperationException("Missing URP shader: "+shaderName);
            material=new Material(shader); AssetDatabase.CreateAsset(material,path); return material;
        }
        static void EssentialsImported(string packageName)
        {
            AssetDatabase.importPackageCompleted-=EssentialsImported;
            EditorApplication.delayCall+=Prepare;
        }
        [MenuItem("DHARA/Build Windows application")]
        public static void BuildWindows()
        {
            if(!File.Exists("Assets/Dhara/SetupComplete.txt")) Prepare();
            if(!File.Exists("Assets/Dhara/SetupComplete.txt")) throw new InvalidOperationException("Finish first-open package and font preparation before building.");
            if(!File.Exists(ScenePath)) throw new InvalidOperationException("Main scene was not prepared.");
            Directory.CreateDirectory("Builds/Windows");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{ScenePath}, locationPathName="Builds/Windows/DHARA.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None });
            if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed: "+report.summary.result);
            Debug.Log("Windows build created: Builds/Windows/DHARA.exe");
        }
    }
}
