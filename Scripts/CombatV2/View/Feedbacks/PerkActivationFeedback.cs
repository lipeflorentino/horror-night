using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PerkActivationFeedback : MonoBehaviour
{
    [SerializeField] private GameObject perkActivationPrefab;
    [SerializeField] private Transform playerAnchor;
    [SerializeField] private Transform enemyAnchor;
    
    private PerkService perkService;
    
    public void Initialize(PerkService service)
    {
        perkService = service;
        // ✅ Novo: Recebe PerkTriggeredEvent ao invés de 3 parâmetros separados
        perkService.OnPerkTriggered += HandlePerkTriggered;
    }
    
    private void HandlePerkTriggered(PerkTriggeredEvent evt)
    {
        if (evt.Owner == null)
            return;
        
        Transform anchor = evt.Owner.IsPlayer ? playerAnchor : enemyAnchor;
        var popup = Instantiate(perkActivationPrefab, anchor);
        
        // Configurar popup com ícone e nome do perk
        var img = popup.GetComponent<Image>();
        PerkSO definition = perkService.GetPerkDefinition(evt.PerkId);
        if (definition != null)
        {
            // Nota: Icon foi removido de PerkSO. Para exibir ícone, use TrickDatabase
            // if (definition.Icon != null) img.sprite = definition.Icon;
        }
        
        var text = popup.GetComponentInChildren<TextMeshProUGUI>();
        text.text = $"{evt.PerkId} acionado!";
        
        // Animar por 2 segundos
        StartCoroutine(AnimatePopup(popup));
        
        // Log detalhado do perk acionado
        Debug.Log($"[PerkFeedback] {evt.PerkId} " +
                  $"- Trigger: {evt.Trigger}, Target: {evt.ModifierTarget}, Value: {evt.AppliedValue}, Stacks: {evt.StacksApplied}");
    }
    
    private IEnumerator AnimatePopup(GameObject popup)
    {
        // Animação de fade in + scale
        // TODO: Implementar animação visual e sonora aqui
        yield return new WaitForSeconds(2f);
        Destroy(popup);
    }
    
    private void OnDestroy()
    {
        if (perkService != null)
            perkService.OnPerkTriggered -= HandlePerkTriggered;
    }
}