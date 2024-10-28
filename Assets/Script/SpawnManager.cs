using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public GameObject[] zombiePrefabs;    // Array of enemy prefabs
    public Transform spawnPoints;        // The spawn points
    public float spawnDelay = 2f;        // Time before spawning starts
    public float spawnInterval = 5f;     // Time between each spawn
    public Transform player;             // Reference to the player's Transform

    void Start()
    {
        // Start repeatedly spawning enemies
        InvokeRepeating("SpawnEnemy", spawnDelay, spawnInterval);
        spawnPoints = gameObject.transform;
    }


    void SpawnEnemy()
    {

        if(player != null)
        {
            if (zombiePrefabs.Length == 0)
            {
                Debug.LogWarning("No enemy prefabs or spawn points assigned!");
                return;
            }

            // Randomly select an enemy prefab from the array
            int enemyIndex = Random.Range(0, zombiePrefabs.Length);
            GameObject enemyToSpawn = zombiePrefabs[enemyIndex];


            // Instantiate the enemy at the chosen spawn point's position and rotation
            GameObject enemy = Instantiate(enemyToSpawn, spawnPoints.position, spawnPoints.rotation);

            // Set the player's Transform in the EnemyAI script
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.SetPlayer(player);
            }
        }

    }

}
