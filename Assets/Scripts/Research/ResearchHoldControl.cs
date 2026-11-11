using UnityEngine;
using UnityEngine.EventSystems;
public class ResearchHoldControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public ResearchCraneInput input;
    public Vector3 command;
    public void OnPointerDown(PointerEventData e) { input.PointerCommand = command; }
    public void OnPointerUp(PointerEventData e) { input.PointerCommand = Vector3.zero; }
    public void OnPointerExit(PointerEventData e) { input.PointerCommand = Vector3.zero; }
    private void OnDisable() { if (input != null) input.PointerCommand = Vector3.zero; }
}
