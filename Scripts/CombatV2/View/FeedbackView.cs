using TMPro;
using UnityEngine;

public class FeedbackView : MonoBehaviour
{
    public TMP_Text TurnOwnerText;
    [SerializeField] private StatusEffectFeedbacks playerStatusEffectFeedbacks;
    [SerializeField] private StatusEffectFeedbacks enemyStatusEffectFeedbacks;

    private PerkService perkService;
    [SerializeField] private PlayerFeedbacks playerFeedbacks;
    [SerializeField] private EnemyFeedbacks enemyFeedbacks;
    [SerializeField] private CombatLogView combatLogView;
    [SerializeField] private AttackEffectFeedbacks attackEffectFeedbacks;

    void Start()
    {
        ResolveFeedbackDependencies();
    }

    public void Init(PerkService perkService, TrickService trickService, Battler playerBattler, Battler enemyBattler)
    {
        if (this.perkService != null)
            this.perkService.OnPerkTriggered -= HandlePerkTriggered;

        ResolveFeedbackDependencies();
        this.perkService = perkService;

        if (playerStatusEffectFeedbacks != null)
            playerStatusEffectFeedbacks.Initialize(perkService, trickService, playerBattler);

        if (enemyStatusEffectFeedbacks != null)
            enemyStatusEffectFeedbacks.Initialize(perkService, trickService, enemyBattler);

        if (this.perkService != null)
            this.perkService.OnPerkTriggered += HandlePerkTriggered;
    }
    
    private void ResolveFeedbackDependencies()
    {
        if (playerFeedbacks == null)
            playerFeedbacks = FindObjectOfType<PlayerFeedbacks>();

        if (enemyFeedbacks == null)
            enemyFeedbacks = FindObjectOfType<EnemyFeedbacks>();

        if (combatLogView == null)
            combatLogView = FindObjectOfType<CombatLogView>();

        if (attackEffectFeedbacks == null)
            attackEffectFeedbacks = FindObjectOfType<AttackEffectFeedbacks>();

        if (playerFeedbacks == null)
            Debug.LogError("[FeedbackView] PlayerFeedbacks reference not found in scene.");

        if (enemyFeedbacks == null)
            Debug.LogError("[FeedbackView] EnemyFeedbacks reference not found in scene.");

        if (attackEffectFeedbacks == null)
            Debug.LogError("[FeedbackView] AttackEffectFeedbacks reference not found in scene.");

        ResolveStatusEffectFeedbacks();
    }

    private void ResolveStatusEffectFeedbacks()
    {
        if (playerStatusEffectFeedbacks != null && enemyStatusEffectFeedbacks != null)
            return;

        StatusEffectFeedbacks[] feedbackViews = FindObjectsOfType<StatusEffectFeedbacks>();
        if (feedbackViews == null || feedbackViews.Length == 0)
            return;

        for (int i = 0; i < feedbackViews.Length; i++)
        {
            StatusEffectFeedbacks view = feedbackViews[i];
            if (view == null)
                continue;

            if (playerStatusEffectFeedbacks == null && view.OwnerBattler != null && view.OwnerBattler.IsPlayer)
                playerStatusEffectFeedbacks = view;

            if (enemyStatusEffectFeedbacks == null && view.OwnerBattler != null && !view.OwnerBattler.IsPlayer)
                enemyStatusEffectFeedbacks = view;
        }

        if (playerStatusEffectFeedbacks == null && feedbackViews.Length > 0)
            playerStatusEffectFeedbacks = feedbackViews[0];

        if (enemyStatusEffectFeedbacks == null && feedbackViews.Length > 1)
            enemyStatusEffectFeedbacks = feedbackViews[1];
    }

    private void HandlePerkTriggered(PerkTriggeredEvent evt)
    {
        if (combatLogView != null)
            combatLogView.ShowTriggerFeedback(evt);
    }

    public void ShowResolveFeedback(ActionResolutionResult result, bool attackerIsPlayer)
    {
        bool targetIsPlayer = !attackerIsPlayer;

        if (!string.IsNullOrWhiteSpace(result.AttackFeedbackText))
        {
            ShowStatusText(result.AttackFeedbackText, attackerIsPlayer);
        }

        if (!string.IsNullOrWhiteSpace(result.DefenseFeedbackText))
        {
            ShowStatusText(result.DefenseFeedbackText, targetIsPlayer);
        }

        if (!result.AppliesDamage)
            return;

        if (targetIsPlayer)
        {
            if (playerFeedbacks == null)
            {
                Debug.LogError("[FeedbackView] PlayerFeedbacks reference is missing, cannot show damage flash.");
                return;
            }

            playerFeedbacks.ShowPlayerDamageFlash();
            return;
        }

        if (enemyFeedbacks == null)
        {
            Debug.LogError("[FeedbackView] EnemyFeedbacks reference is missing, cannot show damage popup.");
            return;
        }

        enemyFeedbacks.ShowDamagePopup(result.Damage);
    }

    public void ShowAttackEffect(bool attackerIsPlayer)
    {
        if (attackEffectFeedbacks == null)
        {
            Debug.LogError("[FeedbackView] AttackEffectFeedbacks reference is missing, cannot show attack effect.");
            return;
        }

        attackEffectFeedbacks.ShowAttackEffect(attackerIsPlayer);
    }

    private void ShowStatusText(string text, bool targetIsPlayer)
    {
        if (targetIsPlayer)
        {
            if (playerFeedbacks == null)
            {
                Debug.LogError("[FeedbackView] PlayerFeedbacks reference is missing, cannot show status text.");
                return;
            }

            playerFeedbacks.ShowStatusText(text);
            return;
        }

        if (enemyFeedbacks == null)
        {
            Debug.LogError("[FeedbackView] EnemyFeedbacks reference is missing, cannot show status popup.");
            return;
        }

        enemyFeedbacks.ShowStatusPopup(text);
    }

    public void ShowTurnStartFeedback(bool isPlayerTurn)
    {
        string turnOwner = isPlayerTurn ? "Turno do Jogador" : "Turno do Inimigo";

        if (TurnOwnerText != null)
            TurnOwnerText.text = turnOwner;
    }

    public void ShowSkipTurnFeedback(bool isPlayerTurn)
    {
        ShowStatusText("Turno pulado", isPlayerTurn);
    }

    private void OnDestroy()
    {
        if (perkService != null)
            perkService.OnPerkTriggered -= HandlePerkTriggered;
    }
}