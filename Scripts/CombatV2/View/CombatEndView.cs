using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatEndView : MonoBehaviour
{
    private const float VictoryTimeoutSeconds = 180f;

    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private GameObject victoryPanel, deathPanel;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text countdownText;

    [Header("Buttons")]
    [SerializeField] private Button primaryButton;
    [SerializeField] private Button secondaryButton;

    private Coroutine countdownRoutine;

    private void Awake()
    {
        Hide();
    }

    public void ShowGameOver(Action onRestart, Action onQuit)
    {
        deathPanel.SetActive(true);
        victoryPanel.SetActive(false);
        ShowRoot();
        SetText("GAME OVER", "Você morreu. Deseja tentar novamente?", string.Empty);
        SetupButton(primaryButton, "Restart", onRestart, true);
        SetupButton(secondaryButton, "Quit", onQuit, true);
    }

    public void ShowVictory(int xpReward, Dictionary<ItemSO, int> itensReward, Action onProceed)
    {
        victoryPanel.SetActive(true);
        deathPanel.SetActive(false);
        ShowRoot();
        SetText("Vitória!", $"Recompensas:\n- XP: +{xpReward}\n- Itens: [\n+{string.Join(", ", itensReward.Select(kvp => $"-{kvp.Key.name}: {kvp.Value}\n]"))}", string.Empty);
        SetupButton(primaryButton, "Continue", onProceed, true);
        SetupButton(secondaryButton, string.Empty, null, false);

        if (countdownRoutine != null)
            StopCoroutine(countdownRoutine);

        countdownRoutine = StartCoroutine(VictoryCountdown(onProceed));
    }

    public void Hide()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }

        if (root != null)
            root.SetActive(false);
    }

    private IEnumerator VictoryCountdown(Action onTimeout)
    {
        float remaining = VictoryTimeoutSeconds;

        while (remaining > 0f)
        {
            if (countdownText != null)
                countdownText.text = $"Retorno automático em {(int)Mathf.Ceil(remaining)}s";

            remaining -= Time.deltaTime;
            yield return null;
        }

        if (countdownText != null)
            countdownText.text = "Retornando...";

        onTimeout?.Invoke();
    }

    private void ShowRoot()
    {
        if (root != null)
        {
            root.SetActive(true);
            root.transform.SetAsLastSibling();
        }
    }

    private void SetText(string title, string subtitle, string countdown)
    {
        if (titleText != null)
            titleText.text = title;

        if (subtitleText != null)
            subtitleText.text = subtitle;

        if (countdownText != null)
            countdownText.text = countdown;
    }

    private void SetupButton(Button button, string label, Action callback, bool isVisible)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(isVisible);
        if (!isVisible)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => callback?.Invoke());

        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.text = label;
    }
}
