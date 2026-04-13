using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HudView : MonoBehaviour
{
    [Header("Dice & Resources")]
    [SerializeField] private TMP_Text diceText;
    
    [Header("Player Status")]
    [SerializeField] private TMP_Text playerHpText;
    [SerializeField] private Slider playerHpSlider;
    [SerializeField] private TMP_Text playerHeartText;
    [SerializeField] private TMP_Text playerBodyText;
    [SerializeField] private TMP_Text playerMindText;
    
    [Header("Enemy Status")]
    [SerializeField] private TMP_Text enemyHpText;
    [SerializeField] private Slider enemyHpSlider;
    
    [Header("Feedback")]
    [SerializeField] private TMP_Text damagePopupText;
    [SerializeField] private TMP_Text actionFeedbackText;
    [SerializeField] private CanvasGroup damagePopupCanvasGroup;

    private HUDAnimator hudAnimator;

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

    /// <summary>
    /// Atualiza display de HP do jogador.
    /// </summary>
    public void UpdatePlayerHP(int currentHP, int maxHP)
    {
        if (playerHpText != null)
            playerHpText.text = $"{currentHP}/{maxHP}";

        if (playerHpSlider != null)
        {
            playerHpSlider.maxValue = maxHP;
            playerHpSlider.value = currentHP;
        }
    }

    /// <summary>
    /// Atualiza display de recursos (Heart, Body, Mind) do jogador.
    /// </summary>
    public void UpdatePlayerResources(int heart, int body, int mind)
    {
        if (playerHeartText != null)
            playerHeartText.text = heart.ToString();

        if (playerBodyText != null)
            playerBodyText.text = body.ToString();

        if (playerMindText != null)
            playerMindText.text = mind.ToString();
    }

    /// <summary>
    /// Atualiza display de HP do inimigo.
    /// </summary>
    public void UpdateEnemyHP(int currentHP, int maxHP)
    {
        if (enemyHpText != null)
            enemyHpText.text = $"{currentHP}/{maxHP}";

        if (enemyHpSlider != null)
        {
            enemyHpSlider.maxValue = maxHP;
            enemyHpSlider.value = currentHP;
        }
    }

    /// <summary>
    /// Mostra feedback de dano recebido com animação.
    /// Número flutuante em vermelho que desaparece.
    /// </summary>
    public void ShowDamagePopup(int damageAmount)
    {
        if (damagePopupText != null && damagePopupCanvasGroup != null)
        {
            damagePopupText.text = $"-{damageAmount}";
            damagePopupText.color = Color.red;
            StartCoroutine(hudAnimator.AnimateDamagePopup(damagePopupCanvasGroup, 0.5f));
        }
    }

    /// <summary>
    /// Mostra feedback de cura com animação.
    /// Número flutuante em verde.
    /// </summary>
    public void ShowHealingPopup(int healAmount)
    {
        if (damagePopupText != null && damagePopupCanvasGroup != null)
        {
            damagePopupText.text = $"+{healAmount}";
            damagePopupText.color = Color.green;
            StartCoroutine(hudAnimator.AnimateDamagePopup(damagePopupCanvasGroup, 0.5f));
        }
    }

    /// <summary>
    /// Mostra feedback de recurso gasto com animação.
    /// </summary>
    public void ShowResourceSpent(int amount, Color resourceColor)
    {
        if (damagePopupText != null && damagePopupCanvasGroup != null)
        {
            damagePopupText.text = $"- {amount}";
            damagePopupText.color = resourceColor;
            StartCoroutine(hudAnimator.AnimateDamagePopup(damagePopupCanvasGroup, 0.4f));
        }
    }

    /// <summary>
    /// Mostra ação executada no feedback de ação.
    /// </summary>
    public void ShowActionFeedback(string actionName)
    {
        if (actionFeedbackText != null)
        {
            actionFeedbackText.text = actionName;
            StartCoroutine(hudAnimator.AnimateActionFeedback(actionFeedbackText, 1f));
        }
    }

    /// <summary>
    /// Anima um shake de câmera ao receber dano.
    /// </summary>
    public void PlayDamageShake()
    {
        StartCoroutine(hudAnimator.AnimateShake(transform, 0.2f, 5f));
    }
}

