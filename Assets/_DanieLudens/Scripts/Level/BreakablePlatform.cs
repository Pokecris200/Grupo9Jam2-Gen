using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(BoxCollider2D))]
public class BreakablePlatform : MonoBehaviour
{
    [SerializeField] private float breakDelay = 0.6f;                      // Seconds after landing before it breaks
    [SerializeField] private float respawnTime = 3f;                       // Seconds until it comes back
    [SerializeField] private float shakeAmount = 0.05f;                    // Shake distance while cracking
    [SerializeField] private Color crackColor = new Color(1f, 0.5f, 0.5f); // Tint while cracking

    private SpriteRenderer sr;     // Platform sprite
    private Tilemap tilemap;       // Platform tilemap
    private BoxCollider2D box;     // Platform collider
    private Vector3 startPosition; // Original position
    private Color startColor;      // Original color
    private bool breaking;         // Already cracking or broken

    // Cache components and the original state
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();  // Get the sprite platform
        tilemap = GetComponent<Tilemap>();     // Get the tilemap platform
        box = GetComponent<BoxCollider2D>();  // Get the collider
        startPosition = transform.position;   // Remember where it belongs
        startColor = GetColor();                // Remember its color
    }

    // Start cracking when the player lands on top
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (breaking || !collision.collider.CompareTag("Player")) return;          // Already breaking or not the player
        float platformTop = transform.TransformPoint(box.offset).y + Mathf.Abs(box.size.y * transform.lossyScale.y) * 0.5f; // Top edge in world space
        bool fromAbove = collision.collider.bounds.min.y >= platformTop - 0.1f;     // Player's feet are on the top edge
        if (fromAbove) StartCoroutine(CrackRoutine());                              // Only break when stepped on
    }

    // Shake and change color, then break
    private IEnumerator CrackRoutine()
    {
        breaking = true; // Only once

        for (float t = 0f; t < breakDelay; t += Time.deltaTime) // Every frame until breakDelay
        {
            float k = t / breakDelay;                                                                  // Progress from 0 to 1
            transform.position = startPosition + (Vector3)(Random.insideUnitCircle * shakeAmount * k); // Shake harder over time
            SetColor(Color.Lerp(startColor, crackColor, k));                                           // Turn red
            yield return null;                                                                         // Next frame
        }

        transform.position = startPosition; // Back in place for the respawn
        SetColor(startColor);               // Original color for the respawn

        if (WorldManager.Instance != null) WorldManager.Instance.StartCoroutine(RespawnRoutine()); // Timer runs on an object that stays active
        gameObject.SetActive(false); // Break: disappears completely
    }

    // Wait, make sure the player is not in the way, then come back
    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);                // Wait before coming back
        while (PlayerInside()) yield return new WaitForSeconds(0.2f); // Don't trap the player
        breaking = false;                                            // Ready to break again
        gameObject.SetActive(true);                                  // Come back
    }

    // Check if the player is standing where the platform would appear
    private bool PlayerInside()
    {
        Vector2 center = transform.TransformPoint(box.offset);                // Collider center in world space
        Vector2 size = Vector2.Scale(box.size, transform.lossyScale) * 0.95f; // Collider size in world space
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, size, 0f)) // Everything in that area
        {
            if (hit.CompareTag("Player")) return true; // The player is there
        }
        return false; // Free space
    }
    
    // Read the platform color (sprite or tiles)
    private Color GetColor()
    {
        if (sr != null) return sr.color;           // Sprite platform
        if (tilemap != null) return tilemap.color; // Tilemap platform
        return Color.white;                        // Nothing to tint
    }

    // Tint the platform (sprite or tiles)
    private void SetColor(Color color)
    {
        if (sr != null) sr.color = color;           // Sprite platform
        if (tilemap != null) tilemap.color = color; // Tilemap platform
    }
}
