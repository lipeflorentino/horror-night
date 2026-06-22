using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Define um Drawback (Desvantagem) que pode ser aplicado aos combatentes.
/// Drawbacks ativam uma série de Perks negativos.
/// </summary>
[CreateAssetMenu(fileName = "Drawback", menuName = "Combat/Drawback")]
public class DrawbackSO : ScriptableObject
{
    [Header("Identificação")]
    public string Id;
    public string DisplayName;
    [TextArea(2, 4)]
    public string Description;
    public Sprite Icon;

    [Header("Configuração")]
    [Tooltip("-1 = Permanente, 0+ = Número de turnos")]
    public int DurationTurns = -1;

    [Header("Efeitos")]
    [Tooltip("IDs dos Perks que este Drawback ativa. Devem ser encontrados em PerkDatabase.")]
    public List<string> PerkIds = new();

    [Header("Metadados")]
    [TextArea(1, 2)]
    public string FlavorText;

    /// <summary>
    /// Valida se o Drawback tem dados válidos
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Id) &&
               !string.IsNullOrEmpty(DisplayName) &&
               PerkIds.Count > 0;
    }
}
