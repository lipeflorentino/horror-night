using UnityEngine;
using Cinemachine;

public class RoomTransitionTrigger : MonoBehaviour
{
    public CinemachineVirtualCamera targetCamera;
    public Transform targetSpawnPoint;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerMovement player = collision.GetComponent<PlayerMovement>();

            RoomManager.Instance.TransitionRoom(
                targetCamera,
                targetSpawnPoint,
                player
            );
        }
    }
}