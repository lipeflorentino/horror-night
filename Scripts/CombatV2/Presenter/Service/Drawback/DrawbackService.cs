using System;

public class DrawbackService
{
    private readonly DrawbackDatabase database;
    private readonly PerkService perkService;

    public event Action<Battler, DrawbackRuntimeInstance> OnDrawbackApplied;
    public event Action<Battler, DrawbackRuntimeInstance> OnDrawbackRemoved;
    public event Action<Battler, DrawbackRuntimeInstance> OnDrawbackExpired;

    public DrawbackService(PerkService perkService)
    {
        database = DrawbackDatabase.GetOrCreateRuntimeDatabase();
        database.EnsureLoaded();
        this.perkService = perkService;
    }

    public DrawbackRuntimeInstance ApplyDrawback(Battler target, string drawbackId, Battler source = null, int durationTurns = -1)
    {
        if (target == null || string.IsNullOrWhiteSpace(drawbackId)) return null;

        DrawbackSO definition = database.GetById(drawbackId);
        if (definition == null) return null;

        DrawbackRuntimeInstance existing = target.Drawbacks.Find(d => d != null && d.Definition != null &&
            d.Definition.Id.Equals(drawbackId, StringComparison.OrdinalIgnoreCase));
        
        if (existing != null) return existing;

        int resolvedDuration = durationTurns >= 0 ? durationTurns : definition.DurationTurns;
        DrawbackRuntimeInstance drawbackInstance = new(definition, target, resolvedDuration, source);
        target.Drawbacks.Add(drawbackInstance);
        
        ApplyDrawbackPerks(target, source, drawbackInstance, resolvedDuration);
        OnDrawbackApplied?.Invoke(target, drawbackInstance);

        return drawbackInstance;
    }

    public void RemoveDrawback(Battler target, string drawbackId)
    {
        if (target == null || string.IsNullOrWhiteSpace(drawbackId)) return;

        for (int i = target.Drawbacks.Count - 1; i >= 0; i--)
        {
            var drawback = target.Drawbacks[i];
            if (drawback?.Definition == null || !drawback.Definition.Id.Equals(drawbackId, StringComparison.OrdinalIgnoreCase)) continue;

            RemoveDrawbackPerks(target, drawback);
            target.Drawbacks.RemoveAt(i);
            OnDrawbackRemoved?.Invoke(target, drawback);
        }
    }

    public void TickTurnEnd(Battler battler)
    {
        if (battler == null || battler.Drawbacks.Count == 0) return;

        for (int i = battler.Drawbacks.Count - 1; i >= 0; i--)
        {
            DrawbackRuntimeInstance drawback = battler.Drawbacks[i];
            if (drawback == null || drawback.Definition == null)
            {
                battler.Drawbacks.RemoveAt(i);
                continue;
            }

            if (drawback.RemainingTurns < 0) continue;

            if (drawback.IsNew)
            {
                drawback.IsNew = false;
                continue;
            }

            drawback.DecreaseDuration();
            
            if (drawback.RemainingTurns == 0)
            {
                RemoveDrawbackPerks(battler, drawback);
                battler.Drawbacks.RemoveAt(i);
                OnDrawbackExpired?.Invoke(battler, drawback);
            }
        }
    }

    private void ApplyDrawbackPerks(Battler target, Battler source, DrawbackRuntimeInstance drawbackInstance, int durationTurns)
    {
        if (perkService == null || drawbackInstance.Definition.PerkIds == null) return;

        for (int i = 0; i < drawbackInstance.Definition.PerkIds.Count; i++)
        {
            PerkRuntimeInstance appliedPerk = perkService.ApplyPerk(target, drawbackInstance.Definition.PerkIds[i], source, -1);
            if (appliedPerk != null)
            {
                appliedPerk.SetSourceDrawback(drawbackInstance);
                if (!drawbackInstance.ActivePerks.Contains(appliedPerk))
                {
                    drawbackInstance.ActivePerks.Add(appliedPerk);
                }
            }
        }
    }

    private void RemoveDrawbackPerks(Battler target, DrawbackRuntimeInstance drawbackInstance)
    {
        if (perkService == null || drawbackInstance?.ActivePerks == null) return;

        for (int i = drawbackInstance.ActivePerks.Count - 1; i >= 0; i--)
        {
            perkService.RemovePerkInstance(target, drawbackInstance.ActivePerks[i]);
        }
        drawbackInstance.ActivePerks.Clear();
    }
}