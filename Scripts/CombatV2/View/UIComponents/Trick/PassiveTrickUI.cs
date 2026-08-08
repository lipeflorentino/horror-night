using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PassiveTrickUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Componentes Core")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text turnCountText;    
    [Header("Cooldown")]
    [SerializeField] private GameObject cooldownOverlay;
    [SerializeField] private TMP_Text cooldownTurnsText;
    private TrickSO trickDefinition;
    private TrickRuntimeInstance runtimeInstance;
    private TrickTooltip tooltip;
    [SerializeField] private GameObject tooltipPrefab;
    public TrickRuntimeInstance RuntimeInstance => runtimeInstance;

    public void Setup(TrickSO definition, TrickRuntimeInstance instance = null)
    {
        if (instance == null)
            return;
        
        trickDefinition = definition;
        runtimeInstance = instance;

       if (icon != null && definition.Icon != null)
            icon.sprite = definition.Icon;

        if (turnCountText != null)
            turnCountText.text = instance != null ? instance.RemainingTurns.ToString() : string.Empty;

        UpdateUI();
    }

    /// <summary>
    /// Sincroniza visualmente o overlay de cooldown e o texto de turnos restantes
    /// </summary>
    public void UpdateUI()
    {
        if (trickDefinition == null || runtimeInstance == null) return;

        // Feedback visual de Cooldown
        bool isActuallyCoolingDown = runtimeInstance.IsCoolingDown && runtimeInstance.WasExpired;
        
        if (cooldownOverlay != null) 
            cooldownOverlay.SetActive(isActuallyCoolingDown);
            
        if (cooldownTurnsText != null)
        {
            cooldownTurnsText.text = runtimeInstance.CooldownTurnsRemaining.ToString();
        }
    }

    private void OnDestroy()
    {
        HideTooltip();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void OnMouseEnter()
    {
        ShowTooltip();
    }

    private void OnMouseExit()
    {
        HideTooltip();
    }

    private void ShowTooltip()
    {
        if (tooltip != null || tooltipPrefab == null || runtimeInstance == null)
            return;

        tooltip = Instantiate(tooltipPrefab, transform).GetComponent<TrickTooltip>();
        if (tooltip != null)
            tooltip.Show(runtimeInstance);
    }

    private void HideTooltip()
    {
        if (tooltip != null)
            Destroy(tooltip.gameObject);
    }
}