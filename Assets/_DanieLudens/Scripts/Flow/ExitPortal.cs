using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitPortal : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer portalRenderer;                 // Portal sprite
    [SerializeField] private Sprite closedSprite;                           // Closed door art (optional)
    [SerializeField] private Sprite openSprite;                             // Open door art (optional)
    [SerializeField] private Color closedColor = Color.gray;                // Placeholder color when closed
    [SerializeField] private Color openColor = new Color(0.4f, 1f, 0.6f);  // Placeholder color when open
    [SerializeField] private GameObject openGlow;                           // Glow shown when open (optional)

    [Header("Level")]
    [SerializeField] private string nextSceneName; // Scene to load; empty = next scene in the build list

    private bool isOpen; // Portal can be used
    private bool used;   // Prevents loading twice

    // Start closed
    void Awake()
    {
        SetOpen(false); // Closed until the escape starts
    }

    // Listen for the escape phase
    void OnEnable()
    {
        EscapeSequence.OnEscapeStarted += Open; // Open when the escape starts
    }

    // Stop listening when disabled
    void OnDisable()
    {
        EscapeSequence.OnEscapeStarted -= Open; // Avoid calls on a disabled object
    }

    // Open the portal
    private void Open()
    {
        SetOpen(true);                    // Change the look and allow entry
        Debug.Log("Exit portal opened"); // Temporary feedback for testing
    }

    // Change the portal between closed and open
    private void SetOpen(bool open)
    {
        isOpen = open; // Save the state

        Sprite art = open ? openSprite : closedSprite;                    // Art for this state
        if (art != null) portalRenderer.sprite = art;                      // Use the real art if assigned
        portalRenderer.color = art != null ? Color.white : (open ? openColor : closedColor); // Placeholder color otherwise

        if (openGlow != null) openGlow.SetActive(open); // Glow only when open
    }

    // Load the next level when the player is inside the open portal
    void OnTriggerStay2D(Collider2D other)
    {
        if (!isOpen || used || !other.CompareTag("Player")) return; // Closed, already used or not the player
        used = true;                                                // Only once
        LoadNextLevel();                                            // Go to the next level
    }

    // Load the next scene by name or by build order
    private void LoadNextLevel()
    {
        if (!string.IsNullOrEmpty(nextSceneName)) // A scene name was set
        {
            SceneManager.LoadScene(nextSceneName); // Load it
            return;
        }

        int next = SceneManager.GetActiveScene().buildIndex + 1; // Next scene in the build list
        if (next < SceneManager.sceneCountInBuildSettings)       // There is one more level
        {
            SceneManager.LoadScene(next); // Load it
        }
        else
        {
            Debug.Log("No more levels: game completed!"); // Last level finished
        }
    }
}
