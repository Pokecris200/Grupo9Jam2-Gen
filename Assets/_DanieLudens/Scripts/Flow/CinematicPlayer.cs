using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class CinematicPlayer : MonoBehaviour
{
    public enum Trigger { OnStart, OnMohanCaught, Manual } // When the cinematic starts by itself

    [Header("Video")]
    [SerializeField] private string videoFile = "Cinematicas/intro_juego.mp4"; // Path inside StreamingAssets
    [SerializeField] private Trigger playWhen = Trigger.OnStart;                  // When it starts by itself
    [SerializeField] private float delay = 0f;                                    // Seconds to wait before playing (e.g. the Mohán catch animation)
    [SerializeField, Range(0f, 1f)] private float volume = 1f;                    // Video sound volume
    [SerializeField] private bool pauseGameAudio = true;                          // Pause the game's music and sounds while it plays

    [Header("Skip")]
    [SerializeField] private bool canSkip = true;                          // Any key, click or gamepad button skips it
    [SerializeField] private float skipAfter = 0.5f;                       // Seconds before skipping works (so the "Play" click doesn't skip it)
    [SerializeField] private string skipHint = "Cualquier tecla: saltar";  // Small text in a corner; empty = no text
    [SerializeField] private string loadingText = "Cargando...";           // Shown if the video ends before the next scene is loaded

    [Header("When it ends")]
    [SerializeField] private string nextScene = "";       // Scene to load by name; empty = stay in this scene
    [SerializeField] private bool preloadNextScene = true; // Load the next scene in the background while the video plays
    [SerializeField] private UnityEvent onFinished;        // Extra things to do at the end (optional)

    private VideoPlayer player;        // Plays the file into a texture
    private RenderTexture texture;     // Frame of the video
    private GameObject screen;         // Canvas that shows the texture over everything
    private RawImage videoImage;       // Where the frames are drawn
    private Text hint;                 // "Skip" / "Loading" text
    private AsyncOperation loading;    // Next scene loading in the background
    private bool started;              // Play() was called
    private bool finished;             // Already ended or skipped
    private float startTime;           // Real time when it started (for skipAfter)

    // Listen for the game over if this is a defeat cinematic
    void OnEnable()
    {
        if (playWhen == Trigger.OnMohanCaught) MohanMeter.OnMohanCaught += Play; // Game over
    }

    // Stop listening when disabled
    void OnDisable()
    {
        if (playWhen == Trigger.OnMohanCaught) MohanMeter.OnMohanCaught -= Play; // Avoid calls on a disabled object
    }

    // Start right away if this is an intro or ending scene
    void Start()
    {
        if (playWhen == Trigger.OnStart) Play(); // Intro and victory
    }

    // Start the cinematic (also callable from a button or another script)
    public void Play()
    {
        if (started) return;            // Only once
        started = true;                 // Remember it
        StartCoroutine(PlayRoutine());  // Wait, build the screen and prepare the video
    }

    // Wait, cover the screen, start loading the next scene and prepare the video
    private IEnumerator PlayRoutine()
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay); // Let the catch animation play

        BuildScreen();                                  // Black screen right away, so the game doesn't peek while the video loads
        if (pauseGameAudio) AudioListener.pause = true; // Silence the game

        if (preloadNextScene && !string.IsNullOrEmpty(nextScene)) // Use the video as a loading screen
        {
            Application.backgroundLoadingPriority = ThreadPriority.Low; // Keep the video smooth while loading
            loading = SceneManager.LoadSceneAsync(nextScene);           // Load in the background
            loading.allowSceneActivation = false;                       // Don't switch until the video ends
        }

        player = gameObject.AddComponent<VideoPlayer>();                       // Video player
        player.playOnAwake = false;                                            // Wait until it's prepared
        player.isLooping = false;                                              // Play once
        player.source = VideoSource.Url;                                       // File from StreamingAssets
        player.url = Application.streamingAssetsPath + "/" + videoFile;        // In WebGL this is a web address
        player.renderMode = VideoRenderMode.RenderTexture;                     // Draw into a texture
        texture = new RenderTexture(960, 540, 0) { filterMode = FilterMode.Point }; // Point = crisp pixel art
        player.targetTexture = texture;                                        // Where the frames go
        videoImage.texture = texture;                                          // Show it on the screen
        player.audioOutputMode = VideoAudioOutputMode.Direct;                  // The only audio mode that also works in WebGL
        player.SetDirectAudioVolume(0, volume);                                // Video volume
        player.loopPointReached += _ => Finish();                              // Ended
        player.errorReceived += (_, message) =>                                // Could not play
        {
            Debug.LogWarning("Cinematic could not play (" + player.url + "): " + message); // Tell the developer
            Finish();                                                                       // Never leave the player stuck on a black screen
        };
        player.prepareCompleted += p =>  // Ready
        {
            videoImage.color = Color.white; // Show the video
            p.Play();                       // Start
        };
        player.Prepare();                // Load the first frames
        startTime = Time.unscaledTime;   // For skipAfter (works even if the game is paused)
    }

    // Show the hint and listen for the skip input
    void Update()
    {
        if (!started || player == null) return; // Nothing playing yet

        if (finished) // Video over: waiting for the next scene
        {
            if (hint != null) { hint.enabled = true; hint.text = loadingText; } // "Cargando..."
            return;
        }

        float elapsed = Time.unscaledTime - startTime;                          // Real seconds since it started
        if (hint != null) hint.enabled = canSkip && elapsed > 1f;               // Hint appears after a second
        if (canSkip && elapsed > skipAfter && SkipPressed()) Finish();          // Skip
    }

    // Any key, mouse click or gamepad button
    private static bool SkipPressed()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true; // Keyboard
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;    // Mouse
        if (Gamepad.current != null)                                                              // Gamepad
        {
            foreach (var control in Gamepad.current.allControls)                                  // Every control
            {
                if (control is UnityEngine.InputSystem.Controls.ButtonControl b && b.wasPressedThisFrame) return true; // Any button
            }
        }
        return false; // Nothing pressed
    }

    // Stop the video and go on (next scene or back to the game)
    private void Finish()
    {
        if (finished) return;                             // Only once
        finished = true;                                  // Remember it
        if (player != null) player.Stop();               // Stop the video
        if (pauseGameAudio) AudioListener.pause = false;  // Game sound back
        onFinished?.Invoke();                             // Extra actions

        if (loading != null) // Next scene was loading in the background
        {
            if (videoImage != null) videoImage.color = Color.black;      // Keep the screen black while it finishes
            Application.backgroundLoadingPriority = ThreadPriority.Normal; // Finish loading at full speed
            loading.allowSceneActivation = true;                           // Switch as soon as it's ready
            return;                                                        // The new scene removes this one
        }

        CleanUp(); // Remove the screen and free the texture
        if (!string.IsNullOrEmpty(nextScene)) SceneManager.LoadScene(nextScene); // Load it now
    }

    // Remove the screen and free the video texture
    private void CleanUp()
    {
        if (screen != null) Destroy(screen); // Remove the canvas
        if (texture != null)                 // Free the video memory
        {
            texture.Release(); // Free the GPU memory
            Destroy(texture);  // Remove the object
        }
    }

    // Free the texture if the scene changes while the video plays
    void OnDestroy()
    {
        CleanUp(); // Nothing left behind
    }

    // Full screen canvas: black background + the video keeping 16:9 + the skip hint
    private void BuildScreen()
    {
        screen = new GameObject("Cinematic Screen");   // Root of the video UI
        var canvas = screen.AddComponent<Canvas>();     // Its own canvas
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Over everything
        canvas.sortingOrder = 1000;                     // Above the HUD and the game over panel
        screen.AddComponent<GraphicRaycaster>();        // Blocks clicks to the buttons underneath

        var background = new GameObject("Black").AddComponent<Image>(); // Black background
        background.transform.SetParent(screen.transform, false);         // Inside the canvas
        background.color = Color.black;                                  // Black
        Stretch(background.rectTransform);                               // Whole screen

        videoImage = new GameObject("Video").AddComponent<RawImage>(); // Video frame
        videoImage.transform.SetParent(background.transform, false);    // On the black background
        videoImage.color = Color.black;                                 // Black until the first frame is ready
        Stretch(videoImage.rectTransform);                              // Whole screen
        var fitter = videoImage.gameObject.AddComponent<AspectRatioFitter>(); // Keep 16:9
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;        // Black bars on screens that aren't 16:9
        fitter.aspectRatio = 16f / 9f;                                       // Video shape

        if (string.IsNullOrEmpty(skipHint) && string.IsNullOrEmpty(loadingText)) return; // No text needed

        hint = new GameObject("Hint").AddComponent<Text>();                 // Small corner text
        hint.transform.SetParent(screen.transform, false);                   // Inside the canvas
        hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Built-in font
        hint.text = skipHint;                                                // "Skip" text
        hint.fontSize = 22;                                                  // Small
        hint.alignment = TextAnchor.LowerRight;                              // Bottom right corner
        hint.color = new Color(0.91f, 0.89f, 0.81f, 0.6f);                   // Soft cream
        hint.raycastTarget = false;                                          // Doesn't block clicks
        hint.enabled = false;                                                // Hidden at first
        var r = hint.rectTransform;                                          // Position
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 0f);           // Bottom right
        r.anchoredPosition = new Vector2(-24f, 16f);                         // Small margin
        r.sizeDelta = new Vector2(500f, 40f);                                // Room for the text
    }

    // Make a RectTransform fill its parent
    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;                // Bottom left
        r.anchorMax = Vector2.one;                 // Top right
        r.offsetMin = r.offsetMax = Vector2.zero;  // No margins
    }
}
