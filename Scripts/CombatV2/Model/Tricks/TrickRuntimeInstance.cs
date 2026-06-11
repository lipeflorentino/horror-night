using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representa uma instância de Trick em tempo de jogo (runtime).
/// Armazena referência ao SO e estado atual (duração, perks ativos, etc).
/// </summary>
public class TrickRuntimeInstance
{
    public TrickSO Definition { get; private set; }
    public Battler Owner { get; private set; }
    public int RemainingTurns { get; set; }
    public float CastTime { get; private set; }
    
    /// <summary>
    /// Perks que foram ativados por este Trick
    /// </summary>
    public List<PerkRuntimeInstance> ActivePerks { get; set; }
    
    public TrickRuntimeInstance(TrickSO definition, Battler owner, int durationTurns)
    {
        Definition = definition;
        Owner = owner;
        RemainingTurns = durationTurns;
        CastTime = Time.time;
        ActivePerks = new List<PerkRuntimeInstance>();
    }
    
    /// <summary>
    /// Retorna true se o trick ainda está ativo
    /// </summary>
    public bool IsActive()
    {
        return RemainingTurns < 0 || RemainingTurns > 0;
    }
    
    /// <summary>
    /// Reduz a duração em 1 turno
    /// </summary>
    public void DecreaseDuration()
    {
        if (RemainingTurns > 0)
            RemainingTurns--;
    }
    
    /// <summary>
    /// Retorna o tempo decorrido desde o cast em segundos
    /// </summary>
    public float GetElapsedTime()
    {
        return Time.time - CastTime;
    }
}
