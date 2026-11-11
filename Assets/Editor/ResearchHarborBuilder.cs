#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ResearchHarborBuilder
{
    public static void Apply(ExperimentSession session, ResearchOperatorView view)
    {
        var old = GameObject.Find("Harbor atmosphere");
        if (old != null) Object.DestroyImmediate(old);
        old = GameObject.Find("Harbor");
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject("Harbor atmosphere");
        var ocean = new GameObject("Animated harbor water", typeof(MeshFilter), typeof(MeshRenderer));
        ocean.transform.SetParent(root.transform,false);
        ocean.transform.position = new Vector3(25,ResearchHarborMotion.WaterLevel,0);
        const string meshPath="Assets/Research/HarborWaterMesh.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(mesh==null){mesh=new Mesh {name="Harbor water / 1600m grid"};AssetDatabase.CreateAsset(mesh,meshPath);}
        mesh.Clear();mesh.indexFormat=IndexFormat.UInt32;
        const int cells=200, stride=cells+1;
        var vertices=new Vector3[stride*stride];var triangles=new int[cells*cells*6];
        for(int z=0;z<=cells;z++)for(int x=0;x<=cells;x++) vertices[z*stride+x]=new Vector3((x/(float)cells-.5f)*1600,0,(z/(float)cells-.5f)*1600);
        int t=0;
        for(int z=0;z<cells;z++)for(int x=0;x<cells;x++)
        {int i=z*stride+x;triangles[t++]=i;triangles[t++]=i+stride;triangles[t++]=i+1;triangles[t++]=i+1;triangles[t++]=i+stride;triangles[t++]=i+stride+1;}
        mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();
        // Include shader displacement in CPU culling bounds.
        mesh.bounds=new Bounds(Vector3.zero,new Vector3(1600,2,1600));EditorUtility.SetDirty(mesh);
        ocean.GetComponent<MeshFilter>().sharedMesh=mesh;
        var water=Mat("Harbor_Water","PortLab/Harbor Water");
        water.SetColor("_DeepColor",new Color(.018f,.15f,.21f));water.SetColor("_CrestColor",new Color(.045f,.30f,.34f));
        Color horizon=new Color(.52f,.67f,.76f);water.SetColor("_HorizonColor",horizon);
        var renderer=ocean.GetComponent<MeshRenderer>();renderer.sharedMaterial=water;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        var sky=Mat("HarborSky","PortLab/Harbor Sky");sky.SetColor("_Zenith",new Color(.13f,.36f,.65f));sky.SetColor("_Horizon",horizon);
        var sun=RenderSettings.sun;
        if(sun!=null){sky.SetVector("_SunDirection",-sun.transform.forward);sun.color=new Color(1,.95f,.86f);}
        RenderSettings.skybox=sky;
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=horizon;
        RenderSettings.fogStartDistance=150;RenderSettings.fogEndDistance=750;
        view.desktopCamera.farClipPlane=1200;view.origin.Camera.farClipPlane=1200;

        Vector3 forward=session.crane.car.position-session.crane.craneRotationPivot.position;forward.y=0;forward.Normalize();
        Vector3 right=Vector3.Cross(Vector3.up,forward);
        var motion=root.AddComponent<ResearchHarborMotion>();motion.routeCenter=session.crane.craneRotationPivot.position+forward*185;
        motion.routeRight=right;motion.routeForward=forward;
        var tug=new GameObject("Harbor tug / background traffic");tug.transform.SetParent(root.transform,false);motion.tug=tug.transform;
        var hull=Solid("TugHull",new Color(.055f,.12f,.15f));var white=Solid("TugCabin",new Color(.86f,.88f,.84f));
        var orange=Solid("TugSafety",new Color(.92f,.36f,.08f));var glass=Solid("TugWindows",new Color(.055f,.20f,.26f));
        Part(tug.transform,"Hull",new Vector3(0,.5f,-1),new Vector3(4.8f,1.6f,10),hull);
        var bow=GameObject.CreatePrimitive(PrimitiveType.Sphere);bow.name="Rounded bow";bow.transform.SetParent(tug.transform,false);bow.transform.localPosition=new Vector3(0,.5f,4);bow.transform.localScale=new Vector3(4.8f,1.6f,5);bow.GetComponent<Renderer>().sharedMaterial=hull;Object.DestroyImmediate(bow.GetComponent<Collider>());
        Part(tug.transform,"Deck trim",new Vector3(0,1.35f,0),new Vector3(4.4f,.18f,10),orange);
        Part(tug.transform,"Wheelhouse",new Vector3(0,2.5f,1.5f),new Vector3(3.2f,2.1f,3.5f),white);
        Part(tug.transform,"Front windows",new Vector3(0,2.85f,3.27f),new Vector3(2.8f,.7f,.035f),glass);
        foreach(float side in new[]{-1f,1f})
        {
            Part(tug.transform,"Side windows",new Vector3(side*1.62f,2.85f,1.5f),new Vector3(.035f,.7f,2.8f),glass);
            for(int i=0;i<4;i++)
            {
                var tyre=GameObject.CreatePrimitive(PrimitiveType.Cylinder);tyre.name="Rubber fender";tyre.transform.SetParent(tug.transform,false);tyre.transform.localPosition=new Vector3(side*2.43f,.85f,-4+i*2.3f);tyre.transform.localRotation=Quaternion.Euler(0,0,90);tyre.transform.localScale=new Vector3(.85f,.13f,.85f);tyre.GetComponent<Renderer>().sharedMaterial=hull;Object.DestroyImmediate(tyre.GetComponent<Collider>());
            }
        }
        Part(tug.transform,"Wheelhouse roof",new Vector3(0,3.62f,1.5f),new Vector3(3.6f,.18f,3.9f),orange);
        Part(tug.transform,"Mast",new Vector3(0,4.5f,.6f),new Vector3(.12f,1.8f,.12f),white);
        Part(tug.transform,"Radar",new Vector3(0,5.3f,.6f),new Vector3(1.5f,.14f,.18f),white);
        Part(tug.transform,"Funnel",new Vector3(0,2.25f,-2.6f),new Vector3(1.2f,1.7f,1.2f),orange);
        Part(tug.transform,"Funnel cap",new Vector3(0,3.1f,-2.6f),new Vector3(1.25f,.25f,1.25f),hull);
        motion.PoseAt(0);AssetDatabase.SaveAssets();
    }
    static Material Mat(string name,string shaderName)
    {
        string path="Assets/Research/Materials/"+name+".mat";var shader=Shader.Find(shaderName);
        if(shader==null)throw new System.InvalidOperationException("Missing harbor shader: "+shaderName);
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}else material.shader=shader;
        EditorUtility.SetDirty(material);return material;
    }
    static Material Solid(string name,Color color){var m=Mat(name,"Universal Render Pipeline/Lit");m.color=color;return m;}
    static void Part(Transform parent,string name,Vector3 position,Vector3 size,Material material)
    {
        var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name=name;part.transform.SetParent(parent,false);part.transform.localPosition=position;part.transform.localScale=size;part.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(part.GetComponent<Collider>());
    }
}
#endif
