using TMPro;
using UnityEngine;

public class CombatInfoPanelView : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text playerInfoText;
    [SerializeField] private TMP_Text enemyInfoText;

    public void Init()
    {
        SetVisible(false);
    }

    public void Bind(Battler player, Battler enemy)
    {
        if (playerInfoText != null)
            playerInfoText.text = BuildCombatInfo(player);

        if (enemyInfoText != null)
            enemyInfoText.text = BuildCombatInfo(enemy);
    }

    public void SetVisible(bool isVisible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(isVisible);
        else
            gameObject.SetActive(isVisible);
    }

    private string BuildCombatInfo(Battler battler)
    {
        if (battler == null)
            return "-";

        return $"{battler.Name}\nATK: {battler.Attack} | DEF: {battler.Defense}\nMIND: {battler.Mind} | HEART: {battler.Heart}\nBODY: {battler.Body} | INIT: {battler.Initiative}\nLVL: {battler.Level}";
    }
}