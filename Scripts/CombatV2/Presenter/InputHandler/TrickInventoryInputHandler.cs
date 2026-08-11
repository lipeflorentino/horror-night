using UnityEngine;

public class TrickInventoryInputHandler : MonoBehaviour
{
    [SerializeField] private TrickInventoryView trickInventoryView;
    [SerializeField] private CombatManager Combat;

    private ITrickInventory playerTrickInventory;

    public void Init(CombatManager combatManager, ITrickInventory trickInventory)
    {
        Combat = combatManager;
        trickInventoryView = trickInventoryView != null ? trickInventoryView : FindObjectOfType<TrickInventoryView>();        
        playerTrickInventory = trickInventory;

        if (trickInventoryView != null)
        {
            trickInventoryView.BindInventory(playerTrickInventory, Combat.GetPerkService());
            trickInventoryView.OnInteractWithInventoryTrick += HandleTrickInteraction;
        }
    }

    private void OnDestroy()
    {
        if (trickInventoryView == null)
            return;

        trickInventoryView.OnInteractWithInventoryTrick -= HandleTrickInteraction;
    }

    private void HandleTrickInteraction(TrickSlot slot, TrickInventoryAction action, TrickInventoryItemLocation location)
    {
        switch (action)
        {
            case TrickInventoryAction.Cast:
                OnCastTrick(slot?.Definition);
                break;
            case TrickInventoryAction.Dischard:
                OnDischardTrick(slot?.Definition, location);
                break;
        }

        if (Combat != null)
            Combat.RefreshCombatUI();
    }

    public void OnCastTrick(TrickSO trick)
    {
        if (Combat == null)
        {
            return;
        }
        
        if (playerTrickInventory == null)
        {
            return;
        }
        
        if (trick == null)
        {
            return;
        }

        bool casted = Combat.TryCastPlayerTrick(trick);
        if (trickInventoryView != null)
        {
            trickInventoryView.SetStatus(casted ? $"Castou {trick.DisplayName}" : $"Falha ao castar {trick.DisplayName}");
            trickInventoryView.Refresh();
        }
    }

    public void OnDischardTrick(TrickSO trick, TrickInventoryItemLocation location)
    {
        if (playerTrickInventory == null)
        {
            return;
        }
        
        if (trick == null)
        {
            return;
        }

        bool discarded = (location.Location == TrickInventoryLocation.CastedActiveSlot || location.Location == TrickInventoryLocation.CastedPassiveSlot)
            ? playerTrickInventory.RemoveCastedTrick(GetSlotType(location.Location), location.SlotIndex)
            : playerTrickInventory.DischardTrick(trick);

        if (trickInventoryView != null)
        {
            trickInventoryView.SetStatus(discarded ? $"Descartou {trick.DisplayName}" : $"Falha ao descartar {trick.DisplayName}");
            trickInventoryView.Refresh();
        }
    }

    private static TrickSlotType GetSlotType(TrickInventoryLocation location)
    {
        return location == TrickInventoryLocation.CastedPassiveSlot
            ? TrickSlotType.CastedPassive
            : TrickSlotType.CastedActive;
    }
}
