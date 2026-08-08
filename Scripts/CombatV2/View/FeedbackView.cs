using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FeedbackView : MonoBehaviour
{
    public TMP_Text TurnOwnerText;
    [SerializeField] private StatusEffectFeedbacks playerStatusEffectFeedbacks;
    [SerializeField] private StatusEffectFeedbacks enemyStatusEffectFeedbacks;

    private DrawbackService drawbackService;
    private BattlerStateService battlerStateService;
    private TrickService trickService;
    private PerkService perkService;
    [SerializeField] private PlayerFeedbacks playerFeedbacks;
    [SerializeField] private EnemyFeedbacks enemyFeedbacks;
    [SerializeField] private CombatLogView combatLogView;
    [SerializeField] private AttackEffectFeedbacks attackEffectFeedbacks;

    void Start()
    {
        ResolveFeedbackDependencies();
    }

    public void Init(TrickService trickService, PerkService perkService, DrawbackService drawbackService, BattlerStateService battlerStateService, Battler playerBattler, Battler enemyBattler)
    {
        if (this.trickService != null)
        {
            this.trickService.OnTrickExpired -= HandleTrickExpired;
        }

        if (this.perkService != null)
        {
            this.perkService.OnPerkTriggered -= HandlePerkTriggered;
        }

        if (this.drawbackService != null)
        {
            this.drawbackService.OnDrawbackApplied -= HandleDrawbackApplied;
            this.drawbackService.OnDrawbackRemoved -= HandleDrawbackRemoved;
        }

        if (this.battlerStateService != null)
        {
            this.battlerStateService.OnBattlerStateApplied -= HandleBattlerStateApplied;
            this.battlerStateService.OnBattlerStateRemoved -= HandleBattlerStateRemoved;
        }

        ResolveFeedbackDependencies();

        this.trickService = trickService;
        this.perkService = perkService;
        this.drawbackService = drawbackService;
        this.battlerStateService = battlerStateService;

        if (playerStatusEffectFeedbacks != null)
            playerStatusEffectFeedbacks.Initialize(drawbackService, battlerStateService, playerBattler);

        if (enemyStatusEffectFeedbacks != null)
            enemyStatusEffectFeedbacks.Initialize(drawbackService, battlerStateService, enemyBattler);

        if (this.trickService != null)
        {
            this.trickService.OnTrickExpired += HandleTrickExpired;
        }

        if (this.perkService != null)
        {
            this.perkService.OnPerkTriggered += HandlePerkTriggered;
        }

        if (this.drawbackService != null)
        {
            this.drawbackService.OnDrawbackApplied += HandleDrawbackApplied;
            this.drawbackService.OnDrawbackRemoved += HandleDrawbackRemoved;
        }

        if (this.battlerStateService != null)
        {
            this.battlerStateService.OnBattlerStateApplied += HandleBattlerStateApplied;
            this.battlerStateService.OnBattlerStateRemoved += HandleBattlerStateRemoved;
        }
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

    private void HandlePerkTriggered(PerkTriggeredEvent evt)
    {
        if (combatLogView != null)
            combatLogView.ShowTrickFeedback(evt.SourceTrick, "triggered");
    }

    private void HandleTrickExpired(Battler battler, TrickRuntimeInstance trick)
    {
        Logger.Log("[HandleTrickExpired] Handling trick expired for " + battler.Name + ": " + trick.Definition.DisplayName);
        if (combatLogView != null)
            combatLogView.ShowTrickFeedback(trick, "expired");
    }

    private void HandleDrawbackApplied(Battler battler, DrawbackRuntimeInstance drawback)
    {
        if (combatLogView != null)
            combatLogView.ShowDrawbackFeedback(battler, drawback, "applied");
    }

    private void HandleDrawbackRemoved(Battler battler, DrawbackRuntimeInstance drawback)
    {
        if (combatLogView != null)
            combatLogView.ShowDrawbackFeedback(battler, drawback, "expired");
    }

    private void HandleBattlerStateApplied(Battler battler, BattlerStateRuntimeInstance state)
    {
        if (combatLogView != null)
            combatLogView.ShowBattlerStateFeedback(battler, state, "applied");
    }

    private void HandleBattlerStateRemoved(Battler battler, BattlerStateRuntimeInstance state)
    {
        if (combatLogView != null)
            combatLogView.ShowBattlerStateFeedback(battler, state, "expired");
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

    public void ShowResolveFeedback(ActionResolutionResult result, bool attackerIsPlayer)
    {
        bool targetIsPlayer = !attackerIsPlayer;

        if (!string.IsNullOrWhiteSpace(result.AttackFeedbackText))
        {
            ShowStatusText(result.AttackFeedbackText, attackerIsPlayer, true);
        }

        if (!string.IsNullOrWhiteSpace(result.DefenseFeedbackText))
        {
            ShowStatusText(result.DefenseFeedbackText, targetIsPlayer, false);
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

    private void ShowStatusText(string text, bool targetIsPlayer, bool isAttackFeedback)
    {
        if (targetIsPlayer)
        {
            if (playerFeedbacks == null)
            {
                Debug.LogError("[FeedbackView] PlayerFeedbacks reference is missing, cannot show status text.");
                return;
            }

            playerFeedbacks.ShowStatusText(text, isAttackFeedback);
            return;
        }

        if (enemyFeedbacks == null)
        {
            Debug.LogError("[FeedbackView] EnemyFeedbacks reference is missing, cannot show status popup.");
            return;
        }

        enemyFeedbacks.ShowStatusPopup(text, isAttackFeedback);
    }

    public void ShowDiceCostFeedback(Dictionary<DiceStatType, int> costs)
    {
        if (playerFeedbacks == null) { Debug.LogError("[FeedbackView] PlayerFeedbacks reference is missing."); return; }
        foreach (var kvp in costs)
            if (kvp.Value > 0)
                playerFeedbacks.ShowResourceCostPopup(kvp.Key, kvp.Value);
    }

    public void ShowTurnStartFeedback(bool isPlayerTurn)
    {
        string turnOwner = isPlayerTurn ? "Turno do Jogador" : "Turno do Inimigo";

        if (TurnOwnerText != null)
            TurnOwnerText.text = turnOwner;
    }

    public void ShowSkipTurnFeedback(bool isPlayerTurn)
    {
        ShowStatusText("Turno pulado", isPlayerTurn, false);
    }

    private void OnDestroy()
    {
        if (perkService != null)
            perkService.OnPerkTriggered -= HandlePerkTriggered;

        if (trickService != null)
            trickService.OnTrickExpired -= HandleTrickExpired;
    }
}