#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ResearchCabinBuilder
{
    static Material shell, trim, fabric, metal, accent;
    public static void Apply(ExperimentSession session, ResearchOperatorView view, ResearchGuide guide, Transform parent)
    {
        shell = Mat("CabinShell", new Color(.30f,.37f,.40f));
        trim = Mat("CabinTrim", new Color(.045f,.075f,.095f));
        fabric = Mat("CabinSeat", new Color(.06f,.13f,.17f));
        metal = Mat("CabinMetal", new Color(.58f,.65f,.66f));
        accent = Mat("CabinAccent", new Color(.2f,.8f,.7f));
        var root = new GameObject("Operator cabin / seated station"); root.transform.SetParent(parent, false);
        var cabin = root.AddComponent<ResearchCabin>(); cabin.session = session; cabin.view = view; view.cabin = cabin;
        cabin.SyncFrame();
        // Open glass viewing regions: opaque structure is limited to frames and the rear floor.
        Box("Rear floor", root.transform, new Vector3(0,-.09f,-.65f), new Vector3(4.5f,.18f,2.1f), shell);
        Box("Left sill", root.transform, new Vector3(-2.22f,0,1.25f), new Vector3(.12f,.12f,2.4f), metal);
        Box("Right sill", root.transform, new Vector3(2.22f,0,1.25f), new Vector3(.12f,.12f,2.4f), metal);
        Box("Front sill", root.transform, new Vector3(0,.1f,2.45f), new Vector3(4.5f,.14f,.12f), metal);
        foreach (float side in new[] {-1f,1f})
        {
            Box("Window pillar",root.transform,new Vector3(side*2.22f,1.25f,2.45f),new Vector3(.09f,2.5f,.09f),shell);
            Box("Rear pillar",root.transform,new Vector3(side*2.22f,1.25f,-1.65f),new Vector3(.12f,2.5f,.12f),shell);
            Box("Roof side",root.transform,new Vector3(side*2.22f,2.5f,.4f),new Vector3(.12f,.12f,4.2f),shell);
        }
        Box("Front roof frame",root.transform,new Vector3(0,2.5f,2.45f),new Vector3(4.5f,.12f,.12f),shell);
        Box("Rear wall",root.transform,new Vector3(0,1.15f,-1.65f),new Vector3(4.5f,2.3f,.15f),shell);
        Box("Roof",root.transform,new Vector3(0,2.58f,-.35f),new Vector3(4.5f,.12f,2.8f),shell);
        Box("Glass floor left frame",root.transform,new Vector3(-.75f,0,1.3f),new Vector3(.045f,.045f,2.2f),metal);
        Box("Glass floor right frame",root.transform,new Vector3(.75f,0,1.3f),new Vector3(.045f,.045f,2.2f),metal);
        // Seat base and back can be inspected by looking down/behind; armrest consoles stay in view.
        Part("Seat pedestal",PrimitiveType.Cylinder,root.transform,new Vector3(0,.27f,-.25f),new Vector3(.34f,.27f,.34f),metal);
        Box("Seat cushion",root.transform,new Vector3(0,.60f,-.2f),new Vector3(.66f,.15f,.72f),fabric);
        Box("Seat back",root.transform,new Vector3(0,1.07f,-.59f),new Vector3(.66f,.87f,.15f),fabric);
        Box("Head rest",root.transform,new Vector3(0,1.55f,-.61f),new Vector3(.42f,.25f,.15f),fabric);
        foreach (float side in new[] {-1f,1f})
        {
            Box("Console support",root.transform,new Vector3(side*.53f,.45f,.05f),new Vector3(.22f,.8f,.38f),shell);
            Box("Padded armrest",root.transform,new Vector3(side*.53f,.87f,-.1f),new Vector3(.28f,.12f,.65f),fabric);
            Box("Joystick console",root.transform,new Vector3(side*.53f,.82f,.43f),new Vector3(.36f,.18f,.44f),trim);
            var pivot = new GameObject(side < 0 ? "Left joystick" : "Right joystick"); pivot.transform.SetParent(root.transform,false);pivot.transform.localPosition=new Vector3(side*.53f,.94f,.43f);
            Part("Joystick shaft",PrimitiveType.Cylinder,pivot.transform,new Vector3(0,.10f,0),new Vector3(.035f,.10f,.035f),metal);
            Part("Joystick grip",PrimitiveType.Capsule,pivot.transform,new Vector3(0,.22f,0),new Vector3(.08f,.10f,.08f),trim);
            Part("Trigger cap",PrimitiveType.Sphere,pivot.transform,new Vector3(0,.30f,0),new Vector3(.055f,.025f,.055f),accent);
            if(side<0)cabin.leftStick=pivot.transform;else cabin.rightStick=pivot.transform;
        }
        var eye = new GameObject("Seated cabin eye");eye.transform.SetParent(root.transform,false);eye.transform.localPosition=new Vector3(0,1.45f,0);cabin.eye=eye.transform;view.operatorEye=eye.transform;
        Label("CABIN / PORTLAB",root.transform,new Vector3(0,2.29f,2.37f),2.8f,.22f,1.1f,Color.white);
        Label("LEFT: ROTATE / REACH",root.transform,new Vector3(-.53f,.75f,.66f),.6f,.14f,.44f,Color.white);
        Label("RIGHT: HOIST",root.transform,new Vector3(.53f,.75f,.66f),.6f,.14f,.44f,Color.white);
        // Monitors are low and offset, leaving the center windscreen clear.
        var screen = Screen(root.transform,"Live load monitor",new Vector3(-.50f,1.03f,1.10f),new Vector2(640,400),.0012f);
        var raw = Rect(screen.transform,"Live load image",12,36,616,348).gameObject.AddComponent<RawImage>();raw.raycastTarget=false;cabin.loadScreen=raw;
        Text(screen.transform,"LOAD CAMERA / LIVE",12,5,616,28,20,new Color(.3f,1,.8f));
        var camera = new GameObject("Cabin load camera").AddComponent<Camera>();camera.transform.SetParent(parent,false);camera.enabled=false;camera.orthographic=true;camera.orthographicSize=6;camera.nearClipPlane=.1f;camera.farClipPlane=180;camera.cullingMask=~((1<<27)|(1<<28)|(1<<29)|(1<<30));camera.depth=-5;camera.clearFlags=CameraClearFlags.Skybox;cabin.loadCamera=camera;
        // Guidance and run controls already live in the sidebar (the right console in XR).
        // Keep this windscreen clear; only the useful live load feed belongs here.
        // Convert the original portrait geometry into the actual full-body instructor.
        Transform actor = guide.avatarHead.parent;
        actor.SetParent(root.transform,false);
        actor.Find("Work vest").localScale=new Vector3(.64f,.40f,.33f);
        guide.avatarHead.localScale=Vector3.one*.88f;
        actor.localPosition=new Vector3(-1.15f,0,1.95f);actor.localRotation=Quaternion.Euler(0,-30,0);
        foreach(float side in new[] {-1f,1f})
        {
            Part("Trouser leg",PrimitiveType.Capsule,actor,new Vector3(side*.16f,.35f,.02f),new Vector3(.24f,.36f,.25f),fabric);
            Box("Safety boot",actor,new Vector3(side*.16f,.085f,-.08f),new Vector3(.27f,.17f,.40f),trim);
        }
        Box("Trouser waist",actor,new Vector3(0,.65f,0),new Vector3(.49f,.24f,.3f),fabric);
        // Facing -Z is the model's forward; point the face toward the seated operator.
        Vector3 away = actor.position-eye.transform.position;away.y=0;actor.rotation=Quaternion.LookRotation(away);
        var npc = actor.gameObject.AddComponent<ResearchInstructor>();npc.guide=guide;npc.cabin=cabin;npc.head=guide.avatarHead;npc.arm=guide.avatarHand;npc.mouth=guide.avatarMouth;
        Label("MAYA / INSTRUCTOR",root.transform,new Vector3(-1.15f,2.02f,1.95f),1.2f,.20f,.72f,new Color(.4f,1,.8f));
        foreach(var image in guide.portraits) if(image!=null) UnityEngine.Object.DestroyImmediate(image.gameObject);
        guide.portraits=Array.Empty<RawImage>();
        if(guide.portraitCamera!=null)UnityEngine.Object.DestroyImmediate(guide.portraitCamera.gameObject);guide.portraitCamera=null;
        var oldLight=GameObject.Find("Portrait soft light");if(oldLight!=null)UnityEngine.Object.DestroyImmediate(oldLight);
        foreach(var bindings in UnityEngine.Object.FindObjectsByType<ResearchGuideConsole>(FindObjectsSortMode.None))
        {
            bindings.cabinView=Button(bindings.transform,"CABIN [C]",24,72,112,58);
        }
        foreach(var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer=28;
        // These canvases remain root objects in the saved scene so rebuilding generated geometry is safe.
        view.worldPanel.transform.SetPositionAndRotation(root.transform.TransformPoint(new Vector3(1.65f,1.2f,.65f)), root.transform.rotation*Quaternion.Euler(0,55,0));
        view.worldPanel.transform.localScale=Vector3.one*.0011f;
        session.experimentVersion="wsd-living-harbor-v5";
        session.setupLabel="Seated cabin with Maya NPC, single live load monitor and animated harbor; identical scenery and controls across layouts";
    }
    static Material Mat(string name,Color color)
    {
        string path="Assets/Research/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;return m;
    }
    static GameObject Part(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Material material)
    {var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
    static void Box(string name,Transform parent,Vector3 p,Vector3 scale,Material m)=>Part(name,PrimitiveType.Cube,parent,p,scale,m);
    static void Label(string value,Transform p,Vector3 pos,float width,float height,float size,Color color)
    {var o=new GameObject(value);o.transform.SetParent(p,false);o.transform.localPosition=pos;var t=o.AddComponent<TextMeshPro>();t.text=value;t.fontSize=size;t.color=color;t.rectTransform.sizeDelta=new Vector2(width,height);t.alignment=TextAlignmentOptions.Center;t.textWrappingMode=TextWrappingModes.Normal;}
    static RectTransform Rect(Transform p,string name,float x,float y,float w,float h)
    {var o=new GameObject(name,typeof(RectTransform));o.transform.SetParent(p,false);var r=o.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    static TextMeshProUGUI Text(Transform p,string value,float x,float y,float w,float h,float size,Color color)
    {var r=Rect(p,value,x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.text=value;t.fontSize=size;t.color=color;t.raycastTarget=false;return t;}
    static Button Button(Transform p,string value,float x,float y,float w,float h)
    {var r=Rect(p,value,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.08f,.25f,.27f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;var t=Text(r,value,4,0,w-8,h,22,Color.white);t.alignment=TextAlignmentOptions.Center;return b;}
    static Canvas Screen(Transform p,string name,Vector3 position,Vector2 size,float scale)
    {
        var o=new GameObject(name,typeof(RectTransform));o.transform.SetParent(p,false);o.transform.localPosition=position;o.transform.localRotation=Quaternion.Euler(18,0,0);o.transform.localScale=Vector3.one*scale;
        var r=o.GetComponent<RectTransform>();r.sizeDelta=size;var c=o.AddComponent<Canvas>();c.renderMode=RenderMode.WorldSpace;
        var background=o.AddComponent<Image>();background.color=new Color(.02f,.055f,.07f);background.raycastTarget=false;return c;
    }
    public static void Build()
    {ResearchExperienceBuilder.Build();}
}
#endif
