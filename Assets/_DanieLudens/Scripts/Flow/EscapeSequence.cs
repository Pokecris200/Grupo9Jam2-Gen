using System;
using System.Collections;
using UnityEngine;

public class EscapeSequence : MonoBehaviour
{
    public static event Action OnEscapeStarted;        // Fired when the escape phase begins
    public static bool IsEscaping { get; private set; } // True during the escape phase

    [SerializeField] private float tensionDelay = 3f;    // Seconds between delivery and escape
    [SerializeField] private AudioSource audioSource;    // Plays the tension sounds
    [SerializeField] private AudioClip[] tensionSounds;  // Rumble, birds, whistles (played one after another)

    // Reset the static state when the level loads
    void Awake()
    {
        IsEscaping = false; // Static values survive scene changes, so reset them here
    }

    // Listen for the altar delivery
    void OnEnable()
    {
        Altar.OnChichasDelivered += StartSequence; // Start when the chichas are delivered
    }

    // Stop listening when disabled
    void OnDisable()
    {
        Altar.OnChichasDelivered -= StartSequence; // Avoid calls on a disabled object
    }

    // Begin the tension sequence
    private void StartSequence()
    {
        StartCoroutine(SequenceRoutine()); // Run the timed sequence
    }

    // Play the tension sounds, wait, then start the escape
    private IEnumerator SequenceRoutine()
    {
        Debug.Log("Tension: the Mohán is coming..."); // Temporary feedback for testing

        int count = Mathf.Max(1, tensionSounds.Length); // Avoid dividing by zero
        float gap = tensionDelay / count;               // Spread the sounds across the delay

        for (int i = 0; i < count; i++) // One step per sound
        {
            if (audioSource != null && i < tensionSounds.Length && tensionSounds[i] != null)
            {
                audioSource.PlayOneShot(tensionSounds[i]); // Play this sound without cutting the others
            }
            yield return new WaitForSeconds(gap); // Wait before the next one
        }

        IsEscaping = true;         // Escape phase is on
        Debug.Log("Escape started!"); // Temporary feedback for testing
        OnEscapeStarted?.Invoke(); // Tell the meter, the player, the portal, the arrow...
    }
}
