using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIOccurrencePopup : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private GameObject choicesUIPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button optionButton1;
    [SerializeField] private Button optionButton2;
    [SerializeField] private Button neutralOptionButton;
    [SerializeField] private TextMeshProUGUI optionButton1Text;
    [SerializeField] private TextMeshProUGUI optionButton2Text;
    [SerializeField] private TextMeshProUGUI neutralOptionButtonText;
    [SerializeField] private Button closeButton;

    [Header("Result UI")]
    [SerializeField] private GameObject resultRoot;
    [SerializeField] private TextMeshProUGUI resultText, resultStatusText, metaText, rollText, affectedStatText;
    [SerializeField] private DiceRollUI diceRollUI;

    public void Show(OccurrenceSO entry, Func<int, OccurrenceResult> onOptionSelected, Action onClose)
    {
        if (root != null)
            root.SetActive(true);

        if (titleText != null)
            titleText.text = entry.title;

        if (descriptionText != null)
            descriptionText.text = entry.description;

        if (resultRoot != null)
            resultRoot.SetActive(false);

        if (resultText != null)
            resultText.text = string.Empty;
        
        if (resultStatusText != null)
            resultStatusText.text = string.Empty;

        if (affectedStatText != null)
            affectedStatText.text = string.Empty;

        if (choicesUIPanel != null)
            choicesUIPanel.SetActive(true);

        SetupChoiceButton(optionButton1, optionButton1Text, entry.profileOption1, 0, onOptionSelected);
        SetupChoiceButton(optionButton2, optionButton2Text, entry.profileOption2, 1, onOptionSelected);
        SetupChoiceButton(neutralOptionButton, neutralOptionButtonText, entry.neutralOption, 2, onOptionSelected);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.gameObject.SetActive(false);
            closeButton.onClick.AddListener(() =>
            {
                onClose?.Invoke();
                if (root != null)
                    root.SetActive(false);
            });
        }
    }

    private void SetupChoiceButton(Button button, TextMeshProUGUI buttonText, string label, int index, Func<int, OccurrenceResult> callback)
    {
        if (buttonText != null)
            buttonText.text = label;

        if (button == null)
            return;

        button.gameObject.SetActive(true);
        button.interactable = true;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            StartCoroutine(HandleOptionSelection(index, callback));
        });
    }

    private IEnumerator HandleOptionSelection(int selectedIndex, Func<int, OccurrenceResult> callback)
    {
        ToggleOptionButtons(false);

        OccurrenceResult result = callback != null ? callback.Invoke(selectedIndex) : default;

        if (result.requiresRoll && diceRollUI != null)
            yield return StartCoroutine(diceRollUI.PlayRollAnimation(result.playerRoll));

        if (resultRoot != null)
            resultRoot.SetActive(true);

        if (resultText != null)
        {
            if (result.requiresRoll)
            {
                string deltaText = result.delta >= 0 ? $"+{result.delta}" : result.delta.ToString();

                resultText.text = result.success ? result.successText : result.failText;
                metaText.text = $"{result.occurrenceRoll}";
                rollText.text = $"{result.playerRoll}";
                resultStatusText.text = result.success ? "Sucesso" : "Falha";
                affectedStatText.text = $"Afetou {result.primaryStat}: {deltaText}";
            }
            else
            {
                resultText.text = $"{result.successText}.";
            }
        }

        if (closeButton != null)
            closeButton.gameObject.SetActive(true);
    }

    private void ToggleOptionButtons(bool interactable)
    {
        if (optionButton1 != null)
            optionButton1.interactable = interactable;

        if (optionButton2 != null)
            optionButton2.interactable = interactable;

        if (neutralOptionButton != null)
            neutralOptionButton.interactable = interactable;

        choicesUIPanel.SetActive(interactable);
    }
}
