using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Exibe a grid de Tricks disponíveis para serem castados.
/// Atualiza-se quando tricks são aplicados ou removidos.
/// </summary>
public class TrickDisplayView : MonoBehaviour
{
    [SerializeField] private TrickIconUI trickIconPrefab;
    [SerializeField] private GridLayoutGroup container;
    [SerializeField] private Button castTrickButton; // Button que chama OnTrickSelected
    
    private TrickService trickService;
    private TrickDatabase trickDatabase;
    private Battler currentBattler;
    private readonly Dictionary<string, TrickIconUI> activeTrickIcons = new();
    private TrickSO selectedTrick;
    
    public void Initialize(Battler battler, TrickService service)
    {
        currentBattler = battler;
        trickService = service;
        trickDatabase = TrickDatabase.GetOrCreateRuntimeDatabase();
        
        trickService.OnTrickCasted += HandleTrickCasted;
        trickService.OnTrickRemoved += HandleTrickRemoved;
        
        RefreshDisplay();
    }
    
    /// <summary>
    /// Recarrega todos os tricks disponíveis na grid
    /// </summary>
    private void RefreshDisplay()
    {
        foreach (var icon in activeTrickIcons.Values)
            Destroy(icon.gameObject);
        
        activeTrickIcons.Clear();
        
        // Carregar todos os tricks disponíveis
        trickDatabase.allTricks.ForEach(trickDef =>
        {
            if (trickDef != null && trickDef.IsValid())
            {
                CreateTrickIcon(trickDef);
            }
        });
    }
    
    /// <summary>
    /// Cria um ícone de trick na grid
    /// </summary>
    private void CreateTrickIcon(TrickSO trickDefinition)
    {
        var icon = Instantiate(trickIconPrefab, container.transform);
        icon.Setup(trickDefinition);
        icon.PlayEnterAnimation();
        
        // Adicionar callback para seleção
        icon.GetComponent<Button>().onClick.AddListener(() => SelectTrick(trickDefinition));
        
        activeTrickIcons[trickDefinition.Id] = icon;
    }
    
    /// <summary>
    /// Callback quando um trick é selecionado na grid
    /// </summary>
    private void SelectTrick(TrickSO trick)
    {
        selectedTrick = trick;
        Debug.Log($"[TrickDisplayView] Selected: {trick.DisplayName}");
        
        // Atualizar UI do botão de cast
        if (castTrickButton != null)
            castTrickButton.interactable = trick.CanCast(currentBattler);
    }
    
    /// <summary>
    /// Cast o trick selecionado (chamado pelo botão)
    /// </summary>
    public void CastSelectedTrick()
    {
        if (selectedTrick != null && currentBattler != null)
        {
            trickService.TryCastTrick(currentBattler, selectedTrick.Id, null);
        }
    }
    
    private void HandleTrickCasted(Battler battler, TrickRuntimeInstance instance)
    {
        if (battler != currentBattler)
            return;
        
        Debug.Log($"[TrickDisplayView] Trick casted: {instance.Definition.DisplayName}");
        // Aqui poderíamos adicionar feedback visual
    }
    
    private void HandleTrickRemoved(Battler battler, string trickId)
    {
        if (battler != currentBattler)
            return;
        
        Debug.Log($"[TrickDisplayView] Trick removed: {trickId}");
        // Aqui poderíamos adicionar feedback visual
    }
    
    private void OnDestroy()
    {
        if (trickService != null)
        {
            trickService.OnTrickCasted -= HandleTrickCasted;
            trickService.OnTrickRemoved -= HandleTrickRemoved;
        }
    }
}
