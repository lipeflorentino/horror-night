using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UILootPopup : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button pickButton;
    [SerializeField] private Button leaveButton;

    private string GetRarityText(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Uncommon => "Incomum",
            Rarity.Rare => "raro",
            Rarity.Epic => "Épico",
            Rarity.Legendary => "Lendário",
            _ => "comum",
        };
    }

    public void Show(ItemSO item, Action onPick, Action onLeave)
    {
        root.SetActive(true);

        nameText.text = item.itemName;
        descriptionText.text = item.description;
        rarityText.text = GetRarityText(item.rarity);
        iconImage.sprite = item.icon;

        pickButton.onClick.RemoveAllListeners();
        leaveButton.onClick.RemoveAllListeners();

        pickButton.onClick.AddListener(() =>
        {
            onPick?.Invoke();
            root.SetActive(false);
        });

        leaveButton.onClick.AddListener(() =>
        {
            onLeave?.Invoke();
            root.SetActive(false);
        });
    }
}