using TMPro;
using UnityEngine;

public class FeedbackView : MonoBehaviour
{
    public TMP_Text TurnOwnerText;
    public void ShowDamageFeedback(int damage)
    {
        // Implement visual feedback for damage (e.g., floating text, screen shake)
        Debug.Log($"Damage Dealt: {damage}");
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