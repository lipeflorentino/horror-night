using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatSceneBindings : MonoBehaviour
{
    [Serializable]
    public class StatBarBinding
    {
        public Image fillImage;
        public TMP_Text valueText;

        public void SetValue(int current, int max)
        {
            if (fillImage != null)
                fillImage.fillAmount = max <= 0 ? 0f : Mathf.Clamp01(current / (float)max);

            if (valueText != null)
                valueText.text = current.ToString();
        }
    }

    [Serializable]
    public class CombatHudBinding
    {
        public StatBarBinding life;
        public StatBarBinding physical;
        public StatBarBinding mental;

        public void SetValues(int lifeValue, int lifeMax, int physicalValue, int physicalMax, int mentalValue, int mentalMax)
        {
            life?.SetValue(lifeValue, lifeMax);
            physical?.SetValue(physicalValue, physicalMax);
            mental?.SetValue(mentalValue, mentalMax);
        }
    }

    public Transform playerSpawnPoint;
    public Transform enemySpawnPoint;

    [Header("Turn + Combat Feedback")]
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text combatLogText;
    [SerializeField] private Image diceImage;
    [SerializeField] private TMP_Text diceValueText;

    [Header("Actions UI")]
    [SerializeField] private GameObject initialActionsUI;
    [SerializeField] private GameObject attackActionsUI;
    [SerializeField] private GameObject defenseActionsUI;
    [SerializeField] private GameObject specialActionsUI;

    [SerializeField] private Button attackButton;
    [SerializeField] private Button defenseButton;
    [SerializeField] private Button itemButton;
    [SerializeField] private Button specialButton;

    [SerializeField] private Button attackLifeButton;
    [SerializeField] private Button attackPhysicalButton;
    [SerializeField] private Button attackMentalButton;

    [SerializeField] private Button defendButton;
    [SerializeField] private Button parryButton;
    [SerializeField] private Button fleeButton;
    [SerializeField] private Button instantKillButton;
    [SerializeField] private Button learnButton;

    [Header("HUD")]
    [SerializeField] private CombatHudBinding playerHud;
    [SerializeField] private CombatHudBinding enemyHud;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;

    public event Action<PlayerActionType> OnPlayerActionSelected;

    private void Awake()
    {
        RegisterButton(attackButton, () => OpenActionMenu(initialActionsUI, attackActionsUI));
        RegisterButton(defenseButton, () => OpenActionMenu(initialActionsUI, defenseActionsUI));
        RegisterButton(specialButton, () => OpenActionMenu(initialActionsUI, specialActionsUI));
        RegisterButton(itemButton, () => TriggerPlayerAction(PlayerActionType.Item));

        RegisterButton(attackLifeButton, () => TriggerPlayerAction(PlayerActionType.AttackLife));
        RegisterButton(attackPhysicalButton, () => TriggerPlayerAction(PlayerActionType.AttackPhysical));
        RegisterButton(attackMentalButton, () => TriggerPlayerAction(PlayerActionType.AttackMental));

        RegisterButton(defendButton, () => TriggerPlayerAction(PlayerActionType.Defend));
        RegisterButton(parryButton, () => TriggerPlayerAction(PlayerActionType.Parry));
        RegisterButton(fleeButton, () => TriggerPlayerAction(PlayerActionType.Flee));
        RegisterButton(instantKillButton, () => TriggerPlayerAction(PlayerActionType.InstantKill));
        RegisterButton(learnButton, () => TriggerPlayerAction(PlayerActionType.Learn));

        SetActionsVisible(false);
    }

    public void SetTurnText(string value)
    {
        if (turnText != null)
            turnText.text = value;
    }

    public void SetCombatLog(string value)
    {
        if (combatLogText != null)
            combatLogText.text += "\n" + value;
    }

    public void SetDiceValue(int value)
    {
        if (diceValueText != null)
            diceValueText.text = value.ToString();

        if (diceImage != null)
            diceImage.enabled = true;
    }

    public void SetActionsVisible(bool visible)
    {
        if (initialActionsUI != null)
            initialActionsUI.SetActive(visible);

        if (attackActionsUI != null)
            attackActionsUI.SetActive(false);

        if (defenseActionsUI != null)
            defenseActionsUI.SetActive(false);

        if (specialActionsUI != null)
            specialActionsUI.SetActive(false);
    }

    public void UpdateHud(
        int playerLife,
        int playerLifeMax,
        int playerPhysical,
        int playerPhysicalMax,
        int playerMental,
        int playerMentalMax,
        int enemyLife,
        int enemyLifeMax,
        int enemyPhysical,
        int enemyPhysicalMax,
        int enemyMental,
        int enemyMentalMax)
    {
        playerHud?.SetValues(playerLife, playerLifeMax, playerPhysical, playerPhysicalMax, playerMental, playerMentalMax);
        enemyHud?.SetValues(enemyLife, enemyLifeMax, enemyPhysical, enemyPhysicalMax, enemyMental, enemyMentalMax);
    }

    public void ShowGameOverUI(Action onRestart)
    {
        Time.timeScale = 0f;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (restartButton == null)
            return;

        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            onRestart?.Invoke();
        });
    }

    private void RegisterButton(Button button, Action callback)
    {
        Debug.Log($"RegisterButton {button} - {callback}");
        if (button == null)
            return;

        Debug.Log("Button register onClick");
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => callback?.Invoke());
    }

    private void OpenActionMenu(GameObject previousMenu, GameObject nextMenu)
    {
        if (previousMenu != null)
            previousMenu.SetActive(false);

        if (nextMenu != null)
            nextMenu.SetActive(true);
    }

    private void TriggerPlayerAction(PlayerActionType action)
    {
        SetActionsVisible(false);
        OnPlayerActionSelected?.Invoke(action);
    }
}
