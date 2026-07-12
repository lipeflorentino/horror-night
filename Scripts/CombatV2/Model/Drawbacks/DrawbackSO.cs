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
    public int DurationMin = -1;
    public int DurationMax = -1;

    /// <summary>
    /// Calcula uma duração aleatória entre DurationMin e DurationMax (inclusive).
    /// Se não configurado, retorna a duração padrão DurationTurns.
    /// </summary>
    public int RollDuration()
    {
        if (DurationMin < 0 || DurationMax < 0)
            return DurationTurns;

        if (DurationMin == DurationMax)
            return DurationMin;

        return Random.Range(DurationMin, DurationMax + 1);
    }

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
