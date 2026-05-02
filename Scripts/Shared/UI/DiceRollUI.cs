using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiceRollUI : MonoBehaviour
{
    [SerializeField] private Image diceImage;
    [SerializeField] private TMP_Text diceValueText;
    [SerializeField] private Image highlightFrame;
    [SerializeField] private TMP_Text bestText;
    [SerializeField, Range(0f, 1f)] private float dimmedAlpha = 0.3f;
    [SerializeField] private float rollDuration = 0.65f;
    [SerializeField] private float spinSpeed = 900f;
    [SerializeField] private float updateInterval = 0.06f;
    
    [Header("Dice Icons")]
    [SerializeField] private Sprite mindDiceIcon;
    [SerializeField] private Sprite heartDiceIcon;
    [SerializeField] private Sprite bodyDiceIcon;

    public void SetHighlighted(bool highlighted)
    {
        if (highlightFrame != null)
            highlightFrame.enabled = highlighted;

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
