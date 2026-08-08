using System.Collections.Generic;
using UnityEngine;

public class StatusEffectFeedbacks : MonoBehaviour
{
    [SerializeField] private GameObject StatusEffectPrefab;
    [SerializeField] private Transform feedbackAnchor;

    private Battler ownerBattler;
    private DrawbackService drawbackService;
    private BattlerStateService battlerStateService;
    private readonly Dictionary<string, int> lastFeedbackFrameByEffect = new();
    private readonly Dictionary<string, GameObject> popupByFeedbackKey = new();

    public Battler OwnerBattler => ownerBattler;

    public void Initialize(DrawbackService drawbackServiceInstance, BattlerStateService battlerStateServiceInstance, Battler owner)
    {
        if (battlerStateService == battlerStateServiceInstance && drawbackService == drawbackServiceInstance && ownerBattler == owner)
            return;

        if (battlerStateService != null)
        {
            battlerStateService.OnBattlerStateApplied -= HandleBattlerStateApplied;
            battlerStateService.OnBattlerStateRemoved -= HandleBattlerStateRemoved;
        }

        if (drawbackService != null)
        {
            drawbackService.OnDrawbackApplied -= HandleDrawbackApplied;
            drawbackService.OnDrawbackRemoved -= HandleDrawbackRemoved;
        }

        ownerBattler = owner;
        battlerStateService = battlerStateServiceInstance;
        drawbackService = drawbackServiceInstance;

        if (battlerStateService != null)
        {
            battlerStateService.OnBattlerStateApplied += HandleBattlerStateApplied;
            battlerStateService.OnBattlerStateRemoved += HandleBattlerStateRemoved;
        }

        if (drawbackService != null)
        {
            drawbackService.OnDrawbackApplied += HandleDrawbackApplied;
            drawbackService.OnDrawbackRemoved += HandleDrawbackRemoved;
        }
    }
    private void HandleBattlerStateApplied(Battler battler, BattlerStateRuntimeInstance state)
    {
        if (!ShouldHandleEvent(battler))
            return;

        if (state == null || state.Definition == null)
            return;

        ShowFeedbackPopup(
            displayName: state.Definition.DisplayName,
            description: state.Definition.Description,
            icon: state.Definition.Icon,
            remainingTurns: state.RemainingTurns,
            feedbackKey: GetFeedbackKey(state.Definition.Id, state.Definition.Id));
    }

    private void HandleBattlerStateRemoved(Battler battler, BattlerStateRuntimeInstance state)
    {
        if (!ShouldHandleEvent(battler))
            return;

        if (state == null)
            return;

        RemovePopup(GetFeedbackKey(state.Definition?.Id, state.Definition?.Id));
    }

    private void HandleDrawbackApplied(Battler battler, DrawbackRuntimeInstance drawback)
    {
        if (!ShouldHandleEvent(battler))
            return;

        if (drawback == null || drawback.Definition == null)
            return;

        ShowFeedbackPopup(
            displayName: drawback.Definition.DisplayName,
            description: drawback.Definition.Description,
            icon: drawback.Definition.Icon,
            remainingTurns: drawback.RemainingTurns,
            feedbackKey: GetFeedbackKey(drawback.InstanceId, drawback.Definition.Id));
    }

    private void HandleDrawbackRemoved(Battler battler, DrawbackRuntimeInstance drawback)
    {
        if (!ShouldHandleEvent(battler))
            return;

        if (drawback == null)
            return;

        RemovePopup(GetFeedbackKey(drawback.InstanceId, drawback.Definition?.Id));
    }

    private bool ShouldHandleEvent(Battler battler)
    {
        return ownerBattler == null || battler == null || battler == ownerBattler;
    }

    private void ShowFeedbackPopup(string displayName, string feedbackKey, string description = "", Sprite icon = null, int remainingTurns = -1)
    {
        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(feedbackKey))
            return;

        if (lastFeedbackFrameByEffect.TryGetValue(feedbackKey, out int lastFrame) && lastFrame == Time.frameCount)
            return;

        lastFeedbackFrameByEffect[feedbackKey] = Time.frameCount;

        if (popupByFeedbackKey.ContainsKey(feedbackKey))
            return;

        if (feedbackAnchor == null || StatusEffectPrefab == null)
            return;

        GameObject popupObject = Instantiate(StatusEffectPrefab, feedbackAnchor);
        if (popupObject.TryGetComponent<StatusEffectUI>(out var popup))
        {
            popup.Setup(icon, remainingTurns, description, displayName);
            popup.PlayEnterAnimation();
        }

        popupByFeedbackKey[feedbackKey] = popupObject;
    }

    private void RemovePopup(string feedbackKey)
    {
        popupByFeedbackKey.TryGetValue(feedbackKey, out GameObject popupObj);
        if (string.IsNullOrWhiteSpace(feedbackKey) || !popupObj)
            return;

        popupByFeedbackKey.Remove(feedbackKey);
        if (popupObj != null && popupObj.TryGetComponent<StatusEffectUI>(out var popup))
        {
            popup.PlayExitAnimation();
        }

        Destroy(popupObj, 4f); // Delay destruction to allow exit animation to play
    }

    private static string GetFeedbackKey(string runtimeId, string fallbackId)
    {
        return string.IsNullOrWhiteSpace(runtimeId) ? fallbackId : runtimeId;
    }

    private void OnDestroy()
    {
        if (battlerStateService != null)
        {
            battlerStateService.OnBattlerStateApplied -= HandleBattlerStateApplied;
            battlerStateService.OnBattlerStateRemoved -= HandleBattlerStateRemoved;
        }

        if (drawbackService != null)
        {
            drawbackService.OnDrawbackApplied -= HandleDrawbackApplied;
            drawbackService.OnDrawbackRemoved -= HandleDrawbackRemoved;
        }
    }
}
