using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatLogView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private CombatLogItemUI logItemPrefab;
    [SerializeField] private GameObject panelRoot;

    [Header("Colors")]
    public Color defaultColor = new(0.3f, 0.95f, 1f, 1f);

    [Header("Behavior")]
    [SerializeField, Min(1)] private int maxLogs = 7;
    [SerializeField, Min(0.1f)] private float lifetimeSeconds = 8f;

    private readonly Queue<CombatLogItemUI> activeLogs = new();

    public void ShowTriggerFeedback(PerkTriggeredEvent evt)
    {
        Logger.Log($"[Perk Triggered] {evt.PerkId}");
        if (evt.SourceTrick?.Definition != null)
        {
            string displayName = string.IsNullOrWhiteSpace(evt.SourceTrick.Definition.DisplayName)
                ? evt.SourceTrick.Definition.Id
                : evt.SourceTrick.Definition.DisplayName;

            ShowTextLog($"{displayName} acionado", evt.SourceTrick.Definition.Icon, defaultColor);
            return;
        }
    }

    public void ShowTextLog(string logText, Sprite icon = null, Color? textColor = null)
    {
        if (string.IsNullOrWhiteSpace(logText) || contentRoot == null || logItemPrefab == null)
            return;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        CombatLogItemUI item = Instantiate(logItemPrefab, contentRoot);
        item.Bind(icon, logText, textColor ?? defaultColor);

        RectTransform rect = item.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = item.gameObject.AddComponent<CanvasGroup>();

        activeLogs.Enqueue(item);

        if (activeLogs.Count > maxLogs)
        {
            CombatLogItemUI oldest = activeLogs.Dequeue();
            if (oldest != null)
                Destroy(oldest.gameObject);
        }

        StartCoroutine(AnimateEntryAndExpire(item, rect, canvasGroup));
    }

    private IEnumerator AnimateEntryAndExpire(CombatLogItemUI item, RectTransform rect, CanvasGroup canvasGroup)
    {
        if (item == null)
            yield break;

        yield return new WaitForSeconds(lifetimeSeconds);
        RemoveAndDestroy(item);
    }

    private void RemoveAndDestroy(CombatLogItemUI item)
    {
        if (item == null)
            return;

        bool removed = false;
        int count = activeLogs.Count;
        for (int i = 0; i < count; i++)
        {
            CombatLogItemUI current = activeLogs.Dequeue();
            if (!removed && current == item)
            {
                removed = true;
                continue;
            }

            activeLogs.Enqueue(current);
        }

        Destroy(item.gameObject);

        if (panelRoot != null && activeLogs.Count == 0)
            panelRoot.SetActive(false);
    }
}