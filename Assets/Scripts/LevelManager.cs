using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
    public string NextScene;

    public TargetArea[] targetAreas;
    private bool[] areaCompleted;

    public TextMeshProUGUI counterText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI resultsText;
    private int placedContainer = 0;
    private int containerAmount;

    private float elapsedTime = 0f;
    private bool levelEnded = false;

    [Header("Star Time Limits")]
    public float twoStar;
    public float threeStar;
    public float fourStar;
    public float fiveStar;

    [Header("Star UI")]
    public GameObject[] stars;

    [Header("Audio")]
    public AudioClip LevelCompleteAudioClip;
    public AudioClip PlacedAudioClip;

    // ── Research / Ergonomics fields ──────────────────────────────────────
    [Header("Research")]
    [Tooltip("Populated at runtime by LayoutManager. Also shown on ResearchHUD.")]
    public string layoutName = "Layout A";

    // Penalty count incremented by CraneController when a danger event fires
    private int penaltyCount = 0;

    /// <summary>Read-only access for ResearchHUD.</summary>
    public int PenaltyCount => penaltyCount;

    /// <summary>Call from LayoutManager after it reads the chosen config.</summary>
    public void SetLayoutName(string name) => layoutName = name;

    /// <summary>Formatted MM:SS string consumed by ResearchHUD.</summary>
    public string GetFormattedTime() => $"{minutes:00}:{seconds:00}";

    /// <summary>Placed / total string consumed by ResearchHUD.</summary>
    public string GetPlacedText() => $"{placedContainer} / {containerAmount}";

    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        areaCompleted = new bool[targetAreas.Length];

        containerAmount = targetAreas.Length;
        if (counterText != null) counterText.text = $"Placed: {placedContainer}/{containerAmount}";
    }


    private void Update()
    {
        if (levelEnded) return;

        elapsedTime += Time.deltaTime;
        UpdateTimerUI();

        
    }

    public void NotifyTargetFilled(TargetArea area)
    {
        int index = System.Array.IndexOf(targetAreas, area);
        if (index < 0 || areaCompleted[index]) return;

        areaCompleted[index] = true;

        if (AudioManagerVR.Instance != null) AudioManagerVR.Instance.PlaySFX2D(PlacedAudioClip);

        Debug.Log($"Target {index} filled!");

        UpdateContainerCounterUI();

        if (AllTargetsCompleted()) {
            if (AudioManagerVR.Instance != null) AudioManagerVR.Instance.PlaySFX2D(LevelCompleteAudioClip);
            EndLevel();
        }
    }

    public void UpdateContainerCounterUI()
    {
        placedContainer++;
        if (counterText != null) counterText.text = $"Placed: {placedContainer}/{containerAmount}";
        //Play sound
    }

    private int minutes;
    private int seconds;

    private void UpdateTimerUI()
    {
        minutes = Mathf.FloorToInt(elapsedTime / 60f);
        seconds = Mathf.FloorToInt(elapsedTime % 60f);

        if (timerText != null)
            timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private bool AllTargetsCompleted()
    {
        foreach (bool completed in areaCompleted)
        {
            if (!completed) return false;
        }
        return true;
    }

    // ── Research Methods ──────────────────────────────────────────────────

    /// <summary>Called by CraneController whenever a danger/over-speed event starts.</summary>
    public void RecordPenalty()
    {
        penaltyCount++;
        Debug.Log($"[LevelManager] Penalty recorded. Total: {penaltyCount}");
    }

    /// <summary>
    /// Appends one row to session_log.csv in Application.persistentDataPath.
    /// On Meta Quest the file lands in:
    ///   /storage/emulated/0/Android/data/&lt;package&gt;/files/session_log.csv
    /// Pull it via adb: adb pull /sdcard/Android/data/&lt;package&gt;/files/session_log.csv
    /// </summary>
    private void ExportSessionCSV()
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, "session_log.csv");
            bool writeHeader = !File.Exists(path);

            using (StreamWriter w = new StreamWriter(path, append: true))
            {
                if (writeHeader)
                    w.WriteLine("Timestamp,Layout,Time_seconds,Containers_Placed,Containers_Total,Penalties,Stars");

                int stars = CalculateStars(elapsedTime);
                w.WriteLine(
                    $"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}," +
                    $"{layoutName}," +
                    $"{elapsedTime.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}," +
                    $"{placedContainer}," +
                    $"{containerAmount}," +
                    $"{penaltyCount}," +
                    $"{stars}"
                );
            }

            Debug.Log($"[LevelManager] Session exported → {path}");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[LevelManager] CSV export failed: {ex.Message}");
        }
    }

    // ─────────────────────────────────────────────────────────────────────

    private void EndLevel()
    {
        if (levelEnded) return;

        levelEnded = true;

        int starCount = CalculateStars(elapsedTime);
        ShowStars(starCount);

        Debug.Log($"Level complete in {elapsedTime:F1} seconds. Stars: {starCount} | Penalties: {penaltyCount}");

        if (resultsText != null && resultsText.transform.parent != null) resultsText.transform.parent.gameObject.SetActive(true);

        if (resultsText != null)
            resultsText.text =
                $"Level Completed!\n" +
                $"Time: {minutes:00}:{seconds:00}\n" +
                $"Penalties: {penaltyCount}";

        ExportSessionCSV();

        StartCoroutine(LoadNextSceneAfterDelay(5f));
    }

    private void ShowStars(int count)
    {
        for (int i = 0; i < stars.Length; i++)
        {
            stars[i].SetActive(i < count);
        }
    }

    private IEnumerator LoadNextSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(NextScene);
    }

    private int CalculateStars(float time)
    {
        if (time <= fiveStar)
            return 5;
        else if (time <= fourStar)
            return 4;
        else if (time <= threeStar)
            return 3;
        else if (time <= twoStar)
            return 2;
        else
            return 1;
    }
}
