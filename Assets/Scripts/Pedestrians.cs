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
        // 1. Asignar capa inicial de forma segura
        SetLayerRecursively(gameObject, "npc");
        
        GetComponent<NavMeshAgent>().enabled = true;
        agent = GetComponent<NavMeshAgent>();
        defaultSpeed = agent.speed;

        // 2. Buscar waypoints por Tag
        GameObject[] waypointObjects = GameObject.FindGameObjectsWithTag("waypoint");
        waypoints = new Transform[waypointObjects.Length];

        for (int i = 0; i < waypointObjects.Length; i++)
        {
            waypoints[i] = waypointObjects[i].transform;
        }

        // 3. Buscar al jugador usando el método moderno de Unity 6
        player = FindAnyObjectByType<PlayerMovement>();
        
        rb = GetComponent<Rigidbody>();
        carHit = GetComponent<Collider>();

        // Solo iniciar movimiento si hay waypoints válidos en la escena
        if (waypoints.Length > 0)
        {
            StartCoroutine(RandomMovement());
        }
        else
        {
            Debug.LogWarning($"[Pedestrians] ¡Cuidado! No se encontraron GameObjects con el Tag 'waypoint' en la escena para {gameObject.name}.");
        }
    }

    void Update()
    {
        if (dead) return;

        if (animator != null && agent != null)
        {
            // Use agent's speed to set the blend tree animation
            animator.SetFloat("speed", agent.velocity.magnitude);
        }

        // SOLUCIÓN CRÍTICA: En vez de apagar el colisionador, lo volvemos Trigger. 
        // Así el Raycast de las balas SÍ impactará al NPC, pero el jugador no se trabará físicamente al caminar.
        if (player != null && carHit != null)
        {
            if (player.enabled)
            {
                carHit.enabled = true;      // Mantenlo encendido para las balas
                carHit.isTrigger = true;    // Conviértelo en fantasma/atravesable para el cuerpo del Player
            }
            else
            {
                carHit.enabled = true;
                carHit.isTrigger = false;   // Sólido de nuevo para que los carros lo puedan atropellar
            }
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
        if (agent == null || !agent.enabled) return;

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
            if (waypoints == null || waypoints.Length == 0) yield return new WaitForSeconds(1f);

            Transform targetWaypoint = waypoints[Random.Range(0, waypoints.Length)];
            
            if (targetWaypoint != null && agent != null && agent.enabled)
            {
                agent.SetDestination(targetWaypoint.position);

                while (!dead && agent.enabled && (agent.remainingDistance > agent.stoppingDistance || agent.pathPending))
                {
                    yield return null; // Wait until the next frame
                }

                if (agent.enabled && !agent.pathPending && agent.velocity.sqrMagnitude < 0.1f)
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

        Debug.Log($"¡Daño recibido! Balazo de: {damage} HP.");
        StopCoroutine("BoostSpeed");
        StartCoroutine(BoostSpeed());
        
        health -= damage;

        if (health <= 0)
        {
            Debug.Log("Health is 0, falling like a ragdoll.");
            Die(); // Llamamos al nuevo método unificado de muerte
        }
    }

    // Nuevo método público para forzar la muerte sin importar fallos
    public void Die()
    {
        if (dead) return;
        dead = true;
        
        StopAllCoroutines();
        TriggerRagdoll();
    }

    public IEnumerator BoostSpeed()
    {
        idleDuration = 0f;
        if (agent != null && agent.enabled) agent.speed = boostedSpeed;

        yield return new WaitForSeconds(boostedDuration);

        if (agent != null && agent.enabled) agent.speed = defaultSpeed;
        idleDuration = 3f;
    }

    private void TriggerRagdoll()
    {
        // Cambiar a la capa dead de forma segura
        SetLayerRecursively(gameObject, "dead");

        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null)
        {
            mainCollider.enabled = true; // Mantener activo en muerte si es necesario
            mainCollider.isTrigger = false;
        }

        if (rb != null) rb.isKinematic = false;

        if (animator != null) animator.enabled = false;
        if (agent != null) agent.enabled = false;

        if (ragdoll != null)
        {
            foreach (Rigidbody rg in ragdoll.GetComponentsInChildren<Rigidbody>())
            {
                if (rg != null) rg.isKinematic = false;
            }
        }
    }

    private void SetLayerRecursively(GameObject obj, string layerName)
    {
        int layerId = LayerMask.NameToLayer(layerName);
        if (layerId == -1) return;

        obj.layer = layerId;
        foreach (Transform child in obj.transform)
        {
            if (child != null)
            {
                SetLayerRecursively(child.gameObject, layerName);
            }
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        if (dead) return;

        // Comportamiento de atropello (Soporta colisión física o Trigger)
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
                
                health = 0;
                Die();
            }
        }
    }

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