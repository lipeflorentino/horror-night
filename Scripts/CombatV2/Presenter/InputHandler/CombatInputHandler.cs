using System;
using System.Collections.Generic;
using UnityEngine;

public class CombatInputHandler : MonoBehaviour
{
    [SerializeField] private CombatManager Combat;
    [SerializeField] private DiceAllocationView diceAllocationView;
    private readonly List<DiceStatType> PowerDiceTypes = new();
    private readonly List<DiceStatType> AccuracyDiceTypes = new();
    private ActionType? SelectedAction = null;
    private ActionType AllowedAction = ActionType.Attack;
    private bool IsWaitingTurnResolution = false;
    private DiceStatType SelectedPowerDiceType = DiceStatType.Body;
    private DiceStatType SelectedAccuracyDiceType = DiceStatType.Mind;

    public void BindDiceAllocationView(DiceAllocationView view)
    {
        if (diceAllocationView != null)
            diceAllocationView.ConfirmClicked -= OnConfirmAction;

        diceAllocationView = view;

        if (diceAllocationView != null)
        {
            diceAllocationView.ConfirmClicked += OnConfirmAction;
            diceAllocationView.SetConfirmInteractable(false);
            diceAllocationView.HideAllocationPanel();
        }
    }

    private void OnDestroy()
    {
        if (diceAllocationView != null)
            diceAllocationView.ConfirmClicked -= OnConfirmAction;
    }

    public void Init(CombatManager cm)
    {
        Combat = cm;
    }

    public void UpdateCombatView()
    {
        Combat.View.UpdateView(Combat.Player, Combat.Enemy);
    }

    public void RefreshDiceAllocationUI()
    {
        RefreshSelectionPreview();
        RefreshDiceButtons();
        UpdateConfirmAvailability();
    }

    public void SetAllowedAction(ActionType allowedAction)
    {
        AllowedAction = allowedAction;
        SelectedAction = null;
        IsWaitingTurnResolution = false;
        PowerDiceTypes.Clear();
        AccuracyDiceTypes.Clear();
        SelectedPowerDiceType = SelectedAccuracyDiceType = GetFirstAvailableDiceType();
        diceAllocationView.HideAllocationPanel();

        RefreshSelectionPreview();
        RefreshDiceButtons();
        UpdateCombatView();
        UpdateConfirmAvailability();
    }

    public void OnSelectAttack()
    {
        if (IsWaitingTurnResolution) return;
        if (AllowedAction != ActionType.Attack)
        {
            Debug.Log("[Input] Attack is disabled for this turn role");
            return;
        }

        SelectedAction = ActionType.Attack;
        diceAllocationView.ShowAllocationPanel("Attack");
        UpdateConfirmAvailability();
    }

    public void OnSelectDefend()
    {
        if (IsWaitingTurnResolution) return;
        if (AllowedAction != ActionType.Defense)
        {
            Debug.Log("[Input] Defense is disabled for this turn role");
            return;
        }

        SelectedAction = ActionType.Defense;
        diceAllocationView.ShowAllocationPanel("Defense");
        UpdateConfirmAvailability();
    }

    public void OnAddDice(DiceStatType diceStatType, DiceRollType diceRollType)
    {
        if (IsWaitingTurnResolution) return;
        if (!CanAddDiceToRoll(diceRollType)) return;
        if (!CanUseDiceType(diceStatType)) return;

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
            
        RefreshSelectionPreview();
        UpdateCombatView();
        RefreshDiceButtons();
        UpdateConfirmAvailability();
    }

    public void OnRemoveDice(DiceStatType diceStatType, DiceRollType diceRollType)
    {
        if (IsWaitingTurnResolution) return;
        if (diceRollType == DiceRollType.Power)
        {
            if (PowerDiceTypes.Count <= 0) return;
            PowerDiceTypes.Remove(diceStatType);
        }
        else
        {
            if (AccuracyDiceTypes.Count <= 0) return;
            AccuracyDiceTypes.Remove(diceStatType);
        }

        RefreshSelectionPreview();
        UpdateCombatView();
        RefreshDiceButtons();
        UpdateConfirmAvailability();
    }

    public void OnConfirmAction()
    {
        if (IsWaitingTurnResolution) return;
        if (SelectedAction == null)
        {
            Debug.Log("[Input] No action selected");
            return;
        }

        if (PowerDiceTypes.Count <= 0 || AccuracyDiceTypes.Count <= 0)
        {
            Debug.Log("[Input] Both Power and Accuracy need at least one dice");
            return;
        }

        IsWaitingTurnResolution = true;
        Combat.ReceivePlayerInput(SelectedAction.Value, new List<DiceStatType>(PowerDiceTypes), new List<DiceStatType>(AccuracyDiceTypes));
        SelectedAction = null;
        diceAllocationView.HideAllocationPanel();
        UpdateConfirmAvailability();
    }

    public void OnSkipTurn()
    {
        if (IsWaitingTurnResolution) return;
        if (AllowedAction != ActionType.Attack) return;

        SelectedAction = null;
        PowerDiceTypes.Clear();
        AccuracyDiceTypes.Clear();
        diceAllocationView.HideAllocationPanel();
        RefreshSelectionPreview();

        IsWaitingTurnResolution = true;
        Combat.ReceivePlayerSkipTurn();
        RefreshDiceButtons();
        UpdateConfirmAvailability();
    }

    public void OnSelectInfoPanel()
    {
        Combat.View.SetInfoPanelVisible();
    }

    private void UpdateConfirmAvailability()
    {
        bool hasValidDiceAllocation = PowerDiceTypes.Count > 0 && AccuracyDiceTypes.Count > 0;
        bool isAvailable = !IsWaitingTurnResolution && SelectedAction != null && hasValidDiceAllocation;
        diceAllocationView.SetConfirmInteractable(isAvailable);
    }

    private void RefreshDiceButtons()
    {
        if (Combat.View.DiceAllocationView == null)
            return;

        bool canAllocate = !IsWaitingTurnResolution;

        foreach (DiceStatType stat in Enum.GetValues(typeof(DiceStatType)))
        {
            bool canAddPower = canAllocate && CanUseDiceType(stat) && CanAddDiceToRoll(DiceRollType.Power);
            bool canAddAccuracy = canAllocate && CanUseDiceType(stat) && CanAddDiceToRoll(DiceRollType.Accuracy);

            Combat.View.DiceAllocationView.SetAddDiceButtonInteractable(
                stat,
                DiceRollType.Power,
                canAddPower
            );

            Combat.View.DiceAllocationView.SetAddDiceButtonInteractable(
                stat,
                DiceRollType.Accuracy,
                canAddAccuracy
            );
            
            Combat.View.DiceAllocationView.SetRemoveDiceButtonInteractable(
                stat,
                DiceRollType.Power,
                PowerDiceTypes.Contains(stat)
            );

            Combat.View.DiceAllocationView.SetRemoveDiceButtonInteractable(
                stat,
                DiceRollType.Accuracy,
                AccuracyDiceTypes.Contains(stat)
            );

            Combat.View.DiceAllocationView.SetAllocatorCount(stat, DiceRollType.Power, PowerDiceTypes.FindAll(x => x == stat).Count);
            Combat.View.DiceAllocationView.SetAllocatorCount(stat, DiceRollType.Accuracy, AccuracyDiceTypes.FindAll(x => x == stat).Count);
        }
    }

    private bool CanUseDiceType(DiceStatType diceType)
    {
        return GetDiceMaxValueForType(diceType) > 0;
    }

    /// <summary>
    /// O primeiro dado de cada teste (Power e Accuracy) é grátis e não consome o pool compartilhado.
    /// Extras além disso consomem <see cref="Battler.CurrentActionDices"/> — mesma regra de <see cref="DiceService.RollMany"/>.
    /// </summary>
    private bool CanAddDiceToRoll(DiceRollType diceRollType)
    {
        bool isFreeFirstDice = diceRollType == DiceRollType.Power
            ? PowerDiceTypes.Count == 0
            : AccuracyDiceTypes.Count == 0;

        return isFreeFirstDice || GetRemainingDiceCount() > 0;
    }

    private int GetRemainingDiceCount()
    {
        int extraPower = Mathf.Max(0, PowerDiceTypes.Count - 1);
        int extraAccuracy = Mathf.Max(0, AccuracyDiceTypes.Count - 1);
        int totalExtraAllocated = extraPower + extraAccuracy;
        return Mathf.Max(0, Combat.Player.CurrentActionDices - totalExtraAllocated);
    }

    private DiceStatType GetFirstAvailableDiceType()
    {
        if (CanUseDiceType(DiceStatType.Body)) return DiceStatType.Body;
        if (CanUseDiceType(DiceStatType.Heart)) return DiceStatType.Heart;
        if (CanUseDiceType(DiceStatType.Mind)) return DiceStatType.Mind;

        return DiceStatType.Body;
    }

    private void RefreshSelectionPreview()
    {
        if (Combat == null || Combat.View == null || Combat.View.DiceAllocationView == null)
            return;

        Combat.View.DiceAllocationView.UpdateDiceAllocationStats(Combat.Player.Mind, Combat.Player.Heart, Combat.Player.Body);

        (List<DiceStatType> powerTypes, List<int> powerFaces, _) = Combat.GetDiceService().ConvertToFacesWithTypes(Combat.Player, PowerDiceTypes);
        (List<DiceStatType> accuracyTypes, List<int> accuracyFaces, _) = Combat.GetDiceService().ConvertToFacesWithTypes(Combat.Player, AccuracyDiceTypes);
        
        (int powerMaxValue, DiceStatType powerPrimaryStat) = GetPreviewMaxValueAndPrimaryStat(powerTypes, powerFaces);
        (int accuracyMaxValue, DiceStatType accuracyPrimaryStat) = GetPreviewMaxValueAndPrimaryStat(accuracyTypes, accuracyFaces);
        
        (int lowMax, int mediumMax, int highMin, int maxValue) powerBoundaries = GetPlayerTierBoundaries(powerMaxValue, powerPrimaryStat, DiceRollType.Power, PowerDiceTypes.Count);
        (int lowMax, int mediumMax, int highMin, int maxValue) accuracyBoundaries = GetPlayerTierBoundaries(accuracyMaxValue, accuracyPrimaryStat, DiceRollType.Accuracy, AccuracyDiceTypes.Count);
        
        int baseActionPower = Combat.GetEffectivePlayerActionPower();

        Dictionary<DiceStatType, int> statTargets = new()
        {
            { DiceStatType.Mind, Combat.Player.GetBaseStatValue(DiceStatType.Mind) },
            { DiceStatType.Heart, Combat.Player.GetBaseStatValue(DiceStatType.Heart) },
            { DiceStatType.Body, Combat.Player.GetBaseStatValue(DiceStatType.Body) },
        };
        
        DiceAllocationContext previewData = DiceAllocationCalculator.CalculatePreview(
            baseActionPower: baseActionPower,
            powerDiceTypes: powerTypes,
            powerFaces: powerFaces,
            accuracyDiceTypes: accuracyTypes,
            accuracyFaces: accuracyFaces,
            powerTierBoundaries: powerBoundaries,
            accuracyTierBoundaries: accuracyBoundaries,
            statBaseTargets: statTargets,
            powerPrimaryStat: powerPrimaryStat,
            allocatedPowerDiceCount: PowerDiceTypes.Count
        );
        
        Combat.View.DiceAllocationView.UpdateSelectionPreview(previewData);
    }

    private static (int maxValue, DiceStatType primaryStat) GetPreviewMaxValueAndPrimaryStat(IReadOnlyList<DiceStatType> diceTypes, IReadOnlyList<int> faces)
    {
        if (diceTypes == null || faces == null || diceTypes.Count == 0 || faces.Count == 0)
            return (1, DiceStatType.Body);

        Dictionary<DiceStatType, int> maxValueByStat = new();
        int itemCount = Mathf.Min(diceTypes.Count, faces.Count);

        for (int i = 0; i < itemCount; i++)
        {
            DiceStatType statType = diceTypes[i];
            int faceValue = Mathf.Max(1, faces[i]);
            maxValueByStat[statType] = maxValueByStat.TryGetValue(statType, out int currentValue)
                ? currentValue + faceValue
                : faceValue;
        }

        int selectedMaxValue = 1;
        DiceStatType selectedStat = DiceStatType.Body;

        foreach (KeyValuePair<DiceStatType, int> pair in maxValueByStat)
        {
            if (pair.Value > selectedMaxValue || (pair.Value == selectedMaxValue && GetStatPriority(pair.Key) > GetStatPriority(selectedStat)))
            {
                selectedMaxValue = pair.Value;
                selectedStat = pair.Key;
            }
        }

        return (Mathf.Max(1, selectedMaxValue), selectedStat);
    }

    private static int GetStatPriority(DiceStatType statType)
    {
        return statType switch
        {
            DiceStatType.Mind => 3,
            DiceStatType.Heart => 2,
            DiceStatType.Body => 1,
            _ => 0
        };
    }

    public int GetDiceMaxValueForType(DiceStatType diceType)
    {
        return Combat.GetDiceService().GetDiceMaxValueForType(Combat.Player, diceType);
    }

    public List<int> GetDiceFacesForSelection(IReadOnlyList<DiceStatType> diceTypes, bool isAggregated = false)
    {
        return isAggregated ? Combat.GetDiceService().ConvertToAggregatedFaces(Combat.Player, diceTypes) : Combat.GetDiceService().ConvertToFaces(Combat.Player, diceTypes);
    }

    public (int lowMax, int mediumMax, int highMin, int maxValue) GetPlayerTierBoundaries(int maxValue, DiceStatType statType, DiceRollType rollType, int allocatedDiceCount = 1)
    {
        return Combat.GetPlayerTierBoundaries(maxValue, statType, rollType, allocatedDiceCount);
    }
}