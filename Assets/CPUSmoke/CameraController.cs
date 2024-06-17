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
        
        private Vector3 cameraPosition;  
        
        private void Awake()
        {
            rotationLock = false;
            //Cursor.lockState = CursorLockMode.Locked;
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
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            cameraPosition.x -= mouseY * mouseSensitivity;
            cameraPosition.y += mouseX * mouseSensitivity;

            transform.rotation = Quaternion.Euler(cameraPosition.x, cameraPosition.y, 0);
        }
    }
}

