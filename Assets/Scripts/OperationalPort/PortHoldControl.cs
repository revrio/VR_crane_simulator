using UnityEngine;
using UnityEngine.EventSystems;
public class PortHoldControl : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
{
    public PortInput input;public Vector3 command;
    public void OnPointerDown(PointerEventData e){if(!input.director.CanOperate||input.director.Paused)return;input.PointerCommand=command;held=true;}
    bool held;
    public void OnPointerUp(PointerEventData e)=>Clear();public void OnPointerExit(PointerEventData e)=>Clear();
    void OnDisable()=>Clear();
    void Clear(){if(held&&input!=null&&input.PointerCommand==command)input.PointerCommand=Vector3.zero;held=false;}
}
