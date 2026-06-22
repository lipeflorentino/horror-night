using System;
using UnityEngine;

/// <summary>
/// Define o modo de acionamento de um Trick.
/// </summary>
public enum TrickActivationMode
{
    Passive,
    Active,
    ActiveCharge
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
