using System;
using UnityEngine;

public class CombatInputHandler : MonoBehaviour
{
    private CombatManager Combat;
    private int AttackDiceAllocated = 0;
    private int DefenseDiceAllocated = 0;
    private ActionType? SelectedAction = null;
    private ActionType AllowedAction = ActionType.Attack;
    private bool IsWaitingTurnResolution = false;
    public event Action<bool> EndTurnAvailabilityChanged;

    public void Init(CombatManager cm)
    {
        Combat = cm;
    }

    public void UpdateCombatView()
    {
        Combat.View.UpdateView(Combat.Player, Combat.Enemy);
    }

    public void SetAllowedAction(ActionType allowedAction)
    {
        AllowedAction = allowedAction;
        SelectedAction = null;
        IsWaitingTurnResolution = false;
        AttackDiceAllocated = 0;
        DefenseDiceAllocated = 0;
        Combat.View.UpdateAddDiceAttackCount(AttackDiceAllocated);
        Combat.View.UpdateAddDiceDefenseCount(DefenseDiceAllocated);
        UpdateCombatView();
        NotifyEndTurnAvailability();

        Debug.Log($"[Input] Turn role updated. Allowed action: {AllowedAction}");
    }

    public void OnAttack()
    {
        if (IsWaitingTurnResolution) return;
        if (AllowedAction != ActionType.Attack)
        {
            Debug.Log("[Input] Attack is disabled for this turn role");
            return;
        }

        SelectedAction = ActionType.Attack;
        NotifyEndTurnAvailability();
        Debug.Log("[Input] Selected ATTACK");
    }

    public void OnDefend()
    {
        if (IsWaitingTurnResolution) return;
        if (AllowedAction != ActionType.Defense)
        {
            Debug.Log("[Input] Defense is disabled for this turn role");
            return;
        }

        SelectedAction = ActionType.Defense;
        NotifyEndTurnAvailability();
        Debug.Log("[Input] Selected DEFENSE");
    }

    public void OnAddDiceToAttack()
    {
        if (IsWaitingTurnResolution) return;
        if (AllowedAction != ActionType.Attack) return;
        if (Combat.Player.CurrentDices <= 0) return;

        AttackDiceAllocated++;
        Combat.Player.CurrentDices--;
        Combat.View.UpdateAddDiceAttackCount(AttackDiceAllocated);
        UpdateCombatView();

        Debug.Log($"[Input] Added dice to ATTACK: {AttackDiceAllocated}");
    }

    public void OnAddDiceToDefense()
    {
        if (IsWaitingTurnResolution) return;
        if (AllowedAction != ActionType.Defense) return;
        if (Combat.Player.CurrentDices <= 0) return;

        DefenseDiceAllocated++;
        Combat.Player.CurrentDices--;
        Combat.View.UpdateAddDiceDefenseCount(DefenseDiceAllocated);
        UpdateCombatView();

        Debug.Log($"[Input] Added dice to DEFENSE: {DefenseDiceAllocated}");
    }

    public void OnRemoveDiceFromAttack()
    {
        if (IsWaitingTurnResolution) return;
        if (AllowedAction != ActionType.Attack) return;
        if (AttackDiceAllocated <= 0) return;

        AttackDiceAllocated--;
        Combat.Player.CurrentDices++;
        Combat.View.UpdateAddDiceAttackCount(AttackDiceAllocated);
        UpdateCombatView();

        Debug.Log($"[Input] Removed dice from ATTACK: {AttackDiceAllocated}");
    }

    public void OnRemoveDiceFromDefense()
    {
        if (IsWaitingTurnResolution) return;
        if (AllowedAction != ActionType.Defense) return;
        if (DefenseDiceAllocated <= 0) return;

        DefenseDiceAllocated--;
        Combat.Player.CurrentDices++;
        Combat.View.UpdateAddDiceDefenseCount(DefenseDiceAllocated);
        UpdateCombatView();

        Debug.Log($"[Input] Removed dice from DEFENSE: {DefenseDiceAllocated}");
    }

    public void OnEndTurn()
    {
        if (IsWaitingTurnResolution) return;
        if (SelectedAction == null)
        {
            Debug.Log("[Input] No action selected");
            return;
        }

        IsWaitingTurnResolution = true;

        Combat.ReceivePlayerInput(
            SelectedAction.Value,
            SelectedAction.Value == ActionType.Attack ? AttackDiceAllocated : DefenseDiceAllocated
        );

        SelectedAction = null;
        NotifyEndTurnAvailability();
    }

    private void NotifyEndTurnAvailability()
    {
        bool isAvailable = !IsWaitingTurnResolution && SelectedAction != null;
        EndTurnAvailabilityChanged?.Invoke(isAvailable);
    }
}
