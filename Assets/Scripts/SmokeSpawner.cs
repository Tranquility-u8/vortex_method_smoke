using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using VortexMethod;

public class SmokeSpawner : MonoBehaviour
{
    public GameObject emitter;
    public Camera camera;
    public KeyCode lockKey = KeyCode.Z;
    public bool rotationLock;

    public RaycastHit hit;
    public Vector3 hitPos;
    public bool isHit;

    public Vector2 mousePos;

    public CameraController CameraController;
    // Start is called before the first frame update
    void Start()
    {
        rotationLock = false;
        CameraController = transform.GameObject().GetComponent<CameraController>();
        camera = transform.GameObject().GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        ProcessLock();
        SpawnSmoke();
    }

    private void ProcessLock()
    {
        if (Input.GetKeyDown(lockKey))
        {
            rotationLock = !rotationLock;
            CameraController.rotationLock = rotationLock;
            
        }
    }

    private void SpawnSmoke()
    {
        mousePos = Input.mousePosition;
        Ray ray = camera.ScreenPointToRay(mousePos);

        isHit = Physics.Raycast(ray, out hit);
        hitPos = hit.point;

        if (isHit && Input.GetMouseButtonDown(1))
        {
            Instantiate(emitter, hitPos, Quaternion.identity);
        }


    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Ray ray = camera.ScreenPointToRay(mousePos);
        Gizmos.DrawRay(ray.origin, ray.direction * 100);
        if(isHit)Gizmos.DrawSphere(hit.point, 0.5f);
        
        
    }
}
