using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

// Shows the world switch as a wave that grows from the amulet, like in the intro video.
// Inside the circle the new world is already visible; outside it, the old one still is.
// It only changes the VISUALS: WorldManager and WorldObject keep working the same
// (colliders still switch instantly, so the gameplay doesn't change).
// Add it to any object in the scene (e.g. next to the WorldManager).
public class WorldWave : MonoBehaviour
{
    [Header("Wave")]
    [SerializeField] private Shader frontShader;                // Mohan/OndaFrente (the ring of light)
    [SerializeField] private float duration = 0.4f;             // Seconds to cover the screen (video: 0.8 the first time, 0.3 in the escape)
    [SerializeField] private float pixelSize = 1f / 32f;        // One art pixel in units (4 px with PPU 128)
    [SerializeField] private string sortingLayer = "Primer_Plano"; // Layer of the ring (in front of the level, behind the UI)
    [SerializeField] private int sortingOrder = 100;            // Above the world, below the UI

    [Header("Amulet")]
    [SerializeField] private SpriteRenderer playerSprite;                 // Fisherman art (auto: tag Player)
    [SerializeField] private Vector2 handOffset = new Vector2(0f, 0.75f); // Raised hand from the sprite pivot (bottom center)
    [SerializeField] private Sprite[] amuletFrames;                       // Slices of amuleto_rapido.png
    [SerializeField] private float amuletFps = 15f;                       // Speed of the amulet animation
    [SerializeField] private Sprite raisedArmPose;                        // Optional: jump_0, arm up while the amulet shows

    private Light2D globalLight;  // Scene global light (its color is the world tint)
    private Camera cam;           // To know how big the screen is
    private SpriteMask mask;      // Growing circle: inside it the new world shows
    private SpriteRenderer front; // Quad with the ring shader, covers the screen
    private Material frontMaterial;
    private SpriteRenderer amulet;

    // Renderers that appear / disappear with the current wave, and the object they belong to
    private readonly List<(Renderer r, WorldObject owner)> appearing = new List<(Renderer, WorldObject)>();
    private readonly List<(Renderer r, WorldObject owner)> disappearing = new List<(Renderer, WorldObject)>();

    private bool hasWorld;       // Already know which world is showing
    private World shownWorld;    // World the screen is showing (or going to)
    private bool pending;        // A switch happened this frame
    private World pendingWorld;
    private bool running;        // Wave in progress
    private float startTime;
    private Color fromColor, toColor, shownColor; // Light colors: before, after, and on screen now
    private float amuletStart = -100f;            // When the amulet animation started

    private static readonly int CenterId = Shader.PropertyToID("_Centro");
    private static readonly int RadiusId = Shader.PropertyToID("_Radio");
    private static readonly int PixelId = Shader.PropertyToID("_PixelSize");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensidad");

    // Create the mask, the ring and the amulet, and find the references
    void Awake()
    {
        cam = Camera.main;
        if (playerSprite == null) // Find the fisherman by tag
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) playerSprite = player.GetComponentInChildren<SpriteRenderer>();
        }
        foreach (Light2D light in FindObjectsByType<Light2D>(FindObjectsSortMode.None)) // Same search as WorldManager
        {
            if (light.lightType == Light2D.LightType.Global) { globalLight = light; break; }
        }
        shownColor = globalLight != null ? globalLight.color : Color.white;

        mask = new GameObject("Wave Mask").AddComponent<SpriteMask>();
        mask.transform.SetParent(transform, false);
        mask.sprite = CircleSprite();
        mask.enabled = false;

        front = new GameObject("Wave Front").AddComponent<SpriteRenderer>();
        front.transform.SetParent(transform, false);
        front.sprite = SquareSprite();
        frontMaterial = new Material(frontShader);
        frontMaterial.SetFloat(PixelId, pixelSize);
        front.sharedMaterial = frontMaterial;
        front.sortingLayerName = sortingLayer;
        front.sortingOrder = sortingOrder;
        front.enabled = false;

        amulet = new GameObject("Amulet").AddComponent<SpriteRenderer>();
        amulet.transform.SetParent(transform, false);
        amulet.enabled = false;
    }

    void OnEnable()
    {
        WorldManager.OnWorldChanged += OnWorldChanged;
        WorldManager.OnSwitchBlocked += OnSwitchBlocked;
    }

    void OnDisable()
    {
        WorldManager.OnWorldChanged -= OnWorldChanged;
        WorldManager.OnSwitchBlocked -= OnSwitchBlocked;
    }

    void Start()
    {
        if (!hasWorld && WorldManager.Instance != null) // Starting world, without a wave
        {
            hasWorld = true;
            shownWorld = WorldManager.Instance.CurrentWorld;
        }
    }

    // WorldManager already changed the light and WorldObject already showed/hid things.
    // We only take note here; the real work happens in LateUpdate, after everyone reacted.
    private void OnWorldChanged(World world)
    {
        if (!hasWorld) // The first call is the starting world: no wave
        {
            hasWorld = true;
            shownWorld = world;
            return;
        }
        if (world == shownWorld && !pending) return;

        fromColor = shownColor;                                       // What is on screen now
        toColor = globalLight != null ? globalLight.color : Color.white; // The new world color
        pendingWorld = world;
        pending = true;
        amuletStart = Time.time; // Raise the amulet
    }

    // Switch not allowed: the amulet still shows, but no wave
    private void OnSwitchBlocked()
    {
        amuletStart = Time.time;
    }

    // Runs after Update and after the Animator, so it wins over both
    void LateUpdate()
    {
        if (pending)
        {
            pending = false;
            if (running) Finish(); // A new switch in the middle of a wave: close the old one
            Begin(shownWorld, pendingWorld);
            shownWorld = pendingWorld;
        }

        if (running)
        {
            float t = (Time.time - startTime) / duration;
            if (t >= 1f) Finish();
            else Step(t);
        }
        else if (globalLight != null)
        {
            shownColor = globalLight.color;
        }

        UpdateAmulet();
    }

    // Show both worlds: the new things only inside the circle, the old ones only outside
    private void Begin(World from, World to)
    {
        appearing.Clear();
        disappearing.Clear();
        foreach (WorldObject wo in WorldObject.All)
        {
            bool before = wo.ExistsIn(from);
            bool after = wo.ExistsIn(to);
            if (before == after) continue; // It is in both (or none): nothing to reveal

            var list = after ? appearing : disappearing;
            var interaction = after ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.VisibleOutsideMask;
            foreach (Renderer r in wo.GetComponentsInChildren<Renderer>(true))
            {
                if (!SetMask(r, interaction)) continue; // This kind of renderer can't be masked: it switches instantly
                r.enabled = true;
                list.Add((r, wo));
            }
        }

        running = true;
        startTime = Time.time;
        mask.enabled = true;
        front.enabled = true;
        Step(0f);
    }

    // Grow the circle, move the ring and blend the light color
    private void Step(float t)
    {
        float eased = 1f - (1f - t) * (1f - t); // Fast at first, slows down at the end (ease-out)
        if (cam == null) cam = Camera.main;     // The camera may appear after Awake
        Vector2 center = HandPosition();

        // Radius that covers the whole screen from the hand
        float h = cam.orthographicSize * 2f;
        float w = h * cam.aspect;
        Vector2 camPos = cam.transform.position;
        float maxRadius = 0f;
        for (int i = 0; i < 4; i++)
        {
            Vector2 corner = camPos + new Vector2((i % 2 == 0 ? -0.5f : 0.5f) * w, (i < 2 ? -0.5f : 0.5f) * h);
            maxRadius = Mathf.Max(maxRadius, Vector2.Distance(center, corner));
        }
        maxRadius += 3f * pixelSize; // Plus the ring width

        // Radius in whole art pixels, so the ring stays on the pixel grid
        float radius = Mathf.Round(eased * maxRadius / pixelSize) * pixelSize;

        // The cut ends one pixel inside the ring, so the ring covers its edge
        mask.transform.position = center;
        mask.transform.localScale = Vector3.one * (2f * Mathf.Max(0f, radius - pixelSize));

        front.transform.position = new Vector3(camPos.x, camPos.y, 0f);
        front.transform.localScale = new Vector3(w + 1f, h + 1f, 1f);
        frontMaterial.SetVector(CenterId, center);
        frontMaterial.SetFloat(RadiusId, radius);
        frontMaterial.SetFloat(IntensityId, 1f - eased * 0.6f); // Bright when it comes out, fades at the end

        shownColor = Color.Lerp(fromColor, toColor, eased);
        if (globalLight != null) globalLight.color = shownColor;
    }

    // Leave everything as WorldObject wants it for the current world
    private void Finish()
    {
        World now = WorldManager.Instance != null ? WorldManager.Instance.CurrentWorld : shownWorld;
        foreach (var (r, _) in appearing) SetMask(r, SpriteMaskInteraction.None);
        foreach (var (r, owner) in disappearing)
        {
            SetMask(r, SpriteMaskInteraction.None);
            r.enabled = owner.ExistsIn(now);
        }
        appearing.Clear();
        disappearing.Clear();

        if (globalLight != null) globalLight.color = toColor;
        shownColor = toColor;
        mask.enabled = false;
        front.enabled = false;
        running = false;
    }

    // Play the amulet frames at the hand (and the raised-arm pose, if there is one)
    private void UpdateAmulet()
    {
        int frame = Mathf.FloorToInt((Time.time - amuletStart) * amuletFps);
        bool show = amuletFrames != null && frame >= 0 && frame < amuletFrames.Length && playerSprite != null;
        amulet.enabled = show;
        if (!show) return;

        amulet.sprite = amuletFrames[frame];
        Vector2 hand = HandPosition();
        amulet.transform.position = new Vector3(Snap(hand.x), Snap(hand.y), playerSprite.transform.position.z);
        amulet.sortingLayerID = playerSprite.sortingLayerID;
        amulet.sortingOrder = playerSprite.sortingOrder + 1;
        if (raisedArmPose != null) playerSprite.sprite = raisedArmPose;
    }

    // Where the raised hand is in the world (it follows the fisherman and his facing)
    private Vector2 HandPosition()
    {
        if (playerSprite == null) return cam.transform.position;
        float side = playerSprite.flipX ? -1f : 1f;
        return playerSprite.transform.TransformPoint(new Vector3(handOffset.x * side, handOffset.y, 0f));
    }

    private float Snap(float v) => Mathf.Round(v / pixelSize) * pixelSize;

    // Only these renderers can be cut by a SpriteMask
    private static bool SetMask(Renderer r, SpriteMaskInteraction interaction)
    {
        switch (r)
        {
            case SpriteRenderer s: s.maskInteraction = interaction; return true;
            case TilemapRenderer t: t.maskInteraction = interaction; return true;
            case ParticleSystemRenderer p: p.maskInteraction = interaction; return true;
            default: return false;
        }
    }

    // White circle 1 unit wide, made in code (no extra asset needed)
    private static Sprite CircleSprite()
    {
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var pixels = new Color32[size * size];
        float c = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = (x - c) * (x - c) + (y - c) * (y - c) <= c * c ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // White square 1 unit wide, for the ring quad
    private static Sprite SquareSprite()
    {
        Texture2D tex = Texture2D.whiteTexture;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
    }
}
