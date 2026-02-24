using UnityEngine;
using System;

public class LevelUpUI : MonoBehaviour
{
    public event Action OnContinuePressed;

    [Header("Input")]
    [SerializeField] private KeyCode continueKey = KeyCode.Space;

    private bool isVisible;

    private void Update()
    {
        if (!isVisible)
            return;

        if (Input.GetKeyDown(continueKey))
        {
            OnContinuePressed?.Invoke();
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
        isVisible = true;
    }

    public void Hide()
    {
        isVisible = false;
        gameObject.SetActive(false);
    }
}