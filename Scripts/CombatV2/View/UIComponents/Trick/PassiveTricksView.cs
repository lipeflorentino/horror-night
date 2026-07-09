using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PassiveTricksView : MonoBehaviour
{
    [SerializeField] private GameObject trickActivationPrefab;
    [SerializeField] private Transform playerAnchor;
    [SerializeField] private Transform enemyAnchor;
    [SerializeField] private float feedbackDuration = 2f;

    private PerkService perkService;
    private TrickService trickService;
    private readonly Dictionary<string, int> lastFeedbackFrameByTrick = new();
    private readonly Dictionary<string, GameObject> popupByTrick = new();

    public void Initialize(PerkService service, TrickService trickServiceInstance)
    {
        if (perkService == service && trickService == trickServiceInstance)
            return;

        if (perkService != null)
            perkService.OnPerkTriggered -= HandlePerkTriggered;

        if (trickService != null)
        {
            trickService.OnTrickExpired -= HandleTrickExpired;
            trickService.OnTrickRemoved -= HandleTrickRemoved;
        }

        perkService = service;
        trickService = trickServiceInstance;

        if (perkService != null)
            perkService.OnPerkTriggered += HandlePerkTriggered;

        if (trickService != null)
        {
            trickService.OnTrickExpired += HandleTrickExpired;
            trickService.OnTrickRemoved += HandleTrickRemoved;
        }
    }

    private void HandlePerkTriggered(PerkTriggeredEvent evt)
    {
        TrickRuntimeInstance sourceTrick = evt.SourceTrick;
        TrickSO trickDefinition = sourceTrick?.Definition;

        if (evt.Owner == null || trickDefinition == null)
            return;

        if (!trickDefinition.IsPassive)
            return;

        string feedbackKey = string.IsNullOrWhiteSpace(sourceTrick.InstanceId) ? trickDefinition.Id : sourceTrick.InstanceId;
        if (lastFeedbackFrameByTrick.TryGetValue(feedbackKey, out int lastFrame) && lastFrame == Time.frameCount)
            return;

        lastFeedbackFrameByTrick[feedbackKey] = Time.frameCount;

        if (popupByTrick.ContainsKey(feedbackKey))
            return;

        Transform anchor = evt.Owner.IsPlayer ? playerAnchor : enemyAnchor;
        if (anchor == null || trickActivationPrefab == null)
            return;

        GameObject popupObject = Instantiate(trickActivationPrefab, anchor);
        if (popupObject.TryGetComponent<TrickIconUI>(out var popup))
        {
            popup.Setup(trickDefinition, string.Empty, sourceTrick);
            popup.PlayEnterAnimation();
        }

        TextMeshProUGUI text = popupObject.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null && string.IsNullOrWhiteSpace(text.text))
            text.text = $"{trickDefinition.DisplayName} acionado!";

        popupByTrick[feedbackKey] = popupObject;
        StartCoroutine(AnimatePopup(popupObject));

        Debug.Log($"[TrickFeedback] {trickDefinition.Id} acionado por perk {evt.PerkId} " +
                  $"- Trigger: {evt.Trigger}, Target: {evt.ModifierTarget}, Value: {evt.AppliedValue}, Stacks: {evt.StacksApplied}");
    }

    private void HandleTrickExpired(Battler battler, TrickRuntimeInstance trick)
    {
        RemovePopupForTrick(trick);
    }

    private void HandleTrickRemoved(Battler battler, string trickId)
    {
        if (string.IsNullOrWhiteSpace(trickId))
            return;

        foreach (var pair in new List<KeyValuePair<string, GameObject>>(popupByTrick))
        {
            if (pair.Key.Equals(trickId, System.StringComparison.OrdinalIgnoreCase))
            {
                RemovePopup(pair.Key);
                break;
            }
        }
    }

    private void RemovePopupForTrick(TrickRuntimeInstance trick)
    {
        if (trick == null)
            return;

        string feedbackKey = string.IsNullOrWhiteSpace(trick.InstanceId) ? trick.Definition?.Id : trick.InstanceId;
        if (!string.IsNullOrWhiteSpace(feedbackKey))
            RemovePopup(feedbackKey);
    }

    private void RemovePopup(string feedbackKey)
    {
        if (string.IsNullOrWhiteSpace(feedbackKey) || !popupByTrick.TryGetValue(feedbackKey, out GameObject popupObject))
            return;

        popupByTrick.Remove(feedbackKey);
        if (popupObject != null)
            StartCoroutine(AnimatePopupExit(popupObject));
    }

    private IEnumerator AnimatePopup(GameObject popup)
    {
        if (popup == null)
            yield break;

        CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
        RectTransform rectTransform = popup.GetComponent<RectTransform>();
        Vector3 startScale = rectTransform != null ? rectTransform.localScale : Vector3.one;

        float elapsed = 0f;
        float enterDuration = Mathf.Min(0.25f, feedbackDuration);
        while (elapsed < enterDuration && popup != null)
        {
            elapsed += Time.deltaTime;
            float t = enterDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / enterDuration);

            if (canvasGroup != null)
                canvasGroup.alpha = t;

            if (rectTransform != null)
                rectTransform.localScale = Vector3.Lerp(startScale * 0.8f, startScale, t);

            yield return null;
        }

        while (popup != null)
            yield return null;
    }

    private IEnumerator AnimatePopupExit(GameObject popup)
    {
        if (popup == null)
            yield break;

        CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
        RectTransform rectTransform = popup.GetComponent<RectTransform>();
        Vector3 startScale = rectTransform != null ? rectTransform.localScale : Vector3.one;

        float elapsed = 0f;
        float exitDuration = Mathf.Min(0.2f, feedbackDuration);
        while (elapsed < exitDuration && popup != null)
        {
            elapsed += Time.deltaTime;
            float t = exitDuration <= 0f ? 1f : Mathf.Clamp01(1f - elapsed / exitDuration);

            if (canvasGroup != null)
                canvasGroup.alpha = t;

            if (rectTransform != null)
                rectTransform.localScale = Vector3.Lerp(startScale, startScale * 0.9f, 1f - t);

            yield return null;
        }

        if (popup != null)
            Destroy(popup);
    }

    private void OnDestroy()
    {
        if (perkService != null)
            perkService.OnPerkTriggered -= HandlePerkTriggered;

        if (trickService != null)
        {
            trickService.OnTrickExpired -= HandleTrickExpired;
            trickService.OnTrickRemoved -= HandleTrickRemoved;
        }
    }
}
