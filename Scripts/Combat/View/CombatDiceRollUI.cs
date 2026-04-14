using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CombatDiceRollUI : MonoBehaviour
{
    [Header("Dice Display")]
    [SerializeField] private Transform diceContainer;
    [SerializeField] private Image dicePrefab;
    
    [Header("Animation Settings")]
    [SerializeField] private float rollDuration = 0.65f;
    [SerializeField] private float spinSpeed = 900f;
    [SerializeField] private float updateInterval = 0.06f;
    [SerializeField] private float spacing = 50f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
    }

    public IEnumerator PlaySingleDiceRoll(int finalValue)
    {
        ClearDiceContainer();
        
        Image dice = Instantiate(dicePrefab, diceContainer);
        Text diceText = dice.GetComponentInChildren<Text>();
        TMP_Text diceTextTMP = dice.GetComponentInChildren<TMP_Text>();

        if (diceText == null && diceTextTMP == null)
            yield break;

        yield return StartCoroutine(AnimateDiceRoll(dice, diceText, diceTextTMP, finalValue));
    }

    public IEnumerator PlayMultipleDiceRoll(int[] finalValues)
    {
        ClearDiceContainer();

        if (finalValues == null || finalValues.Length == 0)
            yield break;

        Image[] diceImages = new Image[finalValues.Length];
        Text[] diceTexts = new Text[finalValues.Length];
        TMP_Text[] diceTextsTMP = new TMP_Text[finalValues.Length];

        for (int i = 0; i < finalValues.Length; i++)
        {
            diceImages[i] = Instantiate(dicePrefab, diceContainer);
            
            RectTransform rectTransform = diceImages[i].GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                float xPos = (i - finalValues.Length / 2f) * spacing;
                rectTransform.anchoredPosition = new Vector2(xPos, 0);
            }

            diceTexts[i] = diceImages[i].GetComponentInChildren<Text>();
            diceTextsTMP[i] = diceImages[i].GetComponentInChildren<TMP_Text>();
        }

        yield return StartCoroutine(AnimateMultipleDice(diceImages, diceTexts, diceTextsTMP, finalValues));
    }

    private IEnumerator AnimateDiceRoll(Image diceImage, Text diceText, TMP_Text diceTextTMP, int finalValue)
    {
        canvasGroup.alpha = 1f;

        float elapsed = 0f;
        float nextValueUpdate = 0f;

        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;

            if (diceImage != null)
                diceImage.transform.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);

            if (elapsed >= nextValueUpdate)
            {
                int randomValue = Random.Range(1, 7);
                if (diceText != null)
                    diceText.text = randomValue.ToString();
                if (diceTextTMP != null)
                    diceTextTMP.text = randomValue.ToString();

                nextValueUpdate += updateInterval;
            }

            yield return null;
        }

        if (diceImage != null)
            diceImage.transform.rotation = Quaternion.identity;

        if (diceText != null)
            diceText.text = finalValue.ToString();
        if (diceTextTMP != null)
            diceTextTMP.text = finalValue.ToString();

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(FadeOut());
    }

    private IEnumerator AnimateMultipleDice(Image[] diceImages, Text[] diceTexts, TMP_Text[] diceTextsTMP, int[] finalValues)
    {
        canvasGroup.alpha = 1f;

        float elapsed = 0f;
        float nextValueUpdate = 0f;

        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;

            for (int i = 0; i < diceImages.Length; i++)
            {
                if (diceImages[i] != null)
                    diceImages[i].transform.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
            }

            if (elapsed >= nextValueUpdate)
            {
                for (int i = 0; i < diceTexts.Length; i++)
                {
                    int randomValue = Random.Range(1, 7);
                    if (diceTexts[i] != null)
                        diceTexts[i].text = randomValue.ToString();
                    if (diceTextsTMP[i] != null)
                        diceTextsTMP[i].text = randomValue.ToString();
                }

                nextValueUpdate += updateInterval;
            }

            yield return null;
        }

        for (int i = 0; i < diceImages.Length; i++)
        {
            if (diceImages[i] != null)
                diceImages[i].transform.rotation = Quaternion.identity;

            if (diceTexts[i] != null)
                diceTexts[i].text = finalValues[i].ToString();
            if (diceTextsTMP[i] != null)
                diceTextsTMP[i].text = finalValues[i].ToString();
        }

        yield return new WaitForSeconds(0.5f);
        
        yield return StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float fadeOutDuration = 0.3f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    private void ClearDiceContainer()
    {
        if (diceContainer == null)
            return;

        foreach (Transform child in diceContainer)
        {
            Destroy(child.gameObject);
        }
    }
}
