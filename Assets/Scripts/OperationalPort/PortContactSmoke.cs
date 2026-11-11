#if UNITY_EDITOR
using System;
using UnityEngine;

// Regression: contact feedback must never freeze the operator's crane or cargo.
public class PortContactSmoke : MonoBehaviour
{
    PortDirector d;
    double started;
    bool contacted;
    void Start()
    {
        d=FindFirstObjectByType<PortDirector>();
        FindFirstObjectByType<PortInput>().disabledForTest=true;
        d.outputOverride="/tmp/port-contact-flow";d.automated=true;d.VoiceMuted=true;
        d.Begin();started=Time.realtimeSinceStartupAsDouble;
    }
    void Update()
    {
        try
        {
            if(Time.realtimeSinceStartupAsDouble-started>90)throw new Exception("Contact regression timed out");
            if(!d.CanOperate)return;
            Vector3 target=d.CargoTop; // Deliberately lower the spreader into a container.
            float ropeTarget=d.config.trolleyHeight-target.y+1;
            d.crane.Command=new Vector3(Mathf.Clamp(target.x-d.crane.GantryX,-1,1),Mathf.Clamp(target.z-d.crane.TrolleyZ,-1,1),Mathf.Clamp(ropeTarget-d.crane.rope.maxLength,-1,1));
            if(d.safety.ContactCount>0)contacted=true;
            if(!contacted)return;
            if(d.crane.hookBody.isKinematic)throw new Exception("Contact froze the spreader");
            d.TogglePause();
            if(!d.Paused)throw new Exception("Manual pause failed");
            d.TogglePause();
            if(d.Paused)throw new Exception("Manual resume failed");
            d.ResetOperation();
            if(d.safety.ContactCount!=0)throw new Exception("Contact metrics did not reset");
            Debug.Log("OPERATIONAL_CONTACT_FLOW_PASSED: impacts do not freeze the crane; manual pause/reset work.");
            UnityEditor.EditorApplication.Exit(0);enabled=false;
        }
        catch(Exception e){Debug.LogException(e);UnityEditor.EditorApplication.Exit(1);enabled=false;}
    }
}
#endif
