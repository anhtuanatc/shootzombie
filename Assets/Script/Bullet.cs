using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage = 20;                // Damage dealt by the bullet
    public float lifetime = 3f;            // Time before the bullet is destroyed automatically
    public GameObject impactEffectPrefab;  // Reference to the impact effect prefab

    void Start()
    {
        // Automatically destroy the bullet after 'lifetime' seconds
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if the bullet hit an enemy with EnemyHealth
        EnemyHealth enemyHealth = collision.gameObject.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            // Apply damage to the enemy
            enemyHealth.TakeDamage(damage);

            // Create the impact effect at the collision point and rotation
            var impactEffect = Instantiate(impactEffectPrefab, collision.contacts[0].point, Quaternion.identity);

            // Destroy the bullet after it hits an enemy
            Destroy(gameObject);
            Destroy(impactEffect, 0.2f);

            return;
        }

        // Check if the bullet hit the SpawnManager or other object with SpawnManagerHealth
        SpawnManagerHealth spawnManagerHealth = collision.gameObject.GetComponent<SpawnManagerHealth>();
        if (spawnManagerHealth != null)
        {
            // Apply damage to the SpawnManager
            spawnManagerHealth.TakeDamage(damage);

            // Create the impact effect at the collision point and rotation
            var impactEffect = Instantiate(impactEffectPrefab, collision.contacts[0].point, Quaternion.identity);

            // Destroy the bullet after it hits the SpawnManager
            Destroy(gameObject);
            Destroy(impactEffect, 0.2f);

            return;
        }

        // Optional: Destroy the bullet if it hits the floor
        if (collision.gameObject.CompareTag("Floor"))
        {
            var impactEffect = Instantiate(impactEffectPrefab, collision.contacts[0].point, Quaternion.identity);
            Destroy(gameObject);
            Destroy(impactEffect, 0.2f);
        }

    }
}