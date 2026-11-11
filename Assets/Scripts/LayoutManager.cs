using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class LayoutManager : MonoBehaviour
{
    public LayoutConfig defaultLayout;
    public Transform[] targetAreaTransforms = Array.Empty<Transform>();
    public Transform[] loadTransforms = Array.Empty<Transform>();
    public Rigidbody[] loadRigidbodies = Array.Empty<Rigidbody>();
    public LayoutConfig ActiveLayout { get; private set; }

    private void Awake() => ApplyLayout(LayoutSelector.HasSelection ? LayoutSelector.Selected : defaultLayout);
    private void Start()
    {
        if (LevelManager.Instance != null && ActiveLayout != null) LevelManager.Instance.SetLayoutName(ActiveLayout.layoutName);
    }
    public bool ApplyLayout(LayoutConfig config)
    {
        if (config == null || config.targetPositions == null || config.loadSpawnPositions == null ||
            config.targetPositions.Length != targetAreaTransforms.Length || config.loadSpawnPositions.Length != loadTransforms.Length)
        { Debug.LogError("[LayoutManager] Layout must contain one position per target and load."); return false; }
        for (int i = 0; i < targetAreaTransforms.Length; i++)
        {
            if (targetAreaTransforms[i] == null) return false;
            targetAreaTransforms[i].position = config.targetPositions[i];
            if (config.targetYawDegrees != null && config.targetYawDegrees.Length == targetAreaTransforms.Length)
                targetAreaTransforms[i].rotation = Quaternion.Euler(0, config.targetYawDegrees[i], 0);
        }
        for (int i = 0; i < loadTransforms.Length; i++)
        {
            if (loadTransforms[i] == null) return false;
            loadTransforms[i].position = config.loadSpawnPositions[i];
            loadTransforms[i].rotation = Quaternion.Euler(0,
                config.loadYawDegrees != null && config.loadYawDegrees.Length == loadTransforms.Length ? config.loadYawDegrees[i] : 0, 0);
            if (i < loadRigidbodies.Length && loadRigidbodies[i] != null && !loadRigidbodies[i].isKinematic)
            { loadRigidbodies[i].linearVelocity = Vector3.zero; loadRigidbodies[i].angularVelocity = Vector3.zero; }
        }
        ActiveLayout = config;
        if (LevelManager.Instance != null) LevelManager.Instance.SetLayoutName(config.layoutName);
        return true;
    }
    public string GetActiveLayoutName() => ActiveLayout != null ? ActiveLayout.layoutName : "Unknown";
}
