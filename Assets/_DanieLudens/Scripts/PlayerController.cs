using UnityEngine;
using UnityEngine.InputSystem; // New Input System

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f; // Horizontal speed

    private Rigidbody2D rb;          // Physics body
    private InputAction moveAction;  // "Move" input action
    private float moveInput;         // Horizontal input (-1 to 1)

    // Cache components and input action
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();                           // Get the Rigidbody2D
        moveAction = InputSystem.actions.FindAction("Move");        // Find the Move action
    }

    // Read player input every frame
    void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>().x; // Keep only the X axis
    }

    // Apply movement with physics
    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y); // Move on X, keep Y (gravity/jump)
    }
}
