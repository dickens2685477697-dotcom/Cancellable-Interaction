using System.Collections;
using System;
using System.Globalization;
using TMPro;
using UnityEngine;

public class ExperimentCounter : MonoBehaviour
{
    public int count = 0;
    public int MaxCount = 20;
    [Min(0.01f)] public float HoldDuration = 3f;
    [Min(0f)] public float EndCaptureDelay = 1f;
    public int TaskNum = 1;

    public float coolDown = 1f;

    public AcceStimulate acce;
    public Collider SphereCollider;

    public TextMeshProUGUI DisplayNum;
    public TextMeshProUGUI TaskPrompt;
    public TextMeshProUGUI GesturePrompt;
    StartBallBehavior2 startBallBehavior;
    private string CurrentTask_trigger = "Use your own dwell time to trigger the ball";
    private string CurrentTask_DoNotTrigger = "Do not trigger";

    private string NormalGesturePrompt = "  ";
    private string BackGesturePrompt = "\"Withdraw\"";
    private string CrossingGesturePrompt = "Move Away your hand";
    public Material shallowBall;
    public Material oriBall;
    private bool isCoolingDown = false; // Add a flag to check if the coroutine is running
    public event Action<string, int, double> TrialEvent;
    private bool study1Protocol, trialInside, holdComplete, protocolFinished;
    private double entryTime;

    // Start is called before the first frame update
    void Start()
    {
        study1Protocol = gameObject.scene.path == "Assets/Scenes/Study1.unity";
        if (study1Protocol)
        {
            MaxCount = Mathf.Max(1, MaxCount);
            HoldDuration = Mathf.Max(0.01f, HoldDuration);
            SphereCollider = GetComponent<Collider>();
            acce = GetComponent<AcceStimulate>();
            startBallBehavior = GetComponentInChildren<StartBallBehavior2>(true);
            acce.InEvent.AddListener(OnStudy1Inside);
            acce.AfterOutEvent.AddListener(OnStudy1Exit);
            acce.MotionTrackingInterrupted += OnTrackingInterrupted;
            DisplayNum.text = MaxCount.ToString();
            TaskPrompt.text = "Touch green";
            GesturePrompt.text = "";
            return;
        }
        DisplayNum.text = 10.ToString();
        SphereCollider = gameObject.GetComponent<Collider>();
        acce = GetComponent<AcceStimulate>();
        acce.InEvent.AddListener(TurnShallow);
        acce.OutEvent.AddListener(UpdateCount);
        acce.OutEvent.AddListener(TurnShallowBack);
        startBallBehavior = GetComponentInChildren<StartBallBehavior2>();
        acce.MotionTrackingInterrupted += OnTrackingInterrupted;
        TaskNum = 0;
        SwitchTask();
    }

    // Update is called once per frame
    void Update()
    {
        if (study1Protocol) return;
        if (count == MaxCount && !isCoolingDown) // Check if the coroutine is not running
        {
            count = 0;
            StartCoroutine(CoolDown());
            SphereCollider.enabled = true;
        }
    }

    private void EmitTrial(string kind, double timestamp)
    {
        TrialEvent?.Invoke(kind, count + 1, timestamp);
    }

    private void OnStudy1Inside()
    {
        if (protocolFinished || startBallBehavior == null || !startBallBehavior.GetState()) return;
        double now = acce.MotionSampleTimestamp;
        if (!trialInside)
        {
            trialInside = true;
            holdComplete = false;
            entryTime = now;
            TurnShallow();
            TaskPrompt.text = "Hold: " + (Math.Ceiling(HoldDuration * 100d) / 100d).ToString("F2", CultureInfo.InvariantCulture) + " s";
            GesturePrompt.text = "";
            EmitTrial("enter", now);
        }
        if (!holdComplete)
        {
            double remaining = Math.Max(0d, HoldDuration - (now - entryTime));
            if (remaining > 0d)
                TaskPrompt.text = "Hold: " + (Math.Ceiling(remaining * 100d) / 100d).ToString("F2", CultureInfo.InvariantCulture) + " s";
            else
            {
                holdComplete = true;
                TaskPrompt.text = "0.00s Withdraw";
                GesturePrompt.text = "";
                EmitTrial("hold_complete", now);
            }
        }
    }

    private void OnStudy1Exit()
    {
        if (!trialInside || protocolFinished) return;
        double now = acce.MotionSampleTimestamp;
        bool completed = holdComplete;
        EmitTrial(completed ? "success" : "early_exit", now);
        trialInside = holdComplete = false;
        if (completed) count++;
        DisplayNum.text = Mathf.Max(0, MaxCount - count).ToString();
        GetComponent<MeshRenderer>().material = oriBall;
        if (count >= MaxCount)
        {
            protocolFinished = true;
            TaskPrompt.text = "Thanks";
            GesturePrompt.text = "";
            if (startBallBehavior != null) startBallBehavior.gameObject.SetActive(false);
            StartCoroutine(FinishAfterCapture());
            return;
        }
        TaskPrompt.text = completed ? "Touch green" : "Too early";
        GesturePrompt.text = "";
        startBallBehavior.TureUnstarted();
    }

    private IEnumerator FinishAfterCapture()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, EndCaptureDelay));
        SphereCollider.enabled = false;
        acce.enabled = false;
    }

    public void UpdateCount()
    {
        if (startBallBehavior.GetState())
        {
            count += 1;
            DisplayNum.text = (10 - count).ToString();
        }
            
    }

    public void LockTriggerOnCrossing()
    {
        if(TaskNum == 3)
        {
            acce.OutEvent.AddListener(()=>
            {
            });
        }
    }

    public void SwitchTask()
    {
        TaskNum += 1;

        switch (TaskNum)
        {
            case 1:
                TaskPrompt.color = Color.red;
                TaskPrompt.text = CurrentTask_trigger;
                GesturePrompt.color = Color.red;
                //GesturePrompt.text = "\"Sweep\"";
                break;
            //case 2:
            //    TaskPrompt.color = Color.red;
            //    TaskPrompt.text = CurrentTask_DoNotTrigger;
            //    GesturePrompt.color = Color.red;
            //    GesturePrompt.text = BackGesturePrompt;
            //    DisplayNum.text = 10.ToString();
            //    break;
            case 2:
                acce.enabled = false;
                GameObject.Find("Task").SetActive(false);
                TaskPrompt.color = Color.green;
                TaskPrompt.text = "Thanks";
                GesturePrompt.text = "";
                break;
        }
    }

    private IEnumerator CoolDown()
    {
        isCoolingDown = true; // Set the flag to true when the coroutine starts

        if (TaskNum <= 3)
        {
            SwitchTask();
        }
        else
        {
            TaskNum = 1;
        }
        SphereCollider.enabled = false;

        yield return new WaitForSeconds(coolDown);
        DisplayNum.text = (10-count).ToString();

        isCoolingDown = false; // Set the flag to false when the coroutine finishes
    }
    void TurnShallow()
    {
        gameObject.GetComponent<MeshRenderer>().material = shallowBall;
    }
    void TurnShallowBack()
    {
        gameObject.GetComponent<MeshRenderer>().material = oriBall;
        startBallBehavior.TureUnstarted();
    }

    private void OnTrackingInterrupted()
    {
        if (!acce.UsesMrtkJointUpdates) return;
        if (study1Protocol)
        {
            if (trialInside) EmitTrial("tracking_interrupted", Time.realtimeSinceStartupAsDouble);
            trialInside = holdComplete = false;
            if (!protocolFinished) TaskPrompt.text = "Tracking lost";
        }
        GetComponent<MeshRenderer>().material = oriBall;
        if (!protocolFinished && startBallBehavior != null) startBallBehavior.TureUnstarted();
    }

    /// <summary>Abort an unfinished trial when the tracking origin changes.</summary>
    public void OnReferenceSpaceReset()
    {
        if (!study1Protocol) return;
        if (protocolFinished)
        {
            // Deactivating this object stops the tail coroutine; finish capture explicitly.
            StopAllCoroutines();
            if (SphereCollider != null) SphereCollider.enabled = false;
            if (acce != null) acce.enabled = false;
            if (startBallBehavior != null) startBallBehavior.gameObject.SetActive(false);
            return;
        }
        if (trialInside) EmitTrial("recenter_interrupted", Time.realtimeSinceStartupAsDouble);
        trialInside = holdComplete = false;
        acce.ResetForReferenceSpaceChange();
        TaskPrompt.text = "Touch green";
        GesturePrompt.text = "";
        GetComponent<MeshRenderer>().material = oriBall;
        if (startBallBehavior != null) startBallBehavior.TureUnstarted();
    }

    private void OnDestroy()
    {
        if (acce != null) acce.MotionTrackingInterrupted -= OnTrackingInterrupted;
    }
}
