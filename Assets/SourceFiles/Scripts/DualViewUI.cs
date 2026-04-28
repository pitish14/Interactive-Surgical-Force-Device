using UnityEngine;
using UnityEngine.UI;

public class DualViewUI : MonoBehaviour
{
    [Header("C-Arm Control")]
    public float rotationStep = 15f;

    [Header("3D Camera Control")]
    public Transform tipTarget;      // Drag Tip here (or auto-find "Tip")
    public Camera mainCam;           // Assign your Camera (or auto use Camera.main)
    public TipOrbitCamera orbitCam;  // Assign TipOrbitCamera on your Camera (auto-find if null)

    private FluoroscopySetup fluoroSetup;
    private GameObject fluoroCanvas;
    private bool fluoroMode = true;

    void Start()
    {
        fluoroSetup = FindFirstObjectByType<FluoroscopySetup>();
        if (fluoroSetup == null)
        {
            Debug.LogError("[DualViewUI] No FluoroscopySetup found in scene!");
            return;
        }

        if (mainCam == null) mainCam = Camera.main;

        // Auto-find Tip if not assigned
        if (tipTarget == null)
        {
            GameObject tipObj = GameObject.Find("Tip");
            if (tipObj != null) tipTarget = tipObj.transform;
        }

        // Auto-find orbit camera if not assigned
        if (orbitCam == null && mainCam != null)
            orbitCam = mainCam.GetComponent<TipOrbitCamera>();

        // Make sure orbit camera has the target
        if (orbitCam != null && orbitCam.target == null && tipTarget != null)
            orbitCam.target = tipTarget;

        // Start in fluoro mode
        if (mainCam != null) mainCam.enabled = false;
        if (orbitCam != null) orbitCam.enabled = false;

        Invoke(nameof(BuildUI), 0.15f);
    }

    void BuildUI()
    {
        // ── Canvas ──────────────────────────────────────────────────────────
        GameObject canvasObj = new GameObject("FluoroscopyCanvas");
        fluoroCanvas = canvasObj;
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        // ── Background ──────────────────────────────────────────────────────
        GameObject bg = CreatePanel("Background", canvasObj.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bg.GetComponent<Image>().color = new Color(0.78f, 0.78f, 0.78f, 1f);

        // ── Header ──────────────────────────────────────────────────────────
        GameObject header = CreatePanel("Header", canvasObj.transform,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -40), new Vector2(0, 0));
        header.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
        AddLabel(header.transform, "FLUOROSCOPY — DUAL VIEW", 20,
            TextAnchor.MiddleCenter, Color.white);

        // ── AP panel ────────────────────────────────────────────────────────
        GameObject leftPanel = CreatePanel("AP_Panel", canvasObj.transform,
            new Vector2(0, 0), new Vector2(0.5f, 1),
            new Vector2(5, 45), new Vector2(-5, -45));
        leftPanel.GetComponent<Image>().color = new Color(0.85f, 0.85f, 0.85f, 1f);
        AddRawImage(leftPanel.transform, fluoroSetup.apRenderTexture);
        AddLabel(leftPanel.transform, "AP  (Anteroposterior)", 16,
            TextAnchor.UpperCenter, new Color(0.05f, 0.05f, 0.05f), new Vector2(0, -4));

        // ── Lateral panel ───────────────────────────────────────────────────
        GameObject rightPanel = CreatePanel("Lateral_Panel", canvasObj.transform,
            new Vector2(0.5f, 0), new Vector2(1, 1),
            new Vector2(5, 45), new Vector2(-5, -45));
        rightPanel.GetComponent<Image>().color = new Color(0.85f, 0.85f, 0.85f, 1f);
        AddRawImage(rightPanel.transform, fluoroSetup.lateralRenderTexture);
        AddLabel(rightPanel.transform, "LAT  (Lateral)", 16,
            TextAnchor.UpperCenter, new Color(0.05f, 0.05f, 0.05f), new Vector2(0, -4));

        // ── Control bar ─────────────────────────────────────────────────────
        BuildRotationControls(canvasObj.transform);

        // ── Mode toggle ─────────────────────────────────────────────────────
        GameObject modeBtn = CreateButton("Switch to 3D View", canvasObj.transform,
            new Vector2(0.76f, 0.945f), new Vector2(0.995f, 0.998f));
        modeBtn.name = "ModeToggleBtn";
        modeBtn.GetComponent<Image>().color = new Color(0.1f, 0.35f, 0.1f, 1f);
        modeBtn.GetComponentInChildren<Text>().color = Color.white;
        modeBtn.GetComponent<Button>().onClick.AddListener(ToggleDisplayMode);
    }

    void ToggleDisplayMode()
    {
        fluoroMode = !fluoroMode;

        // Show/hide fluoro UI
        foreach (string n in new[] { "Background", "AP_Panel", "Lateral_Panel", "Header", "ControlBar" })
        {
            Transform t = fluoroCanvas.transform.Find(n);
            if (t != null) t.gameObject.SetActive(fluoroMode);
        }

        // Update toggle button
        Transform modeBtn = fluoroCanvas.transform.Find("ModeToggleBtn");
        if (modeBtn != null)
        {
            Text btnText = modeBtn.GetComponentInChildren<Text>();
            if (btnText) btnText.text = fluoroMode ? "Switch to 3D View" : "Switch to Fluoro";
            modeBtn.GetComponent<Image>().color = fluoroMode
                ? new Color(0.1f, 0.35f, 0.1f, 1f)
                : new Color(0.35f, 0.1f, 0.1f, 1f);
        }

        if (fluoroMode)
        {
            // --- Switching TO fluoro ---
            fluoroSetup.ApplyFluoro();

            if (orbitCam != null) orbitCam.enabled = false;
            if (mainCam != null) mainCam.enabled = false;
        }
        else
        {
            // --- Switching TO 3D ---
            fluoroSetup.RestoreOriginalMaterials();

            if (mainCam != null) mainCam.enabled = true;

            // Ensure orbit cam is set up
            if (orbitCam == null && mainCam != null)
                orbitCam = mainCam.GetComponent<TipOrbitCamera>();

            if (orbitCam != null)
            {
                if (orbitCam.target == null)
                {
                    // Try to use tipTarget (or auto-find)
                    if (tipTarget == null)
                    {
                        GameObject tipObj = GameObject.Find("Tip");
                        if (tipObj != null) tipTarget = tipObj.transform;
                    }
                    orbitCam.target = tipTarget;
                }

                orbitCam.enabled = true;
                orbitCam.SnapToTarget();
            }
        }
    }

    void BuildRotationControls(Transform parent)
    {
        GameObject bar = CreatePanel("ControlBar", parent,
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 0), new Vector2(0, 40));
        bar.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);

        AddLabel(bar.transform, "C-ARM CONTROL", 13, TextAnchor.MiddleLeft,
            Color.white, new Vector2(10, 0));

        GameObject btnL = CreateButton("◀  Rotate C-Arm", bar.transform,
            new Vector2(0.28f, 0.1f), new Vector2(0.48f, 0.9f));
        btnL.GetComponent<Button>().onClick.AddListener(() => fluoroSetup.RotateCArm(-rotationStep));
        btnL.GetComponentInChildren<Text>().color = Color.white;

        GameObject btnR = CreateButton("Rotate C-Arm  ▶", bar.transform,
            new Vector2(0.52f, 0.1f), new Vector2(0.72f, 0.9f));
        btnR.GetComponent<Button>().onClick.AddListener(() => fluoroSetup.RotateCArm(rotationStep));
        btnR.GetComponentInChildren<Text>().color = Color.white;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    GameObject CreatePanel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        return obj;
    }

    void AddRawImage(Transform parent, RenderTexture rt)
    {
        GameObject obj = new GameObject("RenderView", typeof(RectTransform), typeof(RawImage));
        obj.transform.SetParent(parent, false);
        RectTransform r = obj.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(2, 2); r.offsetMax = new Vector2(-2, -26);
        obj.GetComponent<RawImage>().texture = rt;
    }

    void AddLabel(Transform parent, string text, int fontSize, TextAnchor anchor,
        Color color, Vector2? offset = null)
    {
        GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = offset ?? Vector2.zero; rt.offsetMax = Vector2.zero;
        Text t = obj.GetComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize; t.color = color; t.alignment = anchor;
    }

    GameObject CreateButton(string label, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject obj = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        obj.GetComponent<Image>().color = new Color(0.35f, 0.35f, 0.45f, 1f);

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(obj.transform, false);
        RectTransform trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        Text t = textObj.GetComponent<Text>();
        t.text = label;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 13; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
        return obj;
    }
}