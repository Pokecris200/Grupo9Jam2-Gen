using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

public class WorldWave : MonoBehaviour
{
    [Header("Wave")]
    [SerializeField] private Shader frontShader;                   // Mohan/WaveFront (the ring of light)
    [SerializeField] private float duration = 0.4f;                // Seconds to cover the screen
    [SerializeField] private float pixelSize = 1f / 32f;           // One art pixel in units (4 px with PPU 128)
    [SerializeField] private string sortingLayer = "Primer_Plano"; // Layer of the ring (in front of the level, behind the UI)
    [SerializeField] private int sortingOrder = 100;               // Order inside that layer

    [Header("Amulet")]
    [SerializeField] private SpriteRenderer playerSprite;                 // Fisherman art (found by the Player tag if empty)
    [SerializeField] private Vector2 handOffset = new Vector2(0f, 0.75f); // Raised hand, measured from the feet
    [SerializeField] private Sprite[] amuletFrames;                       // Slices of amuleto_rapido.png
    [SerializeField] private float amuletFps = 15f;                       // Speed of the amulet animation
    [SerializeField] private Sprite raisedArmPose;                        // Optional: jump_0, arm up while the amulet shows

    private Light2D globalLight;     // Scene global light (its color is the world tint)
    private Camera cam;              // To know how big the screen is
    private SpriteMask mask;         // Growing circle: inside it the new world shows
    private SpriteRenderer front;    // Quad with the ring shader, covers the screen
    private Material frontMaterial;  // Material of the ring (one per wave object)
    private SpriteRenderer amulet;   // Amulet shown in the raised hand

    private readonly List<(Renderer r, WorldObject owner)> appearing = new List<(Renderer, WorldObject)>();    // Renderers that appear with this wave
    private readonly List<(Renderer r, WorldObject owner)> disappearing = new List<(Renderer, WorldObject)>(); // Renderers that disappear with this wave

    private bool hasWorld;                        // Already know which world is showing
    private World shownWorld;                     // World the screen is showing (or going to)
    private bool pending;                         // A switch happened this frame
    private World pendingWorld;                   // World to reveal in LateUpdate
    private bool running;                         // Wave in progress
    private float startTime;                      // When the current wave started
    private Color fromColor, toColor, shownColor; // Light colors: before, after, and on screen now
    private float amuletStart = -100f;            // When the amulet animation started

    private static readonly int CenterId = Shader.PropertyToID("_Center");       // Shader property IDs
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int PixelId = Shader.PropertyToID("_PixelSize");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

    // Create the mask, the ring and the amulet, and find the references
    void Awake()
    {
        cam = Camera.main; // Camera that shows the level

        if (playerSprite == null) // Not assigned in the Inspector
        {
            GameObject player = GameObject.FindWithTag("Player");                              // Find the fisherman by tag
            if (player != null) playerSprite = player.GetComponentInChildren<SpriteRenderer>(); // Use his art
        }

        foreach (Light2D light in FindObjectsByType<Light2D>(FindObjectsSortMode.None)) // Every 2D light (same search as WorldManager)
        {
            if (light.lightType == Light2D.LightType.Global) // Only the global one
            {
                globalLight = light; // Use it
                break;
            }
        }
        shownColor = globalLight != null ? globalLight.color : Color.white; // Color on screen right now

        mask = new GameObject("Wave Mask").AddComponent<SpriteMask>(); // Circle that cuts the two worlds
        mask.transform.SetParent(transform, false);                     // Keep it under this object
        mask.sprite = CircleSprite();                                   // Round shape made in code
        mask.enabled = false;                                           // Off until a wave starts

        front = new GameObject("Wave Front").AddComponent<SpriteRenderer>(); // Quad that draws the ring
        front.transform.SetParent(transform, false);                         // Keep it under this object
        front.sprite = SquareSprite();                                       // Plain square to draw on
        frontMaterial = new Material(frontShader);                           // Own copy of the ring material
        frontMaterial.SetFloat(PixelId, pixelSize);                          // Ring on the pixel art grid
        front.sharedMaterial = frontMaterial;                                // Use it
        front.sortingLayerName = sortingLayer;                               // In front of the level
        front.sortingOrder = sortingOrder;                                   // Order inside that layer
        front.enabled = false;                                               // Off until a wave starts

        amulet = new GameObject("Amulet").AddComponent<SpriteRenderer>(); // Amulet in the hand
        amulet.transform.SetParent(transform, false);                     // Keep it under this object
        amulet.enabled = false;                                           // Hidden until the player raises it
    }

    // Listen for the amulet and the world changes
    void OnEnable()
    {
        WorldManager.OnWorldChanged += OnWorldChanged;   // The world changed: start the wave
        WorldManager.OnSwitchStarted += OnSwitchStarted; // The player raised the amulet
    }

    // Stop listening when disabled
    void OnDisable()
    {
        WorldManager.OnWorldChanged -= OnWorldChanged;   // Avoid calls on a disabled object
        WorldManager.OnSwitchStarted -= OnSwitchStarted; // Avoid calls on a disabled object
    }

    // Remember the starting world (no wave for it)
    void Start()
    {
        if (!hasWorld && WorldManager.Instance != null) // Starting world not known yet
        {
            hasWorld = true;                                 // Now it is
            shownWorld = WorldManager.Instance.CurrentWorld; // Save it
        }
    }

    // The player raises the amulet: start its animation before the world changes
    private void OnSwitchStarted()
    {
        amuletStart = Time.time; // Raise the amulet
    }

    // Take note of the new world; the wave itself starts in LateUpdate, after everyone reacted
    private void OnWorldChanged(World world)
    {
        if (!hasWorld) // First call is the starting world
        {
            hasWorld = true;    // Now it is known
            shownWorld = world; // Save it
            return;             // No wave for it
        }
        if (world == shownWorld && !pending) return; // Nothing new to show

        fromColor = shownColor;                                          // What is on screen now
        toColor = globalLight != null ? globalLight.color : Color.white; // The new world color
        pendingWorld = world;                                            // World to reveal
        pending = true;                                                  // Start the wave in LateUpdate
    }

    // Start, move and finish the wave after Update and after the Animator, so it wins over both
    void LateUpdate()
    {
        if (pending) // A switch happened this frame
        {
            pending = false;                 // Handle it once
            if (running) Finish();           // A new switch in the middle of a wave: close the old one
            Begin(shownWorld, pendingWorld); // Start the new wave
            shownWorld = pendingWorld;       // The screen is going to this world
        }

        if (running) // Wave in progress
        {
            float t = (Time.time - startTime) / duration; // Progress from 0 to 1
            if (t >= 1f) Finish();                        // Done
            else Step(t);                                 // Keep growing
        }
        else if (globalLight != null) // No wave
        {
            shownColor = globalLight.color; // Follow the light as it is
        }

        UpdateAmulet(); // Amulet in the hand
    }

    // Show both worlds: the new things only inside the circle, the old ones only outside
    private void Begin(World from, World to)
    {
        appearing.Clear();    // Start with empty lists
        disappearing.Clear();

        foreach (WorldObject wo in WorldObject.All) // Every object that belongs to a world
        {
            bool before = wo.ExistsIn(from); // Was it visible?
            bool after = wo.ExistsIn(to);    // Will it be visible?
            if (before == after) continue;   // In both worlds (or none): nothing to reveal

            var list = after ? appearing : disappearing;                                                                     // Which list it goes to
            var interaction = after ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.VisibleOutsideMask; // New inside, old outside
            foreach (Renderer r in wo.GetComponentsInChildren<Renderer>(true)) // Every renderer of the object
            {
                if (!SetMask(r, interaction)) continue; // Can't be masked: it switches instantly
                r.enabled = true;                       // Visible, the mask decides where
                list.Add((r, wo));                      // Remember it for Finish
            }
        }

        running = true;         // Wave in progress
        startTime = Time.time;  // From now
        mask.enabled = true;    // Turn on the cut
        front.enabled = true;   // Turn on the ring
        Step(0f);               // First frame of the wave
    }

    // Grow the circle, move the ring and blend the light color
    private void Step(float t)
    {
        float eased = 1f - (1f - t) * (1f - t); // Fast at first, slows down at the end (ease-out)
        if (cam == null) cam = Camera.main;     // The camera may appear after Awake
        Vector2 center = HandPosition();        // The wave comes out of the hand

        float h = cam.orthographicSize * 2f;      // Screen height in units
        float w = h * cam.aspect;                 // Screen width in units
        Vector2 camPos = cam.transform.position;  // Screen center
        float maxRadius = 0f;                     // Distance to the farthest corner
        for (int i = 0; i < 4; i++)               // The four corners
        {
            Vector2 corner = camPos + new Vector2((i % 2 == 0 ? -0.5f : 0.5f) * w, (i < 2 ? -0.5f : 0.5f) * h); // This corner
            maxRadius = Mathf.Max(maxRadius, Vector2.Distance(center, corner));                                // Keep the farthest
        }
        maxRadius += 3f * pixelSize; // Plus the ring width

        float radius = Mathf.Round(eased * maxRadius / pixelSize) * pixelSize; // Whole art pixels, so the ring stays on the grid

        mask.transform.position = center;                                                // Cut centered on the hand
        mask.transform.localScale = Vector3.one * (2f * Mathf.Max(0f, radius - pixelSize)); // One pixel inside the ring, so the ring covers its edge

        front.transform.position = new Vector3(camPos.x, camPos.y, 0f); // Ring quad covers the screen
        front.transform.localScale = new Vector3(w + 1f, h + 1f, 1f);   // A bit bigger than the screen
        frontMaterial.SetVector(CenterId, center);                      // Ring center
        frontMaterial.SetFloat(RadiusId, radius);                       // Ring size
        frontMaterial.SetFloat(IntensityId, 1f - eased * 0.6f);         // Bright when it comes out, fades at the end

        shownColor = Color.Lerp(fromColor, toColor, eased);      // Light goes from one world to the other
        if (globalLight != null) globalLight.color = shownColor; // Apply it
    }

    // Leave everything as WorldObject wants it for the current world
    private void Finish()
    {
        World now = WorldManager.Instance != null ? WorldManager.Instance.CurrentWorld : shownWorld; // Real current world
        foreach (var (r, _) in appearing) SetMask(r, SpriteMaskInteraction.None);                     // New things: no cut anymore
        foreach (var (r, owner) in disappearing) // Old things
        {
            SetMask(r, SpriteMaskInteraction.None); // No cut anymore
            r.enabled = owner.ExistsIn(now);        // Hidden if they don't belong to this world
        }
        appearing.Clear();    // Empty the lists
        disappearing.Clear();

        if (globalLight != null) globalLight.color = toColor; // Final world color
        shownColor = toColor;                                 // Save it
        mask.enabled = false;                                 // Turn off the cut
        front.enabled = false;                                // Turn off the ring
        running = false;                                      // Wave done
    }

    // Play the amulet frames at the hand (and the raised-arm pose, if there is one)
    private void UpdateAmulet()
    {
        int frame = Mathf.FloorToInt((Time.time - amuletStart) * amuletFps);                                       // Current frame
        bool show = amuletFrames != null && frame >= 0 && frame < amuletFrames.Length && playerSprite != null; // Still playing?
        amulet.enabled = show;                                                                                   // Show or hide it
        if (!show) return;                                                                                       // Nothing else to do

        amulet.sprite = amuletFrames[frame];                                                                  // Frame of the animation
        Vector2 hand = HandPosition();                                                                        // Where the hand is
        amulet.transform.position = new Vector3(Snap(hand.x), Snap(hand.y), playerSprite.transform.position.z); // On the pixel grid
        amulet.sortingLayerID = playerSprite.sortingLayerID;                                                  // Same layer as the fisherman
        amulet.sortingOrder = playerSprite.sortingOrder + 1;                                                  // Just in front of him
        if (raisedArmPose != null) playerSprite.sprite = raisedArmPose;                                       // Arm up (wins over the Animator)
    }

    // Where the raised hand is in the world (it follows the fisherman and his facing)
    private Vector2 HandPosition()
    {
        if (playerSprite == null) return cam.transform.position;                                               // No player: screen center
        float side = playerSprite.flipX ? -1f : 1f;                                                            // Facing left or right
        return playerSprite.transform.TransformPoint(new Vector3(handOffset.x * side, handOffset.y, 0f)); // Hand in world space
    }

    // Round a position to the art pixel grid
    private float Snap(float v) => Mathf.Round(v / pixelSize) * pixelSize;

    // Set how a renderer reacts to the mask (only these kinds can be cut)
    private static bool SetMask(Renderer r, SpriteMaskInteraction interaction)
    {
        switch (r)
        {
            case SpriteRenderer s: s.maskInteraction = interaction; return true;         // Sprites
            case TilemapRenderer t: t.maskInteraction = interaction; return true;        // Tilemaps
            case ParticleSystemRenderer p: p.maskInteraction = interaction; return true; // Particles
            default: return false;                                                       // Anything else switches instantly
        }
    }

    // White circle 1 unit wide, made in code (no extra asset needed)
    private static Sprite CircleSprite()
    {
        const int size = 256;                                                                            // Texture size in pixels
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point }; // Crisp edges
        var pixels = new Color32[size * size];                                                           // All the pixels
        float c = (size - 1) / 2f;                                                                       // Center
        for (int y = 0; y < size; y++)                                                                   // Every row
        {
            for (int x = 0; x < size; x++) // Every column
            {
                bool inside = (x - c) * (x - c) + (y - c) * (y - c) <= c * c;                                 // Inside the circle?
                pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0); // White inside, clear outside
            }
        }
        tex.SetPixels32(pixels);                                                                  // Write the pixels
        tex.Apply();                                                                              // Send them to the GPU
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size); // 1 unit wide sprite
    }

    // White square 1 unit wide, for the ring quad
    private static Sprite SquareSprite()
    {
        Texture2D tex = Texture2D.whiteTexture;                                                                // Built-in white texture
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width); // 1 unit wide sprite
    }
}
