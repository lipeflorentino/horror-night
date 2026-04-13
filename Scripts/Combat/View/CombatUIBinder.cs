using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(InputView))]
public class CombatUIBinder : MonoBehaviour
{
    [SerializeField] private InputView inputView;
    [SerializeField] private CombatManager combatManager;

    private void Reset()
    {
        if (inputView == null)
            inputView = GetComponent<InputView>();

        if (combatManager == null)
            combatManager = GetComponent<CombatManager>();
    }

    private void OnEnable()
    {
        if (inputView == null)
            return;

        inputView.OnAttack += HandleAttack;
        inputView.OnInvestigate += HandleInvestigate;
        inputView.OnDefend += HandleDefend;
        inputView.OnAddAttackDice += HandleAddAttackDice;
        inputView.OnAddInvestigateDice += HandleAddInvestigateDice;
        inputView.OnAddDefendDice += HandleAddDefendDice;
        inputView.OnEndTurn += HandleEndTurn;
        inputView.OnItem += HandleItem;
        inputView.OnSkill += HandleSkill;
        inputView.OnUseItem += HandleUseItem;
        inputView.OnUseSkill += HandleUseSkill;
        inputView.OnInfo += HandleInfo;
    }

    private void OnDisable()
    {
        if (inputView == null)
            return;

        inputView.OnAttack -= HandleAttack;
        inputView.OnInvestigate -= HandleInvestigate;
        inputView.OnDefend -= HandleDefend;
        inputView.OnAddAttackDice -= HandleAddAttackDice;
        inputView.OnAddInvestigateDice -= HandleAddInvestigateDice;
        inputView.OnAddDefendDice -= HandleAddDefendDice;
        inputView.OnEndTurn -= HandleEndTurn;
        inputView.OnItem -= HandleItem;
        inputView.OnSkill -= HandleSkill;
        inputView.OnUseItem -= HandleUseItem;
        inputView.OnUseSkill -= HandleUseSkill;
        inputView.OnInfo -= HandleInfo;
    }

    private void HandleAttack()
    {
        if (combatManager != null)
            combatManager.PlayerAttack();
    }

    private void HandleInvestigate()
    {
        if (combatManager != null)
            combatManager.PlayerInvestigate();
    }

    private void HandleDefend()
    {
        if (combatManager != null)
            combatManager.PlayerDefend();
    }

    private void HandleAddAttackDice(int diceAmount)
    {
        if (combatManager != null)
            combatManager.PlayerAddAttackDice(diceAmount);
    }

    private void HandleAddInvestigateDice(int diceAmount)
    {
        if (combatManager != null)
            combatManager.PlayerAddInvestigateDice(diceAmount);
    }

    private void HandleAddDefendDice(int diceAmount)
    {
        if (combatManager != null)
            combatManager.PlayerAddDefendDice(diceAmount);
    }

    private void HandleEndTurn()
    {
        if (combatManager != null)
            combatManager.EndPlayerTurn();
    }

    private void HandleItem()
    {
        if (combatManager != null)
            combatManager.PlayerUseItem();
    }

    private void HandleSkill()
    {
        if (combatManager != null)
            combatManager.PlayerSkills();
    }

    private void HandleUseItem()
    {
        if (combatManager != null)
            combatManager.PlayerSelectItem(1);
    }

    private void HandleUseSkill()
    {
        if (combatManager != null)
            combatManager.PlayerUseSkill();
    }

    private void HandleInfo()
    {
        if (combatManager != null)
            combatManager.PlayerInfo();
    }
}
