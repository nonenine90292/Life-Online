using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class PedestrianSpawner : MonoBehaviour
{
    public GameObject[] pedestrianPrefab; // Pedestrian prefab to spawn
    public Transform player; // Reference to the player transform
    public float spawnRadius = 100f; // Maximum range to spawn pedestrians
    public float minSpawnDistance = 20f; // Minimum distance from player to spawn pedestrians
    public float despawnDistance = 200f; // Distance beyond which pedestrians are destroyed
    public float spawnInterval = 2f; // Interval for spawning pedestrians
    public int initialSpawnCount = 10; // Number of pedestrians to spawn at the start
    public int maxPedestrianCount = 50; // Maximum number of pedestrians allowed

    private List<GameObject> spawnedPedestrians = new List<GameObject>(); // List to track pedestrians

    // Expose current pedestrian count as a public variable
    public int currentPedestrianCount;

    void Start()
    {

        if (pedestrianPrefab == null)
        {
            Debug.LogError("Pedestrian Prefab is NOT assigned!");
            return;
        }
        if (player == null)
        {
            Debug.LogError("Player Transform is NOT assigned!");
            return;
        }
        minSpawnDistance = 5f;
        // Spawn multiple pedestrians at the start
        SpawnInitialPedestrians();

        // Start spawning pedestrians one by one at regular intervals
        StartCoroutine(SpawnPedestrians());
    }

    void Update()
    {
        // Check if pedestrians are too far from the player
        DespawnDistantPedestrians();

        // Update the current pedestrian count
        currentPedestrianCount = spawnedPedestrians.Count;
    }

    void SpawnInitialPedestrians()
    {
        for (int i = 0; i < initialSpawnCount; i++)
        {
            Vector3 spawnPosition = GetSpawnPositionCloseToPlayer();
            if (spawnPosition != Vector3.zero)
            {
                GameObject pedestrian = Instantiate(pedestrianPrefab[RandomPed()], spawnPosition, Quaternion.identity);
                spawnedPedestrians.Add(pedestrian);
            }
            else
            {
                Debug.LogWarning($"Failed to find a valid spawn position for pedestrian {i + 1}");
            }
        }
        minSpawnDistance = 20;
    }

    int RandomPed()
    {
        int rand = Random.Range(0, pedestrianPrefab.Length);
        return rand;
    }
    IEnumerator SpawnPedestrians()
    {
        while (true)
        {
            // Check if the number of pedestrians is within the limit
            if (spawnedPedestrians.Count < maxPedestrianCount)
            {
                Vector3 spawnPosition = GetSpawnPositionCloseToPlayer();
                if (spawnPosition != Vector3.zero)
                {
                    GameObject pedestrian = Instantiate(pedestrianPrefab[RandomPed()], spawnPosition, Quaternion.identity);
                    spawnedPedestrians.Add(pedestrian);
                }
                else
                {
                    Debug.LogWarning("Failed to find a valid spawn position for pedestrian");
                }
            }
            else
            {
                Debug.Log("Pedestrian count has reached the limit. Skipping spawn.");
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    Vector3 GetSpawnPositionCloseToPlayer()
    {
        // Start from the min distance and gradually go further out if no valid position is found
        float currentRadius = minSpawnDistance;
        float radiusStep = (spawnRadius - minSpawnDistance) / 5f; // Step size to increase radius

        for (int step = 0; step < 5; step++) // Try multiple ranges from close to further out
        {
            for (int i = 0; i < 5; i++) // Try 5 attempts at each radius
            {
                Vector3 randomDirection = Random.insideUnitSphere * currentRadius;
                randomDirection += player.position;
                randomDirection.y = player.position.y; // Keep it level with the player

                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomDirection, out hit, 10f, NavMesh.AllAreas))
                {
                    // Check if the spawn position is on either footpath or cross area
                    int area = hit.mask;
                    int footpathArea = NavMesh.GetAreaFromName("Walkable");

                    // If the position is in either footpath or cross, return it
                    if ((area & (1 << footpathArea)) != 0)
                    {
                        if (Vector3.Distance(hit.position, player.position) >= minSpawnDistance)
                        {
                            return hit.position; // Return valid NavMesh position on footpath or cross
                        }
                    }
                }
            }
            currentRadius += radiusStep; // Increase the radius to try further out if close positions fail
        }

        return Vector3.zero; // No valid position found
    }


    void DespawnDistantPedestrians()
    {
        for (int i = spawnedPedestrians.Count - 1; i >= 0; i--)
        {
            GameObject pedestrian = spawnedPedestrians[i];
            if (Vector3.Distance(pedestrian.transform.position, player.position) > despawnDistance)
            {
                Destroy(pedestrian);
                spawnedPedestrians.RemoveAt(i);
            }
        }
    }
}
