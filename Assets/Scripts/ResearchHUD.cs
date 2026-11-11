// ResearchHUD.cs
// World-Space Canvas that displays real-time session metrics for the ergonomics professor.
// Attach to a Canvas set to World Space. Place it on the crane cabin wall or operator panel.
// The HUD shows: Layout name | Timer | Containers placed | Penalties

using TMPro;
using UnityEngine;

public class ResearchHUD : MonoBehaviour
{
    [Header("Text References — assign in Inspector")]
    [Tooltip("Displays the active layout name, e.g. 'Layout A — Wide Bay'")]
    public TextMeshProUGUI layoutNameText;

    [Tooltip("Large MM:SS clock visible at a glance.")]
    public TextMeshProUGUI timerText;

    [Tooltip("X / N containers placed counter.")]
    public TextMeshProUGUI placedText;

    [Tooltip("Penalty count. Turns amber at 3, red at 6.")]
    public TextMeshProUGUI penaltyText;

    [Tooltip("Stars earned (optional — mirrors the existing star UI).")]
    public TextMeshProUGUI starText;

    [Header("Penalty Colour Thresholds")]
    public Color penaltyNormal  = Color.white;
    public Color penaltyWarning = new Color(1f, 0.65f, 0f);   // amber
    public Color penaltyDanger  = Color.red;
    public int   warningThreshold = 3;
    public int   dangerThreshold  = 6;

    [Header("Update Rate")]
    [Tooltip("How many times per second the HUD refreshes (saves CPU vs. every frame).")]
    public float refreshRate = 10f;

    private float _refreshInterval;
    private float _nextRefreshTime;

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        _refreshInterval = 1f / Mathf.Max(1f, refreshRate);
        _nextRefreshTime = Time.time + _refreshInterval;

        // Initial blank state
        SetText(layoutNameText, "—");
        SetText(timerText,      "00:00");
        SetText(placedText,     "0 / 0");
        SetText(penaltyText,    "Penalties: 0");
        SetText(starText,       "");
    }

    void Update()
    {
        if (Time.time < _nextRefreshTime) return;
        _nextRefreshTime = Time.time + _refreshInterval;
        Refresh();
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void Refresh()
    {
        if (LevelManager.Instance == null) return;

        var lm = LevelManager.Instance;

        SetText(layoutNameText, lm.layoutName);
        SetText(timerText,      lm.GetFormattedTime());
        SetText(placedText,     lm.GetPlacedText());

        int pen = lm.PenaltyCount;
        SetText(penaltyText, $"Penalties: {pen}");
        if (penaltyText != null)
        {
            if      (pen >= dangerThreshold)  penaltyText.color = penaltyDanger;
            else if (pen >= warningThreshold) penaltyText.color = penaltyWarning;
            else                              penaltyText.color = penaltyNormal;
        }
    }

    private void SetText(TextMeshProUGUI tmp, string value)
    {
        if (tmp != null) tmp.text = value;
    }
}
