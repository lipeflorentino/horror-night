using System;
using UnityEngine;
using UnityEngine.UI;

public class CombatSceneBindings : MonoBehaviour
{
    public Transform playerSpawnPoint;
    public Transform enemySpawnPoint;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;

    public void ShowGameOverUI(Action onRestart)
    {
        Time.timeScale = 0f;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (restartButton == null)
            return;

        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            onRestart?.Invoke();
        });
    }
}
