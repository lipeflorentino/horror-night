using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum InventoryItemAction
{
    Use,
    Equip,
    Unequip,
    Discard
}

public enum InventoryItemLocation
{
    ItemSlots,
    WeaponSlot,
    RelicSlot
}

public class InventoryItemView : MonoBehaviour
{
    [Header("Item Info")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text statBonusText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private Image iconImage;

    [Header("Interaction")]
    [SerializeField] private Button interactButton;
    [SerializeField] private GameObject interactionPanel;
    [SerializeField] private Button useButton;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button unequipButton;
    [SerializeField] private Button discardButton;

    private ItemSO boundItem;
    private int boundCount;
    private InventoryItemLocation location;

    public event Action<InventoryItemView> ItemSelected;
    public event Action<ItemSO, InventoryItemAction, InventoryItemLocation> OnInteractWithItem;

    private void Awake()
    {
        if (interactButton != null) interactButton.onClick.AddListener(HandleSelectClick);
        if (useButton != null) useButton.onClick.AddListener(() => RaiseInteraction(InventoryItemAction.Use));
        if (equipButton != null) equipButton.onClick.AddListener(() => RaiseInteraction(InventoryItemAction.Equip));
        if (unequipButton != null) unequipButton.onClick.AddListener(() => RaiseInteraction(InventoryItemAction.Unequip));
        if (discardButton != null) discardButton.onClick.AddListener(() => RaiseInteraction(InventoryItemAction.Discard));
        ShowInteractionPanel(false);
    }

    private void OnDestroy()
    {
        if (interactButton != null) interactButton.onClick.RemoveListener(HandleSelectClick);
        if (useButton != null) useButton.onClick.RemoveAllListeners();
        if (equipButton != null) equipButton.onClick.RemoveAllListeners();
        if (unequipButton != null) unequipButton.onClick.RemoveAllListeners();
        if (discardButton != null) discardButton.onClick.RemoveAllListeners();
    }

    public void Bind(ItemSO item, int count, InventoryItemLocation itemLocation)
    {
        if (item == null) return;

        boundItem = item;
        boundCount = count;
        location = itemLocation;

        if (nameText != null) nameText.text = item.itemName;
        if (countText != null) countText.text = count.ToString();
        if (statBonusText != null) statBonusText.text = item.statBonus;
        if (effectText != null) effectText.text = item.specialEffect;
        if (descriptionText != null) descriptionText.text = item.description;
        if (typeText != null) typeText.text = item.type.ToString();
        if (iconImage != null) iconImage.sprite = item.icon;
    }

    public void ShowInteractionPanel(bool visible)
    {
        if (interactionPanel != null) interactionPanel.SetActive(visible);
    }

    public void ConfigureActions()
    {
        bool isEquipable = boundItem != null && (boundItem.type == ItemType.Weapon || boundItem.type == ItemType.Relic);
        bool isConsumable = boundItem != null && boundItem.type == ItemType.Consumable;
        bool inItemSlots = location == InventoryItemLocation.ItemSlots;

        if (useButton != null) useButton.gameObject.SetActive(inItemSlots && isConsumable);
        if (equipButton != null) equipButton.gameObject.SetActive(inItemSlots && isEquipable);
        if (unequipButton != null) unequipButton.gameObject.SetActive(!inItemSlots && isEquipable);
        if (discardButton != null) discardButton.gameObject.SetActive(boundItem != null);
    }

    private void HandleSelectClick()
    {
        if (boundItem == null) return;
        ItemSelected?.Invoke(this);
    }

    private void RaiseInteraction(InventoryItemAction action)
    {
        if (boundItem == null) return;
        OnInteractWithItem?.Invoke(boundItem, action, location);

        if (action == InventoryItemAction.Use || action == InventoryItemAction.Equip || action == InventoryItemAction.Unequip || action == InventoryItemAction.Discard)
            ShowInteractionPanel(false);
    }
}
