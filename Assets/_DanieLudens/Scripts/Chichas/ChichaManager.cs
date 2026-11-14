using System;
using UnityEngine;

public class ChichaManager : MonoBehaviour
{
    public static ChichaManager Instance { get; private set; } // Singleton access
    public static event Action<int, int> OnChichaCollected;   // Fired on each pickup (collected, total)
    public static event Action OnAllChichasCollected;          // Fired when the last one is collected

    public int Collected { get; private set; } // Chichas picked up so far
    public int Total { get; private set; }     // Chichas in the level

    // Set up the singleton
    void Awake()
    {
        if (Instance != null && Instance != this) // Another manager already exists
        {
            Destroy(gameObject); // Remove the duplicate
            return;
        }
        Instance = this; // Register as the instance
    }

    // Count the chichas in the level
    void Start()
    {
        Total = FindObjectsByType<Chicha>(FindObjectsSortMode.None).Length; // Every active chicha in the scene
    }

    // Register a pickup and notify listeners
    public void Collect()
    {
        Collected++;                                        // One more chicha
        Debug.Log("Chicha collected: " + Collected + "/" + Total); // Temporary feedback for testing
        OnChichaCollected?.Invoke(Collected, Total);        // Tell the UI and other systems

        if (Collected >= Total) // That was the last one
        {
            Debug.Log("All chichas collected!"); // Temporary feedback for testing
            OnAllChichasCollected?.Invoke();     // Start the escape phase
        }
    }

    // Clear the singleton when destroyed
    void OnDestroy()
    {
        if (Instance == this) Instance = null; // Release the instance
    }
}
