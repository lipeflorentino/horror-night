using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class SystemMessagesUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float displayDuration = 1.5f;
    [SerializeField] private Vector3 punchScale = new(0.15f, 0.15f, 0.15f);

    private Sequence feedbackSequence;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    public void ShowStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        feedbackSequence?.Kill();
        transform.DOKill();
        canvasGroup.DOKill();

        gameObject.SetActive(true);
        transform.localScale = Vector3.one;

        feedbackSequence = DOTween.Sequence();

        canvasGroup.alpha = 0f;
        feedbackSequence.Join(canvasGroup.DOFade(1f, fadeDuration));
        feedbackSequence.Join(transform.DOPunchScale(punchScale, fadeDuration, 4, 0.5f));

        feedbackSequence.AppendInterval(displayDuration);

        feedbackSequence.Append(canvasGroup.DOFade(0f, fadeDuration));
        feedbackSequence.OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }

    private void OnDestroy()
    {
        feedbackSequence?.Kill();
        transform.DOKill();
        canvasGroup.DOKill();
    }
}