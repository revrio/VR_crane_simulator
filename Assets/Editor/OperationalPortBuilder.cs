#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Object=UnityEngine.Object;

public static partial class OperationalPortBuilder
{
    public const string ScenePath="Assets/Scenes/OperationalPort.unity";
    const string Folder="Assets/OperationalPort";
    static Material yellow,navy,metal,glass,white,rubber,concrete,teal;
    static PortDirector director;
    static PortInput input;
    [MenuItem("VR Research Demo/Build Operational Five Container Scene")]
    public static void Build()
    {
        Directory.CreateDirectory(Folder+"/Materials");AssetDatabase.Refresh();
        var source=EditorSceneManager.OpenScene(ResearchDemoBuilder.ScenePath);
        var oldCabin=Object.FindFirstObjectByType<ResearchCabin>();var oldInstructor=Object.FindFirstObjectByType<ResearchInstructor>();
        var cabin=oldCabin.gameObject;var eye=oldCabin.eye;var screen=oldCabin.loadScreen;
        var maya=oldInstructor.gameObject;var head=oldInstructor.head;var arm=oldInstructor.arm;var mouth=oldInstructor.mouth;
        var origin=Object.FindFirstObjectByType<XROrigin>();
        var water=GameObject.Find("Animated harbor water");var sun=RenderSettings.sun;var sky=RenderSettings.skybox;
        var manager=GameObject.Find("XR Interaction Manager");var events=GameObject.Find("EventSystem");
        Object.DestroyImmediate(oldInstructor);Object.DestroyImmediate(oldCabin);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        foreach(var obj in new[]{cabin,origin.gameObject,water,sun.gameObject,manager,events})
        {obj.transform.SetParent(null,true);SceneManager.MoveGameObjectToScene(obj,scene);}
        EditorSceneManager.CloseScene(source,true);
        cabin.transform.SetPositionAndRotation(new Vector3(0,15,-22),Quaternion.identity);
        origin.Camera.enabled=false;origin.Camera.GetComponent<AudioListener>().enabled=false;origin.Camera.cullingMask=-1;
        water.transform.position=new Vector3(0,-.7f,0);
        RenderSettings.skybox=sky;RenderSettings.sun=sun;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.6f,.68f,.75f);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.52f,.67f,.76f);RenderSettings.fogStartDistance=140;RenderSettings.fogEndDistance=750;
        yellow=Mat("GantryYellow",new Color(.95f,.61f,.08f));navy=Mat("HullBlue",new Color(.025f,.10f,.16f));
        metal=Mat("GalvanizedSteel",new Color(.38f,.44f,.47f),.7f);glass=Mat("CabGlass",new Color(.035f,.15f,.21f));
        white=Mat("PortWhite",new Color(.82f,.86f,.84f));rubber=Mat("Tyres",new Color(.025f,.033f,.039f));
        concrete=Mat("Quay",new Color(.27f,.31f,.32f));teal=Mat("GuideTeal",new Color(.08f,.8f,.7f));
        string path=Folder+"/IterationThree.asset";var config=AssetDatabase.LoadAssetAtPath<PortScenario>(path);
        if(config==null){config=ScriptableObject.CreateInstance<PortScenario>();AssetDatabase.CreateAsset(config,path);}
        config.multipleCargo=true;config.version="operational-i3";config.anchorSettledCargo=true;config.transitHookHeight=13;config.operationalTraffic=true;
        config.handlerStart=new Vector3(-38,0,-30);EditorUtility.SetDirty(config);
        var root=new GameObject("Operational terminal / five container shipment");director=root.AddComponent<PortDirector>();director.config=config;
        input=root.AddComponent<PortInput>();input.director=director;
        input.craneActions=AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/Resources/PortCraneControls.inputactions");
        var view=root.AddComponent<PortView>();director.view=view;view.director=director;view.origin=origin;view.cabin=cabin.transform;view.eye=eye;view.loadScreen=screen;
        var camera=new GameObject("Operational spectator").AddComponent<Camera>();camera.nearClipPlane=.1f;camera.farClipPlane=1200;camera.fieldOfView=58;camera.gameObject.AddComponent<AudioListener>();view.cameraMain=camera;
        view.loadCamera=new GameObject("Operational load feed").AddComponent<Camera>();view.loadCamera.enabled=false;view.loadCamera.orthographic=true;view.loadCamera.orthographicSize=7;
        view.loadCamera.cullingMask=~((1<<26)|(1<<28));view.loadCamera.nearClipPlane=.1f;view.loadCamera.farClipPlane=100;
        BuildQuay();BuildShip();BuildVehicles();BuildCargo();BuildCrane();
        BuildTaskMarkers(root);
        director.voice=root.AddComponent<AudioSource>();director.voice.playOnAwake=false;director.voice.volume=.8f;
        director.sfx=root.AddComponent<AudioSource>();director.sfx.playOnAwake=false;
        director.lockSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/mhit/mhit1.wav");
        director.impactSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/mhit/mhit3.wav");
        director.successSound=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/correct-356013.mp3");
        var coach=maya.AddComponent<PortMaya>();coach.director=director;coach.head=head;coach.arm=arm;coach.mouth=mouth;
        coach.voiceKeys=new[]{"welcome","lower","couple","lift","land","release","settle","paused","truck_intake","handler_clear","shipment_complete"};
        coach.voiceClips=coach.voiceKeys.Select(k=>AssetDatabase.LoadAssetAtPath<AudioClip>((k=="truck_intake"||k=="handler_clear"||k=="shipment_complete"?"Assets/OperationalPort/Voice/":"Assets/Research/Voice/")+k+".wav")).ToArray();
        view.desktopPanel=BuildPanel(false);view.worldPanel=BuildPanel(true);
        view.worldPanel.transform.SetParent(cabin.transform,false);view.worldPanel.transform.localPosition=new Vector3(1.6f,1.3f,1);
        view.worldPanel.transform.localRotation=Quaternion.Euler(0,45,0);view.worldPanel.transform.localScale=Vector3.one*.0013f;
        view.worldPanel.worldCamera=origin.Camera;view.worldPanel.gameObject.SetActive(false);
        director.vehicles.director=director;director.vehicles.truckMotor=Motor(director.vehicles.truck.gameObject);
        director.vehicles.handlerMotor=Motor(director.vehicles.handler.gameObject);director.vehicles.craneMotor=Motor(director.crane.trolley.gameObject);
        BuildArena(root);
        director.ResetOperation();view.Select(0);
        EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath)).ToArray();
        Debug.Log("OPERATIONAL_PORT_BUILT "+ScenePath);
    }
    static void BuildQuay()
    {
        Box(null,"Quay deck",new Vector3(0,-.5f,-20),new Vector3(200,1,52),concrete,true);
        Box(null,"Truck road",new Vector3(0,.012f,-24),new Vector3(108,.02f,8),navy);
        for(int x=-50;x<=50;x+=6)Box(null,"Road centre line",new Vector3(x,.03f,-24),new Vector3(2.8f,.02f,.08f),white);
        Outline("Truck transfer stand",new Vector3(-14,.05f,-24),new Vector2(13,4),yellow);
        Outline("Yard buffer / C1",new Vector3(0,.05f,-12),new Vector2(8,4),teal);
        Label(null,"TRUCK TRANSFER",new Vector3(-14,.065f,-27),new Vector2(12,1),5,Quaternion.Euler(90,0,0));
        Label(null,"YARD BUFFER",new Vector3(0,.065f,-16),new Vector2(18,1),5,Quaternion.Euler(90,0,0));
        for(int x=-50;x<=50;x+=4)Box(null,"Quay edge stripe",new Vector3(x,.04f,5.5f),new Vector3(2,.05f,.55f),yellow);
        foreach(float z in new[]{-20f,2f})Box(null,"Crane runway rail",new Vector3(0,.05f,z),new Vector3(55,.1f,.16f),metal);
        var cargoMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Research/FreightContainerMesh.asset");
        var mats=FreightMaterials();
        for(int i=0;i<18;i++)
        {
            var obj=new GameObject("Stored export freight",typeof(MeshFilter),typeof(MeshRenderer));
            int slot=i%9;
            // Keep stored freight outside the truck lane and handler's curved approach/reverse routes.
            obj.transform.position=new Vector3(35+slot%3*8,1.3f+i/9*2.6f,-18+slot/3*6);
            obj.transform.localScale=new Vector3(6/3.4f,2,1.5f);obj.GetComponent<MeshFilter>().sharedMesh=cargoMesh;
            obj.GetComponent<Renderer>().sharedMaterials=mats;
            obj.AddComponent<BoxCollider>().size=new Vector3(3.4f,1.3f,1.6f);
        }
    }
    static void BuildShip()
    {
        var ship=new GameObject("Moored feeder vessel / operational berth");
        Box(ship.transform,"Ship hull",new Vector3(0,1,18),new Vector3(65,3.5f,18),navy,true);
        var bow=Part(ship.transform,"Rounded bow",PrimitiveType.Sphere,new Vector3(31,1,18),new Vector3(13,3.5f,18),navy);
        Box(ship.transform,"Boot stripe",new Vector3(0,-.35f,18),new Vector3(64,.45f,18.05f),rubber);
        director.shipDeck=Box(ship.transform,"Ship deck support",new Vector3(0,2.8f,18),new Vector3(60,.4f,16),concrete,true).GetComponent<Collider>();
        Box(ship.transform,"Bridge superstructure",new Vector3(-24,6.8f,18),new Vector3(9,7.6f,12),white,true);
        Box(ship.transform,"Bridge upper deck",new Vector3(-24,11,18),new Vector3(10,1.2f,12.5f),white);
        for(int i=0;i<7;i++)Box(ship.transform,"Bridge glazing",new Vector3(-28.2f+i*1.4f,11.1f,11.72f),new Vector3(.9f,.7f,.05f),glass);
        Box(ship.transform,"Funnel",new Vector3(-25,13.1f,20),new Vector3(3.2f,3.1f,4),yellow);
        Box(ship.transform,"Funnel cap",new Vector3(-25,14.7f,20),new Vector3(3.4f,.4f,4.2f),rubber);
        for(int x=-28;x<=28;x+=4)
        {
            Box(ship.transform,"Ship rail stanchion",new Vector3(x,3.5f,10),new Vector3(.07f,1,.07f),white,true);
            Box(ship.transform,"Ship handrail",new Vector3(x,4,10),new Vector3(4,.07f,.07f),white,true);
        }
        for(int x=-25;x<=25;x+=10)
        {
            Part(null,"Quay fender",PrimitiveType.Cylinder,new Vector3(x,0,6.4f),new Vector3(1.2f,1.5f,1.2f),rubber);
            Part(null,"Bollard",PrimitiveType.Cylinder,new Vector3(x,.3f,4.4f),new Vector3(.5f,.3f,.5f),yellow);
        }
        for(int i=0;i<3;i++)
        {
            float x=(i-1)*7.5f;
            Outline("Ship deck slot",new Vector3(x,3.04f,16),new Vector2(7.5f,3.7f),white);
            Label(null,"SHIP-"+(i+1).ToString("00"),new Vector3(x,3.065f,19.3f),new Vector2(7.3f,1),5,Quaternion.Euler(90,0,0));
        }
        Label(ship.transform,"PORTLAB  /  FEEDER 01",new Vector3(12,1.7f,8.97f),new Vector2(24,1.2f),7,Quaternion.identity);
    }
    static void BuildVehicles()
    {
        var root=new GameObject("Terminal handling vehicles");var vehicles=root.AddComponent<PortVehicles>();director.vehicles=vehicles;
        vehicles.director=director;
        var truck=new GameObject("T1 / delivery truck");vehicles.truck=truck.transform;
        var tb=truck.AddComponent<Rigidbody>();tb.isKinematic=true;
        Box(truck.transform,"Trailer chassis",new Vector3(0,1.25f,-2),new Vector3(2.5f,.3f,8),metal,true);
        Box(truck.transform,"Tractor chassis",new Vector3(0,.9f,3.5f),new Vector3(2.4f,.45f,4.2f),rubber,true);
        Box(truck.transform,"Truck cab",new Vector3(0,2.3f,4.1f),new Vector3(2.45f,2.5f,2.2f),white,true);
        Box(truck.transform,"Truck windscreen",new Vector3(0,2.8f,5.22f),new Vector3(2.1f,.8f,.025f),glass);
        Box(truck.transform,"Cab grille",new Vector3(0,1.7f,5.23f),new Vector3(1.4f,.5f,.035f),rubber);
        foreach(float side in new[]{-1f,1f})
        {
            Box(truck.transform,"Side window",new Vector3(side*1.24f,2.8f,4.15f),new Vector3(.03f,.75f,1.4f),glass);
            Box(truck.transform,"Truck headlamp",new Vector3(side*.85f,1.45f,5.25f),new Vector3(.4f,.2f,.04f),yellow);
        }
        vehicles.truckWheels=Wheels(truck.transform,new[]{-4.8f,-3.4f,2.1f,4.6f},1.3f,.6f,.65f);
        var handler=new GameObject("H1 / reachstacker");vehicles.handler=handler.transform;
        var hb=handler.AddComponent<Rigidbody>();hb.isKinematic=true;
        Box(handler.transform,"Handling chassis",new Vector3(0,1.1f,0),new Vector3(3.6f,1.3f,5.7f),yellow,true);
        Box(handler.transform,"Rear counterweight",new Vector3(0,1.7f,-2.6f),new Vector3(3.8f,1.8f,1.6f),yellow,true);
        Box(handler.transform,"Operator cab",new Vector3(-1.15f,2.6f,.3f),new Vector3(1.2f,2.2f,2),glass,true);
        Box(handler.transform,"Cab roof",new Vector3(-1.15f,3.75f,.3f),new Vector3(1.35f,.2f,2.2f),yellow);
        vehicles.handlerWheels=Wheels(handler.transform,new[]{-1.8f,1.8f},1.9f,.85f,.9f);
        vehicles.boom=Box(null,"Reachstacker outer boom",Vector3.zero,Vector3.one,yellow).transform;
        vehicles.innerBoom=Box(null,"Reachstacker telescopic boom",Vector3.zero,Vector3.one,metal).transform;
        var tool=new GameObject("Reachstacker rotator and spreader");vehicles.toolHead=tool.transform;
        Box(tool.transform,"Handler head",Vector3.zero,new Vector3(5.8f,.25f,2.2f),yellow);
        Box(tool.transform,"Hydraulic rotator",Vector3.up*.25f,new Vector3(.8f,.5f,.8f),metal);
        foreach(float side in new[]{-1f,1f})foreach(float fore in new[]{-1f,1f})Box(tool.transform,"Lock foot",new Vector3(side*2.8f,-.2f,fore*1.05f),new Vector3(.25f,.3f,.25f),metal);
    }
    static void BuildCargo()
    {
        director.allCargo=new PortCargo[5];string[] colours={"FreightCoral","FreightTeal","FreightOchre","FreightBlue","FreightGreen"};
        for(int i=0;i<5;i++)
        {
            string id="C"+(i+1);var cargo=new GameObject(id+" / persistent consignment");var body=cargo.AddComponent<Rigidbody>();
            body.mass=80;body.linearDamping=.8f;body.angularDamping=2;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            var collider=cargo.AddComponent<BoxCollider>();collider.size=director.config.cargoSize;
            var entity=cargo.AddComponent<PortCargo>();entity.cargoId=id;entity.body=body;entity.footprint=collider;director.allCargo[i]=entity;
            cargo.AddComponent<PortCargoContact>().director=director;
            var visual=new GameObject("Corrugated "+id,typeof(MeshFilter),typeof(MeshRenderer));visual.transform.SetParent(cargo.transform,false);
            visual.transform.localScale=new Vector3(6/3.4f,2,1.5f);visual.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Research/FreightContainerMesh.asset");
            var materials=FreightMaterials();materials[0]=AssetDatabase.LoadAssetAtPath<Material>("Assets/Research/Materials/"+colours[i]+".mat");visual.GetComponent<Renderer>().sharedMaterials=materials;
            Label(cargo.transform,id,new Vector3(0,1.32f,0),new Vector2(3,1.6f),15,Quaternion.Euler(90,0,0));
            foreach(float side in new[]{-1f,1f})Label(cargo.transform,"PORTLAB / "+id,new Vector3(0,.25f,side*1.205f),new Vector2(5,1),7,Quaternion.Euler(0,side<0?0:180,0));
        }
        director.cargo=director.allCargo[0].body;
    }
    static void BuildCrane()
    {
        var root=new GameObject("Operational quay gantry");var crane=root.AddComponent<PortCrane>();director.crane=crane;crane.director=director;
        var frame=new GameObject("Gantry frame / along-quay travel");crane.frame=frame.transform;
        foreach(float x in new[]{-15f,15f})foreach(float z in new[]{-20f,2f})
        {
            Box(frame.transform,"Gantry tower",new Vector3(x,10.5f,z),new Vector3(.9f,21,1.1f),yellow,true);
            Box(frame.transform,"Bogie chassis",new Vector3(x,.5f,z),new Vector3(3,1,1.5f),yellow,true);
            Box(frame.transform,"Tower knee brace",new Vector3(x*.55f,17.5f,z),new Vector3(15,.55f,.55f),yellow);
        }
        foreach(float z in new[]{-20f,2f})Box(frame.transform,"Portal cross beam",new Vector3(0,20,z),new Vector3(31,1.3f,1.4f),yellow);
        foreach(float x in new[]{-1.3f,1.3f})
        {
            Box(frame.transform,"Bridge girder",new Vector3(x,22,1),new Vector3(.6f,1,54),yellow);
            Box(frame.transform,"Trolley running rail",new Vector3(x,21.35f,1),new Vector3(.7f,.15f,54),metal);
        }
        for(float z=-24;z<28;z+=4)Box(frame.transform,"Bridge tie",new Vector3(0,22.6f,z),new Vector3(3.2f,.2f,.3f),yellow);
        var trolley=new GameObject("Crane trolley");crane.trolley=trolley.transform;
        Box(trolley.transform,"Trolley chassis",Vector3.zero,new Vector3(3.7f,.5f,2.4f),yellow);
        Box(trolley.transform,"Hoist winch",Vector3.up*.6f,new Vector3(1.8f,.7f,1.5f),rubber);
        foreach(var t in frame.GetComponentsInChildren<Transform>())t.gameObject.layer=26;
        foreach(var t in trolley.GetComponentsInChildren<Transform>())t.gameObject.layer=26;
        var hook=new GameObject("Operational spreader physics");crane.hookBody=hook.AddComponent<Rigidbody>();crane.hookBody.mass=30;
        crane.hookBody.constraints=RigidbodyConstraints.FreezeRotation;crane.hookBody.linearDamping=1.2f;crane.hookBody.interpolation=RigidbodyInterpolation.Interpolate;crane.hookBody.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        var box=hook.AddComponent<BoxCollider>();box.center=new Vector3(0,-.1f,0);box.size=new Vector3(1.4f,.4f,.7f);
        crane.rope=hook.AddComponent<Rope>();crane.rope.hook=crane.hookBody;crane.rope.basePoint=trolley.transform;crane.rope.stiffness=35000;crane.rope.damping=1800;
        var spreader=new GameObject("Yellow container spreader");crane.spreader=spreader.transform;
        foreach(float side in new[]{-1f,1f})
        {
            Box(spreader.transform,"Long spreader beam",new Vector3(0,-.25f,side*1.05f),new Vector3(5.9f,.3f,.22f),yellow);
            Box(spreader.transform,"Spreader end beam",new Vector3(side*2.8f,-.25f,0),new Vector3(.3f,.3f,2.3f),yellow);
            Box(spreader.transform,"Hoist block cheek",new Vector3(side*.6f,.65f,0),new Vector3(.12f,.7f,1.1f),yellow);
            foreach(float fore in new[]{-1f,1f})Box(spreader.transform,"Twistlock",new Vector3(side*2.8f,-.45f,fore*1.05f),new Vector3(.25f,.2f,.25f),metal);
        }
        Box(spreader.transform,"Spreader centre frame",new Vector3(0,.0f,0),new Vector3(5.6f,.25f,.45f),yellow);
        for(int i=0;i<14;i++){var stripe=Box(spreader.transform,"Hazard marking",new Vector3(-2.55f+i*.39f,-.25f,-1.17f),new Vector3(.16f,.3f,.01f),rubber);stripe.transform.localRotation=Quaternion.Euler(0,0,-20);}
        crane.cables=new Transform[4];var cable=AssetDatabase.LoadAssetAtPath<Material>("Assets/Research/Materials/HoistWireRope.mat");
        for(int i=0;i<4;i++)crane.cables[i]=Part(null,"Hoist cable "+(i+1),PrimitiveType.Cylinder,Vector3.zero,Vector3.one,cable).transform;
        Outline("Final parking ring / ground reference",new Vector3(0,.08f,-12),new Vector2(4,3),teal);
        var ring=new GameObject("Final parking height ring").AddComponent<LineRenderer>();ring.sharedMaterial=teal;ring.positionCount=65;ring.startWidth=ring.endWidth=.06f;
        for(int i=0;i<65;i++){float a=i*Mathf.PI/32;ring.SetPosition(i,director.config.park+new Vector3(Mathf.Cos(a)*2,0,Mathf.Sin(a)*2));}
    }
    static Canvas BuildPanel(bool world)
    {
        var obj=new GameObject(world?"Operational VR console":"Operational desktop console",typeof(RectTransform));var canvas=obj.AddComponent<Canvas>();
        canvas.renderMode=world?RenderMode.WorldSpace:RenderMode.ScreenSpaceOverlay;
        if(world){obj.AddComponent<TrackedDeviceGraphicRaycaster>();obj.GetComponent<RectTransform>().sizeDelta=new Vector2(560,1080);}
        else{var scaler=obj.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;}
        obj.AddComponent<GraphicRaycaster>();
        var r=Rect(obj.transform,"Port control station",0,0,560,1080);r.anchorMin=r.anchorMax=new Vector2(1,.5f);r.pivot=new Vector2(1,.5f);r.anchoredPosition=Vector2.zero;
        r.gameObject.AddComponent<Image>().color=new Color(.025f,.045f,.068f);
        var panel=r.gameObject.AddComponent<PortPanel>();panel.director=director;
        Text(r,"PORTLAB  /  OPERATIONS 03",24,22,512,28,19,new Color(1,.7f,.25f));
        Text(r,"FIVE-CONTAINER SHIPMENT",24,60,512,36,27,Color.white);
        Text(r,"MAYA / PORT INSTRUCTOR",24,112,512,24,17,new Color(.15f,.85f,.75f));
        panel.caption=Text(r,"Choose a layout, then Start operation",24,148,512,132,20,Color.white);
        panel.stage=Text(r,"IDLE",24,298,350,30,20,new Color(1,.7f,.25f));
        panel.time=Text(r,"00:00",394,294,140,38,27,Color.white);
        panel.custody=Text(r,"CARGO",24,343,512,52,20,new Color(.15f,.85f,.75f));
        panel.manifest=Text(r,"MANIFEST",24,410,512,26,17,Color.white);
        panel.layouts=new Button[4];string[] presets={"A / Flat","B / Stacks","C / Retrieve","D / Build"};
        for(int i=0;i<4;i++){panel.layouts[i]=Button(r,presets[i],24+i*130,450,122,32);panel.layouts[i].GetComponentInChildren<TextMeshProUGUI>().fontSize=17;}
        panel.start=Button(r,"START OPERATION [P]",24,510,512,42);
        panel.pause=Button(r,"Pause [Esc]",24,590,248,36);panel.reset=Button(r,"Reset [R]",284,590,252,36);
        panel.couple=Button(r,"Lock / release [Space]",24,640,248,36);panel.changeView=Button(r,"Change view [Tab]",284,640,252,36);
        panel.cabin=Button(r,"Cabin [C]",24,690,248,36);panel.voice=Button(r,"Voice [M]",284,690,252,36);
        panel.start.GetComponentInChildren<TextMeshProUGUI>().fontSize=18;
        panel.safetyStatus=Text(r,"MARKED ROUTES / TRAFFIC",24,792,512,40,15,new Color(1,.75f,.3f));
        panel.controls=Text(r,"CONTROLS",24,838,512,84,16,new Color(.15f,.85f,.75f));
        panel.results=Text(r,"5 CONTAINERS / PRACTICE",24,934,512,84,16,Color.white);
        panel.folder=Button(r,"Open operational CSV folder",24,1030,512,32);
        if(!world)
        {
            var controls=Rect(obj.transform,"Hold controls",0,0,1260,55);controls.anchorMin=controls.anchorMax=Vector2.zero;controls.pivot=Vector2.zero;controls.anchoredPosition=new Vector2(18,24);
            string[] labels={"A / Left","D / Right","W / Shipward","S / Yardward","UP / Raise","DOWN / Lower"};
            Vector3[] commands={Vector3.left,Vector3.right,Vector3.up,Vector3.down,Vector3.back,Vector3.forward};
            for(int i=0;i<6;i++){var b=Button(controls,labels[i],i*160,0,150,40);b.GetComponentInChildren<TextMeshProUGUI>().fontSize=17;var h=b.gameObject.AddComponent<PortHoldControl>();h.input=input;h.command=commands[i];}
        }
        return canvas;
    }
    static void BuildTaskMarkers(GameObject root)
    {
        var markers=root.AddComponent<PortTaskMarkers>();markers.director=director;
        markers.source=MarkerLine("Active pickup outline",yellow,.055f);markers.target=MarkerLine("Active receiving outline",teal,.07f);markers.route=MarkerLine("Destination route",white,.035f);
        var a=new GameObject("Current cargo label").AddComponent<TextMeshPro>();a.fontSize=4;a.alignment=TextAlignmentOptions.Center;a.rectTransform.sizeDelta=new Vector2(6,1);markers.sourceLabel=a;
        var b=new GameObject("Current destination label").AddComponent<TextMeshPro>();b.fontSize=3.5f;b.alignment=TextAlignmentOptions.Center;b.rectTransform.sizeDelta=new Vector2(8,1);b.color=new Color(.2f,1,.8f);markers.targetLabel=b;
    }
    static LineRenderer MarkerLine(string name,Material material,float width){var line=new GameObject(name).AddComponent<LineRenderer>();line.sharedMaterial=material;line.startWidth=line.endWidth=width;return line;}
    static AudioSource Motor(GameObject parent){var source=parent.AddComponent<AudioSource>();source.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/engine-6000.wav");source.loop=true;source.playOnAwake=true;source.volume=0;source.spatialBlend=.75f;source.minDistance=6;source.maxDistance=100;return source;}
    static Transform[] Wheels(Transform parent,float[] axleZ,float x,float y,float radius)
    {
        var result=new List<Transform>();foreach(float z in axleZ)foreach(float side in new[]{-1f,1f})
        {var wheel=Part(parent,"Tyre",PrimitiveType.Cylinder,new Vector3(side*x,y,z),new Vector3(radius*2,.24f,radius*2),rubber);wheel.transform.localRotation=Quaternion.Euler(0,0,90);result.Add(wheel.transform);}
        return result.ToArray();
    }
    static Material[] FreightMaterials()=>new[]{"FreightCoral","DetailSteel","DetailRubber","DetailOxide"}.Select(n=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Research/Materials/"+n+".mat")).ToArray();
    static Material Mat(string name,Color color,float metallic=.25f){string path=Folder+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",.3f);EditorUtility.SetDirty(m);return m;}
    static GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material m,bool collision=false)=>Part(parent,name,PrimitiveType.Cube,p,size,m,collision);
    static GameObject Part(Transform parent,string name,PrimitiveType type,Vector3 p,Vector3 size,Material m,bool collision=false){var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=p;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=m;if(!collision)Object.DestroyImmediate(obj.GetComponent<Collider>());return obj;}
    static void Outline(string name,Vector3 p,Vector2 size,Material m)
    {
        for(int side=-1;side<=1;side+=2){Box(null,name,p+new Vector3(side*size.x*.5f,0,0),new Vector3(.10f,.04f,size.y),m);Box(null,name,p+new Vector3(0,0,side*size.y*.5f),new Vector3(size.x,.04f,.10f),m);}
    }
    static void Label(Transform parent,string value,Vector3 p,Vector2 size,float font,Quaternion rotation){var obj=new GameObject(value);obj.transform.SetParent(parent,false);obj.transform.localPosition=p;obj.transform.localRotation=rotation;var text=obj.AddComponent<TextMeshPro>();text.text=value;text.fontSize=font;text.rectTransform.sizeDelta=size;text.color=Color.white;text.alignment=TextAlignmentOptions.Center;}
    static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height){var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);var r=obj.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);return r;}
    static TextMeshProUGUI Text(Transform parent,string value,float x,float y,float width,float height,float font,Color color){var r=Rect(parent,value,x,y,width,height);var text=r.gameObject.AddComponent<TextMeshProUGUI>();text.text=value;text.fontSize=font;text.color=color;text.raycastTarget=false;return text;}
    static Button Button(Transform parent,string value,float x,float y,float width,float height){var r=Rect(parent,value,x,y,width,height);var img=r.gameObject.AddComponent<Image>();img.color=new Color(.09f,.25f,.30f);var button=r.gameObject.AddComponent<Button>();button.targetGraphic=img;Text(r,value,5,3,width-10,height-6,21,Color.white).alignment=TextAlignmentOptions.Center;return button;}
}
#endif
