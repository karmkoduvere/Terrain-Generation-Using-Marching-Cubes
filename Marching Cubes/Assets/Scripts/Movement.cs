using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public float moveSpeed = 10.0f;      // Camera movement speed.
    public float rotationSpeed = 2.0f;  // Camera rotation speed.
    
    private float rotationX = 0.0f;
    // Left rigidbody stuff here incase we want to try having collision with generated mesh
    //private Rigidbody _rb;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        //_rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        if (!Input.GetKey(KeyCode.Tab)) {
        // Camera Movement
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        float moveUpDown = Input.GetAxis("Jump"); // Jump key for moving up, Ctrl key for moving down
        //bool gPressed = Input.GetKeyDown(KeyCode.G);

        Vector3 moveDirection = new Vector3(moveHorizontal, moveUpDown, moveVertical);
        moveDirection.Normalize();

        //if (gPressed) switchGravity();

        transform.Translate(moveDirection * moveSpeed * Time.deltaTime);

        // Camera Rotation
        float rotateHorizontal = Input.GetAxis("Mouse X");
        float rotateVertical = -Input.GetAxis("Mouse Y"); 
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y + rotateHorizontal * rotationSpeed, 0);

        rotationX = Mathf.Clamp(rotationX + rotateVertical * rotationSpeed, -90.0f, 90.0f); // Limit vertical rotation to avoid camera flipping
        Camera.main.transform.rotation = Quaternion.Euler(rotationX, transform.rotation.eulerAngles.y + rotateHorizontal * rotationSpeed, 0);
        }
    }
    /*
    private void switchGravity()
    {
        _rb.useGravity = !_rb.useGravity;
        _rb.isKinematic = !_rb.isKinematic;
        if (_rb.useGravity) moveSpeed = moveSpeed * 0.1f; 
        else moveSpeed = moveSpeed * 10f;
    }*/
}
