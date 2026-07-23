using System.Collections.Generic;

public class Battler
{
    public string Name;
    public int Level;
    public int HP, MaxHp;
    public int Heart;
    public int Mind;
    public int Body;
    public int Momentum;
    public int MaxMomentum = 6;
    public int BaseHeart;
    public int BaseMind;
    public int BaseBody;
    public int Attack;
    public int Defense;
    public int Initiative;
    public int Focus;
    public int Strength;
    public int Agility;

    public int CurrentDices;
    public int MaxDices;
    public bool IsPlayer;
    public List<BattlerStateRuntimeInstance> ActiveStates = new();
    public List<PerkRuntimeInstance> Perks = new();
    public List<TrickRuntimeInstance> Tricks = new();
    public List<DrawbackRuntimeInstance> Drawbacks = new();

    public Battler(string name, int level, int hp, int heart, int mind, int body, int attack, int defense, int initiative, int maxDices, bool isPlayer, int maxHp = -1, int focus = 0, int strength = 0, int agility = 0, int baseHeart = -1, int baseBody = -1, int baseMind = -1)
    {
        Name = name;
        Level = level;
        HP = hp;
        Heart = heart;
        Mind = mind;
        Body = body;
        Momentum = 0;
        BaseHeart = baseHeart >= 0 ? baseHeart : heart;
        BaseBody = baseBody >= 0 ? baseBody : body;
        BaseMind = baseMind >= 0 ? baseMind : mind;
        Attack = attack;
        Defense = defense;
        Initiative = initiative;
        Focus = focus;
        Strength = strength;
        Agility = agility;
        MaxDices = maxDices;
        CurrentDices = maxDices;
        MaxHp = maxHp > 0 ? maxHp : HP;
        IsPlayer = isPlayer;
    }

    public int GetCurrentStatValue(DiceStatType statType)
    {
        return statType switch
        {
            DiceStatType.Mind => Mind,
            DiceStatType.Heart => Heart,
            DiceStatType.Body => Body,
            _ => 0
        };
    }

    public int GetBaseStatValue(DiceStatType statType)
    {
        return statType switch
        {
            DiceStatType.Mind => BaseMind,
            DiceStatType.Heart => BaseHeart,
            DiceStatType.Body => BaseBody,
            _ => 0
        };
    }

    public void ReceiveDamage(int damage)
    {
        HP -= damage;
        if (HP < 0) HP = 0;
    }

    public void RecoverDices(int amount)
    {
        CurrentDices += amount;
        if (CurrentDices > MaxDices)
            CurrentDices = MaxDices;
    }

    public int CurrentActionDices => System.Math.Max(0, CurrentDices);

    public void SpendActionDices(int amount)
    {
        CurrentDices = System.Math.Max(0, CurrentDices - amount);
    }

    public void AddMomentum(int amount)
    {
        if (amount <= 0)
            return;

        Momentum += amount;
        if (Momentum > MaxMomentum)
            Momentum = MaxMomentum;
    }

    public bool SpendMomentum(int amount)
    {
        if (amount <= 0 || Momentum < amount)
            return false;

        Momentum -= amount;
        return true;
    }

    public void ResetMomentum()
    {
        Momentum = 0;
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
            if (perk == null || !perk.IsActive())
                continue;

            if (perk.SourceTrick == null || perk.SourceTrick.IsActive())
            {
                if (added.Add(perk))
                    perks.Add(perk);
            }
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

    /// <summary>
    /// Retorna todos os drawbacks ativos
    /// </summary>
    public List<DrawbackRuntimeInstance> GetActiveDrawbacks()
    {
        return Drawbacks.FindAll(d => d != null && d.IsActive());
    }
}
