using TMPro;
using UnityEngine;

public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private GameObject container;
    [SerializeField] private TMP_Text promptText;

    public void Show(string text)
    {
        if (promptText != null)
        {
            promptText.text = text;
        }

        if (container != null)
        {
            container.SetActive(true);
        }
    }

    public void Hide()
    {
        if (container != null)
        {
            container.SetActive(false);
        }
    }
}
