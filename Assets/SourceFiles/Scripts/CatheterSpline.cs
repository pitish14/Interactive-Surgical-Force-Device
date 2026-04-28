using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CatheterSpline : MonoBehaviour
{
    public Transform baseAnchor;   // start
    public Transform tip;          // end

    [Header("Shape")]
    public int segments = 25;
    public float segmentLength = 0.01f;
    public int iterations = 8;

    [Header("Smooth rendering")]
    public int subdivisions = 6;

    LineRenderer lr;
    Vector3[] p;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
    }

    void Start()
    {
        if (baseAnchor == null) baseAnchor = transform;
        Init();
    }

    void Init()
    {
        p = new Vector3[segments + 1];

        Vector3 start = baseAnchor.position;
        Vector3 end = tip != null ? tip.position : start + baseAnchor.forward * segments * segmentLength;

        for (int i = 0; i < p.Length; i++)
        {
            float t = i / (float)(p.Length - 1);
            p[i] = Vector3.Lerp(start, end, t);
        }
    }

    void LateUpdate()
    {
        if (baseAnchor == null || tip == null) return;
        if (p == null || p.Length != segments + 1) Init();

        // Pin ends
        p[0] = baseAnchor.position;
        p[p.Length - 1] = tip.position;

        // Solve segment lengths
        for (int it = 0; it < iterations; it++)
        {
            p[0] = baseAnchor.position;
            p[p.Length - 1] = tip.position;

            // forward
            for (int i = 1; i < p.Length; i++)
                KeepDistance(i, i - 1);

            // backward
            for (int i = p.Length - 2; i >= 0; i--)
                KeepDistance(i, i + 1);
        }

        RenderCatmullRom();
    }

    void KeepDistance(int a, int b)
    {
        Vector3 dir = p[a] - p[b];
        float dist = dir.magnitude;
        if (dist < 1e-6f) return;

        Vector3 target = p[b] + dir / dist * segmentLength;

        // don't move endpoints
        if (a != 0 && a != p.Length - 1)
            p[a] = target;
    }

    void RenderCatmullRom()
    {
        List<Vector3> samples = new List<Vector3>(p.Length * subdivisions);

        for (int i = 0; i < p.Length - 1; i++)
        {
            Vector3 p0 = p[Mathf.Max(i - 1, 0)];
            Vector3 p1 = p[i];
            Vector3 p2 = p[i + 1];
            Vector3 p3 = p[Mathf.Min(i + 2, p.Length - 1)];

            int steps = (i == p.Length - 2) ? subdivisions + 1 : subdivisions;

            for (int s = 0; s < steps; s++)
            {
                float t = s / (float)subdivisions;
                samples.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }

        lr.positionCount = samples.Count;
        lr.SetPositions(samples.ToArray());
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }
}