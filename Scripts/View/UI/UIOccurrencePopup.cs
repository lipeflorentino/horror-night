using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIOccurrencePopup : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button rollButton;
    [SerializeField] private Button closeButton;

    [Header("Result UI")]
    [SerializeField] private GameObject resultRoot;
    [SerializeField] private TextMeshProUGUI resultText;

    public void Show(OccurrenceSO entry, int occurrenceRoll, Func<OccurrenceResult> onRoll, Action onClose)
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

        if (rollButton != null)
        {
            rollButton.onClick.RemoveAllListeners();
            rollButton.interactable = true;
            rollButton.onClick.AddListener(() =>
            {
                OccurrenceResult result = onRoll != null ? onRoll.Invoke() : default;

                if (resultRoot != null)
                    resultRoot.SetActive(true);

                if (resultText != null)
                {
                    string message = result.success ? result.successText : result.failText;
                    resultText.text = $"{message}\n\nRolagem: {result.playerRoll} x Meta: {result.occurrenceRoll}";
                }

                rollButton.interactable = false;

                if (closeButton != null)
                    closeButton.gameObject.SetActive(true);
            });
        }

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
}
