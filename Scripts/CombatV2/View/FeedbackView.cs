using TMPro;
using UnityEngine;

public class FeedbackView : MonoBehaviour
{
    public TMP_Text TurnOwnerText;
    [SerializeField] private PlayerFeedbacks playerFeedbacks;
    [SerializeField] private EnemyFeedbacks enemyFeedbacks;
    void Start()
    {
        playerFeedbacks = FindObjectOfType<PlayerFeedbacks>();
        enemyFeedbacks = FindObjectOfType<EnemyFeedbacks>();

        if (playerFeedbacks == null)
            Debug.LogError("FeedbackView: PlayerFeedbacks component is missing.");

        if (enemyFeedbacks == null)
            Debug.LogError("FeedbackView: EnemyFeedbacks component is missing.");
    }
    public void ShowDamageFeedback(int damage, bool targetIsPlayer)
    {
        Debug.Log($"Damage Dealt: {damage}");
        if (damage <= 0)
            return;

        if (targetIsPlayer)
        {
            playerFeedbacks.ShowPlayerDamageFlash();
            return;
        }

        enemyFeedbacks.ShowDamagePopup(damage);
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