using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattlerPanelView : MonoBehaviour
{
    private int currentMind, currentBody, currentHeart;

    [Header("HUD Text")]
    public TMP_Text NameText; 
    public TMP_Text LevelText;
    public TMP_Text MindText;
    public TMP_Text HeartText;
    public TMP_Text BodyText;
    public Image HpFill;
    public TMP_Text HpText;
    public TMP_Text momentumText;

    public void Bind(Battler battler, PerkService perkService = null)
    {
        // Bind Stats
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

        if (momentumText != null)
            momentumText.text = battler.Momentum.ToString();
    }
}
