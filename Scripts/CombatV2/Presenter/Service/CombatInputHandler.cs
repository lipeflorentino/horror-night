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
        // Combat.View.ActionPanel.SetSelectedDiceTypeLabel(SelectedDiceType.ToString());
        Combat.View.ActionPanel.HideConfirmPanel();
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

    public void OnAddPowerDice()
    {
        
        if (IsWaitingTurnResolution) return;
        if (GetRemainingDiceCount() <= 0) return;
        if (!CanUseDiceType(SelectedPowerDiceType)) return;

        PowerDiceTypes.Add(SelectedPowerDiceType);
        UpdateCombatView();
        NotifyConfirmAvailability();
    }

    public void OnAddAccuracyDice()
    {
        if (IsWaitingTurnResolution) return;
        if (GetRemainingDiceCount() <= 0) return;
        if (!CanUseDiceType(SelectedAccuracyDiceType)) return;

        AccuracyDiceTypes.Add(SelectedAccuracyDiceType);
        UpdateCombatView();
        NotifyConfirmAvailability();
    }

    public void OnRemovePowerDice()
    {
        if (IsWaitingTurnResolution) return;
        if (PowerDiceTypes.Count <= 0) return;

        PowerDiceTypes.Remove(SelectedPowerDiceType);
        UpdateCombatView();
        NotifyConfirmAvailability();
    }

    public void OnRemoveAccuracyDice()
    {
        if (IsWaitingTurnResolution) return;
        if (AccuracyDiceTypes.Count <= 0) return;

        AccuracyDiceTypes.Remove(SelectedAccuracyDiceType);
        UpdateCombatView();
        NotifyConfirmAvailability();

        Debug.Log($"[Input] Removed dice from ACCURACY: {AccuracyDiceTypes.Count}");
    }

    public void OnSelectMindPowerDiceType()
    {
        SelectDiceType(DiceStatType.Mind, "power");
    }

    public void OnSelectHeartPowerDiceType()
    {
        SelectDiceType(DiceStatType.Heart, "power");
    }

    public void OnSelectBodyPowerDiceType()
    {
        SelectDiceType(DiceStatType.Body, "power");
    }

    public void OnSelectMindAccuracyDiceType()
    {
        SelectDiceType(DiceStatType.Mind, "accuracy");
    }

    public void OnSelectHeartAccuracyDiceType()
    {
        SelectDiceType(DiceStatType.Heart, "accuracy");
    }

    public void OnSelectBodyAccuracyDiceType()
    {
        SelectDiceType(DiceStatType.Body, "accuracy");
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

    public void OnEndTurn()
    {
        OnSkipTurn();
    }

    public void OnSkipTurn()
    {
        if (IsWaitingTurnResolution) return;

        SelectedAction = null;
        PowerDiceTypes.Clear();
        AccuracyDiceTypes.Clear();
        Combat.View.ActionPanel.HideConfirmPanel();

        IsWaitingTurnResolution = true;
        Combat.ReceivePlayerSkipTurn();
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

    private void SelectDiceType(DiceStatType diceType, string type)
    {
        if (!CanUseDiceType(diceType))
        {
            Debug.Log($"[Input] {diceType} die cannot be selected because its stat is 0.");
            return;
        }

        if (type == "power")
        {
            SelectedPowerDiceType = diceType;
        }  
        else
        {
            SelectedAccuracyDiceType = diceType;
        }
        
        Combat.View.ActionPanel.SetSelectedDiceTypeLabel(diceType.ToString());
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
}
