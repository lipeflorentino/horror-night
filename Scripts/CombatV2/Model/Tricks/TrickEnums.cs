using System;
using UnityEngine;

/// <summary>
/// Define quando um Trick é aplicado ao battler
/// </summary>
public enum TrickTiming
{
    Instant,        // Aplicado imediatamente no mesmo turno
    NextTurn        // Aplicado no próximo turno
}

/// <summary>
/// Define a raridade/qualidade de um Trick (para UI e balanceamento)
/// </summary>
public enum TrickRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

/// <summary>
/// Define os grupos de slots suportados pelo TrickInventory.
/// </summary>
public enum TrickSlotType
{
    Identity,
    Casted
}
