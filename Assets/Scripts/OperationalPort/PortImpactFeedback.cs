using UnityEngine;
using UnityEngine.XR;

public class PortImpactFeedback : MonoBehaviour
{
    public PortDirector director;
    public ParticleSystem dust;
    public AudioSource contactAudio;
    public AudioClip scrape,strongImpact;
    public Renderer beacon;
    public LineRenderer ring;
    public int EffectCount {get;private set;}
    float ringTime;
    MaterialPropertyBlock color;
    public void ResetEffects(){EffectCount=0;ringTime=0;ring.enabled=false;dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
    public void Impact(Vector3 point,bool strong)
    {
        EffectCount++;ringTime=.7f;ring.enabled=true;ring.positionCount=25;
        for(int i=0;i<25;i++){float a=i*Mathf.PI/12;ring.SetPosition(i,point+new Vector3(Mathf.Cos(a)*.4f,.05f,Mathf.Sin(a)*.4f));}
        if(!dust.isPlaying)dust.Play();
        var emit=new ParticleSystem.EmitParams{position=point,startColor=strong?new Color(.55f,.52f,.44f):new Color(.65f,.65f,.62f)};dust.Emit(emit,strong?14:6);
        contactAudio.transform.position=point;contactAudio.pitch=strong?.75f:1.1f;contactAudio.PlayOneShot(strong?strongImpact:scrape,strong?.65f:.4f);
        var hand=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if(hand.TryGetHapticCapabilities(out var capabilities)&&capabilities.supportsImpulse)hand.SendHapticImpulse(0,strong?.6f:.25f,strong?.2f:.08f);
    }
    void Update()
    {
        if(director.Paused){contactAudio.Pause();return;}
        contactAudio.UnPause();
        if(ringTime>0){ringTime-=Time.deltaTime;ring.enabled=ringTime>0;}
        if(color==null)color=new MaterialPropertyBlock();color.SetColor("_BaseColor",new Color(.15f,.3f,.25f));beacon.SetPropertyBlock(color);
    }
}
