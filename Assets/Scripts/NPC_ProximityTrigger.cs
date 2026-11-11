// NPC_ProximityTrigger.cs
// Controls the female assistant NPC near the crane cabin.
// Triggers an Animator Wave when the player enters proximity, then returns to Idle.
// Attach to the root GameObject of the NPC prefab.

using System.Collections;
using UnityEngine;
using Unity.XR.CoreUtils;

public class NPC_ProximityTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Animator on the character mesh. Must have triggers: Wave, Point.")]
    public Animator animator;

    [Tooltip("If left null, will auto-find the XR Origin's camera in the scene.")]
    public Transform playerHead;

    [Header("Proximity Settings")]
    [Tooltip("Distance at which the NPC reacts to the player (metres).")]
    public float triggerRadius = 3.5f;

    [Tooltip("Minimum seconds between wave interactions.")]
    public float waveCooldown = 10f;

    [Tooltip("One-shot wave happens this many seconds after scene load (greeting).")]
    public float greetingDelay = 3f;

    [Header("Idle Bob (makes NPC look alive without animation)")]
    [Tooltip("Enable subtle up/down breathing when idle.")]
    public bool enableIdleBob = true;

    [Tooltip("Vertical bob amplitude in metres.")]
    public float bobAmplitude = 0.003f;

    [Tooltip("Bob cycles per second.")]
    public float bobFrequency = 0.5f;

    // Animator parameter hashes — cached for performance
    private static readonly int WaveTrigger  = Animator.StringToHash("Wave");
    private static readonly int PointTrigger = Animator.StringToHash("Point");

    private float _nextWaveAllowedTime;
    private bool  _playerInRange;
    private bool  _greetingDone;
    private Vector3 _basePosition;

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        _basePosition = transform.position;

        // Auto-find player head if not assigned
        if (playerHead == null)
        {
            var origin = FindAnyObjectByType<XROrigin>();
            if (origin != null)
                playerHead = origin.Camera.transform;
            else
                Debug.LogWarning("[NPC_ProximityTrigger] Could not find XROrigin. Assign playerHead manually.");
        }

        // Trigger greeting wave after greetingDelay seconds
        StartCoroutine(GreetingRoutine());
    }

    void Update()
    {
        if (playerHead == null) return;

        HandleProximity();
        HandleIdleBob();
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void HandleProximity()
    {
        float dist = Vector3.Distance(transform.position, playerHead.position);
        bool inRange = dist < triggerRadius;

        // Rising edge: player just walked into range
        if (inRange && !_playerInRange && _greetingDone)
            TryTriggerWave();

        _playerInRange = inRange;
    }

    private void HandleIdleBob()
    {
        if (!enableIdleBob) return;

        float newY = _basePosition.y + Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void TryTriggerWave()
    {
        if (Time.time < _nextWaveAllowedTime) return;

        if (animator != null)
            animator.SetTrigger(WaveTrigger);

        _nextWaveAllowedTime = Time.time + waveCooldown;
        Debug.Log("[NPC] Wave triggered.");
    }

    /// <summary>Triggers a pointing animation (call from script or UnityEvent if needed).</summary>
    public void TriggerPoint()
    {
        if (animator != null)
            animator.SetTrigger(PointTrigger);
    }

    private IEnumerator GreetingRoutine()
    {
        yield return new WaitForSeconds(greetingDelay);
        if (animator != null)
            animator.SetTrigger(WaveTrigger);
        _nextWaveAllowedTime = Time.time + waveCooldown;
        _greetingDone = true;
        Debug.Log("[NPC] Greeting wave done.");
    }

    // Draw gizmo in Scene view so you can see the trigger radius
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.35f);
        Gizmos.DrawSphere(transform.position, triggerRadius);
    }
}
