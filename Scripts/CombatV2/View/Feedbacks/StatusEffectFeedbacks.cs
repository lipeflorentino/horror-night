using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectFeedbacks : MonoBehaviour
{
    [SerializeField] private GameObject StatusEffectPrefab;
    [SerializeField] private Transform feedbackAnchor;

    private Battler ownerBattler;
    private PerkService perkService;
    private TrickService trickService;
    private readonly Dictionary<string, int> lastFeedbackFrameByEffect = new();
    private readonly Dictionary<string, GameObject> popupByFeedbackKey = new();

    public Battler OwnerBattler => ownerBattler;

    public void Initialize(PerkService service, TrickService trickServiceInstance, Battler owner)
    {
        if (perkService == service && trickService == trickServiceInstance && ownerBattler == owner)
            return;

        if (perkService != null)
        {
            perkService.OnBattlerStateApplied -= HandleBattlerStateApplied;
            perkService.OnBattlerStateRemoved -= HandleBattlerStateRemoved;
            perkService.OnDrawbackApplied -= HandleDrawbackApplied;
            perkService.OnDrawbackRemoved -= HandleDrawbackRemoved;
        }

        if (trickService != null)
        {
            trickService.OnTrickCasted -= HandleTrickCasted;
            trickService.OnTrickExpired -= HandleTrickExpired;
            trickService.OnTrickRemoved -= HandleTrickRemoved;
            trickService.OnTrickChanged -= HandleTrickChanged;
        }

        ownerBattler = owner;
        perkService = service;
        trickService = trickServiceInstance;

        if (perkService != null)
        {
            perkService.OnBattlerStateApplied += HandleBattlerStateApplied;
            perkService.OnBattlerStateRemoved += HandleBattlerStateRemoved;
            perkService.OnDrawbackApplied += HandleDrawbackApplied;
            perkService.OnDrawbackRemoved += HandleDrawbackRemoved;
        }

        if (trickService != null)
        {
            trickService.OnTrickCasted += HandleTrickCasted;
            trickService.OnTrickExpired += HandleTrickExpired;
            trickService.OnTrickRemoved += HandleTrickRemoved;
            trickService.OnTrickChanged += HandleTrickChanged;
        }
    }

    private void HandleTrickCasted(Battler battler, TrickRuntimeInstance trick)
    {
        Logger.Log($"trick casted: {trick.Definition.DisplayName}, {trick.InstanceId}, {trick.RemainingTurns}");
        if (!ShouldHandleEvent(battler))
            return;

        if (trick == null || trick.Definition == null)
            return;

        if (trick.Definition.DurationTurns < 0 || trick.SlotType == TrickSlotType.Identity)
            return;

        ShowFeedbackPopup(
            displayName: trick.Definition.DisplayName,
            description: trick.Definition.Description,
            icon: trick.Definition.Icon,
            remainingTurns: trick.RemainingTurns,
            feedbackKey: GetFeedbackKey(trick.InstanceId, trick.Definition.Id));
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

    private void HandleTrickExpired(Battler battler, TrickRuntimeInstance trick)
    {
        Logger.Log($"[HandleTrickExpired] Handling trick expired for {battler.Name}: {trick.Definition.DisplayName}");
        if (!ShouldHandleEvent(battler))
            return;

        RemovePopupForTrick(trick);
    }

    private void HandleTrickRemoved(Battler battler, string trickId)
    {
        Logger.Log($"[HandleTrickRemoved] Handling trick removed: {trickId}");
        if (!ShouldHandleEvent(battler))
            return;

        if (string.IsNullOrWhiteSpace(trickId))
            return;

        foreach (var pair in new List<KeyValuePair<string, GameObject>>(popupByFeedbackKey))
        {
            if (pair.Key.Equals(trickId, System.StringComparison.OrdinalIgnoreCase))
            {
                RemovePopup(pair.Key);
                break;
            }
        }
    }

    private void HandleTrickChanged(Battler battler, TrickRuntimeInstance trick)
    {
        if (!ShouldHandleEvent(battler))
            return;

        if (trick == null || trick.Definition == null)
            return;

        string feedbackKey = GetFeedbackKey(trick.InstanceId, trick.Definition.Id);
        if (popupByFeedbackKey.TryGetValue(feedbackKey, out GameObject popupObject) && popupObject != null && popupObject.TryGetComponent<StatusEffectUI>(out var popup))
        {
            popup.RefreshDuration(trick.RemainingTurns);
        }
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

    private void RemovePopupForTrick(TrickRuntimeInstance trick)
    {
        if (trick == null)
            return;

        string feedbackKey = GetFeedbackKey(trick.InstanceId, trick.Definition?.Id);
        if (!string.IsNullOrWhiteSpace(feedbackKey))
            RemovePopup(feedbackKey);
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

        Destroy(popupObj, 2f); // Delay destruction to allow exit animation to play
    }

    private static string GetFeedbackKey(string runtimeId, string fallbackId)
    {
        return string.IsNullOrWhiteSpace(runtimeId) ? fallbackId : runtimeId;
    }

    private void OnDestroy()
    {
        if (perkService != null)
        {
            perkService.OnBattlerStateApplied -= HandleBattlerStateApplied;
            perkService.OnBattlerStateRemoved -= HandleBattlerStateRemoved;
            perkService.OnDrawbackApplied -= HandleDrawbackApplied;
            perkService.OnDrawbackRemoved -= HandleDrawbackRemoved;
        }

        if (trickService != null)
        {
            trickService.OnTrickCasted -= HandleTrickCasted;
            trickService.OnTrickExpired -= HandleTrickExpired;
            trickService.OnTrickRemoved -= HandleTrickRemoved;
        }
    }
}
