using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatEndView : MonoBehaviour
{
    private const float VictoryTimeoutSeconds = 180f;

    [Header("Root")]
    [SerializeField] private GameObject root;

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
        ShowRoot();
        SetText("GAME OVER", "Você morreu. Deseja tentar novamente?", string.Empty);
        SetupButton(primaryButton, "Restart", onRestart, true);
        SetupButton(secondaryButton, "Quit", onQuit, true);
    }

    public void ShowVictory(Action onProceed)
    {
        ShowRoot();
        SetText("Vitória!", "Placeholder de recompensas\n- XP\n- Loot\n- Evento", string.Empty);
        SetupButton(primaryButton, "Prosseguir", onProceed, true);
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
