using UnityEngine;

public class OccurrenceSystem : MonoBehaviour
{
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

        OccurrenceSO selectedOccurrence = database != null ? database.GetRandom() : null;

        if (selectedOccurrence == null)
            return;

        int occurrenceRoll = Random.Range(0, Mathf.Max(0, selectedOccurrence.rollRange) + 1);

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (popup == null)
        {
            ResolveOccurrence(selectedOccurrence, occurrenceRoll);
            if (playerMovement != null)
                playerMovement.enabled = true;
            return;
        }

        popup.Show(
            selectedOccurrence,
            occurrenceRoll,
            onRoll: () => ResolveOccurrence(selectedOccurrence, occurrenceRoll),
            onClose: () =>
            {
                if (playerMovement != null)
                    playerMovement.enabled = true;
            });
    }

    private OccurrenceResult ResolveOccurrence(OccurrenceSO selectedOccurrence, int occurrenceRoll)
    {
        int playerStatValue = playerStatus != null ? playerStatus.GetStatValue(selectedOccurrence.successStat) : 0;
        int playerRoll = Random.Range(0, Mathf.Max(0, playerStatValue) + 1);
        bool success = playerRoll > occurrenceRoll;

        if (playerStatus != null)
        {
            if (success)
                playerStatus.ApplyStatDelta(selectedOccurrence.successStat, selectedOccurrence.successValue);
            else
                playerStatus.ApplyStatDelta(selectedOccurrence.failStat, selectedOccurrence.failValue);
        }

        return new OccurrenceResult
        {
            success = success,
            occurrenceRoll = occurrenceRoll,
            playerRoll = playerRoll,
            successText = selectedOccurrence.successText,
            failText = selectedOccurrence.failText
        };
    }
}
