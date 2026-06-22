using UnityEngine;
using System.Collections.Generic;

public class CarAI : MonoBehaviour
{
    [Header("Car Settings")]
    public Transform path;                // Path of waypoints
    public float maxSteerAngle = 30f;    // Maximum steering angle
    public float acceleration = 1000f;   // Acceleration force
    public float maxSpeed = 50f;         // Maximum speed
    public float brakeForce = 3000f;     // Brake force
    public float waypointDistance = 1f;  // Distance to switch to the next waypoint

    [Header("Obstacle Detection")]
    public float detectionDistance = 10f; // Distance to detect obstacles

    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheel; // Front Left Wheel
    public WheelCollider frontRightWheel; // Front Right Wheel
    public WheelCollider rearLeftWheel; // Rear Left Wheel
    public WheelCollider rearRightWheel; // Rear Right Wheel

    [Header("Wheel Meshes")]
    public Transform frontLeftWheelTransform;  // Wheel mesh for front left wheel
    public Transform frontRightWheelTransform; // Wheel mesh for front right wheel
    public Transform rearLeftWheelTransform;   // Wheel mesh for rear left wheel
    public Transform rearRightWheelTransform;  // Wheel mesh for rear right wheel

    [Header("Wheel Flip Settings")]
    public bool flipFrontLeftWheel;   // Flip Front Left Wheel
    public bool flipFrontRightWheel;  // Flip Front Right Wheel
    public bool flipRearLeftWheel;    // Flip Rear Left Wheel
    public bool flipRearRightWheel;   // Flip Rear Right Wheel

    [Header("Player Settings")]
    public Transform player;                // Reference to the player's transform
    public float invisibilityDistance = 100f; // Distance beyond which the car becomes invisible
    public float distanceToPlayer;          // Public distance to the player

    private List<Transform> nodes;       // List of waypoints
    public int currentNode = 0;         // Current waypoint index
    private bool isBraking = false;      // Indicates if braking is active
    private bool obstacleDetected = false; // Indicates if an obstacle is detected
    private Rigidbody rb;                // Reference to Rigidbody
    private float currentSpeed;          // Current speed of the car
    private List<MeshRenderer> meshRenderers; // Cache of all MeshRenderers       
    public GameObject[] ped;
    Transform drivingPos;
    public GameObject driver;

    void Start()
    {
        // 1. Buscar el jugador usando el método moderno de Unity 6
        PlayerMovement playerScript = FindAnyObjectByType<PlayerMovement>();
        if (playerScript != null)
        {
            player = playerScript.transform;
        }

        // 2. Buscar la ruta usando el método moderno de Unity 6
        Path pathScript = FindAnyObjectByType<Path>();
        if (pathScript != null)
        {
            path = pathScript.transform;
        }

        // CONTROL DE SEGURIDAD CRÍTICO: Si no hay ruta en la escena, desactivamos el script para evitar crasheos
        if (path == null)
        {
            Debug.LogError($"[CarAI] ¡Error en {gameObject.name}! No se encontró ningún objeto con el script 'Path' en la escena. Desactivando IA.");
            enabled = false;
            return;
        }

        // 3. Buscar y verificar el punto del conductor
        drivingPos = transform.Find("DrivingPosition");
        if (drivingPos == null)
        {
            Debug.LogWarning($"[CarAI] No se encontró el objeto hijo 'DrivingPosition' en {gameObject.name}. Usando el centro del carro como respaldo.");
            drivingPos = this.transform; 
        }

        // 4. Instanciar al conductor si el array tiene peatones asignados
        if (ped != null && ped.Length > 0)
        {
            driver = Instantiate(ped[RandomPed()], drivingPos.position, drivingPos.rotation);
        }
        else
        {
            Debug.LogWarning($"[CarAI] No hay Prefabs de peatones asignados en el array 'Ped' de {gameObject.name}.");
        }

        // 5. Referenciar el Rigidbody
        rb = GetComponent<Rigidbody>();

        // 6. Inicializar los waypoints de la ruta
        Transform[] pathTransforms = path.GetComponentsInChildren<Transform>();
        nodes = new List<Transform>();

        foreach (Transform t in pathTransforms)
        {
            if (t != path.transform) // Excluir el objeto padre
            {
                nodes.Add(t);
            }
        }

        // CONTROL DE SEGURIDAD: Verificar si la ruta tiene puntos dentro
        if (nodes.Count == 0)
        {
            Debug.LogError($"[CarAI] El objeto Path ('{path.name}') no contiene ningún punto hijo adentro. Desactivando IA.");
            enabled = false;
            return;
        }

        // 7. Encontrar el waypoint más cercano al arrancar
        currentNode = FindClosestWaypoint();

        // Asegurar que el índice no desborde la lista
        if (currentNode >= nodes.Count)
        {
            currentNode = 0;
        }

        // Mirar hacia el primer waypoint asignado
        if (nodes.Count > 0 && currentNode < nodes.Count)
        {
            transform.LookAt(nodes[currentNode]);
        }

        // Cachear todos los MeshRenderers en objetos hijos
        meshRenderers = new List<MeshRenderer>(GetComponentsInChildren<MeshRenderer>());
    }

    int RandomPed()
    {
        int rand = Random.Range(0, ped.Length);
        return rand;
    }

    private int FindClosestWaypoint()
    {
        float closestDistance = Mathf.Infinity;
        int closestIndex = 0;

        for (int i = 0; i < nodes.Count; i++)
        {
            float distance = Vector3.Distance(transform.position, nodes[i].position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    void FixedUpdate()
    {
        DetectarObstaculo();

        if (!obstacleDetected)
        {
            isBraking = false; 
            ApplySteer();
            Drive();
            CheckWaypointDistance();
        }
        else
        {
            StopCar(); 
        }

        ApplyBrakes();
    }

    void Update()
    {
        UpdateWheelPosition(frontLeftWheel, frontLeftWheelTransform, flipFrontLeftWheel);
        UpdateWheelPosition(frontRightWheel, frontRightWheelTransform, flipFrontRightWheel);
        UpdateWheelPosition(rearLeftWheel, rearLeftWheelTransform, flipRearLeftWheel);
        UpdateWheelPosition(rearRightWheel, rearRightWheelTransform, flipRearRightWheel);

        UpdateDistanceToPlayer();

        if (driver != null && drivingPos != null)
        {
            driver.transform.position = drivingPos.position;
            driver.transform.rotation = drivingPos.rotation;
        }
    }

    private void UpdateDistanceToPlayer()
    {
        if (player != null)
        {
            distanceToPlayer = Vector3.Distance(transform.position, player.position);
        }
    }

    private void ApplySteer()
    {
        if (nodes == null || nodes.Count == 0 || currentNode >= nodes.Count) return;

        Vector3 carPosition = transform.position;
        Vector3 currentWaypoint = nodes[currentNode].position;
        Vector3 previousWaypoint = nodes[(currentNode == 0 ? nodes.Count - 1 : currentNode - 1)].position;

        Vector3 pathDirection = (currentWaypoint - previousWaypoint).normalized;
        Vector3 closestPointOnPath = Vector3.Project(carPosition - previousWaypoint, pathDirection) + previousWaypoint;

        Vector3 targetPoint = Vector3.Lerp(closestPointOnPath, currentWaypoint, 0.5f);
        Vector3 relativeVector = transform.InverseTransformPoint(targetPoint);

        float steer = (relativeVector.x / relativeVector.magnitude) * maxSteerAngle;

        if (frontLeftWheel != null) frontLeftWheel.steerAngle = steer;
        if (frontRightWheel != null) frontRightWheel.steerAngle = steer;

        Debug.DrawLine(transform.position, targetPoint, Color.green);
    }

    private void Drive()
    {
        if (rb == null) return;
        currentSpeed = rb.linearVelocity.magnitude * 3.6f;

        if (currentSpeed < maxSpeed && !isBraking)
        {
            if (rearLeftWheel != null) rearLeftWheel.motorTorque = acceleration;
            if (rearRightWheel != null) rearRightWheel.motorTorque = acceleration;
        }
        else
        {
            if (rearLeftWheel != null) rearLeftWheel.motorTorque = 0;
            if (rearRightWheel != null) rearRightWheel.motorTorque = 0;
        }
    }

    private void CheckWaypointDistance()
    {
        if (nodes == null || nodes.Count == 0 || currentNode >= nodes.Count) return;

        if (Vector3.Distance(transform.position, nodes[currentNode].position) < waypointDistance)
        {
            currentNode++;
            if (currentNode >= nodes.Count)
            {
                currentNode = 0; 
            }
        }
    }

    private void DetectarObstaculo()
    {
        if (rb == null) return;

        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        float brakingDistance = Mathf.Pow(rb.linearVelocity.magnitude, 2) / (2 * (brakeForce / rb.mass));
        float adjustedDistance = Mathf.Max(brakingDistance, detectionDistance);

        RaycastHit hit;
        if (Physics.SphereCast(rayOrigin, 1f, transform.forward, out hit, adjustedDistance))
        {
            if (hit.transform != this.transform)
            {
                obstacleDetected = true;
                return;
            }
        }
        obstacleDetected = false;
    }

    private void StopCar()
    {
        if (obstacleDetected && rb != null)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
            isBraking = true;
            if (rearLeftWheel != null) rearLeftWheel.motorTorque = 0;
            if (rearRightWheel != null) rearRightWheel.motorTorque = 0;

            ApplyBrakes();
        }
    }

    private void ApplyBrakes()
    {
        float currentBrake = isBraking ? brakeForce : 0f;

        if (frontLeftWheel != null) frontLeftWheel.brakeTorque = currentBrake;
        if (frontRightWheel != null) frontRightWheel.brakeTorque = currentBrake;
        if (rearLeftWheel != null) rearLeftWheel.brakeTorque = currentBrake;
        if (rearRightWheel != null) rearRightWheel.brakeTorque = currentBrake;
    }

    private void UpdateWheelPosition(WheelCollider wheelCollider, Transform wheelTransform, bool flip)
    {
        if (wheelCollider == null || wheelTransform == null) return;

        Vector3 wheelPos;
        Quaternion wheelRot;
        wheelCollider.GetWorldPose(out wheelPos, out wheelRot);

        if (flip)
        {
            wheelRot *= Quaternion.Euler(0f, 180f, 0f);
        }

        wheelTransform.position = wheelPos;
        wheelTransform.rotation = wheelRot;
    }
}