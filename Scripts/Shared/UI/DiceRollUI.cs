using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class DiceRollUI : MonoBehaviour
{
    [SerializeField] private Image diceImage;
    [SerializeField] private TMP_Text diceValueText;
    // [SerializeField] private Image highlightFrame;
    [SerializeField] private TMP_Text bestText;
    [SerializeField, Range(0f, 1f)] private float dimmedAlpha = 0.3f;
    [SerializeField] private float rollDuration = 0.65f;
    [SerializeField] private float spinSpeed = 900f;
    [SerializeField] private float updateInterval = 0.06f;
    
    [Header("Dice Icons")]
    [SerializeField] private Sprite mindDiceIcon;
    [SerializeField] private Sprite heartDiceIcon;
    [SerializeField] private Sprite bodyDiceIcon;

    private RectTransform rectTransform;
    public RectTransform RectTransform => rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetHighlighted(bool highlighted)
    {
        // TODO: criar no futuro um particle effect para highlight
        // if (highlightFrame != null)
        //    highlightFrame.enabled = highlighted;

        if (bestText != null)
            bestText.gameObject.SetActive(highlighted);

        SetAlpha(highlighted ? 1f : dimmedAlpha);
    }

    public void ClearValue()
    {
        if (diceValueText != null)
            diceValueText.text = "0";

        SetHighlighted(false);
        SetAlpha(1f);
    }

    public IEnumerator PlayRollAnimation(int finalValue, int maxRandomValue = 6)
    {
        if (diceImage == null && diceValueText == null)
            yield break;

        float elapsed = 0f;
        float nextValueUpdate = 0f;

        if (diceImage != null)
            diceImage.enabled = true;

        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;

            if (diceImage != null)
                diceImage.transform.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);

            if (diceValueText != null && elapsed >= nextValueUpdate)
            {
                diceValueText.text = Random.Range(1, maxRandomValue + 1).ToString();
                nextValueUpdate += updateInterval;
            }

            yield return null;
        }

        if (diceImage != null)
            diceImage.transform.rotation = Quaternion.identity;

        if (diceValueText != null)
            diceValueText.text = finalValue.ToString();
    }

    public IEnumerator PlayFadeOut(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(1f, 0f, elapsed / duration));
            yield return null;
        }
        SetAlpha(0f);
        gameObject.SetActive(false);
    }

    public IEnumerator PlayFadeIn(float duration)
    {
        gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(0f, 1f, elapsed / duration));
            yield return null;
        }
        SetAlpha(1f);
    }

    public IEnumerator PlayMoveTo(Vector2 targetAnchoredPosition, float duration)
    {
        if (rectTransform == null)
            yield break;

        Vector2 startPosition = rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetAnchoredPosition, elapsed / duration);
            yield return null;
        }

        rectTransform.anchoredPosition = targetAnchoredPosition;
    }

    public IEnumerator PlayPulse(float duration, float scaleMultiplier = 1.2f)
    {
        if (rectTransform == null)
            yield break;

        Vector3 originalScale = rectTransform.localScale;
        Vector3 peakScale = originalScale * scaleMultiplier;
        float halfDuration = duration * 0.5f;

        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            rectTransform.localScale = Vector3.Lerp(originalScale, peakScale, elapsed / halfDuration);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            rectTransform.localScale = Vector3.Lerp(peakScale, originalScale, elapsed / halfDuration);
            yield return null;
        }

        rectTransform.localScale = originalScale;
    }

    public void SetValueText(int value)
    {
        if (diceValueText != null)
            diceValueText.text = value.ToString();
    }

    public void SetDiceIcon(DiceStatType statType)
    {
        if (diceImage != null)
            diceImage.sprite = GetIcon(statType);
    }

    private void SetAlpha(float alpha)
    {
        if (diceImage != null)
        {
            Color imageColor = diceImage.color;
            imageColor.a = alpha;
            diceImage.color = imageColor;
        }

        if (diceValueText != null)
        {
            Color valueColor = diceValueText.color;
            valueColor.a = alpha;
            diceValueText.color = valueColor;
        }
    }

    public Sprite GetIcon(DiceStatType type)
    {
        return type switch
        {
            DiceStatType.Mind => mindDiceIcon,
            DiceStatType.Heart => heartDiceIcon,
            _ => bodyDiceIcon,
        };

    }
}