using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;   // Pause menu
    [SerializeField] private GameObject optionsPanel; // Volume settings opened from the pause menu

    [Header("Audio UI")]
    [SerializeField] private AudioSource sfxSource; // Plays the click
    [SerializeField] private AudioClip clickSound;  // Button click

    private bool isPaused;        // Game is paused
    private bool canPause = true; // False after the Mohán catches the player

    // Listen for the game over
    void OnEnable()
    {
        MohanMeter.OnMohanCaught += BlockPause; // No pause on the death screen
    }

    // Stop listening when disabled
    void OnDisable()
    {
        MohanMeter.OnMohanCaught -= BlockPause; // Avoid calls on a disabled object
    }

    // Start unpaused with the menus hidden
    void Start()
    {
        Time.timeScale = 1f;                                     // Normal time
        if (pausePanel != null) pausePanel.SetActive(false);     // Hidden
        if (optionsPanel != null) optionsPanel.SetActive(false); // Hidden
    }

    // Toggle the pause with Esc
    void Update()
    {
        if (!canPause || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return; // Nothing to do
        if (isPaused) ResumeGame(); // Esc again: back to the game
        else PauseGame();           // Esc: open the pause menu
    }

    // Play the click sound (buttons)
    public void PlayClickSound()
    {
        if (sfxSource != null && clickSound != null) sfxSource.PlayOneShot(clickSound); // Click
    }

    // Freeze the game and show the pause menu
    public void PauseGame()
    {
        isPaused = true;                                       // Paused
        pausePanel.SetActive(true);                            // Show the menu
        Time.timeScale = 0f;                                   // Freeze physics, timers and animations
        InputSystem.actions.FindActionMap("Player").Disable(); // No moving, jumping or switching while paused
    }

    // Close every menu and continue
    public void ResumeGame()
    {
        isPaused = false;                                        // Playing
        pausePanel.SetActive(false);                             // Hide the menu
        if (optionsPanel != null) optionsPanel.SetActive(false); // Hide the options too
        Time.timeScale = 1f;                                     // Normal time
        InputSystem.actions.FindActionMap("Player").Enable();    // Controls back
    }

    // Open the volume settings (button)
    public void OpenOptions()
    {
        pausePanel.SetActive(false);  // Hide the pause menu
        optionsPanel.SetActive(true); // Show the options
    }

    // Back from the volume settings (button)
    public void CloseOptions()
    {
        optionsPanel.SetActive(false); // Hide the options
        pausePanel.SetActive(true);    // Back to the pause menu
    }

    // Go back to the main menu (button)
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;                                  // Unfreeze before leaving
        InputSystem.actions.FindActionMap("Player").Enable(); // Controls ready for the next scene
        SceneManager.LoadScene(0);                            // Main menu is always scene 0
    }

    // The Mohán caught the player: Esc no longer opens the pause
    private void BlockPause()
    {
        canPause = false; // The death screen owns the UI now
    }
}
