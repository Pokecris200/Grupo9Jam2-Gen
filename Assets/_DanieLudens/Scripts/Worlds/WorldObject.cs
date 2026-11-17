using System.Collections.Generic;
using UnityEngine;

public class WorldObject : MonoBehaviour
{
    public enum Presence { RealOnly, SpiritOnly, Both } // Where this object exists

    public static readonly List<WorldObject> All = new List<WorldObject>(); // Every active WorldObject in the scene

    [SerializeField] private Presence presence = Presence.RealOnly; // Worlds where the object is active

    private Renderer[] renderers;   // Sprites and tilemaps to show or hide
    private Collider2D[] colliders; // Colliders to turn on or off

    // Cache renderers and colliders, including children
    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);   // All renderers in this object
        colliders = GetComponentsInChildren<Collider2D>(true); // All 2D colliders in this object
    }

    // Register, subscribe and sync with the current world
    void OnEnable()
    {
        All.Add(this);                                                                     // Register in the global list
        WorldManager.OnWorldChanged += ApplyWorld;                                         // Listen for world switches
        if (WorldManager.Instance != null) ApplyWorld(WorldManager.Instance.CurrentWorld); // Sync if the manager already exists
    }

    // Unregister and stop listening
    void OnDisable()
    {
        All.Remove(this);                          // Remove from the global list
        WorldManager.OnWorldChanged -= ApplyWorld; // Avoid calls on a disabled object
    }

    // Check if this object exists in the given world
    public bool ExistsIn(World world)
    {
        return presence == Presence.Both
            || (presence == Presence.RealOnly && world == World.Real)
            || (presence == Presence.SpiritOnly && world == World.Spirit); // Compare presence with the world
    }

    // Turn all colliders of this object on or off
    public void SetCollidersEnabled(bool on)
    {
        foreach (var c in colliders) c.enabled = on; // Apply to every collider
    }

    // Show or hide the object for the given world
    private void ApplyWorld(World world)
    {
        bool exists = ExistsIn(world);                   // Does it exist in this world?
        foreach (var r in renderers) r.enabled = exists; // Show or hide visuals
        SetCollidersEnabled(exists);                     // Turn collisions on or off
    }
}