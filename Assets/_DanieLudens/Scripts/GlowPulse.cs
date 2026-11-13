using UnityEngine;

public class GlowPulse : MonoBehaviour
{
    [SerializeField] private float speed = 2f;         // Pulses per second (roughly)
    [SerializeField] private float minAlpha = 0.25f;   // Faintest transparency
    [SerializeField] private float maxAlpha = 0.7f;    // Strongest transparency
    [SerializeField] private float scaleAmount = 0.15f; // How much it grows at the peak

    private SpriteRenderer sr; // Glow sprite
    private Vector3 baseScale; // Original size

    // Cache the sprite and its original size
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>(); // Get the glow sprite
        baseScale = transform.localScale;    // Remember the starting scale
    }

    // Animate transparency and size with a sine wave
    void Update()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f;         // Value that goes 0 → 1 → 0
        Color c = sr.color;                                          // Copy the current color
        c.a = Mathf.Lerp(minAlpha, maxAlpha, t);                     // Fade between min and max
        sr.color = c;                                                // Apply the new color
        transform.localScale = baseScale * (1f + scaleAmount * t);   // Grow and shrink slightly
    }
}