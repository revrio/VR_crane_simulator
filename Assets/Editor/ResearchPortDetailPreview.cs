#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ResearchPortDetailPreview
{
    public static void BuildAndValidate(){ResearchExperienceBuilder.Build();ResearchValidation.Run();}
    public static void CheckAndCapture(ResearchOperatorView view)
    {
        var session=ExperimentSession.Instance;
        var rig=Object.FindFirstObjectByType<ResearchRigVisual>();rig.SyncVisuals();
        ResearchValidation.Check(rig!=null && rig.cables.Length==4,"four physical-looking hoist cables present");
        ResearchValidation.Check(rig.GetComponentsInChildren<Collider>().Length==0,"detail rig does not alter physics");
        ResearchValidation.Check((view.desktopCamera.cullingMask&(1<<27))!=0,"cabin sees supporting boom layer");
        var planes=GeometryUtility.CalculateFrustumPlanes(view.desktopCamera);
        ResearchValidation.Check(rig.boom.GetComponentsInChildren<Renderer>().Any(r=>GeometryUtility.TestPlanesAABB(planes,r.bounds)),"boom visible above cabin windscreen");
        var yard=GameObject.Find("Container yard stacks");
        ResearchValidation.Check(yard.transform.childCount>=15 && yard.GetComponentsInChildren<Collider>().Length==0,"decorative freight present without collisions");
        foreach(var load in session.loads)
        {
            var mesh=load.transform.Find("Detailed "+load.loadId).GetComponent<MeshFilter>().sharedMesh;
            ResearchValidation.Check(mesh.bounds.size.x<=3.401f && mesh.bounds.size.y<=1.301f && mesh.bounds.size.z<=1.601f,"container detail fits validated footprint");
            ResearchValidation.Check(load.GetComponentsInChildren<Collider>().Length==1,"single original cargo collider retained");
        }
        Vector3 pivot=session.crane.craneRotationPivot.position;
        Capture("docs/images/detailed-port.png",pivot+new Vector3(42,42,-45),new Vector3(pivot.x,31.2f,pivot.z),56,true);
        session.SelectLayout(session.layoutC);
        Vector3 stack=session.loads[1].transform.position;
        Capture("docs/images/stacked-layout-c.png",stack+new Vector3(5,3,-6),stack,48,false);
        Vector3 spreader=session.hook.transform.position;
        Capture("docs/images/spreader-detail.png",spreader+new Vector3(3,1.8f,-3.4f),spreader+Vector3.up*.25f,48,false);
        session.SelectLayout(session.layoutA);view.EnterCabin();rig.SyncVisuals();
        Debug.Log("RESEARCH_PORT_DETAILS_PREVIEW_PASSED");
    }
    static void Capture(string path,Vector3 position,Vector3 focus,float fov,bool originalCrane)
    {
        var camera=new GameObject("Port detail capture").AddComponent<Camera>();camera.enabled=false;
        camera.fieldOfView=fov;camera.farClipPlane=1200;camera.transform.position=position;camera.transform.LookAt(focus);
        camera.cullingMask=~((1<<27)|(1<<28)|(1<<30));if(!originalCrane)camera.cullingMask&=~(1<<29);
        var target=new RenderTexture(1280,960,24);target.Create();
        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
        var previous=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(1280,960,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,960),0,0);image.Apply();
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,image.EncodeToPNG());
        RenderTexture.active=previous;target.Release();Object.DestroyImmediate(image);Object.DestroyImmediate(target);Object.DestroyImmediate(camera.gameObject);
    }
}
#endif
