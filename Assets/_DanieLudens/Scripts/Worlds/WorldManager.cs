using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public enum World { Real, Spirit } // The two possible worlds

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance { get; private set; } // Singleton access
    public static event Action<World> OnWorldChanged;        // Fired when the world changes
    public static event Action OnSwitchBlocked;              // Fired when a switch is not allowed

    [SerializeField] private Light2D globalLight;                                  // Scene global light
    [SerializeField] private Color realWorldColor = Color.white;                   // Light color in Real world
    [SerializeField] private Color spiritWorldColor = new Color(0.45f, 0.6f, 1f); // Light color in Spirit world

    [Header("Switch Check")]
    [SerializeField] private Collider2D playerCollider;  // Player body used to check overlaps
    [SerializeField] private LayerMask solidLayers;      // Layers that can block a switch
    [SerializeField] private float overlapShrink = 0.9f; // Smaller box so touching the floor doesn't count

    public World CurrentWorld { get; private set; } = World.Real; // Active world, starts in Real

    private InputAction switchAction;                                       // "SwitchWorld" input action
    private readonly List<WorldObject> appearing = new List<WorldObject>(); // Objects that would appear on switch

    // Set up the singleton and input action
    void Awake()
    {
        if (Instance != null && Instance != this) // Another manager already exists
        {
            Destroy(gameObject); // Remove the duplicate
            return;
        }
        Instance = this;                                               // Register as the instance
        switchAction = InputSystem.actions.FindAction("SwitchWorld"); // Find the SwitchWorld action
        FindMissingReferences(); // Fill empty references so the prefab works in any scene
    }

    // Find the light and the player by themselves if nothing was assigned
    private void FindMissingReferences()
    {
        if (globalLight == null) // No light assigned in the Inspector
        {
            foreach (Light2D light in FindObjectsByType<Light2D>(FindObjectsSortMode.None)) // Every 2D light in the scene
            {
                if (light.lightType == Light2D.LightType.Global) // Only the global one
                {
                    globalLight = light; // Use it
                    break;
                }
            }
        }

        if (playerCollider == null) // No player assigned in the Inspector
        {
            GameObject player = GameObject.FindWithTag("Player");                  // Find the player by tag
            if (player != null) playerCollider = player.GetComponent<Collider2D>(); // Use its body
        }
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
            SwitchWorld(); // Try to toggle the world
        }
    }

    // Switch to the other world if the player is not inside something solid there
    public void SwitchWorld()
    {
        World target = CurrentWorld == World.Real ? World.Spirit : World.Real; // The world we want to enter

        if (IsBlocked(target)) // Something solid would appear on the player
        {
            Debug.Log("World switch blocked: something solid is in the way"); // Temporary feedback for testing
            OnSwitchBlocked?.Invoke();                                         // Let other systems react (sound, shake)
            return;
        }

        CurrentWorld = target; // Enter the new world
        ApplyWorld();          // Update visuals and notify
    }

    // Check if the player would overlap a solid object that only exists in the target world
    private bool IsBlocked(World target)
    {
        if (playerCollider == null) return false; // No player assigned, allow the switch

        appearing.Clear(); // Reuse the list
        foreach (var wo in WorldObject.All)
        {
            if (wo.ExistsIn(target) && !wo.ExistsIn(CurrentWorld)) appearing.Add(wo); // Only objects that would appear
        }

        foreach (var wo in appearing) wo.SetCollidersEnabled(true); // Turn them on just for the check

        Bounds b = playerCollider.bounds;                                                                 // Player box in world space
        Collider2D[] hits = Physics2D.OverlapBoxAll(b.center, b.size * overlapShrink, 0f, solidLayers); // Colliders inside the player box

        bool blocked = false; // Assume the switch is allowed
        foreach (var hit in hits)
        {
            WorldObject owner = hit.GetComponentInParent<WorldObject>(); // WorldObject that owns this collider
            if (owner != null && appearing.Contains(owner))              // It is one of the appearing objects
            {
                blocked = true; // The player would be stuck
                break;
            }
        }

        foreach (var wo in appearing) wo.SetCollidersEnabled(false); // Turn them back off
        return blocked;
    }

    // Update the light and notify listeners
    private void ApplyWorld()
    {
        if (globalLight != null) globalLight.color = CurrentWorld == World.Real ? realWorldColor : spiritWorldColor; // Pick light color
        OnWorldChanged?.Invoke(CurrentWorld);                                               // Tell subscribers
    }

    // Clear the singleton when destroyed
    void OnDestroy()
    {
        if (Instance == this) Instance = null; // Release the instance
    }
}
