using UnityEngine;

[CreateAssetMenu(menuName="PortLab/Operational scenario")]
public class PortScenario : ScriptableObject
{
    public string version="operational-i1";
    public Vector3 cargoSize=new Vector3(6,2.6f,2.4f);
    public Vector3 yard=new Vector3(0,1.3f,-12), ship=new Vector3(0,4.3f,16), park=new Vector3(0,10,-12);
    public Vector3 truckStart=new Vector3(-44,0,-24), truckStand=new Vector3(-12,0,-24), truckExit=new Vector3(48,0,-24);
    public Vector3 truckCargoOffset=new Vector3(0,2.7f,-2);
    public Vector3 handlerStart=new Vector3(-14,0,-40), handlerPickup=new Vector3(-14,0,-31.5f), handlerDrop=new Vector3(0,0,-19.5f), handlerExit=new Vector3(16,0,-30);
    public float handlerReach=7.5f, truckSpeed=5, handlerSpeed=4, placementDwell=1.2f;
    public Vector2 gantryLimits=new Vector2(-12,12), trolleyLimits=new Vector2(-20,24);
    public float trolleyHeight=21, gantrySpeed=2.5f, trolleySpeed=5, hoistSpeed=3.5f;
    public bool multipleCargo;
    public float transitHookHeight=13;
    public bool anchorSettledCargo=true;
    public bool operationalTraffic;
    public float strongImpactSpeed=2.5f,nearClearance=.75f,personClearance=1.5f;
    public float crossingBoundary=6,workerSpeed=2,trafficSpeed=4;
}
