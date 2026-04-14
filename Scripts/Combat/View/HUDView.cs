using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HudView : MonoBehaviour
{
    [Header("Dice & Resources")]
    [SerializeField] private TMP_Text diceText;
    [SerializeField] private CombatDiceRollUI diceRollUI;
    
    [Header("Player Status")]
    [SerializeField] private TMP_Text playerHpText;
    [SerializeField] private Image playerHpSlider;
    [SerializeField] private TMP_Text playerHeartText;
    [SerializeField] private TMP_Text playerBodyText;
    [SerializeField] private TMP_Text playerMindText;
    
    [Header("Enemy Status")]
    [SerializeField] private TMP_Text enemyHpText;
    [SerializeField] private Image enemyHpSlider;
    
    [Header("Feedback")]
    [SerializeField] private TMP_Text damagePopupText;
    [SerializeField] private TMP_Text actionFeedbackText;
    [SerializeField] private CanvasGroup damagePopupCanvasGroup;

    private HUDAnimator hudAnimator;
    private TurnManager turnManager;

    private void Awake()
    {
        hudAnimator = new HUDAnimator();
    }

    public void UpdateDice(int value)
    {
        if (diceText == null)
            return;

        diceText.text = value.ToString();
    }
    
    public void UpdatePlayerHP(int currentHP, int maxHP)
    {
        if (playerHpText != null)
            playerHpText.text = $"{currentHP}/{maxHP}";

        if (playerHpSlider != null)
        {
            playerHpSlider.fillAmount = maxHP <= 0f ? 0f : Mathf.Clamp01((float)currentHP / maxHP);
        }
    }
    
    public void UpdatePlayerResources(int heart, int body, int mind)
    {
        if (playerHeartText != null)
            playerHeartText.text = heart.ToString();

        if (playerBodyText != null)
            playerBodyText.text = body.ToString();

        if (playerMindText != null)
            playerMindText.text = mind.ToString();
    }
    public void UpdateEnemyHP(int currentHP, int maxHP)
    {
        if (enemyHpText != null)
            enemyHpText.text = $"{currentHP}/{maxHP}";

        if (enemyHpSlider != null)
        {
            enemyHpSlider.fillAmount = maxHP <= 0f ? 0f : Mathf.Clamp01((float)currentHP / maxHP);
        }
    }
    
    public void ShowDamagePopup(int damageAmount)
    {
        if (damagePopupText != null && damagePopupCanvasGroup != null)
        {
            damagePopupText.text = $"-{damageAmount}";
            damagePopupText.color = Color.red;
            StartCoroutine(hudAnimator.AnimateDamagePopup(damagePopupCanvasGroup, 0.5f));
        }
    }
    
    public void ShowHealingPopup(int healAmount)
    {
        if (damagePopupText != null && damagePopupCanvasGroup != null)
        {
            damagePopupText.text = $"+{healAmount}";
            damagePopupText.color = Color.green;
            StartCoroutine(hudAnimator.AnimateDamagePopup(damagePopupCanvasGroup, 0.5f));
        }
    }
    
    public void ShowResourceSpent(int amount, Color resourceColor)
    {
        if (damagePopupText != null && damagePopupCanvasGroup != null)
        {
            damagePopupText.text = $"- {amount}";
            damagePopupText.color = resourceColor;
            StartCoroutine(hudAnimator.AnimateDamagePopup(damagePopupCanvasGroup, 0.4f));
        }
    }
    
    public void ShowActionFeedback(string actionName)
    {
        if (actionFeedbackText != null)
        {
            actionFeedbackText.text = actionName;
            StartCoroutine(hudAnimator.AnimateActionFeedback(actionFeedbackText, 1f));
        }
    }
    
    public void PlayDamageShake()
    {
        StartCoroutine(hudAnimator.AnimateShake(transform, 0.2f, 5f));
    }

    /// <summary>
    /// Define a referência do TurnManager para sincronizar dados disponíveis.
    /// </summary>
    public void SetTurnManager(TurnManager turnManager)
    {
        this.turnManager = turnManager;
        if (turnManager != null)
            UpdateAvailableDice(turnManager.availableDice);
    }

    /// <summary>
    /// Atualiza o display de dados disponíveis no HUD.
    /// </summary>
    public void UpdateAvailableDice(int availableDiceCount)
    {
        if (diceText != null)
            diceText.text = availableDiceCount.ToString();
    }

    /// <summary>
    /// Toca animação de um dado sendo rolado.
    /// </summary>
    public void PlaySingleDiceRoll(int finalValue)
    {
        if (diceRollUI != null)
            StartCoroutine  (diceRollUI.PlaySingleDiceRoll(finalValue));
    }

    /// <summary>
    /// Toca animação de múltiplos dados sendo rolados simultaneamente.
    /// </summary>
    public void PlayMultipleDiceRoll(int[] finalValues)
    {
        if (diceRollUI != null)
            StartCoroutine(diceRollUI.PlayMultipleDiceRoll(finalValues));
    }
}

