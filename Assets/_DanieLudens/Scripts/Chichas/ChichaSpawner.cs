using System.Collections.Generic;
using UnityEngine;

public class ChichaSpawner : MonoBehaviour
{
    [SerializeField] private GameObject chichaPrefab; // Chicha to spawn
    [SerializeField] private int chichaCount = 4;     // How many chichas this level has (4, 8, 13)
    [SerializeField] private Transform[] spawnPoints; // Possible places (uses the children if empty)

    // Spawn the chichas before the ChichaManager counts them in Start
    void Awake()
    {
        List<Transform> points = new List<Transform>(spawnPoints != null && spawnPoints.Length > 0 ? spawnPoints : GetChildPoints()); // Available places

        if (chichaCount > points.Count) // Not enough places
        {
            Debug.LogWarning("ChichaSpawner: only " + points.Count + " spawn points for " + chichaCount + " chichas"); // Tell the level designer
        }

        int count = Mathf.Min(chichaCount, points.Count); // Never more than the available places
        for (int i = 0; i < count; i++)                   // One chicha per step
        {
            int index = Random.Range(0, points.Count);                                     // Pick a random place
            Instantiate(chichaPrefab, points[index].position, Quaternion.identity);        // Create the chicha there
            points.RemoveAt(index);                                                        // Never use the same place twice
        }
    }

    // Use the children of this object as spawn points
    private Transform[] GetChildPoints()
    {
        Transform[] children = new Transform[transform.childCount]; // One slot per child
        for (int i = 0; i < transform.childCount; i++)              // Every child
        {
            children[i] = transform.GetChild(i); // Save it
        }
        return children;
    }

    // Show the spawn points in the editor
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f);   // Chicha-like amber
        foreach (Transform child in transform)      // Every point
        {
            Gizmos.DrawWireSphere(child.position, 0.3f); // Small circle at each possible place
        }
    }
}