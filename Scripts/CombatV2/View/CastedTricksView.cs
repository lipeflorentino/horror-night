using System.Collections.Generic;
using UnityEngine;

public class CastedTricksView : MonoBehaviour
{
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private ActiveTrickUI activeTrickPrefab;
    [SerializeField] private PassiveTrickUI passiveTrickPrefab;
    [SerializeField] private Transform activeTricksRoot;
    [SerializeField] private Transform passiveTricksRoot;
    [SerializeField] private GameObject activeTricksLabel;
    [SerializeField] private GameObject passiveTricksLabel;
    private static readonly string[] ActiveInputKeys = { "Q", "W", "E", "R" };

    private readonly List<ActiveTrickUI> instantiatedActiveIcons = new();
    private readonly List<PassiveTrickUI> instantiatedPassiveIcons = new();

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

    private void HandleReleaseClicked(TrickRuntimeInstance instance)
    {
        if (combatManager != null)
        {
            combatManager.ExecuteManualTrickActivation(instance);
        }
    }

    public void Refresh()
    {
        if (combatManager == null || combatManager.PlayerTrickInventory == null)
            return;

        RefreshActiveTricks();
        RefreshPassiveTricks();
    }

    public void RefreshActiveTricks()
    {
        ClearActiveTrickIcons();

        if (activeTrickPrefab == null || activeTricksRoot == null)
            return;

        IReadOnlyList<TrickSlot> activeSlots = combatManager.PlayerTrickInventory.ActiveCastedSlots;
        for (int i = 0; i < activeSlots.Count; i++)
        {
            TrickSlot slot = activeSlots[i];
            if (slot == null || slot.IsEmpty || slot.RuntimeInstance == null || slot.Definition == null)
                continue;

            ActiveTrickUI iconUI = Instantiate(activeTrickPrefab, activeTricksRoot);
            iconUI.Setup(slot.Definition, ActiveInputKeys[i], slot.RuntimeInstance);
            iconUI.OnReleaseClicked += HandleReleaseClicked;
            instantiatedActiveIcons.Add(iconUI);
        }

        activeTricksLabel.SetActive(instantiatedActiveIcons.Count > 0);
    }

    public void RefreshPassiveTricks()
    {
        ClearPassiveTrickIcons();

        if (passiveTrickPrefab == null || passiveTricksRoot == null)
            return;

        IReadOnlyList<TrickSlot> passiveSlots = combatManager.PlayerTrickInventory.PassiveCastedSlots;
        for (int i = 0; i < passiveSlots.Count; i++)
        {
            TrickSlot slot = passiveSlots[i];
            if (slot == null || slot.IsEmpty || slot.RuntimeInstance == null || slot.Definition == null)
                continue;

            PassiveTrickUI iconUI = Instantiate(passiveTrickPrefab, passiveTricksRoot);
            iconUI.Setup(slot.Definition, slot.RuntimeInstance);
            instantiatedPassiveIcons.Add(iconUI);
        }

        passiveTricksLabel.SetActive(instantiatedPassiveIcons.Count > 0);
    }

    private void ClearPassiveTrickIcons()
    {
        for (int i = 0; i < instantiatedPassiveIcons.Count; i++)
        {
            if (instantiatedPassiveIcons[i] != null)
            {
                Destroy(instantiatedPassiveIcons[i].gameObject);
            }
        }
        instantiatedPassiveIcons.Clear();
    }

    private void ClearActiveTrickIcons()
    {
        for (int i = 0; i < instantiatedActiveIcons.Count; i++)
        {
            if (instantiatedActiveIcons[i] != null)
            {
                instantiatedActiveIcons[i].OnReleaseClicked -= HandleReleaseClicked;
                Destroy(instantiatedActiveIcons[i].gameObject);
            }
        }
        instantiatedActiveIcons.Clear();
    }
}
