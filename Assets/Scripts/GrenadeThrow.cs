using System;
using System.Collections;
using System.Collections.Generic;
using GPUSmoke;
using UnityEngine;
using Random = UnityEngine.Random;


public class GrenadeThrowController : MonoBehaviour
{
    [Header("Smoke")]
    // [SerializeField] private SmokeSource smokeSource;
    // [SerializeField] private float minLifespan = 0f;
    // [SerializeField] private float maxLifespan = 1f;
    // [SerializeField] private float smokeDuration;

    [Header("Launcher")] 
    [SerializeField] private float speed;
    [SerializeField] private GameObject grenadePrefab;
    [SerializeField] private SmokeSystem grenadeSmokeSystem;
    
    void Start()
    {

    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Quote))
        {
            Launch();    
        }
    }

    void Launch()
    {
        //StartCoroutine(OnSmoke());
        GameObject grenade = Instantiate(grenadePrefab, transform.position, transform.rotation);
        grenade.GetComponent<Rigidbody>().velocity = transform.forward * speed + Vector3.up * 2;
        //set random angular velocity
        grenade.GetComponent<Rigidbody>().angularVelocity = Random.insideUnitSphere * 10;
        grenade.GetComponentInChildren<SmokeSource>().SmokeSystem = grenadeSmokeSystem;
    }

    // IEnumerator OnSmoke()
    // {
    //     smokeSource.MinLifespan = minLifespan;
    //     smokeSource.MaxLifespan = maxLifespan;
    //     yield return new WaitForSeconds(smokeDuration);
    //     smokeSource.MinLifespan = 0f;
    //     smokeSource.MaxLifespan = 0f;
    // }
}
