using System;
using UnityEngine;
using UnityEngine.UI;

public class GameOverView
{
    private readonly GameObject panel;
    private readonly Button restart;

    public GameOverView(GameObject panel, Button restart)
    {
        this.panel = panel;
        this.restart = restart;
    }

    public void Show(Action onRestart)
    {
        Time.timeScale = 0f;
        panel.SetActive(true);

        restart.onClick.RemoveAllListeners();
        restart.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            onRestart?.Invoke();
        });
    }
}