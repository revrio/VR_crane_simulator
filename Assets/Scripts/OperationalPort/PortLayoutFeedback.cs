using System;
using System.IO;
using System.Linq;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Post-task self-report, kept separate from objective task timing/incident data.
public class PortLayoutFeedback : MonoBehaviour
{
    public PortDirector director;
    public static readonly string[] Labels={"Head turning / looking effort","Neck posture / movement effort","Shoulder / arm reaching effort","Neck discomfort","Shoulder discomfort","Hand / wrist discomfort","Eye strain"};
    public readonly float[] Values=new float[7];
    public readonly bool[] Answered=new bool[7];
    public bool Dismissed {get;private set;}
    public string Message {get;private set;}="";
    string runId;
    public bool Available=>director.Stage==PortStage.Complete&&director.Log!=null;
    public bool Visible=>Available&&!Dismissed;
    void Update()
    {
        string current=director.Log?.Id;
        if(current==runId)return;
        runId=current;Array.Clear(Values,0,Values.Length);Array.Clear(Answered,0,Answered.Length);Dismissed=false;Message="";
    }
    public void Rate(int index,float value)
    {
        if(!Available)return;
        Values[index]=Mathf.Round(Mathf.Clamp(value,0,10));Answered[index]=true;
        Save("draft");
    }
    public void Submit()
    {
        if(Answered.Any(a=>!a)){Message="Rate each item, including 0 when there was none.";return;}
        if(Save("submitted")){Dismissed=true;Message="Feedback saved with this layout's session.";}
    }
    public void Skip(){if(Save("skipped"))Dismissed=true;}
    bool Save(string status)
    {
        if(!Available)return false;
        try
        {
            string header="schema_version,run_id,layout_id,input_mode,status,submitted_utc,head_movement_effort_0_10,neck_movement_effort_0_10,shoulder_reach_effort_0_10,neck_discomfort_0_10,shoulder_discomfort_0_10,hand_wrist_discomfort_0_10,eye_strain_0_10\n";
            string row=string.Join(",",new[]{"post_layout_v1",director.Log.Id,director.Plan.layoutId,PortView.SimulatedVR?"SimulatedXR":director.view.HeadsetRunning?"QuestXR":"Desktop",status,DateTime.UtcNow.ToString("O")}.Concat(Values.Select((v,i)=>Answered[i]?v.ToString("0",CultureInfo.InvariantCulture):"")));
            string file=Path.Combine(director.Log.DirectoryPath,"layout_feedback.csv");
            File.WriteAllText(file+".tmp",header+row+"\n");File.Copy(file+".tmp",file,true);File.Delete(file+".tmp");
            Message="";return true;
        }
        catch(Exception e){Message="Could not save feedback: "+e.Message;Debug.LogError(Message);return false;}
    }
}

// Uses the existing canvas/raycaster so sliders work with mouse and XR rays.
public class PortFeedbackForm : MonoBehaviour
{
    public PortLayoutFeedback feedback;
    GameObject form;
    TextMeshProUGUI[] labels;
    Slider[] sliders;
    TextMeshProUGUI status;
    void Start()
    {
        var panel=GetComponent<PortPanel>();
        feedback=panel.director.GetComponent<PortLayoutFeedback>();
        form=Rect(transform,"Post-layout feedback",0,0,560,1080).gameObject;
        form.AddComponent<Image>().color=new Color(.025f,.055f,.075f,.995f);
        Text(form.transform,"LAYOUT COMPLETE / FEEDBACK",24,25,512,45,25);
        Text(form.transform,"How did you feel during this layout?\n0 = none / no effort     10 = extreme\nSelf-report, not a clinical WSD score.",24,82,512,100,20);
        labels=new TextMeshProUGUI[7];sliders=new Slider[7];
        for(int i=0;i<7;i++)
        {
            int item=i;float y=190+i*94;
            labels[i]=Text(form.transform,"",24,y,512,32,20);
            var track=Rect(form.transform,"Rating "+i,80,y+36,400,26);
            track.gameObject.AddComponent<Image>().color=new Color(.11f,.19f,.23f);
            var slider=track.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=10;slider.wholeNumbers=true;slider.navigation=new Navigation{mode=Navigation.Mode.None};
            var handle=Rect(track,"Handle",0,0,30,36);handle.anchorMin=handle.anchorMax=new Vector2(0,.5f);handle.pivot=new Vector2(.5f,.5f);handle.anchoredPosition=Vector2.zero;
            var image=handle.gameObject.AddComponent<Image>();image.color=new Color(.1f,.85f,.75f);slider.handleRect=handle;slider.targetGraphic=image;
            slider.onValueChanged.AddListener(v=>feedback.Rate(item,v));sliders[i]=slider;
            Button(form.transform,"−",24,y+28,42,42,()=>feedback.Rate(item,feedback.Values[item]-1));
            Button(form.transform,"+",494,y+28,42,42,()=>feedback.Rate(item,feedback.Values[item]+1));
            Button(form.transform,"0 / None",80,y+65,140,25,()=>feedback.Rate(item,0));
        }
        status=Text(form.transform,"",24,870,512,70,18);
        Button(form.transform,"SAVE FEEDBACK",24,960,310,55,feedback.Submit);
        Button(form.transform,"Skip",350,960,186,55,feedback.Skip);
        form.SetActive(false);
    }
    void Update()
    {
        if(feedback==null||form==null)return;
        form.SetActive(feedback.Visible);
        if(!feedback.Visible)return;
        form.transform.SetAsLastSibling();
        for(int i=0;i<7;i++)
        {
            labels[i].text=PortLayoutFeedback.Labels[i]+"  "+(feedback.Answered[i]?feedback.Values[i].ToString("0")+" / 10":"— not rated");
            sliders[i].SetValueWithoutNotify(feedback.Values[i]);
        }
        status.text=feedback.Message==""?"Move each slider, or use + / −. Select 0 / None explicitly where appropriate. Saved alongside the layout's task data.":feedback.Message;
    }
    static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    static TextMeshProUGUI Text(Transform parent,string text,float x,float y,float w,float h,float size)
    {var r=Rect(parent,text,x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.text=text;t.fontSize=size;t.color=Color.white;t.raycastTarget=false;return t;}
    static void Button(Transform parent,string text,float x,float y,float w,float h,UnityEngine.Events.UnityAction action)
    {var r=Rect(parent,text,x,y,w,h);r.gameObject.AddComponent<Image>().color=new Color(.08f,.31f,.36f);var b=r.gameObject.AddComponent<Button>();b.navigation=new Navigation{mode=Navigation.Mode.None};b.onClick.AddListener(action);Text(r,text,5,3,w-10,h-6,18).alignment=TextAlignmentOptions.Center;}
}
