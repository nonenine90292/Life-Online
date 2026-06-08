using UnityEngine;

public class CameraCollision : MonoBehaviour
{
    public Transform target; // The target (e.g., the player)
    public Vector3 offset = new Vector3(0, 2, -5); // The offset for camera position
    public float smoothTime = 0.3f; // Smooth time for camera movement
    public float collisionRadius = 0.5f; // The radius of the sphere used to check for collisions
    public float minDistance = 2f; // Minimum allowed distance to the player when colliding
    public float maxDistance = 5f; // Maximum camera distance from the player

    private Vector3 currentVelocity = Vector3.zero; // Current velocity for smoothing
    private float currentYaw = 0f; // Current yaw rotation (horizontal)
    private float currentPitch = 0f; // Current pitch rotation (vertical)

    // Limits for the camera's vertical (pitch) rotation
    public float minPitch = -30f; // Minimum vertical angle
    public float maxPitch = 60f; // Maximum vertical angle

    void Start()
    {
        // Lock and hide the cursor when the game starts
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Get mouse input for rotating the camera
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        currentYaw += mouseX;
        currentPitch -= mouseY;

        // Clamp the vertical rotation to avoid flipping the camera
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        // Calculate the desired camera rotation
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        // Calculate the desired position of the camera based on the offset
        Vector3 desiredPosition = target.position + rotation * offset;

        // Perform collision detection using a Raycast
        Vector3 direction = desiredPosition - target.position;
        RaycastHit hit;

        // Perform a raycast to check for obstacles
        if (Physics.Raycast(target.position, direction.normalized, out hit, direction.magnitude))
        {
            // If we hit something, adjust the camera position to avoid clipping
            float hitDistance = Mathf.Clamp(hit.distance - collisionRadius, minDistance, maxDistance);
            desiredPosition = target.position + direction.normalized * hitDistance;
        }
        else
        {
            // If no collision is detected, ensure the camera is within the min/max distance
            float distance = Vector3.Distance(target.position, desiredPosition);
            float clampedDistance = Mathf.Clamp(distance, minDistance, maxDistance);

            // Adjust the camera position to respect the clamped distance
            desiredPosition = target.position + direction.normalized * clampedDistance;
        }

        // Smoothly move the camera to the desired position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);

        // Make the camera look at the target
        transform.LookAt(target.position + Vector3.up * 1.5f); // Adjust for slightly above the player
    }
}
