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

    private Transform drivingPos, enterPosition, enterPositionNPC; // Driving positions
    public GameObject nearestCar, cam, gun;
    GameObject ped;
    public GameObject OnfootControls, driveControls;
    public CarController currentcar;

    private void Awake()
    {
        playerAnimator = GetComponent<Animator>();
        playerMovementScript = GetComponent<MonoBehaviour>(); 
        characterController = GetComponent<CharacterController>();
        playerShooting = GetComponent<PlayerShooting>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            CheckCar();
        }
        
        // Mientras maneja, fijar posición sin excepción
        if (driving && drivingPos != null)
        {
            transform.position = drivingPos.position;
            transform.rotation = drivingPos.rotation;
        }
        Application.targetFrameRate = 60;
    }

    void TryEnterNearestCar()
    {
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
        if (cam == null || nearestCar == null) return;
        if (cam.GetComponent<CameraScript>() != null) cam.GetComponent<CameraScript>().enabled = false;
        if (cam.GetComponent<CarCameraController>() != null)
        {
            cam.GetComponent<CarCameraController>().enabled = true;
            cam.GetComponent<CarCameraController>().SetCar(nearestCar);
        }
    }

    void PlayerCamera()
    {
        if (cam == null) return;
        if (cam.GetComponent<CameraScript>() != null) cam.GetComponent<CameraScript>().enabled = true;
        if (cam.GetComponent<CarCameraController>() != null) cam.GetComponent<CarCameraController>().enabled = false;
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
            Debug.LogWarning("EnterPosition or DrivingPosition missing!");
            opening = false;
            return;
        }

        // Posicionar jugador en la puerta
        transform.position = enterPosition.position;
        transform.rotation = enterPosition.rotation;

        // Desactivar físicas y control a pie para que no interfieran con la animación
        if (characterController != null) characterController.enabled = false;
        if (playerMovementScript != null) playerMovementScript.enabled = false;
        if (playerShooting != null) playerShooting.enabled = false;

        CarAI carai = nearestCar.GetComponent<CarAI>();
        if (carai != null)
        {
            ped = carai.driver;
            carai.enabled = false;
            if (ped != null) StartCoroutine(ThrowOutPed());
        }

        nearestCar.GetComponent<Rigidbody>().isKinematic = true;
        if (gun != null) gun.SetActive(false);

        // --- LÓGICA DE ANIMACIÓN SEGURA ---
        // Forzamos directamente al Animator a reproducir el estado "Entering Car" desde el inicio.
        // Esto rompe cualquier bucle causado por malas configuraciones de flechas de transición.
        playerAnimator.Play("Entering Car", 0, 0f);
        
        // Ponemos driving en false al iniciar para que sepa que APENAS está entrando
        playerAnimator.SetBool("driving", false);

        Transform door = car.transform.Find("door");
        if (door != null && door.GetComponent<Animation>() != null)
        {
            door.GetComponent<Animation>().Play();
        }

        insideCar = true;
        StartCoroutine(WaitForDrivingAnimationToStart());
    }

    IEnumerator WaitForDrivingAnimationToStart()
    {
        // Espera exacta a que termine de reproducirse la animación de subir (3.15 segundos)
        yield return new WaitForSeconds(3.15f); 

        MoveToDrivingPosition();

        currentcar = nearestCar.GetComponent<CarController>();
        if (currentcar != null) currentcar.enabled = true;
        
        nearestCar.GetComponent<Rigidbody>().isKinematic = false;
        
        // Pasado el tiempo, cambiamos a true. Esto activará la transición hacia la animación de manejo fija.
        playerAnimator.SetBool("driving", true);  
        opening = false;
    }

    void MoveToDrivingPosition()
    {
        if (drivingPos != null)
        {
            transform.position = drivingPos.position;
            transform.rotation = drivingPos.rotation;
            driving = true;
        }
    }

    void ExitCar()
    {
        if (opening) return; 
        opening = true;

        if (nearestCar.GetComponent<Rigidbody>().linearVelocity.magnitude < 2f)
        {
            OnfootControls.SetActive(true);
            driveControls.SetActive(false);
            nearestCar.GetComponent<Rigidbody>().isKinematic = true;
            PlayerCamera();
            
            CarController carCtrl = nearestCar.GetComponent<CarController>();
            if (carCtrl != null) carCtrl.enabled = false;
            
            driving = false;
            
            // Enviamos la orden de salir
            playerAnimator.SetBool("driving", false);
            playerAnimator.Play("Exiting Car", 0, 0f);

            Transform door = nearestCar.transform.Find("door");
            if (door != null && door.GetComponent<Animation>() != null)
            {
                door.GetComponent<Animation>().Play();
            }

            Transform exitPosition = nearestCar.transform.Find("ExitPosition");
            if (exitPosition != null)
            {
                transform.position = exitPosition.position;
                transform.rotation = exitPosition.rotation;
            }

            StartCoroutine(MoveOutside());  
        }
        else
        {
            opening = false; 
        }
    }

    IEnumerator MoveOutside()
    {
        yield return new WaitForSeconds(2.5f); // Tiempo de espera para bajarse del coche

        Transform outPosition = nearestCar.transform.Find("OutPosition");
        if (outPosition != null)
        {
            transform.position = outPosition.position;
            transform.rotation = outPosition.rotation;
        }

        opening = false;
        if (playerMovementScript != null) playerMovementScript.enabled = true;
        if (playerShooting != null) playerShooting.enabled = true;
        if (characterController != null) characterController.enabled = true;
        if (gun != null) gun.SetActive(true);
    }

    IEnumerator ThrowOutPed()
    {
        if (ped == null) yield break;
        
        enterPositionNPC = nearestCar.transform.Find("EnterPositionNPC");
        if (enterPositionNPC != null)
        {
            ped.transform.position = enterPositionNPC.position;
            ped.transform.rotation = enterPositionNPC.rotation;
        }
        
        Transform pedBody = ped.transform.Find("Body");
        if (pedBody != null && pedBody.GetComponent<Animator>() != null)
        {
            pedBody.GetComponent<Animator>().SetBool("driving", false);
        }

        Collider[] childColliders = ped.GetComponentsInChildren<Collider>();
        foreach (Collider collider in childColliders)
        {
            collider.enabled = true; 
        }

        // CORRECCIÓN NAVMESH REPETITIVO: Desactivamos el agente antes de moverlo de coordenadas
        UnityEngine.AI.NavMeshAgent pedAgent = ped.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (pedAgent != null) pedAgent.enabled = false;

        yield return new WaitForSeconds(0.2f);
        
        if (ped != null)
        {
            ped.transform.position = new Vector3(
                ped.transform.position.x, 
                ped.transform.position.y + 0.1f, 
                ped.transform.position.z
            );
            
            // Forzamos la actualización de posición en el NavMesh de Unity
            if (pedAgent != null)
            {
                pedAgent.Warp(ped.transform.position);
                pedAgent.enabled = true;
            }

            Pedestrians pedScript = ped.GetComponent<Pedestrians>();
            if (pedScript != null)
            {
                pedScript.enabled = true;
                ped.GetComponent<Collider>().enabled = true;
                StartCoroutine(pedScript.BoostSpeed());
            }
        }
    }

    public void CheckCar()
    {
        if (!opening)
        {
            if (!driving) TryEnterNearestCar();
            else ExitCar();
        }
    }

    public void MobileControls(float direction) { if (currentcar != null && currentcar.enabled) currentcar.MobileControls(direction); }
    public void AccelerateCar() => MobileControls(1.5f);
    public void ReverseCar() => MobileControls(-1.5f);
    public void IdleCar() => MobileControls(0f);
    public void Brake() { if (currentcar != null && currentcar.enabled) currentcar.ApplyBrakes(); }
    public void Steer(float value) { if (currentcar != null && currentcar.enabled) currentcar.Steer(value); }
    public void SteerLeft() => Steer(-1f);
    public void SteerRight() => Steer(1f);
    public void ResetSteer() => Steer(0f);
}