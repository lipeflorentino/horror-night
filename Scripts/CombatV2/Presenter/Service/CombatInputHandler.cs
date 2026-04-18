using UnityEngine;

public class CombatInputHandler : MonoBehaviour
{
    private CombatManager _combat;
    private int _attackDiceAllocated = 0;
    private int _defenseDiceAllocated = 0;
    private ActionType? _selectedAction = null;

    public void Init(CombatManager combat)
    {
        _combat = combat;
    }

    public void OnAttack()
    {
        _selectedAction = ActionType.Attack;
        Debug.Log("[Input] Selected ATTACK");
    }

    public void OnDefend()
    {
        _selectedAction = ActionType.Defense;
        Debug.Log("[Input] Selected DEFENSE");
    }

    public void OnAddDiceToAttack()
    {
        if (_combat.Player.CurrentDices <= 0) return;

        _attackDiceAllocated++;
        _combat.Player.CurrentDices--;

        Debug.Log($"[Input] Added dice to ATTACK: {_attackDiceAllocated}");
    }

    public void OnAddDiceToDefense()
    {
        if (_combat.Player.CurrentDices <= 0) return;

        _defenseDiceAllocated++;
        _combat.Player.CurrentDices--;

        Debug.Log($"[Input] Added dice to DEFENSE: {_defenseDiceAllocated}");
    }

    public void OnEndTurn()
    {
        if (_selectedAction == null)
        {
            Debug.Log("[Input] No action selected");
            return;
        }

        _combat.ReceivePlayerInput(
            _selectedAction.Value,
            _attackDiceAllocated,
            _defenseDiceAllocated
        );

        _attackDiceAllocated = 0;
        _defenseDiceAllocated = 0;
        _selectedAction = null;
    }
}