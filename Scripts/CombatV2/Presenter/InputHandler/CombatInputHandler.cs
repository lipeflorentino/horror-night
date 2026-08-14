using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Responsável apenas por receber o input do jogador, validar se a ação é permitida no estado atual,
/// e solicitar atualizações à View ou enviar o comando final ao CombatManager.
/// </summary>
public class CombatInputHandler : MonoBehaviour
{
    // ==========================================
    // SESSÃO: DEPENDÊNCIAS E ESTADO
    // ==========================================
    [Header("Dependencies")]
    [SerializeField] private CombatManager Combat;
    [SerializeField] private DiceAllocationView diceAllocationView;

    // Estado da seleção atual do jogador (Model do Input)
    private readonly List<DiceStatType> PowerDiceTypes = new();
    private readonly List<DiceStatType> AccuracyDiceTypes = new();
    
    private ActionType? SelectedAction = null;
    private ActionType AllowedAction = ActionType.Attack;
    
    private DiceStatType SelectedPowerDiceType = DiceStatType.Body;
    private DiceStatType SelectedAccuracyDiceType = DiceStatType.Mind;
    
    private bool IsWaitingTurnResolution = false;

    // ==========================================
    // SESSÃO: INICIALIZAÇÃO E CICLO DE VIDA
    // ==========================================
    public void Init(CombatManager cm)
    {
        Combat = cm;
    }

    public void BindDiceAllocationView(DiceAllocationView view)
    {
        UnbindCurrentView();
        diceAllocationView = view;

        if (diceAllocationView != null)
        {
            diceAllocationView.ConfirmClicked += OnConfirmAction;
            diceAllocationView.ThresholdStrategyChanged += OnThresholdStrategySelected;
            diceAllocationView.SetConfirmInteractable(false);
            diceAllocationView.HideAllocationPanel();
        }
    }

    private void UnbindCurrentView()
    {
        if (diceAllocationView != null)
        {
            diceAllocationView.ConfirmClicked -= OnConfirmAction;
            diceAllocationView.ThresholdStrategyChanged -= OnThresholdStrategySelected;
        }
    }

    private void OnDestroy()
    {
        UnbindCurrentView();
    }

    // ==========================================
    // SESSÃO: RECEPÇÃO DE INPUTS DA VIEW (AÇÕES)
    // ==========================================
    public void SetAllowedAction(ActionType allowedAction)
    {
        AllowedAction = allowedAction;
        ResetSelectionState();
        
        diceAllocationView.HideAllocationPanel();
        RefreshAllUI();
    }

    public void OnSelectAttack()
    {
        if (IsWaitingTurnResolution || AllowedAction != ActionType.Attack) return;

        SelectedAction = ActionType.Attack;
        diceAllocationView.ShowAllocationPanel("Attack");
        UpdateConfirmAvailability();
    }

    public void OnSelectDefend()
    {
        if (IsWaitingTurnResolution || AllowedAction != ActionType.Defense) return;

        SelectedAction = ActionType.Defense;
        diceAllocationView.ShowAllocationPanel("Defense");
        UpdateConfirmAvailability();
    }

    public void OnAddDice(DiceStatType diceStatType, DiceRollType diceRollType)
    {
        if (IsWaitingTurnResolution || !CanAddDiceToRoll() || !CanUseDiceType(diceStatType) || GetAllocatedStatCount(diceStatType) >= GetMaxAllowedDiceCount(diceStatType)) return;

        if (diceRollType == DiceRollType.Power)
        {
            SelectedPowerDiceType = diceStatType; 
            PowerDiceTypes.Add(SelectedPowerDiceType);
        }
        else
        {
            SelectedAccuracyDiceType = diceStatType;
            AccuracyDiceTypes.Add(SelectedAccuracyDiceType);
        } 
            
        RefreshAllUI();
    }

    public void OnRemoveDice(DiceStatType diceStatType, DiceRollType diceRollType)
    {
        if (IsWaitingTurnResolution) return;

        if (diceRollType == DiceRollType.Power && PowerDiceTypes.Count > 0)
        {
            PowerDiceTypes.Remove(diceStatType);
        }
        else if (diceRollType == DiceRollType.Accuracy && AccuracyDiceTypes.Count > 0)
        {
            AccuracyDiceTypes.Remove(diceStatType);
        }

        RefreshAllUI();
    }

    public void OnThresholdStrategySelected(CombatRules.ThresholdStrategy strategy)
    {
        CombatRules.SetPlayerStrategy(strategy);
        RefreshAllUI(); // Agora encapsula a atualização geral
    }

    public void OnSelectInfoPanel()
    {
        Combat.View.SetInfoPanelVisible();
    }

    // ==========================================
    // SESSÃO: COMANDOS DE RESOLUÇÃO DE TURNO
    // ==========================================
    public void OnConfirmAction()
    {
        if (IsWaitingTurnResolution || SelectedAction == null) return;
        if (PowerDiceTypes.Count <= 0 || AccuracyDiceTypes.Count <= 0) return;

        IsWaitingTurnResolution = true;
        Combat.ReceivePlayerInput(SelectedAction.Value, new List<DiceStatType>(PowerDiceTypes), new List<DiceStatType>(AccuracyDiceTypes));
        
        SelectedAction = null;
        diceAllocationView.HideAllocationPanel();
        UpdateConfirmAvailability();
    }

    public void OnSkipTurn()
    {
        if (IsWaitingTurnResolution || AllowedAction != ActionType.Attack) return;

        ResetSelectionState();
        diceAllocationView.HideAllocationPanel();
        RefreshSelectionPreview();

        IsWaitingTurnResolution = true;
        Combat.ReceivePlayerSkipTurn();
        
        RefreshDiceButtons();
        UpdateConfirmAvailability();
    }

    // ==========================================
    // SESSÃO: ORQUESTRAÇÃO DE VIEW (ATUALIZAÇÕES)
    // ==========================================
    public void RefreshAllUI()
    {
        RefreshSelectionPreview();
        RefreshDiceButtons();
        UpdateConfirmAvailability();
        Combat.View.UpdateView(Combat.Player, Combat.Enemy);
    }

    private void UpdateConfirmAvailability()
    {
        bool hasValidDiceAllocation = PowerDiceTypes.Count > 0 && AccuracyDiceTypes.Count > 0;
        bool isAvailable = !IsWaitingTurnResolution && SelectedAction != null && hasValidDiceAllocation;
        diceAllocationView.SetConfirmInteractable(isAvailable);
    }

    private void RefreshDiceButtons()
    {
        if (Combat.View.DiceAllocationView == null) return;

        bool canAllocate = !IsWaitingTurnResolution;

        foreach (DiceStatType stat in Enum.GetValues(typeof(DiceStatType)))
        {
            bool canAdd = canAllocate && CanUseDiceType(stat) && CanAddDiceToRoll() && GetAllocatedStatCount(stat) < GetMaxAllowedDiceCount(stat);

            Combat.View.DiceAllocationView.SetAddDiceButtonInteractable(stat, DiceRollType.Power, canAdd);
            Combat.View.DiceAllocationView.SetAddDiceButtonInteractable(stat, DiceRollType.Accuracy, canAdd);
            
            Combat.View.DiceAllocationView.SetRemoveDiceButtonInteractable(stat, DiceRollType.Power, PowerDiceTypes.Contains(stat));
            Combat.View.DiceAllocationView.SetRemoveDiceButtonInteractable(stat, DiceRollType.Accuracy, AccuracyDiceTypes.Contains(stat));

            Combat.View.DiceAllocationView.SetAllocatorCount(stat, DiceRollType.Power, PowerDiceTypes.FindAll(x => x == stat).Count);
            Combat.View.DiceAllocationView.SetAllocatorCount(stat, DiceRollType.Accuracy, AccuracyDiceTypes.FindAll(x => x == stat).Count);
        }
    }

    private void RefreshSelectionPreview()
    {
        if (Combat.View.DiceAllocationView == null) return;

        // 1. Base Stats Update
        Combat.View.DiceAllocationView.UpdateDiceAllocationStats(Combat.Player.Mind, Combat.Player.Heart, Combat.Player.Body);

        // 2. Coleta de Dados via Serviços
        var diceService = Combat.GetDiceService();
        (List<DiceStatType> powerTypes, List<int> powerFaces, _) = diceService.ConvertToFacesWithTypes(Combat.Player, PowerDiceTypes);
        (List<DiceStatType> accuracyTypes, List<int> accuracyFaces, _) = diceService.ConvertToFacesWithTypes(Combat.Player, AccuracyDiceTypes);
        
        // USO DO NOVO CALCULATOR EXTERNO
        (int powerMax, DiceStatType powerPrimStat) = DicePreviewCalculator.GetPreviewMaxValueAndPrimaryStat(powerTypes, powerFaces, DiceRollType.Power);
        (int accMax, DiceStatType accPrimStat) = DicePreviewCalculator.GetPreviewMaxValueAndPrimaryStat(accuracyTypes, accuracyFaces, DiceRollType.Accuracy);
        
        var powerBoundaries = Combat.GetPlayerTierBoundaries(powerMax, powerPrimStat, DiceRollType.Power, PowerDiceTypes.Count);
        var accuracyBoundaries = Combat.GetPlayerTierBoundaries(accMax, accPrimStat, DiceRollType.Accuracy, AccuracyDiceTypes.Count);
        
        // 3. Montagem do Contexto e Envio para a View
        Dictionary<DiceStatType, int> statTargets = new()
        {
            { DiceStatType.Mind, Combat.Player.GetBaseStatValue(DiceStatType.Mind) },
            { DiceStatType.Heart, Combat.Player.GetBaseStatValue(DiceStatType.Heart) },
            { DiceStatType.Body, Combat.Player.GetBaseStatValue(DiceStatType.Body) },
        };
        
        DiceAllocationContext previewData = DiceAllocationCalculator.CalculatePreview(
            baseActionPower: Combat.GetEffectivePlayerActionPower(),
            powerDiceTypes: powerTypes, 
            powerFaces: powerFaces,
            accuracyDiceTypes: accuracyTypes, 
            accuracyFaces: accuracyFaces,
            powerTierBoundaries: powerBoundaries, 
            accuracyTierBoundaries: accuracyBoundaries,
            statBaseTargets: statTargets, 
            powerPrimaryStat: powerPrimStat,
            allocatedPowerDiceCount: PowerDiceTypes.Count,
            selectedAction: SelectedAction ?? AllowedAction,
            seccondaryEffects: Combat.Player.ActionSecondaryEffects
        );

        previewData.WarnWearStats = new List<DiceStatType>();
        Dictionary<DiceStatType, int> rawDiceCounts = new();
        
        foreach (var t in PowerDiceTypes) { rawDiceCounts.TryAdd(t, 0); rawDiceCounts[t]++; }
        foreach (var t in AccuracyDiceTypes) { rawDiceCounts.TryAdd(t, 0); rawDiceCounts[t]++; }
        foreach (var kvp in rawDiceCounts)
        {
            if (kvp.Value >= 3) previewData.WarnWearStats.Add(kvp.Key);
        }
        
        Combat.View.DiceAllocationView.UpdateSelectionPreview(previewData);

        // 4. Custos e Displays Finais
        Dictionary<DiceStatType, int> allocationCosts = new()
        {
            { DiceStatType.Mind, GetAllocatedStatCount(DiceStatType.Mind) },
            { DiceStatType.Heart, GetAllocatedStatCount(DiceStatType.Heart) },
            { DiceStatType.Body, GetAllocatedStatCount(DiceStatType.Body) },
        };

        Combat.View.DiceAllocationView.UpdateAllocationCostFeedback(allocationCosts);
        Combat.View.DiceAllocationView.UpdateDicePoolDisplay(
            Combat.Player.CurrentActionDices, Combat.Player.MaxDices, 
            PowerDiceTypes.Count, AccuracyDiceTypes.Count);
    }

    // ==========================================
    // SESSÃO: REGRAS DE VALIDAÇÃO (HELPERS LOCAIS)
    // ==========================================
    private void ResetSelectionState()
    {
        SelectedAction = null;
        IsWaitingTurnResolution = false;
        PowerDiceTypes.Clear();
        AccuracyDiceTypes.Clear();
        SelectedPowerDiceType = SelectedAccuracyDiceType = GetFirstAvailableDiceType();
    }

    private bool CanUseDiceType(DiceStatType diceType) => GetDiceMaxValueForType(diceType) > 0;

    private bool CanAddDiceToRoll() => GetRemainingDiceCount() > 0;

    private int GetRemainingDiceCount() => Mathf.Max(0, Combat.Player.CurrentActionDices - (PowerDiceTypes.Count + AccuracyDiceTypes.Count));

    private int GetAllocatedStatCount(DiceStatType stat) => PowerDiceTypes.FindAll(x => x == stat).Count + AccuracyDiceTypes.FindAll(x => x == stat).Count;

    private int GetMaxAllowedDiceCount(DiceStatType stat)
    {
        int max = Combat.Player.MaxDices; // Valor padrão alto para não limitar, será ajustado pelos perks
        PerkModifierTarget target = stat switch
        {
            DiceStatType.Mind => PerkModifierTarget.MindDice,
            DiceStatType.Heart => PerkModifierTarget.HeartDice,
            DiceStatType.Body => PerkModifierTarget.BodyDice,
            _ => PerkModifierTarget.BodyDice
        };

        var perks = Combat.Player.GetEffectivePerks();
        foreach (var perk in perks)
        {
            if (perk.Definition.Rule.ModifierTarget == target && perk.Definition.Rule.Operation == PerkOperation.Restrain)
            {
                max = Mathf.Min(max, (int)perk.Definition.Rule.Value);
            }
        }
        return max;
    }

    private DiceStatType GetFirstAvailableDiceType()
    {
        if (CanUseDiceType(DiceStatType.Body)) return DiceStatType.Body;
        if (CanUseDiceType(DiceStatType.Heart)) return DiceStatType.Heart;
        return DiceStatType.Mind;
    }

    // Mantidos para compatibilidade externa, embora possam ser delegados no futuro
    public int GetDiceMaxValueForType(DiceStatType diceType) => Combat.GetDiceService().GetDiceMaxValueForType(Combat.Player, diceType);
    
    public List<int> GetDiceFacesForSelection(IReadOnlyList<DiceStatType> diceTypes, bool isAggregated = false) => 
        isAggregated ? Combat.GetDiceService().ConvertToAggregatedFaces(Combat.Player, diceTypes) : Combat.GetDiceService().ConvertToFaces(Combat.Player, diceTypes);
}