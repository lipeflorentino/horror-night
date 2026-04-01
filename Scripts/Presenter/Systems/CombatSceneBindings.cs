using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatSceneBindings : MonoBehaviour
{
    public Transform playerSpawnPoint;
    public Transform enemySpawnPoint;

    [Header("Turn + Combat Feedback")]
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text combatLogText;
    [SerializeField] private DiceRollUI diceRollUI;

    [Header("Combat Feedback")]
    [SerializeField] private GameObject playerHitUI;
    [SerializeField] private SpriteRenderer enemySpriteRenderer;
    [SerializeField] private TMP_Text playerFeedbackText;
    [SerializeField] private TMP_Text enemyFeedbackText;
    [SerializeField] private float damageFlashDuration = 0.15f;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private Color actionFeedbackColor = new(1f, 0.9f, 0.3f);
    [SerializeField] private Color criticalFeedbackColor = new(1f, 0.35f, 0.35f);

    [Header("Actions UI")]
    [SerializeField] private GameObject initialActionsUI;
    [SerializeField] private GameObject attackActionsUI;
    [SerializeField] private GameObject defenseActionsUI;
    [SerializeField] private GameObject specialActionsUI;

    [SerializeField] private Button attackButton;
    [SerializeField] private Button itemButton;
    [SerializeField] private Button specialButton;

    [SerializeField] private Button attackHeartButton;
    [SerializeField] private Button attackBodyButton;
    [SerializeField] private Button attackMindButton;

    [SerializeField] private Button defendButton;
    [SerializeField] private Button parryButton;
    [SerializeField] private Button fleeButton;
    [SerializeField] private Button instantKillButton;
    [SerializeField] private Button learnButton;

    [Header("Learn UI")]
    [SerializeField] private Button learnInfoIconButton;
    [SerializeField] private GameObject learnInfoPanel;
    [SerializeField] private TMP_Text learnInfoText;

    [Header("HUD")]
    [SerializeField] private CombatUI.CombatHudBinding playerHud;
    [SerializeField] private CombatUI.CombatHudBinding enemyHud;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;

    private CombatPresenter presenter;

    public event Action<PlayerActionType> OnPlayerActionSelected
    {
        add
        {
            if (presenter != null)
                presenter.OnPlayerActionSelected += value;
        }
        remove
        {
            if (presenter != null)
                presenter.OnPlayerActionSelected -= value;
        }
    }

    private void Awake()
    {
        CombatUI combatUI = new(
            this,
            turnText,
            combatLogText,
            diceRollUI,
            playerHitUI,
            enemySpriteRenderer,
            playerFeedbackText,
            enemyFeedbackText,
            damageFlashDuration,
            damageFlashColor,
            actionFeedbackColor,
            criticalFeedbackColor,
            initialActionsUI,
            attackActionsUI,
            defenseActionsUI,
            specialActionsUI,
            attackHeartButton,
            attackBodyButton,
            attackMindButton,
            instantKillButton,
            learnButton,
            learnInfoIconButton,
            learnInfoPanel,
            learnInfoText,
            playerHud,
            enemyHud,
            gameOverPanel,
            restartButton);

        CombatInputController inputController = new(
            attackButton,
            itemButton,
            specialButton,
            attackHeartButton,
            attackBodyButton,
            attackMindButton,
            defendButton,
            parryButton,
            fleeButton,
            instantKillButton,
            learnButton,
            learnInfoIconButton);

        presenter = new CombatPresenter(combatUI, inputController);
    }

    public void SetTurnText(string value) => presenter.SetTurnText(value);
    public void SetCombatLog(string value, CombatLogCategory category = CombatLogCategory.Default) => presenter.SetCombatLog(value, category);
    public IEnumerator PlayDiceRoll(int value) => presenter.PlayDiceRoll(value);
    public void SetActionsVisible(bool visible) => presenter.SetActionsVisible(visible);
    public void ShowAttackMenu() => presenter.ShowAttackMenu();
    public void ShowDefenseMenu() => presenter.ShowDefenseMenu();
    public void UpdateAttackButtonAvailability(bool heartEnabled, bool bodyEnabled, bool mindEnabled) => presenter.UpdateAttackButtonAvailability(heartEnabled, bodyEnabled, mindEnabled);
    public void UpdateSpecialActionAvailability(bool instantKillEnabled, bool learnEnabled) => presenter.UpdateSpecialActionAvailability(instantKillEnabled, learnEnabled);
    public void SetEnemyLearnState(int revealLevel, EnemySO enemy, int currentHeart, int currentBody, int currentMind, TurnManagerStats enemyStats) => presenter.SetEnemyLearnState(revealLevel, enemy, currentHeart, currentBody, currentMind, enemyStats);
    public void ResetDiceValue() => presenter.ResetDiceValue();
    public void UpdateHud(int playerheart, int playerheartMax, int playerbody, int playerbodyMax, int playermind, int playermindMax, int enemyheart, int enemyheartMax, int enemybody, int enemybodyMax, int enemymind, int enemymindMax) => presenter.UpdateHud(playerheart, playerheartMax, playerbody, playerbodyMax, playermind, playermindMax, enemyheart, enemyheartMax, enemybody, enemybodyMax, enemymind, enemymindMax);
    public void ShowGameOverUI(Action onRestart) => presenter.ShowGameOverUI(onRestart);
    public void RefreshCombatVisualReferences() => presenter.RefreshCombatVisualReferences();
    public void NotifyPlayerDamage(int amount, bool critical = false) => presenter.NotifyPlayerDamage(amount, critical);
    public void NotifyEnemyDamage(int amount, bool critical = false) => presenter.NotifyEnemyDamage(amount, critical);
    public void NotifyPlayerAction(string actionText) => presenter.NotifyPlayerAction(actionText);
    public void NotifyEnemyAction(string actionText) => presenter.NotifyEnemyAction(actionText);
}
