using UnityEngine;
using System.Collections.Generic;

public class PlayerRestrictor : MonoBehaviour
{
    private readonly HashSet<object> controlOwners = new HashSet<object>();
    private readonly Dictionary<GameObject, bool> previousStates = new Dictionary<GameObject, bool>();

    public void AcquireControls(object owner)
    {
        if (!controlOwners.Add(owner) || controlOwners.Count != 1) return;
        foreach (var obj in new[] { Locomotion, RightTeleportInteractor, RightTeleportStabilizer, LeftTeleportInteractor, LeftTeleportStabilizer })
        {
            if (obj == null) continue;
            previousStates[obj] = obj.activeSelf;
            obj.SetActive(false);
        }
    }

    public void ReleaseControls(object owner)
    {
        if (!controlOwners.Remove(owner) || controlOwners.Count != 0) return;
        foreach (var pair in previousStates) if (pair.Key != null) pair.Key.SetActive(pair.Value);
        previousStates.Clear();
    }
    [SerializeField] private GameObject Locomotion;
    [SerializeField] private GameObject Turn; // In Locomotion
    [SerializeField] private GameObject Move; // In Locomotion   
    [SerializeField] private GameObject Teleportation; // In Locomotion
    [SerializeField] private GameObject RightTeleportInteractor;
    [SerializeField] private GameObject RightNearFarInteractor;
    [SerializeField] private GameObject RightTeleportStabilizer;
    [SerializeField] private GameObject LeftTeleportInteractor;
    [SerializeField] private GameObject LeftNearFarInteractor;
    [SerializeField] private GameObject LeftTeleportStabilizer;

    public void DisableLocomotion()
    {
        Locomotion.SetActive(false);
    }
    public void EnableLocomotion()
    {
        Locomotion.SetActive(true);
    }
    public void EnableNearFarInteractor()
    {
        RightNearFarInteractor.SetActive(true);
        LeftNearFarInteractor.SetActive(true);
    }
    public void DisableNearFarInteractor()
    {
        RightNearFarInteractor.SetActive(false);
        LeftNearFarInteractor.SetActive(false);
    }

    public void DisableTeleportInteractors()
    {
        RightTeleportInteractor.SetActive(false);
        RightTeleportStabilizer.SetActive(false);
        LeftTeleportInteractor.SetActive(false);
        LeftTeleportStabilizer.SetActive(false);
    }
    public void EnableTeleportInteractors()
    {
        RightTeleportInteractor.SetActive(true);
        RightTeleportStabilizer.SetActive(true);
        LeftTeleportInteractor.SetActive(true);
        LeftTeleportStabilizer.SetActive(true);
    }
}
