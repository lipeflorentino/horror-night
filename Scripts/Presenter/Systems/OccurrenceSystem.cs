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

    public void TriggerEvent(LevelNode node, LevelSO level)
    {
        if (node == null || database == null)
            return;

        OccurrenceSO selectedEvent = database != null ? database.GetRandom() : null;

        if (selectedEvent == null)
            return;

        int occurrenceRoll = Random.Range(0, Mathf.Max(0, selectedEvent.rollRange) + 1);

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (popup == null)
        {
            ResolveEvent(selectedEvent, occurrenceRoll);
            if (playerMovement != null)
                playerMovement.enabled = true;
            return;
        }

        popup.Show(
            selectedEvent,
            occurrenceRoll,
            onRoll: () => ResolveEvent(selectedEvent, occurrenceRoll),
            onClose: () =>
            {
                if (playerMovement != null)
                    playerMovement.enabled = true;
            });
    }

    private OccurrenceResult ResolveEvent(OccurrenceSO selectedEvent, int occurrenceRoll)
    {
        int playerStatValue = playerStatus != null ? playerStatus.GetStatValue(selectedEvent.successStat) : 0;
        int playerRoll = Random.Range(0, Mathf.Max(0, playerStatValue) + 1);
        bool success = playerRoll > occurrenceRoll;

        if (playerStatus != null)
        {
            if (success)
                playerStatus.ApplyStatDelta(selectedEvent.successStat, selectedEvent.successValue);
            else
                playerStatus.ApplyStatDelta(selectedEvent.failStat, selectedEvent.failValue);
        }

        return new OccurrenceResult
        {
            success = success,
            occurrenceRoll = occurrenceRoll,
            playerRoll = playerRoll,
            successText = selectedEvent.successText,
            failText = selectedEvent.failText
        };
    }
}
