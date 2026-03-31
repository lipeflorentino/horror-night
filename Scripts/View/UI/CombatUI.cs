using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatUI
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
        public StatBarBinding heart;
        public StatBarBinding body;
        public StatBarBinding mind;

        public void SetValues(int heartValue, int heartMax, int bodyValue, int bodyMax, int mindValue, int mindMax)
        {
            heart?.SetValue(heartValue, heartMax);
            body?.SetValue(bodyValue, bodyMax);
            mind?.SetValue(mindValue, mindMax);
        }
    }

    private readonly MonoBehaviour coroutineRunner;
    private readonly TMP_Text turnText;
    private readonly TMP_Text combatLogText;
    private readonly DiceRollUI diceRollUI;

    private readonly GameObject playerHitUI;
    private SpriteRenderer enemySpriteRenderer;
    private readonly TMP_Text playerFeedbackText;
    private readonly TMP_Text enemyFeedbackText;
    private readonly float damageFlashDuration;
    private readonly Color damageFlashColor;
    private readonly Color actionFeedbackColor;
    private readonly Color criticalFeedbackColor;

    private readonly GameObject initialActionsUI;
    private readonly GameObject attackActionsUI;
    private readonly GameObject defenseActionsUI;
    private readonly GameObject specialActionsUI;

    private readonly Button attackHeartButton;
    private readonly Button attackBodyButton;
    private readonly Button attackMindButton;
    private readonly Button instantKillButton;
    private readonly Button learnButton;

    private readonly Button learnInfoIconButton;
    private readonly GameObject learnInfoPanel;
    private readonly TMP_Text learnInfoText;

    private readonly CombatHudBinding playerHud;
    private readonly CombatHudBinding enemyHud;

    private readonly GameObject gameOverPanel;
    private readonly Button restartButton;

    private readonly Queue<string> combatLogEntries = new();
    private const int MaxLogEntries = 5;
    private Coroutine playerFeedbackRoutine;
    private Coroutine enemyFeedbackRoutine;

    public CombatUI(
        MonoBehaviour coroutineRunner,
        TMP_Text turnText,
        TMP_Text combatLogText,
        DiceRollUI diceRollUI,
        GameObject playerHitUI,
        SpriteRenderer enemySpriteRenderer,
        TMP_Text playerFeedbackText,
        TMP_Text enemyFeedbackText,
        float damageFlashDuration,
        Color damageFlashColor,
        Color actionFeedbackColor,
        Color criticalFeedbackColor,
        GameObject initialActionsUI,
        GameObject attackActionsUI,
        GameObject defenseActionsUI,
        GameObject specialActionsUI,
        Button attackHeartButton,
        Button attackBodyButton,
        Button attackMindButton,
        Button instantKillButton,
        Button learnButton,
        Button learnInfoIconButton,
        GameObject learnInfoPanel,
        TMP_Text learnInfoText,
        CombatHudBinding playerHud,
        CombatHudBinding enemyHud,
        GameObject gameOverPanel,
        Button restartButton)
    {
        this.coroutineRunner = coroutineRunner;
        this.turnText = turnText;
        this.combatLogText = combatLogText;
        this.diceRollUI = diceRollUI;
        this.playerHitUI = playerHitUI;
        this.enemySpriteRenderer = enemySpriteRenderer;
        this.playerFeedbackText = playerFeedbackText;
        this.enemyFeedbackText = enemyFeedbackText;
        this.damageFlashDuration = damageFlashDuration;
        this.damageFlashColor = damageFlashColor;
        this.actionFeedbackColor = actionFeedbackColor;
        this.criticalFeedbackColor = criticalFeedbackColor;
        this.initialActionsUI = initialActionsUI;
        this.attackActionsUI = attackActionsUI;
        this.defenseActionsUI = defenseActionsUI;
        this.specialActionsUI = specialActionsUI;
        this.attackHeartButton = attackHeartButton;
        this.attackBodyButton = attackBodyButton;
        this.attackMindButton = attackMindButton;
        this.instantKillButton = instantKillButton;
        this.learnButton = learnButton;
        this.learnInfoIconButton = learnInfoIconButton;
        this.learnInfoPanel = learnInfoPanel;
        this.learnInfoText = learnInfoText;
        this.playerHud = playerHud;
        this.enemyHud = enemyHud;
        this.gameOverPanel = gameOverPanel;
        this.restartButton = restartButton;

        SetActionsVisible(false);

        if (this.learnInfoIconButton != null)
            this.learnInfoIconButton.gameObject.SetActive(false);

        if (this.learnInfoPanel != null)
            this.learnInfoPanel.SetActive(false);

        ResolveCombatVisualReferences();
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

        yield return coroutineRunner.StartCoroutine(diceRollUI.PlayRollAnimation(value));
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

    public void ShowSpecialMenu()
    {
        SetActionsVisible(true);
        if (initialActionsUI != null)
            initialActionsUI.SetActive(false);
        if (specialActionsUI != null)
            specialActionsUI.SetActive(true);
    }

    public void UpdateAttackButtonAvailability(bool heartEnabled, bool bodyEnabled, bool mindEnabled)
    {
        if (attackHeartButton != null)
            attackHeartButton.interactable = heartEnabled;

        if (attackBodyButton != null)
            attackBodyButton.interactable = bodyEnabled;

        if (attackMindButton != null)
            attackMindButton.interactable = mindEnabled;
    }

    public void UpdateSpecialActionAvailability(bool instantKillEnabled, bool learnEnabled)
    {
        if (instantKillButton != null)
            instantKillButton.interactable = instantKillEnabled;

        if (learnButton != null)
            learnButton.interactable = learnEnabled;
    }

    public void SetEnemyLearnState(int revealLevel, EnemySO enemy, int currentHeart, int currentBody, int currentMind, TurnManagerStats enemyStats)
    {
        bool hasInfo = revealLevel > 0;

        if (learnInfoIconButton != null)
            learnInfoIconButton.gameObject.SetActive(hasInfo);

        if (!hasInfo)
        {
            if (learnInfoPanel != null)
                learnInfoPanel.SetActive(false);
            return;
        }

        if (learnInfoText == null || enemy == null)
            return;

        string basicInfo = $"{enemy.enemyName}\nArquetipo: {enemy.archetype}\nHeart: {currentHeart} | Body: {currentBody} | Mind: {currentMind}";
        if (revealLevel == 1)
        {
            learnInfoText.text = basicInfo;
            return;
        }

        string advancedInfo = $"ATK: {enemyStats.attack} | DEF: {enemyStats.defense}\nCrit: {enemyStats.criticalHitChance}% | Parry: {enemyStats.parryChance}%\nRegra: {enemy.specialRule}";
        learnInfoText.text = $"{basicInfo}\n\n{advancedInfo}";
    }

    public void ResetDiceValue()
    {
        if (diceRollUI != null)
            diceRollUI.ClearValue();
    }

    public void UpdateHud(int playerheart, int playerheartMax, int playerbody, int playerbodyMax, int playermind, int playermindMax, int enemyheart, int enemyheartMax, int enemybody, int enemybodyMax, int enemymind, int enemymindMax)
    {
        playerHud?.SetValues(playerheart, playerheartMax, playerbody, playerbodyMax, playermind, playermindMax);
        enemyHud?.SetValues(enemyheart, enemyheartMax, enemybody, enemybodyMax, enemymind, enemymindMax);
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

        coroutineRunner.StartCoroutine(FlashPlayerUIRoutine(playerHitUI));
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

    public void ToggleLearnInfoPanel()
    {
        if (learnInfoPanel == null)
            return;

        learnInfoPanel.SetActive(!learnInfoPanel.activeSelf);
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

        coroutineRunner.StartCoroutine(FlashSpriteRoutine(spriteRenderer));
    }

    private IEnumerator FlashPlayerUIRoutine(GameObject hitPlayerUI)
    {
        if (hitPlayerUI == null)
            yield break;

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
            coroutineRunner.StopCoroutine(playerFeedbackRoutine);

        if (!isPlayer && enemyFeedbackRoutine != null)
            coroutineRunner.StopCoroutine(enemyFeedbackRoutine);

        Coroutine newRoutine = coroutineRunner.StartCoroutine(ShowFeedbackTextRoutine(feedbackText, value, color));

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

    private void ResolveCombatVisualReferences()
    {
        if (enemySpriteRenderer == null)
        {
            EnemyBattler enemyBattler = UnityEngine.Object.FindObjectOfType<EnemyBattler>();
            if (enemyBattler != null)
                enemySpriteRenderer = enemyBattler.GetComponentInChildren<SpriteRenderer>();
        }

        if (playerFeedbackText != null)
            playerFeedbackText.gameObject.SetActive(false);

        if (enemyFeedbackText != null)
            enemyFeedbackText.gameObject.SetActive(false);
    }
}
