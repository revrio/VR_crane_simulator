using UnityEngine;
public class PortMaya : MonoBehaviour
{
    public PortDirector director;
    public Transform head,arm,mouth;
    public string[] voiceKeys;
    public AudioClip[] voiceClips;
    string lastKey;
    Quaternion armRest;
    float next;
    int lastAccepted;
    float acceptedUntil;
    AudioClip acceptedClip;
    void Start()
    {
        armRest=arm.localRotation;
        if(!director.automated)director.VoiceMuted=false;
        director.voice.enabled=true;director.voice.mute=false;
        director.voice.spatialBlend=0;director.voice.volume=.85f;
        acceptedClip=Resources.Load<AudioClip>("PortVoice/Common/accepted")??Resources.Load<AudioClip>("PortDeliveryAccepted");
    }
    void LateUpdate()
    {
        string key=director.VoiceKey;
        if(director.AcceptedDeliveries<lastAccepted){lastAccepted=director.AcceptedDeliveries;acceptedUntil=0;}
        if(director.AcceptedDeliveries>lastAccepted)
        {
            lastAccepted=director.AcceptedDeliveries;
            director.voice.Stop();
            if(!director.VoiceMuted&&acceptedClip!=null)
            {
                director.voice.clip=acceptedClip;director.voice.Play();
                acceptedUntil=Time.unscaledTime+acceptedClip.length+.15f;
            }
            lastKey=null;next=acceptedUntil+.2f;
        }
        if(Time.unscaledTime<acceptedUntil&&!director.Paused)return;
        if(key!=lastKey){lastKey=key;next=Time.unscaledTime+.65f;if(director.voice.isPlaying)director.voice.Stop();}
        if(!director.VoiceMuted&&!string.IsNullOrEmpty(key)&&Time.unscaledTime>=next)
        {
            int i=System.Array.IndexOf(voiceKeys,key);
            string mode=PortView.SimulatedVR?"Simulator":director.view.HeadsetRunning?"Quest":"Desktop";
            var clip=Resources.Load<AudioClip>("PortVoice/"+mode+"/"+key)??Resources.Load<AudioClip>("PortVoice/Common/"+key);
            if(clip==null)clip=i>=0&&i<voiceClips.Length?voiceClips[i]:Resources.Load<AudioClip>("PortMaya_"+key);
            if(clip!=null){director.voice.clip=clip;director.voice.Play();}
            next=director.Stage==PortStage.Idle?float.PositiveInfinity:Time.unscaledTime+35;
        }
        if(director.Paused)return;
        var viewer=director.view.HeadsetRunning?director.view.origin.Camera.transform:director.view.eye;
        Vector3 local=transform.InverseTransformPoint(viewer.position)-head.localPosition;
        var look=Quaternion.LookRotation(-local.normalized).eulerAngles;
        head.localRotation=Quaternion.Slerp(head.localRotation,Quaternion.Euler(Mathf.Clamp(Mathf.DeltaAngle(0,look.x),-15,20),Mathf.Clamp(Mathf.DeltaAngle(0,look.y),-40,40),0),.07f);
        arm.localRotation=Quaternion.Slerp(arm.localRotation,director.Stage==PortStage.Idle?Quaternion.Euler(0,0,-135+Mathf.Sin(Time.time*4)*12):armRest,.08f);
        mouth.localScale=new Vector3(.10f,director.voice.isPlaying?.025f+Mathf.Sin(Time.time*17)*.01f:.012f,.018f);
    }
}
