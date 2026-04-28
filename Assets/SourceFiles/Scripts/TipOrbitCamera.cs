using UnityEngine;

[DisallowMultipleComponent]
public class TipOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                     // Assign Tip here
    public Vector3 targetOffset = Vector3.zero;  // Optional: offset from tip center

    [Header("Distance")]
    public float distance = 0.25f;
    public float minDistance = 0.05f;
    public float maxDistance = 2.0f;
    public float zoomSpeed = 0.25f;

    [Header("Rotation")]
    public float yaw = 0f;
    public float pitch = 20f;
    public float minPitch = -60f;
    public float maxPitch = 80f;
    public float rotateSpeed = 180f;

    [Header("Input")]
    public int rotateMouseButton = 1;            // 1 = Right Mouse Button

    [Header("Smoothing")]
    public float positionLerp = 15f;
    public float rotationLerp = 15f;

    void LateUpdate()
    {
        if (!target) return;

        // Rotate while holding RMB
        if (Input.GetMouseButton(rotateMouseButton))
        {
            yaw += Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * rotateSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // Zoom with scroll wheel
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
        }

        Vector3 focus = target.position + targetOffset;

        Quaternion desiredRot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPos = focus - desiredRot * Vector3.forward * distance;

        transform.position = Vector3.Lerp(transform.position, desiredPos, positionLerp * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotationLerp * Time.deltaTime);
    }

    public void SnapToTarget()
    {
        if (!target) return;

        Vector3 focus = target.position + targetOffset;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = focus - rot * Vector3.forward * distance;
        transform.rotation = rot;
    }
}