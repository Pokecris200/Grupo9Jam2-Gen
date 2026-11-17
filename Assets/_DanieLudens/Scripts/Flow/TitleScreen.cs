using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class TitleScreen : MonoBehaviour
{
    [Header("Videos (inside StreamingAssets)")]
    [SerializeField] private string loopVideo = "Cinematicas/titulo_loop.mp4";   // Calm scene that repeats
    [SerializeField] private string exitVideo = "Cinematicas/titulo_salida.mp4"; // Title rises, boat leaves
    [SerializeField] private Texture poster;                                      // First frame, shown while the video loads

    [Header("Music")]
    [SerializeField] private AudioSource music;           // Calm loop (titulo_musica)
    [SerializeField] private float musicFadeOut = 3.5f;   // Seconds to fade it out after the key press

    [Header("Prompt")]
    [SerializeField] private string promptText = "Presiona cualquier tecla"; // Bottom text
    [SerializeField] private TMP_FontAsset promptFont;    // RuinaAncestral-Bold SDF
    [SerializeField] private float promptSize = 34f;      // Font size
    [SerializeField] private Color promptColor = new Color(0.96f, 0.86f, 0.55f, 1f); // Warm cream
    [SerializeField] private float blinkSpeed = 2.2f;     // Soft blink
    [SerializeField] private float inputDelay = 0.6f;     // Ignore keys for a moment at start

    [Header("Transition")]
    [SerializeField] private float crossfade = 0.25f;     // Blend from the loop to the exit video
    [SerializeField] private string nextScene = "Main Menu Cristian"; // Loaded in the background

    private VideoPlayer loopPlayer;    // Plays the loop
    private VideoPlayer exitPlayer;    // Plays the exit
    private RenderTexture loopTexture; // Loop frames
    private RenderTexture exitTexture; // Exit frames
    private RawImage loopImage;        // Shows the loop
    private RawImage exitImage;        // Shows the exit (fades in on top)
    private TextMeshProUGUI prompt;    // "Presiona cualquier tecla"
    private AsyncOperation loading;    // Menu loading in the background
    private bool pressed;              // Key already pressed
    private bool leaving;              // Menu already requested
    private float startTime;           // For inputDelay

    // Build the screen, start the videos and preload the menu
    void Start()
    {
        startTime = Time.unscaledTime;                                   // Input delay starts now
        BuildScreen();                                                   // Poster, two video layers and the prompt
        loopPlayer = CreatePlayer(loopVideo, true, out loopTexture);     // Looping video
        exitPlayer = CreatePlayer(exitVideo, false, out exitTexture);    // Exit video, ready but stopped
        loopImage.texture = loopTexture;                                 // Show the loop
        exitImage.texture = exitTexture;                                 // Exit waits hidden
        loopPlayer.prepareCompleted += p => { p.Play(); StartCoroutine(ShowWhenFirstFrame(p, loopImage)); }; // Start the loop
        exitPlayer.loopPointReached += _ => GoToMenu();                  // Exit ends: open the menu
        loopPlayer.errorReceived += (_, m) => Debug.LogWarning("Title loop: " + m);  // Tell the developer
        exitPlayer.errorReceived += (_, m) => { Debug.LogWarning("Title exit: " + m); GoToMenu(); }; // Never get stuck
        loopPlayer.Prepare();                                            // Load the loop
        exitPlayer.Prepare();                                            // Load the exit so it starts instantly

        if (!string.IsNullOrEmpty(nextScene))                            // Preload the menu
        {
            Application.backgroundLoadingPriority = ThreadPriority.Low;  // Keep the video smooth
            loading = SceneManager.LoadSceneAsync(nextScene);            // Load in the background
            loading.allowSceneActivation = false;                        // Wait for the exit video
        }
    }

    // Blink the prompt and wait for any key
    void Update()
    {
        if (pressed || prompt == null) return; // Already leaving

        float blink = Mathf.Sin(Time.unscaledTime * blinkSpeed) * 0.5f + 0.5f; // 0 to 1
        var c = promptColor;                                                   // Base color
        c.a = Mathf.Lerp(0.35f, 1f, blink);                                    // Never fully gone
        prompt.color = c;                                                      // Apply

        if (Time.unscaledTime - startTime > inputDelay && AnyInput()) StartCoroutine(Leave()); // Start the exit
    }

    // Exit video, title rises, music fades, then the menu
    private IEnumerator Leave()
    {
        pressed = true;                                    // Only once
        exitPlayer.Play();                                 // Start the exit video
        float wait = 0f;                                   // Safety timer
        while (exitPlayer.frame < 1 && wait < 1f) { wait += Time.unscaledDeltaTime; yield return null; } // Wait for its first frame

        float startVolume = music != null ? music.volume : 0f; // Music volume now
        float t = 0f;                                          // Timer
        var promptStart = prompt.color;                        // Prompt color now
        while (t < musicFadeOut)                               // Fade everything
        {
            t += Time.unscaledDeltaTime;                                          // Advance
            float k = Mathf.Clamp01(t / crossfade);                               // Crossfade progress
            exitImage.color = new Color(1f, 1f, 1f, k);                           // Exit video on top
            prompt.color = new Color(promptStart.r, promptStart.g, promptStart.b, promptStart.a * (1f - k)); // Prompt out
            if (music != null) music.volume = startVolume * (1f - Mathf.Clamp01(t / musicFadeOut)); // Music out
            yield return null;                                                    // Next frame
        }
        if (music != null) music.Stop(); // Silence

        float limit = (float)exitPlayer.length + 2f - t;                           // Exit should have ended by then
        while (limit > 0f) { limit -= Time.unscaledDeltaTime; yield return null; } // Wait for loopPointReached
        GoToMenu();                                                                 // Safety: never stuck on the title
    }

    // Open the menu (already loaded in the background)
    private void GoToMenu()
    {
        if (leaving) return;                                            // Only once
        leaving = true;                                                 // Remember it
        Application.backgroundLoadingPriority = ThreadPriority.Normal;  // Finish at full speed
        if (loading != null) loading.allowSceneActivation = true;       // Switch as soon as it's ready
        else if (!string.IsNullOrEmpty(nextScene)) SceneManager.LoadScene(nextScene); // Fallback
    }

    // Show a video layer only once it has a real frame (avoids a black flash)
    private IEnumerator ShowWhenFirstFrame(VideoPlayer player, RawImage image)
    {
        float wait = 0f;                                                    // Safety timer
        while (player.frame < 1 && wait < 2f) { wait += Time.unscaledDeltaTime; yield return null; } // First frame
        image.color = Color.white;                                          // Show it over the poster
    }

    // Any key, mouse click or gamepad button
    private static bool AnyInput()
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

    // Video player that draws into its own texture
    private VideoPlayer CreatePlayer(string file, bool loop, out RenderTexture texture)
    {
        var player = gameObject.AddComponent<VideoPlayer>();                    // New player
        player.playOnAwake = false;                                             // We decide when
        player.isLooping = loop;                                                // Loop or once
        player.source = VideoSource.Url;                                        // From StreamingAssets
        player.url = Application.streamingAssetsPath + "/" + file;              // Works in WebGL too
        player.renderMode = VideoRenderMode.RenderTexture;                      // Into a texture
        player.audioOutputMode = VideoAudioOutputMode.None;                     // Videos have no sound
        player.skipOnDrop = true;                                               // Keep in time on slow machines
        texture = new RenderTexture(960, 540, 0) { filterMode = FilterMode.Point }; // Crisp pixel art
        player.targetTexture = texture;                                         // Draw here
        return player;                                                          // Ready
    }

    // Full screen canvas: poster, loop, exit and the prompt
    private void BuildScreen()
    {
        var root = new GameObject("Title Screen");             // UI root
        root.transform.SetParent(transform, false);            // Under this object
        var canvas = root.AddComponent<Canvas>();              // Its own canvas
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;     // Over everything

        var black = NewImage<Image>("Black", root.transform);  // Black bars on screens that aren't 16:9
        black.color = Color.black;                             // Black

        var posterImage = NewImage<RawImage>("Poster", black.transform); // Static first frame
        posterImage.texture = poster;                                    // From the project
        posterImage.color = poster != null ? Color.white : Color.black;  // Black if missing
        Fit(posterImage.rectTransform);                                  // 16:9

        loopImage = NewImage<RawImage>("Loop", black.transform);         // Loop video
        loopImage.color = new Color(1f, 1f, 1f, 0f);                     // Hidden until its first frame
        Fit(loopImage.rectTransform);                                    // 16:9

        exitImage = NewImage<RawImage>("Exit", black.transform);         // Exit video
        exitImage.color = new Color(1f, 1f, 1f, 0f);                     // Hidden until the key press
        Fit(exitImage.rectTransform);                                    // 16:9

        prompt = new GameObject("Prompt").AddComponent<TextMeshProUGUI>(); // Bottom text
        prompt.transform.SetParent(root.transform, false);                 // On top of the videos
        if (promptFont != null) prompt.font = promptFont;                  // Game font
        prompt.text = promptText;                                          // "Presiona cualquier tecla"
        prompt.fontSize = promptSize;                                      // Size
        prompt.alignment = TextAlignmentOptions.Bottom;                    // Centered at the bottom
        prompt.textWrappingMode = TextWrappingModes.NoWrap;                // One line
        prompt.raycastTarget = false;                                      // Doesn't block anything
        var r = prompt.rectTransform;                                      // Position
        r.anchorMin = new Vector2(0f, 0f);                                 // Bottom left
        r.anchorMax = new Vector2(1f, 0f);                                 // Bottom right
        r.pivot = new Vector2(0.5f, 0f);                                   // Bottom center
        r.anchoredPosition = new Vector2(0f, 24f);                         // Small margin
        r.sizeDelta = new Vector2(0f, 60f);                                // Height
    }

    // New UI element filling its parent
    private static T NewImage<T>(string name, Transform parent) where T : Graphic
    {
        var go = new GameObject(name, typeof(RectTransform));  // UI object
        go.transform.SetParent(parent, false);                 // Inside the parent
        var g = go.AddComponent<T>();                          // Image or RawImage
        g.raycastTarget = false;                               // Never blocks input
        var r = g.rectTransform;                               // Fill the parent
        r.anchorMin = Vector2.zero;                            // Bottom left
        r.anchorMax = Vector2.one;                             // Top right
        r.offsetMin = r.offsetMax = Vector2.zero;              // No margins
        return g;                                              // Ready
    }

    // Keep a video layer at 16:9 inside the screen
    private static void Fit(RectTransform r)
    {
        var fitter = r.gameObject.AddComponent<AspectRatioFitter>(); // Keep the shape
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; // Black bars if needed
        fitter.aspectRatio = 16f / 9f;                                // Video shape
    }

    // Free the video textures
    void OnDestroy()
    {
        foreach (var tex in new[] { loopTexture, exitTexture }) // Both textures
        {
            if (tex == null) continue; // Not created
            tex.Release();             // Free GPU memory
            Destroy(tex);              // Remove the object
        }
    }
}
