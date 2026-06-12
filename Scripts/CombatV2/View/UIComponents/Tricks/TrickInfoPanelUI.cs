using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class TrickInfoPanelUI : MonoBehaviour
{
    public static TrickInfoPanelUI Instance { get; private set; }

    [Header("Trick Info")]
    [SerializeField] private GameObject trickInfoPanel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] private TMP_Text durationText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text tagsText;
    [SerializeField] private TMP_Text slotText;

    [Header("Interaction Buttons")]
    [SerializeField] private Button castButton;
    [SerializeField] private Button dischardButton;
    [SerializeField] private Button closeButton;

    private RectTransform rectTransform;

    public event Action<TrickInventoryAction> OnRaiseInteraction;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);

        rectTransform = GetComponent<RectTransform>();

        if (castButton != null) castButton.onClick.AddListener(() => RaiseInteraction(TrickInventoryAction.Cast));
        if (dischardButton != null) dischardButton.onClick.AddListener(() => RaiseInteraction(TrickInventoryAction.Dischard));
        if (closeButton != null) closeButton.onClick.AddListener(() => RaiseInteraction(TrickInventoryAction.Close));

        HideTooltip();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (castButton != null) castButton.onClick.RemoveAllListeners();
        if (dischardButton != null) dischardButton.onClick.RemoveAllListeners();
        if (closeButton != null) closeButton.onClick.RemoveAllListeners();
    }

    public void SetTrickInfo(TrickSO trick, TrickRuntimeInstance runtimeInstance, TrickInventoryItemLocation location)
    {
        if (trick == null)
            return;

        if (nameText != null) nameText.text = trick.DisplayName;
        if (levelText != null) levelText.text = $"Level: {trick.Level}";
        if (costText != null) costText.text = $"Cost: {FormatCost(trick)}";
        if (cooldownText != null) cooldownText.text = runtimeInstance != null ? $"Cooldown: {runtimeInstance.CooldownTurnsRemaining}/{trick.CooldownTurns}" : $"Cooldown: {trick.CooldownTurns}";
        if (durationText != null) durationText.text = FormatDuration(trick, runtimeInstance);
        if (rarityText != null) rarityText.text = $"Rarity: {trick.Rarity}";
        if (descriptionText != null) descriptionText.text = trick.Description;
        if (tagsText != null) tagsText.text = trick.Tags != null && trick.Tags.Count > 0 ? $"Tags: {string.Join(", ", trick.Tags.ToArray())}" : "Tags: -";
        if (slotText != null) slotText.text = FormatSlot(location);

        ConfigureActions(trick, runtimeInstance, location);
    }

    public void ShowTooltip(Vector3 position)
    {
        if (trickInfoPanel != null)
            trickInfoPanel.SetActive(true);

        if (rectTransform != null)
            rectTransform.position = position;
    }

    public void HideTooltip()
    {
        if (trickInfoPanel != null)
            trickInfoPanel.SetActive(false);
    }

    public void RaiseInteraction(TrickInventoryAction action)
    {
        OnRaiseInteraction?.Invoke(action);
    }

    private void ConfigureActions(TrickSO trick, TrickRuntimeInstance runtimeInstance, TrickInventoryItemLocation location)
    {
        bool hasTrick = trick != null;
        bool canCast = hasTrick && location.Location == TrickInventoryLocation.LearnedTricks && runtimeInstance == null;
        bool canDischard = hasTrick && location.Location != TrickInventoryLocation.IdentitySlot;

        if (castButton != null) castButton.gameObject.SetActive(canCast);
        if (dischardButton != null) dischardButton.gameObject.SetActive(canDischard);
        if (closeButton != null) closeButton.gameObject.SetActive(hasTrick);
    }

    private static string FormatCost(TrickSO trick)
    {
        if (trick == null)
            return "-";

        string cost = string.Empty;
        if (trick.MindCost > 0) cost += $"{trick.MindCost} Mind ";
        if (trick.BodyCost > 0) cost += $"{trick.BodyCost} Body ";
        if (trick.HeartCost > 0) cost += $"{trick.HeartCost} Heart";
        return string.IsNullOrWhiteSpace(cost) ? "Free" : cost.Trim();
    }

    private static string FormatDuration(TrickSO trick, TrickRuntimeInstance runtimeInstance)
    {
        if (trick == null)
            return "Duration: -";

        if (trick.DurationTurns < 0)
            return "Duration: Permanent";

        return runtimeInstance != null ? $"Duration: {runtimeInstance.RemainingTurns}/{trick.DurationTurns}" : $"Duration: {trick.DurationTurns}";
    }

    private static string FormatSlot(TrickInventoryItemLocation location)
    {
        return location.Location switch
        {
            TrickInventoryLocation.IdentitySlot => $"Identity Slot {location.SlotIndex + 1}",
            TrickInventoryLocation.CastedSlot => $"Casted Slot {location.SlotIndex + 1}",
            _ => "Learned Trick"
        };
    }
}
