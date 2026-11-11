using UnityEngine;
public class PortCargoContact : MonoBehaviour
{
    public PortDirector director;
    void OnCollisionEnter(Collision collision)=>director.Contact(collision,GetComponent<PortCargo>());
    void OnCollisionStay(Collision collision){if(director.safety!=null)director.Contact(collision,GetComponent<PortCargo>());}
}
