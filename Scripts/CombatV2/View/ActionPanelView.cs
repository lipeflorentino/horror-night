using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionPanelView : MonoBehaviour
{
    public Button SelectAttackButton;
    public Button SelectDefendButton;
    public Button AddMindPowerDiceButton, AddHeartPowerDiceButton, AddBodyPowerDiceButton, AddMindAccuracyDiceButton, AddHeartAccuracyDiceButton, AddBodyAccuracyDiceButton;
    public Button RemoveMindPowerDiceButton, RemoveHeartPowerDiceButton, RemoveBodyPowerDiceButton, RemoveMindAccuracyDiceButton, RemoveHeartAccuracyDiceButton, RemoveBodyAccuracyDiceButton;
    public GameObject AttackDiceCountContainer, DefendDiceCountContainer;
    public Button EndTurnButton;
    public Toggle InfoToggle;

    [Header("Confirm Action Panel")]
    public GameObject ConfirmPanel;
    public TMP_Text ConfirmActionText;
    public Button ConfirmButton;

    [Header("Stat Dice Selector")]
    public TMP_Text SelectedDiceTypeText;

    public event Action SelectAttackClicked;
    public event Action SelectDefendClicked;
    public event Action SkipTurnClicked;
    public event Action ConfirmClicked;
    public event Action<DiceStatType, DiceRollType> AddMindPowerDiceClicked, AddHeartPowerDiceClicked, AddBodyPowerDiceClicked, AddMindAccuracyDiceClicked, AddHeartAccuracyDiceClicked, AddBodyAccuracyDiceClicked;
    public event Action<DiceStatType, DiceRollType> RemoveMindPowerDiceClicked, RemoveHeartPowerDiceClicked, RemoveBodyPowerDiceClicked, RemoveMindAccuracyDiceClicked, RemoveHeartAccuracyDiceClicked, RemoveBodyAccuracyDiceClicked;
    public event Action<bool> InfoToggled;
    private CombatInputHandler BoundInputHandler;

    private void Awake()
    {
        if (SelectAttackButton != null)
            SelectAttackButton.onClick.AddListener(HandleSelectAttackClick);

        if (SelectDefendButton != null)
            SelectDefendButton.onClick.AddListener(HandleSelectDefendClick);

        if (EndTurnButton != null)
            EndTurnButton.onClick.AddListener(HandleSkipTurnClick);

        if (ConfirmButton != null)
            ConfirmButton.onClick.AddListener(HandleConfirmClick);

        // ADD

        if (AddMindPowerDiceButton != null)
            AddMindPowerDiceButton.onClick.AddListener(HandleAddMindPowerDiceClick);

        if (AddHeartPowerDiceButton != null)
            AddHeartPowerDiceButton.onClick.AddListener(HandleAddHeartPowerDiceClick);

        if (AddBodyPowerDiceButton != null)
            AddBodyPowerDiceButton.onClick.AddListener(HandleAddBodyPowerDiceClick);

        if (AddMindAccuracyDiceButton != null)
            AddMindAccuracyDiceButton.onClick.AddListener(HandleAddMindAccuracyDiceClick);

        if (AddHeartAccuracyDiceButton != null)
            AddHeartAccuracyDiceButton.onClick.AddListener(HandleAddHeartAccuracyDiceClick);

        if (AddBodyAccuracyDiceButton != null)
            AddBodyAccuracyDiceButton.onClick.AddListener(HandleAddBodyAccuracyDiceClick);

        // REMOVE

        if (RemoveMindPowerDiceButton != null)
            RemoveMindPowerDiceButton.onClick.AddListener(HandleRemoveMindPowerDiceClick);

        if (RemoveHeartPowerDiceButton != null)
            RemoveHeartPowerDiceButton.onClick.AddListener(HandleRemoveHeartPowerDiceClick);

        if (RemoveBodyPowerDiceButton != null)
            RemoveBodyPowerDiceButton.onClick.AddListener(HandleRemoveBodyPowerDiceClick);

        if (RemoveMindAccuracyDiceButton != null)
            RemoveMindAccuracyDiceButton.onClick.AddListener(HandleRemoveMindAccuracyDiceClick);

        if (RemoveHeartAccuracyDiceButton != null)
            RemoveHeartAccuracyDiceButton.onClick.AddListener(HandleRemoveHeartAccuracyDiceClick);

        if (RemoveBodyAccuracyDiceButton != null)
            RemoveBodyAccuracyDiceButton.onClick.AddListener(HandleRemoveBodyAccuracyDiceClick);

        // TOGGLE

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

        if (EndTurnButton != null)
            EndTurnButton.onClick.RemoveListener(HandleSkipTurnClick);

        if (ConfirmButton != null)
            ConfirmButton.onClick.RemoveListener(HandleConfirmClick);

        // ADD

        if (AddMindPowerDiceButton != null)
            AddMindPowerDiceButton.onClick.RemoveListener(HandleAddMindPowerDiceClick);

        if (AddHeartPowerDiceButton != null)
            AddHeartPowerDiceButton.onClick.RemoveListener(HandleAddHeartPowerDiceClick);

        if (AddBodyPowerDiceButton != null)
            AddBodyPowerDiceButton.onClick.RemoveListener(HandleAddBodyPowerDiceClick);

        if (AddMindAccuracyDiceButton != null)
            AddMindAccuracyDiceButton.onClick.RemoveListener(HandleAddMindAccuracyDiceClick);  

        if (AddHeartAccuracyDiceButton != null)
            AddHeartAccuracyDiceButton.onClick.RemoveListener(HandleAddHeartAccuracyDiceClick);

        if (AddBodyAccuracyDiceButton != null)
            AddBodyAccuracyDiceButton.onClick.RemoveListener(HandleAddBodyAccuracyDiceClick);

        // REMOVE

        if (RemoveMindPowerDiceButton != null)
            RemoveMindPowerDiceButton.onClick.RemoveListener(HandleRemoveMindPowerDiceClick);

        if (RemoveHeartPowerDiceButton != null)
            RemoveHeartPowerDiceButton.onClick.RemoveListener(HandleRemoveHeartPowerDiceClick);

        if (RemoveBodyPowerDiceButton != null)
            RemoveBodyPowerDiceButton.onClick.RemoveListener(HandleRemoveBodyPowerDiceClick);

        if (RemoveMindAccuracyDiceButton != null)
            RemoveMindAccuracyDiceButton.onClick.RemoveListener(HandleRemoveMindAccuracyDiceClick);  

        if (RemoveHeartAccuracyDiceButton != null)
            RemoveHeartAccuracyDiceButton.onClick.RemoveListener(HandleRemoveHeartAccuracyDiceClick);

        if (RemoveBodyAccuracyDiceButton != null)
            RemoveBodyAccuracyDiceButton.onClick.RemoveListener(HandleRemoveBodyAccuracyDiceClick);

        // TOGGLE

        if (InfoToggle != null)
            InfoToggle.onValueChanged.RemoveListener(HandleInfoToggleChanged);

        if (BoundInputHandler != null)
            BoundInputHandler.ConfirmAvailabilityChanged -= SetConfirmInteractable;
    }

    public void BindInput(CombatInputHandler inputHandler)
    {
        if (BoundInputHandler != null)
        {
            SelectAttackClicked -= BoundInputHandler.OnSelectAttack;
            SelectDefendClicked -= BoundInputHandler.OnSelectDefend;
            AddMindPowerDiceClicked -= BoundInputHandler.OnAddDice;
            RemoveMindPowerDiceClicked -= BoundInputHandler.OnRemoveDice;
            AddHeartPowerDiceClicked -= BoundInputHandler.OnAddDice;
            RemoveHeartPowerDiceClicked -= BoundInputHandler.OnRemoveDice;
            AddBodyPowerDiceClicked -= BoundInputHandler.OnAddDice;
            RemoveBodyPowerDiceClicked -= BoundInputHandler.OnRemoveDice;
            AddMindAccuracyDiceClicked -= BoundInputHandler.OnAddDice;
            RemoveMindAccuracyDiceClicked -= BoundInputHandler.OnRemoveDice;
            AddHeartAccuracyDiceClicked -= BoundInputHandler.OnAddDice;
            RemoveHeartAccuracyDiceClicked -= BoundInputHandler.OnRemoveDice;
            AddBodyAccuracyDiceClicked -= BoundInputHandler.OnAddDice;
            RemoveBodyAccuracyDiceClicked -= BoundInputHandler.OnRemoveDice;
            SkipTurnClicked -= BoundInputHandler.OnSkipTurn;
            ConfirmClicked -= BoundInputHandler.OnConfirmAction;
            InfoToggled -= BoundInputHandler.OnToggleInfoPanel;
        }

        if (BoundInputHandler != null)
            BoundInputHandler.ConfirmAvailabilityChanged -= SetConfirmInteractable;

        BoundInputHandler = inputHandler;

        SelectAttackClicked += inputHandler.OnSelectAttack;
        SelectDefendClicked += inputHandler.OnSelectDefend;

        // Power Dices
        AddMindPowerDiceClicked += inputHandler.OnAddDice;
        RemoveMindPowerDiceClicked += inputHandler.OnRemoveDice;
        AddHeartPowerDiceClicked += inputHandler.OnAddDice;
        RemoveHeartPowerDiceClicked += inputHandler.OnRemoveDice;
        AddBodyPowerDiceClicked += inputHandler.OnAddDice;
        RemoveBodyPowerDiceClicked += inputHandler.OnRemoveDice;

        // Accuracy Dices
        AddMindAccuracyDiceClicked += inputHandler.OnAddDice;
        RemoveMindAccuracyDiceClicked += inputHandler.OnRemoveDice;
        AddHeartAccuracyDiceClicked += inputHandler.OnAddDice;
        RemoveHeartAccuracyDiceClicked += inputHandler.OnRemoveDice;
        AddBodyAccuracyDiceClicked += inputHandler.OnAddDice;
        RemoveBodyAccuracyDiceClicked += inputHandler.OnRemoveDice;

        SkipTurnClicked += inputHandler.OnSkipTurn;
        ConfirmClicked += inputHandler.OnConfirmAction;

        InfoToggled += inputHandler.OnToggleInfoPanel;

        inputHandler.ConfirmAvailabilityChanged += SetConfirmInteractable;

        if (InfoToggle != null)
            InfoToggle.SetIsOnWithoutNotify(false);

        SetConfirmInteractable(false);
        SetSelectedDiceTypeLabel("P:Body | A:Body");
        HideConfirmPanel();
    }

    public void SetAddDiceButtonsInteractable(DiceStatType type, bool isInteractable)
    {
        switch (type)
        {
            case DiceStatType.Mind:
                if (AddMindPowerDiceButton != null) AddMindPowerDiceButton.interactable = isInteractable;
                if (AddMindAccuracyDiceButton != null) AddMindAccuracyDiceButton.interactable = isInteractable;
                break;
            case DiceStatType.Heart:
                if (AddHeartPowerDiceButton != null) AddHeartPowerDiceButton.interactable = isInteractable;
                if (AddHeartAccuracyDiceButton != null) AddHeartAccuracyDiceButton.interactable = isInteractable;
                break;
            case DiceStatType.Body:
                if (AddBodyPowerDiceButton != null) AddBodyPowerDiceButton.interactable = isInteractable;
                if (AddBodyAccuracyDiceButton != null) AddBodyAccuracyDiceButton.interactable = isInteractable;
                break;
        }
    }

    public void SetRemoveDiceButtonsInteractable(DiceStatType type, bool hasPowerDie, bool hasAccuracyDie)
    {
        switch (type)
        {
            case DiceStatType.Mind:
                if (RemoveMindPowerDiceButton != null) RemoveMindPowerDiceButton.interactable = hasPowerDie;
                if (RemoveMindAccuracyDiceButton != null) RemoveMindAccuracyDiceButton.interactable = hasAccuracyDie;
                break;
            case DiceStatType.Heart:
                if (RemoveHeartPowerDiceButton != null) RemoveHeartPowerDiceButton.interactable = hasPowerDie;
                if (RemoveHeartAccuracyDiceButton != null) RemoveHeartAccuracyDiceButton.interactable = hasAccuracyDie;
                break;
            case DiceStatType.Body:
                if (RemoveBodyPowerDiceButton != null) RemoveBodyPowerDiceButton.interactable = hasPowerDie;
                if (RemoveBodyAccuracyDiceButton != null) RemoveBodyAccuracyDiceButton.interactable = hasAccuracyDie;
                break;
        }
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

        if (AddMindPowerDiceButton != null)
            AddMindPowerDiceButton.interactable = isInteractable;

        if (RemoveMindPowerDiceButton != null)
            RemoveMindPowerDiceButton.interactable = isInteractable;

        if (AddMindAccuracyDiceButton != null)
            AddMindAccuracyDiceButton.interactable = isInteractable;

        if (RemoveMindAccuracyDiceButton != null)
            RemoveMindAccuracyDiceButton.interactable = isInteractable;

        if (AddHeartPowerDiceButton != null)
            AddHeartPowerDiceButton.interactable = isInteractable;

        if (RemoveHeartPowerDiceButton != null)
            RemoveHeartPowerDiceButton.interactable = isInteractable;

        if (AddHeartAccuracyDiceButton != null)
            AddHeartAccuracyDiceButton.interactable = isInteractable;

        if (RemoveHeartAccuracyDiceButton != null)
            RemoveHeartAccuracyDiceButton.interactable = isInteractable;

        if (AddBodyPowerDiceButton != null)
            AddBodyPowerDiceButton.interactable = isInteractable;

        if (RemoveBodyPowerDiceButton != null)
            RemoveBodyPowerDiceButton.interactable = isInteractable;

        if (AddBodyAccuracyDiceButton != null)
            AddBodyAccuracyDiceButton.interactable = isInteractable;

        if (RemoveBodyAccuracyDiceButton != null)
            RemoveBodyAccuracyDiceButton.interactable = isInteractable;

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

    private void HandleSkipTurnClick()
    {
        SkipTurnClicked?.Invoke();
    }

    private void HandleConfirmClick()
    {
        ConfirmClicked?.Invoke();
    }

    private void HandleAddMindPowerDiceClick()
    {
        AddMindPowerDiceClicked?.Invoke(DiceStatType.Mind, DiceRollType.Power);
    }

    private void HandleAddHeartPowerDiceClick()
    {
        AddHeartPowerDiceClicked?.Invoke(DiceStatType.Heart, DiceRollType.Power);
    }

    private void HandleAddBodyPowerDiceClick()
    {
        AddBodyPowerDiceClicked?.Invoke(DiceStatType.Body, DiceRollType.Power);
    }

    private void HandleAddMindAccuracyDiceClick()
    {
        AddMindAccuracyDiceClicked?.Invoke(DiceStatType.Mind, DiceRollType.Accuracy);
    }

    private void HandleAddHeartAccuracyDiceClick()
    {
        AddHeartAccuracyDiceClicked?.Invoke(DiceStatType.Heart, DiceRollType.Accuracy);
    }

    private void HandleAddBodyAccuracyDiceClick()
    {
        AddBodyAccuracyDiceClicked?.Invoke(DiceStatType.Body, DiceRollType.Accuracy);
    }

    private void HandleRemoveMindPowerDiceClick()
    {
        RemoveMindPowerDiceClicked?.Invoke(DiceStatType.Mind, DiceRollType.Power);
    }

    private void HandleRemoveHeartPowerDiceClick()
    {
        RemoveHeartPowerDiceClicked?.Invoke(DiceStatType.Heart, DiceRollType.Power);
    }

    private void HandleRemoveBodyPowerDiceClick()
    {
        RemoveBodyPowerDiceClicked?.Invoke(DiceStatType.Body, DiceRollType.Power);
    }

    private void HandleRemoveMindAccuracyDiceClick()
    {
        RemoveMindAccuracyDiceClicked?.Invoke(DiceStatType.Mind, DiceRollType.Accuracy);
    }

    private void HandleRemoveHeartAccuracyDiceClick()
    {
        RemoveHeartAccuracyDiceClicked?.Invoke(DiceStatType.Heart, DiceRollType.Accuracy);
    }

    private void HandleRemoveBodyAccuracyDiceClick()
    {
        RemoveBodyAccuracyDiceClicked?.Invoke(DiceStatType.Body, DiceRollType.Accuracy);
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

        SetSkipButtonVisible(isPlayerAttacker);
    }

    public void SetSkipButtonVisible(bool isVisible)
    {
        if (EndTurnButton != null)
            EndTurnButton.gameObject.SetActive(isVisible);
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
        if (action == null)
        {
            Debug.LogWarning("HighlightSelectedAction called with null action.");
            return;
        }

        bool isAttack = action.Definition != null && action.Definition.Type == ActionType.Attack;
        if (SelectAttackButton != null)
            SelectAttackButton.image.color = isAttack ? new Color(0.85f, 0.35f, 0.35f, 1f) : Color.white;

        if (SelectDefendButton != null)
            SelectDefendButton.image.color = isAttack ? Color.white : new Color(0.35f, 0.65f, 0.95f, 1f);
    }
}
