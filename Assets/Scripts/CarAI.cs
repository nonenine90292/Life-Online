using UnityEngine;
using System.Collections.Generic;

public class CarAI : MonoBehaviour
{
    [Header("Car Settings")]
    public Transform path;               // Path of waypoints
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
    public Transform player;               // Reference to the player's transform
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
        player = FindFirstObjectByType<PlayerMovement>().transform;
        path = FindFirstObjectByType<Path>().transform;
        drivingPos = transform.Find("DrivingPosition");
        driver = Instantiate(ped[RandomPed()], drivingPos.position, drivingPos.rotation);
        // Reference the Rigidbody
        rb = GetComponent<Rigidbody>();

        // Initialize waypoints from the path
        Transform[] pathTransforms = path.GetComponentsInChildren<Transform>();
        nodes = new List<Transform>();

        foreach (Transform t in pathTransforms)
        {
            if (t != path.transform) // Exclude the parent object
            {
                nodes.Add(t);
            }
        }

        // Find the closest waypoint at the start
        currentNode = FindClosestWaypoint();

        // Move towards the first waypoint initially
        if (nodes.Count > 0)
        {
            transform.LookAt(nodes[currentNode]); // Face the first waypoint
        }

        // Cache all MeshRenderers in child objects
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
            // Calculate the distance between the car and each waypoint
            float distance = Vector3.Distance(transform.position, nodes[i].position);

            // Check if this waypoint is closer than the previous closest
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex + 1; // Return the index of the closest waypoint
    }

    void FixedUpdate()
    {
        // Detect obstacles
        DetectObstacle();

        // Execute AI controls only if no obstacle is detected
        if (!obstacleDetected)
        {
            isBraking = false; // Reset braking when no obstacle is detected
            ApplySteer();
            Drive();
            CheckWaypointDistance();
        }
        else
        {
            StopCar(); // Stop the car when an obstacle is detected
        }

        ApplyBrakes();
    }

    void Update()
    {
        // Update wheel meshes to match the wheel colliders
        UpdateWheelPosition(frontLeftWheel, frontLeftWheelTransform, flipFrontLeftWheel);
        UpdateWheelPosition(frontRightWheel, frontRightWheelTransform, flipFrontRightWheel);
        UpdateWheelPosition(rearLeftWheel, rearLeftWheelTransform, flipRearLeftWheel);
        UpdateWheelPosition(rearRightWheel, rearRightWheelTransform, flipRearRightWheel);

        // Update the distance to the player
        UpdateDistanceToPlayer();

        driver.transform.position = drivingPos.position;
        driver.transform.rotation = drivingPos.rotation;
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
        Vector3 carPosition = transform.position;
        Vector3 currentWaypoint = nodes[currentNode].position;
        Vector3 previousWaypoint = nodes[(currentNode == 0 ? nodes.Count - 1 : currentNode - 1)].position;

        Vector3 pathDirection = (currentWaypoint - previousWaypoint).normalized;
        Vector3 closestPointOnPath = Vector3.Project(carPosition - previousWaypoint, pathDirection) + previousWaypoint;

        Vector3 targetPoint = Vector3.Lerp(closestPointOnPath, currentWaypoint, 0.5f);
        Vector3 relativeVector = transform.InverseTransformPoint(targetPoint);

        float steer = (relativeVector.x / relativeVector.magnitude) * maxSteerAngle;

        frontLeftWheel.steerAngle = steer;
        frontRightWheel.steerAngle = steer;

        Debug.DrawLine(transform.position, targetPoint, Color.green);
    }

    private void Drive()
    {
        currentSpeed = rb.linearVelocity.magnitude * 3.6f;

        if (currentSpeed < maxSpeed && !isBraking)
        {
            rearLeftWheel.motorTorque = acceleration;
            rearRightWheel.motorTorque = acceleration;
        }
        else
        {
            rearLeftWheel.motorTorque = 0;
            rearRightWheel.motorTorque = 0;
        }
    }

    private void CheckWaypointDistance()
    {
        if (Vector3.Distance(transform.position, nodes[currentNode].position) < waypointDistance)
        {
            currentNode++;
            if (currentNode >= nodes.Count)
            {
                currentNode = 0; // Loop back to the first waypoint
            }
        }
    }

    private void DetectObstacle()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        float brakingDistance = Mathf.Pow(rb.linearVelocity.magnitude, 2) / (2 * (brakeForce / rb.mass));
        float adjustedDistance = Mathf.Max(brakingDistance, detectionDistance);

        RaycastHit hit;
        if (Physics.SphereCast(rayOrigin, 1f, transform.forward, out hit, adjustedDistance))
        {
            obstacleDetected = true;
        }
        else
        {
            obstacleDetected = false;
        }
    }

    private void StopCar()
    {
        if (obstacleDetected)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);
            isBraking = true;
            rearLeftWheel.motorTorque = 0;
            rearRightWheel.motorTorque = 0;

            ApplyBrakes();
        }
    }

    private void ApplyBrakes()
    {
        if (isBraking)
        {
            frontLeftWheel.brakeTorque = brakeForce;
            frontRightWheel.brakeTorque = brakeForce;
            rearLeftWheel.brakeTorque = brakeForce;
            rearRightWheel.brakeTorque = brakeForce;
        }
        else
        {
            frontLeftWheel.brakeTorque = 0;
            frontRightWheel.brakeTorque = 0;
            rearLeftWheel.brakeTorque = 0;
            rearRightWheel.brakeTorque = 0;
        }
    }



    private void UpdateWheelPosition(WheelCollider wheelCollider, Transform wheelTransform, bool flip)
    {
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
