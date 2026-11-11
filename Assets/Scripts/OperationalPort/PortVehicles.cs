using UnityEngine;

public class PortVehicles : MonoBehaviour
{
    public PortDirector director;
    public Transform truck,handler,boom,innerBoom,toolHead;
    public Transform[] truckWheels,handlerWheels;
    public AudioSource truckMotor,handlerMotor,craneMotor;
    public float gripHeight=2.7f;
    Vector3 lastTruck,lastHandler;
    float truckTurn,handlerTurn;
    float lastGantry,lastTrolley,lastRope;
    void Start(){lastTruck=truck.position;lastHandler=handler.position;lastGantry=director.crane.GantryX;lastTrolley=director.crane.TrolleyZ;lastRope=director.crane.rope.maxLength;}
    void LateUpdate()
    {
        SyncRig();
        float td=Vector3.Distance(truck.position,lastTruck),hd=Vector3.Distance(handler.position,lastHandler);
        truckTurn+=td*70;handlerTurn+=hd*45;
        foreach(var wheel in truckWheels)wheel.localRotation=Quaternion.Euler(0,0,90)*Quaternion.Euler(0,truckTurn,0);
        foreach(var wheel in handlerWheels)wheel.localRotation=Quaternion.Euler(0,0,90)*Quaternion.Euler(0,handlerTurn,0);
        lastTruck=truck.position;lastHandler=handler.position;
        bool active=director.Active&&!director.Paused;
        truckMotor.volume=active?(td>.001f?.12f:.035f):0;truckMotor.pitch=td>.001f?.85f:.60f;
        handlerMotor.volume=active&&director.Stage>=PortStage.HandlerApproach&&director.Stage<=PortStage.HandlerClear?.11f:0;
        handlerMotor.pitch=hd>.001f?.85f:.65f;
        bool craneMoving=Mathf.Abs(director.crane.GantryX-lastGantry)+Mathf.Abs(director.crane.TrolleyZ-lastTrolley)+Mathf.Abs(director.crane.rope.maxLength-lastRope)>.0005f;
        craneMotor.volume=active&&director.CanOperate&&craneMoving?director.crane.Command.magnitude*.075f:0;
        craneMotor.pitch=.65f+director.crane.Command.magnitude*.16f;
        lastGantry=director.crane.GantryX;lastTrolley=director.crane.TrolleyZ;lastRope=director.crane.rope.maxLength;
    }
    public void SyncRig()
    {
        Vector3 grip=director.HandlerCargoPoint+Vector3.up*(director.config.cargoSize.y*.5f+.18f);
        toolHead.SetPositionAndRotation(grip,Quaternion.identity);
        Vector3 a=handler.TransformPoint(new Vector3(0,2.25f,-1.6f)),delta=grip-a;
        boom.SetPositionAndRotation((a+grip)*.5f,Quaternion.LookRotation(delta));boom.localScale=new Vector3(.8f,.65f,delta.magnitude);
        innerBoom.SetPositionAndRotation(Vector3.Lerp(a,grip,.72f),Quaternion.LookRotation(delta));innerBoom.localScale=new Vector3(.62f,.48f,delta.magnitude*.56f);
    }
}
