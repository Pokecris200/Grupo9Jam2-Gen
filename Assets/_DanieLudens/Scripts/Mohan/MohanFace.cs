using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MohanFace : MonoBehaviour
{
    [Header("Apparitions")]
    [SerializeField] private int startAfterChichas = 2;                // Chichas needed before the face starts to appear
    [SerializeField] private Vector2 waitRange = new Vector2(6f, 14f); // Random seconds between apparitions (empty meter)
    [SerializeField] private float showTime = 1.5f;                    // Seconds the face stays visible
    [SerializeField] private float fadeTime = 0.5f;                    // Slide and fade time
    [SerializeField] private float minAlpha = 0.15f;                   // Visibility with an empty meter
    [SerializeField] private float peekDistance = 150f;                // Pixels the face slides in from the edge
    [SerializeField] private AudioSource audioSource;                  // Plays the apparition sound
    [SerializeField] private AudioClip appearSound;                    // Sound when the face shows up

    [Header("Chase")]
    [SerializeField] private float chaseMinPeek = 150f;       // Pixels inside the screen with an empty meter
    [SerializeField] private float chaseMaxPeekRatio = 0.35f; // Part of the screen width it reaches with a full meter
    [SerializeField] private float chaseMinAlpha = 0.35f;     // Visibility at the start of the chase
    [SerializeField] private float chaseMaxScale = 1.8f;      // Size with a full meter
    [SerializeField] private float chaseFadeIn = 1f;          // Seconds to fade in when the chase starts

    [Header("Catch")]
    [SerializeField] private float catchTime = 1.5f;      // Seconds to reach the player (match GameOverManager delay)
    [SerializeField] private float noiseStrength = 80f;   // Erratic movement in pixels
    [SerializeField] private float noiseSpeed = 3f;       // How fast the erratic movement changes
    [SerializeField] private float catchScale = 2.5f;     // Final size when it reaches the player

    private Image image;        // Face image
    private RectTransform rect; // Face position on screen
    private bool active;        // Random apparitions enabled
    private bool chasing;       // Escape phase: the face stays and gets closer
    private bool caught;        // Game over sequence running

    // Cache components and start invisible
    void Awake()
    {
        image = GetComponent<Image>();        // Get the face image
        rect = GetComponent<RectTransform>(); // Get its RectTransform
        SetAlpha(0f);                         // Hidden at the start
    }

    // Listen for chichas, escape and game over
    void OnEnable()
    {
        ChichaManager.OnChichaCollected += HandleCollected; // Start after enough chichas
        EscapeSequence.OnEscapeStarted += StartChase;       // Stay on screen during the escape
        MohanMeter.OnMohanCaught += HandleCaught;           // Catch the player
    }

    // Stop listening when disabled
    void OnDisable()
    {
        ChichaManager.OnChichaCollected -= HandleCollected; // Avoid calls on a disabled object
        EscapeSequence.OnEscapeStarted -= StartChase;       // Avoid calls on a disabled object
        MohanMeter.OnMohanCaught -= HandleCaught;           // Avoid calls on a disabled object
    }

    // Start the apparitions once enough chichas are collected
    private void HandleCollected(int collected, int total)
    {
        if (collected >= startAfterChichas) Activate(); // Enough chichas
    }

    // Turn on the random apparitions (only once)
    private void Activate()
    {
        if (active || chasing || caught) return; // Already running, chasing or game over
        active = true;                           // Mark as running
        StartCoroutine(ApparitionLoop());        // Start appearing at random moments
    }

    // Wait a random time, peek, repeat (until the chase or the game over)
    private IEnumerator ApparitionLoop()
    {
        while (!caught && !chasing) // Only before the escape
        {
            float meter = MohanMeter.Instance != null ? MohanMeter.Instance.Value : 0f;        // Current meter (0 to 1)
            float wait = Random.Range(waitRange.x, waitRange.y) * Mathf.Lerp(1f, 0.4f, meter); // More often as the meter fills
            yield return new WaitForSeconds(wait);                                             // Random pause
            if (caught || chasing) yield break;                                                // Stop if the phase changed
            yield return Peek();                                                               // Show the face
        }
    }

    // Slide in from a side, stay, slide out
    private IEnumerator Peek()
    {
        bool fromLeft = Random.value < 0.5f;                                      // Pick a side
        float y = Random.Range(Screen.height * 0.25f, Screen.height * 0.75f);    // Random height
        rect.localScale = new Vector3(fromLeft ? 1f : -1f, 1f, 1f);               // Face looks toward the center
        float halfWidth = Mathf.Abs(rect.rect.width * rect.lossyScale.x) * 0.5f; // Half the face size in pixels

        Vector2 hidden = new Vector2(fromLeft ? -halfWidth : Screen.width + halfWidth, y);  // Just outside the screen
        Vector2 shown = hidden + new Vector2(fromLeft ? peekDistance : -peekDistance, 0f); // Peeking in

        float meter = MohanMeter.Instance != null ? MohanMeter.Instance.Value : 0f;            // Current meter
        float maxAlpha = Mathf.Lerp(minAlpha, 1f, meter);                                      // More visible with a fuller meter
        if (audioSource != null && appearSound != null) audioSource.PlayOneShot(appearSound); // Apparition sound

        yield return Move(hidden, shown, 0f, maxAlpha, fadeTime); // Slide in and fade in
        yield return new WaitForSeconds(showTime);                 // Stay a moment
        yield return Move(shown, hidden, maxAlpha, 0f, fadeTime); // Slide out and fade out
    }

    // Move and fade the face over time
    private IEnumerator Move(Vector2 from, Vector2 to, float fromAlpha, float toAlpha, float time)
    {
        for (float t = 0f; t < time; t += Time.deltaTime) // Every frame until time
        {
            if (caught || chasing) yield break;           // Stop if the phase changed
            float k = t / time;                           // Progress from 0 to 1
            rect.position = Vector2.Lerp(from, to, k);    // Move
            SetAlpha(Mathf.Lerp(fromAlpha, toAlpha, k));  // Fade
            yield return null;                            // Next frame
        }
        rect.position = to; // End exactly at the target
        SetAlpha(toAlpha);  // End exactly at the target alpha
    }

    // Switch from random apparitions to the chase
    private void StartChase()
    {
        if (caught) return;               // Too late
        chasing = true;                   // Stop the random apparitions
        StopAllCoroutines();              // Cancel any peek in progress
        StartCoroutine(ChaseRoutine());   // Stay on screen and get closer
    }

    // Stay behind the player and get closer as the meter fills
    private IEnumerator ChaseRoutine()
    {
        bool fromLeft = PortalIsToTheRight();                                                  // Come from behind the player
        float seed = Random.value * 100f;                                                      // Different noise each time
        float appear = 0f;                                                                     // Fade in progress (0 to 1)
        if (audioSource != null && appearSound != null) audioSource.PlayOneShot(appearSound); // Chase starts

        while (!caught) // Until game over
        {
            appear = Mathf.MoveTowards(appear, 1f, Time.deltaTime / chaseFadeIn);  // Fade in at the start
            float meter = MohanMeter.Instance != null ? MohanMeter.Instance.Value : 0f; // Current meter

            float scale = Mathf.Lerp(1f, chaseMaxScale, meter);                        // Bigger as it gets closer
            rect.localScale = new Vector3(fromLeft ? scale : -scale, scale, 1f);       // Face looks toward the center
            float halfWidth = Mathf.Abs(rect.rect.width * rect.lossyScale.x) * 0.5f;  // Half the face size in pixels

            float peek = Mathf.Lerp(chaseMinPeek, Screen.width * chaseMaxPeekRatio, meter);     // How far inside the screen
            float x = fromLeft ? -halfWidth + peek : Screen.width + halfWidth - peek;           // Position from its side
            float nx = (Mathf.PerlinNoise(seed, Time.time * noiseSpeed) - 0.5f) * 2f;           // Smooth random X (-1 to 1)
            float ny = (Mathf.PerlinNoise(seed + 50f, Time.time * noiseSpeed) - 0.5f) * 2f;     // Smooth random Y (-1 to 1)
            Vector2 wobble = new Vector2(nx, ny) * noiseStrength * 0.5f;                        // Restless movement

            rect.position = new Vector2(x, Screen.height * 0.5f) + wobble;  // Stalking from the side
            SetAlpha(Mathf.Lerp(chaseMinAlpha, 1f, meter) * appear);        // More visible as the meter fills
            yield return null;                                              // Next frame
        }
    }

    // Check if the exit portal is to the right of the player
    private bool PortalIsToTheRight()
    {
        ExitPortal portal = FindFirstObjectByType<ExitPortal>();             // Find the portal
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player"); // Find the player
        if (portal == null || playerObject == null) return Random.value < 0.5f; // Random side if something is missing
        return portal.transform.position.x > playerObject.transform.position.x; // True if the portal is to the right
    }

    // Start the catch sequence
    private void HandleCaught()
    {
        caught = true;                   // Stop everything else
        StopAllCoroutines();             // Cancel any peek or chase in progress
        StartCoroutine(CatchRoutine());  // Rush to the player
    }

    // Rush toward the player with erratic movement, growing and fully visible
    private IEnumerator CatchRoutine()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player"); // Find the player
        bool visible = image.color.a > 0.05f;                                  // Was the face already on screen?
        bool fromLeft = Random.value < 0.5f;                                   // Side to come from if it was hidden
        Vector2 start = visible ? (Vector2)rect.position : new Vector2(fromLeft ? 0f : Screen.width, Screen.height * 0.5f); // Start where it is
        float startScale = Mathf.Abs(rect.localScale.x);                       // Current size
        float startAlpha = image.color.a;                                      // Current visibility
        float seed = Random.value * 100f;                                      // Different noise each time
        if (audioSource != null && appearSound != null) audioSource.PlayOneShot(appearSound); // Scream

        for (float t = 0f; t < catchTime; t += Time.deltaTime) // Every frame until catchTime
        {
            float k = t / catchTime; // Progress from 0 to 1
            Vector2 target = playerObject != null
                ? (Vector2)Camera.main.WorldToScreenPoint(playerObject.transform.position) // Player on screen
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);                 // Screen center as fallback

            Vector2 basePosition = Vector2.Lerp(start, target, k * k);                      // Speeds up as it gets closer
            float nx = (Mathf.PerlinNoise(seed, Time.time * noiseSpeed) - 0.5f) * 2f;       // Smooth random X (-1 to 1)
            float ny = (Mathf.PerlinNoise(seed + 50f, Time.time * noiseSpeed) - 0.5f) * 2f; // Smooth random Y (-1 to 1)
            Vector2 wobble = new Vector2(nx, ny) * noiseStrength * (1f - k);                 // Calmer near the player

            rect.position = basePosition + wobble;                                     // Erratic approach
            rect.localScale = Vector3.one * Mathf.Lerp(startScale, catchScale, k);     // Grows as it approaches
            SetAlpha(Mathf.Lerp(startAlpha, 1f, k));                                   // Fully visible at the end
            yield return null;                                                         // Next frame
        }
    }

    // Change only the image transparency
    private void SetAlpha(float alpha)
    {
        Color c = image.color; // Copy the color
        c.a = alpha;           // Set the transparency
        image.color = c;       // Apply it
    }
}
