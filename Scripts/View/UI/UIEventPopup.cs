using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UIEventPopup : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button pickButton;
    [SerializeField] private Button leaveButton;

    public void Show(ItemSO item, Action onPick, Action onLeave)
    {
        root.SetActive(true);

        nameText.text = item.itemName;
        descriptionText.text = item.description;
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
