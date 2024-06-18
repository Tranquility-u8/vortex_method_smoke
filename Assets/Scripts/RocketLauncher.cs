using System.Collections;
using System.Collections.Generic;
using GPUSmoke;
using UnityEngine;



public class RocketLauncher : MonoBehaviour
{
    [Header("Launcher")] 
    [SerializeField] private ParticleSystem particleSystem;
    [SerializeField] private SmokeSource smokeSource;
    [SerializeField] private float minLifespan = 0f;
    [SerializeField] private float maxLifespan = 1f;
    
    [Header("Rocket")]
    private SmokeSystem smokeSystem;
    [SerializeField] private GameObject rocketPrefab;
    
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
        StartCoroutine(OnSmoke(minLifespan, maxLifespan));
        Instantiate(rocketPrefab);
        
    }

    IEnumerator OnSmoke(float _minLifespan, float _maxLifespan)
    {
        smokeSource.MinLifespan = _minLifespan;
        smokeSource.MaxLifespan = _maxLifespan;
        yield return new WaitForSeconds(0.7f);
        smokeSource.MinLifespan = 0f;
        smokeSource.MaxLifespan = 0f;
    }
    
}
