using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Gerencia animações de feedback visual da HUD.
/// Fornece métodos para animar popups de dano, feedback de ações, e shake de câmera.
/// </summary>
public class HUDAnimator
{
    /// <summary>
    /// Anima um popup de dano/cura que desaparece gradualmente.
    /// Move o texto para cima enquanto reduz a opacidade.
    /// </summary>
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

            // Interpolação linear para posição
            canvasGroup.transform.localPosition = Vector3.Lerp(startPosition, endPosition, progress);

            // Fade out
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);

            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.transform.localPosition = startPosition;
    }

    /// <summary>
    /// Anima o feedback de ação com fade in e fade out.
    /// </summary>
    public IEnumerator AnimateActionFeedback(TMP_Text feedbackText, float duration)
    {
        if (feedbackText == null)
            yield break;

        float elapsed = 0f;
        float fadeDuration = duration * 0.3f;
        
        Color originalColor = feedbackText.color;
        Color transparent = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);

        // Fade in
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeDuration;
            feedbackText.color = Color.Lerp(transparent, originalColor, progress);
            yield return null;
        }

        feedbackText.color = originalColor;

        // Wait
        yield return new WaitForSeconds(duration - (fadeDuration * 2f));

        elapsed = 0f;

        // Fade out
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeDuration;
            feedbackText.color = Color.Lerp(originalColor, transparent, progress);
            yield return null;
        }

        feedbackText.color = transparent;
    }

    /// <summary>
    /// Anima um shake de câmera para simular impacto de dano.
    /// </summary>
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

    /// <summary>
    /// Anima uma barra (HP slider) com suavidade.
    /// </summary>
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

    /// <summary>
    /// Pulsa um texto (para destacar informação importante).
    /// </summary>
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

            // Pulsar usando sine wave para efeito suave
            float scale = Mathf.Lerp(1f, scaleAmount, Mathf.Sin(progress * Mathf.PI));
            text.transform.localScale = originalScale * scale;

            yield return null;
        }

        text.transform.localScale = originalScale;
    }
}
