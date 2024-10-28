using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public Transform player;           // Reference to the player's Transform
    public float speed = 3f;           // Speed at which the enemy moves toward the player
    public int damageToPlayer = 20;    // Damage this enemy deals to the player on collision

    void Update()
    {
        // Check if player Transform is assigned
        if (player != null)
        {
            // Move toward the player's position
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;

            // Optional: Face the player
            transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
        }
    }

    // Method to set the player Transform from another script
    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
    }
    void OnCollisionEnter(Collision collision)
    {
        // Check if the enemy collided with the player
        PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            // Apply damage to the player
            playerHealth.TakeDamage(damageToPlayer);
        }
    }
}
