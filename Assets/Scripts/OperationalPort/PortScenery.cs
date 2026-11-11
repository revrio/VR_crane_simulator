using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

// Rendering only: no colliders, rigidbodies, cargo IDs, obstacles or task slots.
// Meshes are combined by material to keep the Quest draw-call cost bounded.
public class PortScenery : MonoBehaviour
{
    public PortDirector director;
    readonly Dictionary<Material,List<CombineInstance>> batches=new Dictionary<Material,List<CombineInstance>>();
    readonly List<Object> owned=new List<Object>();
    Transform root;
    Mesh cube;
    Material hull,oxide,steel,ivory,glass,dark,yellow,asphalt;
    Material[] freight;
    Texture2D weather;
    void Start()
    {
        root=new GameObject("Port scenery / render-only / no gameplay collisions").transform;
        root.SetParent(transform,false);
        var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=primitive.GetComponent<MeshFilter>().sharedMesh;
        primitive.SetActive(false);Destroy(primitive);
        weather=new Texture2D(64,64,TextureFormat.RGB24,false);weather.wrapMode=TextureWrapMode.Repeat;
        var random=new System.Random(7742);
        for(int y=0;y<64;y++)for(int x=0;x<64;x++)
        {
            float streak=(x%13==0?.16f:0)*(1-y/64f),v=.83f+(float)random.NextDouble()*.17f-streak;
            weather.SetPixel(x,y,new Color(v,v*.98f,v*.94f));
        }
        weather.Apply();owned.Add(weather);
        hull=Mat("Weathered ocean blue",new Color(.045f,.12f,.16f),.45f);
        oxide=Mat("Red antifouling",new Color(.32f,.095f,.065f));
        steel=Mat("Galvanized deck fittings",new Color(.39f,.43f,.44f),.65f);
        ivory=Mat("Marine off white",new Color(.77f,.79f,.75f));glass=Mat("Bridge tinted glazing",new Color(.035f,.12f,.16f),.5f);
        dark=Mat("Rubber and black steel",new Color(.03f,.035f,.035f));yellow=Mat("Faded safety ochre",new Color(.83f,.57f,.14f));
        asphalt=Mat("Terminal extension",new Color(.25f,.28f,.28f));
        freight=new[]{Mat("Freight oxide",new Color(.52f,.16f,.1f)),Mat("Freight blue",new Color(.12f,.26f,.33f)),Mat("Freight green",new Color(.2f,.31f,.25f)),Mat("Freight cream",new Color(.69f,.65f,.49f)),Mat("Freight grey",new Color(.42f,.46f,.46f))};
        UpgradeWorkingShip();
        BackgroundShip(new Vector3(-145,0,135),12,"OCEAN MERIDIAN",0);
        BackgroundShip(new Vector3(160,0,205),-18,"NORTH ATLANTIC",2);
        Yard();Flush();
        Debug.Log("PORT_SCENERY_READY: detailed feeder, two background ships and render-only yard freight; original task colliders unchanged.");
    }
    Material Mat(string name,Color color,float metallic=.15f)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",color);m.SetTexture("_BaseMap",weather);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",.22f);m.enableInstancing=true;owned.Add(m);return m;
    }
    void Box(Vector3 p,Vector3 size,Material m,Quaternion rotation=default)
    {
        if(rotation==default)rotation=Quaternion.identity;
        if(!batches.TryGetValue(m,out var list)){list=new List<CombineInstance>();batches.Add(m,list);}
        list.Add(new CombineInstance{mesh=cube,transform=Matrix4x4.TRS(p,rotation,size)});
    }
    void Beam(Vector3 a,Vector3 b,float width,Material m)
    {Box((a+b)*.5f,new Vector3(width,width,Vector3.Distance(a,b)),m,Quaternion.LookRotation(b-a));}
    void UpgradeWorkingShip()
    {
        var vessel=GameObject.Find("Moored feeder vessel / operational berth");
        if(vessel!=null)foreach(var renderer in vessel.GetComponentsInChildren<MeshRenderer>())
            if(renderer.name=="Ship hull"||renderer.name=="Rounded bow"||renderer.name=="Boot stripe")renderer.enabled=false;
        if(vessel!=null)foreach(var label in vessel.GetComponentsInChildren<TextMeshPro>())label.enabled=false;
        Hull(new Vector3(0,0,18),Quaternion.identity,1);
        // Far-side deck stacks stay clear of every gameplay slot (z=16).
        for(int x=-12;x<=19;x+=8)for(int tier=0;tier<2;tier++)Container(new Vector3(x,4.3f+tier*2.6f,23.5f),freight[(x+20+tier)%5]);
        for(int side=-1;side<=1;side+=2)
        {
            float z=18+side*8.6f;
            for(int x=-28;x<=28;x+=3)
            {
                Box(new Vector3(x,3.5f,z),new Vector3(.055f,1,.055f),ivory);
                Box(new Vector3(x,3.9f,z),new Vector3(3,.055f,.055f),ivory);
                Box(new Vector3(x,3.5f,z),new Vector3(3,.045f,.045f),steel);
            }
            for(int x=-24;x<=28;x+=13)
            {
                Box(new Vector3(x,3.2f,z-side*.5f),new Vector3(1.3f,.3f,.7f),steel);
                Box(new Vector3(x,3.5f,z-side*.5f),new Vector3(.6f,.4f,.5f),dark);
                Beam(new Vector3(x,3.4f,z),new Vector3(x-3,.4f,5.8f),.08f,oxide);
            }
        }
        for(int level=0;level<3;level++)for(int window=0;window<5;window++)
            Box(new Vector3(-27.5f+window*1.7f,5.5f+level*1.7f,11.96f),new Vector3(.7f,.65f,.04f),glass);
        Box(new Vector3(-24,12,18),new Vector3(8,.1f,10),steel);
        Box(new Vector3(-22,15.2f,18),new Vector3(.18f,6,.18f),ivory);
        Box(new Vector3(-22,17,18),new Vector3(4,.1f,.1f),steel);
        Box(new Vector3(-22,18.3f,18),new Vector3(2,.35f,.45f),ivory);
        Box(new Vector3(-28,5.2f,24.2f),new Vector3(5,.5f,1.4f),yellow); // covered lifeboat
        Label("PORTLAB MERIDIAN",new Vector3(9,1.65f,8.92f),12,1.1f,Quaternion.identity);
        for(int i=0;i<6;i++)Box(new Vector3(29+i*.12f,.3f+i*.17f,9.3f),new Vector3(.65f,.04f,.025f),ivory);
    }
    void Hull(Vector3 origin,Quaternion rotation,float scale)
    {
        // Watertight faceted feeder hull, tapered stern and raked bow.
        float[] x={-33,-30,-20,12,25,33,38};float[] widths={5,8.8f,9,9,8,5,.15f};
        var vertices=new List<Vector3>();var triangles=new List<int>();
        for(int i=0;i<x.Length;i++)
        {
            float w=widths[i];
            foreach(var v in new[]{new Vector3(x[i],2.96f,-w),new Vector3(x[i],.1f,-w*.88f),new Vector3(x[i],-1.7f,-w*.5f),new Vector3(x[i],-1.7f,w*.5f),new Vector3(x[i],.1f,w*.88f),new Vector3(x[i],2.96f,w)})vertices.Add(origin+rotation*(v*scale));
        }
        for(int i=0;i<x.Length-1;i++)for(int j=0;j<6;j++)
        {int a=i*6+j,b=i*6+(j+1)%6,c=(i+1)*6+j,d=(i+1)*6+(j+1)%6;triangles.AddRange(new[]{a,c,b,b,c,d});}
        for(int j=1;j<5;j++){triangles.AddRange(new[]{0,j,j+1});int o=(x.Length-1)*6;triangles.AddRange(new[]{o,o+j+1,o+j});}
        var mesh=new Mesh{name="Tapered feeder hull"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);
        RenderMesh(mesh,hull);
        for(int side=-1;side<=1;side+=2)
        {
            Box(origin+rotation*new Vector3(-2,-.5f,side*8.85f)*scale,new Vector3(52,.65f,.1f)*scale,oxide,rotation);
            Box(origin+rotation*new Vector3(-2,.2f,side*8.95f)*scale,new Vector3(52,.2f,.1f)*scale,dark,rotation);
        }
    }
    void BackgroundShip(Vector3 p,float yaw,string name,int palette)
    {
        Quaternion r=Quaternion.Euler(0,yaw,0);Hull(p,r,1.3f);
        Box(p+r*new Vector3(-28,8,0),new Vector3(13,12,18),ivory,r);
        Box(p+r*new Vector3(-28,13.5f,0),new Vector3(14,1.5f,19),ivory,r);
        for(int i=0;i<8;i++)Box(p+r*new Vector3(-33+i*1.4f,13.5f,-9.55f),new Vector3(.9f,.8f,.04f),glass,r);
        Box(p+r*new Vector3(-31,16,2),new Vector3(3,4,4),oxide,r);
        Box(p+r*new Vector3(-26,18,0),new Vector3(.2f,7,.2f),ivory,r);
        for(int row=0;row<3;row++)for(int col=0;col<7;col++)for(int tier=0;tier<2+(col%3==0?1:0);tier++)
            Container(p+r*new Vector3(-14+col*7,5.1f+tier*2.6f,-6+row*5),freight[(palette+col+row+tier)%5],r);
        Label(name,p+r*new Vector3(0,2,-11.8f),20,1.5f,r);
    }
    void Container(Vector3 p,Material paint,Quaternion r=default)
    {
        if(r==default)r=Quaternion.identity;
        Box(p,new Vector3(6,2.5f,2.35f),paint,r);
        for(int side=-1;side<=1;side+=2)
        {
            for(int rib=0;rib<14;rib++)Box(p+r*new Vector3(-2.8f+rib*.43f,0,side*1.2f),new Vector3(.07f,2.3f,.065f),paint,r);
            Box(p+r*new Vector3(0,-1.23f,side*1.2f),new Vector3(6,.09f,.1f),steel,r);
            Box(p+r*new Vector3(side*2.95f,0,0),new Vector3(.08f,2.5f,2.4f),paint,r);
        }
        for(int z=-1;z<=1;z+=2)
            Box(p+r*new Vector3(3.06f,0,z*.57f),new Vector3(.06f,2.1f,.06f),steel,r);
    }
    void Yard()
    {
        Box(new Vector3(0,-.11f,-100),new Vector3(290,.2f,110),asphalt);
        // Outer blocks only: do not obstruct the +/-22 m task/worker area,
        // the existing truck roads or the reachstacker's route.
        for(int side=-1;side<=1;side+=2)for(int block=0;block<3;block++)for(int bay=0;bay<5;bay++)for(int tier=0;tier<2+(bay%3==0?1:0);tier++)
            Container(new Vector3(side*(72+block*9),1.3f+tier*2.6f,-12-bay*5),freight[(bay+block+tier+(side+1))%5]);
        for(int row=0;row<2;row++)for(int bay=0;bay<14;bay++)
            Container(new Vector3(-64+bay*9,1.3f,-112-row*6),freight[(bay+row)%5]);
        for(int x=-130;x<=130;x+=22)
        {
            Box(new Vector3(x,8,-64),new Vector3(.18f,16,.18f),steel);
            Box(new Vector3(x,16,-64),new Vector3(4,.1f,.14f),steel);
            for(int lamp=-1;lamp<=1;lamp+=2)Box(new Vector3(x+lamp*1.5f,15.9f,-64),new Vector3(.8f,.25f,.6f),ivory);
        }
        for(int i=0;i<12;i++)
        {
            float x=-110+i*20;
            Box(new Vector3(x,.02f,-58),new Vector3(5,.025f,.12f),ivory);
            Box(new Vector3(x,1,-144),new Vector3(.1f,2,.1f),steel);
            if(i<11){Box(new Vector3(x+10,1.7f,-144),new Vector3(20,.06f,.06f),steel);Box(new Vector3(x+10,.7f,-144),new Vector3(20,.06f,.06f),steel);}
        }
        for(int i=0;i<8;i++)
        {
            var p=new Vector3(-105+i*3,0,-68);
            for(int layer=0;layer<4;layer++)Box(p+Vector3.up*(.1f+layer*.18f),new Vector3(1.5f,.1f,1.1f),freight[3]);
            Box(p+new Vector3(0,1.2f,3),new Vector3(1.2f,2.4f,1.2f),oxide);
        }
        Label("EXPORT STACKS  /  TERMINAL 04",new Vector3(0,.05f,-104),45,3,Quaternion.Euler(90,0,0));
    }
    void Label(string text,Vector3 p,float width,float height,Quaternion rotation)
    {
        var obj=new GameObject(text);obj.transform.SetParent(root,false);obj.transform.SetPositionAndRotation(p,rotation);
        var label=obj.AddComponent<TextMeshPro>();label.text=text;label.fontSize=height*3;label.rectTransform.sizeDelta=new Vector2(width,height*2);label.alignment=TextAlignmentOptions.Center;label.color=new Color(.8f,.82f,.77f);
    }
    void RenderMesh(Mesh mesh,Material material)
    {
        var obj=new GameObject(material.name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(root,false);
        obj.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
    }
    void Flush()
    {
        foreach(var batch in batches)
        {
            var mesh=new Mesh{name="Batched scenery / "+batch.Key.name,indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(batch.Value.ToArray(),true,true);owned.Add(mesh);RenderMesh(mesh,batch.Key);
        }
        batches.Clear();
    }
    void OnDestroy(){foreach(var item in owned)if(item!=null)Destroy(item);}
}
