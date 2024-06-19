using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SmokeSourceEntity : MonoBehaviour
{
    public Material Material;
    public Mesh SphereMesh;
    public float SphereRadius;

    public T GetOrAddComponent<T>() where T : Component
    {
        T c = this.GetComponent<T>();
        if (c == null)
            c = this.AddComponent<T>();
        return c;
    }

    // Start is called before the first frame update
    void Start()
    {
        this.tag = "Selectable";
        this.transform.localScale = new Vector3(SphereRadius * 2, SphereRadius * 2, SphereRadius * 2);

        var mesh_filter = GetOrAddComponent<MeshFilter>();
        mesh_filter.sharedMesh = SphereMesh;

        var mesh_renderer = GetOrAddComponent<MeshRenderer>();
        mesh_renderer.sharedMaterial = Material;
        // mesh_renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var collider = GetOrAddComponent<SphereCollider>();
        collider.radius = 0.5f;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
