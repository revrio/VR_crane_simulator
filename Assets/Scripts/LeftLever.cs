using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class LeftLever : MonoBehaviour
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
        if (manager != null) action = manager.Actions.XRILeft.Thumbstick;
    }

    private void Update()
    {
        bool selected = grab != null && grab.isSelected;
        if (selected != held)
        {
            held = selected;
            if (held) restrictor?.AcquireControls(this); else restrictor?.ReleaseControls(this);
        }
        Vector2 axis = held && action != null ? action.ReadValue<Vector2>() : Vector2.zero;
        CraneController?.MoveCar(Mathf.Abs(axis.y) >= Mathf.Abs(axis.x) && Mathf.Abs(axis.y) > 0.1f ? axis.y : 0f);
        CraneController?.CraneRotation(Mathf.Abs(axis.x) > Mathf.Abs(axis.y) && Mathf.Abs(axis.x) > 0.1f ? axis.x : 0f);
    }

    private void OnDisable()
    {
        held = false;
        restrictor?.ReleaseControls(this);
        CraneController?.MoveCar(0f);
        CraneController?.CraneRotation(0f);
    }
}
