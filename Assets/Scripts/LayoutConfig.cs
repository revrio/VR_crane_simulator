// LayoutConfig.cs
// ScriptableObject that defines a named layout variant for the seaport experiment.
// Create instances via: Right-click in Project > Create > Research > Layout Config

using UnityEngine;

[CreateAssetMenu(menuName = "Research/Layout Config", fileName = "NewLayoutConfig")]
public class LayoutConfig : ScriptableObject
{
    public string layoutId = "A";
    public float[] targetYawDegrees;
    public float[] loadYawDegrees;
    [Header("Identity")]
    [Tooltip("Shown on the Research HUD and in CSV export.")]
    public string layoutName = "Layout A — Wide Bay";

    [Header("Description (for prof reference)")]
    [TextArea(2, 5)]
    public string description = "Containers spread across the wide bay. Low spatial pressure, baseline difficulty.";

    [Header("Target Zone World Positions")]
    [Tooltip("World-space positions for each TargetArea. Must match the number of TargetArea objects in the scene.")]
    public Vector3[] targetPositions;

    [Header("Spawn Positions for Load Containers")]
    [Tooltip("World-space positions where load containers spawn at scene start.")]
    public Vector3[] loadSpawnPositions;

    [Header("Difficulty")]
    [Range(1, 5)]
    public int difficultyRating = 2;
}
