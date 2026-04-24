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
    public Toggle InfoToggle;
    public event Action AttackClicked;
    public event Action DefendClicked;
    public event Action AddDiceToAttackClicked;
    public event Action RemoveDiceFromAttackClicked;
    public event Action AddDiceToDefenseClicked;
    public event Action RemoveDiceFromDefenseClicked;
    public event Action EndTurnClicked;
    public event Action<bool> InfoToggled;
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

        if (InfoToggle != null)
            InfoToggle.onValueChanged.AddListener(HandleInfoToggleChanged);

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

        if (InfoToggle != null)
            InfoToggle.onValueChanged.RemoveListener(HandleInfoToggleChanged);

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
        InfoToggled += inputHandler.OnToggleInfoPanel;
        inputHandler.EndTurnAvailabilityChanged += SetEndTurnInteractable;
        InfoToggle.SetIsOnWithoutNotify(false);
        SetEndTurnInteractable(false);
    }

    public void SetEndTurnInteractable(bool isInteractable)
    {
        if (EndTurnButton != null)
        {
            EndTurnButton.interactable = isInteractable;
        }
    }

    public void SetAllInteractable(bool isInteractable)
    {
        if (AttackButton != null)
            AttackButton.interactable = isInteractable;

        if (DefendButton != null)
            DefendButton.interactable = isInteractable;

        if (AddDiceAttackButton != null)
            AddDiceAttackButton.interactable = isInteractable;

        if (RemoveDiceAttackButton != null)
            RemoveDiceAttackButton.interactable = isInteractable;

        if (AddDiceDefenseButton != null)
            AddDiceDefenseButton.interactable = isInteractable;

        if (RemoveDiceDefenseButton != null)
            RemoveDiceDefenseButton.interactable = isInteractable;

        if (EndTurnButton != null)
            EndTurnButton.interactable = isInteractable;

        if (InfoToggle != null)
            InfoToggle.interactable = isInteractable;
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

    private void HandleInfoToggleChanged(bool isEnabled)
    {
        InfoToggled?.Invoke(isEnabled);
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
