using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeSourceSelector : MonoBehaviour
{
    public Material HighlightMaterial;
    public Material SelectionMaterial;
    public Camera Camera;

    private Transform _target;
    private float _targetDist;
    private RaycastHit _raycastHit;

    private void Start()
    {

    }

    private void SetTarget(Transform target, Material material)
    {
        if (target != _target && _target != null)
        {
            var smoke_entity = _target.GetComponent<SmokeSourceEntity>();
            _target.GetComponent<MeshRenderer>().sharedMaterial = smoke_entity.Material;
        }
        _target = target;
        if (_target != null)
            _target.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private Material GetTargetMaterial()
    {
        return _target?.GetComponent<MeshRenderer>().sharedMaterial;
    }

    private bool GetAnyMouseButton()
    {
        return Input.GetMouseButton(0) || Input.GetMouseButton(1);
    }

    void Update()
    {
        Ray ray = new(Camera.transform.position, Camera.transform.forward);

        bool drag = GetAnyMouseButton() && GetTargetMaterial() == SelectionMaterial;
        if (drag)
        {
            _target.position = ray.origin + ray.direction * _targetDist;

            if (Input.GetMouseButtonDown(1))
            {
                var copy = Instantiate(_raycastHit.transform.gameObject);
                SetTarget(copy.transform, SelectionMaterial);
            }
            
            if (Input.GetKeyDown(KeyCode.Delete)) {
                var obj = _target.gameObject;
                SetTarget(null, null);
                obj.SetActive(false);
                Destroy(obj, 1.0f);
            }
        }

        if (!drag)
        {
            if (Physics.Raycast(ray, out _raycastHit) && _raycastHit.transform.CompareTag("Selectable"))
            {
                if (GetAnyMouseButton()) {
                    SetTarget(_raycastHit.transform, SelectionMaterial);
                    _targetDist = Vector3.Distance(ray.origin, _raycastHit.transform.position);
                }
                else
                    SetTarget(_raycastHit.transform, HighlightMaterial);
            }
            else
                SetTarget(null, null);
        }
    }
}
