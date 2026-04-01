using System;
using System.Collections;

public class CombatPresenter
{
    private readonly CombatUI combatUI;
    private readonly CombatInputController inputController;
    private bool isAwaitingDefenseAction;

    public event Action<PlayerActionType> OnPlayerActionSelected;

    public CombatPresenter(CombatUI combatUI, CombatInputController inputController)
    {
        this.combatUI = combatUI;
        this.inputController = inputController;

        this.inputController.OnAttackMenuRequested += HandleAttackMenuRequested;
        this.inputController.OnSpecialMenuRequested += HandleSpecialMenuRequested;
        this.inputController.OnBackRequested += HandleBackRequested;
        this.inputController.OnDefenseBackRequested += HandleDefenseBackRequested;
        this.inputController.OnLearnInfoToggleRequested += HandleLearnInfoToggleRequested;
        this.inputController.OnPlayerActionSelected += HandlePlayerActionSelected;
    }

    public void SetTurnText(string value) => combatUI.SetTurnText(value);
    public void SetCombatLog(string value, CombatLogCategory category = CombatLogCategory.Default) => combatUI.SetCombatLog(value, category);
    public IEnumerator PlayDiceRoll(int value) => combatUI.PlayDiceRoll(value);
    public void SetActionsVisible(bool visible) => combatUI.SetActionsVisible(visible);
    public void ShowAttackMenu() => combatUI.ShowAttackMenu();
    public void ShowInitialMenu() => combatUI.ShowInitialMenu();
    public void ShowDefenseMenu() => combatUI.ShowDefenseMenu();
    public void UpdateAttackButtonAvailability(bool heartEnabled, bool bodyEnabled, bool mindEnabled) => combatUI.UpdateAttackButtonAvailability(heartEnabled, bodyEnabled, mindEnabled);
    public void UpdateSpecialActionAvailability(bool instantKillEnabled, bool learnEnabled) => combatUI.UpdateSpecialActionAvailability(instantKillEnabled, learnEnabled);
    public void SetEnemyLearnState(int revealLevel, EnemySO enemy, int currentHeart, int currentBody, int currentMind, TurnManagerStats enemyStats) => combatUI.SetEnemyLearnState(revealLevel, enemy, currentHeart, currentBody, currentMind, enemyStats);
    public void ResetDiceValue() => combatUI.ResetDiceValue();
    public void UpdateHud(int playerheart, int playerheartMax, int playerbody, int playerbodyMax, int playermind, int playermindMax, int enemyheart, int enemyheartMax, int enemybody, int enemybodyMax, int enemymind, int enemymindMax) => combatUI.UpdateHud(playerheart, playerheartMax, playerbody, playerbodyMax, playermind, playermindMax, enemyheart, enemyheartMax, enemybody, enemybodyMax, enemymind, enemymindMax);
    public void ShowGameOverUI(Action onRestart) => combatUI.ShowGameOverUI(onRestart);
    public void RefreshCombatVisualReferences() => combatUI.RefreshCombatVisualReferences();
    public void NotifyPlayerDamage(int amount, bool critical = false) => combatUI.NotifyPlayerDamage(amount, critical);
    public void NotifyEnemyDamage(int amount, bool critical = false) => combatUI.NotifyEnemyDamage(amount, critical);
    public void NotifyPlayerAction(string actionText, PlayerActionType? actionType = null) => combatUI.NotifyPlayerAction(actionText, actionType);
    public void NotifyEnemyAction(string actionText, EnemyActionType? actionType = null) => combatUI.NotifyEnemyAction(actionText, actionType);
    public void BeginDefenseSelection() => isAwaitingDefenseAction = true;
    public void EndDefenseSelection() => isAwaitingDefenseAction = false;

    private void HandleAttackMenuRequested()
    {
        combatUI.ShowAttackMenu();
    }

    private void HandleSpecialMenuRequested()
    {
        combatUI.ShowSpecialMenu();
    }

    private void HandleLearnInfoToggleRequested()
    {
        combatUI.ToggleLearnInfoPanel();
    }

    private void HandleBackRequested()
    {
        combatUI.ShowInitialMenu();
    }

    private void HandleDefenseBackRequested()
    {
        if (isAwaitingDefenseAction)
            return;

        combatUI.ShowInitialMenu();
    }

    private void HandlePlayerActionSelected(PlayerActionType action)
    {
        combatUI.SetActionsVisible(false);
        OnPlayerActionSelected?.Invoke(action);
    }
}
