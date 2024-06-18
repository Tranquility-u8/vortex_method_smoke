using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RocketController : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float launchForce;
    [SerializeField] private float y_border;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        rb.AddForce(transform.forward * launchForce, ForceMode.Impulse);
    }

    void Update()
    {
        if (transform.position.y < y_border)
        {
            Destroy(this.gameObject);
        }
        
        if (rb.velocity != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(rb.velocity);
        }
    }
}
