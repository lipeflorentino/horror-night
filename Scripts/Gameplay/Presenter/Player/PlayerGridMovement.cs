using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerGridMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelController levelController;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Movement")]
    [SerializeField] private float moveDuration = 0.25f;

    private bool isMoving = false;

    public event Action<int> OnMoveCompleted;

    private void Start()
    {
        if (levelController == null)
            levelController = FindObjectOfType<LevelController>();

        // Sincroniza posição inicial
        Vector3 startPos = levelController.GetWorldPositionFromIndex(levelController.CurrentIndex);
        transform.position = startPos;
    }

    private void Update()
    {
        if (isMoving)
            return;

        int direction = 0;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            direction = -1;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            direction = 1;

        if (direction != 0)
            TryMove(direction);
    }

    private void TryMove(int direction)
    {
        bool success = levelController.TryMove(direction);

        if (!success)
            return;

        if (direction < 0)
            spriteRenderer.flipX = true;
        else if (direction > 0)
            spriteRenderer.flipX = false;

        Vector3 targetPos = levelController.GetWorldPositionFromIndex(levelController.CurrentIndex);

        StartCoroutine(MoveTo(targetPos));
    }

    private IEnumerator MoveTo(Vector3 targetPosition)
    {
        isMoving = true;

        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
        isMoving = false;

        OnMoveCompleted?.Invoke(levelController.CurrentIndex);
    }
}
