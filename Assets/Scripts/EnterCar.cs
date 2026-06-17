using UnityEngine;
using System.Collections;

public class EnterCar : MonoBehaviour
{
    public float detectionRange = 10f; // Range to find cars
    public string carTag = "Car"; // Tag assigned to all car GameObjects
    public bool insideCar;
    public bool driving, opening;

    private Animator playerAnimator; // Animator component on the player
    private MonoBehaviour playerMovementScript; // Movement script on the player
    private CharacterController characterController; // CharacterController component on the player
    private PlayerShooting playerShooting;

    private Transform drivingPos,enterPosition, enterPositionNPC; // Driving position for later movement
    public GameObject nearestCar, cam, gun;
    GameObject ped;
    public GameObject OnfootControls, driveControls;
    public CarController currentcar;


    private void Awake()
    {
        // Get references to required components
        playerAnimator = GetComponent<Animator>();
        playerMovementScript = GetComponent<MonoBehaviour>(); // Replace with the specific type if needed
        characterController = GetComponent<CharacterController>();
        playerShooting = GetComponent<PlayerShooting>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            CheckCar();
        }
        if (driving)
        {
            transform.position = drivingPos.position;
            transform.rotation = drivingPos.rotation;
        }
        Application.targetFrameRate = 60;
    }

    void TryEnterNearestCar()
    {
        // Find all cars in range
        GameObject[] cars = GameObject.FindGameObjectsWithTag(carTag);
        nearestCar = null;
        float shortestDistance = detectionRange;

        foreach (var car in cars)
        {
            float distance = Vector3.Distance(transform.position, car.transform.position);
            if (distance < shortestDistance)
            {
                nearestCar = car;
                shortestDistance = distance;
            }
        }

        if (nearestCar != null)
        {
            GetInsideCar(nearestCar);
        }
        else
        {
            Debug.Log("No cars in range!");
        }
    }

    void CarCamera()
    {
        cam.GetComponent<CameraScript>().enabled = false;
        cam.GetComponent<CarCameraController>().enabled = true;
        cam.GetComponent<CarCameraController>().SetCar(nearestCar);
    }

    void PlayerCamera()
    {
        cam.GetComponent<CameraScript>().enabled = true;
        cam.GetComponent<CarCameraController>().enabled = false;
    }

    void GetInsideCar(GameObject car)
    {
        OnfootControls.SetActive(false);
        driveControls.SetActive(true);
        opening = true;
        enterPosition = car.transform.Find("EnterPosition");
        drivingPos = car.transform.Find("DrivingPosition");
        CarCamera();

        if (enterPosition == null || drivingPos == null)
        {
            Debug.LogWarning("EnterPosition or DrivingPosition transform not found on the car!");
            return;
        }

        // Move the player to the enter position
        transform.position = enterPosition.position;
        transform.rotation = enterPosition.rotation;

        // Disable movement and character controller
        if (characterController != null)
        {
            characterController.enabled = false;
        }
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }
        playerShooting.enabled = false;
        CarAI carai = nearestCar.GetComponent<CarAI>();
        ped = carai.driver;
        carai.enabled = false;
        StartCoroutine(ThrowOutPed());
        nearestCar.GetComponent<Rigidbody>().isKinematic = true;
        gun.SetActive(false);

        // Play the enter car animation
        playerAnimator.SetBool("driving", true);
        car.transform.Find("door").GetComponent<Animation>().Play();

        // Set insideCar state and start monitoring animation
        insideCar = true;
        StartCoroutine(WaitForDrivingAnimationToStart());

        Debug.Log("Player started entering the car!");
    }

    IEnumerator WaitForDrivingAnimationToStart()
    {
        yield return new WaitForSeconds(3.15f); // Wait for the "enter_car" animation to finish

        // Once "enter_car" animation finishes, move the player to the driving position
        MoveToDrivingPosition();

        // Enable car controls and trigger the "driving" animation
        currentcar=nearestCar.GetComponent<CarController>();
        currentcar.enabled = true;
        nearestCar.GetComponent<Rigidbody>().isKinematic = false;
        playerAnimator.SetBool("driving", true);  // Trigger the "driving" animation
        opening = false;

        Debug.Log("Player has finished entering the car and is now driving.");
    }

    void MoveToDrivingPosition()
    {
        if (drivingPos != null)
        {
            transform.position = drivingPos.position;
            transform.rotation = drivingPos.rotation;
            driving = true;
            Debug.Log("Player moved to driving position!");
        }
        else
        {
            Debug.LogWarning("Driving position is not assigned!");
        }
    }

    void ExitCar()
    {
        
        if (opening) return; // Prevent the player from exiting while the process is ongoing

        opening = true;

        // Optional: Allow exit even if the car is moving, or modify speed check as needed
        if (nearestCar.GetComponent<Rigidbody>().linearVelocity.magnitude < 2f)
        {
            OnfootControls.SetActive(true);
            driveControls.SetActive(false);
            nearestCar.GetComponent<Rigidbody>().isKinematic = true;
            PlayerCamera();
            nearestCar.GetComponent<CarController>().enabled = false;
            driving = false;
            playerAnimator.SetBool("driving", false);
            nearestCar.transform.Find("door").GetComponent<Animation>().Play();

            Transform exitPosition = nearestCar.transform.Find("ExitPosition");
            if (exitPosition == null)
            {
                Debug.LogWarning("Exit position is null! Cannot exit car.");
                opening = false; // Reset opening flag if exit fails
                return;
            }

            transform.position = exitPosition.position;
            transform.rotation = exitPosition.rotation;

            StartCoroutine(MoveOutside());  // Proceed with the exit animation
        }
        else
        {
            Debug.Log("Car is moving too fast to exit!");
            opening = false; // Reset opening flag even if exit isn't allowed due to speed
        }
    }

    IEnumerator MoveOutside()
    {
        // Wait for the exit animation to play (ensure the correct state is checked here)
        while (!playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Move"))  // Change "Move" to your actual exit animation name if needed
        {
            yield return null; // Wait for the next frame
        }

        // Move to the out position
        Transform outPosition = nearestCar.transform.Find("OutPosition");
        if (outPosition != null)
        {
            transform.position = outPosition.position;
            transform.rotation = outPosition.rotation;
        }
        else
        {
            Debug.LogWarning("Out position is null! Cannot move outside.");
        }

        // Final cleanup after exiting
        opening = false;
        playerMovementScript.enabled = true;
        playerShooting.enabled = true;
        characterController.enabled = true;
        gun.SetActive(true);

        Debug.Log("Player has exited the car.");
    }

    IEnumerator ThrowOutPed()
    {
        enterPositionNPC = nearestCar.transform.Find("EnterPositionNPC");
        ped.transform.position = enterPositionNPC.position;
        ped.transform.rotation = enterPositionNPC.rotation;
        ped.transform.Find("Body").gameObject.GetComponent<Animator>().SetBool("driving", false);
        Collider[] childColliders = ped.GetComponentsInChildren<Collider>();
        foreach (Collider collider in childColliders)
        {
            collider.enabled = true; // Enable each collider
        }
        //ped.GetComponent<Rigidbody>().isKinematic = false;
        Animator pedAnimator = ped.transform.Find("Body").gameObject.GetComponent<Animator>();

        // Wait until the blend tree named "Walking" starts
        while (!pedAnimator.GetCurrentAnimatorStateInfo(0).IsName("Walking"))
        {
            yield return null; // Wait for the next frame
        }

        ped.transform.position = new Vector3(
            ped.transform.position.x, 
            ped.transform.position.y+1f, 
            ped.transform.position.z
        );
        
        ped.GetComponent<Pedestrians>().enabled = true;
        ped.GetComponent<Collider>().enabled = true;
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(ped.GetComponent<Pedestrians>().BoostSpeed());
    }

    public void CheckCar()
    {
        if (!opening)
        {
            if (!driving)
            {
                TryEnterNearestCar();
                
            }
            else
            {
                ExitCar();
                
            }
        }
    }

    public void AccelerateCar()
    {
        currentcar.MobileControls(1.5f);
    }
    public void ReverseCar()
    {
        currentcar.MobileControls(-1.5f);
    }
    public void Brake()
    {
        currentcar.ApplyBrakes();
    }
    public void IdleCar()
    {
        currentcar.MobileControls(0);
    }
    public void SteerLeft()
    {
        currentcar.Steer(-1f);
    }
    public void SteerRight()
    {
        currentcar.Steer(1f);
    }
    public void ResetSteer()
    {
        currentcar.Steer(0f);
    }
}
