using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3.5f;
    public float sprintSpeed = 6.5f;
    public float rotationSpeed = 12f;
    public float gravity = -9.81f;

    [Header("References")]
    public Transform cameraTransform;
    public Transform upperBodyBone;

    private CharacterController controller;
    private Animator animator;

    private Vector3 moveDirection;
    private Vector3 velocity;

    private float currentSpeed;

    private bool isSprinting = false;
    public bool isAiming = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleAiming();
        HandleMovement();
        HandleGravity();
        HandleRotation();
        HandleAnimations();
        HandleMovementInput();
        UpdateAimLayer();
    }

    void LateUpdate()
    {
        HandleUpperBodyAim();
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        moveDirection = (forward * vertical + right * horizontal).normalized;

        isSprinting =
            Input.GetKey(KeyCode.LeftShift) &&
            vertical > 0.1f &&
            !isAiming;

        currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        controller.Move(moveDirection * currentSpeed * Time.deltaTime);
    }

    void HandleGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);
    }

    void HandleRotation()
    {
        if (isAiming)
        {
            Vector3 lookDirection = cameraTransform.forward;

            lookDirection.y = 0f;

            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
        else if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    void HandleAnimations()
    {
        if (animator == null)
            return;

        float speed = moveDirection.magnitude;

        if (isSprinting)
            speed = 1f;
        else
            speed *= 0.5f;

        animator.SetFloat(
            "Speed",
            speed,
            0.1f,
            Time.deltaTime
        );
    }

    void HandleMovementInput()
    {
        if (animator == null)
            return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        animator.SetFloat(
            "Horizontal",
            horizontal,
            0.1f,
            Time.deltaTime
        );

        animator.SetFloat(
            "Vertical",
            vertical,
            0.1f,
            Time.deltaTime
        );
    }

    void HandleAiming()
    {
        isAiming = Input.GetMouseButton(1);

        animator.SetBool("IsAiming", isAiming);
    }

    void HandleUpperBodyAim()
    {
        if (!isAiming)
            return;

        if (upperBodyBone == null)
            return;

        float pitch = cameraTransform.eulerAngles.x;

        if (pitch > 180f)
            pitch -= 360f;

        Quaternion targetRotation = Quaternion.Euler(
            pitch,
            0f,
            0f
        );

        upperBodyBone.localRotation = Quaternion.Lerp(
            upperBodyBone.localRotation,
            targetRotation,
            Time.deltaTime * 10f
        );
    }

    void UpdateAimLayer()
    {
        if (animator == null)
            return;

        int layerIndex = animator.GetLayerIndex("AimingLayer");

        if (layerIndex == -1)
            return;

        float targetWeight = isAiming ? 1f : 0f;

        float currentWeight = animator.GetLayerWeight(layerIndex);

        animator.SetLayerWeight(
            layerIndex,
            Mathf.Lerp(
                currentWeight,
                targetWeight,
                Time.deltaTime * 8f
            )
        );
    }
}