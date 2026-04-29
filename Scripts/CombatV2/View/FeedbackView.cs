using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackView : MonoBehaviour
{
    public TMP_Text TurnOwnerText;
    [SerializeField] private PlayerFeedbacks playerFeedbacks;
    [SerializeField] private EnemyFeedbacks enemyFeedbacks;
    [Header("Attack Effect")]
    [SerializeField] private GameObject attackEffectPrefab;
    [SerializeField] private Transform playerAttackEffectAnchor;
    [SerializeField] private Transform enemyAttackEffectAnchor;
    [SerializeField] private float attackEffectDuration = 0.75f;
    [SerializeField] private CanvasGroup canvasGroup;
    void Start()
    {
        playerFeedbacks = FindObjectOfType<PlayerFeedbacks>();
        enemyFeedbacks = FindObjectOfType<EnemyFeedbacks>();

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
        if (attackEffectPrefab == null || anchor == null)
        {
            Debug.Log("[Feedback] Attack effect prefab or anchor is missing.");
            return;
        }

        GameObject effect = Instantiate(attackEffectPrefab, anchor.position, Quaternion.identity, anchor);
        Image slashImage = effect.GetComponent<Image>();
        slashImage.color = attackerIsPlayer 
            ? new Color(1f, 1f, 1f, 1f)
            : new Color(1f, 0.3f, 0.3f, 1f);
        RectTransform rect = effect.GetComponent<RectTransform>();
        rect.rotation = Quaternion.Euler(0, 0, Random.Range(-30f, 30f));
        StartCoroutine(AnimateSlash(rect, canvasGroup));

        Destroy(effect, attackEffectDuration);
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

    private IEnumerator AnimateSlash(RectTransform rect, CanvasGroup cg)
    {
        float duration = 0.3f;
        float time = 0f;

        Vector3 startScale = Vector3.one * 1f;
        Vector3 midScale = Vector3.one * 1.8f;
        Vector3 endScale = Vector3.one * 2.3f;

        rect.localScale = startScale;
        cg.alpha = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            if (t < 0.3f)
            {
                float p = t / 0.3f;
                rect.localScale = Vector3.Lerp(startScale, midScale, p);
                cg.alpha = Mathf.Lerp(0, 1, p);
            }
            else
            {
                float p = (t - 0.3f) / 0.7f;
                rect.localScale = Vector3.Lerp(midScale, endScale, p);
                cg.alpha = Mathf.Lerp(1, 0, p);
            }

            yield return null;
        }

        Destroy(rect.gameObject);
    }
}
