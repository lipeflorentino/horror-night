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
}