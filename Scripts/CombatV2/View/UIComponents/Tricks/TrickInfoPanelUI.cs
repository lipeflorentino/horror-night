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
    [SerializeField] private Image trickThumbnailImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] private TMP_Text durationText;
    [SerializeField] private TrickRarityUI rarityIcon;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text tagsText;
    [SerializeField] private TMP_Text momentumCostText;

    [Header("Requirements")]
    [SerializeField] private Transform requirementsContainer;
    [SerializeField] private TrickRequirementUI requirementPrefab;

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

        if (trickThumbnailImage != null) trickThumbnailImage.sprite = trick.Thumbnail;
        if (nameText != null) nameText.text = trick.DisplayName;
        if (levelText != null) levelText.text = $"{trick.Level}";
        if (cooldownText != null) cooldownText.text = trick.CooldownTurns > 0 ? $"{trick.CooldownTurns} " + "Turnos" : "-";
        if (durationText != null) durationText.text = FormatDuration(trick);
        if (rarityIcon != null) rarityIcon.Setup($"{trick.Rarity}");
        if (descriptionText != null) descriptionText.text = trick.Description;
        if (tagsText != null) tagsText.text = trick.Tags != null && trick.Tags.Count > 0 ? $"{string.Join(", ", trick.Tags.ToArray())}" : "-";
        if (momentumCostText != null) momentumCostText.text = trick.MomentumCost > 0 ? $"{trick.MomentumCost}" : "-";

        PopulateRequirements(trick.Requirements);

        ConfigureActions(trick, runtimeInstance, location);
    }

    public void ShowPanel()
    {
        if (trickInfoPanel != null)
            trickInfoPanel.SetActive(true);
    }

    public void ShowTooltip(Vector3 position)
    {
        ShowPanel();

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

    private void PopulateRequirements(TrickRequirements requirements)
    {
        if (requirementsContainer == null || requirementPrefab == null)
            return;

        for (int i = requirementsContainer.childCount - 1; i >= 0; i--)
            Destroy(requirementsContainer.GetChild(i).gameObject);

        if (requirements == null)
            return;

        foreach (var (statKey, value) in requirements.GetActiveRequirements())
        {
            TrickRequirementUI requirementUI = Instantiate(requirementPrefab, requirementsContainer);
            requirementUI.Setup(statKey, value);
        }
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