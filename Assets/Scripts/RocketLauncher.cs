using System.Collections;
using System.Collections.Generic;
using GPUSmoke;
using UnityEngine;



public class RocketLauncher : MonoBehaviour
{
    [Header("Smoke")] 
    [SerializeField] private ParticleSystem particleSystem;
    [SerializeField] private SmokeSource smokeSource;
    [SerializeField] private float minLifespan = 0f;
    [SerializeField] private float maxLifespan = 1f;
    [SerializeField] private float smokeDuration;

    [Header("Launcher")] 
    [SerializeField] private Transform launcherPoint;
    [SerializeField] private GameObject rocketPrefab;
    [SerializeField] private SmokeSystem rocketSmokeSystem;
    
    void Start()
    {
        particleSystem.Stop();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Launch();    
        }
    }

    void Launch()
    {
        particleSystem.Play();
        StartCoroutine(OnSmoke());
        GameObject rocket = Instantiate(rocketPrefab, launcherPoint.position, launcherPoint.rotation);
        rocket.GetComponentInChildren<SmokeSource>().SmokeSystem = rocketSmokeSystem;
    }

    IEnumerator OnSmoke()
    {
        smokeSource.MinLifespan = minLifespan;
        smokeSource.MaxLifespan = maxLifespan;
        yield return new WaitForSeconds(smokeDuration);
        smokeSource.MinLifespan = 0f;
        smokeSource.MaxLifespan = 0f;
    }
    
}
