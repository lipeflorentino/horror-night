using System;
using System.Collections.Generic;
using UnityEngine;

public class CombatInputHandler : MonoBehaviour
{
    private const int DicePerTurn = 3;
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
        Debug.Log("[Input] Selected ATTACK");
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
        Debug.Log("[Input] Selected DEFENSE");
    }

    public void OnAddDice(DiceStatType diceStatType, DiceRollType diceRollType)
    {
        if (IsWaitingTurnResolution) return;
        if (GetRemainingDiceCount() <= 0) return;
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

    public void OnToggleInfoPanel(bool isVisible)
    {
        Combat.View.SetInfoPanelVisible(isVisible);
    }

    private void NotifyConfirmAvailability()
    {
        bool hasValidDiceAllocation = PowerDiceTypes.Count + AccuracyDiceTypes.Count > 0;
        bool isAvailable = !IsWaitingTurnResolution && SelectedAction != null && hasValidDiceAllocation;
        ConfirmAvailabilityChanged?.Invoke(isAvailable);
    }

    private void RefreshDiceButtons()
    {
        if (Combat == null || Combat.View == null || Combat.View.ActionPanel == null)
            return;

        bool canAllocateDice = !IsWaitingTurnResolution && GetRemainingDiceCount() > 0;
        Combat.View.ActionPanel.SetAddDiceButtonsInteractable(
            DiceStatType.Mind, canAllocateDice && CanUseDiceType(DiceStatType.Mind));
        Combat.View.ActionPanel.SetAddDiceButtonsInteractable(
            DiceStatType.Heart, canAllocateDice && CanUseDiceType(DiceStatType.Heart));
        Combat.View.ActionPanel.SetAddDiceButtonsInteractable(
            DiceStatType.Body, canAllocateDice && CanUseDiceType(DiceStatType.Body));

        Combat.View.ActionPanel.SetRemoveDiceButtonsInteractable(DiceStatType.Mind, PowerDiceTypes.Contains(DiceStatType.Mind), AccuracyDiceTypes.Contains(DiceStatType.Mind));
        Combat.View.ActionPanel.SetRemoveDiceButtonsInteractable(DiceStatType.Heart, PowerDiceTypes.Contains(DiceStatType.Heart), AccuracyDiceTypes.Contains(DiceStatType.Heart));
        Combat.View.ActionPanel.SetRemoveDiceButtonsInteractable(DiceStatType.Body, PowerDiceTypes.Contains(DiceStatType.Body), AccuracyDiceTypes.Contains(DiceStatType.Body));
    }

    private bool CanUseDiceType(DiceStatType diceType)
    {
        return Combat.GetDiceMaxValueForType(Combat.Player, diceType) > 0;
    }

    private int GetRemainingDiceCount()
    {
        return Mathf.Max(0, DicePerTurn - (PowerDiceTypes.Count + AccuracyDiceTypes.Count));
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

        List<int> powerFaces = Combat.GetDiceFacesForSelection(PowerDiceTypes);
        List<int> accuracyFaces = Combat.GetDiceFacesForSelection(AccuracyDiceTypes);
        (int lowMax, int mediumMax, int highMin) boundaries = Combat.GetPlayerTierBoundaries(6);
        Combat.View.ActionPanel .UpdateSelectionPreview(PowerDiceTypes, powerFaces, AccuracyDiceTypes, accuracyFaces, boundaries);
    }

}
