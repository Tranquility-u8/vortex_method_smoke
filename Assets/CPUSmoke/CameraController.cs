using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VortexMethod
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float mouseSensitivity = 80.0f;
        [SerializeField] private float speed = 0.3f;
        public bool rotationLock;

        private float _xRotation = 0, _yRotation = 0;

        private void Awake()
        {
            rotationLock = false;
            //Cursor.lockState = CursorLockMode.Locked;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (!rotationLock)
            {
                RotateView();
            }

            Move();
        }

        void Move()
        {
            float x = Input.GetAxisRaw("Horizontal") * speed * Time.deltaTime;
            float y = Input.GetAxisRaw("Vertical") * speed * Time.deltaTime;
            transform.position += transform.TransformDirection(x * Vector3.right + y * Vector3.forward);

            if (Input.GetKey(KeyCode.E))
            {
                transform.position += Vector3.up * speed * Time.deltaTime;
            }
            else if (Input.GetKey(KeyCode.Q))
            {
                transform.position += Vector3.down * speed * Time.deltaTime;
            }
        }

        void RotateView()
        {
            float mouse_x = Input.GetAxisRaw("Mouse X") * Time.deltaTime * mouseSensitivity;
            float mouse_y = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * mouseSensitivity;

            _yRotation += mouse_x;
            _xRotation -= mouse_y;
            _xRotation = Math.Clamp(_xRotation, -90f, 90f);

            transform.rotation = Quaternion.Euler(_xRotation, _yRotation, 0);
        }
    }
}

