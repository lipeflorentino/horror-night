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

        inputView.OnRecharge += HandleRecharge;
        inputView.OnRechargeBoosted += HandleRechargeBoosted;
        inputView.OnInvestigate += HandleInvestigate;
        inputView.OnFlee += HandleFlee;
        inputView.OnAttack += HandleAttack;
        inputView.OnEndTurn += HandleEndTurn;
        inputView.OnUseItem += HandleUseItem;
        inputView.OnSkills += HandleSkills;
        inputView.OnInfo += HandleInfo;
    }

    private void OnDisable()
    {
        if (inputView == null)
            return;

        inputView.OnRecharge -= HandleRecharge;
        inputView.OnRechargeBoosted -= HandleRechargeBoosted;
        inputView.OnInvestigate -= HandleInvestigate;
        inputView.OnFlee -= HandleFlee;
        inputView.OnAttack -= HandleAttack;
        inputView.OnEndTurn -= HandleEndTurn;
        inputView.OnUseItem -= HandleUseItem;
        inputView.OnSkills -= HandleSkills;
        inputView.OnInfo -= HandleInfo;
    }

    private void HandleRecharge()
    {
        if (combatManager != null)
            combatManager.PlayerRecharge(false);
    }

    private void HandleRechargeBoosted()
    {
        if (combatManager != null)
            combatManager.PlayerRecharge(true);
    }

    private void HandleInvestigate()
    {
        if (combatManager != null)
            combatManager.PlayerInvestigate();
    }

    private void HandleFlee(int diceAmount)
    {
        if (combatManager != null)
            combatManager.PlayerFlee(diceAmount);
    }

    private void HandleAttack()
    {
        if (combatManager != null)
            combatManager.PlayerAttack();
    }

    private void HandleEndTurn()
    {
        if (combatManager != null)
            combatManager.EndPlayerTurn();
    }

    private void HandleUseItem()
    {
        if (combatManager != null)
            combatManager.PlayerUseItem();
    }

    private void HandleSkills()
    {
        if (combatManager != null)
            combatManager.PlayerSkills();
    }

    private void HandleInfo()
    {
        if (combatManager != null)
            combatManager.PlayerInfo();
    }
}
