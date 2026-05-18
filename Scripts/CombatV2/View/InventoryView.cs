using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryView : MonoBehaviour
{
    [SerializeField] private Transform weaponSlotsRoot;
    [SerializeField] private Transform relicSlotsRoot;
    [SerializeField] private Transform consumableSlotsRoot;
    [SerializeField] private InventoryItemView itemPrefab;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject interactionPanel;
    [SerializeField] private TMP_Text interactionItemNameText;
    [SerializeField] private Button useButton;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button unequipButton;
    [SerializeField] private Button discardButton;

    private readonly List<GameObject> spawnedItems = new();
    private ItemSO selectedItem;
    private PlayerInventory boundInventory;

    public event Action<ItemSO> UseRequested;
    public event Action<ItemSO> EquipRequested;
    public event Action<ItemSO> UnequipRequested;
    public event Action<ItemSO> DiscardRequested;

    private void OnEnable()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (useButton != null)
            useButton.onClick.AddListener(HandleUseClick);

        if (equipButton != null)
            equipButton.onClick.AddListener(HandleEquipClick);

        if (unequipButton != null)
            unequipButton.onClick.AddListener(HandleUnequipClick);

        if (discardButton != null)
            discardButton.onClick.AddListener(HandleDiscardClick);

        HideInteractionPanel();
        Refresh();
    }

    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (useButton != null)
            useButton.onClick.RemoveListener(HandleUseClick);

        if (equipButton != null)
            equipButton.onClick.RemoveListener(HandleEquipClick);

        if (unequipButton != null)
            unequipButton.onClick.RemoveListener(HandleUnequipClick);

        if (discardButton != null)
            discardButton.onClick.RemoveListener(HandleDiscardClick);
    }

    public void BindInventory(PlayerInventory playerInventory)
    {
        boundInventory = playerInventory;
    }

    public void Refresh()
    {
        ClearSpawnedItems();

        Dictionary<ItemSO, int> grouped = new();
       if (boundInventory == null)
            return;

        foreach (ItemSO item in boundInventory.items)
        {
            if (item == null)
                continue;

            if (!grouped.ContainsKey(item))
                grouped[item] = 0;

            grouped[item]++;
        }

        foreach (KeyValuePair<ItemSO, int> pair in grouped)
        {
            Transform parent = GetParentByType(pair.Key.type);
            if (parent == null)
                continue;

            InventoryItemView view = Instantiate(itemPrefab, parent);
            view.Bind(pair.Key, pair.Value);
            view.InteractRequested += OpenInteractionForItem;
            spawnedItems.Add(view.gameObject);
        }
    }

    public void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    public void Open()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        HideInteractionPanel();
        gameObject.SetActive(false);
    }

    private Transform GetParentByType(ItemType itemType)
    {
        return itemType switch
        {
            ItemType.Weapon => weaponSlotsRoot,
            ItemType.Relic => relicSlotsRoot,
            _ => consumableSlotsRoot,
        };
    }

    private void ClearSpawnedItems()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            InventoryItemView itemView = spawnedItems[i] != null ? spawnedItems[i].GetComponent<InventoryItemView>() : null;
            if (itemView != null)
                itemView.InteractRequested -= OpenInteractionForItem;

            Destroy(spawnedItems[i]);
        }

        spawnedItems.Clear();
    }

    private void OpenInteractionForItem(ItemSO item)
    {
        selectedItem = item;
        if (interactionItemNameText != null)
            interactionItemNameText.text = item != null ? item.itemName : string.Empty;

        if (item == null)
        {
            HideInteractionPanel();
            return;
        }

        bool isConsumable = item.type == ItemType.Consumable;
        bool isEquipable = item.type == ItemType.Weapon || item.type == ItemType.Relic;
        bool isEquipped = IsItemEquipped(item);

        if (useButton != null)
            useButton.gameObject.SetActive(isConsumable);

        if (equipButton != null)
            equipButton.gameObject.SetActive(isEquipable && !isEquipped);

        if (unequipButton != null)
            unequipButton.gameObject.SetActive(isEquipable && isEquipped);

        if (discardButton != null)
            discardButton.gameObject.SetActive(item.type != ItemType.Broken || !isEquipped);

        if (interactionPanel != null)
            interactionPanel.SetActive(true);
    }

    private bool IsItemEquipped(ItemSO item)
    {
        if (boundInventory == null || item == null)
            return false;

        IReadOnlyList<EquippedItemInstance> equippedCollection = item.type == ItemType.Weapon
            ? boundInventory.GetEquippedWeapons()
            : boundInventory.GetEquippedRelics();

        for (int i = 0; i < equippedCollection.Count; i++)
        {
            if (equippedCollection[i].SourceItem == item)
                return true;
        }

        return false;
    }

    private void HideInteractionPanel()
    {
        selectedItem = null;
        if (interactionPanel != null)
            interactionPanel.SetActive(false);
    }

    private void HandleUseClick() => UseRequested?.Invoke(selectedItem);
    private void HandleEquipClick() => EquipRequested?.Invoke(selectedItem);
    private void HandleUnequipClick() => UnequipRequested?.Invoke(selectedItem);
    private void HandleDiscardClick() => DiscardRequested?.Invoke(selectedItem);
}
