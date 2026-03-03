using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class SlotRow
{
    public Image[] row;
}

public class UILevelIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelController levelController;

    [SerializeField] private SlotRow[] slots; // Precisa de 5 imagens

    [Header("Visuals")]
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color currentColor = Color.white;
    [SerializeField] private Color portalColor = Color.magenta;

    private const int windowSize = 5;
    private const int centerIndex = 0; // posição central da janela visual

    private void OnEnable()
    {
        if (levelController == null)
            levelController = FindObjectOfType<LevelController>();

        levelController.OnNodeChanged += Refresh;
        Refresh(levelController.CurrentIndex);
    }

    private void OnDestroy()
    {
        if (levelController != null)
            levelController.OnNodeChanged -= Refresh;
    }

    private void Refresh(int currentIndex)
    {
        if (slots == null || slots.Length < windowSize)
        {
            Debug.LogError("Slots não configurados corretamente!");
            return;
        }
        
        if (levelController == null || levelController.nodes == null)
        {
            Debug.LogWarning("LevelController ou nodes ainda não inicializado.");
            return;
        }
        
        for (int i = 0; i < windowSize; i++)
        {
            int offset = i - centerIndex;
            int realIndex = currentIndex + offset;

            if (realIndex < 0 || realIndex >= levelController.nodes.Length)
            {
                slots[i].row[0].gameObject.SetActive(false);
                slots[i].row[1].gameObject.SetActive(false);
                continue;
            }

            slots[i].row[0].gameObject.SetActive(true);

            LevelNode node = levelController.nodes[realIndex];

            if (realIndex == currentIndex)
            {
                slots[i].row[0].color = currentColor;
                slots[i].row[1].gameObject.SetActive(true);
            }
            else if (node.definition.nodeType == NodeType.Portal)
            {
                slots[i].row[0].color = portalColor;
                slots[i].row[1].gameObject.SetActive(false);
            }
            else
            {
                slots[i].row[0].color = normalColor;
                slots[i].row[1].gameObject.SetActive(false);
            }
        }
    }
}