using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VortexMethod
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float mouseSensitivity = 80.0f;
        [SerializeField] private float speed = 0.3f;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        void Update()
        {
            RotateView();
            Move();
        }

        void Move()
        {
            float x = Input.GetAxisRaw("Horizontal") * speed;
            float y = Input.GetAxisRaw("Vertical") * speed;
            transform.position += transform.TransformDirection(x * Vector3.right + y * Vector3.forward);

            if (Input.GetKey(KeyCode.E))
            {
                transform.position += Vector3.up * speed;
            }
            else if (Input.GetKey(KeyCode.Q))
            {
                transform.position += Vector3.down * speed;
            }
        }

        void RotateView()
        {
            float _mouse_x = Input.GetAxis("Mouse X");
            float _mouse_y = Input.GetAxis("Mouse Y");

            Quaternion xQuaternion = Quaternion.AngleAxis(-_mouse_y * mouseSensitivity, Vector3.right);
            Quaternion yQuaternion = Quaternion.AngleAxis(_mouse_x * mouseSensitivity, Vector3.up);

            Quaternion targetRotation = xQuaternion * transform.localRotation * yQuaternion;

            Vector3 euler = targetRotation.eulerAngles;
            euler.z = 0;
            targetRotation.eulerAngles = euler;

            transform.localRotation = targetRotation;
        }
    }
}

