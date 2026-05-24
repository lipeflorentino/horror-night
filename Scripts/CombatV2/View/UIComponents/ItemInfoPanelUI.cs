using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[RequireComponent(typeof(RectTransform))]
public class ItemInfoPanelUI : MonoBehaviour
{
    public static ItemInfoPanelUI Instance { get; private set; }

    [Header("Item Info")]
    [SerializeField] private GameObject itemInfoPanel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text statBonusText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text typeText;
    
    [Header("Interaction Buttons")]
    [SerializeField] private Button useButton;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button unequipButton;
    [SerializeField] private Button dischardButton;
    [SerializeField] private Button closeButton;

    private RectTransform rectTransform;
    public event Action<InventoryItemAction> OnRaiseInteraction;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        rectTransform = GetComponent<RectTransform>();

        if (useButton != null) useButton.onClick.AddListener(() => RaiseInteraction(InventoryItemAction.Use));
        if (equipButton != null) equipButton.onClick.AddListener(() => RaiseInteraction(InventoryItemAction.Equip));
        if (unequipButton != null) unequipButton.onClick.AddListener(() => RaiseInteraction(InventoryItemAction.Unequip));
        if (dischardButton != null) dischardButton.onClick.AddListener(() => RaiseInteraction(InventoryItemAction.Discard));
        if (closeButton != null) closeButton.onClick.AddListener(() => RaiseInteraction(InventoryItemAction.Close));
    }

    private void OnDestroy()
    {
        if (useButton != null) useButton.onClick.RemoveAllListeners();
        if (equipButton != null) equipButton.onClick.RemoveAllListeners();
        if (unequipButton != null) unequipButton.onClick.RemoveAllListeners();
        if (dischardButton != null) dischardButton.onClick.RemoveAllListeners();
        if (closeButton != null) closeButton.onClick.RemoveAllListeners();
    }

    public void ConfigureActions(ItemSO boundItem, InventoryItemLocation location)
    {
        bool isEquipable = boundItem != null && (boundItem.type == ItemType.Weapon || boundItem.type == ItemType.Relic);
        bool isConsumable = boundItem != null && boundItem.type == ItemType.Consumable;
        bool inItemSlots = location == InventoryItemLocation.ItemSlots;

        if (useButton != null) useButton.gameObject.SetActive(inItemSlots && isConsumable);
        if (equipButton != null) equipButton.gameObject.SetActive(inItemSlots && isEquipable);
        if (unequipButton != null) unequipButton.gameObject.SetActive(!inItemSlots && isEquipable);
        if (dischardButton != null) dischardButton.gameObject.SetActive(boundItem != null);
        if (closeButton != null) closeButton.gameObject.SetActive(boundItem != null);
    }
    
    public void SetItemInfo(ItemSO item, int count, string statBonus, string effect, string description, string type, InventoryItemLocation location)
    {
        nameText.text = item.itemName;
        countText.text = $"Count: {count}";
        statBonusText.text = $"Stat Bonus: {statBonus}";
        effectText.text = $"Effect: {effect}";
        descriptionText.text = description;
        typeText.text = $"Type: {type}";
        ConfigureActions(item, location);
    }

    public void ShowTooltip(Vector3 position)
    {
        itemInfoPanel.SetActive(true);
        rectTransform.position = position;
    }

    public void HideTooltip()
    {
        itemInfoPanel.SetActive(false);
    }

    public void RaiseInteraction(InventoryItemAction action)
    {
        OnRaiseInteraction?.Invoke(action);
    }
}