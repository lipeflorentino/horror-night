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
    [SerializeField] private InventoryItemUI itemPrefab;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject inventoryPanel;

    private readonly List<InventoryItemUI> spawnedItems = new();
    private ICombatInventory boundInventory;

    public event Action<ItemSO, InventoryItemAction, InventoryItemLocation> OnInteractWithInventoryItem;

    // TODO: remove start after tests
    private void Start()
    {
    }

    private void OnEnable()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        Close();
        Refresh();
    }

    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    public void BindInventory(ICombatInventory playerInventory)
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
        inventoryPanel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        CloseAllInteractionPanels();
        inventoryPanel.SetActive(false);
    }

    private void SpawnInventoryItems()
    {
        Dictionary<ItemSO, int> grouped = new();
        foreach (ItemSO item in boundInventory.Items)
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
            
        InventoryItemUI inventoryItemView = Instantiate(itemPrefab, parent);
        ItemInfoPanelUI itemInfoPanel = FindObjectOfType<ItemInfoPanelUI>();
        inventoryItemView.SetItemInfoPanel(itemInfoPanel);
        
        inventoryItemView.Bind(item, count, location);
        inventoryItemView.ItemSelected += HandleItemSelected;
        inventoryItemView.OnInteractWithItem += HandleItemInteraction;
        inventoryItemView.ShowInteractionPanel(false);
        spawnedItems.Add(inventoryItemView);
    }

    private void HandleItemSelected(InventoryItemUI selectedView)
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            InventoryItemUI view = spawnedItems[i];
            if (view == null)
                continue;

            bool shouldOpen = view == selectedView;
            view.ShowInteractionPanel(shouldOpen);
        }
    }

    private void HandleItemInteraction(ItemSO item, InventoryItemAction action, InventoryItemLocation location)
    {
        OnInteractWithInventoryItem?.Invoke(item, action, location);
    }

    private void ClearSpawnedItems()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            InventoryItemUI itemView = spawnedItems[i];
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
