using UnityEngine;

public class WorldObject : MonoBehaviour
{
    public enum Presence { RealOnly, SpiritOnly, Both } // Where this object exists

    [SerializeField] private Presence presence = Presence.RealOnly; // Worlds where the object is active

    private Renderer[] renderers;   // Sprites and tilemaps to show or hide
    private Collider2D[] colliders; // Colliders to turn on or off

    // Cache renderers and colliders, including children
    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);   // All renderers in this object
        colliders = GetComponentsInChildren<Collider2D>(true); // All 2D colliders in this object
    }

    // Subscribe to world changes and sync with the current world
    void OnEnable()
    {
        WorldManager.OnWorldChanged += ApplyWorld;                                         // Listen for world switches
        if (WorldManager.Instance != null) ApplyWorld(WorldManager.Instance.CurrentWorld); // Sync if the manager already exists
    }

    // Stop listening when disabled or destroyed
    void OnDisable()
    {
        WorldManager.OnWorldChanged -= ApplyWorld; // Avoid calls on a disabled object
    }

    // Show or hide the object for the given world
    private void ApplyWorld(World world)
    {
        bool exists = presence == Presence.Both
                      || (presence == Presence.RealOnly && world == World.Real)
                      || (presence == Presence.SpiritOnly && world == World.Spirit); // Does it exist in this world?

        foreach (var r in renderers) r.enabled = exists; // Show or hide visuals
        foreach (var c in colliders) c.enabled = exists; // Turn collisions on or off
    }
}