using UnityEngine;

public class TipFollowCamera : MonoBehaviour
{
    public Transform target;          // Tip
    public Vector3 offset = new Vector3(0f, 0.05f, -0.12f);
    public float followSpeed = 10f;
    public float lookSpeed = 10f;

    void LateUpdate()
    {
        if (!target) return;

        // Move
        Vector3 desiredPos = target.TransformPoint(offset);
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);

        // Look
        Quaternion desiredRot = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, lookSpeed * Time.deltaTime);
    }
}