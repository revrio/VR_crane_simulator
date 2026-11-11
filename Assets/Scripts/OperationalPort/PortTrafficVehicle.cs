using UnityEngine;

public class PortTrafficVehicle : MonoBehaviour
{
    public string vehicleId;
    public Rigidbody body;
    public Transform[] wheels;
    public AudioSource motor;
    public float laneZ=-38,direction=1,startX=-65;
    public bool Waiting {get;private set;}
    public int Visits {get;private set;}
    public float Distance {get;private set;}
    float turn;
    public void ResetVehicle()
    {
        body.position=new Vector3(startX,0,laneZ);body.rotation=Quaternion.Euler(0,direction>0?90:-90,0);Waiting=false;Visits=0;Distance=turn=0;
        if(motor!=null)motor.volume=0;
    }
    public void Step(float speed,float dt,bool gate)
    {
        Vector3 p=body.position;bool before=direction>0?p.x<=-35.99f:p.x>=-18.01f;
        Waiting=gate&&before&&Mathf.Abs(p.x-(direction>0?-36:-18))<.1f;
        float next=p.x+direction*speed*dt;
        if(gate&&before)next=direction>0?Mathf.Min(next,-36):Mathf.Max(next,-18);
        float moved=Mathf.Abs(next-p.x);Distance+=moved;turn+=moved*65;
        if(Mathf.Abs(next)>76){next=-next;Visits++;}
        body.MovePosition(new Vector3(next,0,laneZ));
        foreach(var wheel in wheels)wheel.localRotation=Quaternion.Euler(0,0,90)*Quaternion.Euler(0,turn,0);
        if(motor!=null){motor.volume=moved>.001f?.07f:.025f;motor.pitch=moved>.001f?.9f:.6f;}
    }
    public void Silence(){if(motor!=null)motor.volume=0;}
}
