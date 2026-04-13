using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDAnimator
{
     public IEnumerator AnimateDamagePopup(CanvasGroup canvasGroup, float duration)
    {
        if (canvasGroup == null)
            yield break;

        float elapsed = 0f;
        Vector3 startPosition = canvasGroup.transform.localPosition;
        Vector3 endPosition = startPosition + Vector3.up * 30f;

        canvasGroup.alpha = 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            canvasGroup.transform.localPosition = Vector3.Lerp(startPosition, endPosition, progress);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);

            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.transform.localPosition = startPosition;
    }

    public IEnumerator AnimateActionFeedback(TMP_Text feedbackText, float duration)
    {
        if (feedbackText == null)
            yield break;

        float elapsed = 0f;
        float fadeDuration = duration * 0.3f;
        
        Color originalColor = feedbackText.color;
        Color transparent = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeDuration;
            feedbackText.color = Color.Lerp(transparent, originalColor, progress);
            yield return null;
        }

        feedbackText.color = originalColor;

        yield return new WaitForSeconds(duration - (fadeDuration * 2f));

        elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeDuration;
            feedbackText.color = Color.Lerp(originalColor, transparent, progress);
            yield return null;
        }

        feedbackText.color = transparent;
    }

    public IEnumerator AnimateShake(Transform target, float duration, float magnitude)
    {
        Vector3 originalPosition = target.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float randomX = Random.Range(-1f, 1f) * magnitude;
            float randomY = Random.Range(-1f, 1f) * magnitude;

            target.localPosition = originalPosition + new Vector3(randomX, randomY, 0f);

            yield return null;
        }

        target.localPosition = originalPosition;
    }

    public IEnumerator AnimateSlider(Slider slider, float targetValue, float duration)
    {
        if (slider == null)
            yield break;

        float elapsed = 0f;
        float startValue = slider.value;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            slider.value = Mathf.Lerp(startValue, targetValue, progress);
            yield return null;
        }

        slider.value = targetValue;
    }

    public IEnumerator AnimatePulse(TMP_Text text, float duration, float scaleAmount = 1.2f)
    {
        if (text == null)
            yield break;

        Vector3 originalScale = text.transform.localScale;
        Vector3 pulsedScale = originalScale * scaleAmount;
        
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            float scale = Mathf.Lerp(1f, scaleAmount, Mathf.Sin(progress * Mathf.PI));
            text.transform.localScale = originalScale * scale;

            yield return null;
        }

        text.transform.localScale = originalScale;
    }
}
