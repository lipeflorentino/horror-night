using UnityEngine;

public class Chest2D : MonoBehaviour
{
    private bool isOpen = false;

    public void Interact()
    {
        if (isOpen) return;

        OpenChest();
    }

    private void OpenChest()
    {
        isOpen = true;
        Debug.Log("Baú aberto!");
        
        // Aqui você pode:
        // - Tocar animação
        // - Dar loot
        // - Trocar sprite
    }
}