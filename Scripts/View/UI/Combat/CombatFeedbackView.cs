using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatFeedbackView
{
    private readonly MonoBehaviour runner;

    private readonly TMP_Text playerText;
    private readonly TMP_Text enemyText;
    private readonly Image playerIcon;
    private readonly Image enemyIcon;

    private readonly Color damageColor;
    private readonly Color actionColor;
    private readonly Color critColor;

    public CombatFeedbackView(
        MonoBehaviour runner,
        TMP_Text playerText,
        TMP_Text enemyText,
        Image playerIcon,
        Image enemyIcon,
        Color damageColor,
        Color actionColor,
        Color critColor)
    {
        this.runner = runner;
        this.playerText = playerText;
        this.enemyText = enemyText;
        this.playerIcon = playerIcon;
        this.enemyIcon = enemyIcon;
        this.damageColor = damageColor;
        this.actionColor = actionColor;
        this.critColor = critColor;
    }

    public void ShowDamage(bool player, int amount, bool crit)
    {
        var text = player ? playerText : enemyText;
        var color = crit ? critColor : damageColor;

        runner.StartCoroutine(ShowRoutine(text, $"-{amount}", color));
    }

    public void ShowAction(bool player, string value, Sprite icon)
    {
        var text = player ? playerText : enemyText;
        var img = player ? playerIcon : enemyIcon;

        runner.StartCoroutine(ShowRoutine(text, value, actionColor, img, icon));
    }

    private IEnumerator ShowRoutine(TMP_Text text, string value, Color color, Image icon = null, Sprite sprite = null)
    {
        text.gameObject.SetActive(true);
        text.text = value;
        text.color = color;

        if (icon != null)
        {
            icon.sprite = sprite;
            icon.gameObject.SetActive(sprite != null);
        }

        yield return new WaitForSeconds(0.7f);

        text.gameObject.SetActive(false);
        if (icon != null)
            icon.gameObject.SetActive(false);
    }
}