using UnityEngine;

public class TrickInventoryInputHandler : MonoBehaviour
{
    [SerializeField] private TrickInventoryView trickInventoryView;
    [SerializeField] private MonoBehaviour playerTrickInventorySource;
    [SerializeField] private CombatManager Combat;

    private ITrickInventory playerTrickInventory;

    public void Init(CombatManager combatManager, ITrickInventory trickInventory)
    {
        Logger.Log($"[TrickInventoryInputHandler] Init: Iniciando TrickInventoryInputHandler.");
        
        Combat = combatManager;
        trickInventoryView = trickInventoryView != null ? trickInventoryView : FindObjectOfType<TrickInventoryView>();
        Logger.Log($"[TrickInventoryInputHandler] Init: Inventário de Tricks do jogador: {(trickInventory != null ? "encontrado" : "null")}. TrickInventoryView: {(trickInventoryView != null ? "encontrada" : "null")}.");
        
        playerTrickInventory = trickInventory;
        playerTrickInventorySource = trickInventory as MonoBehaviour;

        if (trickInventoryView != null)
        {
            Logger.Log($"[TrickInventoryInputHandler] Init: Vinculando inventário à TrickInventoryView...");
            trickInventoryView.BindInventory(playerTrickInventory);
            trickInventoryView.OnInteractWithInventoryTrick += HandleTrickInteraction;
            Logger.Log("[TrickInventoryInputHandler] Init: Inscrito a OnInteractWithInventoryTrick com sucesso.");
        }
        else
        {
            Logger.Log("[TrickInventoryInputHandler] Init: TrickInventoryView não encontrada! Tricks UI não será funcional.");
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
        Logger.Log($"[TrickInventoryInputHandler] HandleTrickInteraction: Ação '{action}' no trick '{trick?.DisplayName}' (ID: {trick?.Id}) em {location.Location}.");
        
        switch (action)
        {
            case TrickInventoryAction.Cast:
                Logger.Log($"[TrickInventoryInputHandler] HandleTrickInteraction: Roteando para OnCastTrick.");
                OnCastTrick(trick);
                break;
            case TrickInventoryAction.Dischard:
                Logger.Log($"[TrickInventoryInputHandler] HandleTrickInteraction: Roteando para OnDischardTrick.");
                OnDischardTrick(trick, location);
                break;
        }

        if (Combat != null)
            Combat.RefreshCombatUI();
    }

    public void OnCastTrick(TrickSO trick)
    {
        Logger.Log($"[TrickInventoryInputHandler] OnCastTrick: Tentando castar '{trick?.DisplayName}' (ID: {trick?.Id}).");
        
        if (Combat == null)
        {
            Logger.Log("[TrickInventoryInputHandler] OnCastTrick: Combat é null!");
            return;
        }
        
        if (playerTrickInventory == null)
        {
            Logger.Log("[TrickInventoryInputHandler] OnCastTrick: playerTrickInventory é null!");
            return;
        }
        
        if (trick == null)
        {
            Logger.Log("[TrickInventoryInputHandler] OnCastTrick: trick é null!");
            return;
        }

        bool casted = Combat.TryCastPlayerTrick(trick);
        Logger.Log($"[TrickInventoryInputHandler] OnCastTrick: Cast result = {casted}.");
        
        if (trickInventoryView != null)
        {
            trickInventoryView.SetStatus(casted ? $"Castou {trick.DisplayName}" : $"Falha ao castar {trick.DisplayName}");
            trickInventoryView.Refresh();
        }
    }

    public void OnDischardTrick(TrickSO trick, TrickInventoryItemLocation location)
    {
        Logger.Log($"[TrickInventoryInputHandler] OnDischardTrick: Tentando descartar '{trick?.DisplayName}' (ID: {trick?.Id}) de {location.Location}.");
        
        if (playerTrickInventory == null)
        {
            Logger.Log("[TrickInventoryInputHandler] OnDischardTrick: playerTrickInventory é null!");
            return;
        }
        
        if (trick == null)
        {
            Logger.Log("[TrickInventoryInputHandler] OnDischardTrick: trick é null!");
            return;
        }

        bool discarded = location.Location == TrickInventoryLocation.CastedSlot
            ? playerTrickInventory.RemoveCastedTrick(location.SlotIndex)
            : playerTrickInventory.DischardTrick(trick);

        Logger.Log($"[TrickInventoryInputHandler] OnDischardTrick: Discard result = {discarded}.");
        
        if (trickInventoryView != null)
        {
            trickInventoryView.SetStatus(discarded ? $"Descartou {trick.DisplayName}" : $"Falha ao descartar {trick.DisplayName}");
            trickInventoryView.Refresh();
        }
    }
}
