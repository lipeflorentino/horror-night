using UnityEngine;

[DisallowMultipleComponent]
public class CombatUIViewBinder : MonoBehaviour
{
    [SerializeField] private CombatUI combatUI;
    [SerializeField] private FeedbackView feedbackView;
    [SerializeField] private LogView logView;
    [SerializeField] private HudView hudView;

    private void OnEnable()
    {
        if (combatUI == null)
            return;

        combatUI.Feedback += HandleFeedback;
        combatUI.Log += HandleLog;
        combatUI.Hud += HandleHud;
    }

    private void OnDisable()
    {
        if (combatUI == null)
            return;

        combatUI.Feedback -= HandleFeedback;
        combatUI.Log -= HandleLog;
        combatUI.Hud -= HandleHud;
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
            hudView.UpdateDice(value);
    }
}
