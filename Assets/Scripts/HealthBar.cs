using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public Scrollbar healthBar;

    public float safeFallHeight = 4f;
    public float damageMultiplier = 10f;

    private CharacterController controller;
    private float highestPoint;
    private bool wasGrounded;

    void Start()
    {
        currentHealth = maxHealth;
        controller = GetComponent<CharacterController>();

        if (healthBar != null)
            healthBar.size = 1f;
    }

    void Update()
    {
        UpdateHealthBar();
        CheckFallDamage();
    }

    void CheckFallDamage()
    {
        if (controller == null) return;

        if (!controller.isGrounded)
        {
            if (transform.position.y > highestPoint)
                highestPoint = transform.position.y;
        }

        if (!wasGrounded && controller.isGrounded)
        {
            float fallHeight = highestPoint - transform.position.y;

            if (fallHeight > safeFallHeight)
            {
                float damage = (fallHeight - safeFallHeight) * damageMultiplier;
                TakeDamage(damage);
            }

            highestPoint = transform.position.y;
        }

        if (controller.isGrounded)
            highestPoint = transform.position.y;

        wasGrounded = controller.isGrounded;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }

        UpdateHealthBar();
    }

    public void Heal(float amount)
    {
        currentHealth += amount;

        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
            healthBar.size = currentHealth / maxHealth;
    }

    void Die()
    {
        gameObject.SetActive(false);
    }
}