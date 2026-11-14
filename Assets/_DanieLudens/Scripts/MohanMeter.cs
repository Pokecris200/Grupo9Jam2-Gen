using System;
using UnityEngine;

public class MohanMeter : MonoBehaviour
{
    public static MohanMeter Instance { get; private set; } // Singleton access
    public static event Action<float> OnMeterChanged;      // Fired while filling (value 0 to 1)
    public static event Action OnMohanCaught;               // Fired when the meter is full (game over)

    [SerializeField] private float fillTime = 20f;       // Seconds in the spirit world to fill the meter
    [SerializeField] private float escapeFillTime = 15f; // Seconds to fill the whole meter in the escape phase

    [Header("Read Only (for testing)")]
    [SerializeField] private float value;        // Current fill from 0 to 1
    [SerializeField] private bool isFilling;     // True while rising (the UI shakes the bar)
    [SerializeField] private bool escapePhase;   // True after all chichas are collected

    public float Value => value;             // Read access for other scripts
    public bool IsFilling => isFilling;      // Read access for the UI
    public bool EscapePhase => escapePhase;  // Read access for other scripts

    private bool caught; // Stops the meter after game over

    // Set up the singleton
    void Awake()
    {
        if (Instance != null && Instance != this) // Another meter already exists
        {
            Destroy(gameObject); // Remove the duplicate
            return;
        }
        Instance = this; // Register as the instance
    }

    // Listen for the moment all chichas are collected
    void OnEnable()
    {
        ChichaManager.OnAllChichasCollected += StartEscape; // Escape phase trigger
    }

    // Stop listening when disabled
    void OnDisable()
    {
        ChichaManager.OnAllChichasCollected -= StartEscape; // Avoid calls on a disabled object
    }

    // Fill the meter in the spirit world, or always during the escape phase
    void Update()
    {
        if (caught) return; // Game already over

        bool inSpirit = WorldManager.Instance != null && WorldManager.Instance.CurrentWorld == World.Spirit; // Player in the spirit world?
        isFilling = escapePhase || inSpirit; // Normal phase: only in spirit. Escape: always
        if (!isFilling) return;              // Real world in normal phase: meter pauses

        float time = escapePhase ? escapeFillTime : fillTime; // Pick the fill speed
        value = Mathf.Min(1f, value + Time.deltaTime / time); // Rise without passing 1
        OnMeterChanged?.Invoke(value);                        // Tell the UI and the Mohán

        if (value >= 1f) // Meter is full
        {
            caught = true;                      // Stop the meter
            isFilling = false;                  // Stop the shake
            Debug.Log("The Mohán caught you!"); // Temporary feedback for testing
            OnMohanCaught?.Invoke();            // Trigger game over
        }
    }

    // Start the escape phase
    private void StartEscape()
    {
        escapePhase = true;                                // Meter now fills in both worlds
        Debug.Log("Escape phase: run to the door!");      // Temporary feedback for testing
    }

    // Clear the singleton when destroyed
    void OnDestroy()
    {
        if (Instance == this) Instance = null; // Release the instance
    }
}
