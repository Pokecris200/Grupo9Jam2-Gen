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

    private Rigidbody2D rb;          // Physics body
    private InputAction moveAction;  // "Move" input action
    private InputAction jumpAction;  // "Jump" input action
    private float moveInput;         // Horizontal input (-1 to 1)
    private bool jumpRequested;      // Jump pressed, waiting for physics
    private bool isGrounded;         // Touching the ground?

    // Cache components and input actions
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();                    // Get the Rigidbody2D
        moveAction = InputSystem.actions.FindAction("Move"); // Find the Move action
        jumpAction = InputSystem.actions.FindAction("Jump"); // Find the Jump action
    }

    // Read player input every frame
    void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>().x; // Keep only the X axis

        if (jumpAction.WasPressedThisFrame()) // Jump pressed this frame
        {
            jumpRequested = true; // Save it for FixedUpdate
        }
    }

    // Apply movement and jump with physics
    void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer); // Check ground under feet

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y); // Move on X, keep Y

        if (jumpRequested && isGrounded) // Only jump when grounded
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce); // Set upward velocity
        }
        jumpRequested = false; // Clear the jump request
    }

    // Draw the ground check circle in the editor
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;                                  // Nothing to draw
        Gizmos.color = Color.yellow;                                      // Gizmo color
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);  // Show detection area
    }
}
