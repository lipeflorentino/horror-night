using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiceRollUI : MonoBehaviour
{
    [SerializeField] private Image diceImage;
    [SerializeField] private TMP_Text diceValueText;
    [SerializeField] private float rollDuration = 0.65f;
    [SerializeField] private float spinSpeed = 900f;
    [SerializeField] private float updateInterval = 0.06f;

    public IEnumerator PlayRollAnimation(int finalValue)
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
                diceValueText.text = Random.Range(1, 21).ToString();
                nextValueUpdate += updateInterval;
            }

            yield return null;
        }

        if (diceImage != null)
            diceImage.transform.rotation = Quaternion.identity;

        if (diceValueText != null)
            diceValueText.text = finalValue.ToString();
    }
}
