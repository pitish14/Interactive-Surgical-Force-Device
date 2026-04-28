using UnityEngine;

public class TipProximity : MonoBehaviour
{
    public float probeDistance = 0.02f;
    public float radius = 0.005f;
    public LayerMask vesselMask = ~0;

    public bool nearWall;
    public float nearDistance;
    public Vector3 nearNormal;

    void Update()
    {
        nearWall = Physics.SphereCast(transform.position, radius, transform.forward,
                                      out RaycastHit hit, probeDistance, vesselMask,
                                      QueryTriggerInteraction.Ignore);

        if (nearWall)
        {
            nearDistance = hit.distance;
            nearNormal = hit.normal;
        }
    }
}