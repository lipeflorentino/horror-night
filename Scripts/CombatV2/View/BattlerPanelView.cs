using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattlerPanelView : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text mindText;
    public TMP_Text heartText;
    public TMP_Text bodyText;

    public Image hpFill;
    public TMP_Text hpText;

    public TMP_Text diceText;

    public void Bind(Battler battler)
    {
        if (nameText != null)
            nameText.text = battler.Name;

        if (mindText != null)
            mindText.text = battler.Mind.ToString();
        if (heartText != null)
            heartText.text = battler.Heart.ToString();
        if (bodyText != null)
            bodyText.text = battler.Body.ToString();

        if (hpText != null)
            hpText.text = battler.HP.ToString();
        if (hpFill != null)
            hpFill.fillAmount = battler.HP / 100f;

        if (diceText != null)
            diceText.text = battler.CurrentDices.ToString();
    }
}