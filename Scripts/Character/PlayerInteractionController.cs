using UnityEngine;

public class PlayerInteractionController2D : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 1.2f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Header("UI")]
    [SerializeField] private InteractionPromptUI interactionPromptUI;

    [Header("Optional")]
    [SerializeField] private Transform interactionPoint;
    // Caso não atribua, usará o próprio transform

    private IInteractable currentInteractable;

    private void Update()
    {
        DetectInteractable();

        if (Input.GetKeyDown(interactionKey))
        {
            TryInteract();
        }
    }

    private void OnDisable()
    {
        ClearCurrentInteractable();
    }

    private void DetectInteractable()
    {
        Vector2 center = interactionPoint != null
            ? interactionPoint.position
            : transform.position;

        Collider2D hit = Physics2D.OverlapCircle(center, interactionRadius, interactableLayer);
        IInteractable foundInteractable = hit?.GetComponent<IInteractable>();
        
        if (ReferenceEquals(foundInteractable, currentInteractable))
        {
            if (currentInteractable != null)
            {
                interactionPromptUI = hit.GetComponent<InteractionPromptUI>();
                ShowPromptForCurrentInteractable();
            }

            return;
        }

        if (currentInteractable != null)
        {
            currentInteractable.OnUnfocus();
        }

        currentInteractable = foundInteractable;

        if (currentInteractable != null)
        {
            currentInteractable.OnFocus();
            ShowPromptForCurrentInteractable();
        }
        else
        {
            interactionPromptUI?.Hide();
        }
    }

    private void TryInteract()
    {
        if (currentInteractable == null)
        {
            return;
        }

        currentInteractable.Interact();
        ShowPromptForCurrentInteractable();
    }

    private void ShowPromptForCurrentInteractable()
    {
        if (interactionPromptUI == null || currentInteractable == null)
        {
            return;
        }

        string interactionText = currentInteractable.GetInteractionText();

        if (string.IsNullOrWhiteSpace(interactionText))
        {
            interactionPromptUI.Hide();
            return;
        }

        interactionPromptUI.Show(interactionText);
    }

    private void ClearCurrentInteractable()
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnUnfocus();
            currentInteractable = null;
        }

        interactionPromptUI?.Hide();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Vector2 center = interactionPoint != null
            ? interactionPoint.position
            : transform.position;

        Gizmos.DrawWireSphere(center, interactionRadius);
    }
}
