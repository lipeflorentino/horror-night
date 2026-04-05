using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class FeedbackView : MonoBehaviour
{
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private float popupDuration = 0.2f;
    [SerializeField] private float visibleDuration = 1.2f;
    [SerializeField] private float popupScale = 1.1f;

    private Coroutine feedbackRoutine;
    private RectTransform feedbackRectTransform;
    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        if (feedbackText != null)
        {
            feedbackRectTransform = feedbackText.rectTransform;
            baseScale = feedbackRectTransform.localScale;
            feedbackText.gameObject.SetActive(false);
        }
    }

    public void Show(string text, bool popup)
    {
        if (feedbackText == null)
            return;

        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        feedbackRoutine = StartCoroutine(ShowRoutine(text, popup));
    }

    private IEnumerator ShowRoutine(string text, bool popup)
    {
        feedbackText.gameObject.SetActive(true);
        feedbackText.text = text;

        if (feedbackRectTransform != null)
            feedbackRectTransform.localScale = baseScale;

        if (popup)
            yield return PlayPopup();

        yield return new WaitForSeconds(visibleDuration);

        if (feedbackRectTransform != null)
            feedbackRectTransform.localScale = baseScale;

        feedbackText.gameObject.SetActive(false);
        feedbackRoutine = null;
    }

    private IEnumerator PlayPopup()
    {
        if (feedbackRectTransform == null || popupDuration <= 0f)
            yield break;

        float elapsed = 0f;
        Vector3 targetScale = baseScale * popupScale;

        while (elapsed < popupDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popupDuration);

            if (t < 0.5f)
            {
                float growT = t / 0.5f;
                feedbackRectTransform.localScale = Vector3.Lerp(baseScale, targetScale, growT);
            }
            else
            {
                float shrinkT = (t - 0.5f) / 0.5f;
                feedbackRectTransform.localScale = Vector3.Lerp(targetScale, baseScale, shrinkT);
            }

            yield return null;
        }

        feedbackRectTransform.localScale = baseScale;
    }
}
