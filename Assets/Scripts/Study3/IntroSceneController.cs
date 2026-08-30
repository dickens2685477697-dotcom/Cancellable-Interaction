using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class IntroSceneController : MonoBehaviour
{
    private struct TechniqueDefinition
    {
        public BallSpawner.Mode mode;
        public string label;

        public TechniqueDefinition(BallSpawner.Mode mode, string label)
        {
            this.mode = mode;
            this.label = label;
        }
    }

    private sealed class SlotRuntime
    {
        public int index;
        public int colorIndex;
        public Renderer indicatorRenderer;
        public GameObject techniqueBall;
    }

    private static readonly TechniqueDefinition[] Techniques =
    {
        new TechniqueDefinition(BallSpawner.Mode.pinchReleaseTrigger, "1.\nPinch-Release-Trigger"),
        new TechniqueDefinition(BallSpawner.Mode.pinchDwell, "2.\nPinch-Dwell"),
        new TechniqueDefinition(BallSpawner.Mode.pinchDirection, "3.\nPinch-Direction"),
        new TechniqueDefinition(BallSpawner.Mode.pinchThumbSwipe, "4.\nPinch-Thumb-Swipe"),
        new TechniqueDefinition(BallSpawner.Mode.dwell, "5.\nDwell"),
        new TechniqueDefinition(BallSpawner.Mode.dwellRelease, "6.\nDwell-Release"),
        new TechniqueDefinition(BallSpawner.Mode.dwellDirection, "7.\nDwell-Direction"),
        new TechniqueDefinition(BallSpawner.Mode.speed, "8.\nSpeed"),
        new TechniqueDefinition(BallSpawner.Mode.speedDwell, "9.\nSpeed-Dwell"),
        new TechniqueDefinition(BallSpawner.Mode.speedRelease, "10.\nSpeed-Release"),
        new TechniqueDefinition(BallSpawner.Mode.speedDirection, "11.\nSpeed-Direction"),
        new TechniqueDefinition(BallSpawner.Mode.release010, "12.\n010-Release"),
        new TechniqueDefinition(BallSpawner.Mode.time010, "13.\n010-Time"),
        new TechniqueDefinition(BallSpawner.Mode.direction010, "14.\n010-direction"),
        new TechniqueDefinition(BallSpawner.Mode.contact010, "15.\n010-Contact")
    };

    [Header("Initial-view placement")]
    [Min(0.1f)] public float forwardDistance = 0.60f;
    [Min(0.01f)] public float ballDiameter = 0.04f;
    [Min(0.01f)] public float horizontalSpacing = 0.09f;
    [Min(0.01f)] public float rowSeparation = 0.12f;
    [Min(0.01f)] public float labelOffset = 0.085f;
    [Min(0.001f)] public float labelWorldScale = 0.008f;
    [Min(0.1f)] public float labelFontSize = 9f;
    [Min(1)] public int labelStaggerRows = 3;
    [Min(0.01f)] public float labelStaggerSpacing = 0.10f;

    [Header("Study3 height alignment")]
    [Tooltip("Align the blue technique-ball row to the Study3 Fitts-ring center (the Preview Spawner world Y).")]
    public bool alignTechniqueRowToStudy3CenterHeight = true;
    [Tooltip("Optional world-space height offset relative to the Study3 Fitts-ring center.")]
    public float study3CenterHeightOffset = 0f;

    [Header("Optional overrides")]
    public BallSpawner previewSpawner;
    public GameObject targetPrefab;
    public Material grayMaterial;
    public Material greenMaterial;
    public Material yellowMaterial;
    public Material redMaterial;
    public Material blueMaterial;

    private readonly List<SlotRuntime> slots = new List<SlotRuntime>();
    private readonly Dictionary<S2AccMode, SlotRuntime> slotsByMode = new Dictionary<S2AccMode, SlotRuntime>();
    private Transform rowsRoot;
    private Material generatedGrayMaterial;

    private void Awake()
    {
        ResolveSpawnerAndAssets();
        ConfigurePreviewSpawner();
        DisableExperimentOnlyObjects();
    }

    private IEnumerator Start()
    {
        yield return null;

        Transform viewer = ResolveViewer();
        if (viewer == null)
        {
            Debug.LogError("IntroSceneController: no Main Camera or CenterEyeAnchor was found.");
            yield break;
        }

        CreateRowsRoot(viewer);
        BuildTechniqueRows();
    }

    private void ResolveSpawnerAndAssets()
    {
        if (previewSpawner == null)
        {
            GameObject spawnerObject = GameObject.Find("Spawner");
            if (spawnerObject != null)
            {
                previewSpawner = spawnerObject.GetComponent<BallSpawner>();
            }
        }

        if (previewSpawner == null)
        {
            return;
        }

        if (targetPrefab == null) targetPrefab = previewSpawner.TargetPrefab;
        if (blueMaterial == null) blueMaterial = previewSpawner.ordinaryBall;
        if (greenMaterial == null) greenMaterial = previewSpawner.greenBall;
        if (yellowMaterial == null) yellowMaterial = previewSpawner.yellowBall;
        if (redMaterial == null) redMaterial = previewSpawner.redBall;

        if (grayMaterial == null && blueMaterial != null)
        {
            generatedGrayMaterial = new Material(blueMaterial)
            {
                name = "Intro Indicator Gray (Runtime)"
            };
            SetMaterialColor(generatedGrayMaterial, new Color(0.42f, 0.42f, 0.42f, 1f));
            grayMaterial = generatedGrayMaterial;
        }
    }

    private void ConfigurePreviewSpawner()
    {
        if (previewSpawner == null)
        {
            Debug.LogError("IntroSceneController: the scene needs a GameObject named 'Spawner' with BallSpawner.");
            return;
        }

        previewSpawner.executionMode = BallSpawner.ExecutionMode.IntroPreview;
        previewSpawner.ApplySelectionSettings();
        previewSpawner.enable_trigger = true;
        previewSpawner.enableMineBall = false;
        previewSpawner.enabledOb = false;
        previewSpawner.enable_preview = false;
        previewSpawner.triggered = false;
        previewSpawner.isReact = false;
        previewSpawner.lastBall = null;
        previewSpawner.nextNextBall = null;

        foreach (AudioSource audioSource in previewSpawner.GetComponents<AudioSource>())
        {
            audioSource.playOnAwake = false;
            audioSource.Stop();
        }

        foreach (MonoBehaviour component in previewSpawner.GetComponents<MonoBehaviour>())
        {
            if (component == null || component == previewSpawner)
            {
                continue;
            }

            string typeName = component.GetType().Name;
            if (typeName.StartsWith("LogWriter") || typeName == "DataCapture")
            {
                component.enabled = false;
            }
        }
    }

    private void DisableExperimentOnlyObjects()
    {
        DisableNamedObject("Got");
        DisableNamedObject("GoNext");
        DisableNamedObject("temp");
    }

    private static void DisableNamedObject(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        if (target != null)
        {
            target.SetActive(false);
        }
    }

    private static Transform ResolveViewer()
    {
        if (Camera.main != null)
        {
            return Camera.main.transform;
        }

        GameObject centerEyeAnchor = GameObject.Find("CenterEyeAnchor");
        return centerEyeAnchor != null ? centerEyeAnchor.transform : null;
    }

    private void CreateRowsRoot(Transform viewer)
    {
        GameObject root = new GameObject("Intro Ball Rows");
        rowsRoot = root.transform;
        rowsRoot.SetParent(transform, false);

        // Keep both rows horizontal in world space while preserving the viewer's
        // initial yaw. This makes every blue-ball center share the Study3 center Y.
        Vector3 horizontalForward = Vector3.ProjectOnPlane(viewer.forward, Vector3.up);
        rowsRoot.rotation = horizontalForward.sqrMagnitude > 0.000001f
            ? Quaternion.LookRotation(horizontalForward.normalized, Vector3.up)
            : Quaternion.identity;

        Vector3 rootPosition = viewer.position + viewer.forward.normalized * forwardDistance;
        if (alignTechniqueRowToStudy3CenterHeight && previewSpawner != null)
        {
            // The interaction row uses local Y = -rowSeparation / 2.
            Vector3 techniqueRowWorldOffset = rowsRoot.rotation
                * new Vector3(0f, -rowSeparation * 0.5f, 0f);
            rootPosition.y = previewSpawner.transform.position.y
                + study3CenterHeightOffset
                - techniqueRowWorldOffset.y;
        }

        rowsRoot.position = rootPosition;
    }

    private void BuildTechniqueRows()
    {
        if (previewSpawner == null || targetPrefab == null)
        {
            Debug.LogError("IntroSceneController: Preview Spawner or S2Trigger prefab is not assigned.");
            return;
        }

        slots.Clear();
        slotsByMode.Clear();

        float centerIndex = (Techniques.Length - 1) * 0.5f;
        for (int i = 0; i < Techniques.Length; i++)
        {
            float x = (i - centerIndex) * horizontalSpacing;
            SlotRuntime slot = new SlotRuntime
            {
                index = i,
                colorIndex = 0,
                indicatorRenderer = CreateIndicator(i, new Vector3(x, rowSeparation * 0.5f, 0f))
            };

            int labelTier = i % Mathf.Max(1, labelStaggerRows);
            float labelY = -rowSeparation * 0.5f
                - labelOffset
                - labelTier * labelStaggerSpacing;
            CreateLabel(i, Techniques[i].label, new Vector3(x, labelY, 0f));
            slots.Add(slot);
            SpawnTechniqueBall(slot, new Vector3(x, -rowSeparation * 0.5f, 0f));
        }
    }

    private Renderer CreateIndicator(int index, Vector3 localPosition)
    {
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.name = $"Indicator_{index + 1:00}";
        indicator.transform.SetParent(rowsRoot, false);
        indicator.transform.localPosition = localPosition;
        indicator.transform.localRotation = Quaternion.identity;
        indicator.transform.localScale = Vector3.one * ballDiameter;

        Collider indicatorCollider = indicator.GetComponent<Collider>();
        if (indicatorCollider != null)
        {
            Destroy(indicatorCollider);
        }

        Renderer indicatorRenderer = indicator.GetComponent<Renderer>();
        indicatorRenderer.sharedMaterial = GetCycleMaterial(0);
        return indicatorRenderer;
    }

    private void CreateLabel(int index, string labelText, Vector3 localPosition)
    {
        GameObject labelObject = new GameObject($"Label_{index + 1:00}");
        labelObject.transform.SetParent(rowsRoot, false);
        labelObject.transform.localPosition = localPosition;
        labelObject.transform.localRotation = Quaternion.identity;
        labelObject.transform.localScale = Vector3.one * labelWorldScale;

        TextMeshPro text = labelObject.AddComponent<TextMeshPro>();
        text.text = labelText;
        text.fontSize = labelFontSize;
        text.alignment = TextAlignmentOptions.Top;
        text.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        float labelWidth = horizontalSpacing * Mathf.Max(1, labelStaggerRows) * 0.9f / labelWorldScale;
        float labelHeight = labelStaggerSpacing * 0.9f / labelWorldScale;
        text.rectTransform.sizeDelta = new Vector2(labelWidth, labelHeight);
    }

    private void SpawnTechniqueBall(SlotRuntime slot, Vector3 localPosition)
    {
        TechniqueDefinition definition = Techniques[slot.index];
        GameObject ball = Instantiate(targetPrefab, rowsRoot);
        ball.name = $"Technique_{slot.index + 1:00}_{definition.mode}";
        ball.transform.localPosition = localPosition;
        ball.transform.localRotation = Quaternion.identity;
        ball.transform.localScale = Vector3.one * ballDiameter;

        MeshRenderer meshRenderer = ball.GetComponent<MeshRenderer>();
        if (meshRenderer != null && blueMaterial != null)
        {
            meshRenderer.sharedMaterial = blueMaterial;
        }

        S2AccMode selectionMode = ball.GetComponent<S2AccMode>();
        if (selectionMode == null)
        {
            Debug.LogError($"{ball.name}: target prefab does not contain S2AccMode.");
            Destroy(ball);
            return;
        }

        selectionMode.isMineBall = false;
        selectionMode.ballKind = 1;
        selectionMode.ConfigureStandalone(definition.mode, previewSpawner, slot.index);
        selectionMode.StandaloneSelectionSucceeded += HandleStandaloneSelectionSucceeded;

        slot.techniqueBall = ball;
        slotsByMode[selectionMode] = slot;
    }

    private void HandleStandaloneSelectionSucceeded(S2AccMode source)
    {
        if (source == null || !slotsByMode.TryGetValue(source, out SlotRuntime slot))
        {
            return;
        }

        slot.colorIndex = (slot.colorIndex + 1) % 4;
        slot.indicatorRenderer.sharedMaterial = GetCycleMaterial(slot.colorIndex);
        source.StandaloneSelectionSucceeded -= HandleStandaloneSelectionSucceeded;
        slotsByMode.Remove(source);
        StartCoroutine(RespawnTechniqueBall(slot, source.gameObject));
    }

    private IEnumerator RespawnTechniqueBall(SlotRuntime slot, GameObject oldBall)
    {
        Vector3 localPosition = oldBall != null
            ? oldBall.transform.localPosition
            : new Vector3((slot.index - (Techniques.Length - 1) * 0.5f) * horizontalSpacing, -rowSeparation * 0.5f, 0f);

        if (oldBall != null)
        {
            Destroy(oldBall);
        }

        yield return null;
        SpawnTechniqueBall(slot, localPosition);
    }

    private Material GetCycleMaterial(int colorIndex)
    {
        switch (colorIndex)
        {
            case 1: return greenMaterial != null ? greenMaterial : grayMaterial;
            case 2: return yellowMaterial != null ? yellowMaterial : grayMaterial;
            case 3: return redMaterial != null ? redMaterial : grayMaterial;
            default: return grayMaterial;
        }
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color);
        if (material.HasProperty("Color_619519269ab54f859f9ee6a4572399b"))
        {
            material.SetColor("Color_619519269ab54f859f9ee6a4572399b", color);
        }
    }

    private void OnDestroy()
    {
        if (generatedGrayMaterial != null)
        {
            Destroy(generatedGrayMaterial);
        }
    }

    private void OnValidate()
    {
        forwardDistance = Mathf.Max(0.1f, forwardDistance);
        ballDiameter = Mathf.Max(0.01f, ballDiameter);
        horizontalSpacing = Mathf.Max(ballDiameter, horizontalSpacing);
        rowSeparation = Mathf.Max(ballDiameter, rowSeparation);
        labelOffset = Mathf.Max(0.01f, labelOffset);
        labelWorldScale = Mathf.Max(0.001f, labelWorldScale);
        labelFontSize = Mathf.Max(0.1f, labelFontSize);
        labelStaggerRows = Mathf.Max(1, labelStaggerRows);
        labelStaggerSpacing = Mathf.Max(0.01f, labelStaggerSpacing);
    }
}
