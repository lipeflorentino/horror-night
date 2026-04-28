using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionPanelView : MonoBehaviour
{
    public Button SelectAttackButton;
    public Button SelectDefendButton;
    public Button AddPowerDiceButton;
    public Button RemovePowerDiceButton;
    public Button AddAccuracyDiceButton;
    public Button RemoveAccuracyDiceButton;
    public GameObject AttackDiceCountContainer, DefendDiceCountContainer;
    public Button EndTurnButton;
    public Toggle InfoToggle;

    [Header("Confirm Action Panel")]
    public GameObject ConfirmPanel;
    public TMP_Text ConfirmActionText;
    public Button ConfirmButton;

    [Header("Stat Dice Selector")]
    public Button SelectMindPowerDiceButton;
    public Button SelectHeartPowerDiceButton;
    public Button SelectBodyPowerDiceButton;
    public Button SelectMindAccuracyDiceButton;
    public Button SelectHeartAccuracyDiceButton;
    public Button SelectBodyAccuracyDiceButton;
    public TMP_Text SelectedDiceTypeText;

    public event Action SelectAttackClicked;
    public event Action SelectDefendClicked;
    public event Action AddPowerDiceClicked;
    public event Action RemovePowerDiceClicked;
    public event Action AddAccuracyDiceClicked;
    public event Action RemoveAccuracyDiceClicked;
    public event Action EndTurnClicked;
    public event Action ConfirmClicked;
    public event Action MindPowerDiceTypeSelected;
    public event Action HeartPowerDiceTypeSelected;
    public event Action BodyPowerDiceTypeSelected;
    public event Action MindAccuracyDiceTypeSelected;
    public event Action HeartAccuracyDiceTypeSelected;
    public event Action BodyAccuracyDiceTypeSelected;
    public event Action<bool> InfoToggled;
    private CombatInputHandler BoundInputHandler;

    private void Awake()
    {
        if (SelectAttackButton != null)
            SelectAttackButton.onClick.AddListener(HandleSelectAttackClick);

        if (SelectDefendButton != null)
            SelectDefendButton.onClick.AddListener(HandleSelectDefendClick);

        if (AddPowerDiceButton != null)
            AddPowerDiceButton.onClick.AddListener(HandleAddPowerDiceClick);

        if (RemovePowerDiceButton != null)
            RemovePowerDiceButton.onClick.AddListener(HandleRemovePowerDiceClick);

        if (AddAccuracyDiceButton != null)
            AddAccuracyDiceButton.onClick.AddListener(HandleAddAccuracyDiceClick);

        if (RemoveAccuracyDiceButton != null)
            RemoveAccuracyDiceButton.onClick.AddListener(HandleRemoveAccuracyDiceClick);

        if (EndTurnButton != null)
            EndTurnButton.onClick.AddListener(HandleEndTurnClick);

        if (ConfirmButton != null)
            ConfirmButton.onClick.AddListener(HandleConfirmClick);

        if (SelectMindPowerDiceButton != null)
            SelectMindPowerDiceButton.onClick.AddListener(HandleMindPowerDiceTypeClick);

        if (SelectHeartPowerDiceButton != null)
            SelectHeartPowerDiceButton.onClick.AddListener(HandleHeartPowerDiceTypeClick);

        if (SelectBodyPowerDiceButton != null)
            SelectBodyPowerDiceButton.onClick.AddListener(HandleBodyPowerDiceTypeClick);

        if (SelectMindAccuracyDiceButton != null)
            SelectMindAccuracyDiceButton.onClick.AddListener(HandleMindAccuracyDiceTypeClick);

        if (SelectHeartAccuracyDiceButton != null)
            SelectHeartAccuracyDiceButton.onClick.AddListener(HandleHeartAccuracyDiceTypeClick);

        if (SelectBodyAccuracyDiceButton != null)
            SelectBodyAccuracyDiceButton.onClick.AddListener(HandleBodyAccuracyDiceTypeClick);

        if (InfoToggle != null)
            InfoToggle.onValueChanged.AddListener(HandleInfoToggleChanged);

        if (BoundInputHandler != null)
            BoundInputHandler.ConfirmAvailabilityChanged -= SetConfirmInteractable;

        HideConfirmPanel();
    }

    private void OnDestroy()
    {
        if (SelectAttackButton != null)
            SelectAttackButton.onClick.RemoveListener(HandleSelectAttackClick);

        if (SelectDefendButton != null)
            SelectDefendButton.onClick.RemoveListener(HandleSelectDefendClick);

        if (AddPowerDiceButton != null)
            AddPowerDiceButton.onClick.RemoveListener(HandleAddPowerDiceClick);

        if (RemovePowerDiceButton != null)
            RemovePowerDiceButton.onClick.RemoveListener(HandleRemovePowerDiceClick);

        if (AddAccuracyDiceButton != null)
            AddAccuracyDiceButton.onClick.RemoveListener(HandleAddAccuracyDiceClick);

        if (RemoveAccuracyDiceButton != null)
            RemoveAccuracyDiceButton.onClick.RemoveListener(HandleRemoveAccuracyDiceClick);

        if (EndTurnButton != null)
            EndTurnButton.onClick.RemoveListener(HandleEndTurnClick);

        if (ConfirmButton != null)
            ConfirmButton.onClick.RemoveListener(HandleConfirmClick);

        if (SelectMindPowerDiceButton != null)
            SelectMindPowerDiceButton.onClick.RemoveListener(HandleMindPowerDiceTypeClick);

        if (SelectHeartPowerDiceButton != null)
            SelectHeartPowerDiceButton.onClick.RemoveListener(HandleHeartPowerDiceTypeClick);

        if (SelectBodyPowerDiceButton != null)
            SelectBodyPowerDiceButton.onClick.RemoveListener(HandleBodyPowerDiceTypeClick);

        if (SelectMindAccuracyDiceButton != null)
            SelectMindAccuracyDiceButton.onClick.RemoveListener(HandleMindAccuracyDiceTypeClick);  

        if (SelectHeartAccuracyDiceButton != null)
            SelectHeartAccuracyDiceButton.onClick.RemoveListener(HandleHeartAccuracyDiceTypeClick);

        if (SelectBodyAccuracyDiceButton != null)
            SelectBodyAccuracyDiceButton.onClick.RemoveListener(HandleBodyAccuracyDiceTypeClick);

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
        SelectAttackClicked += inputHandler.OnSelectAttack;
        SelectDefendClicked += inputHandler.OnSelectDefend;
        AddPowerDiceClicked += inputHandler.OnAddPowerDice;
        RemovePowerDiceClicked += inputHandler.OnRemovePowerDice;
        AddAccuracyDiceClicked += inputHandler.OnAddAccuracyDice;
        RemoveAccuracyDiceClicked += inputHandler.OnRemoveAccuracyDice;
        EndTurnClicked += inputHandler.OnSkipTurn;
        ConfirmClicked += inputHandler.OnConfirmAction;
        MindPowerDiceTypeSelected += inputHandler.OnSelectMindPowerDiceType;
        HeartPowerDiceTypeSelected += inputHandler.OnSelectHeartPowerDiceType;
        BodyPowerDiceTypeSelected += inputHandler.OnSelectBodyPowerDiceType;
        MindAccuracyDiceTypeSelected += inputHandler.OnSelectMindAccuracyDiceType;
        HeartAccuracyDiceTypeSelected += inputHandler.OnSelectHeartAccuracyDiceType;
        BodyAccuracyDiceTypeSelected += inputHandler.OnSelectBodyAccuracyDiceType;
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
        if (SelectAttackButton != null)
            SelectAttackButton.interactable = isInteractable;

        if (SelectDefendButton != null)
            SelectDefendButton.interactable = isInteractable;

        if (AddPowerDiceButton != null)
            AddPowerDiceButton.interactable = isInteractable;

        if (RemovePowerDiceButton != null)
            RemovePowerDiceButton.interactable = isInteractable;

        if (AddAccuracyDiceButton != null)
            AddAccuracyDiceButton.interactable = isInteractable;

        if (RemoveAccuracyDiceButton != null)
            RemoveAccuracyDiceButton.interactable = isInteractable;

        if (EndTurnButton != null)
            EndTurnButton.interactable = isInteractable;

        if (ConfirmButton != null)
            ConfirmButton.interactable = isInteractable;

        if (InfoToggle != null)
            InfoToggle.interactable = isInteractable;
    }

    private void HandleSelectAttackClick()
    {
        SelectAttackClicked?.Invoke();
    }

    private void HandleSelectDefendClick()
    {
        SelectDefendClicked?.Invoke();
    }

    private void HandleAddPowerDiceClick()
    {
        AddPowerDiceClicked?.Invoke();
    }

    private void HandleAddAccuracyDiceClick()
    {
        AddAccuracyDiceClicked?.Invoke();
    }

    private void HandleRemovePowerDiceClick()
    {
        RemovePowerDiceClicked?.Invoke();
    }

    private void HandleRemoveAccuracyDiceClick()
    {
        RemoveAccuracyDiceClicked?.Invoke();
    }

    private void HandleEndTurnClick()
    {
        EndTurnClicked?.Invoke();
    }

    private void HandleConfirmClick()
    {
        ConfirmClicked?.Invoke();
    }

    private void HandleMindPowerDiceTypeClick()
    {
        MindPowerDiceTypeSelected?.Invoke();
    }

    private void HandleHeartPowerDiceTypeClick()
    {
        HeartPowerDiceTypeSelected?.Invoke();
    }

    private void HandleBodyPowerDiceTypeClick()
    {
        BodyPowerDiceTypeSelected?.Invoke();
    }

    private void HandleMindAccuracyDiceTypeClick()
    {
        MindAccuracyDiceTypeSelected?.Invoke();
    }

    private void HandleHeartAccuracyDiceTypeClick()
    {
        HeartAccuracyDiceTypeSelected?.Invoke();
    }

    private void HandleBodyAccuracyDiceTypeClick()
    {
        BodyAccuracyDiceTypeSelected?.Invoke();
    }

    private void HandleInfoToggleChanged(bool isEnabled)
    {
        InfoToggled?.Invoke(isEnabled);
    }

    public void SetPlayerRoleButtons(bool isPlayerAttacker)
    {
        if (SelectAttackButton != null)
            SelectAttackButton.gameObject.SetActive(isPlayerAttacker);

        if (SelectDefendButton != null)
            SelectDefendButton.gameObject.SetActive(!isPlayerAttacker);
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
