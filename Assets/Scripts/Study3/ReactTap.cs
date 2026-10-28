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
using Unity.Mathematics;
using System;

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
public class ReactTap : MonoBehaviour
{
    public UnityEvent<bool> InEvent_Bool;
    public UnityEvent InEvent, BeforeHesEvent, HesEvent, OutEvent, AfterOutEvent, ChangeEvent, CancelEvent;

    public float UpdateT, WaitTime, PrepTime, HesTime, vpre, v, vc, apre, a, ac, Vb, Vbc, Ab, Vupper, V_vec, A_vec, V_up, V_down = 0.031f, A_ = 5.003f, V_cross, V_vec_pre, A_vec_pre;
    public bool HighLighted, Hesitated, Init, Delay, Fast, FastForKeyboard, Acce, AcceOnly, AcceOld, Velo, Prepared, Entered, current_state, CloseSti, Invoked, bigger, smaller, curBigger, AccePlus, AngleMode, toTrigger, WristSpeedMode = true;
    public MixedRealityPose IndexTipPose1;// previous frame's IndexTip pose(写的真差，不知道写个pre�?) prev_手指节点
    public MixedRealityPose IndexTipPose2; // Current frame's IndexTip pose 手指节点 图片中的a
    public MixedRealityPose IndexDistalPose, IndexKnucklePose; // Current frame's IndexDistal joint pose 手指第一指关�? 图片中的c
    public MixedRealityPose IndexMiddlePose; // 手指第二指关�? 图片中的b
    public MixedRealityPose WristPose; // Current wrist pose 手腕节点 图片中的d
    public MixedRealityPose WristPosePre; // Previous frame's wrist position prev_手腕节点
    Bounds Bounds;
    public Vector3 boundCenter;
    public TMP_Text[] Debugger;
    public bool showDebug;
    public char condition;
    public Material OutMate, InMate, HLMate;
    public MeshRenderer meshRenderer;
    public float Distance;

    // Wrist speed parameters
    public float WristSpeedThreshold = 0.2f; // Wrist speed threshold
    public float wristSpeed; // Current wrist speed
    public float wristSpeedPre; // Previous frame's wrist speed


    // Set Condition C's bound to Acceleration instead of Velocity.
    public bool useAcce = false;
    private bool isCanceled = false;
    public float AccelerationBoundForConditionC;

    float X_distance;
    float Y_distance;
    float Z_distance;
    float V_x;
    float V_y;
    float V_z;
    float A_x;
    float A_y;
    float A_z;
    float V_xpre;
    float V_ypre;
    float V_zpre;
    float A_xpre;
    float A_ypre;
    float A_zpre;
    public float a_abs;

    public float initialAngle; // Initial angle
    public float currentAngle; // Current angle
    public float angleChangeThreshold = 6.0f; // Angle change threshold

    // Angle velocity parameters
    public float previousAngle; // Previous angle
    public float angleVelocity; // Angle velocity
    public float angleVelocityThreshold = 5.0f; // Angle velocity threshold (degrees per second)

    // Angle-based trigger control flags
    public bool wristSpeedTriggersDisabledB = false; // Flag to disable wrist speed triggers when angle changes significantly
    private bool initialAngleSet = false; // Flag to track if initial angle has been set once

    // 6.27 csr 
    public float avgPeakValue = 0.1f;
    public bool avgPeakValueDisabled = false;
    public bool BF_EventEnabled = false;
    public bool C_EventEnabled = false;
    public bool B_EventEnabled = false;
    public bool B_triggered = false;
    public bool crossedThreshold;

    // CWC: 新增的球内移动距离参数
    public float InBallMovementThreshold = 0.03f; // 球内移动距离阈值，可在Unity中调整
    public float angleChangeThreshold_new = 5.0f; // 手腕手指夹角变化阈值

    [Header("Adaptive Enter Speed")]
    public bool useAdaptiveEnterSpeed = false;
    public float adaptiveEnterSpeedThreshold = 0.2f;
    public float adaptiveEnterSpeedK = 1.0f;
    private float adaptiveEnterSpeedAtEntry = 0.0f;

    [Header("Adaptive Realtime Finger Speed")]
    public bool useAdaptiveRealtimeFingerSpeed = false;
    [InspectorName("finger speed thershold")]
    public float fingerSpeedThershold = 0.2f;
    [InspectorName("adaptive V_down min")]
    public float adaptiveVDownMin = 0.031f;
    [InspectorName("adaptive V_down max")]
    public float adaptiveVDownMax = 0.031f;

    [Header("Enter-Based React Trigger")]
    [Tooltip("When enabled, entering the ReactTap collider immediately satisfies the speed and acceleration gate, then keeps the existing angle and movement checks.")]
    public bool enterSatisfiesSpeedAcceCondition = false;

    // CWC: 用于计算球内移动距离的变量
    private Vector3 entryPosition; // 进入球时的位置
    public float currentInBallDistance; // 当前球内移动距离
    public float storedCurrentInBallDistance = 0f; // 当前球内移动距离
    private bool hasEnteredBall = false; // 是否已记录进入位置

    // CWC: 用于角度计算的变量
    private float entryAngle; // 进入球时的手腕手指夹角
    private float currentAngleChange; // 当前角度变化

    // CWC: 用于控制触发状态的变量
    private bool reacConditionFullyMet = false; // 完整Reac条件是否满足
    private bool shouldStopRecording = false; // 是否应停止记录速度加速度

    private bool shouldStopDistanceCalculation = false; // 是否应停止计算球内移动距离

    // CWC: 新增变量用于满足速度加速度条件时的位置记录
    public Vector3 speedAcceMetPosition; // 满足速度加速度条件时的手指位置
    private bool speedAcceConditionMet = false; // 是否已满足速度加速度条件
    
    // CWC: 新增变量用于跟踪角度变化过大且手指在球内的状态
    private bool angleExceededInBall = false; // 角度变化过大且手指在球内的状态

    void Start()
    {
        condition = 'N';
        HighLighted = false;
        Hesitated = false;
        Init = true;
        Prepared = false;
        Entered = false;
        current_state = false;
        CloseSti = false;
        Invoked = false;
        bigger = false;
        smaller = false;
        curBigger = false;
        toTrigger = false;
        UpdateT = 0.0f;
        vpre = 0.0f;
        v = 0.0f;
        apre = 0.0f;
        a = 0.0f;

        X_distance = float.MaxValue;
        Y_distance = float.MaxValue;
        Z_distance = float.MaxValue;
        V_x = float.MaxValue;
        V_y = float.MaxValue;
        V_z = float.MaxValue;
        A_x = float.MaxValue;
        A_y = float.MaxValue;
        A_z = float.MaxValue;
        V_xpre = float.MaxValue;
        V_ypre = float.MaxValue;
        V_zpre = float.MaxValue;
        A_xpre = float.MaxValue;
        A_ypre = float.MaxValue;
        A_zpre = float.MaxValue;
        a_abs = 0.0f;

        initialAngle = 0f;
        currentAngle = 0f;
        previousAngle = 0f;
        angleVelocity = 0f;
        wristSpeedTriggersDisabledB = false;
        initialAngleSet = false;

        Bounds = gameObject.GetComponent<Collider>().bounds;
        meshRenderer = gameObject.GetComponent<MeshRenderer>();

        InEvent ??= new UnityEvent();
        InEvent_Bool ??= new UnityEvent<bool>();
        BeforeHesEvent ??= new UnityEvent();
        HesEvent ??= new UnityEvent();
        OutEvent ??= new UnityEvent();
        AfterOutEvent ??= new UnityEvent();

        wristSpeed = 0.0f; // Initialize wrist speed
        wristSpeedPre = 0.0f; // Initialize previous wrist speed
        avgPeakValueDisabled = false;
        BF_EventEnabled = false;
        C_EventEnabled = false;
        //avgPeakValue = 0.0f;

        // CWC: 初始化新添加的变量
        speedAcceMetPosition = Vector3.zero;
        speedAcceConditionMet = false;
    }

    void Update()
    {
        // Make sure the collider moves with the body locked transform
        Bounds.center = transform.position;
    }

    void FixedUpdate()
    {
        Bounds = gameObject.GetComponent<Collider>().bounds;
        if (Study3HandSamplingDiagnostics.TryGetJointPose(this, TrackedHandJoint.IndexTip, Handedness.Any, out IndexTipPose2) &&
            Study3HandSamplingDiagnostics.TryGetJointPose(this, TrackedHandJoint.IndexDistalJoint, Handedness.Any, out IndexDistalPose) &&
            Study3HandSamplingDiagnostics.TryGetJointPose(this, TrackedHandJoint.Wrist, Handedness.Any, out WristPose) &&
            Study3HandSamplingDiagnostics.TryGetJointPose(this, TrackedHandJoint.IndexKnuckle, Handedness.Any, out IndexKnucklePose)) // Get current wrist pose
        {
            #region V and A Calculation: V is without +-, A has +- for V's tendency to change bigger or smaller
            if (Init)
            {
                Init = false;
                IndexTipPose1 = IndexTipPose2;
                WristPosePre = WristPose; // Store current wrist pose as previous
                previousAngle = CalculateAngle_between_finger_wrist();// Initialize previous angle
                return;

            }
            //old
            Distance = (IndexTipPose2.Position - IndexTipPose1.Position).magnitude;
            if (Distance < 0.001f)
            {
                return;
            }
            v = Distance / Time.fixedDeltaTime;

            a = (v - vpre) / Time.fixedDeltaTime;

            vpre = v;
            apre = a;
            // Replace with directional velocity and acceleration calculation
            X_distance = IndexTipPose2.Position.x - IndexTipPose1.Position.x;
            Y_distance = IndexTipPose2.Position.y - IndexTipPose1.Position.y;
            Z_distance = IndexTipPose2.Position.z - IndexTipPose1.Position.z;
            V_x = X_distance / Time.fixedDeltaTime;
            V_y = Y_distance / Time.fixedDeltaTime;
            V_z = Z_distance / Time.fixedDeltaTime;
            IndexTipPose1 = IndexTipPose2;
            A_x = (V_x - V_xpre) / Time.fixedDeltaTime;
            A_y = (V_y - V_ypre) / Time.fixedDeltaTime;
            A_z = (V_z - V_zpre) / Time.fixedDeltaTime;

            V_vec = math.sqrt(V_x * V_x + V_y * V_y + V_z * V_z);
            A_vec = math.sqrt(A_x * A_x + A_y * A_y + A_z * A_z);
            V_vec_pre = math.sqrt(V_xpre * V_xpre + V_ypre * V_ypre + V_zpre * V_zpre);
            A_vec_pre = math.sqrt(A_xpre * A_xpre + A_ypre * A_ypre + A_zpre * A_zpre);
            A_xpre = A_x;
            A_ypre = A_y;
            A_zpre = A_z;
            V_xpre = V_x;
            V_ypre = V_y;
            V_zpre = V_z;
            a_abs = math.abs(a);

            // Calculate wrist speed
            float wristDistance = (WristPose.Position - WristPosePre.Position).magnitude;
            wristSpeed = wristDistance / Time.fixedDeltaTime;

            WristPosePre = WristPose; // Update previous wrist position
            // csr: calculate wrist speed (in every frame)
            crossedThreshold = (wristSpeedPre < WristSpeedThreshold && wristSpeed >= WristSpeedThreshold) ||
                                                (wristSpeedPre > WristSpeedThreshold && wristSpeed <= WristSpeedThreshold);
            // Calculate angle
            currentAngle = CalculateAngle_between_finger_wrist();
            angleVelocity = Mathf.Abs(currentAngle - previousAngle) / Time.fixedDeltaTime;
            previousAngle = currentAngle;
            #endregion

            // CWC: 首先检查手指是否在球内，无论shouldStopRecording状态如何
            bool isInBall = Bounds.Contains(IndexTipPose2.Position) || Bounds.Contains(IndexDistalPose.Position);
            
            // 处理手指离开球体的情况
            if (!isInBall && Entered)
            {
                // After the 0 of 010, performing the 10 of 010
                if (wristSpeed > avgPeakValue) C_EventEnabled = true;//csr 筛除掉在球内速度不够，但是出球了速度够了
                                                                     //if (toTrigger && !CloseSti && !wristSpeedTriggersDisabled && C_EventEnabled && !B_triggered) // Check trigger conditions
                if (!wristSpeedTriggersDisabledB && C_EventEnabled && !B_triggered)
                {
                    // C event - 添加角度检查条件
                    // 只有在角度变化小于阈值时才触发OutEvent
                    if (currentAngleChange <= angleChangeThreshold_new)
                    {
                         //print("triggerC! - 角度条件满足");
                        //CWC：OutEvent.Invoke();
                        toTrigger = false;
                    }
                    else
                    {
                         //print("C事件触发但角度变化过大，不触发OutEvent");
                        
                        // 如果速度加速度条件已满足，调用OutMaterial并设置角度超限标志
                        if (speedAcceConditionMet)
                        {
                            OutMaterial();
                            angleExceededInBall = true;
                             //print("C事件角度变化过大且速度加速度条件已满足，调用OutMaterial并设置angleExceededInBall标志");
                        }
                    }
                }

                 //print("out ball" + wristSpeed + " " + wristSpeedTriggersDisabledB + " " + C_EventEnabled + " " + B_triggered);

                AfterOutEvent.Invoke();
                UpdateT = 0;

                // 修改：手指离开小球时的处理逻辑
                if (reacConditionFullyMet)
                {
                    // 如果已经触发成功，离开球体时重置所有状态
                     //print("触发成功后手指离开小球，重置所有状态");
                    Prepared = false;
                    Entered = false;
                    wristSpeedTriggersDisabledB = false;
                    C_EventEnabled = false;
                    Hesitated = true;
                    CloseSti = false;
                    condition = ' ';
                    B_triggered = false;
                    BF_EventEnabled = false;
                    B_EventEnabled = false;

                    // 重置相关变量
                    hasEnteredBall = false;

                    currentInBallDistance = 0f;
                    currentAngleChange = 0f;
                    shouldStopRecording = false;
                    reacConditionFullyMet = false;
                    shouldStopDistanceCalculation = true;
                    speedAcceConditionMet = false;
                    speedAcceMetPosition = Vector3.zero;
                    initialAngleSet = false;
                    angleExceededInBall = false;

                     //UnityEngine.Debug.Log("触发成功后离开球体，重置所有状态");

                    // 更新前一帧的腕部速度并返回
                    wristSpeedPre = wristSpeed;
                    return;
                }
                else if (speedAcceConditionMet && currentAngleChange <= angleChangeThreshold_new)
                {
                    // 角度满足条件但未触发成功，不重置状态，继续计算距离
                     //print($"手指离开小球但角度满足条件({currentAngleChange:F2}° <= {angleChangeThreshold_new}°)，保持状态不重置，继续执行距离计算");
                    Entered = false; // 只重置Entered状态
                    // 注意：不重置speedAcceConditionMet和speedAcceMetPosition，以便出球后继续检测距离
                    // 不return，继续执行后续的距离计算逻辑
                }
                else if (speedAcceConditionMet)
                {
                    // 检查是否在球内时角度变化过大
                    if (angleExceededInBall)
                    {
                        // 速度加速度条件已满足但角度变化过大且保持此状态出球，重置所有状态
                         //print($"手指离开小球，速度加速度条件已满足但角度变化过大且保持此状态出球，重置所有状态");
                        Prepared = false;
                        Entered = false;
                        wristSpeedTriggersDisabledB = false;
                        C_EventEnabled = false;
                        Hesitated = false;
                        CloseSti = false;
                        condition = ' ';
                        B_triggered = false;
                        BF_EventEnabled = false;
                        B_EventEnabled = false;
                        
                        // 重置相关变量
                        hasEnteredBall = false;

                        currentInBallDistance = 0f;
                        currentAngleChange = 0f;
                        shouldStopRecording = false;
                        reacConditionFullyMet = false;
                        shouldStopDistanceCalculation = true;
                        speedAcceConditionMet = false;
                        speedAcceMetPosition = Vector3.zero;
                        initialAngleSet = false;
                        angleExceededInBall = false;
                    }
                    else
                    {
                        // 速度加速度条件已满足但角度暂时不满足，保持关键状态不重置
                         //print($"手指离开小球，速度加速度条件已满足但角度暂时不满足({currentAngleChange:F2}° > {angleChangeThreshold_new}°)，保持关键状态继续");
                        Entered = false; // 只重置Entered状态
                        // 保持speedAcceConditionMet、speedAcceMetPosition、currentAngleChange等关键状态
                        // 让用户有机会重新进入小球继续完成手势
                    }
                }
                else
                {
                    // 角度不满足条件或未满足速度加速度条件，重置所有状态
                     //print($"手指离开小球且未满足速度加速度条件，重置所有状态");
                    Prepared = false;
                    Entered = false;
                    wristSpeedTriggersDisabledB = false;
                    C_EventEnabled = false;
                    Hesitated = false;
                    CloseSti = false;
                    condition = ' ';
                    B_triggered = false;
                    BF_EventEnabled = false;
                    B_EventEnabled = false;

                    // CWC: 重置相关变量
                    hasEnteredBall = false;

                    currentInBallDistance = 0f;
                    currentAngleChange = 0f;
                    shouldStopRecording = false;
                    reacConditionFullyMet = false;
                    shouldStopDistanceCalculation = true;
                    speedAcceConditionMet = false;
                    speedAcceMetPosition = Vector3.zero;
                    initialAngleSet = false;
                    angleExceededInBall = false;

                     //UnityEngine.Debug.Log("离开球体，重置所有状态");

                    // 更新前一帧的腕部速度并返回
                    wristSpeedPre = wristSpeed;
                    return;
                }
            }

            #region V and A Calculation: V is without +-, A has +- for V's tendency to change bigger or smaller
            // if (Init)
            // {
            //     Init = false;
            //     IndexTipPose1 = IndexTipPose2;
            //     WristPosePre = WristPose; // Store current wrist pose as previous
            //     previousAngle = CalculateAngle_between_finger_wrist();// Initialize previous angle
            //     return;

            // }
            // //old
            // Distance = (IndexTipPose2.Position - IndexTipPose1.Position).magnitude;
            // v = Distance / Time.fixedDeltaTime;

            // a = (v - vpre) / Time.fixedDeltaTime;

            // vpre = v;
            // apre = a;
            // // Replace with directional velocity and acceleration calculation
            // X_distance = IndexTipPose2.Position.x - IndexTipPose1.Position.x;
            // Y_distance = IndexTipPose2.Position.y - IndexTipPose1.Position.y;
            // Z_distance = IndexTipPose2.Position.z - IndexTipPose1.Position.z;
            // V_x = X_distance / Time.fixedDeltaTime;
            // V_y = Y_distance / Time.fixedDeltaTime;
            // V_z = Z_distance / Time.fixedDeltaTime;
            // IndexTipPose1 = IndexTipPose2;
            // A_x = (V_x - V_xpre) / Time.fixedDeltaTime;
            // A_y = (V_y - V_ypre) / Time.fixedDeltaTime;
            // A_z = (V_z - V_zpre) / Time.fixedDeltaTime;

            // V_vec = math.sqrt(V_x * V_x + V_y * V_y + V_z * V_z);
            // A_vec = math.sqrt(A_x * A_x + A_y * A_y + A_z * A_z);
            // V_vec_pre = math.sqrt(V_xpre * V_xpre + V_ypre * V_ypre + V_zpre * V_zpre);
            // A_vec_pre = math.sqrt(A_xpre * A_xpre + A_ypre * A_ypre + A_zpre * A_zpre);
            // A_xpre = A_x;
            // A_ypre = A_y;
            // A_zpre = A_z;
            // V_xpre = V_x;
            // V_ypre = V_y;
            // V_zpre = V_z;
            // a_abs = math.abs(a);

            // // Calculate wrist speed
            // wristDistance = (WristPose.Position - WristPosePre.Position).magnitude;
            // wristSpeed = wristDistance / Time.fixedDeltaTime;

            // WristPosePre = WristPose; // Update previous wrist position
            // // csr: calculate wrist speed (in every frame)
            // crossedThreshold = (wristSpeedPre < WristSpeedThreshold && wristSpeed >= WristSpeedThreshold) ||
            //                                     (wristSpeedPre > WristSpeedThreshold && wristSpeed <= WristSpeedThreshold);
            // // Calculate angle
            // currentAngle = CalculateAngle_between_finger_wrist();
            // angleVelocity = Mathf.Abs(currentAngle - previousAngle) / Time.fixedDeltaTime;
            // previousAngle = currentAngle;

            #endregion

            #region 010 Behavior
            if (isInBall)
            {
                Entered = true;

                // CWC: 进入球体时记录位置和角度（包括重新进入的情况）
                if (!hasEnteredBall)
                {
                    entryPosition = IndexTipPose2.Position;
                    entryAngle = CalculateAngle_between_finger_wrist();
                    adaptiveEnterSpeedAtEntry = V_vec;
                    hasEnteredBall = true;

                    currentInBallDistance = 0f;
                    currentAngleChange = 0f;
                    reacConditionFullyMet = false;
                    shouldStopRecording = false;
                    shouldStopDistanceCalculation = false;
                    speedAcceConditionMet = false;
                    initialAngleSet = false;
                    angleExceededInBall = false;
                    TrySatisfySpeedAcceConditionOnEnter();
                    // //UnityEngine.Debug.Log($"进入球体，记录初始位置: {entryPosition}, 初始角度: {entryAngle}");
                }
            }

            // CWC: 计算角度变化（必须在进入球体后才计算）
            if (hasEnteredBall)
            {
                currentAngleChange = Mathf.Abs(CalculateAngle_between_finger_wrist() - entryAngle);

                // 实时输出角度变化信息
                if (speedAcceConditionMet)
                {
                    // //print($"实时角度检测 - 当前角度: {CalculateAngle_between_finger_wrist():F2}°, 初始角度: {entryAngle:F2}°, 角度变化: {currentAngleChange:F2}°, 阈值: {angleChangeThreshold_new}°, 角度是否满足: {currentAngleChange <= angleChangeThreshold_new}");
                }
            }

            // CWC: 在WristSpeedMode下，检查速度和加速度条件（球内外都可以执行）
            // 修改：需要同时满足速度和加速度条件，且必须在进入球体后才开始检测
            if (WristSpeedMode && hasEnteredBall && !shouldStopRecording && !shouldStopDistanceCalculation)
            {
                // 检查速度和加速度条件，如果都满足且还未记录位置，则记录当前位置
                if (JudgeCurrentV_vec() && JudgeA_vec() && !speedAcceConditionMet)
                {
                    speedAcceMetPosition = IndexTipPose2.Position;
                    speedAcceConditionMet = true;
                    // //UnityEngine.Debug.Log($"速度和加速度条件都满足，记录满足条件时的位置: {speedAcceMetPosition}");
                }
                
                // 如果速度加速度条件已满足，持续检查角度变化
                if (speedAcceConditionMet && !angleExceededInBall)
                {
                    // //UnityEngine.Debug.Log("持续检查角度变化： " + currentAngleChange);
                    if (currentAngleChange > angleChangeThreshold_new)
                    {
                        OutMaterial();
                        angleExceededInBall = true;
                        
                        CancelEvent.Invoke();
                        // //print($"速度加速度条件已满足但角度变化过大({currentAngleChange:F2}° > {angleChangeThreshold_new}°)，调用OutMaterial");
                    }
                }

                // 满足速度加速度条件后的处理已在前面完成
            }

            // CWC: 计算移动距离和角度变化（球内外都可以计算）
            // 修改：仅在满足速度加速度条件后才开始计算距离
            if (!shouldStopDistanceCalculation)
            {
                if (speedAcceConditionMet)
                {
                    // 以满足速度加速度条件时的位置为圆心，计算当前位置到圆心的距离
                    currentInBallDistance = Vector3.Distance(speedAcceMetPosition, IndexTipPose2.Position);
                     //print("位移距离： " + currentInBallDistance + "位移距离差： " + (currentInBallDistance - InBallMovementThreshold) + "阈值： " + InBallMovementThreshold + " 是否在球内：" + isInBall);
                    
                    ChangeEvent.Invoke();
                        
                    
                    // 检查是否超过阈值
                    if (currentInBallDistance > InBallMovementThreshold)
                    {
                        // 检查角度是否满足条件：不仅要当前角度小于阈值，还要确保之前没有超过过阈值
                        if (currentAngleChange <= angleChangeThreshold_new && !angleExceededInBall)
                        {
                            // 距离超过阈值且角度满足条件，触发OutEvent
                             //print($"距离超过阈值且角度满足条件({currentAngleChange:F2}° <= {angleChangeThreshold_new}°)，触发OutEvent成功");
                            if (!Invoked)
                            {
                                
                                
                                OutEvent.Invoke();
                                 //print("触发0utEvent成功1");
                                BeforeHesEvent.Invoke();
                                HesEvent.Invoke();
                            }

                            // 根据手指位置决定何时重置状态
                            if (!isInBall)
                            {
                                // 触发成功时手指已在球外，立即重置所有状态
                                 //print("触发成功且手指已在球外，立即重置所有状态");
                                 //print("-------------------------------------------");
                                Prepared = false;
                                wristSpeedTriggersDisabledB = false;
                                C_EventEnabled = false;
                                Hesitated = true;
                                CloseSti = false;
                                condition = ' ';
                                B_triggered = false;
                                BF_EventEnabled = false;
                                B_EventEnabled = false;

                                // 重置相关变量
                                hasEnteredBall = false;

                                currentInBallDistance = 0f;
                                currentAngleChange = 0f;
                                shouldStopRecording = true;
                                reacConditionFullyMet = true;
                                shouldStopDistanceCalculation = true;
                                speedAcceConditionMet = false;
                                speedAcceMetPosition = Vector3.zero;
                                initialAngleSet = false;
                            }
                            else
                            {
                                // 触发成功时手指仍在球内，标记为已触发，等待手指离开球时重置
                                 //print("触发成功但手指仍在球内，等待手指离开球时重置状态");
                                shouldStopRecording = true;
                                reacConditionFullyMet = true;
                                shouldStopDistanceCalculation = true;
                                // 不立即重置其他状态，等到手指离开球时再重置
                            }
                        }
                        else
                        {
                            // 角度不满足条件，无法触发
                             //print($"距离超过阈值但角度不满足条件({currentAngleChange:F2}° > {angleChangeThreshold_new}°)，无法触发");
                            
                            // 如果角度变化过大，设置angleExceededInBall标志
                            if (currentAngleChange > angleChangeThreshold_new && !angleExceededInBall)
                            {
                                angleExceededInBall = true;
                                 //print($"角度变化过大({currentAngleChange:F2}° > {angleChangeThreshold_new}°)，设置angleExceededInBall标志");
                            }

                            // 如果手指已离开球体，则重置状态
                            if (!isInBall)
                            {
                                 //print("手指已离开球体且角度不满足条件，重置所有状态");
                                Prepared = false;
                                Entered = false;
                                wristSpeedTriggersDisabledB = false;
                                C_EventEnabled = false;
                                Hesitated = false;
                                CloseSti = false;
                                condition = ' ';
                                B_triggered = false;
                                BF_EventEnabled = false;
                                B_EventEnabled = false;

                                // 重置相关变量
                                hasEnteredBall = false;

                                currentInBallDistance = 0f;
                                currentAngleChange = 0f;
                                shouldStopRecording = false;
                                reacConditionFullyMet = false;
                                shouldStopDistanceCalculation = true;
                                speedAcceConditionMet = false;
                                speedAcceMetPosition = Vector3.zero;
                                initialAngleSet = false;
                            }
                        }
                    }
                }
                else
                {

                    // 未满足速度加速度条件时，距离为0
                    currentInBallDistance = 0f;
                }
            }

            // CWC: 角度变化已在前面计算，此处移除重复代码

            // CWC: 如果应该停止记录，则跳过后续的触发逻辑，但保持距离计算
            if (shouldStopRecording)
            {
                // 更新前一帧的腕部速度
                wristSpeedPre = wristSpeed;
                return;
            }

            // 只有在球内时才执行后续的触发逻辑
            if (isInBall)
            {

                if (JudgeCurrentV_vec() && JudgeA_vec())// 满足A条件
                {

                    BF_EventEnabled = true;//CSR: 这个也许要改成All_EventEnabled?
                    // Set initial angle only once when entering the collider - for angle detection
                    if (!initialAngleSet)
                    {
                        initialAngle = currentAngle;
                        initialAngleSet = true;
                         //UnityEngine.Debug.Log("Setting initial angle ONCE: " + initialAngle);
                    }
                }
                // 如果角度变化过大且手指在球内，阻止后续触发
                if (angleExceededInBall)
                {
                     //print("角度变化过大且手指在球内，阻止后续触发");
                    wristSpeedPre = wristSpeed;
                    return;
                }
                
                if (!Invoked)
                {
                    if (BF_EventEnabled)
                    {
                        InEvent.Invoke();
                        // //UnityEngine.Debug.Log("InEvent Invoke!!");
                    }
                    //InMaterial();
                }
                UpdateT += Time.deltaTime;



                // If already enter hesitate state, do not act more
                if (Hesitated)
                    return;

                // CSR - Check for angle changes to disable wrist speed triggers when significant finger movement occurs
                if (WristSpeedMode && initialAngleSet) // 只有在initialAngle被正确设置后才进行角度检查
                {
                    // Check if angle change exceeds threshold to disable trigger 1 and 2 (D event)
                    float angleChange = Mathf.Abs(currentAngle - initialAngle);
                     //UnityEngine.Debug.Log("Checking angle change: " + angleChange + " vs threshold: " + angleChangeThreshold + " | wristSpeedTriggersDisabled: " + wristSpeedTriggersDisabledB);
                    if (angleChange > angleChangeThreshold)//CSR: 不考虑波峰值了
                    {
                        // D event 
                         //UnityEngine.Debug.Log("angleChange bigger than threshold - DISABLING wrist speed triggers");
                        wristSpeedTriggersDisabledB = true;
                    }
                }
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
                // CSR
                if (WristSpeedMode)
                {
                    if (BF_EventEnabled) // Use vector velocity and acceleration for consistency
                    {
                        // 注意：速度加速度条件检测已移至函数前面统一处理，这里不再重复检测

                        // 如果速度和加速度条件已经满足，检查是否可以触发事件
                        if (speedAcceConditionMet && hasEnteredBall && !shouldStopRecording)
                        {
                            // 此时速度和加速度条件已经满足，现在先检查角度变化条件
                            //InMaterial();
                            // //print("速度和加速度条件已满足，检查角度变化,InMaterial");

                            // 先检查角度变化条件
                            if (currentAngleChange <= angleChangeThreshold_new)
                            {
                                if (currentInBallDistance >= InBallMovementThreshold && !wristSpeedTriggersDisabledB)// CSR: 添加了wristSpeedTriggersDisabledB条件
                                {
                                    // 距离也满足：触发变色和提示音
                                    // //print("所有条件满足，触发变色和提示音");
                                    if (!Invoked)
                                    {
                                        OutEvent.Invoke();
                                        // //print("触发0utEvent成功2");
                                        BeforeHesEvent.Invoke();
                                        HesEvent.Invoke();
                                    }
                                    reacConditionFullyMet = true;

                                    // 只有在所有条件都满足时才记录WristPose并停止记录速度加速度
                                    RecordWristPoseToCSV();
                                    shouldStopRecording = true;
                                    Hesitated = true;
                                }
                                else
                                {
                                    // 距离不够或wristSpeedTriggersDisabledB为true：不触发变色和提示音，也不设置Hesitated
                                    reacConditionFullyMet = false;
                                }
                            }
                            else
                            {
                                // 角度变化过大：直接不触发，不设置Hesitated，不停止记录
                                OutMaterial();
                                // //print("OutMaterial,角度变化过大，直接不触发变色和提示音");
                                reacConditionFullyMet = false;
                                
                                // 如果速度加速度条件已满足，设置角度超限标志
                                if (speedAcceConditionMet)
                                {
                                    angleExceededInBall = true;
                                    // //print("速度加速度条件已满足但角度变化过大，设置angleExceededInBall标志");
                                }
                            }
                        }
                        
                        //|| (speedChange > WristSpeedThreshold * 0.5f); // Trigger on significant speed change
                        // CSR: 不考虑波峰值了
                        if (!wristSpeedTriggersDisabledB)
                        {
                            // F event
                            C_EventEnabled = true;// C event enabled
                        }
                        // //print("crossthreshold is " + crossedThreshold + "wristSpeedTriggersDisabledB" + wristSpeedTriggersDisabledB);
                        if (!wristSpeedTriggersDisabledB && crossedThreshold)
                        {
                            // B event - 添加角度检查条件
                            // //UnityEngine.Debug.Log("TRIGGERING wrist speed event - crossed threshold from " + wristSpeedPre + " to " + wristSpeed + " (threshold: " + WristSpeedThreshold + ")");

                            // 只有在角度变化小于阈值时才触发OutEvent
                            if (currentAngleChange <= angleChangeThreshold_new)
                            {
                                if (!B_triggered)//csr: Ensure only trigger once,or will triger many times
                                {
                                    //OutEvent.Invoke();
                                    B_triggered = true;
                                    // //print("b trigger - 角度条件满足");
                                    // //print(wristSpeed + " " + wristSpeedTriggersDisabledB + " " + C_EventEnabled + " " + B_triggered);
                                }
                                toTrigger = true;
                                // //UnityEngine.Debug.Log("trigger by speed crossing threshold in ball");
                            }
                            else
                            {
                                // //print("手腕速度超过阈值但角度变化过大，不触发OutEvent");
                                
                                // 如果速度加速度条件已满足，设置角度超限标志并调用OutMaterial
                                if (speedAcceConditionMet)
                                {
                                    OutMaterial();
                                    angleExceededInBall = true;
                                     //print("速度加速度条件已满足但角度变化过大，调用OutMaterial并设置angleExceededInBall标志");
                                }
                            }
                            //OutEvent.Invoke();
                            //B_EventEnabled = true;// B event enabled
                        }
                        //AfterOutEvent.Invoke(); this is wrong when // this line,everything goes right
                        // 2) Default trigger setting for wrist speed mode
                        // Set toTrigger = true as fallback if no other triggers fired and not disabled
                        if (!toTrigger && !Hesitated && !wristSpeedTriggersDisabledB) // Default fallback trigger when no other conditions trigger
                        {
                            toTrigger = true;
                             //UnityEngine.Debug.Log("WristSpeedMode: Default toTrigger = true (ready for exit trigger)");
                        }
                         //print("in ball" + wristSpeed + " " + wristSpeedTriggersDisabledB + " " + C_EventEnabled + " " + B_triggered);
                    }
                }
                // current_state = true;// csr: current state is true means in the ball
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
            // 注意：手指离开检测逻辑已移至函数开头统一处理
            #endregion

            // Update previous wrist speed (In every frame)
            wristSpeedPre = wristSpeed;
        }


    }

    public bool JudgeV_vec()
    {
        if (vpre != float.MaxValue && apre != float.MaxValue)
        {
            float velocityThreshold = GetCurrentVelocityThreshold();
            //??? V_vec_pre > V_down &&
            if (V_vec < velocityThreshold)
            {
                ac = a;
                vc = v;
                condition = '1';
                // if (Entered) // 只在球内时输出调试信息
                // {
                //     print($"速度条件满足 - V_vec: {V_vec:F4}, V_down阈值: {V_down:F4}");
                // }
                return true;
            }
            // else if (Entered) // 条件不满足时也输出调试信息
            // {
            //     print($"速度条件不满足 - V_vec: {V_vec:F4}, V_down阈值: {V_down:F4}");
            // }
        }
        return false;
    }

    private void TrySatisfySpeedAcceConditionOnEnter()
    {
        if (!enterSatisfiesSpeedAcceCondition || speedAcceConditionMet)
        {
            return;
        }

        speedAcceMetPosition = entryPosition;
        speedAcceConditionMet = true;
        BF_EventEnabled = true;
        initialAngle = currentAngle;
        initialAngleSet = true;
    }

    public bool JudgeCurrentV_vec()
    {
        return useAdaptiveRealtimeFingerSpeed ? JudgeAdaptiveRealtimeV_vec() : JudgeV_vec();
    }

    public bool JudgeAdaptiveRealtimeV_vec()
    {
        if (vpre != float.MaxValue && apre != float.MaxValue)
        {
            float velocityThreshold = GetAdaptiveRealtimeVelocityThreshold();
            if (V_vec < velocityThreshold)
            {
                ac = a;
                vc = v;
                condition = '1';
                return true;
            }
        }
        return false;
    }

    private float GetAdaptiveRealtimeVelocityThreshold()
    {
        if (fingerSpeedThershold <= 0f)
        {
            return adaptiveVDownMax;
        }

        if (V_vec > fingerSpeedThershold)
        {
            return adaptiveVDownMax;
        }

        float t = Mathf.Clamp01(V_vec / fingerSpeedThershold);
        return Mathf.Lerp(adaptiveVDownMin, adaptiveVDownMax, t);
    }

    private float GetCurrentVelocityThreshold()
    {
        if (!useAdaptiveEnterSpeed)
        {
            return V_down;
        }

        return adaptiveEnterSpeedAtEntry < adaptiveEnterSpeedThreshold
            ? adaptiveEnterSpeedK * V_down
            : V_down;
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

    public bool JudgeA_vec()
    {
        if (vpre != float.MaxValue && apre != float.MaxValue)
        {
            if (A_vec < A_)
            {
                ac = a;
                vc = v;
                condition = '2';
                if (Entered) // 只在球内时输出调试信息
                {
                    // //print($"加速度条件满足 - A_vec: {A_vec:F4}, A_阈值: {A_:F4}");
                }
                return true;
            }
            else if (Entered) // 条件不满足时也输出调试信息
            {
                // //print($"加速度条件不满足 - A_vec: {A_vec:F4}, A_阈值: {A_:F4}");
            }
        }
        return false;
    }
    public bool JudgeC()
    {
        if (useAcce)
        {
             //print("Using Acceleration for condition C");
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
             //print("Using Acceleration for condition C");
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
        //meshRenderer.material = OutMate;
    }

    public void InMaterial()
    {
        meshRenderer.material = InMate;
    }

    public float CalculateAngle_between_finger_wrist()
    {
        /// calculate angel between 'a-c' and 'd-c'
        Vector3 vector1 = IndexDistalPose.Position - IndexTipPose2.Position;//c-a(same as picture)
        Vector3 vector2 = IndexDistalPose.Position - WristPose.Position;//c-d(same as picture)
        return Vector3.Angle(vector1, vector2);// return angel between 'c-a' and 'c-d'
    }

    public bool GetIsCancel()
    {
        return isCanceled;
    }

    private void RecordWristPoseToCSV() // CWC: 记录当前WristPose位置到CSV
    {
        Vector3 wristPos = WristPose.Position;
        //UnityEngine.Debug.Log($"记录WristPose到CSV: X={wristPos.x:F4}, Y={wristPos.y:F4}, Z={wristPos.z:F4}");

        // 直接创建和写入CSV文件
        string date = System.DateTime.Now.ToString("yyyyMMdd-HH-mm-ss");
        string filePath = Application.persistentDataPath + "/Logs/" + date + "/";
        string fileName = date + "-WristPose-" + gameObject.name + ".csv";
        string fullPath = filePath + fileName;

        // 创建目录
        if (!System.IO.Directory.Exists(filePath))
        {
            System.IO.Directory.CreateDirectory(filePath);
        }

        // 检查是否需要创建文件和写入标题
        bool fileExists = System.IO.File.Exists(fullPath);
        string reacStatus = reacConditionFullyMet ? "SUCCESS" : "FAILED";

        try
        {
            using (var fs = new System.IO.FileStream(fullPath, System.IO.FileMode.Append, System.IO.FileAccess.Write))
            using (var sw = new System.IO.StreamWriter(fs))
            {
                // 如果是新文件，先写入标题行
                if (!fileExists)
                {
                    sw.WriteLine("Timestamp,BallName,WristX,WristY,WristZ,ReacConditionMet,AngleChange,InBallDistance");
                }

                // 写入数据
                string timestamp = Time.time.ToString("F4");
                string logLine = $"{timestamp},{gameObject.name},{wristPos.x:F6},{wristPos.y:F6},{wristPos.z:F6},{reacStatus},{currentAngleChange:F2},{currentInBallDistance:F4}";
                sw.WriteLine(logLine);
            }

            //UnityEngine.Debug.Log($"WristPose记录成功: {gameObject.name} - {reacStatus}");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"WristPose记录失败: {e.Message}");
        }
    }
}
