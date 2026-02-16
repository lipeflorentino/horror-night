using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;

    [Header("Step By Step Settings")]
    public float stepSpeed = 5f;
    public float stepDuration = 0.2f;
    public float pauseDuration = 0.2f;

    private Rigidbody2D rb;
    private float moveInput;
    private bool isStepWalking = false;
    private bool isStepping = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        bool shiftPressed = Input.GetKey(KeyCode.LeftShift);

        if (shiftPressed && moveInput != 0)
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
        rb.velocity = new Vector2(moveInput * walkSpeed, rb.velocity.y);
    }

    private IEnumerator StepByStepWalk()
    {
        isStepWalking = true;

        while (Input.GetKey(KeyCode.LeftShift) && moveInput != 0)
        {
            // STEP
            isStepping = true;
            float timer = 0f;

            while (timer < stepDuration)
            {
                rb.velocity = new Vector2(moveInput * stepSpeed, rb.velocity.y);
                timer += Time.deltaTime;
                yield return null;
            }

            // PAUSE
            isStepping = false;
            rb.velocity = new Vector2(0, rb.velocity.y);

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
}
