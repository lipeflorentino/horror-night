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
    [SerializeField] private TrickSlotUI learnedTrickSlotPrefab, castedTrickSlotPrefab, identityTrickSlotPrefab;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject trickInventoryPanel;
    [SerializeField] private TrickInfoPanelUI trickInfoPanel;

    private readonly List<TrickSlotUI> spawnedSlots = new();
    private ITrickInventory boundInventory;
    private TrickSlotUI lastSelectedView;

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
        Logger.Log($"[TrickInventoryView] Refresh: Atualizando display de tricks. boundInventory null: {boundInventory == null}");
        
        ClearSpawnedSlots();

        if (boundInventory == null)
        {
            Logger.Log($"[TrickInventoryView] Refresh: boundInventory é null, não será possível renderizar tricks.");
            return;
        }

        Logger.Log($"[TrickInventoryView] Refresh: Iniciando spawn de slots. IdentitySlots={boundInventory.IdentitySlots.Count}, LearnedTricks={boundInventory.LearnedTricks.Count}, CastedSlots={boundInventory.CastedSlots.Count}");
        
        SpawnSlots(boundInventory.IdentitySlots, identitySlotsRoot, TrickInventoryLocation.IdentitySlot);
        SpawnLearnedTricks();
        SpawnSlots(boundInventory.CastedSlots, castedSlotsRoot, TrickInventoryLocation.CastedSlot);

        Logger.Log($"[TrickInventoryView] Refresh: Spawn concluído. Total de slots renderizados: {spawnedSlots.Count}");
        
        if (IsInventoryOpen())
            SelectDefaultTrick();
        else if (trickInfoPanel != null)
            trickInfoPanel.HideTooltip();
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
        {
            Logger.Log($"[TrickInventoryView] SpawnLearnedTricks: LearnedTricks é null!");
            return;
        }

        Logger.Log($"[TrickInventoryView] SpawnLearnedTricks: Renderizando {boundInventory.LearnedTricks.Count} learned tricks.");
        
        for (int i = 0; i < boundInventory.LearnedTricks.Count; i++)
        {
            TrickSO trick = boundInventory.LearnedTricks[i];
            if (trick == null)
            {
                Logger.Log($"[TrickInventoryView] SpawnLearnedTricks[{i}]: Trick é null!");
                continue;
            }

            Logger.Log($"[TrickInventoryView] SpawnLearnedTricks[{i}]: Renderizando learned trick '{trick.DisplayName}' (ID: {trick.Id})");
            SpawnTrickView(trick, FindCastedRuntime(trick), learnedTricksRoot, new TrickInventoryItemLocation(TrickInventoryLocation.LearnedTricks));
        }
        
        Logger.Log($"[TrickInventoryView] SpawnLearnedTricks: Conclusão. LearnedTricksRoot children count: {(learnedTricksRoot != null ? learnedTricksRoot.childCount : -1)}");
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
        {
            Logger.Log($"[TrickInventoryView] SpawnSlots: Slots é null para location {location}!");
            return;
        }
        
        Logger.Log($"[TrickInventoryView] SpawnSlots: Renderizando {slots.Count} slots para {location}. Parent: {(parent != null ? parent.name : "null")}");

        for (int i = 0; i < slots.Count; i++)
        {
            TrickSlot slot = slots[i];
            int slotIndex = slot?.SlotIndex ?? i;
            
            if (slot?.Definition != null)
            {
                Logger.Log($"[TrickInventoryView] SpawnSlots[{i}] ({location}): Renderizando slot com trick '{slot.Definition.DisplayName}' (ID: {slot.Definition.Id}). IsLocked: {slot.IsLocked}");
            }
            else
            {
                Logger.Log($"[TrickInventoryView] SpawnSlots[{i}] ({location}): Slot vazio.");
            }
            
            SpawnTrickView(slot?.Definition, slot?.RuntimeInstance, parent, new TrickInventoryItemLocation(location, slotIndex), slot != null && slot.IsLocked);
        }
        
        Logger.Log($"[TrickInventoryView] SpawnSlots: Conclusão para {location}. Parent children count: {(parent != null ? parent.childCount : -1)}");
    }

    private void SpawnTrickView(TrickSO trick, TrickRuntimeInstance runtimeInstance, Transform parent, TrickInventoryItemLocation location, bool isLocked = false)
    {
        if (trick == null && runtimeInstance == null)
        {
            Logger.Log($"[TrickInventoryView] SpawnTrickView: Criando UI para slot vazio em {location}.");
        }
        else if (trick != null)
        {
            Logger.Log($"[TrickInventoryView] SpawnTrickView: Criando UI para trick '{trick.DisplayName}' (ID: {trick.Id}) em {location}. IsLocked: {isLocked}");
        }
        
        TrickSlotUI trickSlotPrefab = location.Location switch
        {
            TrickInventoryLocation.IdentitySlot => identityTrickSlotPrefab != null ? identityTrickSlotPrefab : null,
            TrickInventoryLocation.CastedSlot => castedTrickSlotPrefab != null ? castedTrickSlotPrefab : null,
            _ => learnedTrickSlotPrefab != null ? learnedTrickSlotPrefab : null,
        };

        if (trickSlotPrefab == null)
        {
            Logger.Log($"[TrickInventoryView] SpawnTrickView: Prefab é null para location {location}!");
            return;
        }
        
        if (parent == null)
        {
            Logger.Log($"[TrickInventoryView] SpawnTrickView: Parent é null para location {location}!");
            return;
        }

        TrickSlotUI trickSlotView = Instantiate(trickSlotPrefab, parent);
        TrickInfoPanelUI panel = trickInfoPanel != null ? trickInfoPanel : FindObjectOfType<TrickInfoPanelUI>();
        trickSlotView.SetTrickInfoPanel(panel);
        trickSlotView.Bind(trick, location, runtimeInstance, isLocked);
        trickSlotView.TrickSelected += HandleTrickSelected;
        trickSlotView.OnInteractWithTrick += HandleTrickInteraction;
        trickSlotView.ShowInteractionPanel(false);
        spawnedSlots.Add(trickSlotView);
        
        Logger.Log($"[TrickInventoryView] SpawnTrickView: UI criada com sucesso. Total de spawned slots: {spawnedSlots.Count}");
    }

    private bool IsInventoryOpen()
    {
        return trickInventoryPanel == null || trickInventoryPanel.activeInHierarchy;
    }

    private void SelectDefaultTrick()
    {
        TrickSlotUI defaultSlot = FindFirstSelectableSlot(TrickInventoryLocation.IdentitySlot)
            ?? FindFirstSelectableSlot(TrickInventoryLocation.LearnedTricks)
            ?? FindFirstSelectableSlot(TrickInventoryLocation.CastedSlot);

        if (defaultSlot != null)
            HandleTrickSelected(defaultSlot);
        else if (trickInfoPanel != null)
        {
            trickInfoPanel.HideTooltip();
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
