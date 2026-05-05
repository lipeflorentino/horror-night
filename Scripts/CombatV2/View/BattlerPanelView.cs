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
    // TODO: implementar effects (buff e debuff)

    public void Bind(Battler battler)
    {
        // Bind Stats
        currentHp = battler.HP;
        maxHp = battler.MaxHp;
        currentMind = battler.Mind;
        currentHeart = battler.Heart;
        currentBody = battler.Body;

        // Bind Texts
        if (NameText != null)
            NameText.text = battler.Name;

        if (LevelText != null)
            LevelText.text = "Lv. " + battler.Level.ToString();

        if (MindText != null)
            MindText.text = battler.Mind.ToString();

        if (HeartText != null)
            HeartText.text = battler.Heart.ToString();

        if (BodyText != null)
            BodyText.text = battler.Body.ToString();

        if (HpText != null)
            HpText.text = battler.HP.ToString();

        if (HpFill != null)
            HpFill.fillAmount = Mathf.Clamp01((float)battler.HP / battler.MaxHp);

        // TODO: verificar criação de currentPowerDices e currentAccuracyDices
        // if (DiceText != null)
        //     DiceText.text = battler.CurrentDices.ToString();

        if (AttackText != null)
            AttackText.text = battler.Attack.ToString();
        
        if (DefenseText != null)
            DefenseText.text = battler.Defense.ToString();

        if (InitiativeText != null)
            InitiativeText.text = battler.Initiative.ToString();
    }
}