using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DicePanelView : MonoBehaviour
{
    [Header("Dynamic Dice Slots")]
    [SerializeField] private DiceRollUI diceRollSlotPrefab;
    [SerializeField] private RectTransform playerSlotsContainer;
    [SerializeField] private RectTransform enemySlotsContainer;
    [SerializeField, Min(1)] private int maxSlotsPerSide = 3;
    [SerializeField] private float postRollDelay = 0.4f;
    [SerializeField] private GameObject diceResolutionPanel;
    [SerializeField] private Color playerPowerColor = Color.green;
    [SerializeField] private Color playerAccuracyColor = Color.blue;
    [SerializeField] private Color enemyPowerColor = new(0.6f, 0.3f, 0.9f);
    [SerializeField] private Color enemyAccuracyColor = Color.red;

    private readonly List<DiceRollUI> runtimePlayerSlots = new();
    private readonly List<DiceRollUI> runtimeEnemySlots = new();
    private bool slotsInitialized;

    public IEnumerator PlayDiceResolution(
        IReadOnlyList<DiceResult> playerPowerRolls,
        IReadOnlyList<DiceResult> playerAccuracyRolls,
        IReadOnlyList<DiceResult> enemyPowerRolls,
        IReadOnlyList<DiceResult> enemyAccuracyRolls)
    {
        EnsureSlotsInitialized();
        ShowDiceResolution(true);

        List<Coroutine> runningCoroutines = new();

        int playerPowerCount = playerPowerRolls != null ? playerPowerRolls.Count : 0;
        int playerAccuracyCount = playerAccuracyRolls != null ? playerAccuracyRolls.Count : 0;
        int enemyPowerCount = enemyPowerRolls != null ? enemyPowerRolls.Count : 0;
        int enemyAccuracyCount = enemyAccuracyRolls != null ? enemyAccuracyRolls.Count : 0;

        int playerCount = Mathf.Min(playerPowerCount + playerAccuracyCount, maxSlotsPerSide);
        int enemyCount = Mathf.Min(enemyPowerCount + enemyAccuracyCount, maxSlotsPerSide);

        PrepareSlots(runtimePlayerSlots, playerCount);
        PrepareSlots(runtimeEnemySlots, enemyCount);

        int slotIndex = 0;
        for (int i = 0; i < playerPowerCount && slotIndex < playerCount; i++, slotIndex++)
        {
            runtimePlayerSlots[slotIndex].SetDiceColor(playerPowerColor);
            runningCoroutines.Add(StartCoroutine(runtimePlayerSlots[slotIndex].PlayRollAnimation(playerPowerRolls[i].Value)));
        }

        for (int i = 0; i < playerAccuracyCount && slotIndex < playerCount; i++, slotIndex++)
        {
            runtimePlayerSlots[slotIndex].SetDiceColor(playerAccuracyColor);
            runningCoroutines.Add(StartCoroutine(runtimePlayerSlots[slotIndex].PlayRollAnimation(playerAccuracyRolls[i].Value)));
        }

        slotIndex = 0;
        for (int i = 0; i < enemyPowerCount && slotIndex < enemyCount; i++, slotIndex++)
        {
            runtimeEnemySlots[slotIndex].SetDiceColor(enemyPowerColor);
            runningCoroutines.Add(StartCoroutine(runtimeEnemySlots[slotIndex].PlayRollAnimation(enemyPowerRolls[i].Value)));
        }

        for (int i = 0; i < enemyAccuracyCount && slotIndex < enemyCount; i++, slotIndex++)
        {
            runtimeEnemySlots[slotIndex].SetDiceColor(enemyAccuracyColor);
            runningCoroutines.Add(StartCoroutine(runtimeEnemySlots[slotIndex].PlayRollAnimation(enemyAccuracyRolls[i].Value)));
        }

        for (int i = 0; i < runningCoroutines.Count; i++)
            yield return runningCoroutines[i];

        yield return new WaitForSeconds(postRollDelay);
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

    private void PrepareSlots(List<DiceRollUI> slots, int usedCount)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            bool isActive = i < usedCount;
            slots[i].gameObject.SetActive(isActive);

            if (isActive)
                slots[i].ClearValue();
        }
    }

    public void HidePanel()
    {
        ShowDiceResolution(false);
    }
}
