using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PortPolishValidation
{
    static double ready;
    static int step;
    static bool previousSimulation;
    const string Pending="PortLab.PolishValidation";
    static PortPolishValidation()
    {
        if(File.Exists("/tmp/portlab-polish-preview-request")||SessionState.GetBool(Pending,false))EditorApplication.update+=Tick;
    }
    [MenuItem("Tools/Operational Port/Validate Scenery and Feedback")]
    public static void Run(){File.WriteAllText("/tmp/portlab-polish-preview-request","run");EditorApplication.update-=Tick;EditorApplication.update+=Tick;}
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        try
        {
            if(File.Exists("/tmp/portlab-polish-preview-request"))
            {
                if(EditorApplication.isPlaying){EditorApplication.ExitPlaymode();return;}
                if(EditorApplication.isPlayingOrWillChangePlaymode)return;
                if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/Scenes/OperationalPort.unity")throw new Exception("Open OperationalPort before validation");
                previousSimulation=PortView.SimulatedVR;SessionState.SetBool("PortLab.PolishPreviousSimulator",previousSimulation);
                EditorPrefs.SetBool("PortLab.SimulatedVR",false);File.Delete("/tmp/portlab-polish-preview-request");
                SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();return;
            }
            if(!SessionState.GetBool(Pending,false))return;
            if(!EditorApplication.isPlaying)return;
            var d=UnityEngine.Object.FindFirstObjectByType<PortDirector>();
            if(d==null||d.GetComponent<PortScenery>()==null||d.GetComponent<PortLayoutFeedback>()==null)return;
            if(ready==0){ready=EditorApplication.timeSinceStartup+4;return;}
            if(EditorApplication.timeSinceStartup<ready)return;
            if(step==0)
            {
                var scenery=GameObject.Find("Port scenery / render-only / no gameplay collisions");
                Check(scenery!=null&&scenery.GetComponentsInChildren<Collider>().Length==0,"scenery must not add collisions");
                Check(scenery.GetComponentsInChildren<PortCargo>().Length==0&&d.allCargo.Length==5,"scenery must preserve five task cargos");
                Check(scenery.GetComponentsInChildren<MeshFilter>().Length<30,"scenery should be batched");
                foreach(var mode in new[]{"Desktop","Quest","Simulator"})
                    foreach(var cue in new[]{"couple","release","lower","paused"})Check(Resources.Load<AudioClip>("PortVoice/"+mode+"/"+cue)!=null,"missing speech "+mode+"/"+cue);
                d.view.Select(4);OperationalPortValidation.Capture("port-polish-overview");
                d.view.Select(3);OperationalPortValidation.Capture("port-polish-cabin");
                d.outputOverride="/tmp/portlab-feedback-validation";d.automated=true;d.Begin();
                typeof(PortDirector).GetField("<Stage>k__BackingField",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(d,PortStage.Complete);
                ready=EditorApplication.timeSinceStartup+1;step=1;return;
            }
            if(step==1)
            {
                var feedback=d.GetComponent<PortLayoutFeedback>();Check(feedback.Visible,"feedback must open on completion");
                var form=UnityEngine.Object.FindFirstObjectByType<PortFeedbackForm>();Check(form!=null,"feedback form missing");
                Check(form.GetComponentsInChildren<UnityEngine.UI.Slider>().Length==7,"seven sliders required");
                OperationalPortValidation.Capture("port-polish-feedback");
                feedback.Submit();Check(!feedback.Dismissed,"unanswered ratings must not submit");
                for(int i=0;i<7;i++)feedback.Rate(i,i);
                feedback.Submit();Check(feedback.Dismissed,"rated form must submit");
                string csv=File.ReadAllText(Path.Combine(d.Log.DirectoryPath,"layout_feedback.csv"));
                Check(csv.Contains("submitted")&&csv.Contains(",0,1,2,3,4,5,6"),"feedback values must persist including explicit zero");
                d.ResetOperation();
                File.WriteAllText("/tmp/portlab-polish-validation.txt","PASSED: scenery has no colliders or task cargo; batched rendering; mode-specific voice clips; seven feedback sliders; unanswered validation; submitted CSV including zero; screenshots captured.");
                Debug.Log("PORT_POLISH_VALIDATION_PASSED");Finish();
            }
        }
        catch(Exception e){File.WriteAllText("/tmp/portlab-polish-validation.txt",e.ToString());Debug.LogException(e);Finish();}
    }
    static void Finish()
    {
        SessionState.SetBool(Pending,false);EditorApplication.update-=Tick;
        EditorPrefs.SetBool("PortLab.SimulatedVR",SessionState.GetBool("PortLab.PolishPreviousSimulator",true));
        if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();
    }
}
