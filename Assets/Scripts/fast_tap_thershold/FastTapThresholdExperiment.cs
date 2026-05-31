using System;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FastTapThresholdExperiment : MonoBehaviour
{
    public enum FastTapThresholdPreset
    {
        Ms500,
        Ms600,
        Ms700,
        Ms800,
        Ms900,
        Ms1000,
        Custom
    }

    private enum ExperimentState
    {
        PilotWelcome,
        FormalWelcome,
        WaitingForStartBall,
        TargetActive
    }

    private enum ExperimentPhase
    {
        Pilot,
        Formal
    }

    private enum TrialColor
    {
        Green,
        Yellow
    }

    [Header("Position")]
    public Vector3 startBallPosition = new Vector3(0f, 1f, 0.2f);
    public float startBallHeightMeters = 1f;
    public float targetDistanceMeters = 0.53f;
    public Vector3 targetForwardAxis = Vector3.forward;
    public bool useExplicitStartBallPosition = true;
    public float welcomePanelZPosition = 0.9f;

    [Header("Threshold")]
    public FastTapThresholdPreset thresholdPreset = FastTapThresholdPreset.Ms500;
    public int customThresholdMs = 500;

    [Header("Block")]
    public int pilotTrialsPerBlock = 6;
    public int formalTrialsPerBlock = 10;

    [Header("Instruction Text")]
    public string greenInstructionDescription = "需要快速离开球";
    public string yellowInstructionDescription = "需要停留一定时间";

    [Header("Audio")]
    public AudioClip successClip;
    public AudioClip failureClip;

    [Header("Target Materials")]
    public Material targetBlueMaterial;
    public Material targetGreenMaterial;
    public Material targetYellowMaterial;

    [Header("Optional References")]
    public Canvas welcomeCanvas;
    public TMP_Text welcomeText;
    public TMP_Text startButtonText;
    public GameObject startButtonObject;
    public TMP_Text greenLegendText;
    public TMP_Text yellowLegendText;
    public Renderer startBallRenderer;
    public Renderer targetBallRenderer;
    public FastTapThresholdLogger logger;

    private const float BallDiameterMeters = 0.06f;
    private const string PilotWelcomeMessage = "welcome pilot expirement";
    private const string FormalWelcomeMessage = "welcome to this expirement";

    private ExperimentState state;
    private ExperimentPhase phase;
    private AudioSource audioSource;
    private GameObject startBallObject;
    private SphereCollider startBallCollider;
    private GameObject targetBallObject;
    private Material startBallMaterial;
    private Color startBallColor = new Color(0.65f, 0.65f, 0.65f, 1f);
    private int currentTrialIndex;
    private int activeTrialsPerBlock;
    private readonly List<TrialColor> blockSequence = new List<TrialColor>();
    private TrialColor currentTrialColor;
    private bool currentTrialColorAssigned;
    private bool fingerInsideStartBall;
    private bool fingerInsideTarget;
    private float trialStartTime;
    private float targetEnterTime;
    private float timeInTargetSeconds;
    private Vector3 finalFingerPosition;
    private bool uiPointerPressed;

    private void Awake()
    {
        EnsureDependencies();
        BuildRuntimeObjectsIfNeeded();
        ConfigureSceneObjects();
        EnterPilotWelcome();
    }

    private void Update()
    {
        bool hasFinger = TryGetIndexTipPosition(out Vector3 fingerPosition);

        switch (state)
        {
            case ExperimentState.PilotWelcome:
            case ExperimentState.FormalWelcome:
                UpdateWelcomeInput(hasFinger, fingerPosition);
                break;
            case ExperimentState.WaitingForStartBall:
                UpdateStartBallInput(hasFinger, fingerPosition);
                break;
            case ExperimentState.TargetActive:
                UpdateTargetInput(hasFinger, fingerPosition);
                break;
        }
    }

    private void EnsureDependencies()
    {
        if (logger == null)
        {
            logger = GetComponent<FastTapThresholdLogger>();
            if (logger == null)
            {
                logger = gameObject.AddComponent<FastTapThresholdLogger>();
            }
        }

        logger.Initialize("pilot_fast_tap");

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;

        if (successClip == null)
        {
            successClip = Resources.Load<AudioClip>("Audio/success");
        }

        if (failureClip == null)
        {
            failureClip = Resources.Load<AudioClip>("Audio/error");
        }

        if (targetBlueMaterial == null)
        {
            targetBlueMaterial = Resources.Load<Material>("ball");
        }

        if (targetGreenMaterial == null)
        {
            targetGreenMaterial = Resources.Load<Material>("ballGreen");
        }

        if (targetYellowMaterial == null)
        {
            targetYellowMaterial = Resources.Load<Material>("S2TargetCurrent");
        }
    }

    private void BuildRuntimeObjectsIfNeeded()
    {
        if (welcomeCanvas == null)
        {
            BuildWelcomeCanvas();
        }

        if (startBallObject == null)
        {
            BuildStartBall();
        }

        if (targetBallObject == null)
        {
            BuildTargetBall();
        }
    }

    private void ConfigureSceneObjects()
    {
        if (startButtonText != null)
        {
            startButtonText.text = "start";
        }

        if (startBallRenderer == null && startBallObject != null)
        {
            startBallRenderer = startBallObject.GetComponent<Renderer>();
        }

        if (targetBallRenderer == null && targetBallObject != null)
        {
            targetBallRenderer = targetBallObject.GetComponent<Renderer>();
        }

        if (startBallRenderer != null)
        {
            startBallMaterial = startBallRenderer.material;
            startBallMaterial.color = startBallColor;
        }

        ApplyTargetIdleMaterial();
        PositionWelcomeCanvas();
        PositionBalls();
        startBallObject.SetActive(false);
        targetBallObject.SetActive(false);
    }

    private void PositionWelcomeCanvas()
    {
        if (welcomeCanvas == null)
        {
            return;
        }

        Vector3 currentPosition = welcomeCanvas.transform.position;
        currentPosition.z = welcomePanelZPosition;
        welcomeCanvas.transform.position = currentPosition;
    }

    private void PositionBalls()
    {
        Vector3 normalizedForward = targetForwardAxis.sqrMagnitude > 0.0001f
            ? targetForwardAxis.normalized
            : Vector3.forward;

        Vector3 startPosition = useExplicitStartBallPosition
            ? startBallPosition
            : new Vector3(0f, startBallHeightMeters, 0.45f);

        startBallObject.transform.position = startPosition;
        targetBallObject.transform.position = startPosition + normalizedForward * targetDistanceMeters;
    }

    private void BuildWelcomeCanvas()
    {
        GameObject canvasObject = new GameObject("FastTapThresholdCanvas");
        canvasObject.transform.SetParent(transform, false);

        welcomeCanvas = canvasObject.AddComponent<Canvas>();
        welcomeCanvas.renderMode = RenderMode.WorldSpace;
        welcomeCanvas.worldCamera = Camera.main;
        canvasObject.AddComponent<GraphicRaycaster>();
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 30f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(560f, 340f);
        canvasObject.transform.position = new Vector3(0f, 1.2f, 0.9f);
        canvasObject.transform.rotation = Quaternion.identity;
        canvasObject.transform.localScale = Vector3.one * 0.0015f;

        GameObject panelObject = new GameObject("WelcomePanel");
        panelObject.transform.SetParent(canvasObject.transform, false);
        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.55f, 0.55f, 0.55f, 0.95f);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(520f, 320f);
        panelRect.anchoredPosition = Vector2.zero;

        GameObject textObject = new GameObject("WelcomeText");
        textObject.transform.SetParent(panelObject.transform, false);
        welcomeText = textObject.AddComponent<TextMeshProUGUI>();
        welcomeText.alignment = TextAlignmentOptions.Center;
        welcomeText.fontSize = 24f;
        welcomeText.color = Color.black;
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.6f);
        textRect.anchorMax = new Vector2(0.9f, 0.88f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        startButtonObject = new GameObject("StartButton");
        startButtonObject.transform.SetParent(panelObject.transform, false);
        Image buttonImage = startButtonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.88f, 0.88f, 0.88f, 1f);
        Button button = startButtonObject.AddComponent<Button>();
        button.onClick.AddListener(HandleStartButtonPressed);
        EventTrigger trigger = startButtonObject.AddComponent<EventTrigger>();
        AddPointerEvent(trigger, EventTriggerType.PointerDown, () => uiPointerPressed = true);
        AddPointerEvent(trigger, EventTriggerType.PointerUp, () => uiPointerPressed = false);
        AddPointerEvent(trigger, EventTriggerType.PointerExit, () => uiPointerPressed = false);
        RectTransform buttonRect = startButtonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.33f, 0.1f);
        buttonRect.anchorMax = new Vector2(0.67f, 0.26f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        GameObject buttonTextObject = new GameObject("StartButtonText");
        buttonTextObject.transform.SetParent(startButtonObject.transform, false);
        startButtonText = buttonTextObject.AddComponent<TextMeshProUGUI>();
        startButtonText.alignment = TextAlignmentOptions.Center;
        startButtonText.fontSize = 30f;
        startButtonText.color = Color.black;
        startButtonText.text = "start";
        RectTransform buttonTextRect = buttonTextObject.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        greenLegendText = BuildLegendText(
            panelObject.transform,
            "GreenLegendText",
            BuildLegendContent("19E64C", "trigger", greenInstructionDescription),
            new Vector2(0.08f, 0.32f),
            new Vector2(0.46f, 0.56f));

        yellowLegendText = BuildLegendText(
            panelObject.transform,
            "YellowLegendText",
            BuildLegendContent("FFD91A", "cancel", yellowInstructionDescription),
            new Vector2(0.54f, 0.32f),
            new Vector2(0.92f, 0.56f));
    }

    private void BuildStartBall()
    {
        startBallObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        startBallObject.name = "FastTapStartBall";
        startBallObject.transform.SetParent(transform, false);
        startBallObject.transform.localScale = Vector3.one * BallDiameterMeters;
        startBallCollider = startBallObject.GetComponent<SphereCollider>();
        startBallRenderer = startBallObject.GetComponent<Renderer>();
    }

    private void BuildTargetBall()
    {
        targetBallObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        targetBallObject.name = "FastTapTargetBall";
        targetBallObject.transform.SetParent(transform, false);
        targetBallObject.transform.localScale = Vector3.one * BallDiameterMeters;
        targetBallRenderer = targetBallObject.GetComponent<Renderer>();
    }

    private void UpdateWelcomeInput(bool hasFinger, Vector3 fingerPosition)
    {
        if (welcomeCanvas == null || !welcomeCanvas.gameObject.activeSelf || !hasFinger || startButtonObject == null)
        {
            return;
        }

        RectTransform buttonRect = startButtonObject.GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4];
        buttonRect.GetWorldCorners(corners);

        Vector3 min = corners[0];
        Vector3 max = corners[2];
        float planeDistance = Mathf.Abs(Vector3.Dot(
            fingerPosition - buttonRect.position,
            buttonRect.forward.normalized));

        bool insideButton =
            fingerPosition.x >= min.x &&
            fingerPosition.x <= max.x &&
            fingerPosition.y >= min.y &&
            fingerPosition.y <= max.y &&
            planeDistance <= 0.03f;

        if (insideButton && !uiPointerPressed)
        {
            uiPointerPressed = true;
            HandleStartButtonPressed();
        }
        else if (!insideButton)
        {
            uiPointerPressed = false;
        }
    }

    private void UpdateStartBallInput(bool hasFinger, Vector3 fingerPosition)
    {
        if (!hasFinger || startBallCollider == null)
        {
            fingerInsideStartBall = false;
            return;
        }

        bool inside = IsInsideSphere(startBallObject.transform.position, BallDiameterMeters * 0.5f, fingerPosition);
        if (inside && !fingerInsideStartBall)
        {
            fingerInsideStartBall = true;
            BeginTrial();
        }
        else if (!inside)
        {
            fingerInsideStartBall = false;
        }
    }

    private void UpdateTargetInput(bool hasFinger, Vector3 fingerPosition)
    {
        if (!hasFinger)
        {
            if (fingerInsideTarget)
            {
                CompleteTrial(true, finalFingerPosition);
            }
            return;
        }

        finalFingerPosition = fingerPosition;
        logger.WriteTrajectorySample(GetCurrentPhaseName(), currentTrialIndex, (Time.time - trialStartTime) * 1000f, fingerPosition);

        bool inside = IsInsideSphere(targetBallObject.transform.position, BallDiameterMeters * 0.5f, fingerPosition);
        float thresholdSeconds = GetCancelThresholdMs() / 1000f;

        if (inside && !fingerInsideTarget)
        {
            fingerInsideTarget = true;
            targetEnterTime = Time.time;
            timeInTargetSeconds = 0f;
            AssignTrialColorIfNeeded();
            ApplyTargetTrialMaterial();
        }

        if (inside)
        {
            timeInTargetSeconds = Mathf.Max(0f, Time.time - targetEnterTime);
            if (timeInTargetSeconds >= thresholdSeconds)
            {
                CompleteTrial(false, fingerPosition);
                return;
            }
        }

        if (!inside && fingerInsideTarget)
        {
            fingerInsideTarget = false;
            CompleteTrial(true, fingerPosition);
        }
    }

    private void HandleStartButtonPressed()
    {
        if (state == ExperimentState.PilotWelcome)
        {
            phase = ExperimentPhase.Pilot;
            activeTrialsPerBlock = pilotTrialsPerBlock;
        }
        else
        {
            phase = ExperimentPhase.Formal;
            activeTrialsPerBlock = formalTrialsPerBlock;
        }

        PrepareNewBlock();
        ShowStartBallState();
    }

    private void PrepareNewBlock()
    {
        currentTrialIndex = 0;
        blockSequence.Clear();

        int greenCount = activeTrialsPerBlock / 2;
        int yellowCount = activeTrialsPerBlock / 2;

        for (int i = 0; i < greenCount; i++)
        {
            blockSequence.Add(TrialColor.Green);
        }

        for (int i = 0; i < yellowCount; i++)
        {
            blockSequence.Add(TrialColor.Yellow);
        }

        System.Random random = new System.Random();
        for (int i = blockSequence.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            TrialColor temp = blockSequence[i];
            blockSequence[i] = blockSequence[swapIndex];
            blockSequence[swapIndex] = temp;
        }
    }

    private void BeginTrial()
    {
        currentTrialIndex++;
        currentTrialColorAssigned = false;
        fingerInsideTarget = false;
        timeInTargetSeconds = 0f;
        targetEnterTime = 0f;
        trialStartTime = Time.time;
        finalFingerPosition = Vector3.zero;

        startBallObject.SetActive(false);
        targetBallObject.SetActive(true);
        ApplyTargetIdleMaterial();
        state = ExperimentState.TargetActive;
    }

    private void CompleteTrial(bool triggered, Vector3 finalPosition)
    {
        bool success = EvaluateSuccess(triggered);
        PlayFeedback(success);

        FastTapTrialResult result = new FastTapTrialResult
        {
            Phase = GetCurrentPhaseName(),
            TrialIndex = currentTrialIndex,
            TargetColor = currentTrialColor == TrialColor.Green ? "green" : "yellow",
            CancelThresholdMs = GetCancelThresholdMs(),
            UsedTimeMs = (Time.time - trialStartTime) * 1000f,
            Success = success,
            TimeInTargetMs = timeInTargetSeconds * 1000f,
            FinalIndexPosition = finalPosition
        };

        logger.WriteTrial(result);

        if (currentTrialIndex >= activeTrialsPerBlock)
        {
            CompleteBlock();
            return;
        }

        ShowStartBallState();
    }

    private void CompleteBlock()
    {
        if (phase == ExperimentPhase.Pilot)
        {
            EnterFormalWelcome();
        }
        else
        {
            EnterPilotWelcome();
        }
    }

    private void ShowStartBallState()
    {
        state = ExperimentState.WaitingForStartBall;
        fingerInsideStartBall = false;
        fingerInsideTarget = false;
        currentTrialColorAssigned = false;
        startBallObject.SetActive(true);
        targetBallObject.SetActive(false);
        ApplyTargetIdleMaterial();
        if (welcomeCanvas != null)
        {
            welcomeCanvas.gameObject.SetActive(false);
        }
    }

    private void EnterPilotWelcome()
    {
        phase = ExperimentPhase.Pilot;
        state = ExperimentState.PilotWelcome;
        ResetSharedState();
        ShowWelcome(PilotWelcomeMessage);
    }

    private void EnterFormalWelcome()
    {
        phase = ExperimentPhase.Formal;
        state = ExperimentState.FormalWelcome;
        ResetSharedState();
        ShowWelcome(FormalWelcomeMessage);
    }

    private void ShowWelcome(string message)
    {
        if (welcomeCanvas != null)
        {
            welcomeCanvas.gameObject.SetActive(true);
        }

        if (welcomeText != null)
        {
            welcomeText.text = message;
        }

        UpdateLegendText();

        if (startBallObject != null)
        {
            startBallObject.SetActive(false);
        }

        if (targetBallObject != null)
        {
            targetBallObject.SetActive(false);
        }

        ApplyTargetIdleMaterial();
    }

    private void ResetSharedState()
    {
        fingerInsideStartBall = false;
        fingerInsideTarget = false;
        currentTrialColorAssigned = false;
        uiPointerPressed = false;
        currentTrialIndex = 0;
        ApplyTargetIdleMaterial();
    }

    private void AssignTrialColorIfNeeded()
    {
        if (currentTrialColorAssigned)
        {
            return;
        }

        int index = Mathf.Clamp(currentTrialIndex - 1, 0, blockSequence.Count - 1);
        currentTrialColor = blockSequence[index];
        currentTrialColorAssigned = true;
    }

    private bool EvaluateSuccess(bool triggered)
    {
        if (currentTrialColor == TrialColor.Green)
        {
            return triggered;
        }

        return !triggered;
    }

    private string GetCurrentPhaseName()
    {
        return phase == ExperimentPhase.Pilot ? "pilot" : "formal";
    }

    private void PlayFeedback(bool success)
    {
        AudioClip clipToPlay = success ? successClip : failureClip;
        if (clipToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
        else
        {
            Debug.LogWarning(success
                ? "FastTapThresholdExperiment: success clip is missing."
                : "FastTapThresholdExperiment: failure clip is missing.");
        }
    }

    private int GetCancelThresholdMs()
    {
        switch (thresholdPreset)
        {
            case FastTapThresholdPreset.Ms500:
                return 500;
            case FastTapThresholdPreset.Ms600:
                return 600;
            case FastTapThresholdPreset.Ms700:
                return 700;
            case FastTapThresholdPreset.Ms800:
                return 800;
            case FastTapThresholdPreset.Ms900:
                return 900;
            case FastTapThresholdPreset.Ms1000:
                return 1000;
            default:
                return Mathf.Max(1, customThresholdMs);
        }
    }

    private void ApplyTargetIdleMaterial()
    {
        ApplyTargetBallMaterial(targetBlueMaterial);
    }

    private void ApplyTargetTrialMaterial()
    {
        ApplyTargetBallMaterial(currentTrialColor == TrialColor.Green ? targetGreenMaterial : targetYellowMaterial);
    }

    private void ApplyTargetBallMaterial(Material material)
    {
        if (targetBallRenderer == null || material == null)
        {
            return;
        }

        targetBallRenderer.sharedMaterial = material;
    }

    private void UpdateLegendText()
    {
        if (greenLegendText != null)
        {
            greenLegendText.text = BuildLegendContent("19E64C", "trigger", greenInstructionDescription);
        }

        if (yellowLegendText != null)
        {
            yellowLegendText.text = BuildLegendContent("FFD91A", "cancel", yellowInstructionDescription);
        }
    }

    private bool TryGetIndexTipPosition(out Vector3 position)
    {
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Any, out MixedRealityPose pose))
        {
            position = pose.Position;
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    private static bool IsInsideSphere(Vector3 center, float radius, Vector3 point)
    {
        return Vector3.Distance(center, point) <= radius;
    }

    private static void AddPointerEvent(EventTrigger trigger, EventTriggerType type, Action action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = type
        };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }

    private static TMP_Text BuildLegendText(Transform parent, string objectName, string content, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject legendObject = new GameObject(objectName);
        legendObject.transform.SetParent(parent, false);

        TMP_Text text = legendObject.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 16f;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.color = Color.black;
        text.text = content;

        RectTransform rect = legendObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return text;
    }

    private static string BuildLegendContent(string colorHex, string keyword, string description)
    {
        return "<color=#" + colorHex + ">•</color>  " + keyword + "\n" + description;
    }
}
