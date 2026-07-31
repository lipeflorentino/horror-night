using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class TrickInventoryView : MonoBehaviour
{
    [FormerlySerializedAs("Slots Root")]
    [SerializeField] private Transform identitySlotsRoot;
    [SerializeField] private Transform learnedTricksRoot;
    [SerializeField] private Transform activeCastedSlotsRoot;
    [SerializeField] private Transform passiveCastedSlotsRoot;
    [FormerlySerializedAs("Prefabs")]
    [SerializeField] private TrickSlotUI activeCastedTrickSlotPrefab;
    [SerializeField] private TrickSlotUI passiveCastedTrickSlotPrefab;
    [SerializeField] private TrickSlotUI identityTrickSlotPrefab;
    [SerializeField] private TrickSlotUI learnedTrickSlotPrefab;
    [Header("Settings")]
    [SerializeField] private int maxLearnedSlots = 16;
    [Header("Components")]
    [SerializeField] private FeedbackPanelUI statusFeedbackPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject trickInventoryPanel;
    [SerializeField] private TrickInfoPanelUI trickInfoPanel;

    private readonly List<TrickSlotUI> spawnedSlots = new();
    private ITrickInventory boundInventory;
    private TrickSlotUI lastSelectedView;
    private static readonly string[] CastedActiveInputKeys = { "Q", "W", "E", "R" };

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
        {
            boundInventory.OnChanged += Refresh;
        }

        Refresh();
    }

    public void Refresh()
    {
        ClearSpawnedSlots();

        if (boundInventory == null)
        {
            return;
        }

        SpawnSlots(boundInventory.IdentitySlots, identitySlotsRoot, TrickInventoryLocation.IdentitySlot);
        SpawnLearnedTricks();
        SpawnActiveCastedSlots();
        SpawnPassiveCastedSlots();

        if (IsInventoryOpen())
            SelectDefaultTrick();
        else if (trickInfoPanel != null)
            trickInfoPanel.HidePanel();
    }

    /// <summary>
    /// Delega o controle da mensagem e animação de feedback para o UIFeedbackPanel.
    /// </summary>
    public void SetStatus(string message)
    {
        if (statusFeedbackPanel != null)
        {
            statusFeedbackPanel.ShowStatus(message);
        }
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
        {
            return;
        }

        for (int i = 0; i < maxLearnedSlots; i++)
        {
            TrickSO trick = i < boundInventory.LearnedTricks.Count ? boundInventory.LearnedTricks[i] : null;
            TrickRuntimeInstance runtime = FindCastedRuntime(trick);
            bool isAlreadyCasted = runtime != null;
            SpawnTrickView(trick, runtime, learnedTricksRoot, new TrickInventoryItemLocation(TrickInventoryLocation.LearnedTricks, i), isAlreadyCasted);
        }
    }

    private TrickRuntimeInstance FindCastedRuntime(TrickSO trick)
    {
        if (trick == null || boundInventory == null)
            return null;

        return FindCastedRuntimeInSlots(trick, boundInventory.ActiveCastedSlots)
            ?? FindCastedRuntimeInSlots(trick, boundInventory.PassiveCastedSlots);
    }

    private TrickRuntimeInstance FindCastedRuntimeInSlots(TrickSO trick, IReadOnlyList<TrickSlot> slots)
    {
        if (slots == null)
            return null;

        for (int i = 0; i < slots.Count; i++)
        {
            TrickSlot slot = slots[i];
            if (slot?.Definition != null && string.Equals(slot.Definition.Id, trick.Id, StringComparison.OrdinalIgnoreCase))
                return slot.RuntimeInstance;
        }

        return null;
    }

    private void SpawnActiveCastedSlots()
    {
        if (boundInventory?.ActiveCastedSlots == null)
            return;

        for (int i = 0; i < boundInventory.ActiveCastedSlots.Count; i++)
        {
            TrickSlot slot = boundInventory.ActiveCastedSlots[i];
            int slotIndex = slot?.SlotIndex ?? i;
            string inputKey = i < CastedActiveInputKeys.Length ? CastedActiveInputKeys[i] : string.Empty;
            SpawnTrickView(slot?.Definition, slot?.RuntimeInstance, activeCastedSlotsRoot, new TrickInventoryItemLocation(TrickInventoryLocation.CastedActiveSlot, slotIndex), slot != null && slot.IsLocked, inputKey);
        }
    }

    private void SpawnPassiveCastedSlots()
    {
        SpawnSlots(boundInventory?.PassiveCastedSlots, passiveCastedSlotsRoot, TrickInventoryLocation.CastedPassiveSlot);
    }

    private void SpawnSlots(IReadOnlyList<TrickSlot> slots, Transform parent, TrickInventoryLocation location)
    {
        if (slots == null)
        {
            return;
        }
        
        for (int i = 0; i < slots.Count; i++)
        {
            TrickSlot slot = slots[i];
            int slotIndex = slot?.SlotIndex ?? i;
            SpawnTrickView(slot?.Definition, slot?.RuntimeInstance, parent, new TrickInventoryItemLocation(location, slotIndex), slot != null && slot.IsLocked);
        }    
    }

    private void SpawnTrickView(TrickSO trick, TrickRuntimeInstance runtimeInstance, Transform parent, TrickInventoryItemLocation location, bool isLocked = false, string inputKey = "")
    {
        TrickSlotUI trickSlotPrefab = location.Location switch
        {
            TrickInventoryLocation.IdentitySlot => identityTrickSlotPrefab != null ? identityTrickSlotPrefab : null,
            TrickInventoryLocation.CastedActiveSlot => activeCastedTrickSlotPrefab != null ? activeCastedTrickSlotPrefab : null,
            TrickInventoryLocation.CastedPassiveSlot => passiveCastedTrickSlotPrefab != null ? passiveCastedTrickSlotPrefab : activeCastedTrickSlotPrefab,
            _ => learnedTrickSlotPrefab != null ? learnedTrickSlotPrefab : null,
        };

        if (trickSlotPrefab == null)
        {
            return;
        }
        
        if (parent == null)
        {
            return;
        }

        TrickSlotUI trickSlotView = Instantiate(trickSlotPrefab, parent);
        TrickInfoPanelUI panel = trickInfoPanel != null ? trickInfoPanel : FindObjectOfType<TrickInfoPanelUI>();
        trickSlotView.SetTrickInfoPanel(panel);
        trickSlotView.Bind(trick, location, runtimeInstance, isLocked, inputKey);
        trickSlotView.TrickSelected += HandleTrickSelected;
        trickSlotView.OnInteractWithTrick += HandleTrickInteraction;
        trickSlotView.ShowInteractionPanel(false);
        spawnedSlots.Add(trickSlotView);    
    }

    private bool IsInventoryOpen()
    {
        return trickInventoryPanel == null || trickInventoryPanel.activeInHierarchy;
    }

    private void SelectDefaultTrick()
    {
        TrickSlotUI defaultSlot = FindFirstSelectableSlot(TrickInventoryLocation.IdentitySlot)
            ?? FindFirstSelectableSlot(TrickInventoryLocation.LearnedTricks)
            ?? FindFirstSelectableSlot(TrickInventoryLocation.CastedActiveSlot)
            ?? FindFirstSelectableSlot(TrickInventoryLocation.CastedPassiveSlot);

        if (defaultSlot != null)
            HandleTrickSelected(defaultSlot);
        else if (trickInfoPanel != null)
        {
            trickInfoPanel.HidePanel();
            ClearSlotSelections();
        }
    }

    private TrickSlotUI FindFirstSelectableSlot(TrickInventoryLocation location)
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            TrickSlotUI view = spawnedSlots[i];
            if (view != null && view.HasTrick && view.Location.Location == location)
                return view;
        }

        return null;
    }

    private void HandleTrickSelected(TrickSlotUI selectedView)
    {
        if (lastSelectedView != null && lastSelectedView != selectedView)
        {
            lastSelectedView.SetSelected(false);
            lastSelectedView.ShowInteractionPanel(false);
        }
        
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            TrickSlotUI view = spawnedSlots[i];
            if (view == null)
                continue;

            if (view == selectedView)
            {
                view.SetSelected(true);
                view.ShowInteractionPanel(true);
            }
            else if (view != lastSelectedView)
            {
                view.SetSelected(false);
            }
        }

        lastSelectedView = selectedView;
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
                slotView.SetSelected(false);
                Destroy(slotView.gameObject);
            }
        }

        spawnedSlots.Clear();
        lastSelectedView = null;
    }

    private void CloseAllInteractionPanels()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
            if (spawnedSlots[i] != null)
            {
                spawnedSlots[i].ShowInteractionPanel(false);
                spawnedSlots[i].SetSelected(false);
            }
    }

    private void ClearSlotSelections()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
            if (spawnedSlots[i] != null)
                spawnedSlots[i].SetSelected(false);
    }
}