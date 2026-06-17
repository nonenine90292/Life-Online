using UnityEngine;
using UnityEngine.EventSystems;

public class CarCameraController : MonoBehaviour
{
    public Transform car; // The car to follow
    public float distance = 5f; // Distance behind the car
    public float height = 2f; // Height above the car
    public float rotationSpeed = 5f; // Speed of camera rotation
    public float followSpeed = 10f; // Speed of camera follow smoothness

    public float autoCenterDelay = 2f; // Time after no mouse input to recenter
    public float autoCenterSpeed = 3f; // Speed of auto-centering

    public Vector3 centerOffset = new Vector3(0, 2f, -5f); // Default position to center the camera
    private Vector3 defaultCenterOffset; // Backup of the default center offset

    public bool isReversing = false; // Public flag for reversing state
    private float reverseTimer = 0f; // Timer for reversing

    public bool isManuallyControlled = false; // Flag for camera manual control

    private Vector3 offset; // Initial offset from the car
    private float yaw; // Camera's current yaw rotation
    private float pitch; // Camera's current pitch rotation

    private float idleTime = 0f; // Timer to track idle time for auto-centering

    // New Pitch Limits
    public float minPitch = -20f; // Minimum pitch angle
    public float maxPitch = 60f;  // Maximum pitch angle

    private void Start()
    {
        // Set the initial offset based on the desired distance and height
        offset = centerOffset; // Use the center offset as the initial position
        defaultCenterOffset = centerOffset; // Backup the default center offset
    }

    public void SetCar(GameObject nearcar)
    {
        car = nearcar.GetComponent<Transform>();
    }

    private void LateUpdate()
    {
        if (car == null) return;

        HandleReversing();
        HandleMouseRotation();
        FollowCar();
        AutoCenterCamera();
    }

    private void FollowCar()
    {
        // Ensure the camera position always respects the current offset
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPosition = car.position + rotation * offset;

        // Smoothly move the camera to the desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // Always look at the car
        transform.LookAt(car.position + Vector3.up * 1f); // Adjust `1f` for better focus
    }

    private void HandleMouseRotation()
    {
        // Ignore input if the pointer is over a UI element
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // Get mouse movement input
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Adjust yaw and pitch based on mouse input
        if (Mathf.Abs(mouseX) > 0.1f || Mathf.Abs(mouseY) > 0.1f)
        {
            isManuallyControlled = true; // Set flag to true on mouse movement
            idleTime = 0f; // Reset idle time when there's mouse movement
            yaw += mouseX * rotationSpeed;
            pitch -= mouseY * rotationSpeed;

            // Clamp pitch to prevent flipping
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
        else
        {
            idleTime += Time.deltaTime; // Increase idle time when no mouse movement
        }
    }

    private void AutoCenterCamera()
    {
        if (!isReversing && idleTime > autoCenterDelay) // Only auto-center if not reversing
        {
            isManuallyControlled = false;

            // Smoothly reset yaw and pitch to follow the car's forward direction
            yaw = Mathf.LerpAngle(yaw, car.eulerAngles.y, autoCenterSpeed * Time.deltaTime);
            pitch = Mathf.Lerp(pitch, 0, autoCenterSpeed * Time.deltaTime); // Neutral pitch

            // Update offset position
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 desiredPosition = car.position + rotation * centerOffset;

            // Smoothly move to the desired position
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

            // Ensure the camera looks at the car
            transform.LookAt(car.position + Vector3.up * 1f); // Adjust for a better look
        }
    }

    private void HandleReversing()
    {
        Rigidbody carRigidbody = car.GetComponent<Rigidbody>();
        if (carRigidbody == null) return;

        // Get the car's velocity and determine reversing state
        Vector3 carVelocity = carRigidbody.linearVelocity;
        float forwardDot = Vector3.Dot(car.forward, carVelocity.normalized);

        // Default to follow behavior (behind the car)
        float targetZ = defaultCenterOffset.z;

        if (carVelocity.magnitude > 0.1f && forwardDot < -0.5f) // Reversing
        {
            reverseTimer += Time.deltaTime;
            if (reverseTimer > 2f) // Reverse delay threshold
            {
                isReversing = true;
                targetZ = Mathf.Abs(defaultCenterOffset.z); // Move camera in front of car
            }
        }
        else
        {
            reverseTimer = 0f;
            isReversing = false;
        }

        // Smoothly adjust the Z offset while keeping X and Y constant
        offset = new Vector3(
            defaultCenterOffset.x,
            defaultCenterOffset.y,
            Mathf.Lerp(offset.z, targetZ, 1.5f * Time.deltaTime)
        );
    }
}
