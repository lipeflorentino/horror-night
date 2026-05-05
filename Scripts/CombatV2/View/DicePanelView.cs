using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DicePanelView : MonoBehaviour
{
    [Header("Dynamic Dice Slots")]
    [SerializeField] private DiceRollUI diceRollSlotPrefab;
    [SerializeField] private RectTransform playerSlotsContainer;
    [SerializeField] private RectTransform enemySlotsContainer;
    [SerializeField, Min(1)] private int maxSlotsPerSide = 3;
    [SerializeField] private float postRollDelay = 0.4f;
    [SerializeField] private float highlightResultDelay = 2f;
    [SerializeField] private GameObject diceResolutionPanel;
    [SerializeField] private TMP_Text playerRollTypeLabel, enemyRollTypeLabel;

    private readonly List<DiceRollUI> runtimePlayerSlots = new();
    private readonly List<DiceRollUI> runtimeEnemySlots = new();
    private bool slotsInitialized;

    public IEnumerator PlayDiceResolution(
        IReadOnlyList<DiceResult> playerRolls,
        IReadOnlyList<DiceResult> enemyRolls,
        DiceRollType rollType)
    {
        EnsureSlotsInitialized();
        ShowDiceResolution(true);
        UpdateRollTypeLabel(rollType);

        List<Coroutine> runningCoroutines = new();

        int playerCount = Mathf.Min(playerRolls != null ? playerRolls.Count : 0, maxSlotsPerSide);
        int enemyCount = Mathf.Min(enemyRolls != null ? enemyRolls.Count : 0, maxSlotsPerSide);
        int playerHighlightedIndex = GetHighlightedRollIndex(playerRolls, playerCount);
        int enemyHighlightedIndex = GetHighlightedRollIndex(enemyRolls, enemyCount);

        PrepareSlots(runtimePlayerSlots, playerCount, playerHighlightedIndex);
        PrepareSlots(runtimeEnemySlots, enemyCount, enemyHighlightedIndex);
        EnqueueRollAnimations(runtimePlayerSlots, playerRolls, playerCount, runningCoroutines);
        EnqueueRollAnimations(runtimeEnemySlots, enemyRolls, enemyCount, runningCoroutines);

        for (int i = 0; i < runningCoroutines.Count; i++)
            yield return runningCoroutines[i];

        yield return new WaitForSeconds(postRollDelay);
        SetHighlightedIndex(runtimePlayerSlots, playerHighlightedIndex, playerCount);
        SetHighlightedIndex(runtimeEnemySlots, enemyHighlightedIndex, enemyCount);
        yield return new WaitForSeconds(highlightResultDelay);
        ShowDiceResolution(false);
    }

    public void ShowDiceResolution(bool status)
    {
        if (diceResolutionPanel != null)
            diceResolutionPanel.SetActive(status);
    }

    private void EnsureSlotsInitialized()
    {
        if (slotsInitialized)
            return;

        if (diceRollSlotPrefab == null || playerSlotsContainer == null || enemySlotsContainer == null)
        {
            Debug.LogWarning("DicePanelView: Missing prefab/container references for dynamic dice slots.");
            slotsInitialized = true;
            return;
        }

        ConfigureContainerLayout(playerSlotsContainer, TextAnchor.MiddleRight);
        ConfigureContainerLayout(enemySlotsContainer, TextAnchor.MiddleLeft);

        CreateSlots(runtimePlayerSlots, playerSlotsContainer);
        CreateSlots(runtimeEnemySlots, enemySlotsContainer);

        slotsInitialized = true;
    }

    private void ConfigureContainerLayout(RectTransform container, TextAnchor alignment)
    {
        HorizontalLayoutGroup layoutGroup = container.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup == null)
            layoutGroup = container.gameObject.AddComponent<HorizontalLayoutGroup>();

        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childAlignment = alignment;
    }

    private void CreateSlots(List<DiceRollUI> slotBuffer, RectTransform container)
    {
        slotBuffer.Clear();

        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);

        for (int i = 0; i < maxSlotsPerSide; i++)
        {
            DiceRollUI slot = Instantiate(diceRollSlotPrefab, container);
            slot.gameObject.SetActive(false);
            slotBuffer.Add(slot);
        }
    }

    private void PrepareSlots(List<DiceRollUI> slots, int usedCount, int highlightedIndex)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            bool isActive = i < usedCount;
            slots[i].gameObject.SetActive(isActive);
            slots[i].SetHighlighted(false);
        }
    }

    private void EnqueueRollAnimations(List<DiceRollUI> slots, IReadOnlyList<DiceResult> rolls, int usedCount, List<Coroutine> runningCoroutines)
    {
        if (rolls == null)
            return;

        for (int i = 0; i < usedCount; i++)
        {
            slots[i].SetDiceIcon(rolls[i].StatType);
            runningCoroutines.Add(StartCoroutine(slots[i].PlayRollAnimation(rolls[i].Value, rolls[i].MaxValue)));
        }
    }

    private void SetHighlightedIndex(List<DiceRollUI> slots, int highlightedIndex, int usedCount)
    {
        for (int i = 0; i < usedCount; i++)
            slots[i].SetHighlighted(i == highlightedIndex);
    }

    private void UpdateRollTypeLabel(DiceRollType rollType)
    {
        if (playerRollTypeLabel != null)
            playerRollTypeLabel.text = rollType == DiceRollType.Accuracy ? "Accuracy Roll" : "Power Roll";
        if (enemyRollTypeLabel != null)
            enemyRollTypeLabel.text = rollType == DiceRollType.Accuracy ? "Accuracy Roll" : "Power Roll";
    }

    private int GetHighlightedRollIndex(IReadOnlyList<DiceResult> rolls, int usedCount)
    {
        if (rolls == null || usedCount <= 0)
            return -1;

        int bestIndex = 0;
        for (int i = 1; i < usedCount; i++)
        {
            if (IsBetterRoll(rolls[i], rolls[bestIndex]))
                bestIndex = i;
        }

        return bestIndex;
    }

    private bool IsBetterRoll(DiceResult candidate, DiceResult currentBest)
    {
        if (candidate.Value != currentBest.Value)
            return candidate.Value > currentBest.Value;

        int candidatePriority = GetStatPriority(candidate.StatType);
        int currentPriority = GetStatPriority(currentBest.StatType);
        if (candidatePriority != currentPriority)
            return candidatePriority > currentPriority;

        return false;
    }

    private int GetStatPriority(DiceStatType statType)
    {
        return statType switch
        {
            DiceStatType.Mind => 3,
            DiceStatType.Heart => 2,
            DiceStatType.Body => 1,
            _ => 0
        };
    }

    public void HidePanel()
    {
        ShowDiceResolution(false);
    }
}
