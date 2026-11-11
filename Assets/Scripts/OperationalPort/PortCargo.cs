using UnityEngine;

public class PortCargo : MonoBehaviour
{
    public string cargoId="C1";
    public Rigidbody body;
    public Collider footprint;
    [System.NonSerialized] public CargoCustody custody;
    [System.NonSerialized] public string slotId;
    public Vector3 Top=>body.position+Vector3.up*1.3f;
}
