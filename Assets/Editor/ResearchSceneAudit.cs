#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.XR.CoreUtils;

public static class ResearchSceneAudit
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
        var lines = new System.Collections.Generic.List<string>();
        foreach (var c in Object.FindObjectsByType<CraneController>(FindObjectsSortMode.None))
        {
            lines.Add($"CRANE {c.name} root={c.transform.root.name} rootpos={c.transform.root.position} scale={c.transform.root.lossyScale}");
            foreach (var t in new[] { c.hook, c.car, c.boomStartCorner, c.boomEndCorner, c.craneRotationPivot })
                lines.Add($"  {t.name} world={t.position} local={t.localPosition} scale={t.lossyScale}");
            lines.Add($"  rope hook {c.hookRope.hook.name} mass={c.hookRope.hook.mass} position={c.hookRope.hook.position} base={c.hookRope.basePoint.position}");
        }
        foreach (var l in Object.FindObjectsByType<Load>(FindObjectsSortMode.None))
            lines.Add($"LOAD {l.name} color={l.containerColor} position={l.transform.position} parent={l.transform.parent?.name} rb={l.GetComponent<Rigidbody>() != null}");
        foreach (var t in Object.FindObjectsByType<TargetArea>(FindObjectsSortMode.None))
            lines.Add($"TARGET {t.name} color={t.requiredColor} position={t.transform.position} bounds={t.GetComponent<Collider>().bounds}");
        foreach (var x in Object.FindObjectsByType<XROrigin>(FindObjectsSortMode.None))
            lines.Add($"XR {x.name} position={x.transform.position} camera={x.Camera.transform.position}");
        foreach (var r in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            var bounds = new Bounds(r.transform.position, Vector3.zero);
            foreach (var mesh in r.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(mesh.bounds);
            lines.Add($"ROOT {r.name} pos={r.transform.position} bounds={bounds}");
        }
        File.WriteAllLines("/tmp/vr-crane-scene-audit.txt", lines);
        Debug.Log("RESEARCH_SCENE_AUDIT_OK");
    }
}
#endif
