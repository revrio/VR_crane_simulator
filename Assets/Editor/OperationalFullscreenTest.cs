using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class OperationalFullscreenTest
{
    const string Request="/tmp/portlab-request-fullscreen";
    static OperationalFullscreenTest()
    {
        if(File.Exists(Request))EditorApplication.update+=LaunchPending;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("PortLab.FullscreenPending",false))
            {
                SessionState.SetBool("PortLab.FullscreenPending",false);
                MaximizeGame();
                Debug.Log("PORTLAB_FULLSCREEN_TEST_RUNNING");
            }
        };
    }
    static void LaunchPending()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        EditorApplication.update-=LaunchPending;
        File.Delete(Request);
        Launch();
    }
    [MenuItem("Tools/Operational Port/Start Fullscreen Test")]
    public static void Launch()
    {
        if(EditorApplication.isPlaying){MaximizeGame();return;}
        const string scene="Assets/Scenes/OperationalPort.unity";
        if(SceneManager.GetActiveScene().path!=scene)
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(scene);
        }
        SessionState.SetBool("PortLab.FullscreenPending",true);
        MaximizeGame();
        EditorApplication.EnterPlaymode();
    }
    static void MaximizeGame()
    {
        var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if(type==null)throw new InvalidOperationException("Unity Game view unavailable");
        var game=EditorWindow.GetWindow(type);
        game.Show();game.Focus();game.maximized=true;
    }
}
