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

    public void ApplyPerk(PerkDefinition definition, Battler source = null, int durationTurns = -1, int stacks = 1)
    {
        new PerkService().ApplyPerk(this, definition, source, durationTurns, stacks);
    }

    public void ApplyPerk(string perkId, Battler source = null, int durationTurns = -1, int stacks = 1)
    {
        new PerkService().ApplyPerk(this, perkId, source, durationTurns, stacks);
    }
}
