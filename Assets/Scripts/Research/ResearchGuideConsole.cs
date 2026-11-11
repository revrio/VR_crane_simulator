using UnityEngine;
using UnityEngine.UI;
public class ResearchGuideConsole : MonoBehaviour
{
    public ResearchGuide guide;
    public ResearchPanel panel;
    public Button repeat, voice, swapOrder, changeView, couple, desktopCouple, cabinView;
    private void Start()
    {
        foreach (var button in GetComponentsInChildren<Button>()) button.navigation = new Navigation { mode = Navigation.Mode.None };
        if (cabinView != null) cabinView.onClick.AddListener(panel.view.EnterCabin);
        repeat.onClick.AddListener(guide.Repeat);
        voice.onClick.AddListener(guide.ToggleVoice);
        swapOrder.onClick.AddListener(()=>{ if(!panel.session.IsActive) { panel.order.value=(panel.order.value+1)%ResearchPanel.Orders.Length;panel.order.RefreshShownValue(); } });
        changeView.onClick.AddListener(panel.view.ToggleDesktopView);
        couple.onClick.AddListener(()=>panel.session.hook.ToggleAttachment());
        if(desktopCouple!=null)desktopCouple.onClick.AddListener(()=>panel.session.hook.ToggleAttachment());
    }
    private void Update()
    {
        swapOrder.interactable=!panel.session.IsActive;
        voice.GetComponentInChildren<TMPro.TextMeshProUGUI>().text=guide.VoiceMuted?"Voice OFF [M]":"Voice ON [M]";
    }
}
