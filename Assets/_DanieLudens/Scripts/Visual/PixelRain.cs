using UnityEngine;

public class PixelRain : MonoBehaviour
{
    [SerializeField] private Material rainMaterial;                 // Material with the Mohan/PixelRain shader
    [SerializeField] private string sortingLayer = "Primer_Plano";  // Layer where this rain is drawn
    [SerializeField] private int sortingOrder = 50;                 // Order inside that layer
    [SerializeField] private float margin = 1f;                     // Extra units around the screen

    [Header("Spirit World")]
    [SerializeField] private float spiritSpeed = 0.12f;                            // Rain speed in the spirit world (1 = normal)
    [SerializeField] private Color spiritTint = new Color(0.7f, 0.8f, 1f, 1f);     // Rain color in the spirit world
    [SerializeField] private float blendTime = 0.6f;                               // Seconds to change between worlds

    private static readonly int TimeId = Shader.PropertyToID("_RainTime"); // Shader property IDs
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Camera cam;          // Camera to cover
    private Material material;   // Own copy of the material
    private Color normalColor;   // Color in the real world
    private float rainTime;      // Time that runs slower in the spirit world
    private float spiritAmount;  // 0 = real world, 1 = spirit world
    private float targetAmount;  // Where spiritAmount is going

    // Create the quad that draws the rain
    void Awake()
    {
        var sr = gameObject.AddComponent<SpriteRenderer>();                                        // Quad renderer
        Texture2D tex = Texture2D.whiteTexture;                                                    // Plain white texture
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width); // 1 unit square
        material = new Material(rainMaterial);                                                     // Own copy, so each layer can change
        sr.sharedMaterial = material;                                                              // Use it
        sr.sortingLayerName = sortingLayer;                                                        // In front of or behind the level
        sr.sortingOrder = sortingOrder;                                                            // Order inside that layer
        normalColor = material.GetColor(ColorId);                                                  // Remember the real world color
    }

    // Listen for the world changes
    void OnEnable()
    {
        WorldManager.OnWorldChanged += HandleWorldChanged; // Slow down in the spirit world
    }

    // Stop listening when disabled
    void OnDisable()
    {
        WorldManager.OnWorldChanged -= HandleWorldChanged; // Avoid calls on a disabled object
    }

    // Find the camera
    void Start()
    {
        cam = Camera.main; // Camera that shows the level
    }

    // Remember which world we are going to
    private void HandleWorldChanged(World world)
    {
        targetAmount = world == World.Spirit ? 1f : 0f; // Spirit world slows the rain
    }

    // Cover the screen and move the rain
    void LateUpdate()
    {
        if (cam == null) cam = Camera.main; // The camera may appear later
        if (cam == null) return;            // Nothing to cover

        float h = cam.orthographicSize * 2f;                                                          // Screen height in units
        float w = h * cam.aspect;                                                                     // Screen width in units
        transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);    // Follow the camera
        transform.localScale = new Vector3(w + margin * 2f, h + margin * 2f, 1f);                    // Cover the whole screen

        spiritAmount = Mathf.MoveTowards(spiritAmount, targetAmount, Time.deltaTime / blendTime); // Smooth change between worlds
        rainTime += Time.deltaTime * Mathf.Lerp(1f, spiritSpeed, spiritAmount);                  // Slower in the spirit world
        material.SetFloat(TimeId, rainTime);                                                     // Move the drops
        material.SetColor(ColorId, Color.Lerp(normalColor, normalColor * spiritTint, spiritAmount)); // Tint in the spirit world
    }
}
