// LayoutSelector.cs
// Lightweight static class that persists the chosen layout between Menu and Seaport scenes.
// Call LayoutSelector.SetLayout(config) from menu buttons; the Seaport scene reads it on Start.

using UnityEngine;

public static class LayoutSelector
{
    private static LayoutConfig _selectedLayout;

    /// <summary>The currently selected layout config. Persists across scene loads in the same app session.</summary>
    public static LayoutConfig Selected => _selectedLayout;

    /// <summary>Select a layout config from a UI button on the Menu scene.</summary>
    public static void SetLayout(LayoutConfig config)
    {
        _selectedLayout = config;
        Debug.Log($"[LayoutSelector] Selected: {config?.layoutName ?? "null"}");
    }

    /// <summary>Returns true if a layout has been chosen.</summary>
    public static bool HasSelection => _selectedLayout != null;
}
