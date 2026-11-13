using UnityEngine;
using UnityEngine.InputSystem; // New Input System

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f; // Horizontal speed

    [Header("Jump")]
    [SerializeField] private float jumpForce = 12f;          // Upward jump velocity
    [SerializeField] private Transform groundCheck;          // Point under the feet
    [SerializeField] private float groundCheckRadius = 0.1f; // Ground detection radius
    [SerializeField] private LayerMask groundLayer;          // What counts as ground

    [Header("Jump Feel")]
    [SerializeField] private float fallGravityMultiplier = 2f; // Extra gravity when falling
    [SerializeField] private float maxFallSpeed = 20f;         // Fall speed limit
    [SerializeField] private float coyoteTime = 0.1f;          // Jump time after leaving a ledge
    [SerializeField] private float jumpBufferTime = 0.1f;      // Jump time before landing

    private Rigidbody2D rb;          // Physics body
    private InputAction moveAction;  // "Move" input action
    private InputAction jumpAction;  // "Jump" input action
    private float moveInput;         // Horizontal input (-1 to 1)
    private bool isGrounded;         // Touching the ground?
    private float baseGravityScale;  // Gravity set in the Inspector
    private float coyoteTimer;       // Time left to use coyote jump
    private float jumpBufferTimer;   // Time left for a buffered jump

    // Cache components, input actions and base gravity
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();                    // Get the Rigidbody2D
        moveAction = InputSystem.actions.FindAction("Move"); // Find the Move action
        jumpAction = InputSystem.actions.FindAction("Jump"); // Find the Jump action
        baseGravityScale = rb.gravityScale;                  // Save the original gravity
    }

    // Read input and update the jump buffer every frame
    void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>().x; // Keep only the X axis

        if (jumpAction.WasPressedThisFrame()) // Jump pressed this frame
        {
            jumpBufferTimer = jumpBufferTime; // Start the buffer
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime; // Count the buffer down
        }
    }

    // Apply movement, jump and gravity with physics
    void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer); // Check ground under feet

        if (isGrounded && rb.linearVelocity.y <= 0.01f) // On the ground and not rising
        {
            coyoteTimer = coyoteTime; // Refill coyote time
        }
        else
        {
            coyoteTimer -= Time.fixedDeltaTime; // Count coyote time down
        }

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y); // Move on X, keep Y

        if (jumpBufferTimer > 0f && coyoteTimer > 0f) // Jump pressed recently and grounded recently
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce); // Set upward velocity
            jumpBufferTimer = 0f; // Use up the buffer
            coyoteTimer = 0f;     // Use up coyote time
        }

        rb.gravityScale = rb.linearVelocity.y < 0f ? baseGravityScale * fallGravityMultiplier : baseGravityScale; // Heavier when falling

        if (rb.linearVelocity.y < -maxFallSpeed) // Falling too fast
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed); // Clamp fall speed
        }
    }

    // Draw the ground check circle in the editor
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;                                 // Nothing to draw
        Gizmos.color = Color.yellow;                                     // Gizmo color
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius); // Show detection area
    }
}
