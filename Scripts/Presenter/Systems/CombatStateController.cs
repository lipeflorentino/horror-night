using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatStateController : MonoBehaviour
{
    public EnemyInstance CurrentEnemy { get; private set; }
    public RunStateSnapshot RunSnapshot { get; private set; }
    public bool CombatActive { get; private set; }
    public bool ShouldRestoreSnapshotOnReturn { get; private set; } = true;

    public bool TryBeginCombat(EnemyInstance enemy)
    {
        if (enemy == null)
            return false;

        if (!CaptureRunSnapshot())
            return false;

        CurrentEnemy = enemy;
        CombatActive = true;
        ShouldRestoreSnapshotOnReturn = true;
        return true;
    }

    public void ApplyCombatResults(int life, int physical, int mental, TurnManagerStats combatStats)
    {
        if (RunSnapshot == null)
            return;

        RunSnapshot.playerStatus.heart = life;
        RunSnapshot.playerStatus.physical = physical;
        RunSnapshot.playerStatus.mind = mental;

        combatStats.Normalize();
        RunSnapshot.playerStatus.combatStats = combatStats;
    }

    public void MarkSkipRestoreOnReturn()
    {
        ShouldRestoreSnapshotOnReturn = false;
    }

    public IEnumerator RestoreRunStateAfterCombatNextFrame()
    {
        yield return null;

        if (RunSnapshot == null)
        {
            Clear();
            yield break;
        }

        LevelController levelController = FindObjectOfType<LevelController>();
        PlayerStatusManager statusManager = FindObjectOfType<PlayerStatusManager>();
        PlayerInventory inventory = FindObjectOfType<PlayerInventory>();

        if (levelController != null)
            levelController.RestoreProgress(RunSnapshot.level, RunSnapshot.levelIndex, RunSnapshot.exploredNodes);

        if (statusManager != null)
            statusManager.RestoreSnapshot(RunSnapshot.playerStatus);

        if (inventory != null)
            inventory.RestoreSnapshot(RunSnapshot.inventoryItems);

        PlayerGridMovement movement = FindObjectOfType<PlayerGridMovement>();
        if (movement != null && levelController != null)
        {
            Vector3 position = levelController.GetWorldPositionFromIndex(levelController.CurrentIndex);
            movement.transform.position = position;
        }

        Clear();
    }

    public void Clear()
    {
        CombatActive = false;
        CurrentEnemy = null;
        RunSnapshot = null;
        ShouldRestoreSnapshotOnReturn = true;
    }

    private bool CaptureRunSnapshot()
    {
        LevelController levelController = FindObjectOfType<LevelController>();
        PlayerStatusManager statusManager = FindObjectOfType<PlayerStatusManager>();
        PlayerInventory inventory = FindObjectOfType<PlayerInventory>();

        if (levelController == null || statusManager == null || inventory == null)
        {
            Debug.LogError("Could not capture run state before combat.");
            return false;
        }

        RunSnapshot = new RunStateSnapshot
        {
            sceneName = SceneManager.GetActiveScene().name,
            level = levelController.currentLevel,
            levelIndex = levelController.CurrentIndex,
            exploredNodes = levelController.CaptureExploredSnapshot(),
            playerStatus = statusManager.GetSnapshot(),
            inventoryItems = inventory.CreateSnapshot()
        };

        return true;
    }
}
