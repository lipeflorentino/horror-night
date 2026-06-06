using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PerkIconUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI stackCount;
    // [SerializeField] private Animator animator;
    
    private PerkSO perkDefinition;
    [SerializeField] private PerkTooltip tooltip;
    [SerializeField] private GameObject tooltipPrefab;
    
    public void Setup(PerkSO definition, int stacks)
    {
        perkDefinition = definition;
        icon.sprite = definition.Icon;
        UpdateStacks(stacks);
    }
    
    public void UpdateStacks(int stacks)
    {
        stackCount.text = stacks > 1 ? stacks.ToString() : "";
    }
    
    public void PlayEnterAnimation()
    {
        // if (animator != null)
        //     animator.SetTrigger("Enter");
    }
    
    public void PlayExitAnimation()
    {
        // if (animator != null)
        //     animator.SetTrigger("Exit");
    }
    
    public void PlayTriggerAnimation()
    {
        // if (animator != null)
        //     animator.SetTrigger("Triggered");
    }
    
    void OnEnable()
    {
        GetComponent<Button>().onClick.AddListener(() => { /* */ });
    }
    
    void OnMouseEnter()
    {
        tooltip = Instantiate(tooltipPrefab, transform).GetComponent<PerkTooltip>();
        tooltip.Show(perkDefinition);
    }
    
    void OnMouseExit()
    {
        Destroy(tooltip.gameObject);
    }
}