using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionPanelView : MonoBehaviour
{
    public Button attackButton;
    public Button defendButton;
    public Button addDiceAttackButton;
    public TMP_Text addDiceAttackCountText;
    public Button addDiceDefenseButton;
    public TMP_Text addDiceDefenseCountText;
    public Button endTurnButton;
    public event Action AttackClicked;
    public event Action DefendClicked;
    public event Action AddDiceToAttackClicked;
    public event Action AddDiceToDefenseClicked;
    public event Action EndTurnClicked;

    private void Awake()
    {
        if (attackButton != null)
            attackButton.onClick.AddListener(HandleAttackClick);

        if (defendButton != null)
            defendButton.onClick.AddListener(HandleDefendClick);

        if (addDiceAttackButton != null)
            addDiceAttackButton.onClick.AddListener(HandleAddDiceToAttackClick);

        if (addDiceDefenseButton != null)
            addDiceDefenseButton.onClick.AddListener(HandleAddDiceToDefenseClick);

        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(HandleEndTurnClick);
    }

    private void OnDestroy()
    {
        if (attackButton != null)
            attackButton.onClick.RemoveListener(HandleAttackClick);

        if (defendButton != null)
            defendButton.onClick.RemoveListener(HandleDefendClick);

        if (addDiceAttackButton != null)
            addDiceAttackButton.onClick.RemoveListener(HandleAddDiceToAttackClick);

        if (addDiceDefenseButton != null)
            addDiceDefenseButton.onClick.RemoveListener(HandleAddDiceToDefenseClick);

        if (endTurnButton != null)
            endTurnButton.onClick.RemoveListener(HandleEndTurnClick);
    }

    public void BindInput(CombatInputHandler inputHandler)
    {
        AttackClicked += inputHandler.OnAttack;
        DefendClicked += inputHandler.OnDefend;
        AddDiceToAttackClicked += inputHandler.OnAddDiceToAttack;
        AddDiceToDefenseClicked += inputHandler.OnAddDiceToDefense;
        EndTurnClicked += inputHandler.OnEndTurn;
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
        attackButton.gameObject.SetActive(isPlayerAttacker);
        addDiceAttackButton.gameObject.SetActive(isPlayerAttacker);

        defendButton.gameObject.SetActive(!isPlayerAttacker);
        addDiceDefenseButton.gameObject.SetActive(!isPlayerAttacker);
    }

    public void UpdateAddDiceAttackCount(int count)
    {
        addDiceAttackCountText.text = count.ToString();
    }

    public void UpdateAddDiceDefenseCount(int count)
    {
        addDiceDefenseCountText.text = count.ToString();
    }
}
