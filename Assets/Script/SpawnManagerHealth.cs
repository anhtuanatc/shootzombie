using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManagerHealth : MonoBehaviour
{
    public int totalHealth = 200;  // Total "health" or "lives" of the SpawnManager

    public void TakeDamage(int damageAmount)
    {
        totalHealth -= damageAmount;

        // Check if health has dropped to zero
        if (totalHealth <= 0)
        {
            WinGame();   // Call the WinGame method
        }
    }

    void WinGame()
    {
        // This method is called when the SpawnManager's health reaches zero
        Destroy(gameObject);  // Destroy the SpawnManager
        Debug.Log("You win! All enemies defeated.");
    }
}
