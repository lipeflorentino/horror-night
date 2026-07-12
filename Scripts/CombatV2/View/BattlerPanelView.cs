using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattlerPanelView : MonoBehaviour
{
    public int currentHp, maxHp, currentMind, currentBody, currentHeart;

    [Header("HUD Text")]
    public TMP_Text NameText, LevelText;
    public TMP_Text MindText;
    public TMP_Text HeartText;
    public TMP_Text BodyText;
    public Image HpFill;
    public TMP_Text HpText;
    public TMP_Text DiceText;
    public TMP_Text AttackText;
    public TMP_Text DefenseText;
    public TMP_Text InitiativeText;
    public TMP_Text FocusText;
    public TMP_Text StrengthText;
    public TMP_Text AgilityText;

    public void Bind(Battler battler, PerkService perkService = null)
    {
        // Bind Stats
        currentHp = battler.HP;
        maxHp = battler.MaxHp;
        currentMind = perkService != null ? perkService.GetEffectiveMind(battler) : battler.Mind;
        currentHeart = perkService != null ? perkService.GetEffectiveHeart(battler) : battler.Heart;
        currentBody = perkService != null ? perkService.GetEffectiveBody(battler) : battler.Body;

        // Bind Texts
        if (NameText != null)
            NameText.text = battler.Name;

        if (LevelText != null)
            LevelText.text = "Lv. " + battler.Level.ToString();

        if (MindText != null)
            MindText.text = currentMind.ToString();

        if (HeartText != null)
            HeartText.text = currentHeart.ToString();

        if (BodyText != null)
            BodyText.text = currentBody.ToString();

        if (HpText != null)
            HpText.text = battler.HP.ToString();

        if (HpFill != null)
            HpFill.fillAmount = Mathf.Clamp01((float)battler.HP / battler.MaxHp);

        // TODO: verificar criação de currentPowerDices e currentAccuracyDices
        // if (DiceText != null)
        //     DiceText.text = battler.CurrentDices.ToString();

        int effectiveAttack = perkService != null ? perkService.GetEffectiveActionPower(battler, null, ActionType.Attack) : battler.Attack;
        int effectiveDefense = perkService != null ? perkService.GetEffectiveActionPower(battler, null, ActionType.Defense) : battler.Defense;

        if (AttackText != null)
            AttackText.text = effectiveAttack.ToString();
        
        if (DefenseText != null)
            DefenseText.text = effectiveDefense.ToString();

        if (InitiativeText != null)
            InitiativeText.text = battler.Initiative.ToString();

        if (FocusText != null)
            FocusText.text = battler.Focus.ToString();

        if (StrengthText != null)
            StrengthText.text = battler.Strength.ToString();

        if (AgilityText != null)
            AgilityText.text = battler.Agility.ToString();

    }
}
