using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class TMPLinkTooltipHandler : MonoBehaviour, IPointerMoveHandler, IPointerExitHandler
{
    private TMP_Text textComponent;
    private int currentLinkIndex = -1;
    [SerializeField] private TooltipUI tooltipUI; 
    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    public void OnPointerMove(PointerEventData eventData)
{
    int linkIndex = TMP_TextUtilities.FindIntersectingLink(textComponent, eventData.position, eventData.pressEventCamera);

    if (linkIndex != -1 && linkIndex != currentLinkIndex)
    {
        currentLinkIndex = linkIndex;
        TMP_LinkInfo linkInfo = textComponent.textInfo.linkInfo[linkIndex];
        
        string linkID = linkInfo.GetLinkID(); 

    string tooltipText = linkInfo.GetLinkID().Replace('[', '<').Replace(']', '>'); 

    if (tooltipUI != null)
    {
        tooltipUI.Show(tooltipText, eventData.position);
    }
    }
    else if (linkIndex == -1 && currentLinkIndex != -1)
    {
        currentLinkIndex = -1;
        
        if (tooltipUI != null)
        {
            tooltipUI.Hide();
        }
    }
}

    public void OnPointerExit(PointerEventData eventData)
    {
        currentLinkIndex = -1;
    }
}