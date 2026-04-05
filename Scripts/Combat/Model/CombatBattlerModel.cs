using UnityEngine;

public class CombatBattlerModel
{
    public int heart;
    public int body;
    public int mind;

    public int maxHeart;
    public int maxBody;
    public int maxMind;

    public int hp;
    public int maxHp;

    public int attack;
    public int defense;
    public int initiative;

    public int TakeDamage(int amount)
    {
        ClampAllValues();

        var incomingDamage = Mathf.Max(0, amount);
        var actualDamage = Mathf.Min(incomingDamage, hp);
        hp -= actualDamage;

        return actualDamage;
    }

    public void RecoverResources(int amountPerStat)
    {
        ClampAllValues();

        var recovery = Mathf.Max(0, amountPerStat);
        heart = Mathf.Clamp(heart + recovery, 0, maxHeart);
        body = Mathf.Clamp(body + recovery, 0, maxBody);
        mind = Mathf.Clamp(mind + recovery, 0, maxMind);
    }

    public bool SpendResources(int heartCost, int bodyCost, int mindCost)
    {
        ClampAllValues();

        var finalHeartCost = Mathf.Max(0, heartCost);
        var finalBodyCost = Mathf.Max(0, bodyCost);
        var finalMindCost = Mathf.Max(0, mindCost);

        if (heart < finalHeartCost || body < finalBodyCost || mind < finalMindCost)
        {
            return false;
        }

        heart = Mathf.Clamp(heart - finalHeartCost, 0, maxHeart);
        body = Mathf.Clamp(body - finalBodyCost, 0, maxBody);
        mind = Mathf.Clamp(mind - finalMindCost, 0, maxMind);

        return true;
    }

    public bool IsDead()
    {
        ClampAllValues();
        return hp == 0;
    }

    private void ClampAllValues()
    {
        maxHeart = Mathf.Max(0, maxHeart);
        maxBody = Mathf.Max(0, maxBody);
        maxMind = Mathf.Max(0, maxMind);
        maxHp = Mathf.Max(0, maxHp);

        heart = Mathf.Clamp(heart, 0, maxHeart);
        body = Mathf.Clamp(body, 0, maxBody);
        mind = Mathf.Clamp(mind, 0, maxMind);
        hp = Mathf.Clamp(hp, 0, maxHp);

        attack = Mathf.Max(0, attack);
        defense = Mathf.Max(0, defense);
        initiative = Mathf.Max(0, initiative);
    }
}
