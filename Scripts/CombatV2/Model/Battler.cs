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

    public int CurrentPowerDices, CurrentAccuracyDices;
    public int MaxPowerDices, MaxAccuracyDices;

    public Battler(string name, int level, int hp, int heart, int mind, int body, int attack, int defense, int initiative, int powerDices, int accuracyDices)
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
        CurrentPowerDices = powerDices;
        MaxPowerDices = powerDices;
        CurrentAccuracyDices = accuracyDices;
        MaxAccuracyDices = accuracyDices;
        MaxHp = HP;
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
}
