using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryView : MonoBehaviour
{
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private Transform weaponSlotsRoot;
    [SerializeField] private Transform relicSlotsRoot;
    [SerializeField] private Transform consumableSlotsRoot;
    [SerializeField] private InventoryItemView itemPrefab;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button closeButton;

    private readonly List<GameObject> spawnedItems = new();

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

    public void Refresh()
    {
        ClearSpawnedItems();

        Dictionary<ItemSO, int> grouped = new();
        foreach (ItemSO item in playerInventory.items)
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
            spawnedItems.Add(view.gameObject);
        }
    }

    public bool UseItem(ItemSO item)
    {
        bool used = playerInventory.UseItem(item);
        statusText.text = used ? $"Usou {item.itemName}" : $"Falha ao usar {item.itemName}";
        Refresh();
        return used;
    }

    public bool EquipItem(ItemSO item)
    {
        bool equipped = playerInventory.UseItem(item);
        statusText.text = equipped ? $"Equipou {item.itemName}" : $"Falha ao equipar {item.itemName}";
        Refresh();
        return equipped;
    }

    public bool UnEquipItem(ItemSO item)
    {
        bool unequipped = playerInventory.UnEquipItem(item);
        statusText.text = unequipped ? $"Desequipou {item.itemName}" : $"Falha ao desequipar {item.itemName}";
        Refresh();
        return unequipped;
    }

    public bool DeschardItem(ItemSO item)
    {
        bool discarded = playerInventory.DeschardItem(item);
        statusText.text = discarded ? $"Descartou {item.itemName}" : $"Falha ao descartar {item.itemName}";
        Refresh();
        return discarded;
    }

    public void Open()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
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
            Destroy(spawnedItems[i]);

        spawnedItems.Clear();
    }
}
