using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    private bool canMove = true;

    [Header("Step By Step Settings")]
    public float stepSpeed = 5f;
    public float stepDuration = 0.2f;
    public float pauseDuration = 0.2f;
    private Rigidbody2D rb;
    private float horizontalInput;
    private float verticalInput;

    private bool isStepWalking = false;
    private bool isStepping = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!canMove) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput   = Input.GetAxisRaw("Vertical");

        HandleFlip();

        bool shiftPressed = Input.GetKey(KeyCode.LeftShift);
        bool hasInput = horizontalInput != 0 || verticalInput != 0;

        if (shiftPressed && hasInput)
        {
            if (!isStepWalking)
                StartCoroutine(StepByStepWalk());
        }
        else
        {
            StopStepWalk();
            NormalWalk();
        }
    }

    private void NormalWalk()
    {
        Vector2 direction = new Vector2(horizontalInput, verticalInput).normalized;
        rb.velocity = direction * walkSpeed;
    }

    private IEnumerator StepByStepWalk()
    {
        isStepWalking = true;

        while (Input.GetKey(KeyCode.LeftShift))
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput   = Input.GetAxisRaw("Vertical");

            Vector2 direction = new Vector2(horizontalInput, verticalInput).normalized;

            if (direction == Vector2.zero)
                break;

            // STEP
            isStepping = true;
            float timer = 0f;

            while (timer < stepDuration)
            {
                rb.velocity = direction * stepSpeed;
                timer += Time.deltaTime;
                yield return null;
            }

            // PAUSE
            isStepping = false;
            rb.velocity = Vector2.zero;

            yield return new WaitForSeconds(pauseDuration);
        }

        StopStepWalk();
    }

    private void StopStepWalk()
    {
        if (isStepWalking)
        {
            StopAllCoroutines();
            isStepWalking = false;
        }
    }

    private void HandleFlip()
    {
        if (horizontalInput > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (horizontalInput < 0)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    public void LockMovement()
    {
        canMove = false;
        rb.velocity = Vector2.zero;
    }

    public void UnlockMovement()
    {
        canMove = true;
    }
}
