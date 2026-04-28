using UnityEngine;
using System.Collections.Generic;

public class FluoroscopySetup : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Drag your cardiovascular model root GameObject here")]
    public GameObject cardiovascularModel;

    [Header("Camera Settings")]
    public float cameraDistance = 2.0f;
    public int renderTextureWidth  = 512;
    public int renderTextureHeight = 768;

    [Header("Fluoroscopy Look")]
    public Color backgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    public Material fluoroscopyMaterial;

    [HideInInspector] public RenderTexture apRenderTexture;
    [HideInInspector] public RenderTexture lateralRenderTexture;

    private Camera apCamera;
    private Camera lateralCamera;

    // Store original materials per renderer so we can restore them
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

    void Start()
    {
        if (cardiovascularModel == null)
        {
            Debug.LogError("[FluoroscopySetup] Please assign the cardiovascular model in the Inspector!");
            return;
        }

        // Save original materials BEFORE overwriting them
        SaveOriginalMaterials();

        if (fluoroscopyMaterial != null)
            ApplyFluoroscopyMaterial();

        Bounds bounds = GetModelBounds();
        Vector3 center = bounds.center;
        float size = bounds.extents.magnitude;
        float dist = size * cameraDistance;

        apRenderTexture      = new RenderTexture(renderTextureWidth, renderTextureHeight, 24);
        lateralRenderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 24);

        apCamera = CreateFluoroscopyCamera("AP_Camera",
            center + new Vector3(0, 0, -dist),
            Quaternion.LookRotation(Vector3.forward),
            apRenderTexture, bounds);

        lateralCamera = CreateFluoroscopyCamera("Lateral_Camera",
            center + new Vector3(-dist, 0, 0),
            Quaternion.LookRotation(Vector3.right),
            lateralRenderTexture, bounds);
    }

    Camera CreateFluoroscopyCamera(string camName, Vector3 position,
        Quaternion rotation, RenderTexture rt, Bounds bounds)
    {
        GameObject camObj = new GameObject(camName);
        camObj.transform.position = position;
        camObj.transform.rotation = rotation;
        camObj.transform.parent   = this.transform;

        Camera cam = camObj.AddComponent<Camera>();
        cam.targetTexture    = rt;
        cam.backgroundColor  = backgroundColor;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.fieldOfView      = 40f;
        cam.orthographic     = true;
        cam.orthographicSize = bounds.extents.y * 1.2f;
        cam.depth            = -10;

        return cam;
    }

    void SaveOriginalMaterials()
    {
        Renderer[] renderers = cardiovascularModel.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            // Copy the array so we have a clean snapshot
            Material[] copy = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = r.sharedMaterials[i];
            originalMaterials[r] = copy;
        }
        Debug.Log($"[FluoroscopySetup] Saved original materials for {renderers.Length} renderers.");
    }

    void ApplyFluoroscopyMaterial()
    {
        Renderer[] renderers = cardiovascularModel.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            Material[] mats = new Material[r.materials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = fluoroscopyMaterial;
            r.materials = mats;
        }
    }

    /// <summary>Called by DualViewUI when switching to 3D mode.</summary>
    public void RestoreOriginalMaterials()
    {
        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null)
                kvp.Key.materials = kvp.Value;
        }
        Debug.Log("[FluoroscopySetup] Restored original materials.");
    }

    /// <summary>Called by DualViewUI when switching back to fluoro mode.</summary>
    public void ApplyFluoro()
    {
        if (fluoroscopyMaterial != null)
            ApplyFluoroscopyMaterial();
    }

    public Bounds GetModelBounds()
    {
        Renderer[] renderers = cardiovascularModel.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(cardiovascularModel.transform.position, Vector3.one);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);
        return bounds;
    }

    public void RotateCArm(float degrees)
    {
        Bounds bounds = GetModelBounds();
        apCamera.transform.RotateAround(bounds.center, Vector3.up, degrees);
        apCamera.transform.LookAt(bounds.center);
    }
}
