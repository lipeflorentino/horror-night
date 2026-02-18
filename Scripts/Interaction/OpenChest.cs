using UnityEngine;

public class Chest2D : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionText = "[E] Abrir baú";

    private bool isOpen;

    public void Interact()
    {
        if (isOpen)
        {
            return;
        }

        OpenChest();
    }

    public void OnFocus()
    {
        // Espaço para highlight no sprite do baú
    }

    public void OnUnfocus()
    {
        // Espaço para remover highlight
    }

    public string GetInteractionText()
    {
        return isOpen ? string.Empty : interactionText;
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
