using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FeedbackView : MonoBehaviour
{
    public TMP_Text TurnOwnerText;
    [SerializeField] private DiceRollUI[] playerDiceRollSlots;
    [SerializeField] private DiceRollUI[] enemyDiceRollSlots;
    [SerializeField] private float postRollDelay = 0.4f;
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

    public IEnumerator PlayDiceResolution(IReadOnlyList<DiceResult> playerRolls, IReadOnlyList<DiceResult> enemyRolls)
    {
        List<Coroutine> runningCoroutines = new();
        PrepareSlots(playerDiceRollSlots, playerRolls.Count);
        PrepareSlots(enemyDiceRollSlots, enemyRolls.Count);

        if (playerDiceRollSlots != null)
            for (int i = 0; i < playerRolls.Count && i < playerDiceRollSlots.Length; i++)
                runningCoroutines.Add(StartCoroutine(playerDiceRollSlots[i].PlayRollAnimation(playerRolls[i].Value)));

        if (enemyDiceRollSlots != null)
            for (int i = 0; i < enemyRolls.Count && i < enemyDiceRollSlots.Length; i++)
                runningCoroutines.Add(StartCoroutine(enemyDiceRollSlots[i].PlayRollAnimation(enemyRolls[i].Value)));

        for (int i = 0; i < runningCoroutines.Count; i++)
            yield return runningCoroutines[i];

        yield return new WaitForSeconds(postRollDelay);
    }

    private void PrepareSlots(DiceRollUI[] slots, int usedCount)
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            bool isActive = i < usedCount;
            slots[i].gameObject.SetActive(isActive);

            if (isActive)
                slots[i].ClearValue();
        }
    }
}