using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiceRollView : MonoBehaviour
{
    [Header("Dynamic Dice Slots")]
    [SerializeField] private DiceRollUI diceRollSlotPrefab;
    [SerializeField] private RectTransform playerSlotsContainer;
    [SerializeField] private RectTransform enemySlotsContainer;
    [SerializeField, Min(1)] private int maxSlotsPerSide = 3;
    [SerializeField] private float postRollDelay = 0.4f;
    [SerializeField] private float highlightResultDelay = 2f;
    [SerializeField] private float mergeMoveDuration = 0.3f;
    [SerializeField] private float mergePulseDuration = 0.2f;
    [SerializeField] private float mergeFadeOutDuration = 0.25f;
    [SerializeField] private GameObject diceResolutionPanel;
    [SerializeField] private TMP_Text playerRollTypeLabel, enemyRollTypeLabel;
    [SerializeField] private DiceTierBarUI tierBar;

    private readonly List<DiceRollUI> runtimePlayerSlots = new();
    private readonly List<DiceRollUI> runtimeEnemySlots = new();
    private bool slotsInitialized;

    public IEnumerator PlayDiceResolution(
        IReadOnlyList<DiceResult> playerRolls,
        IReadOnlyList<DiceResult> enemyRolls,
        DiceRollType rollType,
        (int lowMax, int mediumMax, int highMin, int maxValue) tierBoundaries)
    {
        EnsureSlotsInitialized();
        SetupResolutionPanel(rollType, tierBoundaries);

        List<DiceResult> playerIndividualRolls = FlattenRolls(playerRolls);
        yield return PlayIndividualRollAnimations(playerIndividualRolls, runtimePlayerSlots, playerSlotsContainer);

        List<DiceResult> enemyIndividualRolls = FlattenRolls(enemyRolls);
        yield return PlayIndividualRollAnimations(enemyIndividualRolls, runtimeEnemySlots, enemySlotsContainer);

        yield return new WaitForSeconds(postRollDelay);

        List<DiceRollUI> activeFinalSlots = null;
        yield return PlayMergeStep(playerRolls, runtimePlayerSlots, result => activeFinalSlots = result);

        HighlightBestResult(activeFinalSlots, playerRolls, tierBoundaries.maxValue);

        yield return new WaitForSeconds(highlightResultDelay);

        tierBar.SetIndicatorVisible(false);
        ShowDiceResolution(false);
    }

    private void SetupResolutionPanel(DiceRollType rollType, (int lowMax, int mediumMax, int highMin, int maxValue) tierBoundaries)
    {
        ShowDiceResolution(true);
        UpdateRollTypeLabel(rollType);
        tierBar.SetBoundaries(tierBoundaries.lowMax, tierBoundaries.mediumMax, tierBoundaries.highMin, tierBoundaries.maxValue);
    }

    private List<DiceResult> FlattenRolls(IReadOnlyList<DiceResult> rolls)
    {
        List<DiceResult> individualRolls = new();
        if (rolls == null)
            return individualRolls;

        for (int i = 0; i < rolls.Count; i++)
        {
            var roll = rolls[i];
            if (roll.SubRolls != null && roll.SubRolls.Count > 0)
                individualRolls.AddRange(roll.SubRolls);
            else
                individualRolls.Add(roll);
        }

        return individualRolls;
    }

    private IEnumerator PlayIndividualRollAnimations(List<DiceResult> individualRolls, List<DiceRollUI> slots, RectTransform container)
    {
        PrepareSlots(slots, individualRolls.Count, container);
        HighlightExtraDices(slots, individualRolls);

        List<Coroutine> runningCoroutines = new();
        for (int i = 0; i < individualRolls.Count; i++)
        {
            slots[i].SetDiceIcon(individualRolls[i].StatType);
            runningCoroutines.Add(StartCoroutine(slots[i].PlayRollAnimation(individualRolls[i].Value, individualRolls[i].MaxValue)));
        }

        for (int i = 0; i < runningCoroutines.Count; i++)
            yield return runningCoroutines[i];
    }

    private IEnumerator PlayMergeStep(IReadOnlyList<DiceResult> playerRolls, List<DiceRollUI> individualSlots, System.Action<List<DiceRollUI>> onComplete)
    {
        List<DiceRollUI> activeFinalSlots = new();

        if (playerRolls == null)
        {
            onComplete?.Invoke(activeFinalSlots);
            yield break;
        }

        playerSlotsContainer.TryGetComponent<HorizontalLayoutGroup>(out var layoutGroup);
        if (layoutGroup != null)
            layoutGroup.enabled = false;

        List<Coroutine> mergeCoroutines = new();
        int individualIndex = 0;

        for (int i = 0; i < playerRolls.Count; i++)
        {
            DiceResult aggregatedRoll = playerRolls[i];
            int subCount = aggregatedRoll.SubRolls != null && aggregatedRoll.SubRolls.Count > 1
                ? aggregatedRoll.SubRolls.Count
                : 1;

            DiceRollUI baseSlot = individualSlots[individualIndex];

            if (subCount > 1)
            {
                Vector2 targetPosition = baseSlot.RectTransform.anchoredPosition;

                for (int j = 1; j < subCount; j++)
                {
                    DiceRollUI slotToMerge = individualSlots[individualIndex + j];
                    mergeCoroutines.Add(StartCoroutine(MergeSlotIntoBase(slotToMerge, targetPosition)));
                }

                mergeCoroutines.Add(StartCoroutine(FinalizeMergedSlot(baseSlot, aggregatedRoll)));
            }

            individualIndex += subCount;
            activeFinalSlots.Add(baseSlot);
        }

        for (int i = 0; i < mergeCoroutines.Count; i++)
            yield return mergeCoroutines[i];

        if (layoutGroup != null)
            layoutGroup.enabled = true;

        onComplete?.Invoke(activeFinalSlots);
    }

    private IEnumerator MergeSlotIntoBase(DiceRollUI slot, Vector2 targetPosition)
    {
        yield return StartCoroutine(slot.PlayMoveTo(targetPosition, mergeMoveDuration));
        yield return StartCoroutine(slot.PlayFadeOut(mergeFadeOutDuration));
    }

    private IEnumerator FinalizeMergedSlot(DiceRollUI baseSlot, DiceResult aggregatedRoll)
    {
        yield return new WaitForSeconds(mergeMoveDuration);
        baseSlot.SetValueText(aggregatedRoll.Value);
        baseSlot.SetExtra(false);
        yield return StartCoroutine(baseSlot.PlayPulse(mergePulseDuration));
    }

    private void HighlightResolvedRollStates(List<DiceRollUI> slots, IReadOnlyList<DiceResult> rolls)
    {
        if (slots == null || rolls == null)
            return;

        int usedCount = Mathf.Min(slots.Count, rolls.Count);
        for (int i = 0; i < usedCount; i++)
        {
            DiceResult roll = rolls[i];
            slots[i].SetMaxRoll(roll.IsMaxRoll);
        }

        for (int i = usedCount; i < slots.Count; i++)
        {
            slots[i].SetMaxRoll(false);
        }
    }

    private void HighlightExtraDices(List<DiceRollUI> slots, IReadOnlyList<DiceResult> rolls)
    {
        if (slots == null || rolls == null)
            return;

        for (int i = 0; i < slots.Count; i++)
        {
            bool isExtra = i < rolls.Count && rolls[i].IsExtra;
            slots[i].SetExtra(isExtra);
        }
    }

    private bool HasExtraSubRoll(DiceResult roll)
    {
        if (roll?.SubRolls == null)
            return false;

        for (int i = 0; i < roll.SubRolls.Count; i++)
        {
            if (roll.SubRolls[i].IsExtra)
                return true;
        }

        return false;
    }

    private void HighlightBestResult(List<DiceRollUI> activeFinalSlots, IReadOnlyList<DiceResult> playerRolls, int maxValue)
    {
        int usedCount = playerRolls != null ? playerRolls.Count : 0;
        int highlightedIndex = GetHighlightedRollIndex(playerRolls, usedCount);
        SetHighlightedIndex(activeFinalSlots, highlightedIndex);
        HighlightResolvedRollStates(activeFinalSlots, playerRolls);

        tierBar.SetRollIndicatorPosition(GetBetterRollValue(playerRolls, usedCount), maxValue);
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

        //ConfigureContainerLayout(playerSlotsContainer, TextAnchor.MiddleRight);
        //ConfigureContainerLayout(enemySlotsContainer, TextAnchor.MiddleLeft);
        CreateSlots(runtimePlayerSlots, playerSlotsContainer);
        CreateSlots(runtimeEnemySlots, enemySlotsContainer);

        slotsInitialized = true;
    }

    private void ConfigureContainerLayout(RectTransform container, TextAnchor alignment)
    {
        if (!container.TryGetComponent<HorizontalLayoutGroup>(out var layoutGroup))
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

    private void PreparePool(List<DiceRollUI> slots, int neededCount, RectTransform container)
    {
        while (slots.Count < neededCount)
        {
            DiceRollUI slot = Instantiate(diceRollSlotPrefab, container);
            slot.gameObject.SetActive(false);
            slots.Add(slot);
        }
    }

    private void PrepareSlots(List<DiceRollUI> slots, int neededCount, RectTransform container)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].gameObject.SetActive(false);
            slots[i].ClearValue();
        }

        PreparePool(slots, neededCount, container);

        for (int i = 0; i < neededCount; i++)
        {
            slots[i].gameObject.SetActive(true);
        }
    }

    private void SetHighlightedIndex(List<DiceRollUI> slots, int highlightedIndex)
    {
        for (int i = 0; i < slots.Count; i++)
            slots[i].SetHighlighted(i == highlightedIndex);
    }

    private void UpdateRollTypeLabel(DiceRollType rollType)
    {
        if (playerRollTypeLabel != null)
        {
            playerRollTypeLabel.text = rollType == DiceRollType.Accuracy 
                ? "<color=#FFFB00>Accuracy Roll</color>" 
                : "<color=#EAA00E>Power Roll</color>";
        }
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

    public int GetBetterRollValue(IReadOnlyList<DiceResult> rolls, int usedCount)
    {
        if (rolls == null || usedCount <= 0)
            return -1;

        int bestIndex = 0;
        for (int i = 1; i < usedCount; i++)
        {
            if (IsBetterRoll(rolls[i], rolls[bestIndex]))
                bestIndex = i;
        }

        return rolls[bestIndex].Value;
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