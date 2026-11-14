using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;       // Panel with the buttons
    [SerializeField] private TextMeshProUGUI tipText;        // Random tip shown on the panel
    [SerializeField] private float delayBeforePanel = 1.5f;  // Time for the Mohán to show up before the panel
    [SerializeField] private string mainMenuScene = "Interfaz-Juan"; // Main menu scene name
    [SerializeField] private string[] tips =
    {
        "No dejes que la barra se llene por completo...",
        "El mundo espiritual guarda lo que buscas, pero no te quedes demasiado.",
        "Escucha el silbido: el Mohán nunca está lejos.",
        "Las chichas brillan en el mundo real. Síguelas."
    }; // Random tips that never say exactly why the player died

    // Hide the panel at the start
    void Awake()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false); // Hidden until game over
    }

    // Listen for the moment the Mohán catches the player
    void OnEnable()
    {
        MohanMeter.OnMohanCaught += HandleCaught; // Meter is full
    }

    // Stop listening when disabled
    void OnDisable()
    {
        MohanMeter.OnMohanCaught -= HandleCaught; // Avoid calls on a disabled object
    }

    // Start the game over sequence
    private void HandleCaught()
    {
        StartCoroutine(GameOverRoutine()); // Run the timed sequence
    }

    // Stop the player, wait, then show the panel
    private IEnumerator GameOverRoutine()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>(); // Find the player
        if (player != null)
        {
            player.enabled = false; // No more movement or jumping
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();                  // Player physics
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // Stop sliding, keep falling
        }
        if (WorldManager.Instance != null) WorldManager.Instance.enabled = false; // No more world switching

        yield return new WaitForSeconds(delayBeforePanel); // Time for the Mohán face (added later)

        if (tipText != null && tips.Length > 0) tipText.text = tips[Random.Range(0, tips.Length)]; // Pick a tip
        if (gameOverPanel != null) gameOverPanel.SetActive(true);                                 // Show the panel
    }

    // Reload the current level (button)
    public void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // Same level from the start
    }

    // Go back to the main menu (button)
    public void GoToMainMenu()
    {
        SceneManager.LoadScene(mainMenuScene); // Load the menu scene
    }
}
