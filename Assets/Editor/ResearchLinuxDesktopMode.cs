#if UNITY_EDITOR_LINUX
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Management;

// Local desktop playback must not start this package's unsupported Linux OpenXR loader.
// Keep this change in memory for research-scene Play mode; Windows lab settings are preserved.
[InitializeOnLoad]
public static class ResearchLinuxDesktopMode
{
    private const string RestoreKey = "PortLabRestoreXrInitialization";
    static ResearchLinuxDesktopMode() => EditorApplication.playModeStateChanged += OnState;
    private static void OnState(PlayModeStateChange state)
    {
        var settings = XRGeneralSettings.Instance;
        if (settings == null) return;
        string path=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        if (state == PlayModeStateChange.ExitingEditMode && (path == ResearchDemoBuilder.ScenePath || path == OperationalPortBuilder.ScenePath || path == "Assets/Scenes/OperationalPortIteration1.unity" || path == "Assets/Scenes/OperationalPortIteration2.unity"))
        {
            SessionState.SetBool(RestoreKey, settings.InitManagerOnStart);
            settings.InitManagerOnStart = false;
        }
        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(RestoreKey, false))
        {
            settings.InitManagerOnStart = true; SessionState.SetBool(RestoreKey, false);
        }
    }
}
#endif
