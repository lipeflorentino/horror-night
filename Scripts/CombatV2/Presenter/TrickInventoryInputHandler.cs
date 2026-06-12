using UnityEngine;

public class TrickInventoryInputHandler : MonoBehaviour
{
    [SerializeField] private TrickInventoryView trickInventoryView;
    [SerializeField] private MonoBehaviour playerTrickInventorySource;
    [SerializeField] private CombatManager Combat;

    private ITrickInventory playerTrickInventory;

    public void Init(CombatManager combatManager, ITrickInventory trickInventory)
    {
        Combat = combatManager;
        trickInventoryView = trickInventoryView != null ? trickInventoryView : FindObjectOfType<TrickInventoryView>();
        Logger.Log($"[TrickInventoryInputHandler] Inicializando com inventário de Tricks do jogador: {trickInventory}");
        playerTrickInventory = trickInventory;
        playerTrickInventorySource = trickInventory as MonoBehaviour;

        if (trickInventoryView != null)
        {
            trickInventoryView.BindInventory(playerTrickInventory);
            trickInventoryView.OnInteractWithInventoryTrick += HandleTrickInteraction;
            Logger.Log("[TrickInventoryInputHandler] Subscribed to trickInventoryView.OnInteractWithInventoryTrick");
        }
    }

    private void OnDestroy()
    {
        if (trickInventoryView == null)
            return;

        trickInventoryView.OnInteractWithInventoryTrick -= HandleTrickInteraction;
    }

    private void HandleTrickInteraction(TrickSO trick, TrickInventoryAction action, TrickInventoryItemLocation location)
    {
        switch (action)
        {
            case TrickInventoryAction.Cast:
                OnCastTrick(trick);
                break;
            case TrickInventoryAction.Dischard:
                OnDischardTrick(trick, location);
                break;
        }

        if (Combat != null)
            Combat.RefreshCombatUI();
    }

    public void OnCastTrick(TrickSO trick)
    {
        Logger.Log($"[TrickInventoryInputHandler] Tentando castar Trick: {trick?.DisplayName}");
        if (Combat == null || playerTrickInventory == null || trick == null)
            return;

        bool casted = Combat.TryCastPlayerTrick(trick);
        if (trickInventoryView != null)
        {
            trickInventoryView.SetStatus(casted ? $"Castou {trick.DisplayName}" : $"Falha ao castar {trick.DisplayName}");
            trickInventoryView.Refresh();
        }
    }

    public void OnDischardTrick(TrickSO trick, TrickInventoryItemLocation location)
    {
        Logger.Log($"[TrickInventoryInputHandler] Tentando descartar Trick: {trick?.DisplayName}");
        if (playerTrickInventory == null || trick == null)
            return;

        bool discarded = location.Location == TrickInventoryLocation.CastedSlot
            ? playerTrickInventory.RemoveCastedTrick(location.SlotIndex)
            : playerTrickInventory.DischardTrick(trick);

        if (trickInventoryView != null)
        {
            trickInventoryView.SetStatus(discarded ? $"Descartou {trick.DisplayName}" : $"Falha ao descartar {trick.DisplayName}");
            trickInventoryView.Refresh();
        }
    }
}
