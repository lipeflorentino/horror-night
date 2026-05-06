using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionLogView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private ActionLogItemUI logItemPrefab;

    [Header("Icons")]
    [SerializeField] private Sprite attackPowerIcon;
    [SerializeField] private Sprite defensePowerIcon;
    [SerializeField] private Sprite attackAccuracyIcon;
    [SerializeField] private Sprite defenseAccuracyIcon;

    [Header("Colors")]
    [SerializeField] private Color attackColor = new(0.3f, 0.95f, 1f, 1f); // ciano
    [SerializeField] private Color defenseColor = new(0.82f, 0.62f, 1f, 1f); // lilas

    [Header("Behavior")]
    [SerializeField, Min(1)] private int maxLogs = 6;
    [SerializeField, Min(0.1f)] private float lifetimeSeconds = 6f;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float slideDuration = 0.2f;
    [SerializeField] private float slideDistance = 40f;

    private readonly Queue<ActionLogItemUI> _activeLogs = new();

    public void ShowFromResult(ActionResolutionResult result)
    {
        TryShowLog(result.AttackPowerLogText, attackPowerIcon, attackColor);
        TryShowLog(result.DefensePowerLogText, defensePowerIcon, defenseColor);
        TryShowLog(result.AttackAccuracyLogText, attackAccuracyIcon, attackColor);
        TryShowLog(result.DefenseAccuracyLogText, defenseAccuracyIcon, defenseColor);
    }

    private void TryShowLog(string logText, Sprite icon, Color textColor)
    {
        if (string.IsNullOrWhiteSpace(logText) || contentRoot == null || logItemPrefab == null)
            return;

        ActionLogItemUI item = Instantiate(logItemPrefab, contentRoot);
        item.Bind(icon, logText, textColor);

        RectTransform rect = item.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = item.gameObject.AddComponent<CanvasGroup>();

        _activeLogs.Enqueue(item);

        if (_activeLogs.Count > maxLogs)
        {
            ActionLogItemUI oldest = _activeLogs.Dequeue();
            if (oldest != null)
                Destroy(oldest.gameObject);
        }

        StartCoroutine(AnimateEntryAndExpire(item, rect, canvasGroup));
    }

    private IEnumerator AnimateEntryAndExpire(ActionLogItemUI item, RectTransform rect, CanvasGroup canvasGroup)
    {
        if (item == null)
            yield break;

        yield return new WaitForSeconds(lifetimeSeconds);
        RemoveAndDestroy(item);

        /* Vector2 basePosition = rect != null ? rect.anchoredPosition : Vector2.zero;
        Vector2 startPosition = basePosition + Vector2.left * slideDistance;

        float t = 0f;
        canvasGroup.alpha = 0f;
        if (rect != null)
            rect.anchoredPosition = startPosition;

        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / slideDuration);
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, p);
            if (rect != null)
                rect.anchoredPosition = Vector2.Lerp(startPosition, basePosition, p);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        if (rect != null)
            rect.anchoredPosition = basePosition;

        yield return new WaitForSeconds(lifetimeSeconds);

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, p);
            yield return null;
        }

        RemoveAndDestroy(item); */
    }

    private void RemoveAndDestroy(ActionLogItemUI item)
    {
        if (item == null)
            return;

        bool removed = false;
        int count = _activeLogs.Count;
        for (int i = 0; i < count; i++)
        {
            ActionLogItemUI current = _activeLogs.Dequeue();
            if (!removed && current == item)
            {
                removed = true;
                continue;
            }

            _activeLogs.Enqueue(current);
        }

        Destroy(item.gameObject);
    }
}