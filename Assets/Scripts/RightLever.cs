using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class RightLever : MonoBehaviour
{
    public CraneController CraneController;
    private XRGrabInteractable grab;
    private InputAction action;
    private PlayerRestrictor restrictor;
    private bool held;

    private void Start()
    {
        grab = GetComponent<XRGrabInteractable>();
        restrictor = FindAnyObjectByType<PlayerRestrictor>();
        var manager = GetComponentInParent<InputManager>();
        if (manager != null) action = manager.Actions.XRIRight.Thumbstick;
    }

    private void Update()
    {
        bool selected = grab != null && grab.isSelected;
        if (selected != held)
        {
            held = selected;
            if (held) restrictor?.AcquireControls(this); else restrictor?.ReleaseControls(this);
        }
        float value = held && action != null ? action.ReadValue<Vector2>().y : 0f;
        CraneController?.MoveHookY(Mathf.Abs(value) > 0.1f ? value : 0f);
    }

    private void OnDisable()
    {
        held = false;
        restrictor?.ReleaseControls(this);
        CraneController?.MoveHookY(0f);
    }
}
