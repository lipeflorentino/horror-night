using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Exibe um ícone de Trick (card/ability) na UI com suporte a tooltip.
/// </summary>
[RequireComponent(typeof(Button))]
public class TrickIconUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI inputKeyText;
    [SerializeField] private Image rarityBorder;
    [SerializeField] private Button releaseButton;

    private TrickSO trickDefinition;
    private TrickRuntimeInstance runtimeInstance;
    private TrickTooltip tooltip;
    [SerializeField] private GameObject tooltipPrefab;

    public TrickSO TrickDefinition => trickDefinition;
    public TrickRuntimeInstance RuntimeInstance => runtimeInstance;
    public event Action<TrickSO> TrickClicked;
    public event Action<TrickRuntimeInstance> OnReleaseClicked;

    /// <summary>
    /// Configura o ícone com dados do trick
    /// </summary>
    public void Setup(TrickSO definition, string inputKeyOverride, TrickRuntimeInstance instance = null)
    {
        trickDefinition = definition;
        runtimeInstance = instance;

        if (icon != null && definition.Icon != null)
            icon.sprite = definition.Icon;

        if (inputKeyText != null)
        {
            inputKeyText.text = inputKeyOverride;
        }

        if (rarityBorder != null)
            rarityBorder.color = GetRarityColor(definition.Rarity);

        if (releaseButton != null)
        {
            releaseButton.onClick.RemoveListener(OnReleaseClickedHandler);
            releaseButton.onClick.AddListener(OnReleaseClickedHandler);
            UpdateReleaseButtonState();
        }
    }

    public void UpdateReleaseButtonState()
    {
        if (releaseButton == null) return;

        bool canRelease = trickDefinition != null && 
                          trickDefinition.ActivationMode == TrickActivationMode.ActiveCharge && 
                          runtimeInstance != null && 
                          runtimeInstance.IsReadyToTrigger;
                          
        releaseButton.gameObject.SetActive(canRelease);
    }

    private void OnReleaseClickedHandler()
    {
        if (runtimeInstance != null)
            OnReleaseClicked?.Invoke(runtimeInstance);
    }

    /// <summary>
    /// Retorna cor baseada na raridade
    /// </summary>
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
        if (releaseButton != null)
            releaseButton.onClick.RemoveListener(OnReleaseClickedHandler);
    }

    public void PlayEnterAnimation()
    {
        // TODO: Implementar animação de entrada
    }

    public void PlayExitAnimation()
    {
        // TODO: Implementar animação de saída
    }

    void OnMouseEnter()
    {
        if (tooltipPrefab != null && trickDefinition != null)
        {
            tooltip = Instantiate(tooltipPrefab, transform).GetComponent<TrickTooltip>();
            if (tooltip != null)
                tooltip.Show(trickDefinition);
        }
    }

    void OnMouseExit()
    {
        if (tooltip != null)
            Destroy(tooltip.gameObject);
    }
}
