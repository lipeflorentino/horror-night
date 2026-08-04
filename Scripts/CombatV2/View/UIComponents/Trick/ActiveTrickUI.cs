using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActiveTrickUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Componentes Core")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI inputKeyText;
    [SerializeField] private TMP_Text turnCountText;    
    [SerializeField] private Image rarityBorder;
    [SerializeField] private Button releaseButton;
    [SerializeField] private GameObject chargesContainer;
    [SerializeField] private GameObject inputkeyContainer;

    [Header("Componentes Novos - Cargas e Cooldown")]
    [SerializeField] private TMP_Text chargesText;
    [SerializeField] private GameObject cooldownOverlay;
    [SerializeField] private TMP_Text cooldownTurnsText;

    private TrickSO trickDefinition;
    private TrickRuntimeInstance runtimeInstance;
    private TrickTooltip tooltip;
    [SerializeField] private GameObject tooltipPrefab;

    public TrickSO TrickDefinition => trickDefinition;
    public TrickRuntimeInstance RuntimeInstance => runtimeInstance;
    public event Action<TrickRuntimeInstance> OnReleaseClicked;

    public void Setup(TrickSO definition, string inputKeyOverride, TrickRuntimeInstance instance = null)
    {
        trickDefinition = definition;
        runtimeInstance = instance;

        if (icon != null && definition.Icon != null)
            icon.sprite = definition.Icon;

        if (turnCountText != null)
            turnCountText.text = instance != null ? instance.RemainingTurns.ToString() : string.Empty;

        if (inputKeyText != null)
            inputKeyText.text = inputKeyOverride;

        if (rarityBorder != null)
            rarityBorder.color = GetRarityColor(definition.Rarity);

        if (releaseButton != null)
        {
            releaseButton.onClick.RemoveListener(OnReleaseClickedHandler);
            releaseButton.onClick.AddListener(OnReleaseClickedHandler);
        }

        UpdateUI();
    }

    /// <summary>
    /// Sincroniza visualmente as cargas, o overlay de cooldown e o botão
    /// </summary>
    public void UpdateUI()
    {
        if (trickDefinition == null || runtimeInstance == null) return;

        // Feedback visual de Cooldown
        bool isActuallyCoolingDown = runtimeInstance.IsCoolingDown && runtimeInstance.WasExpired;

        // Feedback visual de Cargas
        if (trickDefinition.ActivationMode == TrickActivationMode.ActiveCharge)
        {
            if (chargesText != null)
            {
                chargesText.text = Mathf.FloorToInt(runtimeInstance.CurrentCharges).ToString();
            }
        }

        chargesContainer.SetActive(trickDefinition.ActivationMode == TrickActivationMode.ActiveCharge && !isActuallyCoolingDown);
        inputkeyContainer.SetActive(!isActuallyCoolingDown);
        
        if (cooldownOverlay != null) 
            cooldownOverlay.SetActive(isActuallyCoolingDown);
            
        if (cooldownTurnsText != null)
        {
            cooldownTurnsText.text = runtimeInstance.CooldownTurnsRemaining.ToString();
        }

        UpdateReleaseButtonState();
    }

    public void UpdateReleaseButtonState()
    {
        if (releaseButton == null) return;

        bool canRelease = trickDefinition != null && 
                          (trickDefinition.ActivationMode == TrickActivationMode.ActiveCharge || 
                           trickDefinition.ActivationMode == TrickActivationMode.Active) && 
                          runtimeInstance != null && 
                          runtimeInstance.IsReadyToTrigger;
                          
        releaseButton.gameObject.SetActive(canRelease);
    }

    private void OnReleaseClickedHandler()
    {
        if (runtimeInstance != null && runtimeInstance.IsReadyToTrigger)
            OnReleaseClicked?.Invoke(runtimeInstance);
    }

    private Color GetRarityColor(TrickRarity rarity)
    {
        return rarity switch
        {
            TrickRarity.Common => Color.gray,
            TrickRarity.Uncommon => Color.green,
            TrickRarity.Rare => Color.cyan,
            TrickRarity.Epic => new Color(1f, 0.5f, 1f), // Magenta
            TrickRarity.Legendary => Color.yellow,
            _ => Color.white
        };
    }

    private void OnDestroy()
    {
        HideTooltip();

        if (releaseButton != null)
            releaseButton.onClick.RemoveListener(OnReleaseClickedHandler);
    }

    public void PlayActivationAnimation()
    {
        // TODO: Implementar animação de entrada
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
        if (tooltip != null || tooltipPrefab == null || trickDefinition == null)
            return;

        tooltip = Instantiate(tooltipPrefab, transform).GetComponent<TrickTooltip>();
        if (tooltip != null)
            tooltip.Show(trickDefinition);
    }

    private void HideTooltip()
    {
        if (tooltip != null)
            Destroy(tooltip.gameObject);
    }
}
