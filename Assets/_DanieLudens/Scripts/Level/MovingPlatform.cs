using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Vector2 moveOffset = new Vector2(3f, 0f); // Travel from the start point (x = horizontal, y = vertical)
    [SerializeField] private float speed = 2f;                         // Units per second
    [SerializeField] private float pauseTime = 0.5f;                   // Seconds it waits at each end

    public Vector2 Velocity { get; private set; } // Current velocity (read by the player to ride along)

    private Rigidbody2D rb;       // Platform physics body
    private Vector2 pointA;       // Start point
    private Vector2 pointB;       // End point
    private bool goingToB = true; // Current direction
    private float pauseTimer;     // Time left waiting at an end

    // Set up the body and the two points
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();                      // Get the body
        rb.bodyType = RigidbodyType2D.Kinematic;               // Moved by code, not by gravity or pushes
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Smooth movement on screen
        pointA = rb.position;                                  // Start where it was placed
        pointB = pointA + moveOffset;                          // End point
    }

    // Move between the two points with the physics
    void FixedUpdate()
    {
        if (pauseTimer > 0f) // Waiting at an end
        {
            pauseTimer -= Time.fixedDeltaTime; // Count down
            Velocity = Vector2.zero;           // Not moving
            return;
        }

        Vector2 target = goingToB ? pointB : pointA;                                    // Where it is going
        Vector2 next = Vector2.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime); // One step closer
        Velocity = (next - rb.position) / Time.fixedDeltaTime;                          // Speed of this step
        rb.MovePosition(next);                                                          // Move the body

        if (next == target) // Reached the end
        {
            goingToB = !goingToB;  // Turn around
            pauseTimer = pauseTime; // Wait a moment
        }
    }

    // Draw the path in the editor
    void OnDrawGizmos()
    {
        Vector3 start = Application.isPlaying ? (Vector3)pointA : transform.position; // Start point
        Vector3 end = start + (Vector3)moveOffset;                                     // End point
        Gizmos.color = Color.magenta;                                                  // Path color
        Gizmos.DrawLine(start, end);                                                   // Path
        Gizmos.DrawWireCube(end, transform.lossyScale);                                // Where it ends
    }
}