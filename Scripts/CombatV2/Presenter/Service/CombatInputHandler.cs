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
        return Combat.GetDiceMaxValueForType(Combat.Player, diceType) > 0;
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

        List<int> powerFaces = Combat.GetDiceFacesForSelection(PowerDiceTypes);
        List<int> accuracyFaces = Combat.GetDiceFacesForSelection(AccuracyDiceTypes);
        Logger.Log($"[Input] Power Dice Types: {string.Join(", ", PowerDiceTypes)}, Faces: {string.Join(", ", powerFaces)}");
        Logger.Log($"[Input] Accuracy Dice Types: {string.Join(", ", AccuracyDiceTypes)}, Faces: {string.Join(", ", accuracyFaces)}");
        DiceStatType powerPrimaryStat = GetPrimaryStat(PowerDiceTypes);
        DiceStatType accuracyPrimaryStat = GetPrimaryStat(AccuracyDiceTypes);
        int powerMaxValue = SumFaces(powerFaces);
        int accuracyMaxValue = SumFaces(accuracyFaces);
        Logger.Log($"[Input] Power Max Value: {powerMaxValue}, Primary Stat: {powerPrimaryStat}");
        Logger.Log($"[Input] Accuracy Max Value: {accuracyMaxValue}, Primary Stat: {accuracyPrimaryStat}");

        (int lowMax, int mediumMax, int highMin) powerBoundaries = Combat.GetPlayerTierBoundaries(powerMaxValue, powerPrimaryStat, DiceRollType.Power);
        (int lowMax, int mediumMax, int highMin) accuracyBoundaries = Combat.GetPlayerTierBoundaries(accuracyMaxValue, accuracyPrimaryStat, DiceRollType.Accuracy);

        Combat.View.ActionPanel.UpdateSelectionPreview(
            PowerDiceTypes,
            powerFaces,
            AccuracyDiceTypes,
            accuracyFaces,
            powerBoundaries,
            accuracyBoundaries);
    }

    private static int SumFaces(IReadOnlyList<int> faces)
    {
        if (faces == null || faces.Count == 0)
            return 1;

        int total = 0;
        for (int i = 0; i < faces.Count; i++)
            total += Mathf.Max(1, faces[i]);

        return Mathf.Max(1, total);
    }

    private static DiceStatType GetPrimaryStat(IReadOnlyList<DiceStatType> diceTypes)
    {
        if (diceTypes == null || diceTypes.Count == 0)
            return DiceStatType.Body;

        int body = 0;
        int heart = 0;
        int mind = 0;

        for (int i = 0; i < diceTypes.Count; i++)
        {
            switch (diceTypes[i])
            {
                case DiceStatType.Body: body++; break;
                case DiceStatType.Heart: heart++; break;
                case DiceStatType.Mind: mind++; break;
            }
        }

        if (mind >= heart && mind >= body) return DiceStatType.Mind;
        if (heart >= body) return DiceStatType.Heart;
        return DiceStatType.Body;
    }

}
