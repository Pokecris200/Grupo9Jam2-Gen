using System;
using TMPro;
using UnityEngine;

public class Altar : MonoBehaviour
{
    public static event Action OnChichasDelivered; // Fired when the player delivers all chichas

    [Header("Distances (tiles)")]
    [SerializeField] private float showCountDistance = 4f; // Distance to show the counter and thoughts
    [SerializeField] private float deliverDistance = 1f;   // Distance to deliver the chichas

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer altarRenderer; // Altar sprite
    [SerializeField] private Sprite deliveredSprite;       // Altar with chichas around it
    [SerializeField] private GameObject aura;              // Glow shown when all chichas are collected
    [SerializeField] private TextMeshPro counterText;      // Counter above the altar (e.g. 2/5)

    [Header("Thoughts")]
    [SerializeField] private string[] missingChichasLines = { "Debo conseguir más chichas y traerlas", "Aún me faltan chichas", "El altar pide más chichas" }; // Random lines near the altar
    [SerializeField] private string[] allCollectedLines = { "Ya tengo todas las chichas, debo llevarlas al altar" };                                   // Line when the last chicha is collected
    [SerializeField] private float thoughtCooldown = 6f; // Seconds between thoughts near the altar

    private Transform player;      // Player position
    private bool allCollected;     // All chichas picked up
    private bool delivered;        // Chichas already delivered
    private bool playerWasNear;    // Player was near on the last frame
    private float nextThoughtTime; // When the next thought is allowed

    // Hide the aura and the counter at the start
    void Awake()
    {
        if (aura != null) aura.SetActive(false);                        // No glow yet
        if (counterText != null) counterText.gameObject.SetActive(false); // No counter yet
    }

    // Find the player
    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player"); // Search by tag
        if (p != null) player = p.transform;                       // Save its transform
    }

    // Listen for chicha pickups
    void OnEnable()
    {
        ChichaManager.OnChichaCollected += HandleCollected;        // Update the counter
        ChichaManager.OnAllChichasCollected += HandleAllCollected; // Start glowing
    }

    // Stop listening when disabled
    void OnDisable()
    {
        ChichaManager.OnChichaCollected -= HandleCollected;        // Avoid calls on a disabled object
        ChichaManager.OnAllChichasCollected -= HandleAllCollected; // Avoid calls on a disabled object
    }

    // Check the player's distance every frame
    void Update()
    {
        if (player == null || delivered) return; // Nothing to do

        bool inRealWorld = WorldManager.Instance == null || WorldManager.Instance.CurrentWorld == World.Real; // Altar only exists in the real world
        float distance = Vector2.Distance(player.position, transform.position);                             // Distance in tiles
        bool near = inRealWorld && distance <= showCountDistance;                                             // Close enough to see the counter

        if (near && !playerWasNear) // Player just arrived
        {
            UpdateCounter(); // Refresh the numbers
            if (!allCollected && Time.time >= nextThoughtTime) // Missing chichas and cooldown passed
            {
                PlayerThoughts.Instance?.SayRandom(missingChichasLines); // Think aloud
                nextThoughtTime = Time.time + thoughtCooldown;           // Wait before the next thought
            }
        }

        if (counterText != null) counterText.gameObject.SetActive(near); // Show the counter only when near
        playerWasNear = near;                                            // Remember for the next frame

        if (allCollected && inRealWorld && distance <= deliverDistance) Deliver(); // Close enough to deliver
    }

    // Refresh the counter when a chicha is collected
    private void HandleCollected(int collected, int total)
    {
        UpdateCounter(); // Show the new numbers
    }

    // Start glowing when the last chicha is collected
    private void HandleAllCollected()
    {
        allCollected = true;                                   // Ready for delivery
        if (aura != null) aura.SetActive(true);                // Turn on the glow
        PlayerThoughts.Instance?.SayRandom(allCollectedLines); // Tell the player what to do
    }

    // Deliver the chichas and start the escape sequence
    private void Deliver()
    {
        delivered = true;                                                       // Only once
        if (deliveredSprite != null) altarRenderer.sprite = deliveredSprite;   // Altar with chichas
        if (aura != null) aura.SetActive(false);                                // Glow off
        if (counterText != null) counterText.gameObject.SetActive(false);       // Counter off
        Debug.Log("Chichas delivered at the altar");                            // Temporary feedback for testing
        OnChichasDelivered?.Invoke();                                           // Start the escape sequence
    }

    // Write the current count above the altar
    private void UpdateCounter()
    {
        if (counterText == null || ChichaManager.Instance == null) return;                         // Nothing to update
        counterText.text = ChichaManager.Instance.Collected + "/" + ChichaManager.Instance.Total; // e.g. 2/5
    }

    // Draw both distances in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;                                 // Counter distance color
        Gizmos.DrawWireSphere(transform.position, showCountDistance); // Show counter range
        Gizmos.color = Color.green;                                // Delivery distance color
        Gizmos.DrawWireSphere(transform.position, deliverDistance);   // Show delivery range
    }
}