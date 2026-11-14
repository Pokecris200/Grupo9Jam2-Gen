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

    [Header("Catch")]
    [SerializeField] private float catchTime = 1.5f;      // Seconds to reach the player (match GameOverManager delay)
    [SerializeField] private float noiseStrength = 80f;   // Erratic movement in pixels
    [SerializeField] private float noiseSpeed = 3f;       // How fast the erratic movement changes
    [SerializeField] private float catchScale = 2.5f;     // Final size when it reaches the player

    private Image image;      // Face image
    private RectTransform rect; // Face position on screen
    private bool active;      // Apparitions enabled
    private bool caught;      // Game over sequence running

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
        EscapeSequence.OnEscapeStarted += Activate;         // Always active during the escape
        MohanMeter.OnMohanCaught += HandleCaught;           // Catch the player
    }

    // Stop listening when disabled
    void OnDisable()
    {
        ChichaManager.OnChichaCollected -= HandleCollected; // Avoid calls on a disabled object
        EscapeSequence.OnEscapeStarted -= Activate;         // Avoid calls on a disabled object
        MohanMeter.OnMohanCaught -= HandleCaught;           // Avoid calls on a disabled object
    }

    // Start the apparitions once enough chichas are collected
    private void HandleCollected(int collected, int total)
    {
        if (collected >= startAfterChichas) Activate(); // Enough chichas
    }

    // Turn on the apparition loop (only once)
    private void Activate()
    {
        if (active || caught) return;      // Already running or game over
        active = true;                     // Mark as running
        StartCoroutine(ApparitionLoop());  // Start appearing at random moments
    }

    // Wait a random time, peek, repeat
    private IEnumerator ApparitionLoop()
    {
        while (!caught) // Until game over
        {
            float meter = MohanMeter.Instance != null ? MohanMeter.Instance.Value : 0f;           // Current meter (0 to 1)
            float wait = Random.Range(waitRange.x, waitRange.y) * Mathf.Lerp(1f, 0.4f, meter);    // More often as the meter fills
            yield return new WaitForSeconds(wait);                                                // Random pause
            if (caught) yield break;                                                              // Stop if caught meanwhile
            yield return Peek();                                                                  // Show the face
        }
    }

    // Slide in from a side, stay, slide out
    private IEnumerator Peek()
    {
        bool fromLeft = Random.value < 0.5f;                                   // Pick a side
        float y = Random.Range(Screen.height * 0.25f, Screen.height * 0.75f); // Random height
        float halfWidth = Mathf.Abs(rect.rect.width * rect.lossyScale.x) * 0.5f; // Half the face size in pixels

        Vector2 hidden = new Vector2(fromLeft ? -halfWidth : Screen.width + halfWidth, y);   // Just outside the screen
        Vector2 shown = hidden + new Vector2(fromLeft ? peekDistance : -peekDistance, 0f);  // Peeking in
        rect.localScale = new Vector3(fromLeft ? 1f : -1f, 1f, 1f);                         // Face looks toward the center

        float meter = MohanMeter.Instance != null ? MohanMeter.Instance.Value : 0f; // Current meter
        float maxAlpha = Mathf.Lerp(minAlpha, 1f, meter);                           // More visible with a fuller meter
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
            if (caught) yield break;                          // Stop if caught meanwhile
            float k = t / time;                               // Progress from 0 to 1
            rect.position = Vector2.Lerp(from, to, k);        // Move
            SetAlpha(Mathf.Lerp(fromAlpha, toAlpha, k));      // Fade
            yield return null;                                // Next frame
        }
        rect.position = to;  // End exactly at the target
        SetAlpha(toAlpha);   // End exactly at the target alpha
    }

    // Start the catch sequence
    private void HandleCaught()
    {
        caught = true;                    // Stop the apparitions
        StopAllCoroutines();              // Cancel any peek in progress
        StartCoroutine(CatchRoutine());   // Rush to the player
    }

    // Rush toward the player with erratic movement, growing and fully visible
    private IEnumerator CatchRoutine()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player"); // Find the player
        bool fromLeft = Random.value < 0.5f;                                   // Pick a side to come from
        Vector2 start = new Vector2(fromLeft ? 0f : Screen.width, Screen.height * 0.5f); // Screen edge
        float seed = Random.value * 100f;                                      // Different noise each time
        float startAlpha = image.color.a;                                      // Current visibility
        if (audioSource != null && appearSound != null) audioSource.PlayOneShot(appearSound); // Scream

        for (float t = 0f; t < catchTime; t += Time.deltaTime) // Every frame until catchTime
        {
            float k = t / catchTime; // Progress from 0 to 1
            Vector2 target = playerObject != null
                ? (Vector2)Camera.main.WorldToScreenPoint(playerObject.transform.position) // Player on screen
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);                 // Screen center as fallback

            Vector2 basePosition = Vector2.Lerp(start, target, k * k); // Speeds up as it gets closer
            float nx = (Mathf.PerlinNoise(seed, Time.time * noiseSpeed) - 0.5f) * 2f;       // Smooth random X (-1 to 1)
            float ny = (Mathf.PerlinNoise(seed + 50f, Time.time * noiseSpeed) - 0.5f) * 2f; // Smooth random Y (-1 to 1)
            Vector2 wobble = new Vector2(nx, ny) * noiseStrength * (1f - k);                 // Calmer near the player

            rect.position = basePosition + wobble;                          // Erratic approach
            rect.localScale = Vector3.one * Mathf.Lerp(1f, catchScale, k);  // Grows as it approaches
            SetAlpha(Mathf.Lerp(startAlpha, 1f, k));                        // Fully visible at the end
            yield return null;                                              // Next frame
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
