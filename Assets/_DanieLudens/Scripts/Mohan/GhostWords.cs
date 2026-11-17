using System.Collections;
using TMPro;
using UnityEngine;

public class GhostWords : MonoBehaviour
{
    [Header("Words")]
    [SerializeField] private TextMeshPro wordPrefab;                                   // Text used for each word
    [SerializeField] private string[] words = { "huye", "corre", "vete", "sálvate" }; // Words that warn the player
    [SerializeField] private string evilWord = "quédese";                             // Voice of something evil in the channel
    [SerializeField] private Color wordColor = new Color(0.8f, 0.9f, 1f);              // Pale ghostly blue
    [SerializeField] private Color evilColor = new Color(0.9f, 0.1f, 0.1f);            // Red for the evil word
    [Range(0f, 1f)] [SerializeField] private float evilChance = 0.15f;                 // Chance for each word to be the evil one

    [Header("Spawning")]
    [SerializeField] private Vector2 spawnInterval = new Vector2(0.4f, 0.9f); // Random seconds between words
    [SerializeField] private float spawnWidth = 1.5f;                         // Horizontal spread above the altar
    [SerializeField] private float spawnHeight = 1.5f;                        // Height above the altar where words appear

    [Header("Movement")]
    [SerializeField] private float riseSpeed = 0.8f;   // Units per second upward
    [SerializeField] private float lifetime = 2f;      // Seconds before a word disappears
    [SerializeField] private float swayAmount = 0.2f;  // Side-to-side movement like smoke
    [SerializeField] private float maxAlpha = 0.85f;   // Max visibility

    private bool running; // True while words are spawning

    // Listen for the delivery and the game over
    void OnEnable()
    {
        Altar.OnChichasDelivered += StartWords; // Start as soon as the chichas are delivered
        MohanMeter.OnMohanCaught += StopWords;  // Stop on game over
    }

    // Stop listening when disabled
    void OnDisable()
    {
        Altar.OnChichasDelivered -= StartWords; // Avoid calls on a disabled object
        MohanMeter.OnMohanCaught -= StopWords;  // Avoid calls on a disabled object
    }

    // Begin spawning words
    private void StartWords()
    {
        if (running) return;             // Already running
        running = true;                  // Mark as running
        StartCoroutine(SpawnLoop());     // Keep spawning words
    }

    // Stop spawning new words (the ones on screen finish their animation)
    private void StopWords()
    {
        running = false; // The loop ends on its next check
    }

    // Spawn a word, wait a random time, repeat
    private IEnumerator SpawnLoop()
    {
        while (running) // Until stopped
        {
            SpawnWord();                                                                // Create one word
            yield return new WaitForSeconds(Random.Range(spawnInterval.x, spawnInterval.y)); // Random pause
        }
    }

    // Create one floating word above the altar
    private void SpawnWord()
    {
        if (wordPrefab == null || words.Length == 0) return; // Nothing to spawn

        bool evil = Random.value < evilChance;                                                                  // Is this the evil word?
        Vector3 position = transform.position + new Vector3(Random.Range(-spawnWidth, spawnWidth) * 0.5f, spawnHeight, 0f); // Random spot above the altar

        TextMeshPro word = Instantiate(wordPrefab, position, Quaternion.identity); // Create a copy of the prefab
        word.text = evil ? evilWord : words[Random.Range(0, words.Length)];        // Pick the text
        word.color = evil ? evilColor : wordColor;                                 // Pick the color
        word.alpha = 0f;                                                           // Start invisible
        StartCoroutine(FloatWord(word));                                           // Animate it
    }

    // Rise, sway and fade the word, then remove it
    private IEnumerator FloatWord(TextMeshPro word)
    {
        Vector3 start = word.transform.position; // Starting point
        float seed = Random.value * 10f;         // Different sway for each word

        for (float t = 0f; t < lifetime; t += Time.deltaTime) // Every frame until lifetime
        {
            float k = t / lifetime;                                                // Progress from 0 to 1
            float sway = Mathf.Sin((t + seed) * 3f) * swayAmount;                  // Side to side
            word.transform.position = start + new Vector3(sway, riseSpeed * t, 0f); // Rise while swaying
            word.alpha = Mathf.Sin(k * Mathf.PI) * maxAlpha;                       // Fade in, then fade out
            yield return null;                                                     // Next frame
        }

        Destroy(word.gameObject); // Remove the word
    }
}
