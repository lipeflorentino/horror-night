using UnityEngine;
using UnityEngine.Events;

public class InteractableObject2D : MonoBehaviour
{
    [Header("Prompt")]
    [SerializeField] private string interactionText = "[E] Interagir";

    [Header("Actions")]
    [SerializeField] private UnityEvent onInteract;

    public void Interact()
    {
        onInteract?.Invoke();
    }

    public void OnFocus()
    {
        // Espaço para feedback visual (outline, highlight etc.)
    }

    public void OnUnfocus()
    {
        // Espaço para remover feedback visual
    }

    public string GetInteractionText()
    {
        return interactionText;
    }
}
