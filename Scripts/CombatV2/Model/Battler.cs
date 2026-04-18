using System;
public class Battler
{
    public string Name;
    public int HP;

    public int Heart;
    public int Mind;
    public int Body;

    public int CurrentDices;
    public int MaxDices;

    public Battler(string name, int hp, int heart, int mind, int body, int dices)
    {
        Name = name;
        HP = hp;
        Heart = heart;
        Mind = mind;
        Body = body;
        CurrentDices = dices;
        MaxDices = dices;
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