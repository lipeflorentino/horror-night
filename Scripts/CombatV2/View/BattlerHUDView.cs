using UnityEngine;
using TMPro;

public class BattlerHUDView : MonoBehaviour
{
    private int currentMind, currentBody, currentHeart;

    [Header("HUD Text")]
    public TMP_Text NameText; 
    public TMP_Text LevelText;
    public StatHudBinding MindHud;
    public StatHudBinding HeartHud;
    public StatHudBinding BodyHud;
    public StatHudBinding HpHud;
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

        MindHud?.SetValue(currentMind, 0);

        HeartHud?.SetValue(currentHeart, 0);

        BodyHud?.SetValue(currentBody, 0);

        HpHud?.SetValue(battler.HP, battler.MaxHp);

        if (momentumText != null)
            momentumText.text = battler.Momentum.ToString();
    }
}
