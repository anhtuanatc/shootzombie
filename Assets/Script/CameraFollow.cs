using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;   // Reference to the player's transform
    public Vector3 offset;     // Offset distance between the camera and player

    void Start()
    {
        // Set a default offset if you want (adjust this based on your scene setup)
        offset = new Vector3(0, 12, -11);
    }

    void LateUpdate()
    {
        // Update the camera's position to follow the player with the offset
        if(player != null)
        {
            transform.position = player.position + offset;
        }

    }
}
