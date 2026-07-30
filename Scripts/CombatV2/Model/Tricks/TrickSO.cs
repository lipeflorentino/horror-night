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
/// - TimingTurns: quantos turnos após o cast o trick leva para ser aplicado
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
    public Sprite Thumbnail;

    [Header("Requisitos")]
    public int Level = 1;
    public TrickRequirements Requirements = new();

    [Header("Custo de Casting")]
    public int MomentumCost = 0;

    [Header("Timing e Duração")]
    [Min(0)]
    public int TimingTurns = 0;
    [Tooltip("-1 = Permanente (identity trick), 0+ = Número de turnos")]
    public int DurationTurns = -1;
    [Tooltip("Quantidade de turnos até que o trick possa ser castado novamente.")]
    public int CooldownTurns = 0;

    [Header("Acionamento")]
    public TrickActivationMode ActivationMode = TrickActivationMode.Active;

    public bool IsPassive => ActivationMode == TrickActivationMode.Passive;
    public bool IsActive => ActivationMode == TrickActivationMode.Active || ActivationMode == TrickActivationMode.ActiveCharge;

    [Header("Efeitos")]
    [Tooltip("IDs dos Perks que este Trick ativa. Devem ser encontrados em PerkDatabase.")]
    public List<string> PerkIds = new();

    [Tooltip("IDs dos Drawbacks associados à ativação manual (ex: exhaustion).")]
    public List<string> DrawbackIds = new();

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
    /// Retorna o custo total em momentum
    /// </summary>
    public int GetTotalCost()
    {
        return MomentumCost;
    }
    
    /// <summary>
    /// Verifica se o battler pode fazer cast deste trick
    /// </summary>
    public bool CanCast(Battler battler, PerkService perkService = null)
    {
        if (battler == null)
            return false;
        
        if (battler.Level < Level)
            return false;

        if (Requirements != null && !Requirements.IsSatisfiedBy(battler, perkService))
            return false;

        if (battler.Momentum < MomentumCost)
            return false;

        return true;
    }
    
    /// <summary>
    /// Retorna descrição formatada para UI
    /// </summary>
    public string GetFormattedDescription()
    {
        return $"{Description}\n\nLevel: {Level} | Requisitos: {Requirements?.ToDisplayString() ?? "Nenhum"} | Momentum: {MomentumCost} | Cooldown: {CooldownTurns}";
    }
}
