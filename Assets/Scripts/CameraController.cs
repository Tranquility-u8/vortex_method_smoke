using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VortexMethod
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float mouseSensitivity = 80.0f;
        [SerializeField] private float speed = 0.3f;

        private float xRotation = 0f;
        private float yRotation = 0f;

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
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            yRotation += mouseX;
            yRotation = Mathf.Clamp(yRotation, -90f, 90f);


            transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }
    }
}

