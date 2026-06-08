using UnityEngine;
using UnityEngine.EventSystems;

public class CarCameraController : MonoBehaviour
{
    public Transform car;

    public float distance = 5f;
    public float height = 2f;

    public float rotationSpeed = 5f;
    public float followSpeed = 10f;

    public float autoCenterDelay = 2f;
    public float autoCenterSpeed = 3f;

    public Vector3 centerOffset = new Vector3(0, 2f, -5f);
    private Vector3 defaultCenterOffset;

    public bool isReversing = false;
    private float reverseTimer = 0f;

    public bool isManuallyControlled = false;

    private Vector3 offset;
    private float yaw;
    private float pitch;

    private float idleTime = 0f;

    public float minPitch = -20f;
    public float maxPitch = 60f;

    public Camera cam;
    public float baseFOV = 60f;
    public float maxFOV = 80f;
    public float fovSpeed = 5f;

    public float cameraLag = 0.08f;
    public float rotationSmoothness = 8f;

    public float velocityInfluence = 0.05f;
    public float lookAheadDistance = 6f;

    public float accelerationCameraPush = 0.35f;
    public float brakingCameraPush = 0.2f;
    public float accelerationSmoothness = 4f;

    public float cameraShakeAmount = 0.03f;
    public float cameraShakeSpeed = 18f;

    public float driftTiltAmount = 8f;

    private float currentAccelerationOffset;
    private float currentTilt;
    private Vector3 shakeOffset;

    private float speedPercent;
    private Vector3 currentVelocity;

    private Rigidbody carRb;

    public Transform cameraTarget;

    private void Start()
    {
        offset = centerOffset;
        defaultCenterOffset = centerOffset;

        if (cam == null)
            cam = Camera.main;

        if (car != null)
        {
            carRb = car.GetComponent<Rigidbody>();
            yaw = car.eulerAngles.y;
            pitch = 5f;
        }
    }

    public void SetCar(GameObject nearcar)
    {
        car = nearcar.transform;
        carRb = car.GetComponent<Rigidbody>();

        yaw = car.eulerAngles.y;
        pitch = 5f;
    }

    private void LateUpdate()
    {
        if (car == null) return;

        HandleReversing();
        HandleMouseRotation();
        FollowCar();
        AutoCenterCamera();
        HandleDynamicFOV();
    }

    private void FollowCar()
    {
        if (carRb == null) return;

        float forwardSpeed = Vector3.Dot(car.forward, carRb.linearVelocity);

        speedPercent = Mathf.Clamp01(carRb.linearVelocity.magnitude / 50f);

        float targetAccelerationOffset = 0f;

        if (forwardSpeed > 1f)
            targetAccelerationOffset = -accelerationCameraPush * speedPercent;
        else if (forwardSpeed < -1f)
            targetAccelerationOffset = brakingCameraPush;

        currentAccelerationOffset = Mathf.Lerp(
            currentAccelerationOffset,
            targetAccelerationOffset,
            accelerationSmoothness * Time.deltaTime
        );

        float horizontalVelocity = Vector3.Dot(car.right, carRb.linearVelocity);
        float targetTilt = -horizontalVelocity * 0.15f;

        targetTilt = Mathf.Clamp(targetTilt, -driftTiltAmount, driftTiltAmount);

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, 5f * Time.deltaTime);

        shakeOffset = new Vector3(
            Mathf.PerlinNoise(Time.time * cameraShakeSpeed, 0f) - 0.5f,
            Mathf.PerlinNoise(0f, Time.time * cameraShakeSpeed) - 0.5f,
            0f
        ) * cameraShakeAmount * speedPercent;

        Vector3 velocityOffset = carRb.linearVelocity * velocityInfluence;

        Quaternion rotation = Quaternion.Euler(pitch, yaw, currentTilt);

        Vector3 dynamicOffset = offset + new Vector3(0f, 0f, currentAccelerationOffset);

        Vector3 desiredPosition = car.position + rotation * dynamicOffset - velocityOffset + shakeOffset;

        RaycastHit hit;
        Vector3 origin = car.position + Vector3.up * 1.5f;
        Vector3 dir = desiredPosition - origin;

        if (Physics.SphereCast(origin, 0.4f, dir.normalized, out hit, dir.magnitude))
        {
            desiredPosition = hit.point - dir.normalized * 0.3f;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            cameraLag
        );

        Vector3 lookTarget;

        if (cameraTarget != null)
            lookTarget = cameraTarget.position;
        else
            lookTarget = car.position + Vector3.up * 1.2f;

        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSmoothness * Time.deltaTime
        );
    }

    private void HandleMouseRotation()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f)
        {
            isManuallyControlled = true;
            idleTime = 0f;

            yaw += mouseX * rotationSpeed;
            pitch -= mouseY * rotationSpeed;

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
        else
        {
            idleTime += Time.deltaTime;
        }
    }

    private void AutoCenterCamera()
    {
        if (!isReversing && idleTime > autoCenterDelay)
        {
            isManuallyControlled = false;

            yaw = Mathf.LerpAngle(yaw, car.eulerAngles.y, autoCenterSpeed * Time.deltaTime);
            pitch = Mathf.Lerp(pitch, 5f, autoCenterSpeed * Time.deltaTime);
        }
    }

    private void HandleReversing()
    {
        if (carRb == null) return;

        Vector3 velocity = carRb.linearVelocity;

        if (velocity.magnitude < 0.1f)
        {
            reverseTimer = 0f;
            isReversing = false;
            return;
        }

        float dot = Vector3.Dot(car.forward, velocity.normalized);

        float targetZ = defaultCenterOffset.z;

        if (dot < -0.5f)
        {
            reverseTimer += Time.deltaTime;

            if (reverseTimer > 2f)
                isReversing = true;
        }
        else
        {
            reverseTimer = 0f;
            isReversing = false;
        }

        offset = new Vector3(
            defaultCenterOffset.x,
            defaultCenterOffset.y,
            Mathf.Lerp(offset.z, targetZ, 1.5f * Time.deltaTime)
        );
    }

    private void HandleDynamicFOV()
    {
        if (cam == null || carRb == null) return;

        float speed = carRb.linearVelocity.magnitude;

        float targetFOV = Mathf.Lerp(baseFOV, maxFOV, speed / 50f);

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovSpeed * Time.deltaTime);
    }
}