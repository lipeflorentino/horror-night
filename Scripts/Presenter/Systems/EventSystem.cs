using UnityEngine;

public class EventSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EventDatabase database;
    [SerializeField] private UIEventPopup popup;
    [SerializeField] private PlayerGridMovement playerMovement;
    [SerializeField] private PlayerStatusManager playerStatus;

    private void Awake()
    {
        if (popup == null)
            popup = FindObjectOfType<UIEventPopup>();

        if (playerMovement == null)
            playerMovement = FindObjectOfType<PlayerGridMovement>();

        if (playerStatus == null)
            playerStatus = FindObjectOfType<PlayerStatusManager>();

        if (database == null)
            database = Resources.Load<EventDatabase>("Data/EventDatabase");
    }

    public void TriggerEvent(LevelNode node, LevelSO level)
    {
        if (node == null || database == null)
            return;

        EventEntrySO selectedEvent = database.GetRandom();

        if (selectedEvent == null)
            return;

        int eventRoll = Random.Range(0, Mathf.Max(0, selectedEvent.rollRange) + 1);

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (popup == null)
        {
            ResolveEvent(selectedEvent, eventRoll);
            if (playerMovement != null)
                playerMovement.enabled = true;
            return;
        }

        popup.Show(
            selectedEvent,
            eventRoll,
            onRoll: () => ResolveEvent(selectedEvent, eventRoll),
            onClose: () =>
            {
                if (playerMovement != null)
                    playerMovement.enabled = true;
            });
    }

    private EventResult ResolveEvent(EventEntrySO selectedEvent, int eventRoll)
    {
        int playerStatValue = playerStatus != null ? playerStatus.GetStatValue(selectedEvent.successStat) : 0;
        int playerRoll = Random.Range(0, Mathf.Max(0, playerStatValue) + 1);
        bool success = playerRoll > eventRoll;

        if (playerStatus != null)
        {
            if (success)
                playerStatus.ApplyStatDelta(selectedEvent.successStat, selectedEvent.successValue);
            else
                playerStatus.ApplyStatDelta(selectedEvent.failStat, selectedEvent.failValue);
        }

        return new EventResult
        {
            success = success,
            eventRoll = eventRoll,
            playerRoll = playerRoll,
            successText = selectedEvent.successText,
            failText = selectedEvent.failText
        };
    }
}

public struct EventResult
{
    public bool success;
    public int eventRoll;
    public int playerRoll;
    public string successText;
    public string failText;
}
