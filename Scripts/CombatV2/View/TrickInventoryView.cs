using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrickInventoryView : MonoBehaviour
{
    [SerializeField] private Transform identitySlotsRoot;
    [SerializeField] private Transform learnedTricksRoot;
    [SerializeField] private Transform castedSlotsRoot;
    [SerializeField] private TrickSlotUI trickSlotPrefab;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject trickInventoryPanel;
    [SerializeField] private TrickInfoPanelUI trickInfoPanel;

    private readonly List<TrickSlotUI> spawnedSlots = new();
    private ITrickInventory boundInventory;

    public event Action<TrickSO, TrickInventoryAction, TrickInventoryItemLocation> OnInteractWithInventoryTrick;

    private void OnEnable()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        Close();
        Refresh();
    }

    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    private void OnDestroy()
    {
        if (boundInventory != null)
            boundInventory.OnChanged -= Refresh;
    }

    public void BindInventory(ITrickInventory trickInventory)
    {
        if (boundInventory != null)
            boundInventory.OnChanged -= Refresh;

        boundInventory = trickInventory;

        if (boundInventory != null)
            boundInventory.OnChanged += Refresh;

        Refresh();
    }

    public void Refresh()
    {
        ClearSpawnedSlots();

        if (boundInventory == null)
            return;

        SpawnSlots(boundInventory.IdentitySlots, identitySlotsRoot, TrickInventoryLocation.IdentitySlot);
        SpawnLearnedTricks();
        SpawnSlots(boundInventory.CastedSlots, castedSlotsRoot, TrickInventoryLocation.CastedSlot);
    }

    public void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    public void Open()
    {
        if (trickInventoryPanel != null)
            trickInventoryPanel.SetActive(true);

        Refresh();
    }

    public void Close()
    {
        CloseAllInteractionPanels();

        if (trickInventoryPanel != null)
            trickInventoryPanel.SetActive(false);
    }

    private void SpawnLearnedTricks()
    {
        if (boundInventory?.LearnedTricks == null)
            return;

        for (int i = 0; i < boundInventory.LearnedTricks.Count; i++)
        {
            TrickSO trick = boundInventory.LearnedTricks[i];
            if (trick == null)
                continue;

            SpawnTrickView(trick, FindCastedRuntime(trick), learnedTricksRoot, new TrickInventoryItemLocation(TrickInventoryLocation.LearnedTricks));
        }
    }

    private TrickRuntimeInstance FindCastedRuntime(TrickSO trick)
    {
        if (trick == null || boundInventory?.CastedSlots == null)
            return null;

        for (int i = 0; i < boundInventory.CastedSlots.Count; i++)
        {
            TrickSlot slot = boundInventory.CastedSlots[i];
            if (slot?.Definition != null && string.Equals(slot.Definition.Id, trick.Id, StringComparison.OrdinalIgnoreCase))
                return slot.RuntimeInstance;
        }

        return null;
    }

    private void SpawnSlots(IReadOnlyList<TrickSlot> slots, Transform parent, TrickInventoryLocation location)
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Count; i++)
        {
            TrickSlot slot = slots[i];
            int slotIndex = slot?.SlotIndex ?? i;
            SpawnTrickView(slot?.Definition, slot?.RuntimeInstance, parent, new TrickInventoryItemLocation(location, slotIndex), slot != null && slot.IsLocked);
        }
    }

    private void SpawnTrickView(TrickSO trick, TrickRuntimeInstance runtimeInstance, Transform parent, TrickInventoryItemLocation location, bool isLocked = false)
    {
        if (trickSlotPrefab == null || parent == null)
            return;

        TrickSlotUI trickSlotView = Instantiate(trickSlotPrefab, parent);
        TrickInfoPanelUI panel = trickInfoPanel != null ? trickInfoPanel : FindObjectOfType<TrickInfoPanelUI>();
        trickSlotView.SetTrickInfoPanel(panel);
        trickSlotView.Bind(trick, location, runtimeInstance, isLocked);
        trickSlotView.TrickSelected += HandleTrickSelected;
        trickSlotView.OnInteractWithTrick += HandleTrickInteraction;
        trickSlotView.ShowInteractionPanel(false);
        spawnedSlots.Add(trickSlotView);
    }

    private void HandleTrickSelected(TrickSlotUI selectedView)
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            TrickSlotUI view = spawnedSlots[i];
            if (view == null)
                continue;

            view.ShowInteractionPanel(view == selectedView);
        }
    }

    private void HandleTrickInteraction(TrickSO trick, TrickInventoryAction action, TrickInventoryItemLocation location)
    {
        OnInteractWithInventoryTrick?.Invoke(trick, action, location);
    }

    private void ClearSpawnedSlots()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            TrickSlotUI slotView = spawnedSlots[i];
            if (slotView != null)
            {
                slotView.TrickSelected -= HandleTrickSelected;
                slotView.OnInteractWithTrick -= HandleTrickInteraction;
                Destroy(slotView.gameObject);
            }
        }

        spawnedSlots.Clear();
    }

    private void CloseAllInteractionPanels()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
            if (spawnedSlots[i] != null)
                spawnedSlots[i].ShowInteractionPanel(false);
    }
}
