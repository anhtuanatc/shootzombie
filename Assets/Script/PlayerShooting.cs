using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public Transform firePoint;    // The point where the bullets will originate (typically attached to the player)
    public GameObject bulletPrefab; // The bullet prefab to instantiate
    public float bulletForce = 20f; // Speed of the bullet

    void Update()
    {
        // Rotate the character to face the mouse
        RotateToMouse();

        // Fire when the left mouse button is clicked
        if (Input.GetButtonDown("Fire1"))
        {
            Shoot();
        }
    }

    void RotateToMouse()
    {
        // Ray from the mouse position to the game world
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero); // Assume the ground is at y = 0
        float rayDistance;

        // Check if the ray hits the ground plane
        if (groundPlane.Raycast(ray, out rayDistance))
        {
            // Get the point where the mouse ray intersects with the ground
            Vector3 targetPoint = ray.GetPoint(rayDistance);

            // Calculate direction to the target point (where mouse is pointing)
            Vector3 direction = (targetPoint - transform.position).normalized;
            direction.y = 0; // Keep the player flat on the ground (ignore y-axis rotation)

            // Rotate the character to face the target point (mouse position)
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = lookRotation;
        }
    }

    void Shoot()
    {
        // Instantiate the bullet at the firePoint
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        // Apply force to the bullet to shoot it forward from the character's facing direction
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.AddForce(firePoint.forward * bulletForce, ForceMode.Impulse);
    }
}
