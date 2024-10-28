using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 100;       // Maximum health of the enemy
    private int currentHealth;        // Current health of the enemy

    void Start()
    {
        currentHealth = maxHealth;    // Initialize health to max at the start
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;    // Reduce health by damage amount

        if (currentHealth <= 0)
        {
            Die();    // Call Die method if health is zero or less
        }
    }

    void Die()
    {
        // Destroy the enemy game object
        Destroy(gameObject);
        // Optional: Add any death effects or sounds here
    }
}
