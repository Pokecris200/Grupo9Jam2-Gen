using UnityEngine;

public class DefeatCinematicPicker : MonoBehaviour
{
    [SerializeField] private CinematicPlayer spiritWorldDefeat; // derrota_dos_mundos.mp4
    [SerializeField] private CinematicPlayer[] otherDefeats;    // final_derrota, cueva, se_convierte, tiple
    [SerializeField] private bool useSpiritWorldRule = true;    // Off = always random among all of them

    private static int lastIndex = -1; // Static: remembered after "Retry" reloads the scene

    // Listen for the game over
    void OnEnable()
    {
        MohanMeter.OnMohanCaught += Pick; // Game over
    }

    // Stop listening when disabled
    void OnDisable()
    {
        MohanMeter.OnMohanCaught -= Pick; // Avoid calls on a disabled object
    }

    // Choose and play one defeat cinematic
    private void Pick()
    {
        bool inSpirit = WorldManager.Instance != null && WorldManager.Instance.CurrentWorld == World.Spirit; // Caught in the spirit world?
        if (useSpiritWorldRule && inSpirit && spiritWorldDefeat != null) // Special defeat for the spirit world
        {
            spiritWorldDefeat.Play(); // "Trapped between two worlds"
            return;
        }

        if (otherDefeats == null || otherDefeats.Length == 0) // No random list
        {
            if (spiritWorldDefeat != null) spiritWorldDefeat.Play(); // Only one assigned: use it
            return;
        }

        int i = Random.Range(0, otherDefeats.Length);                                      // Random defeat
        if (otherDefeats.Length > 1 && i == lastIndex) i = (i + 1 + Random.Range(0, otherDefeats.Length - 1)) % otherDefeats.Length; // Never the same twice in a row
        lastIndex = i;                                                                     // Remember it
        if (otherDefeats[i] != null) otherDefeats[i].Play();                              // Play it
    }
}
