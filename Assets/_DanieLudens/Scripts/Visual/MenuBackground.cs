using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuBackground : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public RectTransform strip;        // Row of repeated tiles that scrolls
        public float speed = 10f;          // Canvas pixels per second (0 = still)
        public float mouseShift = 10f;     // Max sideways shift following the mouse
        [HideInInspector] public float offset; // Current scroll inside one tile
    }

    [System.Serializable]
    public class FireflyGroup
    {
        public RectTransform area;         // Where they fly (full screen rect)
        public int count = 10;             // How many
        public float size = 20f;           // Canvas pixels
        [Range(0f, 1f)] public float brightness = 1f; // Max alpha
        public float speed = 25f;          // Canvas pixels per second
    }

    [Header("Parallax")]
    [SerializeField] private Layer[] layers;                 // Back to front
    [SerializeField] private float tileWidth = 1024f;        // Width of one tile in canvas pixels
    [SerializeField] private float mouseSmoothing = 2.5f;    // Higher = follows the mouse faster
    [SerializeField] private float pixelSnap = 2f;           // Round positions to this (crisp pixel art)

    [Header("Fireflies")]
    [SerializeField] private Sprite fireflySprite;           // Small glowing dot
    [SerializeField] private Color fireflyColor = new Color(0.85f, 1f, 0.45f, 1f); // Yellow green
    [SerializeField] private FireflyGroup[] fireflyGroups;   // Behind the ruins, in front of the foliage

    private float mouse;          // Smoothed mouse position, -1 (left) to 1 (right)
    private Firefly[] flies;      // Every firefly of every group

    private class Firefly
    {
        public RectTransform rect;  // Its transform
        public Image image;         // Its image (alpha pulses)
        public FireflyGroup group;  // Settings of its group
        public Vector2 pos;         // Position inside the area
        public float seed;          // Random seed for the wander
        public float pulseRate;     // Glow speed
        public float pulsePhase;    // Glow offset
    }

    // Create the fireflies
    void Start()
    {
        SpawnFireflies(); // Build them at runtime so the scene stays light
    }

    // Scroll the layers and move the fireflies
    void Update()
    {
        float dt = Time.unscaledDeltaTime; // Works even if the game is paused
        float t = Time.unscaledTime;       // Clock for the noise

        float target = 0f;                                                       // Center when there is no mouse
        if (Mouse.current != null && Screen.width > 0)                           // Mouse available
            target = Mathf.Clamp(Mouse.current.position.ReadValue().x / Screen.width * 2f - 1f, -1f, 1f); // -1 to 1
        mouse = Mathf.Lerp(mouse, target, 1f - Mathf.Exp(-mouseSmoothing * dt)); // Smooth follow

        foreach (var layer in layers) // Move each layer
        {
            if (layer.strip == null) continue;                                // Not assigned
            layer.offset = Mathf.Repeat(layer.offset + layer.speed * dt, tileWidth); // Loop inside one tile
            float x = -layer.offset - mouse * layer.mouseShift;              // Scroll left + follow the mouse
            layer.strip.anchoredPosition = new Vector2(Snap(x), layer.strip.anchoredPosition.y); // Crisp
        }

        if (flies == null) return; // No fireflies
        foreach (var f in flies)   // Move each firefly
        {
            var size = f.group.area.rect.size;                                          // Area size
            float angle = Mathf.PerlinNoise(f.seed, t * 0.15f) * Mathf.PI * 4f;         // Slowly changing direction
            f.pos += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.6f) * f.group.speed * dt; // Wander, flatter vertically
            f.pos.x = Mathf.Repeat(f.pos.x, size.x);                                    // Wrap left/right
            f.pos.y = Mathf.Clamp(f.pos.y, size.y * 0.05f, size.y * 0.75f);             // Stay low, like over the grass
            f.rect.anchoredPosition = new Vector2(Snap(f.pos.x - mouse * 12f), Snap(f.pos.y)); // Slight parallax too

            float glow = Mathf.Sin(t * f.pulseRate + f.pulsePhase) * 0.5f + 0.5f;       // 0 to 1
            var c = fireflyColor;                                                        // Base color
            c.a = glow * glow * f.group.brightness;                                      // Soft blink
            f.image.color = c;                                                           // Apply
        }
    }

    // Create the fireflies of every group
    private void SpawnFireflies()
    {
        if (fireflySprite == null || fireflyGroups == null) return; // Nothing to spawn
        var list = new System.Collections.Generic.List<Firefly>();  // All of them
        foreach (var g in fireflyGroups) // Each group
        {
            if (g.area == null) continue;     // Not assigned
            var size = g.area.rect.size;      // Area size
            for (int i = 0; i < g.count; i++) // Each firefly
            {
                var go = new GameObject("Firefly", typeof(RectTransform), typeof(Image)); // UI object
                go.transform.SetParent(g.area, false);                                    // Inside its group
                var rect = (RectTransform)go.transform;                                   // Its rect
                rect.anchorMin = rect.anchorMax = Vector2.zero;                           // Bottom-left coordinates
                rect.sizeDelta = new Vector2(g.size, g.size);                             // Size
                var image = go.GetComponent<Image>();                                     // Its image
                image.sprite = fireflySprite;                                             // Glow dot
                image.raycastTarget = false;                                              // Never blocks buttons
                list.Add(new Firefly
                {
                    rect = rect,
                    image = image,
                    group = g,
                    pos = new Vector2(Random.Range(0f, size.x), Random.Range(size.y * 0.05f, size.y * 0.75f)), // Random start
                    seed = Random.Range(0f, 100f),         // Different wander
                    pulseRate = Random.Range(1.2f, 2.8f),  // Different glow speed
                    pulsePhase = Random.Range(0f, 6.28f)   // Not in sync
                });
            }
        }
        flies = list.ToArray(); // Store them
    }

    // Round to the pixel grid so the pixel art stays sharp
    private float Snap(float v)
    {
        return pixelSnap > 0f ? Mathf.Round(v / pixelSnap) * pixelSnap : v; // Nearest step
    }
}
