using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CatheterChain : MonoBehaviour
{
    [Header("Chain Settings")]
    public int segmentCount = 20;
    public float segmentLength = 0.05f;
    public float segmentRadius = 0.005f;

    [Header("Physics Settings")]
    public float jointSpring = 10f;
    public float jointDamper = 5f;
    public float angularLimit = 20f;        // Max bend angle per joint (degrees)
    public float segmentMass = 0.01f;
    public float linearDrag = 2f;
    public float angularDrag = 5f;

    [Header("Visuals")]
    public int lineRendererSmoothness = 3;  // Subdivisions between segments for smoothing
    public Material catheterMaterial;       // Assign in Inspector (or leave null for default)

    private GameObject[] segments;
    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        BuildChain();
        SetupLineRenderer();
    }

    void BuildChain()
    {
        segments = new GameObject[segmentCount];

        GameObject previousSegment = null;

        for (int i = 0; i < segmentCount; i++)
        {
            // --- Create segment GameObject ---
            GameObject seg = new GameObject($"Segment_{i}");
            seg.transform.parent = this.transform;

            // Position each segment below the previous one
            seg.transform.position = this.transform.position + Vector3.down * i * segmentLength;

            // --- Capsule Collider ---
            CapsuleCollider col = seg.AddComponent<CapsuleCollider>();
            col.radius = segmentRadius;
            col.height = segmentLength;
            col.direction = 1; // Y-axis aligned

            // --- Rigidbody ---
            Rigidbody rb = seg.AddComponent<Rigidbody>();
            rb.mass = segmentMass;
            rb.linearDamping = linearDrag;
            rb.angularDamping = angularDrag;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // --- HingeJoint (connect to previous segment) ---
            if (previousSegment != null)
            {
                ConfigurableJoint joint = seg.AddComponent<ConfigurableJoint>();
                joint.connectedBody = previousSegment.GetComponent<Rigidbody>();

                // Anchor at the top of this segment
                joint.anchor = new Vector3(0, segmentLength * 0.5f, 0);
                joint.connectedAnchor = new Vector3(0, -segmentLength * 0.5f, 0);

                // Lock linear motion — segments stay connected
                joint.xMotion = ConfigurableJointMotion.Locked;
                joint.yMotion = ConfigurableJointMotion.Locked;
                joint.zMotion = ConfigurableJointMotion.Locked;

                // Allow limited angular rotation on X and Z (bending)
                joint.angularXMotion = ConfigurableJointMotion.Limited;
                joint.angularYMotion = ConfigurableJointMotion.Limited;
                joint.angularZMotion = ConfigurableJointMotion.Limited;

                // Angular limits
                SoftJointLimit limit = new SoftJointLimit();
                limit.limit = angularLimit;
                joint.highAngularXLimit = limit;
                joint.lowAngularXLimit = new SoftJointLimit { limit = -angularLimit };
                joint.angularYLimit = limit;
                joint.angularZLimit = limit;

                // Spring drive to return to neutral (gives catheter its stiffness)
                JointDrive drive = new JointDrive();
                drive.positionSpring = jointSpring;
                drive.positionDamper = jointDamper;
                drive.maximumForce = Mathf.Infinity;

                joint.angularXDrive = drive;
                joint.angularYZDrive = drive;
            }
            else
            {
                // First segment — fix it in place (this is the "handle" end)
                rb.isKinematic = true;
            }

            segments[i] = seg;
            previousSegment = seg;
        }
    }

    void SetupLineRenderer()
    {
        int pointCount = segmentCount * lineRendererSmoothness;
        lineRenderer.positionCount = pointCount;
        lineRenderer.startWidth = segmentRadius * 2f;
        lineRenderer.endWidth = segmentRadius * 2f;
        lineRenderer.useWorldSpace = true;

        if (catheterMaterial != null)
            lineRenderer.material = catheterMaterial;
    }

    void Update()
    {
        UpdateLineRenderer();
    }

    void UpdateLineRenderer()
    {
        // Build a smooth curve through all segment positions using Catmull-Rom interpolation
        Vector3[] points = new Vector3[segmentCount];
        for (int i = 0; i < segmentCount; i++)
            points[i] = segments[i].transform.position;

        int totalPoints = segmentCount * lineRendererSmoothness;
        lineRenderer.positionCount = totalPoints;

        for (int i = 0; i < totalPoints; i++)
        {
            float t = (float)i / (totalPoints - 1) * (segmentCount - 1);
            int index = Mathf.FloorToInt(t);
            float frac = t - index;

            // Catmull-Rom spline
            Vector3 p0 = points[Mathf.Clamp(index - 1, 0, segmentCount - 1)];
            Vector3 p1 = points[Mathf.Clamp(index,     0, segmentCount - 1)];
            Vector3 p2 = points[Mathf.Clamp(index + 1, 0, segmentCount - 1)];
            Vector3 p3 = points[Mathf.Clamp(index + 2, 0, segmentCount - 1)];

            lineRenderer.SetPosition(i, CatmullRom(p0, p1, p2, p3, frac));
        }
    }

    Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
        );
    }

    /// <summary>
    /// Call this to move the tip (handle end) of the catheter — e.g. from another script.
    /// </summary>
    public void MoveTip(Vector3 worldPosition)
    {
        if (segments != null && segments.Length > 0)
            segments[0].transform.position = worldPosition;
    }

    /// <summary>
    /// Returns the position of the catheter's free end (insertion tip).
    /// </summary>
    public Vector3 GetTipPosition()
    {
        return segments[segmentCount - 1].transform.position;
    }
}
