using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
static class OperationalXRHandsOriginMigration
{
    const string ScenePath="Assets/Scenes/OperationalPort.unity";
    const string HandsOriginPath="Assets/Samples/XR Interaction Toolkit/3.1.1/Hands Interaction Demo/Prefabs/XR Origin Hands (XR Rig).prefab";

    static OperationalXRHandsOriginMigration()
    {
        EditorApplication.delayCall+=TryMigrate;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=TryMigrate;
        };
    }

    [MenuItem("Tools/Operational Port/Use Hands XR Origin")]
    static void TryMigrate()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;

        Scene scene=SceneManager.GetActiveScene();
        if(!scene.IsValid()||scene.path!=ScenePath)return;

        PortView view=Object.FindFirstObjectByType<PortView>(FindObjectsInactive.Include);
        if(view==null)return;

        XROrigin handsOrigin=null;
        foreach(var candidate in Object.FindObjectsByType<XROrigin>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(candidate.name.Contains("XR Origin Hands"))
            {
                handsOrigin=candidate;
                break;
            }

        XROrigin previousOrigin=view.origin;
        bool changed=false;
        if(handsOrigin==null)
        {
            GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(HandsOriginPath);
            if(prefab==null)
            {
                Debug.LogError($"Cannot migrate Operational Port to the Hands XR Origin: prefab missing at {HandsOriginPath}.");
                return;
            }

            GameObject instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
            handsOrigin=instance.GetComponent<XROrigin>();
            if(previousOrigin!=null)
                instance.transform.SetPositionAndRotation(previousOrigin.transform.position,previousOrigin.transform.rotation);
            Undo.RegisterCreatedObjectUndo(instance,"Add Hands XR Origin");
            changed=true;
        }

        if(!handsOrigin.gameObject.activeSelf)
        {
            handsOrigin.gameObject.SetActive(true);
            changed=true;
        }
        if(view.origin!=handsOrigin)
        {
            view.origin=handsOrigin;
            EditorUtility.SetDirty(view);
            changed=true;
        }
        if(view.worldPanel.worldCamera!=handsOrigin.Camera)
        {
            view.worldPanel.worldCamera=handsOrigin.Camera;
            EditorUtility.SetDirty(view.worldPanel);
            changed=true;
        }

        Transform locomotion=handsOrigin.transform.Find("Locomotion");
        if(locomotion!=null&&locomotion.gameObject.activeSelf)
        {
            locomotion.gameObject.SetActive(false);
            EditorUtility.SetDirty(locomotion.gameObject);
            changed=true;
        }

        Transform panel=view.worldPanel.transform;
        Vector3 panelPosition=new Vector3(1.6f,1.3f,1f);
        Quaternion panelRotation=Quaternion.Euler(0,45,0);
        Vector3 panelScale=Vector3.one*.0013f;
        if(panel.parent!=view.cabin||panel.localPosition!=panelPosition||panel.localRotation!=panelRotation||panel.localScale!=panelScale)
        {
            panel.SetParent(view.cabin,false);
            panel.localPosition=panelPosition;
            panel.localRotation=panelRotation;
            panel.localScale=panelScale;
            EditorUtility.SetDirty(panel);
            changed=true;
        }

        if(previousOrigin!=null&&previousOrigin!=handsOrigin)
        {
            Undo.DestroyObjectImmediate(previousOrigin.gameObject);
            changed=true;
        }

        if(!changed)return;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Operational Port now uses XR Origin Hands (XR Rig) as its single XR origin.",handsOrigin);
    }
}
