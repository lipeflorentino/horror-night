using UnityEngine;
using TMPro;
using System.Collections;

public class PromptFeedbackEffect : MonoBehaviour
{
    [SerializeField] private TMP_Text promptText;

    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.35f;
    [SerializeField] private float shakeMagnitude = 10f;
    [SerializeField] private float shakeFrequency = 40f;

    [Header("Flash Settings")]
    [SerializeField] private float flashSpeed = 15f;
    [SerializeField] private float minAlpha = 0.2f;

    private Coroutine currentRoutine;
    private Vector3 originalPosition;
    private Color originalColor;

    private void Awake()
    {
        if (promptText == null)
            promptText = GetComponent<TMP_Text>();

        originalPosition = promptText.rectTransform.anchoredPosition;
        originalColor = promptText.color;
    }

    public void PlayDeniedEffect()
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(DeniedEffectRoutine());
    }

    private IEnumerator DeniedEffectRoutine()
    {
        float elapsed = 0f;
        RectTransform rect = promptText.rectTransform;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            // Shake
            float shakeOffset = Mathf.Sin(elapsed * shakeFrequency) * shakeMagnitude;
            rect.anchoredPosition = originalPosition + new Vector3(shakeOffset, 0f, 0f);

            // Flash
            float alpha = Mathf.Lerp(minAlpha, 1f, Mathf.PingPong(elapsed * flashSpeed, 1f));
            Color c = originalColor;
            c.a = alpha;
            promptText.color = c;

            yield return null;
        }

        // Reset
        rect.anchoredPosition = originalPosition;
        promptText.color = originalColor;

        currentRoutine = null;
    }
}
