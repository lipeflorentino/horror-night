using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Define um Trick (Card/Ability) que pode ser castado durante combate.
/// Um Trick é um container de múltiplos Perks atômicos.
/// 
/// Responsabilidades:
/// - Metadados: Nome, Descrição, Ícone, Rarity
/// - Requisitos: Level, Custo em stats (Mind/Body/Heart)
/// - Efeitos: Lista de Perks que ativa
/// - Duração: Por quantos turnos o trick permanece ativo
/// - Timing: Instant ou NextTurn
/// </summary>
[CreateAssetMenu(fileName = "Trick", menuName = "Combat/Trick")]
public class TrickSO : ScriptableObject
{
    [Header("Identificação")]
    public string Id;
    public string DisplayName;
    [TextArea(2, 4)]
    public string Description;
    public Sprite Icon;

    [Header("Requisitos")]
    public int Level = 1;

    [Header("Custo de Casting (Consumido imediatamente)")]
    public int MindCost = 0;
    public int BodyCost = 0;
    public int HeartCost = 0;

    [Header("Timing e Duração")]
    public TrickTiming Timing = TrickTiming.Instant;
    [Tooltip("-1 = Permanente (identity trick), 0+ = Número de turnos")]
    public int DurationTurns = -1;

    [Header("Efeitos")]
    [Tooltip("IDs dos Perks que este Trick ativa. Devem ser encontrados em PerkDatabase.")]
    public List<string> PerkIds = new();

    [Header("Metadados")]
    public TrickRarity Rarity = TrickRarity.Common;
    [Tooltip("Tags para filtro e categorização (ex: power, defense, attack, passive)")]
    public List<string> Tags = new();
    [TextArea(1, 2)]
    public string FlavorText;
    
    /// <summary>
    /// Valida se o Trick tem dados válidos
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Id) &&
               !string.IsNullOrEmpty(DisplayName) &&
               PerkIds.Count > 0;
    }
    
    /// <summary>
    /// Retorna o custo total em todas as stats
    /// </summary>
    public int GetTotalCost()
    {
        return MindCost + BodyCost + HeartCost;
    }
    
    /// <summary>
    /// Verifica se o battler pode fazer cast deste trick
    /// </summary>
    public bool CanCast(Battler battler)
    {
        if (battler == null)
            return false;
        
        if (battler.Level < Level)
            return false;
        
        if (battler.Mind < MindCost || battler.Body < BodyCost || battler.Heart < HeartCost)
            return false;
        
        return true;
    }
    
    /// <summary>
    /// Retorna descrição formatada para UI
    /// </summary>
    public string GetFormattedDescription()
    {
        return $"{Description}\n\nLevel: {Level} | Custo: Mind {MindCost}, Body {BodyCost}, Heart {HeartCost}";
    }
}
