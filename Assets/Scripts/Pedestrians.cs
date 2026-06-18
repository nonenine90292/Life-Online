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
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = true;
            defaultSpeed = agent.speed;
        }

        SetLayerRecursively(gameObject, "npc");

        // Find all objects with the "waypoint" tag and add them to the waypoints array
        GameObject[] waypointObjects = GameObject.FindGameObjectsWithTag("waypoint");
        waypoints = new Transform[waypointObjects.Length];

        for (int i = 0; i < waypointObjects.Length; i++)
        {
            waypoints[i] = waypointObjects[i].transform;
        }

        player = FindFirstObjectByType<PlayerMovement>();
        rb = GetComponent<Rigidbody>();
        carHit = GetComponent<Collider>();

        // Solo iniciamos el movimiento si hay waypoints válidos
        if (waypoints.Length > 0)
        {
            StartCoroutine(RandomMovement());
        }
        else
        {
            Debug.LogWarning($"El NPC {gameObject.name} no encontró waypoints con el tag 'waypoint'!");
        }
    }

    void Update()
    {
        if (dead) return; // Si está muerto, no ejecutes nada en Update

        // SEGURIDAD: Validar que el agente esté activo y en el NavMesh antes de pedir velocidad
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            animator.SetFloat("speed", agent.velocity.magnitude);
        }
        else
        {
            animator.SetFloat("speed", 0f);
        }

        if (player != null && player.enabled)
        {
            carHit.enabled = false;
        }
        else
        {
            carHit.enabled = true;
        }

        // SEGURIDAD: Solo chequear áreas si el agente está listo y sobre el NavMesh
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
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
    }

    void MoveToNearestWalkableArea(int walkableArea)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

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
        // Esperar un frame al inicio para asegurar que el NPC se asentó en el mapa
        yield return null;

        while (!dead)
        {
            // SEGURIDAD ANTES DE OPERAR EL AGENTE (Evita errores de NavMesh)
            if (agent != null && agent.enabled && agent.isOnNavMesh && waypoints.Length > 0)
            {
                Transform targetWaypoint = waypoints[Random.Range(0, waypoints.Length)];
                agent.SetDestination(targetWaypoint.position);

                // Ciclo de espera mientras camina, agregando la verificación de isOnNavMesh
                while (!dead && agent.enabled && agent.isOnNavMesh && (agent.remainingDistance > agent.stoppingDistance || agent.pathPending))
                {
                    yield return null; // Wait until the next frame
                }

                if (agent.enabled && agent.isOnNavMesh && !agent.pathPending && agent.velocity.sqrMagnitude < 0.1f)
                {
                    Debug.Log($"Reached destination: {targetWaypoint.name}");
                }
            }

            yield return new WaitForSeconds(idleDuration);
        }
    }

    public void TakeDamage(int damage)
    {
        if (dead) return;

        Debug.Log("Bullet hit detected. Boosting speed!");
        StopCoroutine("BoostSpeed");
        StartCoroutine(BoostSpeed());
        health -= damage;

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Health is 0, falling like a ragdoll.");
        dead = true;
        StopAllCoroutines(); // Frena movimiento y boost de velocidad al seco
        TriggerRagdoll();
    }

    public IEnumerator BoostSpeed()
    {
        if (agent == null || !agent.enabled) yield break;

        idleDuration = 0f;
        agent.speed = boostedSpeed;

        yield return new WaitForSeconds(boostedDuration);

        if (agent != null && agent.enabled)
        {
            agent.speed = defaultSpeed;
        }
        idleDuration = 3f;
    }

    private void TriggerRagdoll()
    {
        SetLayerRecursively(gameObject, "dead");

        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null)
            mainCollider.enabled = false;

        if (rb != null)
            rb.isKinematic = false;

        animator.enabled = false;
        
        if (agent != null)
            agent.enabled = false; // Apagamos el agente para que no tire más errores tras morir

        if (ragdoll != null)
        {
            foreach (Rigidbody rg in ragdoll.GetComponentsInChildren<Rigidbody>())
            {
                rg.isKinematic = false;
            }
        }
    }

    private void SetLayerRecursively(GameObject obj, string layer)
    {
        int layerIndex = LayerMask.NameToLayer(layer);
        
        // SEGURIDAD: Si la capa no existe en Unity, no la asigna y evita el error [0...31]
        if (layerIndex != -1)
        {
            obj.layer = layerIndex;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
        else
        {
            Debug.LogWarning($"La capa '{layer}' no existe en la configuración de Unity. ¡Créala en Edit > Project Settings > Tags and Layers!");
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        if (dead) return;

        if (collision.gameObject.CompareTag("Car"))
        {
            Rigidbody carRb = collision.gameObject.GetComponent<Rigidbody>();

            if (carRb != null && carRb.linearVelocity.magnitude > 4f)
            {
                if (bloodSplash != null)
                {
                    GameObject blood = Instantiate(bloodSplash, transform.position, transform.rotation);
                    Destroy(blood, 0.5f);
                }
                Die();
            }
        }
    }

    // Detect shooting sound
    public void OnHeardShooting(Vector3 soundOrigin)
    {
        if (dead) return;

        float distance = Vector3.Distance(transform.position, soundOrigin);
        if (distance <= hearingRange)
        {
            Debug.Log("Shooting sound heard! Boosting speed.");
            StopCoroutine("BoostSpeed");
            StartCoroutine(BoostSpeed());
        }
    }
}
