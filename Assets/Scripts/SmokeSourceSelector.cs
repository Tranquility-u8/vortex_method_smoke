using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeSourceSelector : MonoBehaviour
{
    public Material HighlightMaterial;
    public Material SelectionMaterial;
    public LayerMask SceneLayerMask;
    public Camera Camera;

    private Transform _target;
    private Vector3 _targetScreenPoint;
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
        {
            _target.GetComponent<MeshRenderer>().sharedMaterial = material;

            if (material == SelectionMaterial)
            {
                _targetScreenPoint = Camera.WorldToScreenPoint(_target.transform.position);
            }
        }
    }

    private void FixTarget()
    {
        if (_target == null)
            return;
        _target.position = Camera.ScreenToWorldPoint(_targetScreenPoint);
        /* float radius = _target.GetComponent<SmokeSourceEntity>().SphereRadius;
        if (Physics.CheckSphere(_target.position, radius, SceneLayerMask)) {
            var ray = Camera.ScreenPointToRay(_targetScreenPoint);
            float distance = Vector3.Distance(_target.position, ray.origin);
            if (Physics.SphereCast(ray.origin, radius, ray.direction, out _raycastHit, distance, SceneLayerMask)) {
                distance = _raycastHit.distance;
                _target.position = ray.origin + distance * ray.direction;
                _targetScreenPoint = Camera.WorldToScreenPoint(_target.position);
            }
        } */
    }

    private Material GetTargetMaterial()
    {
        return _target?.GetComponent<MeshRenderer>().sharedMaterial;
    }

    void Update()
    {
        Ray ray = new(Camera.transform.position, Camera.transform.forward);

        bool drag = Input.GetMouseButton(0) && GetTargetMaterial() == SelectionMaterial;
        if (drag)
        {
            FixTarget();
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
                Input.GetKeyDown(KeyCode.C))
            {
                var copy = Instantiate(_raycastHit.transform.gameObject);
                SetTarget(copy.transform, SelectionMaterial);
            }

            if (Input.GetKeyDown(KeyCode.Delete))
            {
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
                if (Input.GetMouseButton(0))
                    SetTarget(_raycastHit.transform, SelectionMaterial);
                else
                    SetTarget(_raycastHit.transform, HighlightMaterial);
            }
            else
                SetTarget(null, null);
        }
    }
}
