using UnityEngine;

public class PlayerInteractionController2D : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 1.2f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Optional")]
    [SerializeField] private Transform interactionPoint; 
    // Caso não atribua, usará o próprio transform

    private IInteractable currentInteractable;

    private void Update()
    {
        DetectInteractable();

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
    }

    private void DetectInteractable()
    {
        Vector2 center = interactionPoint != null 
            ? interactionPoint.position 
            : transform.position;

        Collider2D hit = Physics2D.OverlapCircle(center, interactionRadius, interactableLayer);

        if (hit != null)
        {
            currentInteractable = hit.GetComponent<IInteractable>();
        }
        else
        {
            currentInteractable = null;
        }
    }

    private void TryInteract()
    {
        currentInteractable?.Interact();
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
