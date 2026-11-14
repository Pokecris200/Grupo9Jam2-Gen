using UnityEngine;

[DefaultExecutionOrder(-50)]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;                       // What the camera follows (found by tag if empty)
    [SerializeField] private Vector2 offset = new Vector2(0f, 2.6f); // Camera position relative to the ground the player stands on
    [SerializeField] private float smoothTime = 0.15f;               // Seconds to catch up (lower = tighter)
    [SerializeField] private float lookAhead = 1.5f;                 // Extra distance shown in the walking direction
    [SerializeField] private float lookAheadSpeed = 3f;              // How fast the look-ahead changes side

    [Header("Vertical")]
    [SerializeField] private float fallFollowDistance = 2f; // Follow down if the player falls this far below the last ground
    [SerializeField] private float upFollowDistance = 4f;   // Follow up if the player goes this far above the last ground

    [Header("Level Bounds")]
    [SerializeField] private bool useBounds = false;                         // Keep the camera inside the level
    [SerializeField] private Vector2 minPosition = new Vector2(-50f, -10f);  // Lowest camera position allowed
    [SerializeField] private Vector2 maxPosition = new Vector2(50f, 10f);    // Highest camera position allowed

    private PlayerController player; // To know when the player is on the ground
    private Vector3 velocity;        // Used by SmoothDamp
    private float currentLook;       // Current look-ahead distance
    private float lastTargetX;       // Target X on the last frame
    private float groundY;           // Height the camera uses as reference

    // Find the player and start on it
    void Start()
    {
        if (target == null) // No target assigned
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player"); // Search by tag
            if (playerObject != null) target = playerObject.transform;           // Use it
        }
        if (target == null) return; // Nothing to follow

        player = target.GetComponent<PlayerController>(); // Get the controller
        lastTargetX = target.position.x;                  // Remember the start X
        groundY = target.position.y;                      // Start at the player's height
        transform.position = DesiredPosition();           // Start on the player without sliding in
    }

    // Follow the target after it has moved
    void LateUpdate()
    {
        if (target == null) return; // Nothing to follow

        UpdateGroundY(); // Decide the reference height

        float moveX = target.position.x - lastTargetX; // How much the target moved this frame
        lastTargetX = target.position.x;               // Remember for the next frame
        if (Mathf.Abs(moveX) > 0.001f)                 // The target is walking
        {
            currentLook = Mathf.MoveTowards(currentLook, Mathf.Sign(moveX) * lookAhead, lookAheadSpeed * Time.deltaTime); // Look where it walks
        }

        transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref velocity, smoothTime); // Glide toward the goal
    }

    // Change the reference height only when it makes sense
    private void UpdateGroundY()
    {
        float y = target.position.y; // Current player height

        if (player == null || player.IsGrounded) // Standing on something
        {
            groundY = y; // New ground: follow it
        }
        else if (y < groundY - fallFollowDistance) // Falling far below the last ground
        {
            groundY = y + fallFollowDistance; // Follow down, keeping the player in view
        }
        else if (y > groundY + upFollowDistance) // Going very high without landing
        {
            groundY = y - upFollowDistance; // Follow up, keeping the player in view
        }
        // Otherwise (a normal jump): keep groundY, so the floor stays still on screen
    }

    // Where the camera wants to be
    private Vector3 DesiredPosition()
    {
        Vector3 goal = new Vector3(target.position.x + offset.x + currentLook, groundY + offset.y, transform.position.z); // Player X, ground Y
        if (useBounds) // Stay inside the level
        {
            goal.x = Mathf.Clamp(goal.x, minPosition.x, maxPosition.x); // Limit X
            goal.y = Mathf.Clamp(goal.y, minPosition.y, maxPosition.y); // Limit Y
        }
        return goal;
    }

    // Draw the bounds in the editor
    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;                              // Nothing to draw
        Gizmos.color = Color.yellow;                         // Bounds color
        Vector3 center = (minPosition + maxPosition) * 0.5f; // Middle of the bounds
        Vector3 size = maxPosition - minPosition;            // Size of the bounds
        Gizmos.DrawWireCube(center, size);                   // Show where the camera can move
    }
}