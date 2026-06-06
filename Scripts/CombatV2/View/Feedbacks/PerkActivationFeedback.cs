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
        perkService.OnPerkTriggered += HandlePerkTriggered;
    }
    
    private void HandlePerkTriggered(Battler battler, string perkId, PerkTrigger trigger)
    {
        PerkSO definition = perkService.GetPerkDefinition(perkId);
        if (definition == null) return;
        
        Transform anchor = battler.IsPlayer ? playerAnchor : enemyAnchor;
        var popup = Instantiate(perkActivationPrefab, anchor);
        
        // Configurar popup
        var img = popup.GetComponent<Image>();
        img.sprite = definition.Icon;
        
        var text = popup.GetComponentInChildren<TextMeshProUGUI>();
        text.text = $"{definition.DisplayName} ativado!";
        
        // Animar por 2 segundos
        StartCoroutine(AnimatePopup(popup));
    }
    
    private IEnumerator AnimatePopup(GameObject popup)
    {
        // Animação de fade in + scale
        // ...
        yield return new WaitForSeconds(2f);
        Destroy(popup);
    }
    
    private void OnDestroy()
    {
        if (perkService != null)
            perkService.OnPerkTriggered -= HandlePerkTriggered;
    }
}