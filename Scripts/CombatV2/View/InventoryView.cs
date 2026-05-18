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

    private readonly List<InventoryItemView> spawnedItems = new();
    private PlayerInventory boundInventory;

    public event Action<ItemSO, InventoryItemAction, InventoryItemLocation> OnInteractWithItem;

    private void OnEnable()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        Refresh();
    }

    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    public void BindInventory(PlayerInventory playerInventory)
    {
        boundInventory = playerInventory;
    }

    public void Refresh()
    {
        ClearSpawnedItems();

        if (boundInventory == null)
            return;

        SpawnInventoryItems();
        SpawnEquippedItems(boundInventory.GetEquippedWeapons(), weaponSlotsRoot, InventoryItemLocation.WeaponSlot);
        SpawnEquippedItems(boundInventory.GetEquippedRelics(), relicSlotsRoot, InventoryItemLocation.RelicSlot);
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
        CloseAllInteractionPanels();
        gameObject.SetActive(false);
    }

    private void SpawnInventoryItems()
    {
        Dictionary<ItemSO, int> grouped = new();

        foreach (ItemSO item in boundInventory.items)
        {
            if (item == null)
                continue;

            if (!grouped.ContainsKey(item))
                grouped[item] = 0;

            grouped[item]++;
        }

        foreach (KeyValuePair<ItemSO, int> pair in grouped)
            SpawnItemView(pair.Key, pair.Value, consumableSlotsRoot, InventoryItemLocation.ItemSlots);
    }

    private void SpawnEquippedItems(IReadOnlyList<EquippedItemInstance> equippedItems, Transform parent, InventoryItemLocation location)
    {
        if (equippedItems == null)
            return;

        for (int i = 0; i < equippedItems.Count; i++)
        {
            ItemSO sourceItem = equippedItems[i]?.SourceItem;
            if (sourceItem == null)
                continue;

            SpawnItemView(sourceItem, 1, parent, location);
        }
    }

    private void SpawnItemView(ItemSO item, int count, Transform parent, InventoryItemLocation location)
    {
        if (itemPrefab == null || parent == null || item == null)
            return;

        InventoryItemView view = Instantiate(itemPrefab, parent);
        view.Bind(item, count, location);
        view.ItemSelected += HandleItemSelected;
        view.OnInteractWithItem += HandleItemInteraction;
        view.ShowInteractionPanel(false);
        spawnedItems.Add(view);
    }

    private void HandleItemSelected(InventoryItemView selectedView)
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            InventoryItemView view = spawnedItems[i];
            if (view == null)
                continue;

            bool shouldOpen = view == selectedView;
            view.ConfigureActions();
            view.ShowInteractionPanel(shouldOpen);
        }
    }

    private void HandleItemInteraction(ItemSO item, InventoryItemAction action, InventoryItemLocation location)
    {
        OnInteractWithItem?.Invoke(item, action, location);
    }

    private void ClearSpawnedItems()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            InventoryItemView itemView = spawnedItems[i];
            if (itemView != null)
            {
                itemView.ItemSelected -= HandleItemSelected;
                itemView.OnInteractWithItem -= HandleItemInteraction;
                Destroy(itemView.gameObject);
            }
        }

        spawnedItems.Clear();
    }

    private void CloseAllInteractionPanels()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
            if (spawnedItems[i] != null)
                spawnedItems[i].ShowInteractionPanel(false);
    }
}
