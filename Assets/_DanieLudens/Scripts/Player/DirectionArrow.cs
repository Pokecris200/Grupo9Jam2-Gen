using UnityEngine;

public class DirectionArrow : MonoBehaviour
{
    [SerializeField] private Transform target;        // Exit portal (found automatically if empty)
    [SerializeField] private float radius = 1.2f;     // Distance from the player's center
    [SerializeField] private float hideDistance = 2f; // Hide when the player is this close to the target
    [SerializeField] private float bobAmount = 0.1f;  // Small pulse toward the target
    [SerializeField] private float bobSpeed = 6f;     // Pulse speed

    private SpriteRenderer sr; // Arrow sprite
    private Transform player;  // Player (the arrow's parent)
    private bool shown;        // True during the escape phase

    // Cache references and start hidden
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>(); // Get the arrow sprite
        player = transform.parent;           // The arrow lives inside the Player
        sr.enabled = false;                  // Hidden until the escape

        if (target == null) // No target assigned
        {
            ExitPortal portal = FindFirstObjectByType<ExitPortal>(); // Look for the portal
            if (portal != null) target = portal.transform;          // Use it as the target
        }
    }

    // Listen for the escape and the game over
    void OnEnable()
    {
        EscapeSequence.OnEscapeStarted += Show; // Show during the escape
        MohanMeter.OnMohanCaught += Hide;       // Hide on game over
    }

    // Stop listening when disabled
    void OnDisable()
    {
        EscapeSequence.OnEscapeStarted -= Show; // Avoid calls on a disabled object
        MohanMeter.OnMohanCaught -= Hide;       // Avoid calls on a disabled object
    }

    // Turn the arrow on
    private void Show()
    {
        shown = true; // Start pointing
    }

    // Turn the arrow off
    private void Hide()
    {
        shown = false; // Stop pointing
    }

    // Place and rotate the arrow after the player has moved
    void LateUpdate()
    {
        if (!shown || target == null || player == null) // Nothing to point at
        {
            sr.enabled = false; // Keep it hidden
            return;
        }

        Vector2 toTarget = target.position - player.position; // Direction and distance to the portal
        bool visible = toTarget.magnitude > hideDistance;      // Hide when already close
        sr.enabled = visible;                                  // Show or hide
        if (!visible) return;                                  // Nothing else to do

        Vector2 dir = toTarget.normalized;                                       // Direction only (length 1)
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;                 // Small pulse
        transform.position = (Vector2)player.position + dir * (radius + bob);   // Orbit around the player

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;     // Direction as an angle in degrees
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);  // Triangle sprite points up, so rotate -90
    }
}
