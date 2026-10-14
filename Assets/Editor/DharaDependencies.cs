using System;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

// This assembly has no application dependencies, so a clean project can resolve
// its packages before the DHARA_READY application assemblies are enabled.
[InitializeOnLoad]
public static class DharaDependencies
{
    static AddAndRemoveRequest request;
    static double deadline;
    static readonly string[] Required = {
        "com.unity.render-pipelines.universal", "com.unity.ugui", "com.unity.inputsystem", "com.unity.test-framework",
        "com.unity.modules.unitywebrequest", "com.unity.modules.particlesystem", "com.unity.modules.audio", "com.unity.modules.physics"
    };
    static DharaDependencies() { EditorApplication.delayCall+=Ensure; }
    static bool Ready => PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Standalone).Split(';').Contains("DHARA_READY");
    static void Ensure() { if(!Ready && !EditorApplication.isCompiling && !EditorApplication.isUpdating) Install(); else if(!Ready) EditorApplication.delayCall+=Ensure; }
    [MenuItem("DHARA/Install or repair dependencies")]
    public static void Install()
    {
        if(request!=null) return;
        Debug.Log("DHARA resolving required Unity packages. Application compilation follows package resolution.");
        request=Client.AddAndRemove(Required); deadline=EditorApplication.timeSinceStartup+600; EditorApplication.update+=Poll;
    }
    static void Poll()
    {
        if(!request.IsCompleted)
        {
            if(EditorApplication.timeSinceStartup>deadline) { EditorApplication.update-=Poll; request=null; Debug.LogError("DHARA package resolution timed out. Use DHARA > Install or repair dependencies to retry."); }
            return;
        }
        EditorApplication.update-=Poll;
        if(request.Status==StatusCode.Failure) { Debug.LogError("DHARA package setup failed: "+request.Error?.message); request=null; return; }
        request=null;
        var symbols=PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Standalone);
        if(!symbols.Split(';').Contains("DHARA_READY")) PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Standalone,symbols+";DHARA_READY");
        Debug.Log("DHARA packages resolved. Application scripts are now enabled.");
    }
}
