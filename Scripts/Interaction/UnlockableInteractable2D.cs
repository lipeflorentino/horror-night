using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class UnlockableInteractable2D : MonoBehaviour, IInteractable
{
    [Header("Lock Settings")]
    [SerializeField] private bool startsLocked = true;
    [SerializeField] private float unlockStrengthCost = 20f;

    [Header("Prompt")]
    [SerializeField] private string lockedPromptFormat = "[E] Desbloquear (Custo: {0} Força)";

    [Header("Target")]
    [SerializeField] private MonoBehaviour interactableTarget;

    [Header("Events")]
    [SerializeField] private UnityEvent onUnlockSuccess;
    [SerializeField] private UnityEvent onUnlockFailed;

    private PlayerStatusManager playerStatus;
    private IInteractable targetInteractable;
    private bool isUnlocked;

    private void Awake()
    {
        playerStatus = FindFirstObjectByType<PlayerStatusManager>();
        targetInteractable = interactableTarget as IInteractable;

        if (interactableTarget != null && targetInteractable == null)
        {
            Debug.LogError($"{name}: Interactable Target precisa implementar IInteractable.", this);
        }

        isUnlocked = !startsLocked;
    }

    public void Interact()
    {
        if (!isUnlocked)
        {
            TryUnlock();
            return;
        }

        targetInteractable?.Interact();
    }

    public void OnFocus()
    {
        if (isUnlocked)
        {
            targetInteractable?.OnFocus();
        }
    }

    public void OnUnfocus()
    {
        if (isUnlocked)
        {
            targetInteractable?.OnUnfocus();
        }
    }

    public string GetInteractionText()
    {
        if (!isUnlocked)
        {
            return string.Format(lockedPromptFormat, unlockStrengthCost);
        }

        return targetInteractable?.GetInteractionText() ?? string.Empty;
    }

    private void TryUnlock()
    {
        if (playerStatus == null)
        {
            Debug.LogWarning($"{name}: PlayerStatusManager não encontrado para desbloqueio.", this);
            onUnlockFailed?.Invoke();
            return;
        }

        bool unlocked = playerStatus.TrySpendStrength(unlockStrengthCost);

        if (!unlocked)
        {
            onUnlockFailed?.Invoke();
            return;
        }

        isUnlocked = true;
        onUnlockSuccess?.Invoke();
    }
}
