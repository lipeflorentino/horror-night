using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InputView : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button rechargeButton;
    [SerializeField] private Button rechargeBoostedButton;
    [SerializeField] private Button investigateButton;
    [SerializeField] private Button fleeButton;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button useItemButton;
    [SerializeField] private Button skillsButton;
    [SerializeField] private Button infoButton;

    [Header("Flee")]
    [SerializeField] private int fleeDiceAmount = 1;

    public event Action OnRecharge;
    public event Action OnRechargeBoosted;
    public event Action<int> OnAddInvestigateDice;
    public event Action<int> OnAddAttackDice;
    public event Action OnEndTurn;
    public event Action<int> OnFlee;
    public event Action OnUseItem;
    public event Action OnSkills;
    public event Action OnInfo;

    private int attackDiceAllocation;
    private int investigateDiceAllocation;

    private void Awake()
    {
        if (rechargeButton != null)
            rechargeButton.onClick.AddListener(RaiseRecharge);

        if (rechargeBoostedButton != null)
            rechargeBoostedButton.onClick.AddListener(RaiseRechargeBoosted);

        if (investigateButton != null)
            investigateButton.onClick.AddListener(RaiseAddInvestigateDice);

        if (fleeButton != null)
            fleeButton.onClick.AddListener(RaiseFlee);

        if (attackButton != null)
            attackButton.onClick.AddListener(RaiseAddAttackDice);

        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(RaiseEndTurn);

        if (useItemButton != null)
            useItemButton.onClick.AddListener(RaiseUseItem);

        if (skillsButton != null)
            skillsButton.onClick.AddListener(RaiseSkills);

        if (infoButton != null)
            infoButton.onClick.AddListener(RaiseInfo);
    }

    private void OnDestroy()
    {
        if (rechargeButton != null)
            rechargeButton.onClick.RemoveListener(RaiseRecharge);

        if (rechargeBoostedButton != null)
            rechargeBoostedButton.onClick.RemoveListener(RaiseRechargeBoosted);

        if (investigateButton != null)
            investigateButton.onClick.RemoveListener(RaiseAddInvestigateDice);

        if (fleeButton != null)
            fleeButton.onClick.RemoveListener(RaiseFlee);

        if (attackButton != null)
            attackButton.onClick.RemoveListener(RaiseAddAttackDice);

        if (endTurnButton != null)
            endTurnButton.onClick.RemoveListener(RaiseEndTurn);

        if (useItemButton != null)
            useItemButton.onClick.RemoveListener(RaiseUseItem);

        if (skillsButton != null)
            skillsButton.onClick.RemoveListener(RaiseSkills);

        if (infoButton != null)
            infoButton.onClick.RemoveListener(RaiseInfo);
    }

    private void RaiseRecharge() => OnRecharge?.Invoke();

    private void RaiseRechargeBoosted() => OnRechargeBoosted?.Invoke();

    private void RaiseAddInvestigateDice()
    {
        investigateDiceAllocation++;
        OnAddInvestigateDice?.Invoke(investigateDiceAllocation);
    }

    private void RaiseFlee() => OnFlee?.Invoke(fleeDiceAmount);

    private void RaiseAddAttackDice()
    {
        attackDiceAllocation++;
        OnAddAttackDice?.Invoke(attackDiceAllocation);
    }

    private void RaiseEndTurn() => OnEndTurn?.Invoke();

    private void RaiseUseItem() => OnUseItem?.Invoke();

    private void RaiseSkills() => OnSkills?.Invoke();
    
    private void RaiseInfo() => OnInfo?.Invoke();
}
