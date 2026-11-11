using UnityEngine;

[DefaultExecutionOrder(-50)]
public class PortCrane : MonoBehaviour
{
    public PortDirector director;
    public Transform frame,trolley,spreader;
    public Rigidbody hookBody;
    public Rope rope;
    public Transform[] cables;
    public Vector3 Command;
    public float GantryX {get;private set;}
    public float TrolleyZ {get;private set;}
    float hoistRate;
    public Vector3 CouplingPoint=>hookBody.position+Vector3.down*.45f;
    public void ResetCrane()
    {
        Command=Vector3.zero;hoistRate=0;GantryX=0;TrolleyZ=director.config.park.z;
        hookBody.isKinematic=true;hookBody.position=director.config.park;hookBody.rotation=Quaternion.identity;
        rope.maxLength=director.config.trolleyHeight-director.config.park.y;
        SyncFrame();SyncVisuals();
    }
    void FixedUpdate()
    {
        if(!director.CanOperate||director.Paused){Command=Vector3.zero;return;}
        var c=director.config;float dt=Time.fixedDeltaTime;
        GantryX=Mathf.Clamp(GantryX+Command.x*c.gantrySpeed*dt,c.gantryLimits.x,c.gantryLimits.y);
        float proposed=Mathf.Clamp(TrolleyZ+Command.y*c.trolleySpeed*dt,c.trolleyLimits.x,c.trolleyLimits.y);
        TrolleyZ=director.traffic!=null?director.traffic.ClampTrolley(proposed,Command.y):proposed;
        float targetRate=Command.z*c.hoistSpeed;
        // Ramp into a lift, but stop the winch immediately at neutral.
        hoistRate=Mathf.Abs(targetRate)<.01f?0:Mathf.MoveTowards(hoistRate,targetRate,2f*dt);
        rope.maxLength=Mathf.Clamp(rope.maxLength+hoistRate*dt,3,21);
        SyncFrame();
    }
    void SyncFrame(){frame.position=new Vector3(GantryX,0,0);trolley.position=new Vector3(GantryX,director.config.trolleyHeight,TrolleyZ);}
    void LateUpdate()=>SyncVisuals();
    public void SyncVisuals()
    {
        spreader.SetPositionAndRotation(hookBody.position,hookBody.rotation);
        for(int i=0;i<cables.Length;i++)
        {
            float x=i<2?-.65f:.65f,z=i%2==0?-.4f:.4f;
            Vector3 top=trolley.position+new Vector3(x,-.2f,z),bottom=hookBody.position+new Vector3(x*.6f,.95f,z);
            Vector3 delta=top-bottom;cables[i].SetPositionAndRotation((top+bottom)*.5f,Quaternion.FromToRotation(Vector3.up,delta));
            cables[i].localScale=new Vector3(.038f,delta.magnitude*.5f,.038f);
        }
    }
}
