using System.Collections;
using System.Collections.Generic;
using GPUSmoke;
using UnityEngine;

public class SmokeExpand : MonoBehaviour
{
    public SmokeSource SmokeSource;
    public float lifeSpan = 10f;
    public float expandFactor = 0.5f;
    private float origRadius;
    // Start is called before the first frame update
    void Start()
    {
        origRadius = SmokeSource.TracerSpawnRadius;
    }

    // Update is called once per frame
    void Update()
    {
        float factor = Time.deltaTime*expandFactor;
        SmokeSource.TracerSpawnRadius *= 1+factor;
        SmokeSource.VortexSpawnRadius *= 1+factor;
        SmokeSource.MaxLifespan*= 1-factor;
        SmokeSource.MinLifespan*= 1-factor;
        if(origRadius/SmokeSource.TracerSpawnRadius < 0.7f)
            Destroy(gameObject);
    }
}
