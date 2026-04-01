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

    [Serializable]
    public class PlayerActionIconBinding
    {
        public PlayerActionType action;
        public Sprite icon;
    }

    [Serializable]
    public class EnemyActionIconBinding
    {
        public EnemyActionType action;
        public Sprite icon;
    }

    private readonly MonoBehaviour coroutineRunner;
    private readonly TMP_Text turnText;
    private readonly TMP_Text combatLogText;
    private readonly DiceRollUI diceRollUI;

    private readonly GameObject playerHitUI;
    private SpriteRenderer enemySpriteRenderer;
    private readonly TMP_Text playerFeedbackText;
    private readonly TMP_Text enemyFeedbackText;
    private readonly Image playerActionIcon;
    private readonly Image enemyActionIcon;
    private readonly Dictionary<PlayerActionType, Sprite> playerActionIcons;
    private readonly Dictionary<EnemyActionType, Sprite> enemyActionIcons;
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
    private Vector3 playerFeedbackBaseScale = Vector3.one;
    private Vector3 enemyFeedbackBaseScale = Vector3.one;
    private bool hasPlayerFeedbackBaseScale;
    private bool hasEnemyFeedbackBaseScale;

    public CombatUI(
        MonoBehaviour coroutineRunner,
        TMP_Text turnText,
        TMP_Text combatLogText,
        DiceRollUI diceRollUI,
        GameObject playerHitUI,
        SpriteRenderer enemySpriteRenderer,
        TMP_Text playerFeedbackText,
        TMP_Text enemyFeedbackText,
        Image playerActionIcon,
        Image enemyActionIcon,
        PlayerActionIconBinding[] playerActionIconMappings,
        EnemyActionIconBinding[] enemyActionIconMappings,
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
        Button attackBackButton,
        Button defenseBackButton,
        Button specialBackButton,
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
        this.playerActionIcon = playerActionIcon;
        this.enemyActionIcon = enemyActionIcon;
        playerActionIcons = BuildPlayerActionIconMap(playerActionIconMappings);
        enemyActionIcons = BuildEnemyActionIconMap(enemyActionIconMappings);
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

    public void ShowInitialMenu()
    {
        SetActionsVisible(true);
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

    public void NotifyPlayerAction(string actionText, PlayerActionType? actionType = null)
    {
        if (string.IsNullOrWhiteSpace(actionText))
            return;

        Sprite actionIcon = actionType.HasValue ? GetPlayerActionIcon(actionType.Value) : null;
        ShowFeedbackText(true, actionText, actionFeedbackColor, true, actionIcon);
    }

    public void NotifyEnemyAction(string actionText, EnemyActionType? actionType = null)
    {
        if (string.IsNullOrWhiteSpace(actionText))
            return;

        Sprite actionIcon = actionType.HasValue ? GetEnemyActionIcon(actionType.Value) : null;
        ShowFeedbackText(false, actionText, actionFeedbackColor, true, actionIcon);
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

    private void ShowFeedbackText(bool isPlayer, string value, Color color, bool isAction = false, Sprite actionIcon = null)
    {
        TMP_Text feedbackText = isPlayer ? playerFeedbackText : enemyFeedbackText;
        Image feedbackIcon = isPlayer ? playerActionIcon : enemyActionIcon;
        
        if (feedbackText == null)
            return;

        if (isPlayer && playerFeedbackRoutine != null)
            coroutineRunner.StopCoroutine(playerFeedbackRoutine);

        if (!isPlayer && enemyFeedbackRoutine != null)
            coroutineRunner.StopCoroutine(enemyFeedbackRoutine);

        Vector3 baseScale = GetFeedbackBaseScale(isPlayer, feedbackText);
        feedbackText.transform.localScale = baseScale;

        Coroutine newRoutine = coroutineRunner.StartCoroutine(ShowFeedbackTextRoutine(feedbackText, feedbackIcon, value, color, isAction, actionIcon, baseScale));

        if (isPlayer)
            playerFeedbackRoutine = newRoutine;
        else
            enemyFeedbackRoutine = newRoutine;
    }

    private IEnumerator ShowFeedbackTextRoutine(TMP_Text feedbackText, Image feedbackIcon, string value, Color color, bool isAction, Sprite actionIcon, Vector3 baseScale)
    {
        feedbackText.gameObject.SetActive(true);
        feedbackText.color = color;
        feedbackText.text = value;
        if (feedbackIcon != null)
        {
            bool hasActionIcon = isAction && actionIcon != null;
            feedbackIcon.sprite = actionIcon;
            feedbackIcon.gameObject.SetActive(hasActionIcon);
        }

        if (isAction)
        {
            Transform target = feedbackText.transform;
            target.localScale = baseScale * 0.85f;
            yield return new WaitForSeconds(0.08f);
            target.localScale = baseScale * 1.1f;
            yield return new WaitForSeconds(0.08f);
            target.localScale = baseScale;
        }

        yield return new WaitForSeconds(0.7f);
        feedbackText.gameObject.SetActive(false);
        if (feedbackIcon != null)
            feedbackIcon.gameObject.SetActive(false);
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
        {
            playerFeedbackBaseScale = playerFeedbackText.transform.localScale;
            hasPlayerFeedbackBaseScale = true;
        }

        if (enemyFeedbackText != null)
        {
            enemyFeedbackBaseScale = enemyFeedbackText.transform.localScale;
            hasEnemyFeedbackBaseScale = true;
        }

        if (playerFeedbackText != null)
            playerFeedbackText.gameObject.SetActive(false);

        if (enemyFeedbackText != null)
            enemyFeedbackText.gameObject.SetActive(false);
    }

    private Vector3 GetFeedbackBaseScale(bool isPlayer, TMP_Text feedbackText)
    {
        if (feedbackText == null)
            return Vector3.one;

        if (isPlayer)
        {
            if (!hasPlayerFeedbackBaseScale)
            {
                playerFeedbackBaseScale = feedbackText.transform.localScale;
                hasPlayerFeedbackBaseScale = true;
            }

            return playerFeedbackBaseScale;
        }

        if (!hasEnemyFeedbackBaseScale)
        {
            enemyFeedbackBaseScale = feedbackText.transform.localScale;
            hasEnemyFeedbackBaseScale = true;
        }

        return enemyFeedbackBaseScale;
    }

    private Sprite GetPlayerActionIcon(PlayerActionType action)
    {
        if (playerActionIcons.TryGetValue(action, out Sprite icon))
            return icon;

        return null;
    }

    private Sprite GetEnemyActionIcon(EnemyActionType action)
    {
        if (enemyActionIcons.TryGetValue(action, out Sprite icon))
            return icon;

        return null;
    }

    private static Dictionary<PlayerActionType, Sprite> BuildPlayerActionIconMap(PlayerActionIconBinding[] mappings)
    {
        Dictionary<PlayerActionType, Sprite> map = new();
        if (mappings == null)
            return map;

        foreach (PlayerActionIconBinding binding in mappings)
        {
            if (binding == null || binding.icon == null)
                continue;

            map[binding.action] = binding.icon;
        }

        return map;
    }

    private static Dictionary<EnemyActionType, Sprite> BuildEnemyActionIconMap(EnemyActionIconBinding[] mappings)
    {
        Dictionary<EnemyActionType, Sprite> map = new();
        if (mappings == null)
            return map;

        foreach (EnemyActionIconBinding binding in mappings)
        {
            if (binding == null || binding.icon == null)
                continue;

            map[binding.action] = binding.icon;
        }

        return map;
    }
}
