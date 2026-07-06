using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TrickActivationFeedback : MonoBehaviour
{
    [FormerlySerializedAs("perkActivationPrefab")]
    [SerializeField] private GameObject trickActivationPrefab;
    [SerializeField] private Transform playerAnchor;
    [SerializeField] private Transform enemyAnchor;
    [SerializeField] private string fallbackInputKeyText = string.Empty;
    [SerializeField] private float feedbackDuration = 2f;
    [SerializeField] private bool onlyShowPassiveTricks = true;

    private PerkService perkService;
    private readonly Dictionary<string, int> lastFeedbackFrameByTrick = new();

    public void Initialize(PerkService service)
    {
        if (perkService == service)
            return;

        if (perkService != null)
            perkService.OnPerkTriggered -= HandlePerkTriggered;

        perkService = service;

        if (perkService != null)
            perkService.OnPerkTriggered += HandlePerkTriggered;
    }

    private void HandlePerkTriggered(PerkTriggeredEvent evt)
    {
        TrickRuntimeInstance sourceTrick = evt.SourceTrick;
        TrickSO trickDefinition = sourceTrick?.Definition;

        if (evt.Owner == null || trickDefinition == null)
            return;

        if (onlyShowPassiveTricks && !trickDefinition.IsPassive)
            return;

        string feedbackKey = string.IsNullOrWhiteSpace(sourceTrick.InstanceId) ? trickDefinition.Id : sourceTrick.InstanceId;
        if (lastFeedbackFrameByTrick.TryGetValue(feedbackKey, out int lastFrame) && lastFrame == Time.frameCount)
            return;

        lastFeedbackFrameByTrick[feedbackKey] = Time.frameCount;

        Transform anchor = evt.Owner.IsPlayer ? playerAnchor : enemyAnchor;
        if (anchor == null || trickActivationPrefab == null)
            return;

        GameObject popupObject = Instantiate(trickActivationPrefab, anchor);
        TrickIconUI popup = popupObject.GetComponent<TrickIconUI>();
        if (popup != null)
        {
            popup.Setup(trickDefinition, fallbackInputKeyText, sourceTrick);
            popup.PlayEnterAnimation();
        }

        TextMeshProUGUI text = popupObject.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null && string.IsNullOrWhiteSpace(text.text))
            text.text = $"{trickDefinition.DisplayName} acionado!";

        StartCoroutine(AnimatePopup(popupObject));

        Debug.Log($"[TrickFeedback] {trickDefinition.Id} acionado por perk {evt.PerkId} " +
                  $"- Trigger: {evt.Trigger}, Target: {evt.ModifierTarget}, Value: {evt.AppliedValue}, Stacks: {evt.StacksApplied}");
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

        yield return new WaitForSeconds(Mathf.Max(0f, feedbackDuration - enterDuration));

        if (popup != null)
            Destroy(popup);
    }

    private void OnDestroy()
    {
        if (perkService != null)
            perkService.OnPerkTriggered -= HandlePerkTriggered;
    }
}
