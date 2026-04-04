

using UnityEngine;

[System.Serializable]
public struct ArchetypePoints
{
    [SerializeField] public int nt;
    [SerializeField] public int nf;
    [SerializeField] public int sj;
    [SerializeField] public int sp;

    public int Get(PlayerArchetype archetype)
    {
        return archetype switch
        {
            PlayerArchetype.NF => nf,
            PlayerArchetype.SJ => sj,
            PlayerArchetype.SP => sp,
            _ => nt
        };
    }

    public void Add(PlayerArchetype archetype, int amount)
    {
        switch (archetype)
        {
            case PlayerArchetype.NF:
                nf += amount;
                break;
            case PlayerArchetype.SJ:
                sj += amount;
                break;
            case PlayerArchetype.SP:
                sp += amount;
                break;
            default:
                nt += amount;
                break;
        }
    }
}