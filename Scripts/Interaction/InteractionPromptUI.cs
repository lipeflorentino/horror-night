using TMPro;
using UnityEngine;

public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private GameObject container;
    [SerializeField] private TMP_Text promptText;

    void Start()
    {
        Hide();
    }

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

    public void Flicker()
    {
        if (promptText != null)
        {
            container.transform.GetComponent<PromptFeedbackEffect>().PlayDeniedEffect();
        }
    }
}
