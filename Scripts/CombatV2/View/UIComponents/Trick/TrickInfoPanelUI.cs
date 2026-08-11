using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class TrickInfoPanelUI : MonoBehaviour
{
    public static TrickInfoPanelUI Instance { get; private set; }

    [Header("Trick Info")]
    [SerializeField] private GameObject trickInfoPanel;
    [SerializeField] private Image trickThumbnailImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] private TMP_Text durationText;
    [SerializeField] private TrickRarityUI rarityIcon;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text tagsText;
    [Header("Momentum Cost")]
    [SerializeField] private TMP_Text momentumCostText;
    [SerializeField] private Image momentumCostHighlight;

    [Header("Requirements")]
    [SerializeField] private Transform requirementsContainer;
    [SerializeField] private TrickRequirementUI requirementPrefab;

    [Header("Interaction Buttons")]
    [SerializeField] private Button castButton;
    [SerializeField] private Button dischardButton;

    // Pool of requirement rows reused across SetTrickInfo() calls to avoid Instantiate/Destroy churn.
    private readonly List<TrickRequirementUI> _requirementPool = new();
    public event Action<TrickInventoryAction> OnRaiseInteraction;
    private PerkService boundPerkService;
    private Tooltipable castTooltip;
    private Tooltipable dischardTooltip;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);

        if (castButton != null) castButton.onClick.AddListener(() => RaiseInteraction(TrickInventoryAction.Cast));
        if (dischardButton != null) dischardButton.onClick.AddListener(() => RaiseInteraction(TrickInventoryAction.Dischard));

        castTooltip = castButton.GetOrAddComponent<Tooltipable>();
        dischardTooltip = dischardButton.GetOrAddComponent<Tooltipable>();

        HidePanel();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (castButton != null) castButton.onClick.RemoveAllListeners();
        if (dischardButton != null) dischardButton.onClick.RemoveAllListeners();
    }

    public void BindPerkService(PerkService perkService)
    {
        boundPerkService = perkService;
    }

    public void SetTrickInfo(TrickSO trick, TrickRuntimeInstance runtimeInstance, Battler owner, TrickInventoryItemLocation location)
    {
        if (trick == null)
            return;

        Battler ownerToUse = runtimeInstance?.Owner ?? owner;
        if (ownerToUse == null)
            return;

        bool hasEnoughMomentum = ownerToUse.Momentum >= trick.MomentumCost;

        if (trickThumbnailImage != null) trickThumbnailImage.sprite = trick.Thumbnail;
        if (nameText != null) nameText.text = trick.DisplayName;
        if (cooldownText != null) cooldownText.text = trick.CooldownTurns > 0 ? $"{trick.CooldownTurns} " + "Turnos" : "-";
        if (durationText != null) durationText.text = FormatDuration(trick);
        if (rarityIcon != null) rarityIcon.Setup($"{trick.Rarity}");
        if (descriptionText != null) descriptionText.text = trick.Description;
        if (tagsText != null) tagsText.text = trick.Tags != null && trick.Tags.Count > 0 ? $"{string.Join(", ", trick.Tags.ToArray())}" : "-";
        if (momentumCostText != null) momentumCostText.text = trick.MomentumCost > 0 ? $"{trick.MomentumCost}" : "-";
        if (momentumCostHighlight != null) momentumCostHighlight.color = Colorization.HexToColor(Colorization.BadColorHex);
        
        momentumCostHighlight.gameObject.SetActive(hasEnoughMomentum == false);

        PopulateRequirements(trick.Requirements, location, ownerToUse);

        ConfigureActions(trick, runtimeInstance, location, ownerToUse);
    }

    public void ShowPanel()
    {
        if (trickInfoPanel != null)
            trickInfoPanel.SetActive(true);
    }

    public void HidePanel()
    {
        if (trickInfoPanel != null)
            trickInfoPanel.SetActive(false);
    }

    public void RaiseInteraction(TrickInventoryAction action)
    {
        OnRaiseInteraction?.Invoke(action);
    }

    private void ConfigureActions(TrickSO trick, TrickRuntimeInstance runtimeInstance, TrickInventoryItemLocation location, Battler owner)
    {
        bool hasTrick = trick != null;
        bool canCast = hasTrick && location.Location == TrickInventoryLocation.LearnedTricks && runtimeInstance == null && trick.CanCast(owner, boundPerkService);
        bool canDischard = hasTrick && location.Location != TrickInventoryLocation.IdentitySlot;

        if (runtimeInstance != null && runtimeInstance.IsActive() && runtimeInstance?.IsCoolingDown == false)
        {
            canDischard = false; // Cannot dischard an already active trick.
        }

        if (castButton != null) castButton.interactable = canCast;
        if (dischardButton != null) dischardButton.interactable = canDischard;

        ConfigureTooltip(castTooltip, canCast ? "Cast this trick." : "Cannot cast this trick.", !canCast);
        ConfigureTooltip(dischardTooltip, canDischard ? "Dischard this trick." : "Cannot dischard this trick.", !canDischard);
    }

    private void ConfigureTooltip(Tooltipable tooltip, string text, bool isDisabled)
    {
        if (tooltip != null)
        {
            tooltip.SetTooltipText(text);
            tooltip.DisableTooltip(isDisabled);
            tooltip.SetTooltipColor(isDisabled ? TooltipUI.TooltipColor.Red : TooltipUI.TooltipColor.Default);
        }
    }

    private void PopulateRequirements(TrickRequirements requirements, TrickInventoryItemLocation location, Battler owner)
    {
        if (requirementsContainer == null || requirementPrefab == null)
            return;

        if (requirements == null || location.Location == TrickInventoryLocation.IdentitySlot)
        {
            DeactivateRequirementsFrom(0);
            return;
        }

        int index = 0;
        foreach (var (statKey, value) in requirements.GetActiveRequirements())
        {
            int availableValue = owner != null ? owner.GetStatValue(statKey, boundPerkService) : 0;
            TrickRequirementUI requirementUI = GetOrCreatePooledRequirement(index);
            requirementUI.Setup(statKey, value, availableValue);
            requirementUI.gameObject.SetActive(true);
            index++;
        }

        DeactivateRequirementsFrom(index);
    }

    private TrickRequirementUI GetOrCreatePooledRequirement(int index)
    {
        if (index < _requirementPool.Count)
            return _requirementPool[index];

        TrickRequirementUI requirementUI = Instantiate(requirementPrefab, requirementsContainer);
        _requirementPool.Add(requirementUI);
        return requirementUI;
    }

    private void DeactivateRequirementsFrom(int index)
    {
        for (int i = index; i < _requirementPool.Count; i++)
            _requirementPool[i].gameObject.SetActive(false);
    }

    private static string FormatDuration(TrickSO trick)
    {
        if (trick == null)
            return "-";

        if (trick.DurationTurns < 0)
            return "Permanent";

        return $"{trick.DurationTurns} " + "Turnos";
    }
}