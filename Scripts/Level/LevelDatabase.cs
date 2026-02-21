using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "Game/Level Database")]
public class LevelDatabase : ScriptableObject
{
    public List<LevelDefinition> Levels;
}