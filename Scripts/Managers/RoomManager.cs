using UnityEngine;
using Cinemachine;
using System.Collections;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    public FadeController fadeController;
    private CinemachineVirtualCamera currentCamera;
    private bool isTransitioning = false;

    private void Awake()
    {
        Instance = this;
    }

    public void TransitionRoom(
        CinemachineVirtualCamera newCamera,
        Transform spawnPoint,
        PlayerMovement player)
    {
        if (!isTransitioning)
            StartCoroutine(DoTransition(newCamera, spawnPoint, player));
    }

    private IEnumerator DoTransition(
        CinemachineVirtualCamera newCamera,
        Transform spawnPoint,
        PlayerMovement player)
    {
        isTransitioning = true;

        player.LockMovement();

        yield return fadeController.FadeOut();

        if (currentCamera != null)
            currentCamera.Priority = 0;

        newCamera.Priority = 10;
        currentCamera = newCamera;

        player.transform.position = spawnPoint.position;

        yield return new WaitForSeconds(0.1f);

        yield return fadeController.FadeIn();

        player.UnlockMovement();

        isTransitioning = false;
    }
}