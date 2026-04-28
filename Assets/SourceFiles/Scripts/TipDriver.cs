using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class TipDriver : MonoBehaviour
{
    [Header("Entry / Direction")]
    public Transform entryPoint;      
    public Transform forwardAnchor;   

    [Header("Input")]
    public KeyCode insertKey = KeyCode.Space;
    public KeyCode rollLeftKey = KeyCode.A;
    public KeyCode rollRightKey = KeyCode.D;

    [Header("Motion")]
    public float insertSpeed = 0.02f;
    public float rollSpeed = 120f;
    public float turnOnHitSpeed = 10f;

    [Header("Collision")]
    public LayerMask vesselMask = ~0;
    public float skin = 0.0005f;

    [Header("Debug")]
    public bool logInput = false;

    Rigidbody rb;
    SphereCollider sc;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sc = GetComponent<SphereCollider>();

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void Start()
    {
        if (entryPoint != null)
            rb.position = entryPoint.position;
        if (entryPoint != null)
            rb.rotation = entryPoint.rotation;
    }

    void FixedUpdate()
    {
        // Read input ONCE (physics step)
        bool inserting = Input.GetKey(insertKey);
        bool rollL = Input.GetKey(rollLeftKey);
        bool rollR = Input.GetKey(rollRightKey);

        if (logInput && (inserting || rollL || rollR))
            Debug.Log($"[TipDriver] inserting={inserting} rollL={rollL} rollR={rollR}");

        // If no inputs, do nothing. (Hard stop)
        if (!inserting && !rollL && !rollR)
            return;

        Vector3 fwd = (forwardAnchor != null) ? forwardAnchor.forward : transform.forward;

        // Roll
        float rollInput = (rollL ? 1f : 0f) + (rollR ? -1f : 0f);
        if (Mathf.Abs(rollInput) > 0.001f)
        {
            Quaternion roll = Quaternion.AngleAxis(rollInput * rollSpeed * Time.fixedDeltaTime, fwd);
            rb.MoveRotation(rb.rotation * roll);
        }

        // Insert
        if (inserting)
        {
            float dt = Time.fixedDeltaTime;
            float radius = GetWorldRadius();
            Vector3 step = fwd * (insertSpeed * dt);

            MoveWithSphereCast(step, radius);
            ResolvePenetration();
        }
    }

    float GetWorldRadius()
    {
        float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        return sc.radius * maxScale;
    }

    void MoveWithSphereCast(Vector3 step, float radius)
    {
        if (step.sqrMagnitude < 1e-12f) return;

        Vector3 dir = step.normalized;
        float dist = step.magnitude;

        if (Physics.SphereCast(rb.position, radius, dir, out RaycastHit hit, dist + skin, vesselMask, QueryTriggerInteraction.Ignore))
        {
            float safe = Mathf.Max(0f, hit.distance - skin);
            rb.MovePosition(rb.position + dir * safe);

            Vector3 slideDir = Vector3.ProjectOnPlane(dir, hit.normal).normalized;
            if (slideDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(slideDir, transform.up);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, turnOnHitSpeed * Time.fixedDeltaTime));
            }
        }
        else
        {
            rb.MovePosition(rb.position + step);
        }
    }

    void ResolvePenetration()
    {
        float radius = GetWorldRadius();
        Collider[] overlaps = Physics.OverlapSphere(rb.position, radius, vesselMask, QueryTriggerInteraction.Ignore);

        foreach (var c in overlaps)
        {
            if (c == null) continue;

            if (Physics.ComputePenetration(
                    sc, rb.position, rb.rotation,
                    c, c.transform.position, c.transform.rotation,
                    out Vector3 pushDir, out float pushDist))
            {
                rb.MovePosition(rb.position + pushDir * (pushDist + skin));
            }
        }
    }
}