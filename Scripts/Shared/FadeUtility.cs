using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Coroutines de fade compartilhadas entre os componentes de Feedback.
// Extraído para evitar duplicação de lógica de lerp de alpha.
public static class FadeUtility
{
    public static IEnumerator FadeImageAlpha(Image image, float from, float to, float duration)
    {
        if (image == null)
            yield break;

        duration = Mathf.Max(0.01f, duration);
        Color color = image.color;
        color.a = from;
        image.color = color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(from, to, t);
            image.color = color;
            yield return null;
        }

        color.a = to;
        image.color = color;
    }

    public static IEnumerator FadeSpriteAlpha(SpriteRenderer spriteRenderer, float from, float to, float duration)
    {
        if (spriteRenderer == null)
            yield break;

        duration = Mathf.Max(0.01f, duration);
        Color color = spriteRenderer.color;
        color.a = from;
        spriteRenderer.color = color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(from, to, t);
            spriteRenderer.color = color;
            yield return null;
        }

        color.a = to;
        spriteRenderer.color = color;
    }
}