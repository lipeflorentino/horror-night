using System;
using System.Collections.Generic;

/// <summary>
/// Representa uma instância de Drawback em tempo de jogo (runtime).
/// Armazena referência ao SO e estado atual.
/// </summary>
public class DrawbackRuntimeInstance
{
    public string InstanceId { get; private set; }
    public DrawbackSO Definition { get; private set; }
    public Battler Owner { get; private set; }
    public Battler Source { get; private set; }
    public int RemainingTurns { get; set; }
    public List<PerkRuntimeInstance> ActivePerks { get; private set; }
    
    public DrawbackRuntimeInstance(DrawbackSO definition, Battler owner, int durationTurns, Battler source = null, string instanceId = null)
    {
        InstanceId = string.IsNullOrWhiteSpace(instanceId) ? Guid.NewGuid().ToString("N") : instanceId;
        Definition = definition;
        Owner = owner;
        Source = source;
        RemainingTurns = durationTurns;
        ActivePerks = new List<PerkRuntimeInstance>();
    }

    /// <summary>
    /// Retorna true se o drawback ainda está ativo.
    /// </summary>
    public bool IsActive()
    {
        return RemainingTurns < 0 || RemainingTurns > 0;
    }

    /// <summary>
    /// Reduz a duração em 1 turno.
    /// </summary>
    public void DecreaseDuration()
    {
        if (RemainingTurns > 0)
            RemainingTurns--;
    }
}
