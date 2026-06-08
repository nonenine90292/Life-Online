using UnityEngine;
using System.Collections;

public class CarController : MonoBehaviour
{
    [Header("Car Settings")]
    public float acceleration = 1000f;
    public float maxSpeed = 50f;
    public float steeringAngle = 30f;
    public float brakeForce = 3000f;

    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("Wheel Transforms")]
    public Transform frontLeftTransform;
    public Transform frontRightTransform;
    public Transform rearLeftTransform;
    public Transform rearRightTransform;

    [Header("Wheel Flip Settings")]
    public bool flipFrontLeft;
    public bool flipFrontRight;
    public bool flipRearLeft;
    public bool flipRearRight;

    [Header("Live Data")]
    public float speed;

    [Header("Engine System")]
    public bool engineOn;
    public KeyCode engineKey = KeyCode.E;
    public float engineStartDelay = 1f;

    [Header("Driving Feel")]
    public float throttleSmoothness = 4f;
    public float steeringSmoothness = 5f;
    public float tractionControl = 0.98f;
    public float downforce = 50f;

    [Header("Fake Engine")]
    public float engineRPM;
    public float minRPM = 800f;
    public float maxRPM = 7500f;
    public float rpmSmoothness = 4f;

    private float inputVertical;
    private float inputHorizontal;
    private bool isBraking;

    public float mobileControls;
    public float mobileControls2;

    private float currentThrottle;
    private float currentSteer;

    private Rigidbody rb;
    private bool isStarting;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        inputVertical = Input.GetAxis("Vertical");
        inputHorizontal = Input.GetAxis("Horizontal");

        isBraking = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.F);

        UpdateWheelPositions();

        UpdateSpeed();

        HandleEngineInput();

        UpdateFakeRPM();
    }

    void FixedUpdate()
    {
        ApplyDownforce();

        Drive();

        Steer();

        Brake();

        ApplyTractionControl();
    }

    public void Drive()
    {
        if (!engineOn) return;

        float targetInput = mobileControls != 0 ? mobileControls : inputVertical;

        currentThrottle = Mathf.Lerp(currentThrottle, targetInput, throttleSmoothness * Time.fixedDeltaTime);

        float motorTorque = currentThrottle * acceleration;

        rearLeftWheel.motorTorque = motorTorque;

        rearRightWheel.motorTorque = motorTorque;

        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rearLeftWheel.motorTorque = 0;
            rearRightWheel.motorTorque = 0;
        }
    }

    private void Steer()
    {
        float targetSteer = inputHorizontal + mobileControls2;

        currentSteer = Mathf.Lerp(currentSteer, targetSteer, steeringSmoothness * Time.fixedDeltaTime);

        float steer = currentSteer * steeringAngle;

        frontLeftWheel.steerAngle = steer;

        frontRightWheel.steerAngle = steer;
    }

    private void Brake()
    {
        if (isBraking)
        {
            ApplyBrakes();
        }
        else
        {
            frontLeftWheel.brakeTorque = 0;
            frontRightWheel.brakeTorque = 0;
            rearLeftWheel.brakeTorque = 0;
            rearRightWheel.brakeTorque = 0;
        }
    }

    public void ApplyBrakes()
    {
        frontLeftWheel.brakeTorque = brakeForce;
        frontRightWheel.brakeTorque = brakeForce;
        rearLeftWheel.brakeTorque = brakeForce;
        rearRightWheel.brakeTorque = brakeForce;
    }

    private void ApplyTractionControl()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);

        localVelocity.x *= tractionControl;

        rb.linearVelocity = transform.TransformDirection(localVelocity);
    }

    private void ApplyDownforce()
    {
        rb.AddForce(-transform.up * downforce * rb.linearVelocity.magnitude);
    }

    private void UpdateWheelPositions()
    {
        UpdateWheelPosition(frontLeftWheel, frontLeftTransform, flipFrontLeft);
        UpdateWheelPosition(frontRightWheel, frontRightTransform, flipFrontRight);
        UpdateWheelPosition(rearLeftWheel, rearLeftTransform, flipRearLeft);
        UpdateWheelPosition(rearRightWheel, rearRightTransform, flipRearRight);
    }

    private void UpdateWheelPosition(WheelCollider collider, Transform wheelTransform, bool shouldFlip)
    {
        Vector3 pos;
        Quaternion rot;

        collider.GetWorldPose(out pos, out rot);

        if (shouldFlip)
        {
            rot *= Quaternion.Euler(0, 180, 0);
        }

        wheelTransform.position = pos;

        wheelTransform.rotation = rot;
    }

    private void UpdateSpeed()
    {
        speed = rb.linearVelocity.magnitude * 3.6f;

        if (Mathf.Abs(inputVertical) < 0.01f && speed < 10f)
        {
            Vector3 currentVelocity = rb.linearVelocity;

            rb.linearVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, Time.deltaTime * 2f);
        }
    }

    private void UpdateFakeRPM()
    {
        float targetRPM = Mathf.Lerp(minRPM, maxRPM, Mathf.Abs(currentThrottle) + rb.linearVelocity.magnitude / maxSpeed);

        engineRPM = Mathf.Lerp(engineRPM, targetRPM, rpmSmoothness * Time.deltaTime);
    }

    private void HandleEngineInput()
    {
        if (Input.GetKeyDown(engineKey) && !isStarting)
        {
            if (engineOn)
            {
                TurnOffEngine();
            }
            else
            {
                StartCoroutine(StartEngine());
            }
        }
    }

    private IEnumerator StartEngine()
    {
        isStarting = true;

        yield return new WaitForSeconds(engineStartDelay);

        engineOn = true;

        isStarting = false;
    }

    private void TurnOffEngine()
    {
        engineOn = false;

        currentThrottle = 0f;
    }

    public void ToggleEngine()
    {
        if (isStarting) return;

        if (engineOn)
        {
            TurnOffEngine();
        }
        else
        {
            StartCoroutine(StartEngine());
        }
    }

    public void MobileControls(float dir)
    {
        mobileControls = Mathf.Clamp(dir, -1f, 1f);
    }

    public void Steer(float dir)
    {
        mobileControls2 = Mathf.Clamp(dir, -1f, 1f);
    }
}