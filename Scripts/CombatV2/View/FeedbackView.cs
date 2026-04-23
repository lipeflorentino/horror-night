using TMPro;
using UnityEngine;

public class FeedbackView : MonoBehaviour
{
    public TMP_Text TurnOwnerText;
    [SerializeField] private PlayerFeedbacks playerFeedbacks;
    [SerializeField] private EnemyFeedbacks enemyFeedbacks;
    [Header("Attack Effect")]
    [SerializeField] private GameObject attackEffectPrefab;
    [SerializeField] private Transform playerAttackEffectAnchor;
    [SerializeField] private Transform enemyAttackEffectAnchor;
    [SerializeField] private float attackEffectDuration = 0.75f;
    void Start()
    {
        playerFeedbacks = FindObjectOfType<PlayerFeedbacks>();
        enemyFeedbacks = FindObjectOfType<EnemyFeedbacks>();

        if (playerFeedbacks == null)
            Debug.LogError("FeedbackView: PlayerFeedbacks component is missing.");

        if (enemyFeedbacks == null)
            Debug.LogError("FeedbackView: EnemyFeedbacks component is missing.");
    }
    public void ShowResolveFeedback(ActionResolutionResult result, bool targetIsPlayer)
    {
        if (!string.IsNullOrWhiteSpace(result.FeedbackText))
        {
            ShowStatusText(result.FeedbackText, targetIsPlayer);
        }

        if (!result.AppliesDamage)
            return;

        if (targetIsPlayer)
        {
            playerFeedbacks.ShowPlayerDamageFlash();
            return;
        }

        enemyFeedbacks.ShowDamagePopup(result.Damage);
    }

    public void ShowAttackEffect(bool attackerIsPlayer)
    {
        Transform anchor = attackerIsPlayer ? playerAttackEffectAnchor : enemyAttackEffectAnchor;
        if (attackEffectPrefab == null || anchor == null)
        {
            Debug.Log("[Feedback] Attack effect prefab or anchor is missing.");
            return;
        }

        GameObject effect = Instantiate(attackEffectPrefab, anchor.position, Quaternion.identity, anchor);
        Destroy(effect, attackEffectDuration);
    }

    private void ShowStatusText(string text, bool targetIsPlayer)
    {
        if (targetIsPlayer)
        {
            playerFeedbacks.ShowStatusText(text);
            return;
        }

        enemyFeedbacks.ShowStatusPopup(text);
    }

    public void ShowHealFeedback(int healAmount)
    {
        // Implement visual feedback for healing
        Debug.Log($"Healed: {healAmount}");
    }

    public void ShowDodgeFeedback()
    {
        // Implement visual feedback for dodging an attack
        Debug.Log("Attack Dodged!");
    }

    public void ShowTurnStartFeedback(bool isPlayerTurn)
    {
        string turnOwner = isPlayerTurn ? "Player's Turn" : "Enemy's Turn";

        if (TurnOwnerText != null)
            TurnOwnerText.text = turnOwner;
    }
}
