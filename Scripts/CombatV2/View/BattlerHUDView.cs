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

    public void Bind(Battler battler)
    {
        // Buscar o valor 'Current' em vez do Teto/Base, para refletir 
        // a subtração imediata após a confirmação do painel de dados.
        currentMind = battler.GetCurrentStatValue(DiceStatType.Mind);
        currentHeart = battler.GetCurrentStatValue(DiceStatType.Heart);
        currentBody = battler.GetCurrentStatValue(DiceStatType.Body);

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
