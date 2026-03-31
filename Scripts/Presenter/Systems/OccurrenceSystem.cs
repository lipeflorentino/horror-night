using UnityEngine;

public class OccurrenceSystem : MonoBehaviour
{
    private const int MinRollRange = 1;

    [Header("References")]
    [SerializeField] private OccurrenceDatabase database;
    [SerializeField] private UIOccurrencePopup popup;
    [SerializeField] private PlayerGridMovement playerMovement;
    [SerializeField] private PlayerStatusManager playerStatus;

    private void Awake()
    {
        if (popup == null)
            popup = FindObjectOfType<UIOccurrencePopup>();

        if (playerMovement == null)
            playerMovement = FindObjectOfType<PlayerGridMovement>();

        if (playerStatus == null)
            playerStatus = FindObjectOfType<PlayerStatusManager>();

        if (database == null)
            database = FindObjectOfType<OccurrenceDatabase>();
    }

    public void TriggerOccurrence(LevelNode node, LevelSO level)
    {
        if (node == null || database == null)
            return;

        OccurrenceSO selectedOccurrence = database.GetRandom();

        if (selectedOccurrence == null)
            return;

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (popup == null)
        {
            ResolveOccurrence(selectedOccurrence, 2);
            if (playerMovement != null)
                playerMovement.enabled = true;
            return;
        }

        popup.Show(
            selectedOccurrence,
            onOptionSelected: selectedOption => ResolveOccurrence(selectedOccurrence, selectedOption),
            onClose: () =>
            {
                if (playerMovement != null)
                    playerMovement.enabled = true;
            });
    }

    private OccurrenceResult ResolveOccurrence(OccurrenceSO selectedOccurrence, int selectedOption)
    {
        OccurrenceResult result = new OccurrenceResult
        {
            selectedOption = selectedOption,
            requiresRoll = selectedOccurrence.requiresRoll,
            optionText = GetOptionText(selectedOccurrence, selectedOption),
            primaryStat = selectedOccurrence.primaryStat,
            successText = selectedOccurrence.successText,
            failText = selectedOccurrence.failText
        };

        if (playerStatus == null)
            return result;

        if (selectedOption == 0)
            playerStatus.AddArchetypePoint(selectedOccurrence.profile1Type);
        else if (selectedOption == 1)
            playerStatus.AddArchetypePoint(selectedOccurrence.profile2Type);

        if (!selectedOccurrence.requiresRoll)
        {
            result.success = true;
            return result;
        }

        int playerStatValue = playerStatus.GetStatValue(selectedOccurrence.primaryStat);
        int rollRange = Mathf.Max(MinRollRange, playerStatValue);

        int targetRoll = Random.Range(1, rollRange + 1);
        int playerRoll = Random.Range(1, rollRange + 1);
        bool success = playerRoll >= targetRoll;

        int delta = Mathf.Max(1, selectedOccurrence.tier);
        if (!success)
            delta = -delta;

        playerStatus.ApplyStatDelta(selectedOccurrence.primaryStat, delta);

        result.success = success;
        result.occurrenceRoll = targetRoll;
        result.playerRoll = playerRoll;
        result.delta = delta;
        result.rollRange = rollRange;

        return result;
    }

    private static string GetOptionText(OccurrenceSO occurrence, int selectedOption)
    {
        return selectedOption switch
        {
            0 => occurrence.profileOption1,
            1 => occurrence.profileOption2,
            _ => occurrence.neutralOption
        };
    }
}
