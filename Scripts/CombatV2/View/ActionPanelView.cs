using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionPanelView : MonoBehaviour
{
    public Button AttackButton;
    public Button DefendButton;
    public Button AddDiceAttackButton;
    public TMP_Text AddDiceAttackCountText;
    public Button AddDiceDefenseButton;
    public TMP_Text AddDiceDefenseCountText;
    public Button EndTurnButton;
    public event Action AttackClicked;
    public event Action DefendClicked;
    public event Action AddDiceToAttackClicked;
    public event Action AddDiceToDefenseClicked;
    public event Action EndTurnClicked;
    private CombatInputHandler BoundInputHandler;

    private void Awake()
    {
        if (AttackButton != null)
            AttackButton.onClick.AddListener(HandleAttackClick);

        if (DefendButton != null)
            DefendButton.onClick.AddListener(HandleDefendClick);

        if (AddDiceAttackButton != null)
            AddDiceAttackButton.onClick.AddListener(HandleAddDiceToAttackClick);

        if (AddDiceDefenseButton != null)
            AddDiceDefenseButton.onClick.AddListener(HandleAddDiceToDefenseClick);

        if (EndTurnButton != null)
            EndTurnButton.onClick.AddListener(HandleEndTurnClick);

        if (BoundInputHandler != null)
            BoundInputHandler.EndTurnAvailabilityChanged -= SetEndTurnInteractable;
    }

    private void OnDestroy()
    {
        if (AttackButton != null)
            AttackButton.onClick.RemoveListener(HandleAttackClick);

        if (DefendButton != null)
            DefendButton.onClick.RemoveListener(HandleDefendClick);

        if (AddDiceAttackButton != null)
            AddDiceAttackButton.onClick.RemoveListener(HandleAddDiceToAttackClick);

        if (AddDiceDefenseButton != null)
            AddDiceDefenseButton.onClick.RemoveListener(HandleAddDiceToDefenseClick);

        if (EndTurnButton != null)
            EndTurnButton.onClick.RemoveListener(HandleEndTurnClick);

        if (BoundInputHandler != null)
            BoundInputHandler.EndTurnAvailabilityChanged -= SetEndTurnInteractable;
    }

    public void BindInput(CombatInputHandler inputHandler)
    {
        if (BoundInputHandler != null)
            BoundInputHandler.EndTurnAvailabilityChanged -= SetEndTurnInteractable;

        BoundInputHandler = inputHandler;
        AttackClicked += inputHandler.OnAttack;
        DefendClicked += inputHandler.OnDefend;
        AddDiceToAttackClicked += inputHandler.OnAddDiceToAttack;
        AddDiceToDefenseClicked += inputHandler.OnAddDiceToDefense;
        EndTurnClicked += inputHandler.OnEndTurn;
        inputHandler.EndTurnAvailabilityChanged += SetEndTurnInteractable;
        SetEndTurnInteractable(false);
    }

    public void SetEndTurnInteractable(bool isInteractable)
    {
        if (EndTurnButton != null)
        {
            EndTurnButton.interactable = isInteractable;
        }
    }

    private void HandleAttackClick()
    {
        AttackClicked?.Invoke();
    }

    private void HandleDefendClick()
    {
        DefendClicked?.Invoke();
    }

    private void HandleAddDiceToAttackClick()
    {
        AddDiceToAttackClicked?.Invoke();
    }

    private void HandleAddDiceToDefenseClick()
    {
        AddDiceToDefenseClicked?.Invoke();
    }

    private void HandleEndTurnClick()
    {
        EndTurnClicked?.Invoke();
    }

    public void SetPlayerRoleButtons(bool isPlayerAttacker)
    {
        Debug.Log("Setting button states. Is Player Attacker: " + isPlayerAttacker);
        AttackButton.gameObject.SetActive(isPlayerAttacker);
        AddDiceAttackButton.gameObject.SetActive(isPlayerAttacker);

        DefendButton.gameObject.SetActive(!isPlayerAttacker);
        AddDiceDefenseButton.gameObject.SetActive(!isPlayerAttacker);
    }

    public void UpdateAddDiceAttackCount(int count)
    {
        AddDiceAttackCountText.text = count.ToString();
    }

    public void UpdateAddDiceDefenseCount(int count)
    {
        AddDiceDefenseCountText.text = count.ToString();
    }
}
