using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    public float transitionSpeed = 3f;
    private bool isTransitioning = false;

    public IEnumerator TransitionToRoom(PlayerMovement player, Transform targetPoint)
    {
        if (isTransitioning)
            yield break;

        isTransitioning = true;

        player.enabled = false;

        Vector3 startCamPos = transform.position;
        Vector3 targetCamPos = new Vector3(
            targetPoint.position.x,
            targetPoint.position.y,
            transform.position.z
        );

        Vector3 startPlayerPos = player.transform.position;
        Vector3 targetPlayerPos = targetPoint.position;

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * transitionSpeed;

            transform.position = Vector3.Lerp(startCamPos, targetCamPos, t);
            player.transform.position = Vector3.Lerp(startPlayerPos, targetPlayerPos, t);

            yield return null;
        }

        player.transform.position = targetPlayerPos;
        transform.position = targetCamPos;

        player.enabled = true;
        isTransitioning = false;
    }
}
