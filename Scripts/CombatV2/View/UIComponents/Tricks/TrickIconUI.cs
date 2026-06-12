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
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image rarityBorder;

    private TrickSO trickDefinition;
    private TrickTooltip tooltip;
    [SerializeField] private GameObject tooltipPrefab;

    public TrickSO TrickDefinition => trickDefinition;
    public event Action<TrickSO> TrickClicked;

    /// <summary>
    /// Configura o ícone com dados do trick
    /// </summary>
    public void Setup(TrickSO definition)
    {
        trickDefinition = definition;

        if (icon != null && definition.Icon != null)
            icon.sprite = definition.Icon;

        if (levelText != null)
            levelText.text = $"Lvl {definition.Level}";

        if (costText != null)
            costText.text = FormatCost(definition);

        if (rarityBorder != null)
            rarityBorder.color = GetRarityColor(definition.Rarity);

        Button button = GetComponent<Button>();
        button.onClick.RemoveListener(OnClicked);
        button.onClick.AddListener(OnClicked);
    }

    /// <summary>
    /// Callback quando o ícone é clicado
    /// </summary>
    private void OnClicked()
    {
        if (trickDefinition == null)
            return;

        TrickClicked?.Invoke(trickDefinition);
        Debug.Log($"[TrickIconUI] Clicked: {trickDefinition.DisplayName}");
    }

    /// <summary>
    /// Formata o custo para exibição (ex: "2M 1B")
    /// </summary>
    private string FormatCost(TrickSO trick)
    {
        string cost = "";
        if (trick.MindCost > 0)
            cost += $"{trick.MindCost}M ";
        if (trick.BodyCost > 0)
            cost += $"{trick.BodyCost}B ";
        if (trick.HeartCost > 0)
            cost += $"{trick.HeartCost}H";

        return string.IsNullOrWhiteSpace(cost) ? "Free" : cost.Trim();
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
        Button button = GetComponent<Button>();
        if (button != null)
            button.onClick.RemoveListener(OnClicked);
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
