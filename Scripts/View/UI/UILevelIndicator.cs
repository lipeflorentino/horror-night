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
    [SerializeField] private GameObject[] slots;
    [SerializeField] private SlotRow[] slotRows;

    [Header("Visuals")]
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color currentColor = Color.white;
    [SerializeField] private Color portalColor = Color.magenta;

    private const int windowSize = 5;

    private void OnEnable()
    {
        if (levelController == null)
            levelController = FindObjectOfType<LevelController>();

        slotRows = new SlotRow[slots.Length];

        for (int i = 0; i < slots.Length; i++)
        {
            GameObject slot = slots[i];

            Image fill = slot.transform.Find("Fill").GetComponent<Image>();
            Image highlight = slot.transform.Find("Highlight").GetComponent<Image>();

            slotRows[i] = new SlotRow
            {
                row = new Image[] { fill, highlight }
            };
        }

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
            int blockStart = currentIndex / windowSize * windowSize;
            int realIndex = blockStart + i;
            
            if (realIndex >= levelController.nodes.Length)
            {
                slotRows[i].row[0].gameObject.SetActive(false);
                continue;
            }

            slotRows[i].row[0].gameObject.SetActive(true);

            LevelNode node = levelController.nodes[realIndex];

            if (realIndex == currentIndex)
            {
                slotRows[i].row[0].color = currentColor;
                slotRows[i].row[1].gameObject.SetActive(true);
            }
            else if (node.definition.nodeType == NodeType.Portal)
            {
                slotRows[i].row[0].color = portalColor;
                slotRows[i].row[1].gameObject.SetActive(false);
            }
            else
            {
                slotRows[i].row[0].color = normalColor;
                slotRows[i].row[1].gameObject.SetActive(false);
            }
        }
    }
}