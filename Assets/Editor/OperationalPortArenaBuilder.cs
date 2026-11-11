#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static partial class OperationalPortBuilder
{
    static void BuildArena(GameObject root)
    {
        BuildHinterland();
        var safety=root.AddComponent<PortSafety>();director.safety=safety;safety.director=director;
        var traffic=root.AddComponent<PortTraffic>();director.traffic=traffic;traffic.director=director;
        traffic.mode=PortTrafficMode.ControlledCrossing;
        Material skin=Mat("WorkerSkin",new Color(.48f,.29f,.19f)),vest=Mat("WorkerVest",new Color(1,.33f,.025f)),pants=Mat("WorkerWorkwear",new Color(.055f,.10f,.15f));
        traffic.crew=Worker("W1",new Vector3(-18,0,0),skin,vest,pants);
        traffic.east=Worker("W2",new Vector3(22,0,-17),skin,yellow,pants);
        traffic.roadWorker=Worker("W3",new Vector3(-27,0,-45.5f),skin,vest,pants);
        traffic.west=Worker("W4",new Vector3(-22,0,-17),skin,yellow,pants);
        foreach(float x in new[]{-22f,22f})
        {
            Box(null,"Protected walking lane",new Vector3(x,.028f,-7),new Vector3(2,.04f,24),teal);
            Label(null,"PEDESTRIANS",new Vector3(x,.07f,-9),new Vector2(9,1),3.2f,Quaternion.Euler(90,90,0));
        }
        for(int i=-17;i<=17;i+=2)Box(null,"Cargo aisle crossing stripe",new Vector3(i,.04f,0),new Vector3(.8f,.04f,2),white);
        foreach(float z in new[]{-6f,6f}){Box(null,"Crane trolley hold line",new Vector3(0,.05f,z),new Vector3(30,.05f,.18f),yellow);Label(null,"HOLD FOR CROSSING",new Vector3(0,.08f,z-.8f),new Vector2(16,1),3.5f,Quaternion.Euler(90,0,0));}
        traffic.craneSignal=Signal(new Vector3(-18,0,1.8f),"W1 / AISLE SIGNAL");
        Box(null,"Rear traffic road",new Vector3(0,.025f,-39.5f),new Vector3(155,.04f,10),navy);
        for(int x=-74;x<=74;x+=6)Box(null,"Rear road divider",new Vector3(x,.06f,-39.5f),new Vector3(2.5f,.04f,.09f),white);
        for(float z=-44;z<=-35;z+=1.1f)Box(null,"Rear pedestrian crossing",new Vector3(-27,.08f,z),new Vector3(2,.04f,.5f),white);
        foreach(float x in new[]{-30f,-24f})Box(null,"Road crossing stop line",new Vector3(x,.08f,-39.5f),new Vector3(.15f,.04f,10),yellow);
        traffic.roadSignal=Signal(new Vector3(-29,0,-45),"W3 / TRUCK CROSSING");
        traffic.trucks=new[]{TrafficTruck("B1",-65,-37,1),TrafficTruck("B2",65,-42,-1)};
        var parked=Object.Instantiate(director.vehicles.truck.gameObject);parked.name="Parked inspection trailer";parked.transform.SetPositionAndRotation(new Vector3(25,0,-16),Quaternion.identity);
        foreach(var audio in parked.GetComponentsInChildren<AudioSource>())Object.DestroyImmediate(audio);
        for(int side=-1;side<=1;side+=2)
        {
            for(int i=0;i<3;i++)Barrier("BARRIER-"+side+"-"+i,new Vector3(side*17,.7f,-15+i*5));
            Box(null,"Lane fence post",new Vector3(side*19,1,-18),new Vector3(.15f,2,.15f),metal,true);
        }
        Barrier("INSPECTION-BARRIER",new Vector3(13.8f,.7f,-5));
        traffic.backgroundCranes=new[]{AmbientCrane(-48,0),AmbientCrane(48,2)};
        var reverse=new GameObject("Reachstacker reverse warning");reverse.transform.SetParent(director.vehicles.handler,false);reverse.transform.localPosition=Vector3.up*2;
        traffic.reverseSignal=reverse.AddComponent<AudioSource>();traffic.reverseSignal.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/Audio/reverse.wav");traffic.reverseSignal.loop=true;traffic.reverseSignal.playOnAwake=false;traffic.reverseSignal.volume=.14f;traffic.reverseSignal.spatialBlend=1;traffic.reverseSignal.minDistance=4;traffic.reverseSignal.maxDistance=80;
        var effects=new GameObject("Pooled contact feedback");var fx=effects.AddComponent<PortImpactFeedback>();safety.feedback=fx;fx.director=director;
        var particles=new GameObject("Contact-local paint and dust");particles.transform.SetParent(effects.transform);
        fx.dust=particles.AddComponent<ParticleSystem>();fx.dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=fx.dust.main;main.loop=false;main.playOnAwake=false;main.startLifetime=new ParticleSystem.MinMaxCurve(.35f,.8f);main.startSpeed=new ParticleSystem.MinMaxCurve(.3f,1.1f);main.startSize=new ParticleSystem.MinMaxCurve(.035f,.075f);main.gravityModifier=.45f;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=96;
        var emission=fx.dust.emission;emission.enabled=false;var shape=fx.dust.shape;shape.shapeType=ParticleSystemShapeType.Hemisphere;shape.radius=.04f;
        var particleMat=Mat("ContactDust",new Color(.6f,.58f,.5f));fx.dust.GetComponent<ParticleSystemRenderer>().sharedMaterial=particleMat;
        fx.contactAudio=effects.AddComponent<AudioSource>();fx.contactAudio.playOnAwake=false;fx.contactAudio.spatialBlend=1;fx.contactAudio.minDistance=5;fx.contactAudio.maxDistance=100;
        fx.scrape=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/mhit/mhit1.wav");fx.strongImpact=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/mhit/mhit3.wav");
        fx.beacon=Part(director.view.cabin,"Safety warning beacon",PrimitiveType.Sphere,new Vector3(.6f,2.7f,2),Vector3.one*.15f,yellow).GetComponent<Renderer>();
        fx.ring=MarkerLine("Contact location halo",yellow,.05f);fx.ring.enabled=false;
        director.crane.hookBody.gameObject.AddComponent<PortSpreaderContact>().director=director;
        foreach(float side in new[]{-1f,1f})
        {
            HookCollider(new Vector3(0,-.25f,side*1.05f),new Vector3(5.9f,.3f,.22f));
            HookCollider(new Vector3(side*2.8f,-.25f,0),new Vector3(.3f,.3f,2.3f));
        }
        // Individual collider proxies share stable actor IDs, so a compound truck/frame contact is one episode.
        int index=0;
        foreach(var collider in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).OrderBy(c=>c.name).ThenBy(c=>c.bounds.center.x).ThenBy(c=>c.bounds.center.z))
        {
            if(collider.isTrigger||collider.transform.IsChildOf(director.view.cabin)||collider.transform.IsChildOf(director.view.origin.transform)||collider.attachedRigidbody==director.crane.hookBody)continue;
            if(collider.GetComponent<PortObstacle>()!=null)continue;
            var worker=collider.GetComponentInParent<PortWorker>();var cargo=collider.GetComponentInParent<PortCargo>();var vehicle=collider.GetComponentInParent<PortTrafficVehicle>();
            bool t1=collider.transform.IsChildOf(director.vehicles.truck),h1=collider.transform.IsChildOf(director.vehicles.handler);
            var tag=collider.gameObject.AddComponent<PortObstacle>();tag.shape=collider;
            tag.kind=worker!=null?PortObstacleKind.Worker:cargo!=null?PortObstacleKind.Cargo:vehicle!=null||t1||h1||collider.transform.IsChildOf(parked.transform)?PortObstacleKind.Vehicle:collider==director.shipDeck||collider.name=="Quay deck"||collider.name=="Ship hull"?PortObstacleKind.Deck:PortObstacleKind.Structure;
            tag.actorId=worker!=null?worker.workerId:cargo!=null?cargo.cargoId:vehicle!=null?vehicle.vehicleId:t1?"T1":h1?"H1":collider.transform.IsChildOf(parked.transform)?"PARKED-TRAILER":collider.name.StartsWith("INSPECTION")?"INSPECTION-BARRIER":"STRUCTURE-"+(++index).ToString("000");
        }
        safety.obstacles=Object.FindObjectsByType<PortObstacle>(FindObjectsSortMode.None);
    }
    static void BuildHinterland()
    {
        var asphalt=Mat("HinterlandAsphalt",new Color(.19f,.23f,.24f));
        Box(null,"Connected terminal hinterland / scenery",new Vector3(0,-.6f,-446),new Vector3(1200,1.2f,800),asphalt);
        foreach(float x in new[]{-50f,50f})
        {
            Box(null,"Terminal access road",new Vector3(x,.015f,-53),new Vector3(8,.03f,62),navy);
            for(int z=-80;z<=-26;z+=6)Box(null,"Access road divider",new Vector3(x,.04f,z),new Vector3(.12f,.03f,2.5f),white);
        }
        foreach(float x in new[]{-65f,0f,65f})
        {
            Box(null,"Logistics warehouse / background",new Vector3(x,5,-90),new Vector3(32,10,20),metal);
            Box(null,"Warehouse roof",new Vector3(x,10.2f,-90),new Vector3(33,.4f,21),navy);
            for(int bay=0;bay<4;bay++)Box(null,"Warehouse loading door",new Vector3(x-11+bay*7,2.5f,-79.9f),new Vector3(5,5,.05f),navy);
            Label(null,"PORTLAB / LOGISTICS",new Vector3(x,8,-79.8f),new Vector2(28,1.3f),6,Quaternion.Euler(0,180,0));
        }
    }
    static void HookCollider(Vector3 center,Vector3 size){var collider=director.crane.hookBody.gameObject.AddComponent<BoxCollider>();collider.center=center;collider.size=size;}
    static Renderer Signal(Vector3 p,string label)
    {
        Box(null,label+" post",p+Vector3.up*1.3f,new Vector3(.12f,2.6f,.12f),metal,true);
        var lamp=Part(null,label,PrimitiveType.Sphere,p+Vector3.up*2.7f,Vector3.one*.4f,teal).GetComponent<Renderer>();Label(null,label,p+Vector3.up*3.3f,new Vector2(6,1),2.5f,Quaternion.identity);return lamp;
    }
    static void Barrier(string id,Vector3 p)
    {
        Box(null,id,p,new Vector3(.45f,1.4f,3),yellow,true);
        for(int i=-2;i<=2;i++)Box(null,"Black barrier stripe",p+new Vector3(-.23f,.1f,i*.55f),new Vector3(.025f,.85f,.25f),rubber);
    }
    static PortWorker Worker(string id,Vector3 p,Material skin,Material vest,Material pants)
    {
        var obj=new GameObject(id+" / terminal worker");obj.transform.position=p;var worker=obj.AddComponent<PortWorker>();worker.workerId=id;
        worker.body=obj.AddComponent<Rigidbody>();worker.body.isKinematic=true;worker.body.interpolation=RigidbodyInterpolation.Interpolate;worker.body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
        worker.shape=obj.AddComponent<CapsuleCollider>();worker.shape.height=1.8f;worker.shape.radius=.27f;worker.shape.center=Vector3.up*.9f;
        Box(obj.transform,"High visibility vest",new Vector3(0,1.16f,0),new Vector3(.48f,.6f,.28f),vest);
        foreach(float side in new[]{-1f,1f})Box(obj.transform,"Reflective vest strip",new Vector3(side*.14f,1.18f,-.15f),new Vector3(.045f,.51f,.025f),white);
        Box(obj.transform,"Reflective waist band",new Vector3(0,.95f,-.15f),new Vector3(.48f,.055f,.025f),white);
        worker.head=Part(obj.transform,"Worker head",PrimitiveType.Sphere,new Vector3(0,1.62f,0),new Vector3(.31f,.37f,.31f),skin).transform;
        Part(obj.transform,"Hard hat shell",PrimitiveType.Sphere,new Vector3(0,1.8f,0),new Vector3(.37f,.22f,.35f),yellow);
        Box(obj.transform,"Hard hat brim",new Vector3(0,1.76f,.04f),new Vector3(.43f,.035f,.42f),yellow);
        worker.leftArm=Limb(obj.transform,"Left arm",new Vector3(-.3f,1.4f,0),new Vector3(.14f,.55f,.14f),pants);
        worker.rightArm=Limb(obj.transform,"Right arm",new Vector3(.3f,1.4f,0),new Vector3(.14f,.55f,.14f),pants);
        worker.leftLeg=Limb(obj.transform,"Left leg",new Vector3(-.13f,.88f,0),new Vector3(.18f,.79f,.2f),pants);
        worker.rightLeg=Limb(obj.transform,"Right leg",new Vector3(.13f,.88f,0),new Vector3(.18f,.79f,.2f),pants);
        Box(worker.leftLeg,"Work boot",new Vector3(0,-.78f,.06f),new Vector3(.2f,.14f,.35f),rubber);Box(worker.rightLeg,"Work boot",new Vector3(0,-.78f,.06f),new Vector3(.2f,.14f,.35f),rubber);
        Label(obj.transform,id,new Vector3(0,2.15f,0),new Vector2(1.7f,.4f),2.5f,Quaternion.identity);return worker;
    }
    static Transform Limb(Transform parent,string label,Vector3 p,Vector3 size,Material m)
    {var pivot=new GameObject(label).transform;pivot.SetParent(parent,false);pivot.localPosition=p;Box(pivot,label+" clothing",new Vector3(0,-size.y*.5f,0),size,m);return pivot;}
    static PortTrafficVehicle TrafficTruck(string id,float x,float z,float direction)
    {
        var obj=Object.Instantiate(director.vehicles.truck.gameObject);obj.name=id+" / rear-road terminal shuttle";
        var vehicle=obj.AddComponent<PortTrafficVehicle>();vehicle.vehicleId=id;vehicle.body=obj.GetComponent<Rigidbody>();vehicle.startX=x;vehicle.laneZ=z;vehicle.direction=direction;
        vehicle.wheels=obj.GetComponentsInChildren<Transform>().Where(t=>t.name=="Tyre").ToArray();vehicle.motor=obj.GetComponent<AudioSource>();
        var freight=new GameObject("External terminal freight / scenery");freight.transform.SetParent(obj.transform,false);freight.transform.localPosition=new Vector3(0,2.7f,-2);freight.transform.localRotation=Quaternion.Euler(0,90,0);freight.transform.localScale=new Vector3(6/3.4f,2,1.5f);
        freight.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Research/FreightContainerMesh.asset");freight.AddComponent<MeshRenderer>().sharedMaterials=FreightMaterials();
        var collider=obj.AddComponent<BoxCollider>();collider.center=new Vector3(0,2.7f,-2);collider.size=new Vector3(2.4f,2.6f,6);
        vehicle.ResetVehicle();return vehicle;
    }
    static PortAmbientCrane AmbientCrane(float x,float phase)
    {
        var root=new GameObject("Neighbouring active gantry "+x);var crane=root.AddComponent<PortAmbientCrane>();crane.centerX=x;crane.phase=phase;
        foreach(float side in new[]{-1f,1f})foreach(float z in new[]{-18f,2f})Box(root.transform,"Neighbour gantry leg",new Vector3(x+side*7,11,z),new Vector3(1,22,1),yellow,true);
        foreach(float side in new[]{-1f,1f})Box(root.transform,"Neighbour boom rail",new Vector3(x+side*.9f,23,5),new Vector3(.35f,1.1f,53),yellow);
        foreach(float z in new[]{-18f,2f})Box(root.transform,"Neighbour portal beam",new Vector3(x,22,z),new Vector3(15,1.3f,1),yellow);
        crane.trolley=Box(null,"Background moving trolley",Vector3.zero,new Vector3(3,.6f,2),metal).transform;
        crane.spreader=Box(null,"Background moving spreader",Vector3.zero,new Vector3(6,.3f,2.3f),yellow).transform;
        crane.cables=new Transform[4];for(int i=0;i<4;i++)crane.cables[i]=Part(null,"Background hoist cable",PrimitiveType.Cylinder,Vector3.zero,Vector3.one,metal).transform;
        crane.ResetMotion();return crane;
    }
}
#endif
