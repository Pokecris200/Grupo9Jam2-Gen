using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerThoughts : MonoBehaviour
{
    public static PlayerThoughts Instance { get; private set; } // Singleton access

    [SerializeField] private TextMeshPro text;              // Text above the player's head
    [SerializeField] private float defaultDuration = 2.5f;  // Seconds a thought stays visible
    [SerializeField] private float fadeTime = 0.3f;         // Fade in and fade out time

    private Coroutine current; // Thought currently being shown

    public bool IsSpeaking => current != null; // True while a thought is on screen

    // Set up the singleton and hide the text
    void Awake()
    {
        if (Instance != null && Instance != this) // Another one already exists
        {
            Destroy(this); // Remove only this component
            return;
        }
        Instance = this;  // Register as the instance
        text.alpha = 0f;  // Start invisible
    }

    // Show a thought for a few seconds
    public void Say(string message, float duration = -1f)
    {
        if (current != null) StopCoroutine(current);                                       // Replace the current thought
        current = StartCoroutine(ShowRoutine(message, duration > 0f ? duration : defaultDuration)); // Start the new one
    }

    // Show a random line from a list
    public void SayRandom(string[] lines, float duration = -1f)
    {
        if (lines == null || lines.Length == 0) return; // Nothing to say
        Say(lines[Random.Range(0, lines.Length)], duration); // Pick one at random
    }

    // Fade in, wait, fade out
    private IEnumerator ShowRoutine(string message, float duration)
    {
        text.text = message;                         // Set the words
        yield return Fade(text.alpha, 1f);           // Fade in
        yield return new WaitForSeconds(duration);   // Stay on screen
        yield return Fade(1f, 0f);                   // Fade out
        current = null;                              // Done speaking
    }

    // Change the text transparency over time
    private IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < fadeTime; t += Time.deltaTime) // Run every frame until fadeTime
        {
            text.alpha = Mathf.Lerp(from, to, t / fadeTime); // Blend the alpha
            yield return null;                               // Wait for the next frame
        }
        text.alpha = to; // Make sure it ends exactly at the target
    }

    // Clear the singleton when destroyed
    void OnDestroy()
    {
        if (Instance == this) Instance = null; // Release the instance
    }
}
