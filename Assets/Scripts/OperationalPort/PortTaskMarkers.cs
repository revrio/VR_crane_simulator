using TMPro;
using UnityEngine;

[DefaultExecutionOrder(400)]
public class PortTaskMarkers : MonoBehaviour
{
    public PortDirector director;
    public LineRenderer source,target,route;
    public TextMeshPro sourceLabel,targetLabel;
    Renderer[] parkingMarkers;
    void Start()
    {
        // Retire the old fixed blue yard outline in previously saved scenes.
        source.startColor=source.endColor=new Color(.1f,.9f,1f);
        source.material=new Material(source.sharedMaterial);
        source.material.SetColor("_BaseColor",new Color(.1f,.9f,1f));
        source.useWorldSpace=target.useWorldSpace=route.useWorldSpace=true;
        var parking=new System.Collections.Generic.List<Renderer>();
        foreach(var item in FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            // The old Outline builder made four separate objects with the
            // same name, so disabling GameObject.Find's first match is not enough.
            if(item.name=="Yard buffer / C1")item.enabled=false;
            if(item.name.StartsWith("Final parking"))parking.Add(item);
        }
        parkingMarkers=parking.ToArray();
    }
    void LateUpdate()
    {
        if(parkingMarkers!=null)foreach(var line in parkingMarkers)if(line!=null)line.enabled=director.Stage==PortStage.Park;
        bool show=director.Plan!=null&&director.CurrentMove!=null&&director.Stage!=PortStage.Complete;
        source.enabled=target.enabled=route.enabled=show;sourceLabel.gameObject.SetActive(show);targetLabel.gameObject.SetActive(show);
        if(!show)return;
        bool intake=director.Active&&!director.CanOperate;
        var body=director.cargo;var slot=intake?director.Plan.Initial(director.CargoId):director.TargetSlot;
        Rectangle(source,director.CargoTop+Vector3.up*.06f,new Vector2(6.3f,2.7f));
        Rectangle(target,slot.position-Vector3.up*(director.config.cargoSize.y*.5f-.05f),slot.footprint);
        sourceLabel.text=director.CargoId+" / PICKUP";sourceLabel.transform.position=director.CargoTop+Vector3.up*.9f;
        targetLabel.text=slot.id+" / "+(intake?"INTAKE":director.CurrentMove.kind.ToUpperInvariant());targetLabel.transform.position=slot.position+Vector3.up*2;
        var camera=director.view.HeadsetRunning?director.view.origin.Camera:director.view.cameraMain;
        sourceLabel.transform.rotation=targetLabel.transform.rotation=camera.transform.rotation;
        route.positionCount=2;route.SetPosition(0,new Vector3(body.position.x,.10f,body.position.z));route.SetPosition(1,new Vector3(slot.position.x,slot.area=="Ship"?3.1f:.1f,slot.position.z));
    }
    static void Rectangle(LineRenderer line,Vector3 centre,Vector2 size)
    {
        line.positionCount=5;float x=size.x*.5f,z=size.y*.5f;
        line.SetPosition(0,centre+new Vector3(-x,0,-z));line.SetPosition(1,centre+new Vector3(-x,0,z));line.SetPosition(2,centre+new Vector3(x,0,z));line.SetPosition(3,centre+new Vector3(x,0,-z));line.SetPosition(4,centre+new Vector3(-x,0,-z));
    }
}
