using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public float moveSpeed = 10.0f;      // Camera movement speed.
    public float rotationSpeed = 2.0f;  // Camera rotation speed.
    private float rotationX = 0.0f;
    void Update()
    {
        // Camera Movement
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        float moveUpDown = Input.GetAxis("Jump"); // Jump key for moving up, Ctrl key for moving down

        Vector3 moveDirection = new Vector3(moveHorizontal, moveUpDown, moveVertical);
        moveDirection.Normalize();

        transform.Translate(moveDirection * moveSpeed * Time.deltaTime);

        // Camera Rotation
        float rotateHorizontal = Input.GetAxis("Mouse X");
        float rotateVertical = -Input.GetAxis("Mouse Y"); // Invert vertical input if necessary

        rotationX += rotateVertical * rotationSpeed;
        rotationX = Mathf.Clamp(rotationX, -90.0f, 90.0f); // Limit vertical rotation to avoid camera flipping

        transform.rotation = Quaternion.Euler(rotationX, transform.rotation.eulerAngles.y + rotateHorizontal * rotationSpeed, 0);
    }
}
