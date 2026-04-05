using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class HudView : MonoBehaviour
{
    [SerializeField] private TMP_Text diceText;

    public void UpdateDice(int value)
    {
        if (diceText == null)
            return;

        diceText.text = value.ToString();
    }
}
