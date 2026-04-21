using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattlerPanelView : MonoBehaviour
{
    public TMP_Text NameText;
    public TMP_Text MindText;
    public TMP_Text HeartText;
    public TMP_Text BodyText;
    public Image HpFill;
    public TMP_Text HpText;
    public TMP_Text DiceText;

    public void Bind(Battler battler)
    {
        if (NameText != null)
            NameText.text = battler.Name;

        if (MindText != null)
            MindText.text = battler.Mind.ToString();

        if (HeartText != null)
            HeartText.text = battler.Heart.ToString();

        if (BodyText != null)
            BodyText.text = battler.Body.ToString();

        if (HpText != null)
            HpText.text = battler.HP.ToString();

        if (HpFill != null)
            HpFill.fillAmount = battler.HP / 100f;

        if (DiceText != null)
            DiceText.text = battler.CurrentDices.ToString();
    }
}