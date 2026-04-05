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

    [Header("Flee")]
    [SerializeField] private int fleeDiceAmount = 1;

    public event Action OnRecharge;
    public event Action OnRechargeBoosted;
    public event Action OnInvestigate;
    public event Action OnAttack;
    public event Action OnEndTurn;
    public event Action<int> OnFlee;

    private void Awake()
    {
        if (rechargeButton != null)
            rechargeButton.onClick.AddListener(RaiseRecharge);

        if (rechargeBoostedButton != null)
            rechargeBoostedButton.onClick.AddListener(RaiseRechargeBoosted);

        if (investigateButton != null)
            investigateButton.onClick.AddListener(RaiseInvestigate);

        if (fleeButton != null)
            fleeButton.onClick.AddListener(RaiseFlee);

        if (attackButton != null)
            attackButton.onClick.AddListener(RaiseAttack);

        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(RaiseEndTurn);
    }

    private void OnDestroy()
    {
        if (rechargeButton != null)
            rechargeButton.onClick.RemoveListener(RaiseRecharge);

        if (rechargeBoostedButton != null)
            rechargeBoostedButton.onClick.RemoveListener(RaiseRechargeBoosted);

        if (investigateButton != null)
            investigateButton.onClick.RemoveListener(RaiseInvestigate);

        if (fleeButton != null)
            fleeButton.onClick.RemoveListener(RaiseFlee);

        if (attackButton != null)
            attackButton.onClick.RemoveListener(RaiseAttack);

        if (endTurnButton != null)
            endTurnButton.onClick.RemoveListener(RaiseEndTurn);
    }

    private void RaiseRecharge() => OnRecharge?.Invoke();

    private void RaiseRechargeBoosted() => OnRechargeBoosted?.Invoke();

    private void RaiseInvestigate() => OnInvestigate?.Invoke();

    private void RaiseFlee() => OnFlee?.Invoke(fleeDiceAmount);

    private void RaiseAttack() => OnAttack?.Invoke();

    private void RaiseEndTurn() => OnEndTurn?.Invoke();
}
