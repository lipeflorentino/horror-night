using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    public List<ItemSO> items = new();

    public void AddItem(ItemSO item)
    {
        items.Add(item);
        Debug.Log("Item adicionado: " + item.itemName);
    }

    public List<ItemSO> CreateSnapshot()
    {
        return new List<ItemSO>(items);
    }

    public void RestoreSnapshot(List<ItemSO> snapshot)
    {
        items = snapshot != null ? new List<ItemSO>(snapshot) : new List<ItemSO>();
    }
}