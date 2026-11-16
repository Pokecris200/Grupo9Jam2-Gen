using UnityEngine;

public class Chicha : MonoBehaviour
{
    [SerializeField] private GameObject chichaRoot; // Whole chicha (bottle + glow) to remove when collected

    private bool collected; // Prevents counting the same chicha twice

    // Collect the chicha when the player touches it
    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || !other.CompareTag("Player")) return; // Ignore if already taken or not the player

        collected = true;                                                      // Mark as taken
        ChichaManager.Instance.Collect();                                      // Add it to the count
        (chichaRoot != null ? chichaRoot : gameObject).SetActive(false);       // Hide bottle and glow
    }
}
