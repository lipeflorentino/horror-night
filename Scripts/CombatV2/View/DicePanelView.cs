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

    private readonly List<DiceRollUI> runtimePlayerSlots = new();
    private readonly List<DiceRollUI> runtimeEnemySlots = new();
    private bool slotsInitialized;

    public IEnumerator PlayDiceResolution(IReadOnlyList<DiceResult> playerRolls, IReadOnlyList<DiceResult> enemyRolls)
    {
        EnsureSlotsInitialized();
        ShowDiceResolution(true);

        List<Coroutine> runningCoroutines = new();

        int playerCount = Mathf.Min(playerRolls.Count, maxSlotsPerSide);
        int enemyCount = Mathf.Min(enemyRolls.Count, maxSlotsPerSide);

        PrepareSlots(runtimePlayerSlots, playerCount);
        PrepareSlots(runtimeEnemySlots, enemyCount);

        for (int i = 0; i < playerCount; i++)
            runningCoroutines.Add(StartCoroutine(runtimePlayerSlots[i].PlayRollAnimation(playerRolls[i].Value)));

        for (int i = 0; i < enemyCount; i++)
            runningCoroutines.Add(StartCoroutine(runtimeEnemySlots[i].PlayRollAnimation(enemyRolls[i].Value)));

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