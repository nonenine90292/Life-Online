using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject bulletPrefab;
    public Transform bulletSpawnPoint;
    public float bulletSpeed = 20f;
    public float fireRate = 0.5f;

    [Header("Aiming Settings")]
    public Transform aimTransform; // The camera or crosshair's aiming target
    public LayerMask aimLayerMask;
    private bool isAiming = false;
    private float nextFireTime = 0f;

    private Animator animator;
    public Animation gun;
    public GameObject crosshair;
    public AudioSource gunaudio;

    [Header("Effects")]
    public ParticleSystem muzzleFlash; // Reference to the particle system
    public GameObject bloodParticlePrefab; // Reference to the blood particle prefab

    [Header("Raycasting")]
    public float raycastDistance = 100f; // Raycast distance
    private bool targetDetected = false;
    private Image crosshairImage;
    private GameObject currentTarget;
    public PlayerMovement player;
    public bool headshot;

    [Header("Pedestrian Detection")]
    public float alertRadius = 15f; // Radius to notify nearby pedestrians of shooting
    public bool onpc;
    private bool isShootButtonHeld = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        crosshairImage = crosshair.GetComponent<Image>();
    }

    void Update()
    {
        isAiming = player.isAiming;
        HandleAiming();
        HandleShooting();

        if (isAiming)
        {
            CheckForTarget(); // Raycast to check for target in front of the player
        }

        if (isShootButtonHeld && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }


    void HandleAiming()
    {
        if (isAiming)
        {
            crosshair.SetActive(true);
        }
        else
        {
            crosshair.SetActive(false);
        }
    }

    void HandleShooting()
    {
        if (onpc) { 
        // For desktop, use mouse input and check if the pointer is not over UI
        if (isAiming && Input.GetMouseButton(0) && Time.time >= nextFireTime && !IsPointerOverUI())
        {
            Shoot();
            nextFireTime = Time.time + fireRate;

            if (animator != null)
            {
                gun.Play();
            }
        }
        }
    }

    void Shoot()
    {
        if (isAiming)
        {
            // Instantiate the bullet at the spawn point with the spawn point's rotation
            //GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
            gunaudio.Play();
            muzzleFlash.Play();

            if (targetDetected)
            {
                RaycastHit hit;
                Vector3 rayDirection = bulletSpawnPoint.transform.TransformDirection(Vector3.forward);
                Vector3 rayStart = bulletSpawnPoint.position;

                if (Physics.Raycast(rayStart, rayDirection, out hit, raycastDistance, aimLayerMask))
                {
                    if (hit.collider.CompareTag("npc") || hit.collider.CompareTag("npc_head"))
                    {
                        // Handle damage logic
                        Pedestrians pedestrian = hit.collider.GetComponentInParent<Pedestrians>();
                        if (headshot)
                        {
                            pedestrian.TakeDamage(10);
                        }
                        else
                        {
                            pedestrian.TakeDamage(1);
                        }

                        // Instantiate blood particle effect at hit point
                        if (bloodParticlePrefab != null)
                        {
                            GameObject bloodEffect = Instantiate(bloodParticlePrefab, hit.point, Quaternion.LookRotation(hit.normal));
                            Destroy(bloodEffect, 0.5f); // Destroy blood effect after 0.5 seconds
                        }
                    }
                }
            }

            // Notify nearby pedestrians of the shooting
            AlertNearbyPedestrians();
        }
    }

    void AlertNearbyPedestrians()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, alertRadius, aimLayerMask);

        foreach (Collider collider in hitColliders)
        {
            Pedestrians pedestrian = collider.GetComponentInParent<Pedestrians>();
            if (pedestrian != null && !pedestrian.dead)
            {
                pedestrian.OnHeardShooting(transform.position);
            }
        }
    }

    void CheckForTarget()
    {
        RaycastHit hit;
        Vector3 rayDirection = bulletSpawnPoint.transform.TransformDirection(Vector3.forward);
        Vector3 rayStart = bulletSpawnPoint.position;

        Debug.DrawRay(rayStart, rayDirection * raycastDistance, Color.red);

        if (Physics.Raycast(rayStart, rayDirection, out hit, raycastDistance, aimLayerMask))
        {
            if (hit.collider.CompareTag("npc") || hit.collider.CompareTag("npc_head"))
            {
                if (!targetDetected)
                {
                    currentTarget = hit.collider.gameObject;
                    crosshairImage.color = Color.red;
                    targetDetected = true;
                }

                if (hit.collider.CompareTag("npc_head"))
                {
                    headshot = true;
                }
            }
        }
        else
        {
            if (targetDetected)
            {
                headshot = false;
                crosshairImage.color = Color.white;
                targetDetected = false;
            }
        }
    }

    // Called when the shoot button is pressed
    public void OnShootButtonDown()
    {
        isShootButtonHeld = true;
    }

    // Called when the shoot button is released
    public void OnShootButtonUp()
    {
        isShootButtonHeld = false;
    }


    private bool IsPointerOverUI()
    {
        // Check if the pointer (touch/mouse) is over a UI element
        if (EventSystem.current != null)
        {
            return EventSystem.current.IsPointerOverGameObject();
        }
        return false;
    }
}
