using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatUI : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private GameObject combatRoot;
    [SerializeField] private GameObject victoryRoot;
    [SerializeField] private GameObject gameOverRoot;

    [Header("Player")]
    [SerializeField] private CombatHudBinding playerHud;
    [SerializeField] private TMP_Text playerHpText;

    [Header("Enemy")]
    [SerializeField] private CombatHudBinding enemyHud;
    [SerializeField] private TMP_Text enemyHpText;

    [Header("Turn")]
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text diceText;
    [SerializeField] private TMP_Text logText;

    [Header("Action Buttons")]
    [SerializeField] private Button rechargeButton;
    [SerializeField] private Toggle rechargeBoostToggle;
    [SerializeField] private Button investigateButton;
    [SerializeField] private Button fleeButton;
    [SerializeField] private TMP_InputField fleeDiceInput;
    [SerializeField] private Button itemButton;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button defendButton;
    [SerializeField] private Button endTurnButton;

    [Header("Result Buttons")]
    [SerializeField] private Button victoryContinueButton;
    [SerializeField] private Button gameOverRestartButton;
    [SerializeField] private Button gameOverQuitButton;

    public event Action<bool> OnRechargeRequested;
    public event Action OnInvestigateRequested;
    public event Action<int> OnFleeRequested;
    public event Action OnUseItemRequested;
    public event Action<PlayerActionType, CombatActionIntensity> OnCombatActionRequested;
    public event Action OnEndTurnRequested;
    public event Action OnVictoryContinueRequested;
    public event Action OnRestartRequested;
    public event Action OnQuitRequested;

    private void Awake()
    {
        BindButtons();
        ShowCombat(false);
    }

    public void ShowCombat(bool visible)
    {
        if (combatRoot != null)
            combatRoot.SetActive(visible);

        if (!visible)
        {
            if (victoryRoot != null)
                victoryRoot.SetActive(false);
            if (gameOverRoot != null)
                gameOverRoot.SetActive(false);
        }
    }

    public void SetTurnState(string value)
    {
        if (stateText != null)
            stateText.text = value;
    }

    public void SetDice(int value)
    {
        if (diceText != null)
            diceText.text = $"Dados: {value}";
    }

    public void SetLog(string value)
    {
        if (logText != null)
            logText.text = value;
    }

    public void SetPlayer(CombatBattlerRuntime battler)
    {
        if (battler == null)
            return;

        playerHud?.SetValues(battler.heart, battler.maxHeart, battler.body, battler.maxBody, battler.mind, battler.maxMind);

        if (playerHpText != null)
            playerHpText.text = $"HP: {battler.hp}/{battler.maxHp}";
    }

    public void SetEnemy(CombatBattlerRuntime battler)
    {
        if (battler == null)
            return;

        enemyHud?.SetValues(battler.heart, battler.maxHeart, battler.body, battler.maxBody, battler.mind, battler.maxMind);

        if (enemyHpText != null)
            enemyHpText.text = $"HP: {battler.hp}/{battler.maxHp}";
    }

    public void ShowVictory()
    {
        if (victoryRoot != null)
            victoryRoot.SetActive(true);
    }

    public void ShowGameOver()
    {
        if (gameOverRoot != null)
            gameOverRoot.SetActive(true);
    }

    private void BindButtons()
    {
        if (rechargeButton != null)
            rechargeButton.onClick.AddListener(() => OnRechargeRequested?.Invoke(rechargeBoostToggle != null && rechargeBoostToggle.isOn));

        if (investigateButton != null)
            investigateButton.onClick.AddListener(() => OnInvestigateRequested?.Invoke());

        if (fleeButton != null)
            fleeButton.onClick.AddListener(() => OnFleeRequested?.Invoke(ReadFleeDiceAmount()));

        if (itemButton != null)
            itemButton.onClick.AddListener(() => OnUseItemRequested?.Invoke());

        if (attackButton != null)
            attackButton.onClick.AddListener(() => OnCombatActionRequested?.Invoke(PlayerActionType.AttackBody, CombatActionIntensity.Normal));

        if (defendButton != null)
            defendButton.onClick.AddListener(() => OnCombatActionRequested?.Invoke(PlayerActionType.Defend, CombatActionIntensity.Low));

        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(() => OnEndTurnRequested?.Invoke());

        if (victoryContinueButton != null)
            victoryContinueButton.onClick.AddListener(() => OnVictoryContinueRequested?.Invoke());

        if (gameOverRestartButton != null)
            gameOverRestartButton.onClick.AddListener(() => OnRestartRequested?.Invoke());

        if (gameOverQuitButton != null)
            gameOverQuitButton.onClick.AddListener(() => OnQuitRequested?.Invoke());
    }

    private int ReadFleeDiceAmount()
    {
        if (fleeDiceInput == null)
            return 1;

        if (!int.TryParse(fleeDiceInput.text, out int value))
            return 1;

        return Mathf.Clamp(value, 1, TurnManager.DicePerTurn);
    }
}
