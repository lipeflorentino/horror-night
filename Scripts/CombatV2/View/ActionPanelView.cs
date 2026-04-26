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
    public Button RemoveDiceDefenseButton;
    public GameObject AttackDiceCountContainer, DefendDiceCountContainer;
    public Button EndTurnButton;
    public Toggle InfoToggle;

    [Header("Confirm Action Panel")]
    public GameObject ConfirmPanel;
    public TMP_Text ConfirmActionText;
    public Button ConfirmButton;

    [Header("Stat Dice Selector")]
    public Button SelectMindDiceButton;
    public Button SelectHeartDiceButton;
    public Button SelectBodyDiceButton;
    public TMP_Text SelectedDiceTypeText;

    public event Action AttackClicked;
    public event Action DefendClicked;
    public event Action AddDiceToAttackClicked;
    public event Action RemoveDiceFromAttackClicked;
    public event Action AddDiceToDefenseClicked;
    public event Action RemoveDiceFromDefenseClicked;
    public event Action EndTurnClicked;
    public event Action ConfirmClicked;
    public event Action MindDiceTypeSelected;
    public event Action HeartDiceTypeSelected;
    public event Action BodyDiceTypeSelected;
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

        if (ConfirmButton != null)
            ConfirmButton.onClick.AddListener(HandleConfirmClick);

        if (SelectMindDiceButton != null)
            SelectMindDiceButton.onClick.AddListener(HandleMindDiceTypeClick);

        if (SelectHeartDiceButton != null)
            SelectHeartDiceButton.onClick.AddListener(HandleHeartDiceTypeClick);

        if (SelectBodyDiceButton != null)
            SelectBodyDiceButton.onClick.AddListener(HandleBodyDiceTypeClick);

        if (InfoToggle != null)
            InfoToggle.onValueChanged.AddListener(HandleInfoToggleChanged);

        if (BoundInputHandler != null)
            BoundInputHandler.ConfirmAvailabilityChanged -= SetConfirmInteractable;

        HideConfirmPanel();
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

        if (ConfirmButton != null)
            ConfirmButton.onClick.RemoveListener(HandleConfirmClick);

        if (SelectMindDiceButton != null)
            SelectMindDiceButton.onClick.RemoveListener(HandleMindDiceTypeClick);

        if (SelectHeartDiceButton != null)
            SelectHeartDiceButton.onClick.RemoveListener(HandleHeartDiceTypeClick);

        if (SelectBodyDiceButton != null)
            SelectBodyDiceButton.onClick.RemoveListener(HandleBodyDiceTypeClick);

        if (InfoToggle != null)
            InfoToggle.onValueChanged.RemoveListener(HandleInfoToggleChanged);

        if (BoundInputHandler != null)
            BoundInputHandler.ConfirmAvailabilityChanged -= SetConfirmInteractable;
    }

    public void BindInput(CombatInputHandler inputHandler)
    {
        if (BoundInputHandler != null)
            BoundInputHandler.ConfirmAvailabilityChanged -= SetConfirmInteractable;

        BoundInputHandler = inputHandler;
        AttackClicked += inputHandler.OnAttack;
        DefendClicked += inputHandler.OnDefend;
        AddDiceToAttackClicked += inputHandler.OnAddDiceToAttack;
        RemoveDiceFromAttackClicked += inputHandler.OnRemoveDiceFromAttack;
        AddDiceToDefenseClicked += inputHandler.OnAddDiceToDefense;
        RemoveDiceFromDefenseClicked += inputHandler.OnRemoveDiceFromDefense;
        EndTurnClicked += inputHandler.OnSkipTurn;
        ConfirmClicked += inputHandler.OnConfirmAction;
        MindDiceTypeSelected += inputHandler.OnSelectMindDiceType;
        HeartDiceTypeSelected += inputHandler.OnSelectHeartDiceType;
        BodyDiceTypeSelected += inputHandler.OnSelectBodyDiceType;
        InfoToggled += inputHandler.OnToggleInfoPanel;
        inputHandler.ConfirmAvailabilityChanged += SetConfirmInteractable;

        if (InfoToggle != null)
            InfoToggle.SetIsOnWithoutNotify(false);

        SetConfirmInteractable(false);
        SetSelectedDiceTypeLabel("Body");
        HideConfirmPanel();
    }

    public void SetConfirmInteractable(bool isInteractable)
    {
        if (ConfirmButton != null)
            ConfirmButton.interactable = isInteractable;
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

        if (ConfirmButton != null)
            ConfirmButton.interactable = isInteractable;

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

    private void HandleConfirmClick()
    {
        ConfirmClicked?.Invoke();
    }

    private void HandleMindDiceTypeClick()
    {
        MindDiceTypeSelected?.Invoke();
    }

    private void HandleHeartDiceTypeClick()
    {
        HeartDiceTypeSelected?.Invoke();
    }

    private void HandleBodyDiceTypeClick()
    {
        BodyDiceTypeSelected?.Invoke();
    }

    private void HandleInfoToggleChanged(bool isEnabled)
    {
        InfoToggled?.Invoke(isEnabled);
    }

    public void SetPlayerRoleButtons(bool isPlayerAttacker)
    {
        if (AttackButton != null)
            AttackButton.gameObject.SetActive(isPlayerAttacker);

        if (DefendButton != null)
            DefendButton.gameObject.SetActive(!isPlayerAttacker);
    }

    public void ShowConfirmPanel(string actionLabel)
    {
        if (ConfirmPanel != null)
            ConfirmPanel.SetActive(true);

        if (ConfirmActionText != null)
            ConfirmActionText.text = actionLabel;
    }

    public void HideConfirmPanel()
    {
        if (ConfirmPanel != null)
            ConfirmPanel.SetActive(false);
    }

    public void SetSelectedDiceTypeLabel(string diceType)
    {
        if (SelectedDiceTypeText != null)
            SelectedDiceTypeText.text = diceType;
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
