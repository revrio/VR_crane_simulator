#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class ResearchPortDetailBuilder
{
    const string Folder = "Assets/Research/";
    static Material steel, dark, yellow, rust;
    static Material[] paints;
    static Mesh containerMesh;

    public static void Apply(ExperimentSession session, ResearchOperatorView view, Transform parent)
    {
        steel=Mat("DetailSteel",new Color(.39f,.44f,.46f),.8f,.55f);
        dark=Mat("DetailRubber",new Color(.025f,.036f,.043f),.1f,.25f);
        yellow=Mat("DetailSafetyYellow",new Color(.98f,.64f,.075f),.45f,.38f);
        rust=Mat("DetailOxide",new Color(.25f,.12f,.055f),.3f,.2f);
        paints=new[] {Mat("FreightCoral",new Color(.68f,.18f,.10f)),Mat("FreightTeal",new Color(.04f,.38f,.46f)),
            Mat("FreightOchre",new Color(.83f,.52f,.10f)),Mat("FreightBlue",new Color(.12f,.23f,.39f)),Mat("FreightGreen",new Color(.20f,.31f,.24f))};
        var texture=PaintTexture();
        foreach(var paint in paints){paint.SetTexture("_BaseMap",texture);paint.enableInstancing=true;EditorUtility.SetDirty(paint);}
        containerMesh=BuildContainerMesh();
        for(int i=0;i<session.loads.Length;i++)
        {
            var load=session.loads[i];
            foreach(Transform child in load.transform.Cast<Transform>().ToArray())
            {
                if(child.name=="Container body") child.GetComponent<Renderer>().enabled=false;
                else Object.DestroyImmediate(child.gameObject);
            }
            var visual=Container(load.transform,paints[i],Vector3.zero,Vector3.one,"Detailed "+load.loadId);
            AddTaskLabels(visual.transform,load.loadId,i);
        }
        AddYard(session,parent);
        AddRig(session,parent);
        AddStackLayout(session);
        session.experimentVersion="wsd-detailed-port-v6";
        session.setupLabel="Detailed freight and hoist; layouts A/B change receiving positions, C changes pickup stacking; same controls and tolerances";
        session.conditionOrder="ABC";
        session.layoutManager.ApplyLayout(session.layoutA);
        EditorUtility.SetDirty(session);
    }

    static void AddStackLayout(ExperimentSession session)
    {
        const string path=Folder+"Layouts/LayoutC.asset";
        var config=AssetDatabase.LoadAssetAtPath<LayoutConfig>(path);
        if(config==null){config=ScriptableObject.CreateInstance<LayoutConfig>();AssetDatabase.CreateAsset(config,path);}
        config.layoutId="C";config.layoutName="Layout C - Stacked pickup";
        config.description="Unstack C1 (top), C2 (middle), C3 (bottom). Ground receiving bays match B. This condition changes source height/arrangement and is separate from the A/B receiving-layout comparison.";
        Vector3 source=session.layoutA.loadSpawnPositions[1];
        config.loadSpawnPositions=Enumerable.Range(0,3).Select(i=>new Vector3(source.x,31.85f+(2-i)*1.3f,source.z)).ToArray();
        config.targetPositions=(Vector3[])session.layoutB.targetPositions.Clone();
        config.loadYawDegrees=new float[3];config.targetYawDegrees=new float[3];config.difficultyRating=3;
        session.layoutC=config;EditorUtility.SetDirty(config);
    }

    static Mesh BuildContainerMesh()
    {
        var root=new GameObject("Container mesh authoring");
        Material paint=paints[0];
        Box(root.transform,"Shell",Vector3.zero,new Vector3(3.30f,1.18f,1.50f),paint);
        for(int side=-1;side<=1;side+=2)
        {
            for(int rib=0;rib<19;rib++)
            {
                float x=-1.49f+rib*.166f;
                Box(root.transform,"Pressed steel side",new Vector3(x,0,side*.773f),new Vector3(.07f,1.12f,.044f),paint);
            }
            foreach(float y in new[]{-.605f,.605f})
                Box(root.transform,"Side rail",new Vector3(0,y,side*.754f),new Vector3(3.27f,.09f,.09f),paint);
            foreach(float x in new[]{-1.65f,1.65f})
            {
                Box(root.transform,"Corner post",new Vector3(x,0,side*.75f),new Vector3(.1f,1.3f,.1f),paint);
                foreach(float y in new[]{-.60f,.60f})
                {
                    Box(root.transform,"Corner casting",new Vector3(x,y,side*.75f),new Vector3(.10f,.10f,.10f),steel);
                    Box(root.transform,"Casting aperture",new Vector3(x,y,side*.799f),new Vector3(.056f,.034f,.002f),dark);
                }
            }
        }
        for(int rib=0;rib<18;rib++)
            Box(root.transform,"Roof corrugation",new Vector3(-1.45f+rib*.17f,.615f,0),new Vector3(.065f,.055f,1.38f),paint);
        foreach(float end in new[]{-1f,1f})
        {
            foreach(float y in new[]{-.605f,.605f})Box(root.transform,"End rail",new Vector3(end*1.65f,y,0),new Vector3(.1f,.09f,1.5f),paint);
            foreach(float leaf in new[]{-1f,1f})
            {
                Box(root.transform,"Door leaf",new Vector3(end*1.664f,0,leaf*.36f),new Vector3(.035f,1.1f,.69f),paint);
                Box(root.transform,"Door gasket",new Vector3(end*1.687f,0,leaf*.015f),new Vector3(.012f,1.12f,.022f),dark);
                foreach(float z in new[]{.18f,.53f})
                {
                    Box(root.transform,"Locking bar",new Vector3(end*1.688f,0,leaf*z),new Vector3(.022f,1.09f,.025f),steel);
                    Box(root.transform,"Lock handle",new Vector3(end*1.695f,-.13f,leaf*z+.055f),new Vector3(.01f,.026f,.13f),steel);
                }
                foreach(float y in new[]{-.38f,.38f})Box(root.transform,"Door hinge",new Vector3(end*1.69f,y,leaf*.66f),new Vector3(.018f,.07f,.11f),steel);
            }
        }
        // Small worn patches near the bottom rail; shared geometry keeps all loads equivalent.
        for(int i=0;i<9;i++)Box(root.transform,"Paint wear",new Vector3(-1.3f+i*.31f,-.54f,-.799f),new Vector3(.055f+(i%3)*.025f,.022f,.001f),rust);
        var materials=new[]{paint,steel,dark,rust};
        var parts=root.GetComponentsInChildren<MeshFilter>();
        var groups=new List<Mesh>();
        foreach(var material in materials)
        {
            var combines=parts.Where(p=>p.GetComponent<Renderer>().sharedMaterial==material)
                .Select(p=>new CombineInstance{mesh=p.sharedMesh,transform=root.transform.worldToLocalMatrix*p.transform.localToWorldMatrix}).ToArray();
            var group=new Mesh{indexFormat=IndexFormat.UInt32};group.CombineMeshes(combines,true,true);groups.Add(group);
        }
        string path=Folder+"FreightContainerMesh.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null){mesh=new Mesh{name="Corrugated freight / doors and castings"};AssetDatabase.CreateAsset(mesh,path);}
        mesh.Clear();mesh.indexFormat=IndexFormat.UInt32;
        mesh.CombineMeshes(groups.Select(g=>new CombineInstance{mesh=g,transform=Matrix4x4.identity}).ToArray(),false,false);
        mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        foreach(var group in groups)Object.DestroyImmediate(group);Object.DestroyImmediate(root);
        return mesh;
    }

    static GameObject Container(Transform parent,Material paint,Vector3 position,Vector3 scale,string name)
    {
        var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);
        obj.transform.localPosition=position;obj.transform.localScale=scale;
        obj.GetComponent<MeshFilter>().sharedMesh=containerMesh;obj.GetComponent<Renderer>().sharedMaterials=new[]{paint,steel,dark,rust};
        return obj;
    }
    static void AddTaskLabels(Transform parent,string id,int index)
    {
        Box(parent,"Roof ID backing",new Vector3(0,.646f,0),new Vector3(1.4f,.005f,.65f),dark);
        Label(parent,id,new Vector3(0,.651f,0),Quaternion.Euler(90,0,0),new Vector2(1.3f,.6f),.65f);
        for(int side=-1;side<=1;side+=2)
        {
            var rotation=Quaternion.Euler(0,side<0?0:180,0);
            Label(parent,"PORTLAB  /  "+id,new Vector3(0,.15f,side*.800f),rotation,new Vector2(2.5f,.38f),.28f);
            Label(parent,$"PLTU  804{index+1}19  /  22G1",new Vector3(0,-.24f,side*.800f),rotation,new Vector2(2.5f,.26f),.12f);
        }
        Label(parent,"CAUTION\nHIGH CUBE",new Vector3(1.702f,.28f,-.38f),Quaternion.Euler(0,-90,0),new Vector2(.5f,.24f),.08f);
    }
    static void AddYard(ExperimentSession session,Transform parent)
    {
        var root=new GameObject("Container yard stacks");root.transform.SetParent(parent,false);
        Bounds deck=GameObject.Find("Quay deck").GetComponent<Renderer>().bounds;
        Vector3 pivot=session.crane.craneRotationPivot.position;
        Vector3 forward=session.crane.car.position-pivot;forward.y=0;forward.Normalize();
        int count=0;
        for(int row=0;row<9;row++)for(int column=0;column<8;column++)
        {
            if((row+column)%2!=0)continue;
            var point=new Vector3(deck.center.x-28+column*8,31.2f,deck.center.z-32+row*8);
            Vector3 offset=point-pivot;offset.y=0;
            if(offset.magnitude<11 || (offset.magnitude<30 && Vector3.Dot(offset,forward)>-8))continue;
            int levels=1+(row*3+column)%3;
            for(int level=0;level<levels;level++)
            {
                var p=point+Vector3.up*(.975f+level*1.95f);
                var container=Container(root.transform,paints[(row+column+level)%paints.Length],p,Vector3.one*1.5f,"Stored freight "+(++count));
                if(level==levels-1)Label(container.transform,"PORTLAB FREIGHT",new Vector3(0,.16f,-.8f),Quaternion.identity,new Vector2(2.7f,.35f),.19f);
            }
        }
        Debug.Log("RESEARCH_YARD_CONTAINERS "+count);
    }

    static void AddRig(ExperimentSession session,Transform parent)
    {
        var root=new GameObject("Detailed crane hoist");root.transform.SetParent(parent,false);
        var visual=root.AddComponent<ResearchRigVisual>();visual.session=session;
        var boom=new GameObject("Cabin-visible boom and rails");boom.transform.SetParent(root.transform,false);visual.boom=boom.transform;
        var crane=session.crane;
        Vector3 start=crane.car.parent.TransformPoint(new Vector3(crane.boomStartCorner.localPosition.x,crane.car.localPosition.y,crane.car.localPosition.z));
        Vector3 end=crane.car.parent.TransformPoint(new Vector3(crane.boomEndCorner.localPosition.x,crane.car.localPosition.y,crane.car.localPosition.z));
        float length=Mathf.Max(Vector3.ProjectOnPlane(start-crane.craneRotationPivot.position,Vector3.up).magnitude,
            Vector3.ProjectOnPlane(end-crane.craneRotationPivot.position,Vector3.up).magnitude)+2;
        length=Mathf.Max(length,30);
        for(int side=-1;side<=1;side+=2)
        {
            Box(boom.transform,"Main girder",new Vector3(side*.95f,0,length*.5f),new Vector3(.25f,.65f,length),yellow);
            Box(boom.transform,"Steel running rail",new Vector3(side*.95f,-.38f,length*.5f),new Vector3(.32f,.12f,length),steel);
        }
        for(float z=2;z<length;z+=3.5f)
        {
            Box(boom.transform,"Cross tie",new Vector3(0,.4f,z),new Vector3(2.2f,.14f,.16f),yellow);
            var diagonal=Box(boom.transform,"Lattice brace",new Vector3(0,.48f,z+1.1f),new Vector3(.10f,.10f,3.1f),steel);
            diagonal.transform.localRotation=Quaternion.Euler(0,38,0);
        }
        foreach(var child in boom.GetComponentsInChildren<Transform>())child.gameObject.layer=27;
        var trolley=new GameObject("Moving hoist trolley");trolley.transform.SetParent(root.transform,false);visual.trolley=trolley.transform;
        Box(trolley.transform,"Trolley chassis",new Vector3(0,.15f,0),new Vector3(2.35f,.35f,1.2f),yellow);
        Box(trolley.transform,"Winch housing",new Vector3(0,.55f,0),new Vector3(1.2f,.55f,.95f),dark);
        foreach(float side in new[]{-1f,1f})foreach(float fore in new[]{-1f,1f})
            Cylinder(trolley.transform,"Rail wheel",new Vector3(side*.95f,.35f,fore*.48f),new Vector3(.37f,.12f,.37f),Quaternion.Euler(0,0,90),steel);
        var head=new GameObject("Container twistlock spreader");head.transform.SetParent(root.transform,false);visual.head=head.transform;
        foreach(float side in new[]{-1f,1f})
        {
            Box(head.transform,"Long spreader beam",new Vector3(0,-.17f,side*.68f),new Vector3(3.3f,.20f,.15f),yellow);
            Box(head.transform,"End beam",new Vector3(side*1.55f,-.17f,0),new Vector3(.18f,.20f,1.5f),yellow);
            Box(head.transform,"Hoist cheek plate",new Vector3(side*.37f,.65f,0),new Vector3(.10f,.65f,.57f),yellow);
            foreach(float fore in new[]{-1f,1f})
            {
                Box(head.transform,"Twistlock shoe",new Vector3(side*1.52f,-.29f,fore*.66f),new Vector3(.22f,.12f,.2f),steel);
                Cylinder(head.transform,"Twistlock pin",new Vector3(side*1.52f,-.35f,fore*.66f),new Vector3(.065f,.05f,.065f),Quaternion.identity,dark);
                var brace=Box(head.transform,"Diagonal frame",new Vector3(side*.65f,.06f,fore*.32f),new Vector3(1.5f,.12f,.11f),yellow);
                brace.transform.localRotation=Quaternion.Euler(0,-side*fore*26,side*-13);
            }
        }
        for(int i=0;i<2;i++)Cylinder(head.transform,"Hoist sheave",new Vector3(i==0?-.18f:.18f,.72f,0),new Vector3(.42f,.12f,.42f),Quaternion.Euler(90,0,0),steel);
        Box(head.transform,"Centre beam",new Vector3(0,.0f,0),new Vector3(1.6f,.25f,.6f),yellow);
        for(int i=0;i<9;i++)
        {
            var stripe=Box(head.transform,"Hazard stripe",new Vector3(-1.4f+i*.35f,-.17f,-.762f),new Vector3(.14f,.21f,.008f),dark);
            stripe.transform.localRotation=Quaternion.Euler(0,0,-25);
        }
        Label(head.transform,"PORTLAB / TWISTLOCK",new Vector3(0,.13f,-.31f),Quaternion.identity,new Vector2(1.45f,.18f),.11f);
        visual.lockLamp=Box(head.transform,"Lock status lamp",new Vector3(.55f,.18f,-.31f),Vector3.one*.09f,yellow).GetComponent<Renderer>();
        var cableMaterial=Mat("HoistWireRope",new Color(.33f,.37f,.39f),.85f,.45f);
        cableMaterial.SetTexture("_BaseMap",CableTexture());cableMaterial.SetTextureScale("_BaseMap",new Vector2(3,40));
        visual.cables=new Transform[4];
        for(int i=0;i<4;i++)visual.cables[i]=Cylinder(root.transform,"Steel hoist cable "+(i+1),Vector3.zero,Vector3.one,Quaternion.identity,cableMaterial).transform;
        // Retain the original rigidbody/collider and spring rope; replace their simple renderers.
        foreach(var renderer in session.hook.GetComponentsInChildren<Renderer>())renderer.enabled=false;
        crane.hookRope.ropeRenderer.enabled=false;
        visual.SyncVisuals();
    }

    static Texture2D PaintTexture()
    {
        const string path=Folder+"WeatheredPaint.asset";
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(texture==null){texture=new Texture2D(256,256,TextureFormat.RGB24,false){name="Paint grain and rain streaks",wrapMode=TextureWrapMode.Repeat};AssetDatabase.CreateAsset(texture,path);}
        for(int y=0;y<256;y++)for(int x=0;x<256;x++)
        {
            float grain=Mathf.PerlinNoise(x*.13f,y*.13f),streak=Mathf.PerlinNoise(x*.18f,y*.012f);
            float value=.88f+grain*.07f+streak*.05f;
            texture.SetPixel(x,y,new Color(value,value,value));
        }
        texture.Apply();EditorUtility.SetDirty(texture);return texture;
    }
    static Texture2D CableTexture()
    {
        const string path=Folder+"WireStrands.asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(texture==null){texture=new Texture2D(64,64,TextureFormat.RGB24,false){name="Helical steel strands",wrapMode=TextureWrapMode.Repeat};AssetDatabase.CreateAsset(texture,path);}
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){float v=.5f+.5f*Mathf.Pow(.5f+.5f*Mathf.Sin((x+y)*Mathf.PI/8),2);texture.SetPixel(x,y,new Color(v,v,v));}
        texture.Apply();EditorUtility.SetDirty(texture);return texture;
    }
    static Material Mat(string name,Color color,float metallic=.35f,float smoothness=.3f)
    {
        string path=Folder+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smoothness);EditorUtility.SetDirty(m);return m;
    }
    static GameObject Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material)=>Part(parent,name,PrimitiveType.Cube,position,scale,Quaternion.identity,material);
    static GameObject Cylinder(Transform parent,string name,Vector3 position,Vector3 scale,Quaternion rotation,Material material)=>Part(parent,name,PrimitiveType.Cylinder,position,scale,rotation,material);
    static GameObject Part(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,Quaternion rotation,Material material)
    {
        var part=GameObject.CreatePrimitive(type);part.name=name;part.transform.SetParent(parent,false);part.transform.localPosition=position;part.transform.localScale=scale;part.transform.localRotation=rotation;part.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(part.GetComponent<Collider>());return part;
    }
    static void Label(Transform parent,string value,Vector3 position,Quaternion rotation,Vector2 size,float fontSize)
    {
        var obj=new GameObject(value);obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localRotation=rotation;
        var text=obj.AddComponent<TextMeshPro>();text.text=value;text.fontSize=fontSize*10;text.color=new Color(.92f,.93f,.85f);text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=size;
    }
}
#endif
