// MenuLayoutButton.cs
// Attach to each layout selection button in the Menu scene.
// Wire each button's LayoutConfig and the target scene name in the Inspector.

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuLayoutButton : MonoBehaviour
{
    [Header("Layout to load")]
    [Tooltip("Drag the corresponding LayoutConfig ScriptableObject here.")]
    public LayoutConfig layoutConfig;

    [Header("Scene to load")]
    [Tooltip("Exact name of the seaport scene (must be in Build Settings).")]
    public string targetSceneName = "SeaportDemo";

    [Header("Optional click sound")]
    public AudioClip clickSound;
    private AudioSource _audioSource;

    private Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();
        _audioSource = GetComponent<AudioSource>();

        if (_button != null)
            _button.onClick.AddListener(OnButtonClicked);
    }

    void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnButtonClicked);
    }

    public void OnButtonClicked()
    {
        if (layoutConfig == null)
        {
            Debug.LogError($"[MenuLayoutButton] No LayoutConfig assigned on {gameObject.name}.");
            return;
        }

        LayoutSelector.SetLayout(layoutConfig);

        if (clickSound != null && _audioSource != null)
            _audioSource.PlayOneShot(clickSound);

        Debug.Log($"[MenuLayoutButton] Loading scene '{targetSceneName}' with layout '{layoutConfig.layoutName}'");
        SceneManager.LoadScene(targetSceneName);
    }
}
