using System;
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

    public int CurrentDices;
    public int MaxDices;

    public Battler(string name, int level, int hp, int heart, int mind, int body, int attack, int defense, int initiative, int dices)
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
        CurrentDices = dices;
        MaxDices = dices;
        MaxHp = HP;
    }

    public void ReceiveDamage(int damage)
    {
        HP -= damage;
        if (HP < 0) HP = 0;
    }

    public void RecoverDice(int amount)
    {
        CurrentDices += amount;
        if (CurrentDices > MaxDices)
            CurrentDices = MaxDices;
    }

    public bool IsAlive()
    {
        return HP > 0;
    }
}
