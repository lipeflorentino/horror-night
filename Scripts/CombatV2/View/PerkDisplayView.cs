using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PerkDisplayView : MonoBehaviour
{
    [SerializeField] private PerkIconUI perkIconPrefab;
    [SerializeField] private GridLayoutGroup container;
    
    private PerkService perkService;
    private Battler currentBattler;
    private readonly Dictionary<string, PerkIconUI> activeIcons = new();
    
    public void Initialize(Battler battler, PerkService service)
    {
        currentBattler = battler;
        perkService = service;
        
        perkService.OnPerkApplied += HandlePerkApplied;
        perkService.OnPerkRemoved += HandlePerkRemoved;
        
        RefreshDisplay();
    }
    
    private void RefreshDisplay()
    {
        foreach (var icon in activeIcons.Values)
            Destroy(icon.gameObject);
            
        activeIcons.Clear();
        
        foreach (var perk in currentBattler.Perks)
        {
            var icon = Instantiate(perkIconPrefab, container.transform);
            icon.Setup(perk.Definition, perk.Stacks);
            icon.PlayEnterAnimation();
            activeIcons[perk.Definition.Id] = icon;
        }
    }
    
    private void HandlePerkApplied(Battler battler, PerkRuntimeInstance instance)
    {
        if (battler != currentBattler) return;
        
        if (activeIcons.ContainsKey(instance.Definition.Id))
        {
            activeIcons[instance.Definition.Id].UpdateStacks(instance.Stacks);
        }
        else
        {
            var icon = Instantiate(perkIconPrefab, container.transform);
            icon.Setup(instance.Definition, instance.Stacks);
            icon.PlayEnterAnimation();
            activeIcons[instance.Definition.Id] = icon;
        }
    }
    
    private void HandlePerkRemoved(Battler battler, string perkId)
    {
        if (battler != currentBattler || !activeIcons.ContainsKey(perkId))
            return;
        
        var icon = activeIcons[perkId];
        icon.PlayExitAnimation();
        Destroy(icon.gameObject, 0.3f);
        activeIcons.Remove(perkId);
    }
    
    private void OnDestroy()
    {
        if (perkService != null)
        {
            perkService.OnPerkApplied -= HandlePerkApplied;
            perkService.OnPerkRemoved -= HandlePerkRemoved;
        }
    }
}