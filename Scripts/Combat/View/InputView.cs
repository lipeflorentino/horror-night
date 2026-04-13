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
    [SerializeField] private Button infoButton;
    [SerializeField] private Button itemButton;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button addAttackDiceButton;
    [SerializeField] private Button addInvestigateDiceButton;
    [SerializeField] private Button addDefendDiceButton;
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
    public event Action OnItem;
    public event Action OnSkill;
    public event Action<int> OnAddAttackDice;
    public event Action<int> OnAddInvestigateDice;
    public event Action<int> OnAddDefendDice;
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

        if (addAttackDiceButton != null)
            addAttackDiceButton.onClick.AddListener(RaiseAddAttackDice);

        if (addInvestigateDiceButton != null)
            addInvestigateDiceButton.onClick.AddListener(RaiseAddInvestigateDice);

        if (addDefendDiceButton != null)
            addDefendDiceButton.onClick.AddListener(RaiseAddDefendDice);

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

        if (addAttackDiceButton != null)
            addAttackDiceButton.onClick.RemoveListener(RaiseAddAttackDice);

        if (addInvestigateDiceButton != null)
            addInvestigateDiceButton.onClick.RemoveListener(RaiseAddInvestigateDice);

        if (addDefendDiceButton != null)
            addDefendDiceButton.onClick.RemoveListener(RaiseAddDefendDice);

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

    private void RaiseEndTurn() => OnEndTurn?.Invoke();

    private void RaiseItem() => OnItem?.Invoke();

    private void RaiseSkill() => OnSkill?.Invoke();

    private void RaiseUseItem() => OnUseItem?.Invoke();

    private void RaiseUseSkill() => OnUseSkill?.Invoke();

    private void RaiseInfo() => OnInfo?.Invoke();
}
