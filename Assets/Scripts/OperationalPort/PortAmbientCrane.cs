using UnityEngine;
public class PortAmbientCrane : MonoBehaviour
{
    public Transform trolley,spreader;
    public Transform[] cables;
    public float centerX,phase;
    float elapsed;
    public float MotionSeconds=>elapsed;
    public void ResetMotion(){elapsed=0;Pose();}
    public void Step(float dt){elapsed+=dt;Pose();}
    void Pose()
    {
        float t=elapsed*.13f+phase,z=3+Mathf.Sin(t)*13,y=11+Mathf.Sin(t*1.5f)*4;
        trolley.position=new Vector3(centerX,22,z);spreader.position=new Vector3(centerX,y,z);
        for(int i=0;i<cables.Length;i++)
        {Vector3 a=trolley.position+new Vector3(i<2?-.6f:.6f,0,i%2==0?-.45f:.45f),b=spreader.position+new Vector3(i<2?-.6f:.6f,0,i%2==0?-.45f:.45f);cables[i].position=(a+b)*.5f;cables[i].localScale=new Vector3(.035f,(a.y-b.y)*.5f,.035f);}
    }
}
