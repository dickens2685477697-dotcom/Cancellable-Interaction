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
/// <param name="IndexTipPose1">Pose for previous frame's IndexTip pose</param>
/// <param name="IndexTipPose2">Pose for current frame's IndexTip pose</param>
/// <param name="IndexDistalPose">Pose for current Distal joint index finger pose</param>
/// <param name="IndexMiddlePose">Pose for current Middle joint index finger pose</param>
/// <param name="Bounds">Object's Bounds, to manipulate the collider</param>
/// <param name="boundCenter">Object bounds' center point</param>
/// <param name="Debugger">Dynamic linking text debugger shower: Row 1 = velocity, Row 2 = Acceleration, ABC = trigger condition</param>
/// <param name="ShowDebug">Bool set true if want to show debugger</param>
/// <param name="condition">Trigger condition in the IxDL: A B or C</param>
/// <param name="OutMate">Material when the finger is out of the button</param>
/// <param name="InMate">Material when the finger is in the button</param>
public class AcceStimulate : MonoBehaviour
{
    // Study1 uses application-received MRTK joint updates; other scenes keep legacy timing.
    public bool useMrtkJointUpdates;
    public bool UsesMrtkJointUpdates { get; private set; }
    public Study3JointSample CurrentJointSample { get; private set; }
    public uint MotionSourceId { get; private set; }
    public long MotionTrackingEpoch { get; private set; }
    public double MotionSampleTimestamp { get; private set; }
    public bool MotionVelocityValid { get; private set; }
    public bool MotionAccelerationValid { get; private set; }
    public event System.Action MotionTrackingInterrupted;
    private bool initialized, subscribed, hasEventPosition, hasEventVelocity;
    private double previousEventTime;

    public long MotionSampleSequence { get; private set; }
    public double MotionSampleFixedTime { get; private set; }
    public float MotionSampleDeltaTime { get; private set; }
    public bool IndexTipPoseValid { get; private set; }
    public bool IndexDistalPoseValid { get; private set; }
    public UnityEvent<bool> InEvent_Bool;
    public UnityEvent InEvent, BeforeHesEvent, HesEvent, OutEvent, AfterOutEvent;
    public float UpdateT, WaitTime, PrepTime, HesTime, vpre, v, vc, apre, a, ac, Vb, Vbc, Ab, Vupper;
    public bool HighLighted, Hesitated, Init, Delay, Fast, FastForKeyboard, Acce, AcceOnly, AcceOld, Velo, Prepared, Entered, CloseSti, Invoked, bigger, smaller, curBigger;
    public MixedRealityPose IndexTipPose1;
    public MixedRealityPose IndexTipPose2;
    public MixedRealityPose IndexDistalPose;
    public MixedRealityPose IndexMiddlePose;
    Bounds Bounds;
    public Vector3 boundCenter;
    public TMP_Text[] Debugger;
    public bool showDebug;
    public char condition;
    public Material OutMate, InMate, HLMate;
    public MeshRenderer meshRenderer;
    public float Distance;

    // Set Condition C's bound to Acceleration instead of Velocity.
    public bool useAcce = false;
    public float AccelerationBoundForConditionC;

    private void Awake()
    {
        UsesMrtkJointUpdates = useMrtkJointUpdates || gameObject.scene.path == "Assets/Scenes/Study1.unity";
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
        SubscribeJointUpdates();
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
        if (UsesMrtkJointUpdates)
        {
            if (hasEventPosition && Time.realtimeSinceStartupAsDouble - previousEventTime > Study3MotionTracker.MaximumAgeSeconds)
                ResetEventMotion(true);
            return;
        }
        MotionSampleSequence++;
        MotionSampleFixedTime = Time.fixedTimeAsDouble;
        MotionSampleDeltaTime = Time.fixedDeltaTime;
        IndexDistalPoseValid = false;
        Bounds = gameObject.GetComponent<Collider>().bounds;
        IndexTipPoseValid = HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Any, out IndexTipPose2);
        if (IndexTipPoseValid)
        {
            #region V and A Calculation: V is without +-, A has +- for V's tendency to change bigger or smaller
            if (Init)
            {
                Init = false;
                IndexTipPose1 = IndexTipPose2;
                return;

            }

            Distance = (IndexTipPose2.Position - IndexTipPose1.Position).magnitude;
            v = Distance / Time.fixedDeltaTime;
            //print("P1  " + IndexTipPose1.Position.ToString("F5"));
            //print("P2  " + IndexTipPose2.Position.ToString("F5"));
            //if (v > Vb)
            //    curBigger = true;
            //else
            //    curBigger = false;
            IndexTipPose1 = IndexTipPose2;
            if (vpre != float.MaxValue)
            {
                a = (v - vpre) / Time.fixedDeltaTime;
            }
            //UnityEngine.Debug.LogFormat("{0}, {1}, {2}",vpre, v, a);
            vpre = v;
            apre = a;

            #endregion

              ProcessContact(Time.deltaTime);
        }
    }

    private void ProcessContact(float sampleDeltaTime)
    {
            #region 010 Behavior
            // Try get the position of Distal and Middle Joint
            if (UsesMrtkJointUpdates)
            {
                IndexDistalPoseValid = CurrentJointSample.TryGetJointPose(TrackedHandJoint.IndexDistalJoint, out IndexDistalPose);
                CurrentJointSample.TryGetJointPose(TrackedHandJoint.IndexMiddleJoint, out IndexMiddlePose);
            }
            else
            {
                IndexDistalPoseValid = HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexDistalJoint, Handedness.Any, out IndexDistalPose);
                HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexMiddleJoint, Handedness.Any, out IndexMiddlePose);
            }
            // Judge if the fingertip is inside
            if (Bounds.Contains(IndexTipPose2.Position))
            {
                bool wasEntered = Entered;
                //UnityEngine.Debug.LogFormat("Inside {0}", name);
                // print("校验");
                // print(Bounds.center.x);
                // print(Bounds.center.y);
                // print(Bounds.center.z);
                // print(IndexTipPose2.Position.x);
                // print(IndexTipPose2.Position.y);
                // print(IndexTipPose2.Position.z);
                // print(Vector3.Distance(Bounds.center, IndexTipPose2.Position));
                Entered = true;
                InEvent.Invoke();
                if (!Invoked)
                {
                    
                    //InMaterial();
                }
                // Change Outlook ,���ﲻ��Ҫ�ı�outlook��ֻ��Ҫ�����¼�����
                //if (!CloseSti && !HighLighted)
                //    meshRenderer.material = InMate;
                UpdateT += UsesMrtkJointUpdates && !wasEntered ? 0f : sampleDeltaTime;

                // If already enter hesitate state, do not act more
                if (Hesitated)
                    return;
                // A baseline sample is useful to logs and contact state but has no velocity.
                if (UsesMrtkJointUpdates && !MotionVelocityValid && !(Delay || Fast || FastForKeyboard))
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

    private void OnEnable()
    {
        if (initialized) SubscribeJointUpdates();
    }

    private void OnDisable()
    {
        UnsubscribeJointUpdates();
        if (UsesMrtkJointUpdates) ResetEventMotion(false);
    }

    private void SubscribeJointUpdates()
    {
        if (!UsesMrtkJointUpdates || subscribed || !isActiveAndEnabled) return;
        Study3HandPoseSource.EnsureAvailable();
        Study3HandPoseSource.JointSampleUpdated += OnJointSample;
        Study3HandPoseSource.SelectedSourceLost += OnSelectedSourceLost;
        subscribed = true;
    }

    private void UnsubscribeJointUpdates()
    {
        if (!subscribed) return;
        Study3HandPoseSource.JointSampleUpdated -= OnJointSample;
        Study3HandPoseSource.SelectedSourceLost -= OnSelectedSourceLost;
        subscribed = false;
    }

    private void OnSelectedSourceLost(uint sourceId)
    {
        if (hasEventPosition && sourceId == MotionSourceId) ResetEventMotion(true);
    }

    private void ResetEventMotion(bool interrupted)
    {
        if (interrupted) MotionTrackingInterrupted?.Invoke();
        hasEventPosition = hasEventVelocity = false;
        MotionVelocityValid = MotionAccelerationValid = IndexTipPoseValid = IndexDistalPoseValid = false;
        Init = true;
        v = a = vpre = apre = float.MaxValue;
        UpdateT = PrepTime = 0f;
        Entered = Prepared = Hesitated = CloseSti = false;
        condition = ' ';
    }

    /// <summary>Discard the current motion baseline after an XR reference-space change.</summary>
    public void ResetForReferenceSpaceChange()
    {
        if (UsesMrtkJointUpdates) ResetEventMotion(false);
    }

    private void OnJointSample(Study3JointSample sample)
    {
        if (!isActiveAndEnabled || !sample.TryGetJointPose(TrackedHandJoint.IndexTip, out MixedRealityPose pose)) return;
        double now = Time.realtimeSinceStartupAsDouble;
        if (double.IsNaN(sample.Timestamp) || double.IsInfinity(sample.Timestamp) || sample.Timestamp > now ||
            now - sample.Timestamp > Study3MotionTracker.MaximumAgeSeconds ||
            float.IsNaN(pose.Position.x) || float.IsNaN(pose.Position.y) || float.IsNaN(pose.Position.z) ||
            float.IsInfinity(pose.Position.x) || float.IsInfinity(pose.Position.y) || float.IsInfinity(pose.Position.z))
        { ResetEventMotion(true); return; }
        if (hasEventPosition && (sample.SourceId != MotionSourceId || sample.TrackingEpoch != MotionTrackingEpoch ||
            sample.Timestamp - previousEventTime > Study3MotionTracker.MaximumAgeSeconds)) ResetEventMotion(true);
        if (hasEventPosition && (sample.Sequence <= MotionSampleSequence || sample.Timestamp <= previousEventTime)) return;

        CurrentJointSample = sample;
        MotionSourceId = sample.SourceId;
        MotionTrackingEpoch = sample.TrackingEpoch;
        MotionSampleSequence = sample.Sequence;
        MotionSampleTimestamp = sample.Timestamp;
        MotionSampleFixedTime = Time.fixedTimeAsDouble;
        MotionSampleDeltaTime = hasEventPosition ? (float)(sample.Timestamp - previousEventTime) : 0f;
        if (hasEventPosition && (MotionSampleDeltaTime <= 0f || float.IsInfinity(MotionSampleDeltaTime)))
        { ResetEventMotion(true); return; }
        IndexTipPoseValid = true;
        IndexTipPose2 = pose;
        MotionAccelerationValid = false;
        if (hasEventPosition)
        {
            Distance = (pose.Position - IndexTipPose1.Position).magnitude;
            float previousSpeed = v;
            v = Distance / MotionSampleDeltaTime;
            MotionVelocityValid = true;
            if (hasEventVelocity)
            {
                a = (v - previousSpeed) / MotionSampleDeltaTime;
                MotionAccelerationValid = true;
            }
            else a = float.MaxValue;
            if (float.IsNaN(v) || float.IsInfinity(v) ||
                (MotionAccelerationValid && (float.IsNaN(a) || float.IsInfinity(a))))
            { ResetEventMotion(true); return; }
            vpre = v;
            apre = a;
            hasEventVelocity = true;
        }
        else
        {
            v = a = vpre = apre = float.MaxValue;
            MotionVelocityValid = false;
        }
        IndexTipPose1 = pose;
        previousEventTime = sample.Timestamp;
        hasEventPosition = true;
        Init = false;
        Bounds = GetComponent<Collider>().bounds;
        ProcessContact(MotionSampleDeltaTime * Time.timeScale);
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
            if (UsesMrtkJointUpdates && !MotionAccelerationValid) return false;
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
        if (UsesMrtkJointUpdates && !MotionAccelerationValid) return false;
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
        if (UsesMrtkJointUpdates ? MotionVelocityValid : vpre != float.MaxValue && apre != float.MaxValue)
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

