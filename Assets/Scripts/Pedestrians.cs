using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Pedestrians : MonoBehaviour
{
    private NavMeshAgent agent;
    public Animator animator;

    // Public array of transforms for predefined positions
    public Transform[] waypoints;

    // Time to idle after reaching a waypoint
    private float idleDuration = 3f;

    // Default and boosted speeds
    public float defaultSpeed;
    public float boostedSpeed = 6f;
    public float boostedDuration = 10f;

    // Health count
    public int health = 3; // Default health is 3

    // Ragdoll setup (assumes you have set up a ragdoll in the inspector)
    public GameObject ragdoll; // Reference to the ragdoll GameObject
    public bool dead;
    Rigidbody rb;
    public PlayerMovement player;
    Collider carHit;

    public GameObject bloodSplash;

    // Shooting sound detection range
    public float hearingRange = 20f;

    void Start()
    {
        SetLayerRecursively(gameObject, "npc");
        GetComponent<NavMeshAgent>().enabled = true;
        agent = GetComponent<NavMeshAgent>();
        defaultSpeed = agent.speed;

        // Find all objects with the "waypoint" tag and add them to the waypoints array
        GameObject[] waypointObjects = GameObject.FindGameObjectsWithTag("waypoint");
        waypoints = new Transform[waypointObjects.Length];

        for (int i = 0; i < waypointObjects.Length; i++)
        {
            waypoints[i] = waypointObjects[i].transform;
        }
        player = FindFirstObjectByType<PlayerMovement>();
        StartCoroutine(RandomMovement());
        rb = GetComponent<Rigidbody>();
        carHit = GetComponent<Collider>();
    }



    void Update()
    {
        // Use agent's speed to set the blend tree animation
        animator.SetFloat("speed", agent.velocity.magnitude);

        if (player.enabled)
        {
            carHit.enabled = false;
        }
        else
        {
            carHit.enabled = true;
        }

        // Check if the NPC is on the "Road" area
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
        {
            int roadArea = NavMesh.GetAreaFromName("Road");
            int walkableArea = NavMesh.GetAreaFromName("Walkable");

            if (hit.mask == (1 << roadArea))
            {
                Debug.Log("NPC is on the road, redirecting to walkable area.");
                MoveToNearestWalkableArea(walkableArea);
            }
        }
    }

    void MoveToNearestWalkableArea(int walkableArea)
    {
        // Find the nearest point on the "Walkable" NavMesh
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit walkableHit, 10.0f, 1 << walkableArea))
        {
            agent.SetDestination(walkableHit.position);
            Debug.Log($"Redirected NPC to Walkable area at: {walkableHit.position}");
        }
        else
        {
            Debug.LogWarning("No Walkable area found nearby!");
        }
    }


    IEnumerator RandomMovement()
    {
        while (!dead)
        {
            Transform targetWaypoint = waypoints[Random.Range(0, waypoints.Length)];
            agent.SetDestination(targetWaypoint.position);

            while (!dead && (agent.remainingDistance > agent.stoppingDistance || agent.pathPending))
            {
                yield return null; // Wait until the next frame
            }

            if (!agent.pathPending && agent.velocity.sqrMagnitude < 0.1f)
            {
                Debug.Log($"Reached destination: {targetWaypoint.name}");
            }

            yield return new WaitForSeconds(idleDuration);
        }
    }

    public void TakeDamage(int damage)
    {
        Debug.Log("Bullet hit detected. Boosting speed!");
        StopCoroutine("BoostSpeed");
        StartCoroutine(BoostSpeed());
        health -= damage;

        if (health <= 0)
        {
            Debug.Log("Health is 0, falling like a ragdoll.");
            dead = true;
            StopCoroutine(RandomMovement());
            TriggerRagdoll();
        }
    }

    public IEnumerator BoostSpeed()
    {
        idleDuration = 0f;
        agent.speed = boostedSpeed;

        yield return new WaitForSeconds(boostedDuration);

        agent.speed = defaultSpeed;
        idleDuration = 3f;
    }

    private void TriggerRagdoll()
    {
        SetLayerRecursively(gameObject, "dead");

        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null)
            mainCollider.enabled = false;

        rb.isKinematic = false;

        animator.enabled = false;
        agent.enabled = false;

        foreach (Rigidbody rg in ragdoll.GetComponentsInChildren<Rigidbody>())
        {
            rg.isKinematic = false;
        }

    }


    private void SetLayerRecursively(GameObject obj, string layer)
    {
        obj.layer = LayerMask.NameToLayer(layer);
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "Car")
        {
            Rigidbody carRb = collision.gameObject.GetComponent<Rigidbody>();

            if (carRb != null && carRb.linearVelocity.magnitude > 4f)
            {
                GameObject blood = Instantiate(bloodSplash, transform.position, transform.rotation);
                Destroy(blood, 0.5f);
                health = 0;
                dead = true;
                StopCoroutine(RandomMovement());
                TriggerRagdoll();
            }
        }
    }

    // Detect shooting sound
    public void OnHeardShooting(Vector3 soundOrigin)
    {
        float distance = Vector3.Distance(transform.position, soundOrigin);
        if (distance <= hearingRange)
        {
            Debug.Log("Shooting sound heard! Boosting speed.");
            StopCoroutine("BoostSpeed");
            StartCoroutine(BoostSpeed());
        }
    }
}
