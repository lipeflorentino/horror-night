using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionPanelView : MonoBehaviour
{
    public Button AttackButton;
    public Button DefendButton;
    public Button AddDiceAttackButton;
    public Button RemoveDiceAttackButton;
    public Button AddDiceDefenseButton;
    public TMP_Text AddDiceAttackCountText;
    public Button RemoveDiceDefenseButton;
    public TMP_Text AddDiceDefenseCountText;
    public GameObject AttackDiceCountContainer, DefendDiceCountContainer;
    public Button EndTurnButton;
    public event Action AttackClicked;
    public event Action DefendClicked;
    public event Action AddDiceToAttackClicked;
    public event Action RemoveDiceFromAttackClicked;
    public event Action AddDiceToDefenseClicked;
    public event Action RemoveDiceFromDefenseClicked;
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

        if (RemoveDiceAttackButton != null)
            RemoveDiceAttackButton.onClick.AddListener(HandleRemoveDiceFromAttackClick);

        if (AddDiceDefenseButton != null)
            AddDiceDefenseButton.onClick.AddListener(HandleAddDiceToDefenseClick);

        if (RemoveDiceDefenseButton != null)
            RemoveDiceDefenseButton.onClick.AddListener(HandleRemoveDiceFromDefenseClick);

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

        if (RemoveDiceAttackButton != null)
            RemoveDiceAttackButton.onClick.RemoveListener(HandleRemoveDiceFromAttackClick);

        if (AddDiceDefenseButton != null)
            AddDiceDefenseButton.onClick.RemoveListener(HandleAddDiceToDefenseClick);

        if (RemoveDiceDefenseButton != null)
            RemoveDiceDefenseButton.onClick.RemoveListener(HandleRemoveDiceFromDefenseClick);

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
        RemoveDiceFromAttackClicked += inputHandler.OnRemoveDiceFromAttack;
        AddDiceToDefenseClicked += inputHandler.OnAddDiceToDefense;
        RemoveDiceFromDefenseClicked += inputHandler.OnRemoveDiceFromDefense;
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

    private void HandleRemoveDiceFromAttackClick()
    {
        RemoveDiceFromAttackClicked?.Invoke();
    }

    private void HandleRemoveDiceFromDefenseClick()
    {
        RemoveDiceFromDefenseClicked?.Invoke();
    }

    private void HandleEndTurnClick()
    {
        EndTurnClicked?.Invoke();
    }

    public void SetPlayerRoleButtons(bool isPlayerAttacker)
    {
        AttackButton.gameObject.SetActive(isPlayerAttacker);
        AddDiceAttackButton.gameObject.SetActive(isPlayerAttacker);
        AttackDiceCountContainer.SetActive(isPlayerAttacker);

        if (RemoveDiceAttackButton != null)
            RemoveDiceAttackButton.gameObject.SetActive(isPlayerAttacker);

        DefendButton.gameObject.SetActive(!isPlayerAttacker);
        AddDiceDefenseButton.gameObject.SetActive(!isPlayerAttacker);
        DefendDiceCountContainer.SetActive(!isPlayerAttacker);

        if (RemoveDiceDefenseButton != null)
            RemoveDiceDefenseButton.gameObject.SetActive(!isPlayerAttacker);
    }

    public void UpdateAddDiceAttackCount(int count)
    {
        AddDiceAttackCountText.text = count.ToString();
    }

    public void UpdateAddDiceDefenseCount(int count)
    {
        AddDiceDefenseCountText.text = count.ToString();
    }

    public void HighlightSelectedAction(ActionInstance action)
    {
        //TODO: Implement visual highlight for the selected action button
        if (action == null)
        {
            Debug.LogWarning("HighlightSelectedAction called with null action.");
            return;
        }
    }
}