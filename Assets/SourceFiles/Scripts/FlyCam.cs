using UnityEngine;

public class FlyCamera : MonoBehaviour
{
    public float speed = 10f;
    public float mouseSensitivity = 2f;

    float rotationX = 0f;
    float rotationY = 0f;

    void Update()
    {
        rotationX += Input.GetAxis("Mouse X") * mouseSensitivity;
        rotationY -= Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.rotation = Quaternion.Euler(rotationY, rotationX, 0);

        float x = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
        float z = Input.GetAxis("Vertical") * speed * Time.deltaTime;

        float y = 0;

        if (Input.GetKey(KeyCode.E)) y = speed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) y = -speed * Time.deltaTime;

        transform.Translate(x, y, z);
    }
}