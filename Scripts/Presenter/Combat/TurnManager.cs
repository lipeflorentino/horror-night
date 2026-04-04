using System;
using UnityEngine;

public class CombatBattlerRuntime
{
    public string displayName;
    public bool isPlayer;

    public int hp;
    public int maxHp;

    public int heart;
    public int maxHeart;
    public int body;
    public int maxBody;
    public int mind;
    public int maxMind;

    public int attack;
    public int defense;
    public int initiative;

    public int knownInfoLevel;

    public bool IsDead => hp <= 0;

    public void RecoverResources(int amount)
    {
        if (amount <= 0)
            return;

        heart = Mathf.Min(maxHeart, heart + amount);
        body = Mathf.Min(maxBody, body + amount);
        mind = Mathf.Min(maxMind, mind + amount);
    }

    public bool SpendCost(int heartCost, int bodyCost, int mindCost)
    {
        if (heart < heartCost || body < bodyCost || mind < mindCost)
            return false;

        heart -= heartCost;
        body -= bodyCost;
        mind -= mindCost;
        return true;
    }

    public int TakeDamage(int incoming)
    {
        int finalDamage = Mathf.Max(1, incoming - defense);
        hp = Mathf.Max(0, hp - finalDamage);
        return finalDamage;
    }
}

public class TurnManager
{
    public const int DicePerTurn = 3;

    public int RemainingDice { get; private set; }

    public event Action<int> OnDiceChanged;

    public void StartTurn()
    {
        RemainingDice = DicePerTurn;
        OnDiceChanged?.Invoke(RemainingDice);
    }

    public bool TrySpendDice(int amount)
    {
        if (amount <= 0 || amount > RemainingDice)
            return false;

        RemainingDice -= amount;
        OnDiceChanged?.Invoke(RemainingDice);
        return true;
    }

    public bool HasDice => RemainingDice > 0;
}
