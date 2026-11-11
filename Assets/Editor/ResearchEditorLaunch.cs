#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ResearchEditorLaunch
{
    [MenuItem("VR Research Demo/Open Research Station")]
    public static void Open()
    {
        EditorSceneManager.OpenScene(ResearchDemoBuilder.ScenePath);
        Selection.activeGameObject = Object.FindFirstObjectByType<ExperimentSession>().gameObject;
        EditorApplication.ExecuteMenuItem("Window/General/Game");
    }
}
#endif
