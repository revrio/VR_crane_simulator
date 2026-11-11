#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class ResearchExperienceBuilder
{
    static Color navy = new Color(.035f,.065f,.09f), teal = new Color(.22f,.83f,.72f), gold = new Color(1,.7f,.25f);
    static Material skin, hair, vest, ink, cream;
    [MenuItem("VR Research Demo/Upgrade Guided Experience")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.OpenScene(ResearchDemoBuilder.ScenePath);
        var s = UnityEngine.Object.FindFirstObjectByType<ExperimentSession>();
        var v = UnityEngine.Object.FindFirstObjectByType<ResearchOperatorView>();
        var prior = GameObject.Find("Guided experience"); if (prior != null) UnityEngine.Object.DestroyImmediate(prior);
        var oldControls = GameObject.Find("Hold controls"); if(oldControls != null) UnityEngine.Object.DestroyImmediate(oldControls);
        var root = new GameObject("Guided experience");
        var guide = root.AddComponent<ResearchGuide>(); guide.session = s; guide.view = v;
        s.experimentVersion = "wsd-guided-v3";
        s.setupLabel = "Guided operator station; identical voice, speed and tolerances across layouts; Shift precision available on desktop";
        foreach (var renderer in s.crane.transform.root.GetComponentsInChildren<Renderer>(true)) renderer.gameObject.layer = 29;
        s.crane.rotationSpeed = 19; s.crane.rotationSmoothness = 7; s.crane.carSpeed = .105f; s.crane.hookSpeed = 4;
        s.homeTolerance = 2f; s.homeHeightTolerance = 2f; s.homeDwellSeconds = .5f;
        s.hook.GetComponent<Rigidbody>().linearDamping = 1.2f;
        foreach (var load in s.loads) load.Body.linearDamping = .5f; s.hook.captureRadius = 1.35f;
        // Start close to the first pickup, with visible ground-level reference rather than a tiny airborne dot.
        Vector3 pivot = s.crane.craneRotationPivot.position;
        Vector3 direction = s.crane.car.position - pivot; direction.y = 0; direction.Normalize();
        Vector3 initial = s.crane.car.position; initial.y = 37.5f;
        s.hook.transform.position = initial;
        s.crane.hookRope.maxLength = s.crane.car.position.y - initial.y;
        foreach (Transform child in s.homeMarker.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
        s.homeMarker.position = initial;
        Mat("GuideTeal",teal); skin = Mat("MayaSkin",new Color(.66f,.39f,.24f)); hair = Mat("MayaHair",new Color(.055f,.035f,.035f));
        vest = Mat("MayaVest",new Color(1,.45f,.07f)); ink = Mat("MayaUniform",new Color(.055f,.13f,.19f)); cream = Mat("MayaHelmet",new Color(.96f,.93f,.79f));
        var homeMat = Mat("GuideTeal",teal);
        Shape("HOME landing pad",s.homeMarker,new Vector3(0,31.24f-initial.y,0),new Vector3(4,.04f,4),homeMat,PrimitiveType.Cylinder);
        var parkRing=Line("HOME height beacon",s.homeMarker,65,.045f,teal);parkRing.useWorldSpace=false;
        for(int i=0;i<65;i++){float angle=i*Mathf.PI*2/64;parkRing.SetPosition(i,new Vector3(Mathf.Cos(angle)*1.1f,0,Mathf.Sin(angle)*1.1f));}
        var homeText = WorldText("HOME / EMPTY HOOK",s.homeMarker,new Vector3(0,31.29f-initial.y,0),3.5f,Color.white);
        homeText.transform.localRotation = Quaternion.Euler(90,0,0);
        // Distinct jobs, but all contained in one legible arc of the quay. Sources and validation identical.
        s.layoutA.targetPositions = Enumerable.Range(0,3).Select(i=>Polar(pivot,direction,35+i*23,22,31.85f)).ToArray();
        s.layoutB.targetPositions = Enumerable.Range(0,3).Select(i=>Polar(pivot,direction,28+i*17,18,31.85f)).ToArray();
        EditorUtility.SetDirty(s.layoutA); EditorUtility.SetDirty(s.layoutB);
        s.layoutManager.ApplyLayout(s.layoutA);
        foreach (var load in s.loads)
            foreach (var label in load.GetComponentsInChildren<TextMeshPro>())
                if (label.transform.localPosition.z < -.7f)
                { var position = label.transform.localPosition; position.z = -.85f; label.transform.localPosition = position; }
        foreach(var target in s.targets)
        {
            target.GetComponent<BoxCollider>().size = new Vector3(5.6f,2,3.8f);
            foreach(Transform child in target.transform)
            {
                if(child.name == "Bay floor marking") child.localScale = new Vector3(5.6f,.02f,3.8f);
                if(child.name == "Bay border")
                {
                    var p=child.localPosition; var scale=child.localScale;
                    if(Mathf.Abs(p.x)>1) { p.x=Mathf.Sign(p.x)*2.8f; scale.z=3.8f; }
                    else { p.z=Mathf.Sign(p.z)*1.9f; scale.x=5.6f; }
                    child.localPosition=p; child.localScale=scale;
                }
            }
        }
        v.overviewEye.position = new Vector3(45,64,66); v.overviewEye.LookAt(new Vector3(24,32,34));
        v.desktopCamera.fieldOfView=54;
        // Remove original looping music/engine; new motor responds to actual control input.
        var oldAudio=UnityEngine.Object.FindFirstObjectByType<AudioManagerVR>(); if(oldAudio!=null) UnityEngine.Object.DestroyImmediate(oldAudio.gameObject);
        foreach(var audio in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None)) audio.enabled=false;
        guide.motorClip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/engine-6000.wav");
        guide.successClip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/correct-356013.mp3");
        guide.ambientClip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/es-seagulls-from-window-01-65459.mp3");
        guide.voiceKeys=new[]{"welcome","align","lower","couple","lift","transfer","land","release","settle","return","paused","complete","stopped"};
        guide.voiceClips=guide.voiceKeys.Select(key=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Research/Voice/"+key+".wav")).ToArray();
        if(guide.voiceClips.Any(c=>c==null)) throw new Exception("Generate all guide voice clips before building.");
        guide.objectiveRing=Line("Objective halo",root.transform,65,.10f,teal);
        guide.hookRing=Line("Hook ground reticle",root.transform,33,.06f,Color.white);
        guide.route=Line("Ground route",root.transform,2,.06f,new Color(.8f,.86f,.5f));
        guide.objectiveLabel=WorldText("ACTIVE JOB",root.transform,Vector3.zero,2.8f,Color.white);
        Avatar(root.transform,guide);
        foreach(var panel in UnityEngine.Object.FindObjectsByType<ResearchPanel>(FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(panel.gameObject);
        guide.captions=new TextMeshProUGUI[2];guide.readouts=new TextMeshProUGUI[2];guide.portraits=new RawImage[2];
        Console(v.desktopPanel,s,v,guide,0);Console(v.worldPanel,s,v,guide,1);
        ResearchCabinBuilder.Apply(s,v,guide,root.transform);
        ResearchHarborBuilder.Apply(s,v);
        ResearchPortDetailBuilder.Apply(s,v,root.transform);
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("RESEARCH_GUIDED_EXPERIENCE_BUILT");
    }
    public static void BuildPlayer(){ Build(); ResearchBuilds.Linux(); }
    static Vector3 Polar(Vector3 p,Vector3 d,float angle,float radius,float y){var v=p+Quaternion.Euler(0,angle,0)*d*radius;v.y=y;return v;}
    static Material Mat(string name,Color c)
    {
        string path="Assets/Research/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=c;return m;
    }
    static GameObject Shape(string name,Transform parent,Vector3 p,Vector3 size,Material m,PrimitiveType type)
    {var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
    static TextMeshPro WorldText(string text,Transform parent,Vector3 p,float width,Color color)
    {var g=new GameObject(text);g.transform.SetParent(parent,false);g.transform.localPosition=p;var t=g.AddComponent<TextMeshPro>();t.text=text;t.fontSize=4;t.color=color;t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=new Vector2(width,1);return t;}
    static LineRenderer Line(string name,Transform p,int n,float width,Color color)
    {var g=new GameObject(name);g.transform.SetParent(p);var l=g.AddComponent<LineRenderer>();l.positionCount=n;l.startWidth=l.endWidth=width;l.sharedMaterial=Mat(name.Replace(" ",""),color);return l;}
    static void Avatar(Transform parent,ResearchGuide guide)
    {
        var actor=new GameObject("Maya / port instructor");actor.transform.SetParent(parent);actor.transform.position=new Vector3(0,-200,0);
        Transform a=actor.transform;
        Shape("Work vest",a,new Vector3(0,.95f,0),new Vector3(.64f,.76f,.33f),vest,PrimitiveType.Capsule);
        Shape("Shirt",a,new Vector3(0,1.27f,.005f),new Vector3(.42f,.25f,.32f),ink,PrimitiveType.Cube);
        for(int side=-1;side<=1;side+=2)
        {
            Shape("Reflective vest stripe",a,new Vector3(side*.16f,1.0f,-.174f),new Vector3(.055f,.55f,.014f),cream,PrimitiveType.Cube);
            Shape("Sleeve",a,new Vector3(side*.36f,1.06f,0),new Vector3(.21f,.36f,.25f),ink,PrimitiveType.Capsule);
        }
        Shape("Safety band",a,new Vector3(0,.81f,-.177f),new Vector3(.56f,.055f,.016f),cream,PrimitiveType.Cube);
        Shape("Neck",a,new Vector3(0,1.44f,0),new Vector3(.18f,.25f,.18f),skin,PrimitiveType.Cylinder);
        var head=new GameObject("Head gestures");head.transform.SetParent(a,false);head.transform.localPosition=new Vector3(0,1.66f,0);guide.avatarHead=head.transform;
        Shape("Hair",head.transform,new Vector3(0,.035f,.055f),new Vector3(.47f,.55f,.39f),hair,PrimitiveType.Sphere);
        Shape("Bun",head.transform,new Vector3(.19f,-.02f,.19f),new Vector3(.22f,.24f,.2f),hair,PrimitiveType.Sphere);
        Shape("Face",head.transform,new Vector3(0,-.005f,-.06f),new Vector3(.365f,.46f,.32f),skin,PrimitiveType.Sphere);
        for(int side=-1;side<=1;side+=2)
        {
            Shape("Eye white",head.transform,new Vector3(side*.079f,.025f,-.204f),new Vector3(.079f,.046f,.025f),cream,PrimitiveType.Sphere);
            Shape("Eye",head.transform,new Vector3(side*.075f,.025f,-.219f),new Vector3(.029f,.035f,.012f),hair,PrimitiveType.Sphere);
            Shape("Brow",head.transform,new Vector3(side*.078f,.08f,-.202f),new Vector3(.09f,.017f,.019f),hair,PrimitiveType.Cube);
        }
        Shape("Nose",head.transform,new Vector3(0,-.027f,-.219f),new Vector3(.05f,.075f,.065f),skin,PrimitiveType.Sphere);
        guide.avatarMouth=Shape("Speaking mouth",head.transform,new Vector3(0,-.112f,-.205f),new Vector3(.1f,.02f,.018f),Mat("MayaLips",new Color(.33f,.10f,.08f)),PrimitiveType.Sphere).transform;
        Shape("Hard hat",head.transform,new Vector3(0,.21f,.01f),new Vector3(.53f,.26f,.47f),cream,PrimitiveType.Sphere);
        Shape("Helmet brim",head.transform,new Vector3(0,.14f,-.06f),new Vector3(.58f,.04f,.55f),cream,PrimitiveType.Cube);
        Shape("Helmet stripe",head.transform,new Vector3(0,.24f,-.19f),new Vector3(.07f,.13f,.04f),vest,PrimitiveType.Cube);
        var arm=new GameObject("Instruction gesture");arm.transform.SetParent(a,false);arm.transform.localPosition=new Vector3(.38f,1.13f,0);arm.transform.localRotation=Quaternion.Euler(0,0,35);guide.avatarHand=arm.transform;
        Shape("Forearm",arm.transform,new Vector3(0,-.18f,0),new Vector3(.15f,.32f,.16f),skin,PrimitiveType.Capsule);
        Shape("Hand",arm.transform,new Vector3(0,-.36f,0),new Vector3(.15f,.18f,.1f),skin,PrimitiveType.Sphere);
        Shape("Tablet arm",a,new Vector3(-.34f,.85f,-.03f),new Vector3(.16f,.3f,.16f),skin,PrimitiveType.Capsule);
        Shape("Tablet",a,new Vector3(-.26f,.77f,-.19f),new Vector3(.3f,.36f,.035f),ink,PrimitiveType.Cube);
        foreach(var t in actor.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
        var cameraObject=new GameObject("Maya portrait camera");cameraObject.transform.SetParent(parent);var cam=cameraObject.AddComponent<Camera>();guide.portraitCamera=cam;
        cam.transform.position=actor.transform.position+new Vector3(0,1.48f,-2.8f);cam.transform.LookAt(actor.transform.position+new Vector3(0,1.40f,0));
        cam.orthographic=true;cam.orthographicSize=.69f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.10f,.24f,.28f);cam.cullingMask=1<<30;cam.nearClipPlane=.1f;cam.farClipPlane=6;cam.depth=-10;
        foreach(var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))if(c!=cam)c.cullingMask &= ~(1<<30);
        var lightObj=new GameObject("Portrait soft light");lightObj.transform.SetParent(parent);var light=lightObj.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.cullingMask=1<<30;light.transform.rotation=Quaternion.Euler(35,130,0);
    }
    static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {var o=new GameObject(name,typeof(RectTransform));o.transform.SetParent(parent,false);var r=o.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    static TextMeshProUGUI Text(Transform parent,string text,float x,float y,float w,float h,float size,Color color)
    {var r=Rect(parent,text,x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.text=text;t.fontSize=size;t.color=color;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;}
    static Button Button(Transform parent,string text,float x,float y,float w,float h,Color color)
    {var r=Rect(parent,text,x,y,w,h);var img=r.gameObject.AddComponent<Image>();img.color=color;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=img;var t=Text(r,text,5,0,w-10,h,17,Color.white);t.alignment=TextAlignmentOptions.Center;return b;}
    static TMP_InputField Input(Transform p,float x,float y,float w,string value)
    {var r=Rect(p,"ID",x,y,w,34);r.gameObject.AddComponent<Image>().color=new Color(.10f,.18f,.23f);r.gameObject.AddComponent<RectMask2D>();var i=r.gameObject.AddComponent<TMP_InputField>();i.textViewport=r;i.textComponent=Text(r,value,8,3,w-16,28,18,Color.white);i.text=value;i.characterLimit=32;return i;}
    static void Console(Canvas canvas,ExperimentSession s,ResearchOperatorView v,ResearchGuide g,int index)
    {
        var r=Rect(canvas.transform,"Guided console",0,0,560,1080);r.anchorMin=r.anchorMax=new Vector2(1,.5f);r.pivot=new Vector2(1,.5f);r.anchoredPosition=Vector2.zero;r.gameObject.AddComponent<Image>().color=navy;
        var p=r.gameObject.AddComponent<ResearchPanel>();p.session=s;p.view=v;
        var bindings=r.gameObject.AddComponent<ResearchGuideConsole>();bindings.guide=g;bindings.panel=p;
        Text(r,"PORTLAB   /   GUIDED OPERATIONS",24,18,512,26,18,gold);
        var portrait=Rect(r,"Maya live portrait",24,58,112,112).gameObject.AddComponent<RawImage>();portrait.raycastTarget=false;g.portraits[index]=portrait;
        Text(r,"MAYA",152,59,370,33,28,Color.white);Text(r,"YOUR PORT INSTRUCTOR",152,96,370,24,16,teal);
        bindings.repeat=Button(r,"Repeat [H]",152,132,170,34,new Color(.1f,.24f,.28f));
        bindings.voice=Button(r,"Voice on/off [M]",332,132,204,34,new Color(.1f,.24f,.28f));
        g.captions[index]=Text(r,"Welcome to your shift",24,188,512,105,22,Color.white);
        g.readouts[index]=Text(r,"FOLLOW HOOK",24,302,512,76,18,teal);
        p.guidance=Text(r,"",0,0,0,0,1,Color.clear);p.guidance.gameObject.SetActive(false);
        p.heading=Text(r,"A / DISPERSED BAYS",24,396,370,27,20,gold);
        p.timer=Text(r,"00:00",394,390,146,40,30,Color.white);
        p.state=Text(r,"READY",24,432,512,24,17,teal);p.job=Text(r,"3 DELIVERIES",24,462,512,24,18,Color.white);
        p.practice=Button(r,"PRACTICE [P]",24,504,248,46,new Color(.12f,.44f,.38f));p.start=Button(r,"MEASURE [Enter]",284,504,252,46,new Color(.16f,.25f,.32f));
        p.pause=Button(r,"Pause [Esc]",24,562,160,36,new Color(.12f,.20f,.25f));p.reset=Button(r,"Reset [R]",196,562,160,36,new Color(.12f,.20f,.25f));p.recenter=Button(r,"Recenter [F]",368,562,168,36,new Color(.12f,.20f,.25f));
        p.layoutA=Button(r,"A / Spread",24,612,164,34,new Color(.12f,.20f,.25f));p.layoutB=Button(r,"B / Compact",198,612,164,34,new Color(.12f,.20f,.25f));
        p.layoutC=Button(r,"C / Stacked",372,612,164,34,new Color(.12f,.20f,.25f));
        Text(r,"ID",24,660,30,26,16,teal);p.participant=Input(r,60,656,170,"P001");
        p.order=Rect(r,"Order",244,656,120,34).gameObject.AddComponent<TMP_Dropdown>();p.order.captionText=Text(p.order.transform,"A > B > C",0,0,120,34,17,Color.white);
        p.order.options=ResearchPanel.Orders.Select(code=>new TMP_Dropdown.OptionData(string.Join(" > ",code.ToCharArray()))).ToList();
        // Order selection uses a single explicit cycling button, avoiding a tiny VR popup.
        p.order.enabled=false;bindings.swapOrder=Button(r,"Swap order",374,656,162,34,new Color(.12f,.20f,.25f));
        p.results=Text(r,"Work element results",24,706,512,90,17,Color.white);
        Text(r,"COMFORT  0 none — 10 maximum",24,805,512,24,16,gold);
        p.discomfort=new Slider[3];p.discomfortLabels=new TextMeshProUGUI[3];
        for(int i=0;i<3;i++)
        {
            p.discomfortLabels[i]=Text(r,"",24,835+i*28,170,26,16,Color.white);
            var sr=Rect(r,"Rating",196,839+i*28,328,20);var slider=sr.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=10;slider.wholeNumbers=true;
            Rect(sr,"Track",0,7,328,5).gameObject.AddComponent<Image>().color=new Color(.15f,.28f,.32f);
            var area=Rect(sr,"Handle area",8,0,312,20);var handle=Rect(area,"Handle",0,0,16,20);var im=handle.gameObject.AddComponent<Image>();im.color=teal;handle.sizeDelta=new Vector2(16,0);handle.pivot=new Vector2(.5f,.5f);slider.handleRect=handle;slider.targetGraphic=im;p.discomfort[i]=slider;
        }
        p.saveRatings=Button(r,"Save ratings",24,932,160,32,new Color(.12f,.20f,.25f));p.next=Button(r,"Next layout",196,932,160,32,new Color(.12f,.20f,.25f));p.openData=Button(r,"CSV folder",368,932,168,32,new Color(.12f,.20f,.25f));
        p.export=Text(r,"",24,977,512,40,15,teal);
        bindings.changeView=Button(r,"Change view [Tab]",24,1026,248,32,new Color(.12f,.20f,.25f));
        bindings.couple=Button(r,"Lock / release",284,1026,252,32,new Color(.12f,.20f,.25f));
        if(index==0)
        {
            var controls=Rect(canvas.transform,"Hold controls",24,0,1260,66);controls.anchorMin=controls.anchorMax=new Vector2(0,0);controls.pivot=new Vector2(0,0);controls.anchoredPosition=new Vector2(24,22);
            var input=UnityEngine.Object.FindFirstObjectByType<ResearchCraneInput>();
            string[] labels={"A  Rotate left","D  Rotate right","W  Outward","S  Inward","UP  Raise","DOWN  Lower"};
            Vector3[] commands={Vector3.left,Vector3.right,Vector3.up,Vector3.down,Vector3.back,Vector3.forward};
            for(int i=0;i<6;i++){var b=Button(controls,labels[i],i*164,0,154,42,new Color(.07f,.16f,.20f,.95f));var hold=b.gameObject.AddComponent<ResearchHoldControl>();hold.input=input;hold.command=commands[i];}
            bindings.desktopCouple=Button(controls,"SPACE  Lock / release",984,0,252,42,new Color(.1f,.35f,.31f));
            Text(controls,"HOLD A CONTROL TO MOVE  /  SHIFT fine control  /  TAB camera  /  SCROLL zoom  /  RMB orbit",0,49,1240,26,17,Color.white);
        }
    }
}
#endif
