using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using static UnityEngine.UI.CanvasScaler;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Security.AccessControl;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;
using System.Diagnostics;
using UnityEditor;
using UnityEngine.Rendering;

/// <summary>
/// Interaction control component for the six interaction methods.
/// Parameters for MenuSystem Demo:
///     WaitTime = 0.5f;
///     HesTime = 0.1f;
///     Vb = 0.2f
///     Ab = -2;
///     Vupper = 1f;
/// </summary>
/// <param name="Delay">Bool for Time Delay mode, set manually</param>
/// <param name="Fast">Bool for Fast tapping mode, set manually</param>
/// <param name="Acce">Bool for Acceleration mode using ABC, set manually</param>
/// <param name="AcceOnly">Bool for Acceleration mode using only B, set manually</param>
/// <param name="AcceOld">Bool for old-version Acceleration mode, set manually</param>
/// <param name="Velo">Bool for Velocity mode using AC, set manually</param>
/// <param name="InEvent"> Event triggered when the finger enters</param>
/// <param name="BeforeHesEvent">Event before the emission of HesEvent</param>
/// <param name="HesEvent">Event emitted when judged as hesitated</param>
/// <param name="OutEvent">Event emitted for normal execution</param>
/// <param name="AfterOutEvent">Event emitted after the finger is out</param>
/// <param name="UpdateT">Time after entering the option bubble</param>
/// <param name="WaitTime">Time waited for time delay mode (Long Tap and Short Tap)</param>
/// <param name="PrepTime">Time after entering 'Prepared' status</param>
/// <param name="HesTime">Time waited for Acce and Velo mode's hesitation confirmation</param>
/// <param name="vpre">Velocity for previous frame</param>
/// <param name="v">Velocity for current frame</param>
/// <param name="vc">Velocity recorded when recording the condition</param>
/// <param name="apre">Acceleration for previous frame</param>
/// <param name="a">Acceleration for current frame</param>
/// <param name="ac">Acceleration recorded when recording the condition</param>
/// <param name="Vb">Velocity bound for Acce and Velo mode's A condition</param>
/// <param name="Vbc">Velocity bound for Acce and Velo mode's C condition</param>
/// <param name="Ab">Acceleration bound for Acce mode's B condition</param>
/// <param name="Abc">Acceleration bound for Acce mode's C condition</param>
/// <param name="Vupper">Velocity bound for Acce mode's B condition</param>
/// <param name="Hesitated">Bool set true if judged as hesitated</param>
/// <param name="Init">Bool set true if component start its first calculation</param>
/// <param name="Prepared">Bool set true if judged as prepared</param>
/// <param name="Entered">Bool set true if finger enters the bubble (first 0 of 010)</param>
/// <param name="CloseSti">Bool set true if decides to close the Bubble's OutEvent function</param>
/// <param name="Invoked">Bool set true if the Bubble's OutEvent is functioned</param>
/// <param name="bigger">Bool set true if previous velocity is bigger than Vb, used in line-crossing version</param>
/// <param name="smaller">Bool set true if previous velocity is smaller than Vb, used in line-crossing version</param>
/// <param name="curBigger">Bool set true if current velocity is bigger than Vb, used in line-crossing version</param>
/// <param name="ThumbTipPose1">Pose for previous frame's ThumbTip pose</param>
/// <param name="ThumbTipPose2">Pose for current frame's ThumbTip pose</param>
/// <param name="ThumbDistalPose">Pose for current Distal joint index finger pose</param>
/// <param name="ThumbMiddlePose">Pose for current Middle joint index finger pose</param>
/// <param name="Bounds">Object's Bounds, to manipulate the collider</param>
/// <param name="boundCenter">Object bounds' center point</param>
/// <param name="Debugger">Dynamic linking text debugger shower: Row 1 = velocity, Row 2 = Acceleration, ABC = trigger condition</param>
/// <param name="ShowDebug">Bool set true if want to show debugger</param>
/// <param name="condition">Trigger condition in the IxDL: A B or C</param>
/// <param name="OutMate">Material when the finger is out of the button</param>
/// <param name="InMate">Material when the finger is in the button</param>
public class AcceStudy3_2 : MonoBehaviour
{
    public UnityEvent<bool> InEvent_Bool;
    public UnityEvent InEvent, BeforeHesEvent, HesEvent, OutEvent, AfterOutEvent;
    public float UpdateT, WaitTime, PrepTime, HesTime, vpre, v, vc, apre, a, ac, Vb, Vbc, Ab, Vupper;
    public bool HighLighted, Hesitated, Init, Delay, Fast, FastForKeyboard, Acce, AcceOnly, AcceOld, Velo, Prepared, Entered, CloseSti, Invoked, bigger, smaller, curBigger;
    public MixedRealityPose ThumbTipPose1;
    public MixedRealityPose ThumbTipPose2;
    public MixedRealityPose ThumbDistalPose;
    public MixedRealityPose ThumbMiddlePose;
    Bounds Bounds;
    public Vector3 boundCenter;
    public TMP_Text[] Debugger;
    public bool showDebug;
    public char condition;
    public Material OutMate, InMate, HLMate;
    public MeshRenderer meshRenderer;
    public float Distance;

    public bool AttributionPoseValid { get; private set; }
    private bool hasMotionBaseline;
    private uint motionSourceId;
    private long motionTrackingEpoch;
    private long motionSequence;
    private long lastProcessedSequence;
    private double motionTimestamp;
    private float motionSampleDeltaTime;
    private bool initialized;
    // Set Condition C's bound to Acceleration instead of Velocity.
    public bool useAcce = false;
    public float AccelerationBoundForConditionC;
    public float triggerDis = 0.015f;

    private void OnEnable()
    {
        ResetMotionBaseline();
        Study3HandPoseSource.EnsureAvailable();
        Study3HandPoseSource.JointSampleUpdated += OnJointSampleUpdated;
        Study3HandPoseSource.JointSampleProcessed += OnJointSampleProcessed;
        Study3HandPoseSource.SelectedSourceLost += OnSelectedSourceLost;
    }

    private void OnDisable()
    {
        Study3HandPoseSource.JointSampleUpdated -= OnJointSampleUpdated;
        Study3HandPoseSource.JointSampleProcessed -= OnJointSampleProcessed;
        Study3HandPoseSource.SelectedSourceLost -= OnSelectedSourceLost;
        ResetMotionBaseline();
    }

    private void ResetMotionBaseline()
    {
        hasMotionBaseline = false;
        lastProcessedSequence = 0;
        motionSampleDeltaTime = 0f;
        AttributionPoseValid = false;
        Init = true;
        v = vpre = a = apre = float.MaxValue;
        Distance = 0f;
        // Do not complete a contact from an invalid or replaced tracking source.
        Entered = Prepared = Hesitated = CloseSti = false;
        UpdateT = PrepTime = 0f;
    }

    private void OnSelectedSourceLost(uint sourceId)
    {
        if (hasMotionBaseline && sourceId == motionSourceId)
            ResetMotionBaseline();
    }

    private void OnJointSampleProcessed(Study3JointSample sample)
    {
        if (initialized && hasMotionBaseline && sample.SourceId == motionSourceId
            && sample.TrackingEpoch == motionTrackingEpoch && sample.Sequence == motionSequence
            && sample.Sequence != lastProcessedSequence)
        {
            lastProcessedSequence = sample.Sequence;
            ProcessSample(motionSampleDeltaTime);
        }
    }

    private void OnJointSampleUpdated(Study3JointSample sample)
    {
        if (!initialized) return;
        if (!sample.TryGetJointPose(TrackedHandJoint.ThumbTip, out MixedRealityPose tip)
            || double.IsNaN(sample.Timestamp) || double.IsInfinity(sample.Timestamp)
            || float.IsNaN(tip.Position.x) || float.IsInfinity(tip.Position.x)
            || float.IsNaN(tip.Position.y) || float.IsInfinity(tip.Position.y)
            || float.IsNaN(tip.Position.z) || float.IsInfinity(tip.Position.z))
        {
            ResetMotionBaseline();
            return;
        }

        if (hasMotionBaseline && sample.SourceId == motionSourceId
            && sample.TrackingEpoch == motionTrackingEpoch && sample.Sequence <= motionSequence)
            return;

        double elapsed = sample.Timestamp - motionTimestamp;
        bool reset = !hasMotionBaseline || Init || sample.SourceId != motionSourceId
            || sample.TrackingEpoch != motionTrackingEpoch || elapsed <= 0 || (float)elapsed <= 0f
            || elapsed > Study3MotionTracker.MaximumAgeSeconds;
        if (reset)
        {
            ResetMotionBaseline();
            hasMotionBaseline = true;
            motionSourceId = sample.SourceId;
            motionTrackingEpoch = sample.TrackingEpoch;
            motionSequence = sample.Sequence;
            motionTimestamp = sample.Timestamp;
            ThumbTipPose1 = ThumbTipPose2 = tip;
            sample.TryGetJointPose(TrackedHandJoint.ThumbDistalJoint, out ThumbDistalPose);
            AttributionPoseValid = true;
            Init = false;
            return;
        }

        ThumbTipPose1 = ThumbTipPose2;
        ThumbTipPose2 = tip;
        sample.TryGetJointPose(TrackedHandJoint.ThumbDistalJoint, out ThumbDistalPose);
        Distance = (ThumbTipPose2.Position - ThumbTipPose1.Position).magnitude;
        v = Distance / (float)elapsed;
        if (vpre != float.MaxValue)
            a = (v - vpre) / (float)elapsed;
        if (float.IsNaN(v) || float.IsInfinity(v) || float.IsNaN(a) || float.IsInfinity(a))
        {
            ResetMotionBaseline();
            return;
        }
        vpre = v;
        apre = a;
        motionSequence = sample.Sequence;
        motionTimestamp = sample.Timestamp;
        motionSampleDeltaTime = (float)elapsed;
        AttributionPoseValid = true;
    }

    void Start()
    {
        condition = 'N';
        HighLighted = false;
        Hesitated = false;
        Init = true;
        Prepared = false;
        Entered = false;
        CloseSti = false;
        Invoked = false;
        bigger = false;
        smaller = false;
        curBigger = false;
        UpdateT = 0.0f;
        vpre = float.MaxValue;
        v = float.MaxValue;
        apre = float.MaxValue;
        a = float.MaxValue;

        Bounds = gameObject.GetComponent<Collider>().bounds;
        meshRenderer = gameObject.GetComponent<MeshRenderer>();


        InEvent ??= new UnityEvent();
        InEvent_Bool ??= new UnityEvent<bool>();
        BeforeHesEvent ??= new UnityEvent();
        HesEvent ??= new UnityEvent();
        OutEvent ??= new UnityEvent();
        AfterOutEvent ??= new UnityEvent();

        initialized = true;

    }

    void Update()
    {
        // Make sure the collider moves with the body locked transform
        //Bounds.center = transform.position;
        //print("kkk");
        //print(gameObject.name);
        //print(Bounds.center.x);
        //print(Bounds.center.y);
        //print(Bounds.center.z);
    }

    void FixedUpdate()
    {
        if (hasMotionBaseline && Time.realtimeSinceStartupAsDouble - motionTimestamp > Study3MotionTracker.MaximumAgeSeconds)
            ResetMotionBaseline();
    }

    private void ProcessSample(float sampleDeltaTime)
    {
        Bounds = gameObject.GetComponent<Collider>().bounds;
        if (AttributionPoseValid && !Init)
        {
            #region 010 Behavior
            // Try get the position of Distal and Middle Joint
            //Study3HandSamplingDiagnostics.TryGetJointPose(this, TrackedHandJoint.ThumbMiddleJoint, Handedness.Any, out ThumbMiddlePose);
            // Judge if the fingertip is inside
            if (Vector3.Distance(Bounds.center, ThumbTipPose2.Position) <= triggerDis)
            {
                //UnityEngine.Debug.LogFormat("Inside {0}", name);
                // print("校验");
                // print(Bounds.center.x);
                // print(Bounds.center.y);
                // print(Bounds.center.z);
                // print(ThumbTipPose2.Position.x);
                // print(ThumbTipPose2.Position.y);
                // print(ThumbTipPose2.Position.z);
                // print(Vector3.Distance(Bounds.center, ThumbTipPose2.Position));
                Entered = true;
                InEvent.Invoke();
                if (!Invoked)
                {

                    //InMaterial();
                }
                // Change Outlook ,���ﲻ��Ҫ�ı�outlook��ֻ��Ҫ�����¼�����
                //if (!CloseSti && !HighLighted)
                //    meshRenderer.material = InMate;
                UpdateT += sampleDeltaTime;

                // If already enter hesitate state, do not act more
                if (Hesitated)
                    return;
                // else, act as the set mode (Delay-Long Tap, Fast-Short Tap, Acce, or Velo)
                if (Delay || Fast || FastForKeyboard)
                {
                    if (UpdateT > WaitTime)
                    {
                        // Invoke the HesEvent if time exceeds the WaitTime
                        if (!Invoked)
                        {
                            BeforeHesEvent.Invoke();
                            HesEvent.Invoke();
                        }
                        Hesitated = true;
                    }
                }
                else if (Acce)
                {
                    if (Prepared)
                    {
                        PrepTime += sampleDeltaTime;
                        if (PrepTime >= HesTime)
                        {
                            //Invoke the HesEvent if prepared status exceeds the HesTime
                            if (!Invoked)
                            {
                                BeforeHesEvent.Invoke();
                                HesEvent.Invoke();
                            }
                            Hesitated = true;
                        }
                        // Condition C
                        else if (JudgeC())
                        {
                            Prepared = false;
                        }
                    }
                    // Condition A and B
                    else if (JudgeAcce())
                    {
                        PrepTime = 0;
                        Prepared = true;
                    }
                }
                else if (Velo)
                {
                    if (Prepared)
                    {
                        PrepTime += sampleDeltaTime;
                        if (PrepTime > HesTime)
                        {
                            //Invoke the HesEvent if prepared status exceeds the HesTime
                            if (!Invoked)
                            {
                                BeforeHesEvent.Invoke();
                                HesEvent.Invoke();
                            }
                            Hesitated = true;
                        }
                        // Condition C
                        else if (JudgeC())
                        {
                            Prepared = false;
                        }
                    }
                    // Condition A
                    else if (JudgeV())
                    {
                        PrepTime = 0;
                        Prepared = true;
                    }
                }
                else if (AcceOnly)
                {
                    if (Prepared)
                    {
                        PrepTime += sampleDeltaTime;
                        if (PrepTime >= HesTime)
                        {
                            //Invoke the HesEvent if prepared status exceeds the HesTime
                            if (!Invoked)
                            {
                                BeforeHesEvent.Invoke();
                                HesEvent.Invoke();
                            }
                            Hesitated = true;
                        }
                    }
                    // Condition B
                    else if (JudgeAcceOnly())
                    {
                        PrepTime = 0;
                        Prepared = true;
                    }
                }
                else if (AcceOld)
                {
                    // Condition A and B
                    if (JudgeAcce())
                    {
                        //Invoke the HesEvent if prepared status exceeds the HesTime
                        if (!Invoked)
                        {
                            BeforeHesEvent.Invoke();
                            HesEvent.Invoke();
                        }
                        Hesitated = true;
                    }
                }
                #region Old Line-crossing version
                // Used when implementing lineCrossing AC
                if (curBigger)
                {
                    bigger = true;
                    smaller = false;
                }
                else
                {
                    smaller = true;
                    bigger = false;
                }
                #endregion
            }
            else
            {
                // After the 0 of 010, performing the 10 of 010
                if (Entered)
                {
                    if (!CloseSti && !Invoked)
                    {
                        // Invoke the OutEvent (the 10 of 010)
                        OutEvent.Invoke();
                    }
                    AfterOutEvent.Invoke();
                    // Out of the inner area
                    //if (!Hesitated)
                    //{
                    //    OutMaterial();
                    //}
                    //OutMaterial();

                }
                UpdateT = 0;
                Prepared = false;
                Entered = false;
                Hesitated = false;
                CloseSti = false;
                condition = ' ';
            }
            #endregion
        }
    }

    public void showDebugger()
    {
        if (showDebug)
        {
            Debugger[0].text = "A=" + ac.ToString();
            Debugger[1].text = "V=" + vc.ToString();
            Debugger[2].text = "T=" + UpdateT.ToString();
            Debugger[3].text = "C=" + condition.ToString();
        }
    }
    /// <summary>
    /// Set ClostSti as true
    /// </summary>
    public void OpenCloseSti()
    {
        CloseSti = true;
    }

    /// <summary>
    /// Condition A and B for Acce mode, line-crossing version
    /// </summary>
    /// <returns></returns>
    public bool JudgeAcceLineCrossing()
    {
        if (vpre != float.MaxValue && apre != float.MaxValue)
        {
            if ((v < Vupper && a < Ab))
            {
                condition = 'B';
                return true;
            }

            if (bigger && !curBigger)
            {
                condition = 'A';
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Condition A and B for Acce mode
    /// </summary>
    /// <returns></returns>
    public bool JudgeAcce()
    {
        if (vpre != float.MaxValue && apre != float.MaxValue)
        {
            if (v < Vupper && a < Ab)
            {
                ac = a;
                vc = v;
                condition = 'B';
                return true;
            }

            if (v < Vb)
            {
                ac = a;
                vc = v;
                condition = 'A';
                return true;
            }
        }
        return false;
    }

    public bool JudgeCLineCrossing()
    {
        return smaller && curBigger;
    }

    public bool JudgeC()
    {
        if (useAcce)
        {
            print("Using Acceleration for condition C");
            if (a > AccelerationBoundForConditionC)
            {
                condition = 'C';
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            print("Using Acceleration for condition C");
            if (v > Vbc)
            {
                condition = 'C';
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Condition B for Acce mode
    /// </summary>
    /// <returns></returns>
    public bool JudgeAcceOnly()
    {
        if (vpre != float.MaxValue && apre != float.MaxValue)
        {
            if ((v < Vupper && a < Ab))
            {
                condition = 'B';
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Condition A for Velo mode, line-crossing version
    /// </summary>
    /// <returns></returns>
    public bool JudgeVLineCrossing()
    {
        if (vpre != float.MaxValue && apre != float.MaxValue)
        {
            if (bigger && !curBigger)
            {
                condition = 'A';
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Condition A for Velo mode, line-crossing version
    /// </summary>
    /// <returns></returns>
    public bool JudgeV()
    {
        if (vpre != float.MaxValue && apre != float.MaxValue)
        {
            if (v < Vb)
            {
                condition = 'A';
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Set Invoked as true
    /// </summary>
    public void SetInvoked()
    {
        Invoked = true;
    }

    /// <summary>
    /// Set Invoked as false
    /// </summary>
    public void UnInvoked()
    {
        Invoked = false;
    }

    public void OutMaterial()
    {
        meshRenderer.material = OutMate;
    }

    public void InMaterial()
    {
        meshRenderer.material = InMate;
    }
    public void ToggleHLMaterial()
    {
        if (HighLighted)
            HighLighted = false;
        else
        {
            HighLighted = true;
            meshRenderer.material = HLMate;
        }

    }

}

