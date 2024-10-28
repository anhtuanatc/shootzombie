using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;         // Maximum health of the player
    private int currentHealth;          // Current health of the player

    void Start()
    {
        currentHealth = maxHealth;      // Set health to maximum at the start
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;  // Decrease health by the damage amount

        if (currentHealth <= 0)
        {
            Die();                      // Trigger the Die method if health is zero or below
        }
    }

    void Die()
    {
        // Handle player defeat, game over, or respawn logic here
        Debug.Log("Player defeated! Game Over.");
        Destroy(gameObject);

    }
}
