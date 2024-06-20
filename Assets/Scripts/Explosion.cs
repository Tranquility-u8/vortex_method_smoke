using System.Collections;
using System.Collections.Generic;
using GPUSmoke;
using Unity.VisualScripting;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    public GameObject explosionPrefab;
    public GameObject smokeSourcePrefab;
    public float explosionForce = 1000f;
    public float explosionRadius = 2f;
    public SmokeSystem SmokeSystem;
    public Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(transform.position.y < -10)
            Destroy(gameObject);
    }
    
    private void OnCollisionEnter(Collision other)
    {
        Explode(other);
        Destroy(this.gameObject);
    }
    
    
    void Explode(Collision other)
    {
        Debug.Log("rocket hit");
        //find collision point
        Vector3 explosionPos = other.contacts[0].point;
        Vector3 explosionNormal = other.contacts[0].normal;
        //instantiate explosion prefab using the collision point and normal
        GameObject explosion = Instantiate(explosionPrefab, explosionPos, Quaternion.LookRotation(explosionNormal));
        explosion.transform.localScale = new Vector3(explosionRadius, explosionRadius, explosionRadius);
        if(smokeSourcePrefab != null)
        {
            GameObject smokeSource = Instantiate(smokeSourcePrefab, explosionPos, Quaternion.LookRotation(explosionNormal));
            smokeSource.GetComponent<SmokeSource>().SmokeSystem = GetComponentInChildren<SmokeSource>().SmokeSystem;
        }
        
        
    }
}
