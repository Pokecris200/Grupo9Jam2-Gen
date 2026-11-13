using System;                           // For Action
using UnityEngine;
using UnityEngine.InputSystem;          // New Input System
using UnityEngine.Rendering.Universal;  // For Light2D

public enum World { Real, Spirit } // The two possible worlds

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance { get; private set; } // Singleton access
    public static event Action<World> OnWorldChanged;        // Fired when the world changes

    [SerializeField] private Light2D globalLight;                                  // Scene global light
    [SerializeField] private Color realWorldColor = Color.white;                   // Light color in Real world
    [SerializeField] private Color spiritWorldColor = new Color(0.45f, 0.6f, 1f); // Light color in Spirit world

    public World CurrentWorld { get; private set; } = World.Real; // Active world, starts in Real

    private InputAction switchAction; // "SwitchWorld" input action

    // Set up the singleton and input action
    void Awake()
    {
        if (Instance != null && Instance != this) // Another manager already exists
        {
            Destroy(gameObject); // Remove the duplicate
            return;
        }
        Instance = this;                                                // Register as the instance
        switchAction = InputSystem.actions.FindAction("SwitchWorld");  // Find the SwitchWorld action
    }

    // Apply the starting world
    void Start()
    {
        ApplyWorld(); // Set initial light and notify listeners
    }

    // Listen for the switch input every frame
    void Update()
    {
        if (switchAction.WasPressedThisFrame()) // Switch pressed this frame
        {
            SwitchWorld(); // Toggle the world
        }
    }

    // Toggle between Real and Spirit worlds
    public void SwitchWorld()
    {
        CurrentWorld = CurrentWorld == World.Real ? World.Spirit : World.Real; // Flip the world
        ApplyWorld();                                                          // Update visuals and notify
    }

    // Update the light and notify listeners
    private void ApplyWorld()
    {
        globalLight.color = CurrentWorld == World.Real ? realWorldColor : spiritWorldColor; // Pick light color
        OnWorldChanged?.Invoke(CurrentWorld);                                               // Tell subscribers
    }

    // Clear the singleton when destroyed
    void OnDestroy()
    {
        if (Instance == this) Instance = null; // Release the instance
    }
}
