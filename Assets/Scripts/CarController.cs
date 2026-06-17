using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Car Settings")]
    public float acceleration = 1000f;   // Acceleration force
    public float maxSpeed = 50f;         // Maximum speed
    public float steeringAngle = 30f;    // Maximum steering angle
    public float brakeForce = 3000f;     // Brake force

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
    public bool flipFrontLeft;  // Should the front left wheel be flipped?
    public bool flipFrontRight; // Should the front right wheel be flipped?
    public bool flipRearLeft;   // Should the rear left wheel be flipped?
    public bool flipRearRight;  // Should the rear right wheel be flipped?

    [Header("Live Data")]
    public float speed; // Shows the live speed of the car

    private float inputVertical;
    private float inputHorizontal;
    private bool isBraking;
    public float mobileControls, mobileControls2;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Get input from player
        inputVertical = Input.GetAxis("Vertical");     // Forward and backward
        inputHorizontal = Input.GetAxis("Horizontal"); // Left and right
        isBraking = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.F);       // Braking

        // Update visual wheel positions
        UpdateWheelPositions();

        // Update live speed
        UpdateSpeed();
    }

    void FixedUpdate()
    {
        // Apply movement and steering
        Drive();
        Steer();
        Brake();
    }

    public void Drive()
    {
        // Use mobileControls if it's non-zero; otherwise, fall back to inputVertical
        float effectiveInput = mobileControls != 0 ? mobileControls : inputVertical;
        float motorTorque = effectiveInput * acceleration;

        // Apply motor torque to rear wheels
        rearLeftWheel.motorTorque = motorTorque;
        rearRightWheel.motorTorque = motorTorque;

        // Limit speed
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rearLeftWheel.motorTorque = 0;
            rearRightWheel.motorTorque = 0;
        }
    }

    private void Steer()
    {
        // Apply steering to front wheels
        float steer = (inputHorizontal+mobileControls2) * steeringAngle;

        frontLeftWheel.steerAngle = steer;
        frontRightWheel.steerAngle = steer;
    }

    private void Brake()
    {
        if (isBraking)
        {
            // Apply brake torque to all wheels
            ApplyBrakes();
        }
        else
        {
            // Reset brake torque when not braking
            frontLeftWheel.brakeTorque = 0;
            frontRightWheel.brakeTorque = 0;
            rearLeftWheel.brakeTorque = 0;
            rearRightWheel.brakeTorque = 0;
        }
    }

    public void ApplyBrakes()
    {
        // Apply brake torque to all wheels when braking
        frontLeftWheel.brakeTorque = brakeForce;
        frontRightWheel.brakeTorque = brakeForce;
        rearLeftWheel.brakeTorque = brakeForce;
        rearRightWheel.brakeTorque = brakeForce;
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

        // Get the position and rotation from the collider
        collider.GetWorldPose(out pos, out rot);

        // If the wheel should be flipped, rotate it 180 degrees around the Y-axis
        if (shouldFlip)
        {
            rot *= Quaternion.Euler(0, 180, 0); // Flip the wheel by 180 degrees on the Y-axis
        }

        // Apply to the wheel transform
        wheelTransform.position = pos;
        wheelTransform.rotation = rot;
    }

    private void UpdateSpeed()
    {
        // Calculate the live speed of the car in kilometers per hour (kph)
        speed = rb.linearVelocity.magnitude * 3.6f; // Magnitude of velocity vector in kph

        // Lerp the speed to 0 if it's very low and the player is not accelerating or reversing
        if (Mathf.Abs(inputVertical) < 0.01f && speed < 10f)
        {
            // Gradually reduce the velocity using Lerp
            Vector3 currentVelocity = rb.linearVelocity;
            rb.linearVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, Time.deltaTime * 2f); // Adjust the "2f" to control the deceleration speed
        }
    }

    public void MobileControls(float dir)
    {
        mobileControls = dir;
    }

    // Public function to steer left
    public void Steer(float dir)
    {
        mobileControls2 = dir;
    }
      
}
