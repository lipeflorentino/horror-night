using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Responsável exclusivo por aplicar mudanças de estado persistentes (Focus, Strength, TrickCharges, etc.) em entidades do jogo quando um perk é engatilhado.
/// </summary>
public class PerkStateApplicator
{
    private readonly Dictionary<PerkModifierTarget, Action<Battler, float, PerkOperation, int, ICombatContext>> statMutators;
    private readonly Dictionary<PerkModifierTarget, Action<TrickRuntimeInstance, float, PerkOperation, int, ICombatContext>> trickMutators;

    public PerkStateApplicator()
    {
        // TODO: verificar o uso de statMutators, se faz sentido aplicar modificações de perks diretamente nos stats do Battler.
        statMutators = new Dictionary<PerkModifierTarget, Action<Battler, float, PerkOperation, int, ICombatContext>>()
        {
            { PerkModifierTarget.Focus, (b, val, op, st, ctx) => b.Focus = Mathf.RoundToInt(PerkRuntimeHelper.ApplyModifier(b.Focus, op, val, st, ctx)) },
            { PerkModifierTarget.Strength, (b, val, op, st, ctx) => b.Strength = Mathf.RoundToInt(PerkRuntimeHelper.ApplyModifier(b.Strength, op, val, st, ctx)) },
            // Exemplo de como escalar no futuro:
            // { PerkModifierTarget.Mind, (b, val, op, st, ctx) => b.Mind = Mathf.RoundToInt(PerkRuntimeHelper.ApplyModifier(b.Mind, op, val, st, ctx)) },
        };

        trickMutators = new Dictionary<PerkModifierTarget, Action<TrickRuntimeInstance, float, PerkOperation, int, ICombatContext>>()
        {
            { PerkModifierTarget.TrickCharges, (t, val, op, st, ctx) => t.AddCharges(Mathf.RoundToInt(PerkRuntimeHelper.ApplyModifier(t.CurrentCharges, op, val, st, ctx))) },
        };
    }

    public void HandlePerkTriggered(PerkTriggeredEvent evt)
    {
        if (evt.Owner == null) return;
        
        int stacks = Mathf.Max(1, evt.StacksApplied);

        if (statMutators.TryGetValue(evt.ModifierTarget, out var mutator))
        {
            mutator(evt.Owner, evt.AppliedValue, evt.Operation, stacks, evt.FullContext);
        }
        else if (trickMutators.TryGetValue(evt.ModifierTarget, out var trickMutator))
        {
            if (evt.SourceTrick != null)
            {
                trickMutator(evt.SourceTrick, evt.AppliedValue, evt.Operation, stacks, evt.FullContext);
                evt.SourceTrick.MarkTriggered();
            }
        }
    }
}