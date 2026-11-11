#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEngine;

public static class ResearchBuilds
{
    [MenuItem("VR Research Demo/Build Linux Desktop Demo")]
    public static void Linux()
    {
        var xr = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
        var loaders = xr.Manager.activeLoaders.ToList(); bool initialize = xr.InitManagerOnStart;
        try
        {
            foreach (var loader in loaders) xr.Manager.TryRemoveLoader(loader);
            xr.InitManagerOnStart = false;
            Build(BuildTarget.StandaloneLinux64, "Builds/Linux/PortLab.x86_64");
        }
        finally { xr.Manager.TrySetLoaders(loaders); xr.InitManagerOnStart = initialize; }
    }
    [MenuItem("VR Research Demo/Build Windows Quest Link Demo")]
    public static void Windows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/PortLab.exe");
    [MenuItem("VR Research Demo/Build Operational Linux")]
    public static void OperationalLinux()
    {
        var xr=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
        var loaders=xr.Manager.activeLoaders.ToList();bool initialize=xr.InitManagerOnStart;
        try{foreach(var loader in loaders)xr.Manager.TryRemoveLoader(loader);xr.InitManagerOnStart=false;Build(BuildTarget.StandaloneLinux64,"Builds/OperationalLinux/PortLabOperations.x86_64",OperationalPortBuilder.ScenePath);}
        finally{xr.Manager.TrySetLoaders(loaders);xr.InitManagerOnStart=initialize;}
    }
    [MenuItem("VR Research Demo/Build Operational Windows")]
    public static void OperationalWindows()=>Build(BuildTarget.StandaloneWindows64,"Builds/OperationalWindows/PortLabOperations.exe",OperationalPortBuilder.ScenePath);
    private static void Build(BuildTarget target, string path, string scenePath=ResearchDemoBuilder.ScenePath)
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
            throw new InvalidOperationException("Install the matching Unity 6000.0.26f1 build-support module for " + target + " in Unity Hub first.");
        if (!File.Exists(scenePath)) throw new InvalidOperationException("Build the requested scene first.");
        var namedTarget = UnityEditor.Build.NamedBuildTarget.Standalone;
        var backend = PlayerSettings.GetScriptingBackend(namedTarget);
        var preloaded = PlayerSettings.GetPreloadedAssets();
        var mode = PlayerSettings.fullScreenMode;
        string product=PlayerSettings.productName;
        byte[] originalSettings = File.ReadAllBytes("ProjectSettings/ProjectSettings.asset");
        try
        {
            PlayerSettings.SetScriptingBackend(namedTarget, ScriptingImplementation.Mono2x);
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            if(scenePath==OperationalPortBuilder.ScenePath)PlayerSettings.productName="PortLab Operations";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { scenePath },
                locationPathName = path, target = target, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Research player build failed: " + report.summary.result);
            Debug.Log("RESEARCH_PLAYER_BUILT " + path);
        }
        finally
        {
            PlayerSettings.SetScriptingBackend(namedTarget, backend);
            PlayerSettings.SetPreloadedAssets(preloaded);
            PlayerSettings.fullScreenMode = mode;
            PlayerSettings.productName = product;
            // Batch-mode editors can quit before native PlayerSettings flush their restored values.
            File.WriteAllBytes("ProjectSettings/ProjectSettings.asset", originalSettings);
        }
    }
}
#endif
