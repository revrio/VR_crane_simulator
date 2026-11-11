using UnityEngine;
public class PortSpreaderContact : MonoBehaviour
{
    public PortDirector director;
    void OnCollisionEnter(Collision collision)=>Report(collision);
    void OnCollisionStay(Collision collision)=>Report(collision);
    void Report(Collision collision)
    {
        director.RecordLoadContact(collision.rigidbody);
        director.safety?.Contact(collision,director.crane.hookBody,"SPREADER");
    }
}
