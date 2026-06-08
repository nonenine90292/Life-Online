using UnityEngine;
using UnityEngine.EventSystems;

public class CameraScript : MonoBehaviour
{
    public Transform target; // The player or object the camera will follow
    public Transform aimTarget; // The new target for aiming (can be a slightly adjusted position in front of the player)
    public Vector3 defaultOffset = new Vector3(0, 2, -5); // Default offset from the target (player)
    public Vector3 aimingOffset = new Vector3(0, 2, -1); // Offset when aiming (closer to the player)
    public float sensitivity = 5f; // Mouse sensitivity for camera rotation
    public float smoothTime = 0.3f; // Smooth movement time for following the target

    private float currentYaw = 0f; // Horizontal rotation (yaw)
    private float currentPitch = 0f; // Vertical rotation (pitch)
    private Vector3 currentVelocity = Vector3.zero; // Current velocity for smoothing

    // Vertical limits for camera rotation (pitch)
    public float minPitch = -30f; // Minimum vertical angle
    public float maxPitch = 40f; // Maximum vertical angle 

    // Adjusted pitch limits when aiming
    public float aimingMinPitch = -10f; // Minimum vertical angle while aiming
    public float aimingMaxPitch = 20f; // Maximum vertical angle while aiming

    private bool isAiming = false; // Whether the player is aiming or not
    private PlayerMovement player;
    public bool onpc;

    void Start()
    {
        // Lock and hide the cursor for desktop gameplay
        if (onpc)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        player = target.GetComponent<PlayerMovement>();
    }

    void LateUpdate()
    {
        if (!target) return;

        isAiming = player.isAiming;

        // Follow the player
        FollowTarget();

        // Only rotate the camera if the pointer is not over UI
        if (!IsPointerOverUI())
        {
            RotateCamera();
        }
    }

    private void FollowTarget()
    {
        // If aiming, switch to aimTarget, else use the original target
        Transform currentTarget = isAiming ? aimTarget : target;

        // Adjust offset based on whether the player is aiming or not
        Vector3 currentOffset = isAiming ? aimingOffset : defaultOffset;

        // Calculate the desired position based on the current target's position and the offset
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Vector3 desiredPosition = currentTarget.position + rotation * currentOffset;

        // Smoothly move the camera to the desired position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);

        // Make the camera look at the current target
        transform.LookAt(currentTarget.position + Vector3.up * 1.5f); // Adjust for slightly above the player
    }

    private void RotateCamera()
    {
        // Get mouse input for rotating the camera
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        // Update yaw (horizontal) and pitch (vertical) rotation
        currentYaw += mouseX;
        currentPitch -= mouseY;

        // Use different pitch limits based on aiming state
        if (isAiming)
        {
            currentPitch = Mathf.Clamp(currentPitch, aimingMinPitch, aimingMaxPitch);
        }
        else
        {
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }
    }

    /// <summary>
    /// Checks if the pointer is over a UI element (e.g., joystick).
    /// </summary>
    /// <returns>True if the pointer is over UI, false otherwise.</returns>
    private bool IsPointerOverUI()
    {
        if (EventSystem.current != null)
        {
            return EventSystem.current.IsPointerOverGameObject();
        }
        return false;
    }
}
