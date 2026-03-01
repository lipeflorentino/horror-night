using UnityEngine;

public class EventSystem : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float basePositiveChance = 0.5f;
    [SerializeField] private int positiveTensionReduction = 1;
    [SerializeField] private int negativeTensionIncrease = 1;

    public void TriggerEvent(LevelNode node, LevelSO level)
    {
        if (node == null)
            return;

        float chance = Mathf.Clamp01(basePositiveChance + (level != null ? level.Event_Weight_Modifier * 0.05f : 0f));
        bool isPositive = Random.value <= chance;

        if (isPositive)
        {
            if (TensionSystem.Instance != null)
                TensionSystem.Instance.CurrentTension = Mathf.Max(0, TensionSystem.Instance.CurrentTension - positiveTensionReduction);

            Debug.Log($"[EventSystem] Positive event triggered on node {node.index}.");
            return;
        }

        if (TensionSystem.Instance != null)
            TensionSystem.Instance.CurrentTension += Mathf.Max(0, negativeTensionIncrease);

        Debug.Log($"[EventSystem] Negative event triggered on node {node.index}.");
    }
}
