using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class CombatUIViewBinder : MonoBehaviour
{
    private CombatUI combatUI;
    [SerializeField] private FeedbackView feedbackView;
    [SerializeField] private LogView logView;
    [SerializeField] private HudView hudView;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text actionQueueText;

    private void OnEnable()
    {
        if (combatUI == null)
            return;

        combatUI.Feedback += HandleFeedback;
        combatUI.Log += HandleLog;
        combatUI.Hud += HandleHud;
        combatUI.TurnText += HandleTurnText;
        combatUI.ActionQueued += HandleActionQueued;
    }

    private void OnDisable()
    {
        if (combatUI == null)
            return;

        combatUI.Feedback -= HandleFeedback;
        combatUI.Log -= HandleLog;
        combatUI.Hud -= HandleHud;
        combatUI.TurnText -= HandleTurnText;
        combatUI.ActionQueued -= HandleActionQueued;
    }

    private void HandleFeedback(string text, bool popup)
    {
        if (feedbackView != null)
            feedbackView.Show(text, popup);
    }

    private void HandleLog(string text, CombatLogStyle style)
    {
        if (logView != null)
            logView.Add(text, style);
    }

    private void HandleHud(int value)
    {
        if (hudView != null)
            hudView.UpdateAvailableDice(value);
    }

    private void HandleTurnText(string text)
    {
        if (turnText != null)
            turnText.text = text;
    }

    private void HandleActionQueued(string text)
    {
        if (actionQueueText != null)
            actionQueueText.text += $"\n- {text}";
    }

    public void Bind(CombatUI ui)
    {
        combatUI = ui;
        if (actionQueueText != null)
            actionQueueText.text = "Queued Actions:";
    }
}
