using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelNodeSO", menuName = "Game/Level Node Definition")]
public class LevelNodeSO : ScriptableObject
{

    [Header("Base Settings")]
    public NodeType nodeType;
    public NodeFlags flags;
}
