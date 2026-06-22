using System.Collections.Generic;
using UnityEngine;

public class ActiveTricksView : MonoBehaviour
{
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private TrickIconUI trickIconPrefab;
    [SerializeField] private Transform activeTricksRoot;
    private static readonly string[] ActiveInputKeys = { "Q", "W", "E", "R" };

    private readonly List<TrickIconUI> instantiatedIcons = new();

    public void Init(CombatManager combatManager)
    {
        this.combatManager = combatManager;

        if (this.combatManager != null && this.combatManager.PlayerTrickInventory != null)
        {
            this.combatManager.PlayerTrickInventory.OnChanged += Refresh;
            Refresh();
        }
    }

    private void OnDestroy()
    {
        if (combatManager != null && combatManager.PlayerTrickInventory != null)
        {
            combatManager.PlayerTrickInventory.OnChanged -= Refresh;
        }
    }

    public void Refresh()
    {
        if (combatManager == null || combatManager.PlayerTrickInventory == null || trickIconPrefab == null || activeTricksRoot == null)
            return;

        ClearIcons();

        IReadOnlyList<TrickSlot> castedSlots = combatManager.PlayerTrickInventory.CastedSlots;
        for (int i = 0; i < castedSlots.Count; i++)
        {
            TrickSlot slot = castedSlots[i];
            if (slot == null || slot.IsEmpty || slot.RuntimeInstance == null || slot.Definition == null || slot.Definition.ActivationMode == TrickActivationMode.Passive)
                continue;

            TrickIconUI iconUI = Instantiate(trickIconPrefab, activeTricksRoot);
            iconUI.Setup(slot.Definition, ActiveInputKeys[i], slot.RuntimeInstance);
            iconUI.OnReleaseClicked += HandleReleaseClicked;
            instantiatedIcons.Add(iconUI);
        }
    }

    private void HandleReleaseClicked(TrickRuntimeInstance instance)
    {
        if (combatManager != null)
        {
            combatManager.ExecuteManualTrickActivation(instance);
        }
    }

    private void ClearIcons()
    {
        for (int i = 0; i < instantiatedIcons.Count; i++)
        {
            if (instantiatedIcons[i] != null)
            {
                instantiatedIcons[i].OnReleaseClicked -= HandleReleaseClicked;
                Destroy(instantiatedIcons[i].gameObject);
            }
        }
        instantiatedIcons.Clear();
    }
}
