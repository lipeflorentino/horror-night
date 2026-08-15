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

    public void ShowTrickFeedback(TrickRuntimeInstance trick, string actionType = "triggered")
    {
        if (trick == null || trick.Definition == null)
            return;

        Color textColor = defaultColor;

        if (actionType == "triggered")
        {
            textColor = Color.yellow;
        }
        else if (actionType == "expired")
        {
            textColor = Color.red;
        }
        else if (actionType == "activated")
        {
            textColor = Color.green;
        }

        string rarityColor = Colorization.GetRarityColor(trick.Definition.Rarity);
        string displayName = string.IsNullOrWhiteSpace(trick.Definition.DisplayName)
            ? trick.Definition.Id
            : trick.Definition.DisplayName;

        ShowTextLog($"<color={rarityColor}>{displayName}</color> {actionType}", trick.Definition.Icon, textColor);
    }

    public void ShowDrawbackFeedback(Battler battler, DrawbackRuntimeInstance drawback, string actionType = "applied")
    {
        if (drawback == null || drawback.Definition == null)
            return;

        Color textColor = defaultColor;

        if (actionType == "applied")
        {
            textColor = Color.yellow;
        }
        else if (actionType == "expired")
        {
            textColor = Color.green;
        }

        string displayName = string.IsNullOrWhiteSpace(drawback.Definition.DisplayName)
            ? drawback.Definition.Id
            : drawback.Definition.DisplayName;

        ShowTextLog($"<color=red>{displayName}</color> {actionType} to {battler.Name}", drawback.Definition.Icon, textColor);
    }

    public void ShowBattlerStateFeedback(Battler battler, BattlerStateRuntimeInstance state, string actionType = "applied")
    {
        if (state == null || state.Definition == null)
            return;

        Color textColor = defaultColor;

        if (actionType == "applied")
        {
            textColor = Color.yellow;
        }
        else if (actionType == "expired")
        {
            textColor = Color.green;
        }

        string displayName = string.IsNullOrWhiteSpace(state.Definition.DisplayName)
            ? state.Definition.Id
            : state.Definition.DisplayName;

        ShowTextLog($"<color=yellow>{displayName}</color> {actionType} to {battler.Name}", state.Definition.Icon, textColor);
    }

    public void ShowTextLog(string logText, Sprite icon = null, Color? textColor = null)
    {
        if (string.IsNullOrWhiteSpace(logText) || contentRoot == null || logItemPrefab == null)
            return;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        CombatLogItemUI item = Instantiate(logItemPrefab, contentRoot);
        item.Bind(icon, logText, textColor ?? defaultColor);

        activeLogs.Enqueue(item);

        if (activeLogs.Count > maxLogs)
        {
            CombatLogItemUI oldest = activeLogs.Dequeue();
            if (oldest != null)
                Destroy(oldest.gameObject);
        }

        StartCoroutine(AnimateEntryAndExpire(item));
    }

    private IEnumerator AnimateEntryAndExpire(CombatLogItemUI item)
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