using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InputView : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button investigateButton;
    [SerializeField] private Button defendButton;
    [SerializeField] private Button fleeButton;
    [SerializeField] private Button infoButton;
    [SerializeField] private Button itemButton;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button addAttackDiceButton;
    [SerializeField] private Button addInvestigateDiceButton;
    [SerializeField] private Button addDefendDiceButton;
    [SerializeField] private Button subtractAttackDiceButton;
    [SerializeField] private Button subtractInvestigateDiceButton;
    [SerializeField] private Button subtractDefendDiceButton;
    [SerializeField] private Button useItemButton;
    [SerializeField] private Button useSkillButton;
    [SerializeField] private Button endTurnButton;

    [Header("Counts")]
    [SerializeField] private TMP_Text attackDiceCountText;
    [SerializeField] private TMP_Text investigateDiceCountText;
    [SerializeField] private TMP_Text defendDiceCountText;

    public event Action OnAttack;
    public event Action OnInvestigate;
    public event Action OnDefend;
    public event Action OnFlee;
    public event Action OnItem;
    public event Action OnSkill;
    public event Action<int> OnAddAttackDice;
    public event Action<int> OnAddInvestigateDice;
    public event Action<int> OnAddDefendDice;
    public event Action<int> OnSubtractAttackDice;
    public event Action<int> OnSubtractInvestigateDice;
    public event Action<int> OnSubtractDefendDice;
    public event Action OnUseItem;
    public event Action OnUseSkill;
    public event Action OnInfo;
    public event Action OnEndTurn;

    private int attackBonusDice;
    private int investigateBonusDice;
    private int defendBonusDice;

    private void Awake()
    {
        if (attackButton != null)
            attackButton.onClick.AddListener(RaiseAttack);

        if (investigateButton != null)
            investigateButton.onClick.AddListener(RaiseInvestigate);

        if (defendButton != null)
            defendButton.onClick.AddListener(RaiseDefend);

        if (fleeButton != null)
            fleeButton.onClick.AddListener(RaiseFlee);

        if (addAttackDiceButton != null)
            addAttackDiceButton.onClick.AddListener(RaiseAddAttackDice);

        if (addInvestigateDiceButton != null)
            addInvestigateDiceButton.onClick.AddListener(RaiseAddInvestigateDice);

        if (addDefendDiceButton != null)
            addDefendDiceButton.onClick.AddListener(RaiseAddDefendDice);

        if (subtractAttackDiceButton != null)
            subtractAttackDiceButton.onClick.AddListener(RaiseSubtractAttackDice);

        if (subtractInvestigateDiceButton != null)
            subtractInvestigateDiceButton.onClick.AddListener(RaiseSubtractInvestigateDice);

        if (subtractDefendDiceButton != null)
            subtractDefendDiceButton.onClick.AddListener(RaiseSubtractDefendDice);

        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(RaiseEndTurn);

        if (itemButton != null)
            itemButton.onClick.AddListener(RaiseItem);

        if (skillButton != null)
            skillButton.onClick.AddListener(RaiseSkill);

        if (useItemButton != null)
            useItemButton.onClick.AddListener(RaiseUseItem);

        if (useSkillButton != null)
            useSkillButton.onClick.AddListener(RaiseUseSkill);

        if (infoButton != null)
            infoButton.onClick.AddListener(RaiseInfo);
    }

    private void OnDestroy()
    {
        if (attackButton != null)
            attackButton.onClick.RemoveListener(RaiseAttack);

        if (investigateButton != null)
            investigateButton.onClick.RemoveListener(RaiseInvestigate);

        if (defendButton != null)
            defendButton.onClick.RemoveListener(RaiseDefend);

        if (fleeButton != null)
            fleeButton.onClick.RemoveListener(RaiseFlee);

        if (addAttackDiceButton != null)
            addAttackDiceButton.onClick.RemoveListener(RaiseAddAttackDice);

        if (addInvestigateDiceButton != null)
            addInvestigateDiceButton.onClick.RemoveListener(RaiseAddInvestigateDice);

        if (addDefendDiceButton != null)
            addDefendDiceButton.onClick.RemoveListener(RaiseAddDefendDice);

        if (subtractAttackDiceButton != null)
            subtractAttackDiceButton.onClick.RemoveListener(RaiseSubtractAttackDice);

        if (subtractInvestigateDiceButton != null)
            subtractInvestigateDiceButton.onClick.RemoveListener(RaiseSubtractInvestigateDice);

        if (subtractDefendDiceButton != null)
            subtractDefendDiceButton.onClick.RemoveListener(RaiseSubtractDefendDice);

        if (endTurnButton != null)
            endTurnButton.onClick.RemoveListener(RaiseEndTurn);

        if (itemButton != null)
            itemButton.onClick.RemoveListener(RaiseItem);

        if (skillButton != null)
            skillButton.onClick.RemoveListener(RaiseSkill);

        if (useItemButton != null)
            useItemButton.onClick.RemoveListener(RaiseUseItem);

        if (useSkillButton != null)
            useSkillButton.onClick.RemoveListener(RaiseUseSkill);

        if (infoButton != null)
            infoButton.onClick.RemoveListener(RaiseInfo);
    }

    private void RaiseAttack()
    {
        attackBonusDice = 0;
        OnAttack?.Invoke();
    }

    private void RaiseInvestigate()
    {
        investigateBonusDice = 0;
        OnInvestigate?.Invoke();
    }

    private void RaiseDefend()
    {
        defendBonusDice = 0;
        OnDefend?.Invoke();
    }

    private void RaiseFlee()
    {
        OnFlee?.Invoke();
    }

    private void RaiseAddAttackDice()
    {
        attackBonusDice++;
        attackDiceCountText.text = "" + attackBonusDice;
        OnAddAttackDice?.Invoke(1 + attackBonusDice);
    }

    private void RaiseAddInvestigateDice()
    {
        investigateBonusDice++;
        investigateDiceCountText.text = "" + investigateBonusDice;
        OnAddInvestigateDice?.Invoke(1 + investigateBonusDice);
    }

    private void RaiseAddDefendDice()
    {
        defendBonusDice++;
        defendDiceCountText.text = "" + defendBonusDice;
        OnAddDefendDice?.Invoke(1 + defendBonusDice);
    }

    private void RaiseSubtractAttackDice()
    {
        if (attackBonusDice > 0)
        {
            attackBonusDice--;
            attackDiceCountText.text = "" + attackBonusDice;
            OnSubtractAttackDice?.Invoke(1);
        }
    }

    private void RaiseSubtractInvestigateDice()
    {
        if (investigateBonusDice > 0)
        {
            investigateBonusDice--;
            investigateDiceCountText.text = "" + investigateBonusDice;
            OnSubtractInvestigateDice?.Invoke(1);
        }
    }

    private void RaiseSubtractDefendDice()
    {
        if (defendBonusDice > 0)
        {
            defendBonusDice--;
            defendDiceCountText.text = "" + defendBonusDice;
            OnSubtractDefendDice?.Invoke(1);
        }
    }

    private void RaiseEndTurn() => OnEndTurn?.Invoke();

    private void RaiseItem() => OnItem?.Invoke();

    private void RaiseSkill() => OnSkill?.Invoke();

    private void RaiseUseItem() => OnUseItem?.Invoke();

    private void RaiseUseSkill() => OnUseSkill?.Invoke();

    private void RaiseInfo() => OnInfo?.Invoke();

    public void ShowPlayerTurnUI()
    {
        SetPrimaryActionsEnabled(true);
        SetSecondaryActionsEnabled(true);
        SetDiceManipulationEnabled(true);
        
        if (endTurnButton != null)
            endTurnButton.interactable = true;
    }

    public void ShowEnemyTurnUI()
    {
        SetPrimaryActionsEnabled(false);
        SetSecondaryActionsEnabled(false);
        SetDiceManipulationEnabled(false);
        
        if (endTurnButton != null)
            endTurnButton.interactable = false;
    }

    public void DisablePrimaryActions()
    {
        if (attackButton != null)
            attackButton.interactable = false;

        if (investigateButton != null)
            investigateButton.interactable = false;

        if (defendButton != null)
            defendButton.interactable = false;
    }

    public void ResetPrimaryActions()
    {
        if (attackButton != null)
            attackButton.interactable = true;

        if (investigateButton != null)
            investigateButton.interactable = true;

        if (defendButton != null)
            defendButton.interactable = true;

        attackBonusDice = 0;
        investigateBonusDice = 0;
        defendBonusDice = 0;
        
        UpdateDiceCounters();
    }

    public void UpdateDiceCounters()
    {
        if (attackDiceCountText != null)
            attackDiceCountText.text = attackBonusDice.ToString();

        if (investigateDiceCountText != null)
            investigateDiceCountText.text = investigateBonusDice.ToString();

        if (defendDiceCountText != null)
            defendDiceCountText.text = defendBonusDice.ToString();
    }

    private void SetPrimaryActionsEnabled(bool enabled)
    {
        if (attackButton != null)
            attackButton.interactable = enabled;

        if (investigateButton != null)
            investigateButton.interactable = enabled;

        if (defendButton != null)
            defendButton.interactable = enabled;
    }

    private void SetSecondaryActionsEnabled(bool enabled)
    {
        if (itemButton != null)
            itemButton.interactable = enabled;

        if (skillButton != null)
            skillButton.interactable = enabled;

        if (useItemButton != null)
            useItemButton.interactable = enabled;

        if (useSkillButton != null)
            useSkillButton.interactable = enabled;
    }

    private void SetDiceManipulationEnabled(bool enabled)
    {
        if (addAttackDiceButton != null)
            addAttackDiceButton.interactable = enabled;

        if (addInvestigateDiceButton != null)
            addInvestigateDiceButton.interactable = enabled;

        if (addDefendDiceButton != null)
            addDefendDiceButton.interactable = enabled;

        if (subtractAttackDiceButton != null)
            subtractAttackDiceButton.interactable = enabled;

        if (subtractInvestigateDiceButton != null)
            subtractInvestigateDiceButton.interactable = enabled;

        if (subtractDefendDiceButton != null)
            subtractDefendDiceButton.interactable = enabled;
    }
    
    public void UpdateAttackDiceCount(int count)
    {
        attackBonusDice = Mathf.Max(0, count);
        if (attackDiceCountText != null)
            attackDiceCountText.text = attackBonusDice.ToString();
    }
    
    public void UpdateInvestigateDiceCount(int count)
    {
        investigateBonusDice = Mathf.Max(0, count);
        if (investigateDiceCountText != null)
            investigateDiceCountText.text = investigateBonusDice.ToString();
    }

    public void UpdateDefendDiceCount(int count)
    {
        defendBonusDice = Mathf.Max(0, count);
        if (defendDiceCountText != null)
            defendDiceCountText.text = defendBonusDice.ToString();
    }

    public void ShowAttackPhaseUI()
    {
        if (attackButton != null)
            attackButton.interactable = true;

        if (investigateButton != null)
            investigateButton.interactable = true;

        if (defendButton != null)
            defendButton.interactable = false;

        SetSecondaryActionsEnabled(true);
        SetDiceManipulationEnabled(true);

        if (endTurnButton != null)
            endTurnButton.interactable = true;

        ResetPrimaryActions();
    }

    public void ShowDefensePhaseUI()
    {
        if (attackButton != null)
            attackButton.interactable = false;

        if (investigateButton != null)
            investigateButton.interactable = false;

        if (defendButton != null)
            defendButton.interactable = true;

        SetSecondaryActionsEnabled(true);
        SetDiceManipulationEnabled(true);

        if (endTurnButton != null)
            endTurnButton.interactable = true;

        ResetPrimaryActions();
    }
}
