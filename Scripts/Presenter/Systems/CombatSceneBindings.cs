using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private DiceRollUI diceRollUI;

    [Header("Combat Feedback")]
    [SerializeField] private GameObject playerHitUI;
    [SerializeField] private SpriteRenderer enemySpriteRenderer;
    [SerializeField] private GameObject playerFeedbackPanel;
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
    private readonly Queue<string> combatLogEntries = new();
    private const int MaxLogEntries = 5;
    private Coroutine playerFeedbackRoutine;
    private Coroutine enemyFeedbackRoutine;

    private void Awake()
    {
        RegisterButton(attackButton, () => OpenActionMenu(initialActionsUI, attackActionsUI));
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
        ResolveCombatVisualReferences();
        playerFeedbackPanel.SetActive(true);
    }

    public void SetTurnText(string value)
    {
        if (turnText != null)
            turnText.text = value;
    }

    public void SetCombatLog(string value, CombatLogCategory category = CombatLogCategory.Default)
    {
        if (combatLogText == null || string.IsNullOrWhiteSpace(value))
            return;

        combatLogEntries.Enqueue(FormatLog(value, category));
        while (combatLogEntries.Count > MaxLogEntries)
            combatLogEntries.Dequeue();

        combatLogText.text = string.Join("\n", combatLogEntries);
    }

    public IEnumerator PlayDiceRoll(int value)
    {
        if (diceRollUI == null)
            yield break;

        yield return StartCoroutine(diceRollUI.PlayRollAnimation(value));
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

    public void ShowAttackMenu()
    {
        SetActionsVisible(true);
        if (initialActionsUI != null)
            initialActionsUI.SetActive(false);
        if (attackActionsUI != null)
            attackActionsUI.SetActive(true);
    }

    public void ShowDefenseMenu()
    {
        SetActionsVisible(true);
        if (initialActionsUI != null)
            initialActionsUI.SetActive(false);
        if (defenseActionsUI != null)
            defenseActionsUI.SetActive(true);
    }

    public void UpdateAttackButtonAvailability(bool lifeEnabled, bool physicalEnabled, bool mentalEnabled)
    {
        if (attackLifeButton != null)
            attackLifeButton.interactable = lifeEnabled;

        if (attackPhysicalButton != null)
            attackPhysicalButton.interactable = physicalEnabled;

        if (attackMentalButton != null)
            attackMentalButton.interactable = mentalEnabled;
    }

    public void ResetDiceValue()
    {
        if (diceRollUI != null)
            diceRollUI.ClearValue();
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

    public void RefreshCombatVisualReferences()
    {
        ResolveCombatVisualReferences();
    }

    public void NotifyPlayerDamage(int amount, bool critical = false)
    {
        if (amount <= 0)
            return;

        StartCoroutine(FlashPlayerUIRoutine(playerHitUI));
        string damageText = critical ? $"CRITICAL HIT! -{amount}" : $"-{amount}";
        ShowFeedbackText(true, damageText, critical ? criticalFeedbackColor : damageFlashColor);
    }

    public void NotifyEnemyDamage(int amount, bool critical = false)
    {
        if (amount <= 0)
            return;

        FlashSprite(enemySpriteRenderer);
        string damageText = critical ? $"CRITICAL HIT! -{amount}" : $"-{amount}";
        ShowFeedbackText(false, damageText, critical ? criticalFeedbackColor : damageFlashColor);
    }

    public void NotifyPlayerAction(string actionText)
    {
        if (string.IsNullOrWhiteSpace(actionText))
            return;

        ShowFeedbackText(true, actionText, actionFeedbackColor);
    }

    public void NotifyEnemyAction(string actionText)
    {
        if (string.IsNullOrWhiteSpace(actionText))
            return;

        ShowFeedbackText(false, actionText, actionFeedbackColor);
    }

    private string FormatLog(string value, CombatLogCategory category)
    {
        string colorHex = category switch
        {
            CombatLogCategory.Damage => "#FF5F5F",
            CombatLogCategory.Action => "#FFD35A",
            CombatLogCategory.Victory => "#6CFF83",
            CombatLogCategory.Defeat => "#FF4040",
            _ => "#B3B3B3"
        };

        return $"<color={colorHex}>{value}</color>";
    }

    private void FlashSprite(SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer == null)
            return;
        
        StartCoroutine(FlashSpriteRoutine(spriteRenderer));
    }

    private IEnumerator FlashPlayerUIRoutine(GameObject hitPlayerUI)
    {
        hitPlayerUI.SetActive(true);
        yield return new WaitForSeconds(damageFlashDuration);
        hitPlayerUI.SetActive(false);
    }

    private IEnumerator FlashSpriteRoutine(SpriteRenderer spriteRenderer)
    {
        Color original = spriteRenderer.color;
        spriteRenderer.color = damageFlashColor;
        yield return new WaitForSeconds(damageFlashDuration);
        spriteRenderer.color = original;
    }

    private void ShowFeedbackText(bool isPlayer, string value, Color color)
    {
        TMP_Text feedbackText = isPlayer ? playerFeedbackText : enemyFeedbackText;
        if (feedbackText == null)
            return;

        if (isPlayer && playerFeedbackRoutine != null)
            StopCoroutine(playerFeedbackRoutine);

        if (!isPlayer && enemyFeedbackRoutine != null)
            StopCoroutine(enemyFeedbackRoutine);

        Coroutine newRoutine = StartCoroutine(ShowFeedbackTextRoutine(feedbackText, value, color));
        if (isPlayer)
            playerFeedbackRoutine = newRoutine;
        else
            enemyFeedbackRoutine = newRoutine;
    }

    private IEnumerator ShowFeedbackTextRoutine(TMP_Text feedbackText, string value, Color color)
    {
        feedbackText.gameObject.SetActive(true);
        feedbackText.color = color;
        feedbackText.text = value;
        yield return new WaitForSeconds(0.85f);
        feedbackText.gameObject.SetActive(false);
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

    private void ResolveCombatVisualReferences()
    {
        if (enemySpriteRenderer == null)
        {
            EnemyBattler enemyBattler = FindObjectOfType<EnemyBattler>();
            if (enemyBattler != null)
                enemySpriteRenderer = enemyBattler.GetComponentInChildren<SpriteRenderer>();
        }

        if (playerFeedbackText != null)
            playerFeedbackText.gameObject.SetActive(false);

        if (enemyFeedbackText != null)
            enemyFeedbackText.gameObject.SetActive(false);
    }
}

public enum CombatLogCategory
{
    Default,
    Action,
    Damage,
    Victory,
    Defeat
}
