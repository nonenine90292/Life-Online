using System.Collections.Generic;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    public GameObject[] carPrefabs; // Array of car prefabs
    public GameObject waypointsParent; // Empty object holding all waypoints
    public Transform player; // Reference to the player's Transform
    public float spawnRadius = 5f; // Radius to check for nearby objects before spawning

    private List<Transform> waypoints = new List<Transform>();
    private Dictionary<Transform, GameObject> activeCars = new Dictionary<Transform, GameObject>(); // Tracks spawned cars per waypoint
    public int carCount;
    public int maxCars;

    private float minSpawnDistance = 150f; // Minimum distance for spawning cars
    private float maxSpawnDistance = 200f; // Maximum distance for spawning cars
    private float minSpawnDistanceSqr;
    private float maxSpawnDistanceSqr;

    void Start()
    {
        // Calculate squared distances for comparison
        minSpawnDistanceSqr = minSpawnDistance * minSpawnDistance;
        maxSpawnDistanceSqr = maxSpawnDistance * maxSpawnDistance;

        // Get all child waypoints from the waypointsParent
        foreach (Transform child in waypointsParent.transform)
        {
            waypoints.Add(child);
        }
    }

    void Update()
    {
        if (carCount < maxCars)
        {
            SpawnCarsInRange();
            print("spawning");
        }
        DespawnCarsOutOfRange();
        carCount = activeCars.Count;
    }

    void SpawnCarsInRange()
    {
        foreach (Transform waypoint in waypoints)
        {
            // Check the squared distance between player and waypoint
            float sqrDistanceToPlayer = (player.position - waypoint.position).sqrMagnitude;

            // If waypoint is within the specified distance range and doesn't already have a car, check for nearby objects
            if (sqrDistanceToPlayer >= minSpawnDistanceSqr && sqrDistanceToPlayer <= maxSpawnDistanceSqr && !activeCars.ContainsKey(waypoint))
            {
                if (!IsObjectNearby(waypoint.position))
                {
                    SpawnCar(waypoint);
                }
            }
        }
    }

    void DespawnCarsOutOfRange()
    {
        // Use a temporary list to track waypoints for cars that need to be despawned
        List<Transform> waypointsToDespawn = new List<Transform>();

        foreach (var entry in activeCars)
        {
            Transform waypoint = entry.Key;
            GameObject car = entry.Value;

            float sqrDistanceToPlayer = (player.position - car.transform.position).sqrMagnitude;

            // Despawn car if it's out of max range
            if (sqrDistanceToPlayer > maxSpawnDistanceSqr)
            {
                Destroy(car.GetComponent<CarAI>().driver);
                Destroy(car);
                waypointsToDespawn.Add(waypoint);
            }
        }

        // Remove despawned cars from the dictionary
        foreach (Transform waypoint in waypointsToDespawn)
        {
            activeCars.Remove(waypoint);
        }
    }

    void SpawnCar(Transform waypoint)
    {
        // Randomly select a car prefab
        GameObject carPrefab = carPrefabs[Random.Range(0, carPrefabs.Length)];

        // Instantiate the car at the waypoint's position and default rotation
        GameObject car = Instantiate(carPrefab, waypoint.position, waypoint.rotation);

        // Determine the direction to the next waypoint
        Transform nextWaypoint = GetNextWaypoint(waypoint);
        if (nextWaypoint != null)
        {
            Vector3 directionToNext = (nextWaypoint.position - waypoint.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(directionToNext);
            car.transform.rotation = Quaternion.Euler(0, lookRotation.eulerAngles.y, 0); // Rotate only on Y axis
        }

        // Track the spawned car in the dictionary
        activeCars[waypoint] = car;
    }

    Transform GetNextWaypoint(Transform currentWaypoint)
    {
        // Get the index of the current waypoint
        int currentIndex = waypoints.IndexOf(currentWaypoint);

        // Return the next waypoint if it exists, otherwise null
        if (currentIndex >= 0 && currentIndex < waypoints.Count - 1)
        {
            return waypoints[currentIndex + 1];
        }

        return null; // No next waypoint (end of path)
    }

    bool IsObjectNearby(Vector3 position)
    {
        // Check for any colliders within the spawn radius around the waypoint
        Collider[] colliders = Physics.OverlapSphere(position, spawnRadius);

        foreach (Collider collider in colliders)
        {
            // Ignore the terrain or waypoints themselves
            if (collider.CompareTag("Car"))
            {
                return true; // Object is nearby
            }
        }

        return false; // No nearby objects
    }
}
