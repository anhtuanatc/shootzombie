using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoDestroyEffect : MonoBehaviour
{
    private ParticleSystem ps;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        // Destroy the GameObject when the particle system has stopped emitting
        if (ps != null && !ps.IsAlive())
        {
            Destroy(gameObject);
        }
    }
}
