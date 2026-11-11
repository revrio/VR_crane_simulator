using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PortPanel : MonoBehaviour
{
    public PortDirector director;
    public TextMeshProUGUI caption,stage,custody,time,controls,results,manifest,safetyStatus;
    public Button start,pause,reset,couple,changeView,cabin,folder,intake,voice;
    public Button trafficMode,stop,recover;
    public Button[] layouts;
    void Start()
    {
        if(director.GetComponent<PortLayoutFeedback>()==null)director.gameObject.AddComponent<PortLayoutFeedback>().director=director;
        if(GetComponent<PortFeedbackForm>()==null)gameObject.AddComponent<PortFeedbackForm>();
        start.onClick.AddListener(director.Begin);pause.onClick.AddListener(director.TogglePause);reset.onClick.AddListener(director.ResetOperation);
        couple.onClick.AddListener(director.ToggleAttachment);changeView.onClick.AddListener(director.view.Next);cabin.onClick.AddListener(director.view.Cabin);
        // Hide legacy controls in previously saved scenes as well as new builds.
        foreach(var button in new[]{intake,trafficMode,stop,recover,voice})if(button!=null)button.gameObject.SetActive(false);
        start.GetComponentInChildren<TextMeshProUGUI>().text="START OPERATION [P]";
        var startRect=(RectTransform)start.transform;startRect.sizeDelta=new Vector2(512,startRect.sizeDelta.y);
        // Compact the remaining controls in existing saved scenes too.
        Place(start,24,510,512);Place(pause,24,568,248);Place(reset,284,568,252);
        Place(couple,24,626,248);Place(changeView,284,626,252);Place(cabin,24,684,512);
        if(layouts!=null)for(int i=0;i<layouts.Length;i++){int index=i;layouts[i].onClick.AddListener(()=>director.SelectLayout((PortLayout)index));}
#if UNITY_WEBGL && !UNITY_EDITOR
        folder.GetComponentInChildren<TextMeshProUGUI>().text="Download operational CSV files";
        folder.onClick.AddListener(()=>PortBrowserExport.Download(director.Log.DirectoryPath));
#else
        folder.onClick.AddListener(()=>Application.OpenURL(new Uri(director.Log.DirectoryPath+System.IO.Path.DirectorySeparatorChar).AbsoluteUri));
#endif
        foreach(var button in GetComponentsInChildren<Button>())button.navigation=new Navigation{mode=Navigation.Mode.None};
    }
    void Update()
    {
        caption.text=director.Instruction;
        stage.text=StageLabel(director.Stage);
        string owner=director.Custody==CargoCustody.Handler?"ON HANDLER":director.Custody==CargoCustody.Truck?"ON TRUCK":director.Custody==CargoCustody.Crane?"LOCKED":director.Custody==CargoCustody.Ship?"ABOARD":director.Custody==CargoCustody.Free?"UNLOCKED":"IN YARD";
        int total=director.Plan?.moves.Length??1;
        custody.text=$"{director.CargoId} / {owner}    MOVE {Mathf.Min(director.MoveIndex+1,total)}/{total}\nABOARD {director.Shipped}/{director.ContainerCount}   /   "+(director.Plan?.label??"READY");
        if(manifest!=null)manifest.text="SHIP ORDER  "+string.Join(" → ",director.Plan.moves.Where(m=>m.kind=="Ship").Select(m=>m.cargoId));
        double seconds=director.Log?.Seconds??0;time.text=$"{(int)seconds/60:00}:{seconds%60:00.0}";
        controls.text=director.view.ModeName+"\nA/D  QUAY     W/S  TROLLEY     ARROWS  HOIST\nSPACE  LOCK / RELEASE     SHIFT  FINE\nTAB  MONITOR   C  CABIN";
        if(director.view.HeadsetRunning)controls.text="SEATED VR\nLEFT STICK X  QUAY / Y  TROLLEY\nRIGHT STICK Y  HOIST / A  LOCK\nRIGHT GRIP  FINE";
        if(director.Stage==PortStage.Park)
            results.text=$"FINAL PARK / TARGET {director.config.park.y:F1} m\nGAP {Vector3.ProjectOnPlane(director.crane.hookBody.position-director.config.park,Vector3.up).magnitude:F1} m  HEIGHT ERROR {director.crane.hookBody.position.y-director.config.park.y:+0.0;-0.0;0.0} m";
        else if(director.Stage==PortStage.Pickup||director.Stage==PortStage.Reposition)
            results.text=$"PICK UP {director.CargoId} / "+(director.ReadyToLock?director.LockControl.ToUpperInvariant():"MOVE CLOSE, THEN PRESS LOCK")+"\n"+director.AlignmentGuidance;
        else if(director.CanOperate)
            results.text=$"{director.TargetSlot.id} / TIER {director.TargetSlot.tier+1}\n"+director.AlignmentGuidance;
        else results.text=$"{director.ContainerCount} CONTAINERS / {total} PLANNED MOVES\n1–4: LAYOUT   P: START OPERATION";
        if(director.Stage==PortStage.Complete&&director.Log!=null)
        {
            int extra=director.Log.Moves.Count(m=>m.kind!="Ship"&&m.outcome=="completed");
            results.text=$"{director.Shipped} SHIPPED / {director.MoveIndex} MOVES / {extra} EXTRA\nTOTAL {director.Log.Seconds:F1}s / CONTACTS {director.Contacts}";
        }
        changeView.GetComponentInChildren<TextMeshProUGUI>().text="Monitor: "+director.view.MonitorName;
        if(director.Log!=null)results.text+="\n"+(director.Log.Error!=null?"EXPORT ERROR: "+director.Log.Error:"PRACTICE DATA / SAVED");
        start.interactable=!director.Active;pause.interactable=director.Active;couple.interactable=director.CanOperate&&!director.Paused&&director.Stage!=PortStage.Park;
        if(safetyStatus!=null)safetyStatus.text=director.traffic.Status+"\n"+(director.safety.ClearanceText!=""?director.safety.ClearanceText:$"CONTACTS {director.safety.ContactCount} / NEAR MISSES {director.safety.NearMissCount}");
        if(layouts!=null)for(int i=0;i<layouts.Length;i++){layouts[i].interactable=!director.Active;layouts[i].GetComponent<Image>().color=i==(int)director.Layout?new Color(.1f,.45f,.36f):new Color(.09f,.25f,.30f);}
        folder.interactable=director.Log!=null&&!director.Active;
        pause.GetComponentInChildren<TextMeshProUGUI>().text=director.Paused?"Resume [Esc]":"Pause [Esc]";
        if(director.view.HeadsetRunning)
        {
            start.GetComponentInChildren<TextMeshProUGUI>().text="START OPERATION";
            pause.GetComponentInChildren<TextMeshProUGUI>().text=director.Paused?"Resume":"Pause";
            reset.GetComponentInChildren<TextMeshProUGUI>().text="Reset";
            couple.GetComponentInChildren<TextMeshProUGUI>().text="Lock / release [Right A]";
            changeView.GetComponentInChildren<TextMeshProUGUI>().text="Monitor: "+director.view.MonitorName;
            cabin.GetComponentInChildren<TextMeshProUGUI>().text="Recenter seated view";
        }
    }
    static void Place(Button button,float x,float y,float width)
    {
        var rect=(RectTransform)button.transform;
        rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,40);
    }
    static string StageLabel(PortStage value)
    {
        switch(value)
        {
            case PortStage.Idle:return "CHOOSE LAYOUT";
            case PortStage.TruckArrival:return "TRUCK ARRIVING";
            case PortStage.TruckParking:return "TRUCK PARKING";
            case PortStage.HandlerApproach:return "HANDLER APPROACH";
            case PortStage.HandlerLift:return "UNLOADING TRUCK";
            case PortStage.HandlerCarry:return "TRANSFER TO YARD";
            case PortStage.HandlerLower:return "SETTING DOWN CARGO";
            case PortStage.HandlerClear:return "HANDLER CLEARING";
            case PortStage.Reposition:return "NEXT PICKUP";
            case PortStage.Pickup:return "YOUR TURN / PICKUP";
            case PortStage.Transfer:return "TRANSFER / STACK";
            case PortStage.Verification:return "CHECK PLACEMENT";
            case PortStage.Park:return "FINAL PARKING";
            case PortStage.Complete:return "SHIPMENT COMPLETE";
            default:return "OPERATION STOPPED";
        }
    }
}
