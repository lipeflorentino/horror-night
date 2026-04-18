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
        nameText.text = battler.Name;

        mindText.text = battler.Mind.ToString();
        heartText.text = battler.Heart.ToString();
        bodyText.text = battler.Body.ToString();

        hpText.text = battler.HP.ToString();
        hpFill.fillAmount = battler.HP / 100f;

        diceText.text = battler.CurrentDices.ToString();
    }
}