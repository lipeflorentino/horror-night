using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackView : MonoBehaviour
{
    public TMP_Text TurnOwnerText;
    [SerializeField] private PlayerFeedbacks playerFeedbacks;
    [SerializeField] private EnemyFeedbacks enemyFeedbacks;
    [SerializeField] private ActionLogView actionLogView;

    [Header("Attack Effect")]
    [SerializeField] private GameObject playerAttackEffectPrefab;
    [SerializeField] private GameObject enemyAttackEffectPrefab;

    [Header("Attack Configs")]
    [SerializeField] private Transform playerAttackEffectAnchor;
    [SerializeField] private Transform enemyAttackEffectAnchor;
    [SerializeField] private float attackEffectDuration = 0.75f;

    void Start()
    {
        playerFeedbacks = FindObjectOfType<PlayerFeedbacks>();
        enemyFeedbacks = FindObjectOfType<EnemyFeedbacks>();
        actionLogView = FindObjectOfType<ActionLogView>();

        if (playerFeedbacks == null)
            Debug.LogError("FeedbackView: PlayerFeedbacks component is missing.");

        if (enemyFeedbacks == null)
            Debug.LogError("FeedbackView: EnemyFeedbacks component is missing.");
    }
    public void ShowResolveFeedback(ActionResolutionResult result, bool targetIsPlayer)
    {
        if (!string.IsNullOrWhiteSpace(result.FeedbackText))
        {
            ShowStatusText(result.FeedbackText, targetIsPlayer);
        }

        actionLogView.ShowFromResult(result);

        if (!result.AppliesDamage)
            return;

        if (targetIsPlayer)
        {
            playerFeedbacks.ShowPlayerDamageFlash();
            return;
        }

        enemyFeedbacks.ShowDamagePopup(result.Damage);
    }

    public void ShowAttackEffect(bool attackerIsPlayer)
    {
        Transform anchor = attackerIsPlayer ? playerAttackEffectAnchor : enemyAttackEffectAnchor;
        GameObject attackEffectPrefab = attackerIsPlayer ? playerAttackEffectPrefab : enemyAttackEffectPrefab;

        if (attackEffectPrefab == null || anchor == null)
        {
            Debug.Log("[Feedback] Attack effect prefab or anchor is missing.");
            return;
        }

        GameObject effect = Instantiate(attackEffectPrefab, anchor.position, Quaternion.identity, anchor);
        Image slashImage = effect.GetComponentInChildren<Image>();
        if (slashImage != null)
        {
            slashImage.color = attackerIsPlayer
                ? new Color(1f, 1f, 1f, 1f)
                : new Color(1f, 0.3f, 0.3f, 1f);
        }

        RectTransform rect = effect.GetComponent<RectTransform>();
        if (rect == null)
        {
            Destroy(effect, attackEffectDuration);
            return;
        }

        rect.rotation = Quaternion.Euler(0, 0, Random.Range(-30f, 30f));
        StartCoroutine(AnimateSlash(rect, slashImage, attackEffectDuration));
    }

    private void ShowStatusText(string text, bool targetIsPlayer)
    {
        if (targetIsPlayer)
        {
            playerFeedbacks.ShowStatusText(text);
            return;
        }

        enemyFeedbacks.ShowStatusPopup(text);
    }

    public void ShowHealFeedback(int healAmount)
    {
        // Implement visual feedback for healing
        Debug.Log($"Healed: {healAmount}");
    }

    public void ShowDodgeFeedback()
    {
        // Implement visual feedback for dodging an attack
        Debug.Log("Attack Dodged!");
    }

    public void ShowTurnStartFeedback(bool isPlayerTurn)
    {
        string turnOwner = isPlayerTurn ? "Player's Turn" : "Enemy's Turn";

        if (TurnOwnerText != null)
            TurnOwnerText.text = turnOwner;
    }

    public void ShowSkipTurnFeedback(bool isPlayerTurn)
    {
        ShowStatusText("Turn skipped", isPlayerTurn);
    }

    private IEnumerator AnimateSlash(RectTransform rect, Image slashImage, float duration)
    {
        duration = Mathf.Max(0.01f, duration);
        float time = 0f;

        Vector3 startScale = Vector3.one * 1f;
        Vector3 midScale = Vector3.one * 1.8f;
        Vector3 endScale = Vector3.one * 2.3f;

        rect.localScale = startScale;
        SetImageAlpha(slashImage, 0f);

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            if (t < 0.3f)
            {
                float p = t / 0.3f;
                rect.localScale = Vector3.Lerp(startScale, midScale, p);
                SetImageAlpha(slashImage, Mathf.Lerp(0, 1, p));
            }
            else
            {
                float p = (t - 0.3f) / 0.7f;
                rect.localScale = Vector3.Lerp(midScale, endScale, p);
                SetImageAlpha(slashImage, Mathf.Lerp(1, 0, p));
            }

            yield return null;
        }

        Destroy(rect.gameObject);
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
