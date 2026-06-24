using System;
using System.Collections.Generic;
using UnityEngine;

public class CombatInputHandler : MonoBehaviour
{
    private CombatManager Combat;
    private readonly List<DiceStatType> PowerDiceTypes = new();
    private readonly List<DiceStatType> AccuracyDiceTypes = new();
    private ActionType? SelectedAction = null;
    private ActionType AllowedAction = ActionType.Attack;
    private bool IsWaitingTurnResolution = false;
    private DiceStatType SelectedPowerDiceType = DiceStatType.Body;
    private DiceStatType SelectedAccuracyDiceType = DiceStatType.Mind;

    public event Action<bool> ConfirmAvailabilityChanged;

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
        NotifyConfirmAvailability();
    }

    public void SetAllowedAction(ActionType allowedAction)
    {
        AllowedAction = allowedAction;
        SelectedAction = null;
        IsWaitingTurnResolution = false;
        PowerDiceTypes.Clear();
        AccuracyDiceTypes.Clear();
        SelectedPowerDiceType = SelectedAccuracyDiceType = GetFirstAvailableDiceType();
        Combat.View.ActionPanel.HideConfirmPanel();
        RefreshSelectionPreview();
        RefreshDiceButtons();
        UpdateCombatView();
        NotifyConfirmAvailability();
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
        Combat.View.ActionPanel.ShowConfirmPanel("Attack");
        NotifyConfirmAvailability();
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
        Combat.View.ActionPanel.ShowConfirmPanel("Defense");
        NotifyConfirmAvailability();
    }

    public void OnAddDice(DiceStatType diceStatType, DiceRollType diceRollType)
    {
        if (IsWaitingTurnResolution) return;
        if (GetRemainingDiceCount(diceRollType) <= 0) return;
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
        NotifyConfirmAvailability();
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
        NotifyConfirmAvailability();
    }

    public void OnConfirmAction()
    {
        if (IsWaitingTurnResolution) return;
        if (SelectedAction == null)
        {
            Debug.Log("[Input] No action selected");
            return;
        }

        if (PowerDiceTypes.Count + AccuracyDiceTypes.Count <= 0)
        {
            Debug.Log("[Input] No dice allocated");
            return;
        }

        IsWaitingTurnResolution = true;
        Combat.ReceivePlayerInput(SelectedAction.Value, new List<DiceStatType>(PowerDiceTypes), new List<DiceStatType>(AccuracyDiceTypes));
        SelectedAction = null;
        Combat.View.ActionPanel.HideConfirmPanel();
        NotifyConfirmAvailability();
    }

    public void OnSkipTurn()
    {
        if (IsWaitingTurnResolution) return;
        if (AllowedAction != ActionType.Attack) return;

        SelectedAction = null;
        PowerDiceTypes.Clear();
        AccuracyDiceTypes.Clear();
        Combat.View.ActionPanel.HideConfirmPanel();
        RefreshSelectionPreview();

        IsWaitingTurnResolution = true;
        Combat.ReceivePlayerSkipTurn();
        RefreshDiceButtons();
        NotifyConfirmAvailability();
    }

    public void OnToggleInfoPanel()
    {
        Combat.View.SetInfoPanelVisible();
    }

    private void NotifyConfirmAvailability()
    {
        bool hasValidDiceAllocation = PowerDiceTypes.Count + AccuracyDiceTypes.Count > 0;
        bool isAvailable = !IsWaitingTurnResolution && SelectedAction != null && hasValidDiceAllocation;
        ConfirmAvailabilityChanged?.Invoke(isAvailable);
    }

    private void RefreshDiceButtons()
    {
        if (Combat.View.ActionPanel == null)
            return;

        bool canAllocate = !IsWaitingTurnResolution;

        int remainingPower = GetRemainingDiceCount(DiceRollType.Power);
        int remainingAccuracy = GetRemainingDiceCount(DiceRollType.Accuracy);

        foreach (DiceStatType stat in Enum.GetValues(typeof(DiceStatType)))
        {
            Combat.View.ActionPanel.SetAddDiceButtonInteractable(
                stat,
                DiceRollType.Power,
                canAllocate && remainingPower > 0 && CanUseDiceType(stat)
            );

            Combat.View.ActionPanel.SetAddDiceButtonInteractable(
                stat,
                DiceRollType.Accuracy,
                canAllocate && remainingAccuracy > 0 && CanUseDiceType(stat)
            );
            
            Combat.View.ActionPanel.SetRemoveDiceButtonInteractable(
                stat,
                DiceRollType.Power,
                PowerDiceTypes.Contains(stat)
            );

            Combat.View.ActionPanel.SetRemoveDiceButtonInteractable(
                stat,
                DiceRollType.Accuracy,
                AccuracyDiceTypes.Contains(stat)
            );
        }
    }

    private bool CanUseDiceType(DiceStatType diceType)
    {
        return GetDiceMaxValueForType(diceType) > 0;
    }

    private int GetRemainingDiceCount(DiceRollType diceRollType)
    {
        int MaxDicesPerTurn = diceRollType == DiceRollType.Power ? Combat.Player.CurrentPowerDices : Combat.Player.CurrentAccuracyDices;
        int allocatedDices = diceRollType == DiceRollType.Power ? PowerDiceTypes.Count : AccuracyDiceTypes.Count;
        return Mathf.Max(0, MaxDicesPerTurn - allocatedDices);
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
        if (Combat == null || Combat.View == null || Combat.View.ActionPanel  == null)
            return;

        Combat.View.ActionPanel.UpdateDiceAllocationStats(Combat.Player.Mind, Combat.Player.Heart, Combat.Player.Body);

        List<int> powerFaces = GetDiceFacesForSelection(PowerDiceTypes, false);
        List<int> accuracyFaces = GetDiceFacesForSelection(AccuracyDiceTypes, false);

        List<DiceStatType> aggregatedPowerTypes = GetAggregatedDiceTypes(PowerDiceTypes);
        List<int> aggregatedPowerFaces = GetDiceFacesForSelection(aggregatedPowerTypes, true);
        
        (int powerMaxValue, DiceStatType powerPrimaryStat) = GetPreviewMaxValueAndPrimaryStat(PowerDiceTypes, powerFaces);
        (int accuracyMaxValue, DiceStatType accuracyPrimaryStat) = GetPreviewMaxValueAndPrimaryStat(AccuracyDiceTypes, accuracyFaces);
        (int lowMax, int mediumMax, int highMin) powerBoundaries = GetPlayerTierBoundaries(powerMaxValue, powerPrimaryStat, DiceRollType.Power);
        (int lowMax, int mediumMax, int highMin) accuracyBoundaries = GetPlayerTierBoundaries(accuracyMaxValue, accuracyPrimaryStat, DiceRollType.Accuracy);

        Combat.View.ActionPanel.UpdateSelectionPreview(
            Combat.GetEffectivePlayerActionPower(),
            PowerDiceTypes,
            powerFaces,
            aggregatedPowerFaces,
            AccuracyDiceTypes,
            accuracyFaces,
            powerBoundaries,
            accuracyBoundaries);
    }

    private static List<DiceStatType> GetAggregatedDiceTypes(List<DiceStatType> diceTypes)
    {
        List<DiceStatType> aggregated = new();
        Dictionary<DiceStatType, int> typeCount = new();
        
        for (int i = 0; i < diceTypes.Count; i++)
        {
            DiceStatType type = diceTypes[i];
            typeCount[type] = typeCount.TryGetValue(type, out int count) ? count + 1 : 1;
        }
        
        for (int i = 0; i < diceTypes.Count; i++)
        {
            DiceStatType type = diceTypes[i];
            if (!aggregated.Contains(type))
                aggregated.Add(type);
        }
        
        return aggregated;
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

    public (int lowMax, int mediumMax, int highMin) GetPlayerTierBoundaries(int maxValue, DiceStatType statType, DiceRollType rollType)
    {
        CombatRollContext context = Combat.BuildPlayerRollContext(maxValue, statType, rollType);
        return Combat.GetDiceService().GetTierBoundaries(context);
    }
}
