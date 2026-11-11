using UnityEngine;

public class PortWorker : MonoBehaviour
{
    public string workerId;
    public Rigidbody body;
    public CapsuleCollider shape;
    public Transform leftArm,rightArm,leftLeg,rightLeg,head;
    public Vector3 Home {get;private set;}
    public Vector3 Goal {get;private set;}
    public float Travelled {get;private set;}
    public bool Walking {get;private set;}
    float gait;
    public void ResetAt(Vector3 position)
    {
        Home=Goal=position;body.position=position;body.rotation=Quaternion.identity;Travelled=gait=0;Walking=false;Pose(0);
    }
    public bool At(Vector3 point)=>Vector3.Distance(body.position,point)<.06f;
    public void Step(Vector3 goal,float speed,float dt)
    {
        Goal=goal;Vector3 delta=goal-body.position;Walking=delta.magnitude>.04f;
        if(Walking)
        {
            Vector3 next=Vector3.MoveTowards(body.position,goal,speed*dt);Travelled+=Vector3.Distance(next,body.position);
            body.MovePosition(next);body.MoveRotation(Quaternion.RotateTowards(body.rotation,Quaternion.LookRotation(delta),240*dt));
            gait+=speed*dt*4;Pose(Mathf.Sin(gait)*27);
        }
        else Pose(0);
    }
    public void Halt(){Walking=false;}
    void Pose(float angle)
    {
        leftArm.localRotation=Quaternion.Euler(angle,0,0);rightArm.localRotation=Quaternion.Euler(-angle,0,0);
        leftLeg.localRotation=Quaternion.Euler(-angle,0,0);rightLeg.localRotation=Quaternion.Euler(angle,0,0);
    }
}
