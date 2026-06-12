using System;
using System.Collections.Generic;
public class Battler
{
    public string Name;
    public int Level;
    public int HP, MaxHp;
    public int Heart;
    public int Mind;
    public int Body;
    public int Attack;
    public int Defense;
    public int Initiative;
    public int Focus;
    public int Strength;
    public int Agility;

    public int CurrentPowerDices, CurrentAccuracyDices;
    public int MaxPowerDices, MaxAccuracyDices;
    public bool IsPlayer;
    public List<BattlerStateInstance> States = new();
    public List<PerkRuntimeInstance> Perks = new();
    public List<TrickRuntimeInstance> Tricks = new();

    public Battler(string name, int level, int hp, int heart, int mind, int body, int attack, int defense, int initiative, int powerDices, int accuracyDices, bool isPlayer, int maxHp = -1, int focus = 0, int strength = 0, int agility = 0)
    {
        Name = name;
        Level = level;
        HP = hp;
        Heart = heart;
        Mind = mind;
        Body = body;
        Attack = attack;
        Defense = defense;
        Initiative = initiative;
        Focus = focus;
        Strength = strength;
        Agility = agility;
        CurrentPowerDices = powerDices;
        MaxPowerDices = powerDices;
        CurrentAccuracyDices = accuracyDices;
        MaxAccuracyDices = accuracyDices;
        MaxHp = maxHp > 0 ? maxHp : HP;
        IsPlayer = isPlayer;
    }

    public void ReceiveDamage(int damage)
    {
        HP -= damage;
        if (HP < 0) HP = 0;
    }

    public void RecoverDices(int amount)
    {
        CurrentPowerDices += amount;
        CurrentAccuracyDices += amount;

        if (CurrentPowerDices > MaxPowerDices)
            CurrentPowerDices = MaxPowerDices;

        if (CurrentAccuracyDices > MaxAccuracyDices)
            CurrentAccuracyDices = MaxAccuracyDices;
    }

    public bool IsAlive()
    {
        return HP > 0;
    }

    public int GetBattlerActionPower(bool isAttacker)
    {
        int atk = Attack;
        int df = Defense;
        return isAttacker ? atk : df;
    }

    public void ApplyState(BattlerStateDefinition definition, Battler source = null, int durationTurns = -1, int stacks = 1)
    {
        new BattlerStateService().ApplyState(this, definition, source, durationTurns, stacks);
    }

    public void RemoveState(string stateId)
    {
        new BattlerStateService().RemoveState(this, stateId);
    }

    public bool HasState(string stateId)
    {
        return new BattlerStateService().HasState(this, stateId);
    }

    /// <summary>
    /// Retorna todos os Perks efetivos (diretos + de Tricks)
    /// </summary>
    public List<PerkRuntimeInstance> GetEffectivePerks()
    {
        List<PerkRuntimeInstance> perks = new();
        HashSet<PerkRuntimeInstance> added = new();

        for (int i = 0; i < Perks.Count; i++)
        {
            PerkRuntimeInstance perk = Perks[i];
            if (perk?.SourceTrick != null && perk.SourceTrick.IsActive() && added.Add(perk))
                perks.Add(perk);
        }

        // Perks de Tricks
        for (int i = 0; i < Tricks.Count; i++)
        {
            if (Tricks[i]?.ActivePerks == null || !Tricks[i].IsActive())
                continue;

            for (int j = 0; j < Tricks[i].ActivePerks.Count; j++)
            {
                PerkRuntimeInstance perk = Tricks[i].ActivePerks[j];
                if (perk != null && added.Add(perk))
                    perks.Add(perk);
            }
        }

        return perks;
    }

    /// <summary>
    /// Verifica se o battler tem um trick ativo
    /// </summary>
    public bool HasTrick(string trickId)
    {
        return Tricks.Find(t => t != null && t.Definition != null && t.Definition.Id == trickId) != null;
    }

    /// <summary>
    /// Retorna um trick pelo ID
    /// </summary>
    public TrickRuntimeInstance GetTrick(string trickId)
    {
        return Tricks.Find(t => t != null && t.Definition != null && t.Definition.Id == trickId);
    }

    /// <summary>
    /// Retorna todos os tricks ativos (ainda com duração > 0 ou permanentes)
    /// </summary>
    public List<TrickRuntimeInstance> GetActiveTricks()
    {
        return Tricks.FindAll(t => t != null && t.IsActive());
    }
}
