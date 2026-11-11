#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class PortWebBuild
{
    [MenuItem("VR Research Demo/Build Operational Web")]
    public static void Build()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            throw new InvalidOperationException("Install WebGL Build Support for Unity 6000.0.26f1 first.");
        string output = Environment.GetEnvironmentVariable("PORTLAB_WEB_OUTPUT");
        if (string.IsNullOrEmpty(output)) output = "Builds/WebGL";
        byte[] settings = File.ReadAllBytes("ProjectSettings/ProjectSettings.asset");
        try
        {
            PlayerSettings.productName = "PortLab Operations";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.template = "PROJECT:DownloadFirst";
            // The packaging step compresses files for our download barrier. Unity
            // receives fully downloaded, decompressed Blob URLs at startup.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = false;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.initialMemorySize = 128;
            PlayerSettings.WebGL.maximumMemorySize = 2048;
            PlayerSettings.runInBackground = false;
            Directory.CreateDirectory(output);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { OperationalPortBuilder.ScenePath },
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Web build failed: " + report.summary.result);
            Debug.Log("PORTLAB_WEB_BUILT " + output);
        }
        finally { File.WriteAllBytes("ProjectSettings/ProjectSettings.asset", settings); }
    }
}
#endif
