using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Exibe tooltip com informações detalhadas do Perk ao passar o mouse
/// </summary>
public class PerkTooltip : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    /// <summary>
    /// Exibe os dados do perk no tooltip
    /// </summary>
    public void Show(PerkRuntimeInstance runtimeInstance)
    {
        string operationValueColor = runtimeInstance.Definition.Rule.Value > 0 ? "green" : "red";
        string conditionValue = runtimeInstance.Definition.Rule.ConditionValue != "" ? $"<color=white>{runtimeInstance.Definition.Rule.ConditionValue}</color>" : "";
        string conditionKey = runtimeInstance.Definition.Rule.ConditionKey != PerkConditionKey.Always ? $"when <color=purple>{runtimeInstance.Definition.Rule.ConditionKey}</color>" : "";

        if (runtimeInstance == null)
            return;
            
        if (iconImage != null && runtimeInstance.SourceTrick != null && runtimeInstance.SourceTrick.Definition.Icon != null)
            iconImage.sprite = runtimeInstance.SourceTrick.Definition.Icon;
        
        if (nameText != null)
            nameText.text = runtimeInstance.Definition.name;

        if (descriptionText != null)
            descriptionText.text = $"<color=blue>{runtimeInstance.Definition.Rule.Operation}</color> <color={operationValueColor}>{runtimeInstance.Definition.Rule.Value}</color> to <color=yellow>{runtimeInstance.Definition.Rule.ModifierTarget}</color> {conditionKey} {conditionValue}";
    }
}
