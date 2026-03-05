using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyRunContext
{
    public int tier;
    public float riskModifier = 1f;
    public bool forcedEncounter;
    public string levelTags;

    public float DifficultyModifier => Mathf.Max(0.1f, riskModifier) * (forcedEncounter ? 1.15f : 1f);

    public HashSet<string> BuildTagSet()
    {
        HashSet<string> tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(levelTags))
            return tags;

        string[] raw = levelTags.Split(',');
        foreach (string tag in raw)
        {
            string trimmed = tag.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
                tags.Add(trimmed);
        }

        return tags;
    }
}

[Serializable]
public class EnemyInstance
{
    public EnemySO source;
    public int life;
    public int physical;
    public int mental;
    public int runTier;
}

[CreateAssetMenu(menuName = "Game/Enemy/Normal")]
public class NormalEnemySO : EnemySO { }

[CreateAssetMenu(menuName = "Game/Enemy/Special")]
public class SpecialEnemySO : EnemySO { }

[CreateAssetMenu(menuName = "Game/Enemy/Boss")]
public class BossEnemySO : EnemySO { }
