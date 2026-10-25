using Oculus.Platform.Samples.VrHoops;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(ReactTap))]
[RequireComponent(typeof(AcceStudy3_2))]
[RequireComponent(typeof(AcceStudy3))]
[RequireComponent(typeof(Pinch))]
public class S2Obstacles : MonoBehaviour
{
    public AcceStudy3 Acce;
    public AcceStudy3_2 Acce2;
    public ReactTap reactTap;
    public S2Player Player;
    public BallSpawner ballSpawner;
    public int modeSelected;
    private readonly DynamicStopMotionTracker dynamicStopMotion = new DynamicStopMotionTracker();
    private Collider dynamicStopCollider;
    private Material dynamicStopIdleMaterial;
    public DynamicStopMotionTracker DynamicStopMotion => dynamicStopMotion;
    private readonly IxDLDynamicMotionTracker ixdlDynamicMotion = new IxDLDynamicMotionTracker();
    public IxDLDynamicMotionTracker IxDLDynamicMotion => ixdlDynamicMotion;
    private bool UsesIxDLDynamic => modeSelected == (int)BallSpawner.Mode.ixdlDynamic;
    private bool UsesDynamicMotion => UsesIxDLDynamic || modeSelected == (int)BallSpawner.Mode.dynamicStop;
    private bool DynamicMotionInside => UsesIxDLDynamic ? ixdlDynamicMotion.IsInside : dynamicStopMotion.IsInside;
    private readonly MagicTap2MotionTracker magicTap2Motion = new MagicTap2MotionTracker();
    private Collider magicTap2Collider;
    private Material magicTap2IdleMaterial;
    public MagicTap2MotionTracker MagicTap2Motion => magicTap2Motion;
    private bool UsesMagicTap2 => modeSelected == (int)BallSpawner.Mode.magicTap2;
    float oldTime;
    public AudioSource source;
    public AudioClip clip;
    public bool inBall = false, inBall2 = false;
    public int ballIndex = 0;
    public int ballKind = 2;
    public bool triggered = false;
    public Pinch pinchob;
    public bool useIndex = false;
    public bool handInBall = false;
    public bool canceled = false;
    public bool handInBall2 = false;
    public LogWriter3 logWriter3;
    // Release Tap gesture detector attached to this obstacle ball.
    public ReleaseGesture releaseGesture;
    private bool pinchHandledThisGesture = false;
    private bool fastTapCancelTriggered = false;
    private bool fastTapReentryRequired = false;
    private bool fastTapCanceledUntilExit = false;
    private float fastTapEntryTimestamp = -1f;
    private float fastTapReactTimestamp = -1f;
    private bool reactReleaseSpeedAcceMet = false;
    private bool reactReleaseTriggered = false;
    private bool reactReleaseReentryRequired = false;
    private SpeedStudyTelemetry speedStudyTelemetry;
    private bool speedReleaseMinSpeedMet = false;
    private bool speedReleaseTriggered = false;
    private bool speedReleaseReentryRequired = false;
    [SerializeField, Min(0f)] private float fastTapCancelTime = 0.5f;
    private bool rainbowCancelTriggered = false;
    private bool rainbowConfirmBlocked = false;
    private bool rainbowReentryRequiresExit = false;
    private bool rainbowArcActive = false;
    private bool rainbowArcInitialized = false;
    private GameObject rainbowArcRoot;
    private Vector3 rainbowEntryPosition;
    private bool rainbowEntryCaptured = false;
    private Vector3 rainbowArcCenter;
    private Vector3 rainbowArcNormal;
    private Vector3 rainbowPrevFingerPosition;
    private Vector3 rainbowFrameTangent;
    private Vector3 rainbowFrameBitangent;
    private float rainbowFrameHalfExtent;
    private float rainbowArcRadius;
    private float rainbowArcThickness = 0.002f;
    private float rainbowFrameDepth = 0.0015f;
    private float rainbowArcHalfAngle = 55f;
    private const int RainbowCapRadialSegments = 8;
    private const int RainbowCapAngularSegments = 32;
    private bool dwellReleaseCancelTriggered = false;
    private bool dwellReleaseReentryRequired = false;
    private bool dwellReleaseInside = false;
    private float dwellReleaseEntryTimestamp = -1f;
    private bool dwellWithdrawCancelTriggered = false;
    private bool dwellWithdrawReentryRequired = false;
    private bool dwellWithdrawInside = false;
    private float dwellWithdrawEntryTimestamp = -1f;
    private bool pinchReleaseTriggerArmed = false;
    private bool pinchReleaseTriggerCompleted = false;
    private bool pinchThumbSwipeTracking = false;
    private bool pinchThumbSwipeCanceled = false;
    private bool pinchThumbSwipeReentryRequired = false;
    private Vector3 pinchThumbSwipeStartThumbPosition = Vector3.zero;
    private float pinchThumbSwipeStartProjection = 0f;
    private float pinchThumbSwipeCurrentContactDistance = float.MaxValue;
    private bool pinchWristTiltInside = false;
    private bool pinchWristTiltTriggered = false;
    private bool pinchWristTiltCanceled = false;
    private bool pinchWristTiltReentryRequired = false;
    private bool pinchWristTiltPinching = false;
    private float pinchWristTiltEntryTimestamp = -1f;
    private Quaternion pinchWristTiltEntryRotation = Quaternion.identity;
    private bool pinchWristTiltHasEntryRotation = false;
    private Vector3 dwellWithdrawEntryVelocityDirection = Vector3.zero;
    private Vector3 dwellWithdrawCurrentVelocityDirection = Vector3.zero;
    private readonly Queue<Vector3> dwellWithdrawPositionHistory = new Queue<Vector3>(3);
    private bool direction010CancelTriggered = false;
    private bool direction010ReentryRequired = false;
    private readonly Queue<Vector3> direction010PositionHistory = new Queue<Vector3>();
    private bool direction010HasSmoothedDirection = false;
    private Vector3 direction010SmoothedDirection = Vector3.zero;
    private bool swing010CancelTriggered = false;
    private bool swing010HasEntryDirection = false;
    private Vector3 swing010EntryDirection = Vector3.zero;
    private bool pinchDirectionCancelTriggered = false;
    private bool pinchDirectionReentryRequired = false;
    private readonly Queue<Vector3> pinchDirectionCenterPositionHistory = new Queue<Vector3>();
    private bool pinchDirectionHasSmoothedDirection = false;
    private Vector3 pinchDirectionSmoothedDirection = Vector3.zero;
    private bool directionCancelLowSpeedMet = false;
    private bool directionCancelEntryAxisCaptured = false;
    private Vector3 directionCancelEntryAxis = Vector3.zero;
    private bool direction010HasAcceptedPosition = false;
    private Vector3 direction010LastAcceptedPosition = Vector3.zero;
    private bool pinchDirectionHasRawPosition = false;
    private Vector3 pinchDirectionPreviousRawPosition = Vector3.zero;
    private bool pinchDirectionHasAcceptedPosition = false;
    private Vector3 pinchDirectionLastAcceptedPosition = Vector3.zero;
    private DirectionCDArrowVisual directionCDArrowVisual;
    public void Start()
    {
        Acce = GetComponent<AcceStudy3>();
        Acce2 = GetComponent<AcceStudy3_2>();
        reactTap = GetComponent<ReactTap>();
        Player = GetComponentInChildren<S2Player>();
        ballSpawner = GameObject.Find("Spawner").GetComponent<BallSpawner>();
        logWriter3 = GameObject.Find("Spawner").GetComponent<LogWriter3>();
        modeSelected = (int)ballSpawner.modeSelected;
        pinchob = GetComponent<Pinch>();
        ApplyPinchSettings();
        EnsureReleaseGesture();
        switch (modeSelected)
        {
            case 0:
                SetFastMode();
                break;
            case 1:
                Acce.Delay = true;
                Acce.WaitTime = Mathf.Max(0f, ballSpawner.longPressTimeThreshold);
                SetHesMode();
                break;
            case 2:
                Acce.Acce = true;
                SetHesMode();
                break;
            case 3:
                SetReactTapMode();
                break;
            case (int)BallSpawner.Mode.magicTap2:
                SetMagicTap2Mode();
                break;
            case 14:
                SetReactReleaseMode();
                break;
            case 15:
                SetSpeedReleaseMode();
                break;
            case 26:
                SetSpeedDirectionMode();
                break;
            case 21:
                SetSpeedMode();
                break;
            case 22:
                SetSpeedDwellMode();
                break;
            case 20:
                SetSpeedWristTiltMode();
                break;
            case 4:
                SetPinchMode();
                break;
            case 5:
                SetFastTapMode();
                break;
            case 29:
                SetSwing010Mode();
                break;
            case 6:
                SetRainbowMode();
                break;
            case 7:
                SetReleaseTapMode();
                break;
            case 8:
                SetPinchMode();
                break;
            case 9:
                SetPinchMode();
                break;
            case 10:
                SetPressMode();
                break;
            case 11:
                Acce.Delay = true;
                Acce.WaitTime = Mathf.Max(0f, ballSpawner.longPressTimeThreshold);
                SetDwellReleaseMode();
                break;
            case 28:
                Acce.Delay = true;
                Acce.WaitTime = Mathf.Max(0f, ballSpawner.longPressTimeThreshold);
                SetDwellDirectionMode();
                break;
            case 12:
                Acce.Delay = true;
                Acce.WaitTime = Mathf.Max(0f, ballSpawner.longPressTimeThreshold);
                SetDwellWithdrawMode();
                break;
            case 13:
                SetPinchReleaseTriggerMode();
                break;
            case 27:
                SetPinchDirectionMode();
                break;
            case 16:
                SetPinchThumbSwipeMode();
                break;
            case 17:
                SetPinchWristTiltMode();
                break;
            case 18:
                SetWristTiltMode();
                break;
            case 19:
                SetDwellWristTiltMode();
                break;
            case 23:
                SetContact010Mode();
                break;
            case 24:
                SetDirection010Mode();
                break;
            case 25:
                SetDirection010Mode();
                break;
            case (int)BallSpawner.Mode.dynamicStop:
            case (int)BallSpawner.Mode.ixdlDynamic:
                SetDynamicStopMode();
                break;


        }
        ApplyDirectionCDArrowVisibility();
        source = GetComponent<AudioSource>();
        source.clip = clip;
        ballIndex = ballSpawner.obstacleObjects.IndexOf(gameObject);
        //SetFastMode();
        //SetHesMode();
    }
    public void SetPinchMode()
    {
        pinchob.PinchEnd.AddListener(() =>
        {
            pinchHandledThisGesture = false;
        });

        if (ballSpawner != null)
        {
            

            //Acce.InEvent.AddListener();
            Acce.InEvent.AddListener(() => {

                if (triggered)
                {
                    print("触发目标球后还没离开");
                    ballSpawner.area_type = 4;
                    ballSpawner.index = -1;
                }
                if (inBall == false && !triggered && !pinchob.isPinch)
                {
                    Player.TurnShallow();
                    inBall = true;
                    print("在障碍球中");
                    ballSpawner.area_type = 2;
                    ballSpawner.index = ballIndex;
                    ballSpawner.errorInTimes++;
                }

                else if (inBall && !triggered && !pinchob.isPinch)
                {
                    ballSpawner.area_type = 2;
                    ballSpawner.index = ballIndex;
                }


            });
            //Acce.HesEvent.AddListener(Player.);
            //Acce.AfterOutEvent.AddListener(Player.TurnShallowBack);
            //Acce.InEvent.AddListener(ballSpawner.changeOpacity);
            //Acce.OutEvent.AddListener(ballSpawner.getNewTarget);
            Acce.AfterOutEvent.AddListener(() =>
            {
                print("离开障碍球");
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;

                //triggered = false;
                oldTime = Time.time;
                inBall = false;
                if (!triggered)
                {
                    Player.TurnYellowBack();
                }
                useIndex = false;
                ballSpawner.triggered = false;
            });

            pinchob.PinchStart.AddListener(() =>
            {
                if (pinchHandledThisGesture)
                {
                    return;
                }

                if (inBall) //手在球里
                {
                    if (!triggered)
                    {
                        pinchHandledThisGesture = true;
                        useIndex = true;
                        Player.TurnYellow();
                        source.Play();
                        print("在障碍球触发");
                        ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
                    }
                }

            });
        }

        if (ballSpawner != null)
        {


            //Acce.InEvent.AddListener();
            Acce2.InEvent.AddListener(() => {

                if (triggered)
                {
                    print("触发目标球后还没离开");
                    ballSpawner.area_type = 4;
                    ballSpawner.index = -1;
                }
                if (inBall2 == false && !triggered && !pinchob.isPinch)
                {
                    Player.TurnShallow();
                    inBall2 = true;
                    print("在障碍球中");
                    ballSpawner.area_type = 2;
                    ballSpawner.index = ballIndex;
                    ballSpawner.errorInTimes++;
                }

                else if (inBall2 && !triggered && !pinchob.isPinch)
                {
                    ballSpawner.area_type = 2;
                    ballSpawner.index = ballIndex;
                }


            });
            //Acce.HesEvent.AddListener(Player.);
            //Acce.AfterOutEvent.AddListener(Player.TurnShallowBack);
            //Acce.InEvent.AddListener(ballSpawner.changeOpacity);
            //Acce.OutEvent.AddListener(ballSpawner.getNewTarget);
            Acce2.AfterOutEvent.AddListener(() =>
            {
                print("离开障碍球");
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;

                //triggered = false;
                oldTime = Time.time;
                inBall2 = false;
                if (!triggered)
                {
                    Player.TurnYellowBack();
                }
                ballSpawner.triggeredBy = "index";
                ballSpawner.triggered = false;
            });

            pinchob.PinchStart.AddListener(() =>
            {
                if (pinchHandledThisGesture)
                {
                    return;
                }

                if (inBall2) //手在球里
                {
                    if (!triggered && !useIndex)
                    {
                        pinchHandledThisGesture = true;
                        ballSpawner.triggeredBy = "thumb";
                        Player.TurnYellow();
                        source.Play();
                        print("在障碍球触发");
                        ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
                    }
                }

            });
        }
    }
    
    public void SetMode(int modeSelected)
    {
        this.modeSelected = modeSelected;
        if (Acce == null || reactTap == null || pinchob == null) return;
        ApplyPinchSettings();
        switch (modeSelected)
        {
            case 0:
                SetFastMode();
                break;
            case 1:
                Acce.Delay = true;
                Acce.WaitTime = Mathf.Max(0f, ballSpawner.longPressTimeThreshold);
                SetHesMode();
                break;
            case 2:
                Acce.Acce = true;
                SetHesMode();
                break;
            case 3:
                SetReactTapMode();
                break;
            case (int)BallSpawner.Mode.magicTap2:
                SetMagicTap2Mode();
                break;
            case 14:
                SetReactReleaseMode();
                break;
            case 15:
                SetSpeedReleaseMode();
                break;
            case 26:
                SetSpeedDirectionMode();
                break;
            case 21:
                SetSpeedMode();
                break;
            case 22:
                SetSpeedDwellMode();
                break;
            case 20:
                SetSpeedWristTiltMode();
                break;
            case 4:
                SetPinchMode();
                break;
            case 5:
                SetFastTapMode();
                break;
            case 29:
                SetSwing010Mode();
                break;
            case 6:
                SetRainbowMode();
                break;
            case 7:
                SetReleaseTapMode();
                break;
            case 8:
                SetPinchMode();
                break;
            case 9:
                SetPinchMode();
                break;
            case 10:
                SetPressMode();
                break;
            case 11:
                Acce.Delay = true;
                Acce.WaitTime = Mathf.Max(0f, ballSpawner.longPressTimeThreshold);
                SetDwellReleaseMode();
                break;
            case 28:
                Acce.Delay = true;
                Acce.WaitTime = Mathf.Max(0f, ballSpawner.longPressTimeThreshold);
                SetDwellDirectionMode();
                break;
            case 12:
                Acce.Delay = true;
                Acce.WaitTime = Mathf.Max(0f, ballSpawner.longPressTimeThreshold);
                SetDwellWithdrawMode();
                break;
            case 13:
                SetPinchReleaseTriggerMode();
                break;
            case 27:
                SetPinchDirectionMode();
                break;
            case 16:
                SetPinchThumbSwipeMode();
                break;
            case 17:
                SetPinchWristTiltMode();
                break;
            case 18:
                SetWristTiltMode();
                break;
            case 19:
                SetDwellWristTiltMode();
                break;
            case 23:
                SetContact010Mode();
                break;
            case 24:
                SetDirection010Mode();
                break;
            case 25:
                SetDirection010Mode();
                break;
            case (int)BallSpawner.Mode.dynamicStop:
            case (int)BallSpawner.Mode.ixdlDynamic:
                SetDynamicStopMode();
                break;
        }

        ApplyDirectionCDArrowVisibility();
    }
    public void ClearMode()
    {
        magicTap2Motion.Reset();
        dynamicStopMotion.Reset();
        ixdlDynamicMotion.Reset();
        ResetReactReleaseState();
        ResetSpeedReleaseState();
        ResetRainbowState();
        ResetDwellReleaseState();
        ResetDwellWithdrawState();
        ResetPinchReleaseTriggerState();
        ResetPinchThumbSwipeState();
        ResetPinchWristTiltState();
        ResetContact010ObstacleState();
        ResetDirection010ObstacleState();
        ResetSwing010State();
        ResetPinchDirectionObstacleCancelState();
        Acce?.InEvent.RemoveAllListeners();
        Acce?.BeforeHesEvent.RemoveAllListeners();
        Acce?.HesEvent.RemoveAllListeners();
        Acce?.AfterOutEvent.RemoveAllListeners();
        Acce2?.InEvent.RemoveAllListeners();
        Acce2?.AfterOutEvent.RemoveAllListeners();

        reactTap?.InEvent.RemoveAllListeners();
        reactTap?.ChangeEvent.RemoveAllListeners();
        reactTap?.CancelEvent.RemoveAllListeners();
        reactTap?.OutEvent.RemoveAllListeners();
        reactTap?.AfterOutEvent.RemoveAllListeners();
        pinchob?.PinchStart.RemoveAllListeners();
        pinchob?.PinchEnd.RemoveAllListeners();
        HideDirectionCDArrow();
    }
    public void SetReactTapMode()
    {
        //print("进入反应式点击模式");
        Acce.InEvent.AddListener(() =>
        {
            if (!handInBall2)
            {
                ballSpawner.errorInTimes++;
                Player.TurnShallow();
                handInBall2 = true;
            }

            if (!triggered)
            {
                //Player.TurnShallow();
                ballSpawner.area_type = 2;
                ballSpawner.index = ballIndex;
                print("在障碍球中");
            }
            else if (triggered)
            {
                print("触发目标球后还没离开");
                ballSpawner.area_type = 4;
                ballSpawner.index = ballIndex;
            }
        });
        Acce.AfterOutEvent.AddListener(() =>
        {
            handInBall2 = false;

        });

        reactTap.InEvent.AddListener(() =>
        {
            if (!handInBall2)
            {
                ballSpawner.errorInTimes++;
                Player.TurnShallow();
                handInBall2 = true;
                if (!triggered)
                {
                    ballSpawner.area_type = 2;
                    ballSpawner.index = ballIndex;
                }
            }

            //print("in触发");
            if (!handInBall)
            {
                ballSpawner.isReact = true;
                logWriter3.obstacleReactFrequency++;
                // 记录初始夹角
                reactTap.initialAngle = reactTap.CalculateAngle_between_finger_wrist(); // 假设 CalculateAngle_between_finger_wrist 方法用于计算夹角
                Debug.Log("initial angle:" + reactTap.initialAngle);
                Player.showCircle();
            }

            handInBall = true;
        });

        reactTap.HesEvent.AddListener(() =>
        {
            canceled = true;
            Player.hideCircle();
            Debug.Log("Hes!");
        });

        reactTap.AfterOutEvent.AddListener(() =>
        {
            //Player2.TurnShallowBack();
            canceled = false;
            handInBall = false;
            if (!triggered)
            {

                oldTime = Time.time;
                Player.TurnShallowBack();

            }
            //if (!ballSpawner.triggered)
            //{
            print("离开障碍球");
            ballSpawner.area_type = 3;
            ballSpawner.index = -1;
            //triggered = false;
            //}
            Player.hideCircle();

            ballSpawner.triggered = false;
        });

        reactTap.OutEvent.AddListener(() =>
        {
            print("out触发");
            if (!reactTap.GetIsCancel() && !canceled)
            {
                if (!triggered)
                {
                    Player.TurnYellow();
                    source.Play();
                    //！！！是否应该放在hesevent，障碍球触发后应该变
                    print("触发障碍球后还没离开");
                    ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
                }
            }
        });
    }

    public void SetFastTapMode()
    {
        Acce.InEvent.AddListener(() =>
        {
            if (!handInBall2)
            {
                fastTapReentryRequired = false;
                fastTapCanceledUntilExit = false;
                canceled = false;
                fastTapCancelTriggered = false;
                ResetFastTapTiming();
                ballSpawner.errorInTimes++;
                Player.TurnShallow();
                handInBall2 = true;
                fastTapEntryTimestamp = Time.time;
                fastTapReactTimestamp = -1f;
            }

            if (!triggered)
            {
                ballSpawner.area_type = 2;
                ballSpawner.index = ballIndex;
            }
            else
            {
                ballSpawner.area_type = 4;
                ballSpawner.index = ballIndex;
            }

            if (!rainbowEntryCaptured)
            {
                rainbowEntryPosition = Acce.HasSurfaceEntryPoint ? Acce.SurfaceEntryPoint : Acce.IndexTipPose2.Position;
                rainbowEntryCaptured = true;
            }
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            handInBall2 = false;
            if (!handInBall)
            {
                fastTapReentryRequired = false;
                fastTapCanceledUntilExit = false;
                ResetFastTapTiming();
            }
        });

        reactTap.InEvent.AddListener(() =>
        {
            if (fastTapReentryRequired || fastTapCanceledUntilExit)
            {
                return;
            }

            if (!handInBall2)
            {
                ballSpawner.errorInTimes++;
                Player.TurnShallow();
                handInBall2 = true;
                fastTapEntryTimestamp = Time.time;
                fastTapReactTimestamp = -1f;
                if (!triggered)
                {
                    ballSpawner.area_type = 2;
                    ballSpawner.index = ballIndex;
                }
                else
                {
                    ballSpawner.area_type = 4;
                    ballSpawner.index = ballIndex;
                }
            }

            if (!handInBall)
            {
                ballSpawner.isReact = true;
                logWriter3.obstacleReactFrequency++;
                reactTap.initialAngle = reactTap.CalculateAngle_between_finger_wrist();
                Player.showCircle();
                fastTapReactTimestamp = Time.time;
            }

            handInBall = true;
            canceled = false;
            fastTapCancelTriggered = false;
        });

        if (modeSelected != (int)BallSpawner.Mode.swing010)
        {
            reactTap.CancelEvent.AddListener(TriggerFastTapCancel);
        }

        reactTap.AfterOutEvent.AddListener(() =>
        {
            if (modeSelected != (int)BallSpawner.Mode.swing010
                && handInBall
                && !fastTapCancelTriggered
                && !canceled
                && HasFastTapTimedOut())
            {
                TriggerFastTapCancel();
            }

            bool shouldConfirm = handInBall
                && !fastTapCancelTriggered
                && !canceled
                && !fastTapReentryRequired
                && !fastTapCanceledUntilExit;

            handInBall = false;
            canceled = false;
            fastTapCancelTriggered = false;
            Player.hideCircle();

            if (!triggered)
            {
                oldTime = Time.time;
                Player.TurnShallowBack();
            }

            ballSpawner.area_type = 3;
            ballSpawner.index = -1;
            ballSpawner.triggered = false;
            ballSpawner.isReact = false;

            if (!shouldConfirm || triggered)
            {
                ResetFastTapTiming();
                return;
            }

            Player.TurnYellow();
            source.Play();
            ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
            ResetFastTapTiming();
        });
    }

    public void SetSwing010Mode()
    {
        ResetSwing010State();
        SetFastTapMode();

        reactTap.InEvent.AddListener(() =>
        {
            CaptureSwing010EntryDirectionIfNeeded();
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            if (!handInBall)
            {
                ResetSwing010State();
            }
        });

        reactTap.AfterOutEvent.AddListener(() =>
        {
            ResetSwing010State();
        });
    }

    public void SetReleaseTapMode()
    {
        // Obstacle Release Tap mirrors target-ball behavior:
        // ReactTap entry starts the reacted state, release cancels, and leaving
        // the obstacle without cancellation registers the obstacle trigger.
        EnsureReleaseGesture();

        Acce.InEvent.AddListener(() =>
        {
            if (!handInBall2)
            {
                fastTapReentryRequired = false;
                fastTapCanceledUntilExit = false;
                canceled = false;
                fastTapCancelTriggered = false;
                ResetFastTapTiming();
                ResetReleaseGestureTracking();
                ballSpawner.errorInTimes++;
                Player.TurnShallow();
                handInBall2 = true;
                fastTapEntryTimestamp = Time.time;
                fastTapReactTimestamp = -1f;
            }

            if (!triggered)
            {
                ballSpawner.area_type = 2;
                ballSpawner.index = ballIndex;
            }
            else
            {
                ballSpawner.area_type = 4;
                ballSpawner.index = ballIndex;
            }
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            handInBall2 = false;
            if (!handInBall)
            {
                fastTapReentryRequired = false;
                fastTapCanceledUntilExit = false;
                ResetFastTapTiming();
                ResetReleaseGestureTracking();
            }
        });

        reactTap.InEvent.AddListener(() =>
        {
            if (fastTapReentryRequired || fastTapCanceledUntilExit)
            {
                return;
            }

            bool enteredReactTap = !handInBall;

            if (!handInBall2)
            {
                ballSpawner.errorInTimes++;
                Player.TurnShallow();
                handInBall2 = true;
                fastTapEntryTimestamp = Time.time;
                fastTapReactTimestamp = -1f;
                if (!triggered)
                {
                    ballSpawner.area_type = 2;
                    ballSpawner.index = ballIndex;
                }
                else
                {
                    ballSpawner.area_type = 4;
                    ballSpawner.index = ballIndex;
                }
            }

            if (!handInBall)
            {
                // React stage is still driven by ReactTap's angle/movement checks.
                ballSpawner.isReact = true;
                logWriter3.obstacleReactFrequency++;
                reactTap.initialAngle = reactTap.CalculateAngle_between_finger_wrist();
                Player.showCircle();
                fastTapReactTimestamp = Time.time;
            }

            handInBall = true;
            canceled = false;
            fastTapCancelTriggered = false;

            if (enteredReactTap)
            {
                ResetReleaseGestureTracking();
            }
        });

        reactTap.AfterOutEvent.AddListener(() =>
        {
            // Leaving the obstacle confirms only if no release-cancel or other cancel happened.
            bool shouldConfirm = handInBall
                && !fastTapCancelTriggered
                && !canceled
                && !fastTapReentryRequired
                && !fastTapCanceledUntilExit;

            handInBall = false;
            canceled = false;
            fastTapCancelTriggered = false;
            Player.hideCircle();
            ResetReleaseGestureTracking();

            if (!triggered)
            {
                oldTime = Time.time;
                Player.TurnShallowBack();
            }

            ballSpawner.area_type = 3;
            ballSpawner.index = -1;
            ballSpawner.triggered = false;
            ballSpawner.isReact = false;

            if (!shouldConfirm || triggered)
            {
                ResetFastTapTiming();
                return;
            }

            Player.TurnYellow();
            source.Play();
            ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
            ResetFastTapTiming();
        });
    }

    public void SetReactReleaseMode()
    {
        EnsureReleaseGesture();
        ResetReactReleaseState();

        Acce.InEvent.AddListener(() =>
        {
            if (reactReleaseReentryRequired)
            {
                return;
            }

            if (!handInBall2)
            {
                ballSpawner.errorInTimes++;
                Player.TurnShallow();
                handInBall2 = true;
                fastTapEntryTimestamp = Time.time;
                fastTapReactTimestamp = -1f;
                ResetReleaseGestureTracking();
            }

            if (!triggered)
            {
                ballSpawner.area_type = 2;
                ballSpawner.index = ballIndex;
            }
            else
            {
                ballSpawner.area_type = 4;
                ballSpawner.index = ballIndex;
            }
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            handInBall2 = false;
            if (!handInBall)
            {
                reactReleaseReentryRequired = false;
                ResetReactReleaseState();
                ResetReleaseGestureTracking();
                ResetFastTapTiming();
                if (!triggered)
                {
                    oldTime = Time.time;
                    Player.TurnShallowBack();
                    ballSpawner.area_type = 3;
                    ballSpawner.index = -1;
                }
            }
        });

        reactTap.InEvent.AddListener(() =>
        {
            if (reactReleaseReentryRequired || reactReleaseTriggered)
            {
                return;
            }

            if (!handInBall2)
            {
                ballSpawner.errorInTimes++;
                Player.TurnShallow();
                handInBall2 = true;
                fastTapEntryTimestamp = Time.time;
                fastTapReactTimestamp = -1f;
            }

            if (!reactReleaseSpeedAcceMet)
            {
                ballSpawner.isReact = true;
                logWriter3.obstacleReactFrequency++;
                reactTap.initialAngle = reactTap.CalculateAngle_between_finger_wrist();
                Player.showCircle();
                fastTapReactTimestamp = Time.time;
                ResetReleaseGestureTracking();
            }

            reactReleaseSpeedAcceMet = true;
            handInBall = true;
            canceled = false;

            if (!triggered)
            {
                ballSpawner.area_type = 2;
                ballSpawner.index = ballIndex;
            }
            else
            {
                ballSpawner.area_type = 4;
                ballSpawner.index = ballIndex;
            }

            CheckReactReleaseIndexTrigger();
        });

        reactTap.AfterOutEvent.AddListener(() =>
        {
            handInBall = false;
            canceled = false;
            Player.hideCircle();
            ResetReleaseGestureTracking();

            if (!triggered)
            {
                oldTime = Time.time;
                Player.TurnShallowBack();
            }

            ballSpawner.area_type = 3;
            ballSpawner.index = -1;
            ballSpawner.triggered = false;
            ballSpawner.isReact = false;

            if (!reactReleaseTriggered)
            {
                ResetFastTapTiming();
            }
        });
    }

    public void SetSpeedReleaseMode()
    {
        EnsureReleaseGesture();
        ResetSpeedReleaseState();

        Acce.InEvent.AddListener(() =>
        {
            if (speedReleaseReentryRequired)
            {
                return;
            }

            if (!handInBall)
            {
                ballSpawner.errorInTimes++;
                Player.TurnShallow();
                handInBall = true;
                handInBall2 = true;
                inBall = true;
                canceled = false;
                speedReleaseMinSpeedMet = false;
                fastTapEntryTimestamp = Time.time;
                ResetReleaseGestureTracking();
            }

            LogSpeedStudy("sample");

            if (!triggered)
            {
                ballSpawner.area_type = 2;
                ballSpawner.index = ballIndex;
            }
            else
            {
                ballSpawner.area_type = 4;
                ballSpawner.index = ballIndex;
            }

            CheckSpeedReleaseIndexTrigger();
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            LogSpeedStudy("exit");
            handInBall = false;
            handInBall2 = false;

            if (!triggered)
            {
                oldTime = Time.time;
                Player.TurnShallowBack();
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
            }

            ResetSpeedReleaseState();
            ResetReleaseGestureTracking();
            ResetFastTapTiming();
        });
    }

    public void SetSpeedMode()
    {
        ResetSpeedReleaseState();

        Acce.InEvent.AddListener(() =>
        {
            if (speedReleaseReentryRequired)
            {
                return;
            }

            if (!handInBall)
            {
                ballSpawner.errorInTimes++;
                Player.TurnShallow();
                handInBall = true;
                handInBall2 = true;
                inBall = true;
                canceled = false;
                speedReleaseMinSpeedMet = false;
                fastTapEntryTimestamp = Time.time;
            }

            LogSpeedStudy("sample");

            if (!triggered)
            {
                ballSpawner.area_type = 2;
                ballSpawner.index = ballIndex;
            }
            else
            {
                ballSpawner.area_type = 4;
                ballSpawner.index = ballIndex;
            }

            CheckSpeedReleaseIndexTrigger();
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            LogSpeedStudy("exit");
            if (handInBall && !speedReleaseTriggered)
            {
                TriggerSpeedObstacleCancel();
            }

            if (!triggered)
            {
                oldTime = Time.time;
                Player.TurnShallowBack();
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
            }

            ResetSpeedReleaseState();
            ResetFastTapTiming();
        });
    }

    public void SetSpeedDirectionMode()
    {
        ResetSpeedReleaseState();
        ResetDirection010ObstacleState();

        Acce.InEvent.AddListener(() =>
        {
            if (speedReleaseReentryRequired || direction010ReentryRequired)
            {
                return;
            }

            if (!handInBall)
            {
                ballSpawner.errorInTimes++;
                Player.TurnShallow();
                handInBall = true;
                handInBall2 = true;
                inBall = true;
                canceled = false;
                speedReleaseMinSpeedMet = false;
                direction010CancelTriggered = false;
                direction010PositionHistory.Clear();
                direction010HasSmoothedDirection = false;
                direction010SmoothedDirection = Vector3.zero;
                fastTapEntryTimestamp = Time.time;
            }

            CaptureDirectionCancelEntryAxisIfNeeded();

            LogSpeedStudy("sample");

            if (!triggered)
            {
                ballSpawner.area_type = 2;
                ballSpawner.index = ballIndex;
            }
            else
            {
                ballSpawner.area_type = 4;
                ballSpawner.index = ballIndex;
            }

            bool directionDecisionReady = false;
            bool shouldCancelForDirection = speedReleaseMinSpeedMet
                && !speedReleaseTriggered
                && !speedReleaseReentryRequired
                && IsDirection010CancelMovement(out directionDecisionReady);

            if (shouldCancelForDirection)
            {
                TriggerDirection010ObstacleCancel();
                return;
            }

            // Keep target and obstacle behavior consistent: movement in the
            // cancellation direction cannot produce a high-speed trigger.
            CheckSpeedReleaseIndexTrigger(directionDecisionReady);
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            LogSpeedStudy("exit");
            if (!triggered)
            {
                oldTime = Time.time;
                Player.TurnShallowBack();
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
            }

            ResetSpeedReleaseState();
            ResetDirection010ObstacleState();
            ResetFastTapTiming();
        });
    }

    public void SetSpeedDwellMode()
    {
        ResetSpeedReleaseState();

        Acce.InEvent.AddListener(() =>
        {
            if (speedReleaseReentryRequired)
            {
                return;
            }

            if (!handInBall)
            {
                ballSpawner.errorInTimes++;
                Player.TurnShallow();
                handInBall = true;
                handInBall2 = true;
                inBall = true;
                canceled = false;
                speedReleaseMinSpeedMet = false;
                fastTapEntryTimestamp = Time.time;
            }

            LogSpeedStudy("sample");

            if (!triggered)
            {
                ballSpawner.area_type = 2;
                ballSpawner.index = ballIndex;
            }
            else
            {
                ballSpawner.area_type = 4;
                ballSpawner.index = ballIndex;
            }

            CheckSpeedReleaseIndexTrigger();
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            LogSpeedStudy("exit");
            if (!triggered)
            {
                oldTime = Time.time;
                Player.TurnShallowBack();
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
            }

            ResetSpeedReleaseState();
            ResetFastTapTiming();
        });
    }

    public void SetSpeedWristTiltMode()
    {
        ResetSpeedReleaseState();
        ResetPinchWristTiltState();

        Acce.InEvent.AddListener(() =>
        {
            if (speedReleaseReentryRequired)
            {
                return;
            }

            if (!handInBall)
            {
                ballSpawner.errorInTimes++;
                Player.TurnShallow();
                handInBall = true;
                handInBall2 = true;
                inBall = true;
                pinchWristTiltInside = true;
                pinchWristTiltPinching = true;
                pinchWristTiltEntryTimestamp = Time.time;
                pinchWristTiltHasEntryRotation = TryGetWristRotation(out pinchWristTiltEntryRotation);
                canceled = false;
                speedReleaseMinSpeedMet = false;
                fastTapEntryTimestamp = Time.time;
            }

            LogSpeedStudy("sample");

            if (!triggered)
            {
                ballSpawner.area_type = 2;
                ballSpawner.index = ballIndex;
            }
            else
            {
                ballSpawner.area_type = 4;
                ballSpawner.index = ballIndex;
            }

            CheckSpeedWristTiltCancel();
            CheckSpeedReleaseIndexTrigger();
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            LogSpeedStudy("exit");
            handInBall = false;
            handInBall2 = false;
            pinchWristTiltInside = false;

            if (!triggered)
            {
                oldTime = Time.time;
                Player.TurnShallowBack();
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
            }

            ResetSpeedReleaseState();
            ResetPinchWristTiltState();
            ResetFastTapTiming();
        });
    }

    public void SetRainbowMode()
    {
        ResetRainbowState();

        Acce.InEvent.AddListener(() =>
        {
            if (rainbowReentryRequiresExit)
            {
                return;
            }

            if (!handInBall2)
            {
                ballSpawner.errorInTimes++;
                Player.TurnShallow();
                handInBall2 = true;
            }

            if (!triggered)
            {
                ballSpawner.area_type = 2;
                ballSpawner.index = ballIndex;
            }
            else
            {
                ballSpawner.area_type = 4;
                ballSpawner.index = ballIndex;
            }

            if (!rainbowEntryCaptured)
            {
                rainbowEntryPosition = Acce.HasSurfaceEntryPoint ? Acce.SurfaceEntryPoint : Acce.IndexTipPose2.Position;
                rainbowEntryCaptured = true;
            }
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            handInBall2 = false;
            if (!handInBall)
            {
                ballSpawner.isReact = false;
            }

            rainbowReentryRequiresExit = false;
        });

        reactTap.InEvent.AddListener(() =>
        {
            if (rainbowReentryRequiresExit)
            {
                return;
            }

            if (!handInBall2)
            {
                ballSpawner.errorInTimes++;
                Player.TurnShallow();
                handInBall2 = true;
                if (!triggered)
                {
                    ballSpawner.area_type = 2;
                    ballSpawner.index = ballIndex;
                }
                else
                {
                    ballSpawner.area_type = 4;
                    ballSpawner.index = ballIndex;
                }
            }

            if (!handInBall)
            {
                ballSpawner.isReact = true;
                logWriter3.obstacleReactFrequency++;
                reactTap.initialAngle = reactTap.CalculateAngle_between_finger_wrist();
                Player.showCircle();
                if (!rainbowArcInitialized)
                {
                    ShowRainbowArc();
                }
            }

            handInBall = true;
            canceled = false;
            rainbowCancelTriggered = false;
            rainbowConfirmBlocked = false;
        });

        reactTap.AfterOutEvent.AddListener(() =>
        {
            if (handInBall && rainbowArcActive && !rainbowCancelTriggered && !rainbowConfirmBlocked)
            {
                CheckRainbowCancel();
            }

            bool shouldConfirm = handInBall && !rainbowCancelTriggered && !canceled && !rainbowConfirmBlocked;

            handInBall = false;
            canceled = false;
            rainbowCancelTriggered = false;
            rainbowConfirmBlocked = false;
            rainbowArcInitialized = false;
            rainbowEntryCaptured = false;
            Player.hideCircle();
            HideRainbowArc();

            if (!triggered)
            {
                oldTime = Time.time;
                Player.TurnShallowBack();
            }

            ballSpawner.area_type = 3;
            ballSpawner.index = -1;
            ballSpawner.triggered = false;
            ballSpawner.isReact = false;

            if (!shouldConfirm || triggered)
            {
                return;
            }

            Player.TurnYellow();
            source.Play();
            ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
        });
    }

    public void SetMagicTap2Mode()
    {
        magicTap2Collider = GetComponent<Collider>();
        magicTap2Motion.Reset();
        handInBall = handInBall2 = inBall = false;
    }

    private void UpdateMagicTap2Obstacle()
    {
        if (ballSpawner == null || Acce == null || !Acce.enabled)
        {
            EndMagicTap2ObstacleContact();
            magicTap2Motion.Reset();
            return;
        }
        if (!HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Any, out MixedRealityPose tip)
            || !HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexDistalJoint, Handedness.Any, out MixedRealityPose distal))
        {
            EndMagicTap2ObstacleContact();
            magicTap2Motion.Reset();
            return;
        }

        bool inside = MagicTap2MotionTracker.ContainsFinger(magicTap2Collider, tip.Position, distal.Position);
        if (!inside && magicTap2Motion.IsInside)
        {
            EndMagicTap2ObstacleContact();
        }
        else if (inside && !magicTap2Motion.IsInside)
        {
            handInBall = handInBall2 = inBall = true;
            if (!triggered)
            {
                magicTap2IdleMaterial = Player.GetComponent<MeshRenderer>().sharedMaterial;
                ballSpawner.errorInTimes++;
                Player.TurnShallow();
            }
        }

        if (inside)
        {
            ballSpawner.indexLoc = tip.Position;
            ballSpawner.area_type = triggered ? 4 : 2;
            ballSpawner.index = ballIndex;
        }

        bool selected = magicTap2Motion.Sample(tip.Position, inside, Time.fixedDeltaTime,
            ballSpawner.magicTap2SpeedThreshold, ballSpawner.magicTap2AccelerationThreshold);
        if (selected && !triggered)
        {
            ballSpawner.triggeredBy = "index";
            ballSpawner.triggerCondition = '2';
            Player.TurnYellow();
            if (source != null) source.Play();
            ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
        }
    }

    private void EndMagicTap2ObstacleContact()
    {
        if (!handInBall) return;
        handInBall = handInBall2 = inBall = false;
        if (!triggered)
        {
            Player.Acced = false;
            if (magicTap2IdleMaterial != null)
            {
                Player.GetComponent<MeshRenderer>().sharedMaterial = magicTap2IdleMaterial;
            }
            oldTime = Time.time;
            ballSpawner.area_type = 3;
            ballSpawner.index = -1;
        }
    }

    public void SetDynamicStopMode()
    {
        dynamicStopCollider = GetComponent<Collider>();
        dynamicStopMotion.Reset();
        ixdlDynamicMotion.Reset();
        handInBall = handInBall2 = inBall = false;
    }

    private void UpdateDynamicStopObstacle()
    {
        // Previously selected start balls have Acce disabled by BallSpawner.
        if (ballSpawner == null || Acce == null || !Acce.enabled)
        {
            EndDynamicStopObstacleContact();
            dynamicStopMotion.Reset();
            ixdlDynamicMotion.Reset();
            return;
        }
        if (!HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Any, out MixedRealityPose pose))
        {
            EndDynamicStopObstacleContact();
            dynamicStopMotion.Reset();
            ixdlDynamicMotion.Reset();
            return;
        }

        bool inside = DynamicStopMotionTracker.ContainsPoint(dynamicStopCollider, pose.Position);
        if (!inside && DynamicMotionInside)
        {
            EndDynamicStopObstacleContact();
        }
        else if (inside && !DynamicMotionInside)
        {
            handInBall = handInBall2 = inBall = true;
            if (!triggered)
            {
                dynamicStopIdleMaterial = Player.GetComponent<MeshRenderer>().sharedMaterial;
                ballSpawner.errorInTimes++;
                Player.TurnShallow();
            }
        }

        if (inside)
        {
            ballSpawner.indexLoc = pose.Position;
            ballSpawner.area_type = triggered ? 4 : 2;
            ballSpawner.index = ballIndex;
        }

        bool selected = UsesIxDLDynamic
            ? ixdlDynamicMotion.Sample(pose.Position, inside, Time.fixedDeltaTime, ballSpawner.GetIxDLDynamicMotionSettings())
            : dynamicStopMotion.Sample(pose.Position, inside, Time.unscaledTimeAsDouble, ballSpawner.GetDynamicStopMotionSettings());
        if (selected && !triggered)
        {
            if (UsesIxDLDynamic)
            {
                logWriter3?.RecordIxDLDynamicTrigger(ixdlDynamicMotion, "obstacle", GetInstanceID(),
                    ballIndex, ballKind, false, pose.Position);
            }
            ballSpawner.triggeredBy = "index";
            ballSpawner.triggerCondition = 'M';
            Player.TurnYellow();
            if (source != null) source.Play();
            ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
        }
    }

    private void EndDynamicStopObstacleContact()
    {
        if (!handInBall)
        {
            return;
        }
        handInBall = handInBall2 = inBall = false;
        if (!triggered)
        {
            // Reset activation feedback immediately, including after an error trigger.
            Player.Acced = false;
            if (dynamicStopIdleMaterial != null)
            {
                Player.GetComponent<MeshRenderer>().sharedMaterial = dynamicStopIdleMaterial;
            }
            oldTime = Time.time;
            ballSpawner.area_type = 3;
            ballSpawner.index = -1;
        }
    }

    public void SetHesMode()
    {
        Acce.BeforeHesEvent.AddListener(Acce.SetInvoked);
        //Acce.HesEvent.AddListener(Acce.OutMaterial);
        Acce.HesEvent.AddListener(Acce.OpenCloseSti); // CloseSti set to be false after out
        Acce.HesEvent.AddListener(Acce.showDebugger);
        Acce.AfterOutEvent.AddListener(Acce.UnInvoked);

        if (ballSpawner != null)
        {
            //Acce.InEvent.AddListener(Player.TurnShallow);
            Acce.InEvent.AddListener(() =>
            {
                if (!triggered)
                {
                    Player.TurnShallow();
                    ballSpawner.area_type = 2;
                    ballSpawner.index = ballIndex;
                    print("在障碍球中");
                }
                else if (triggered)
                {
                    print("触发目标球后还没离开");
                    ballSpawner.area_type = 4;
                    ballSpawner.index = ballIndex;
                }
                if (!handInBall2 && !triggered)
                {
                    ballSpawner.errorInTimes++;
                    handInBall2 = true;
                }
                else if (handInBall2 && !triggered)
                {

                }


            });
            //Acce.HesEvent.AddListener();
            //Acce.AfterOutEvent.AddListener(Player.TurnShallowBack);
            //Acce.InEvent.AddListener(ballSpawner.changeOpacity);
            //Acce.OutEvent.AddListener(ballSpawner.getNewTarget);
            Acce.HesEvent.AddListener(() =>
            {
                if (!triggered)
                {
                    Player.TurnYellow();
                    source.Play();
                    //！！！是否应该放在hesevent，障碍球触发后应该变
                    print("触发障碍球后还没离开");
                    ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
                }

            });
            Acce.AfterOutEvent.AddListener(() =>
            {
                handInBall2 = false;
                inBall = false;
                if (!triggered)
                {
                    
                    oldTime = Time.time;
                    Player.TurnShallowBack();

                }
                //if (!ballSpawner.triggered)
                //{
                print("离开障碍球");
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
                //triggered = false;
                //}


                ballSpawner.triggered = false;
            });
        }
    }

    public void SetPinchThumbSwipeMode()
    {
        ResetPinchThumbSwipeState();

        // Only a fresh PinchStart inside begins tracking. Entering while
        // already pinching only updates contact state.
        Acce.InEvent.AddListener(() =>
        {
            HandlePinchThumbSwipeObstacleEnter(true);
        });

        Acce2.InEvent.AddListener(() =>
        {
            HandlePinchThumbSwipeObstacleEnter(false);
        });

        pinchob.PinchStart.AddListener(() =>
        {
            if (pinchThumbSwipeCanceled || pinchThumbSwipeReentryRequired || triggered)
            {
                return;
            }

            if (HasAnyPinchContact() && TryCapturePinchThumbSwipeStart())
            {
                pinchThumbSwipeTracking = true;
            }
        });

        pinchob.PinchEnd.AddListener(() =>
        {
            if (pinchThumbSwipeCanceled || pinchThumbSwipeReentryRequired || triggered)
            {
                ResetPinchThumbSwipeTracking();
                return;
            }

            if (!pinchThumbSwipeTracking)
            {
                return;
            }

            ResolvePinchThumbSwipeObstacleEnd();
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            HandlePinchThumbSwipeObstacleExit(true);
        });

        Acce2.AfterOutEvent.AddListener(() =>
        {
            HandlePinchThumbSwipeObstacleExit(false);
        });
    }

    public void SetPinchWristTiltMode()
    {
        ResetPinchWristTiltState();

        Acce.InEvent.AddListener(() =>
        {
            if (pinchWristTiltReentryRequired || triggered)
            {
                ballSpawner.area_type = triggered ? 4 : 3;
                ballSpawner.index = -1;
                return;
            }

            if (!pinchWristTiltInside)
            {
                Player.TurnShallow();
                pinchWristTiltInside = true;
                ballSpawner.area_type = 2;
                ballSpawner.index = ballIndex;
                ballSpawner.errorInTimes++;
            }
            else
            {
                ballSpawner.area_type = 2;
                ballSpawner.index = ballIndex;
            }

        });

        pinchob.PinchStart.AddListener(() =>
        {
            if (pinchWristTiltCanceled || pinchWristTiltReentryRequired || triggered)
            {
                return;
            }

            if (!pinchWristTiltInside)
            {
                return;
            }

            pinchWristTiltPinching = true;
            pinchWristTiltHasEntryRotation = TryGetWristRotation(out pinchWristTiltEntryRotation);
        });

        pinchob.PinchEnd.AddListener(() =>
        {
            if (pinchWristTiltCanceled || pinchWristTiltReentryRequired || triggered)
            {
                ResetPinchWristTiltTracking();
                return;
            }

            if (!pinchWristTiltPinching)
            {
                return;
            }

            if (IsPinchWristTiltPastThreshold())
            {
                CancelPinchWristTiltObstacle();
                return;
            }

            TriggerPinchWristTiltObstacle();
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            pinchWristTiltInside = false;
            if (pinchWristTiltReentryRequired)
            {
                ClearPinchWristTiltReentry();
                return;
            }

            ballSpawner.area_type = 3;
            ballSpawner.index = -1;
            oldTime = Time.time;
            if (!triggered)
            {
                Player.TurnYellowBack();
            }
            ballSpawner.triggered = false;
            ResetPinchWristTiltState();
        });
    }

    public void SetWristTiltMode()
    {
        ResetPinchWristTiltState();

        Acce.InEvent.AddListener(() =>
        {
            if (pinchWristTiltReentryRequired || triggered)
            {
                ballSpawner.area_type = triggered ? 4 : 3;
                ballSpawner.index = -1;
                return;
            }

            if (!pinchWristTiltInside)
            {
                Player.TurnShallow();
                pinchWristTiltInside = true;
                pinchWristTiltPinching = true;
                pinchWristTiltEntryTimestamp = Time.time;
                pinchWristTiltHasEntryRotation = TryGetWristRotation(out pinchWristTiltEntryRotation);
                ballSpawner.area_type = 2;
                ballSpawner.index = ballIndex;
                ballSpawner.errorInTimes++;
            }
            else
            {
                ballSpawner.area_type = 2;
                ballSpawner.index = ballIndex;
            }
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            bool wasInside = pinchWristTiltInside;
            pinchWristTiltInside = false;

            if (pinchWristTiltReentryRequired)
            {
                ClearPinchWristTiltReentry();
                return;
            }

            if (!wasInside || pinchWristTiltTriggered || pinchWristTiltCanceled)
            {
                return;
            }

            if (IsPinchWristTiltPastThreshold())
            {
                CancelPinchWristTiltObstacle();
                return;
            }

            TriggerWristTiltObstacle();
            ballSpawner.area_type = 3;
            ballSpawner.index = -1;
            oldTime = Time.time;
            ballSpawner.triggered = false;
            ResetPinchWristTiltState();
        });
    }

    public void SetDwellWristTiltMode()
    {
        ResetPinchWristTiltState();

        Acce.InEvent.AddListener(() =>
        {
            if (pinchWristTiltReentryRequired || triggered)
            {
                ballSpawner.area_type = triggered ? 4 : 3;
                ballSpawner.index = -1;
                return;
            }

            if (!pinchWristTiltInside)
            {
                Player.TurnShallow();
                pinchWristTiltInside = true;
                pinchWristTiltPinching = true;
                pinchWristTiltEntryTimestamp = Time.time;
                pinchWristTiltHasEntryRotation = TryGetWristRotation(out pinchWristTiltEntryRotation);
                ballSpawner.area_type = 2;
                ballSpawner.index = ballIndex;
                ballSpawner.errorInTimes++;
            }
            else
            {
                ballSpawner.area_type = 2;
                ballSpawner.index = ballIndex;
            }
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            bool wasInside = pinchWristTiltInside;
            pinchWristTiltInside = false;

            if (pinchWristTiltReentryRequired)
            {
                ClearPinchWristTiltReentry();
                return;
            }

            if (!wasInside || pinchWristTiltTriggered || pinchWristTiltCanceled)
            {
                return;
            }

            ballSpawner.area_type = 3;
            ballSpawner.index = -1;
            oldTime = Time.time;
            if (!triggered)
            {
                Player.TurnYellowBack();
            }
            ballSpawner.triggered = false;
            ResetPinchWristTiltState();
        });
    }

    public void SetDwellReleaseMode()
    {
        EnsureReleaseGesture();
        Acce.BeforeHesEvent.AddListener(Acce.SetInvoked);
        Acce.HesEvent.AddListener(Acce.OpenCloseSti);
        Acce.HesEvent.AddListener(Acce.showDebugger);
        Acce.AfterOutEvent.AddListener(Acce.UnInvoked);

        if (ballSpawner != null)
        {
            Acce.InEvent.AddListener(() =>
            {
                if (dwellReleaseReentryRequired)
                {
                    return;
                }

                if (!triggered)
                {
                    Player.TurnShallow();
                    ballSpawner.area_type = 2;
                    ballSpawner.index = ballIndex;
                    print("在障碍球中");
                }
                else
                {
                    print("触发目标球后还没离开");
                    ballSpawner.area_type = 4;
                    ballSpawner.index = ballIndex;
                }

                if (!handInBall2 && !triggered)
                {
                    dwellReleaseInside = true;
                    dwellReleaseEntryTimestamp = Time.time;
                    ResetReleaseGestureTracking();
                    ballSpawner.errorInTimes++;
                    handInBall2 = true;
                }
            });

            Acce.HesEvent.AddListener(() =>
            {
                if (dwellReleaseCancelTriggered || dwellReleaseReentryRequired)
                {
                    return;
                }

                dwellReleaseInside = false;
                if (!triggered)
                {
                    Player.TurnYellow();
                    source.Play();
                    print("触发障碍球后还没离开");
                    ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
                }
            });

            Acce.AfterOutEvent.AddListener(() =>
            {
                handInBall2 = false;
                inBall = false;
                ResetDwellReleaseState();

                if (!triggered)
                {
                    oldTime = Time.time;
                    Player.TurnShallowBack();
                }

                print("离开障碍球");
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
                ballSpawner.triggered = false;
            });
        }
    }

    public void SetDwellWithdrawMode()
    {
        Acce.BeforeHesEvent.AddListener(Acce.SetInvoked);
        Acce.HesEvent.AddListener(Acce.OpenCloseSti);
        Acce.HesEvent.AddListener(Acce.showDebugger);
        Acce.AfterOutEvent.AddListener(Acce.UnInvoked);

        if (ballSpawner != null)
        {
            Acce.InEvent.AddListener(() =>
            {
                if (dwellWithdrawReentryRequired)
                {
                    return;
                }

                if (!triggered)
                {
                    Player.TurnShallow();
                    ballSpawner.area_type = 2;
                    ballSpawner.index = ballIndex;
                    print("在障碍球中");
                }
                else
                {
                    print("触发目标球后还没离开");
                    ballSpawner.area_type = 4;
                    ballSpawner.index = ballIndex;
                }

                if (!handInBall2 && !triggered)
                {
                    dwellWithdrawInside = true;
                    dwellWithdrawEntryTimestamp = Time.time;
                    dwellWithdrawEntryVelocityDirection = GetUserFacingDirection();
                    dwellWithdrawCurrentVelocityDirection = Vector3.zero;
                    dwellWithdrawPositionHistory.Clear();
                    ballSpawner.errorInTimes++;
                    handInBall2 = true;
                }

                if (!triggered)
                {
                    RecordDwellWithdrawFingerPosition();
                }
            });

            Acce.HesEvent.AddListener(() =>
            {
                if (dwellWithdrawCancelTriggered || dwellWithdrawReentryRequired)
                {
                    return;
                }

                dwellWithdrawInside = false;
                if (!triggered)
                {
                    Player.TurnYellow();
                    source.Play();
                    print("触发障碍球后还没离开");
                    ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
                }
            });

            Acce.AfterOutEvent.AddListener(() =>
            {
                handInBall2 = false;
                inBall = false;
                ResetDwellWithdrawState();

                if (!triggered)
                {
                    oldTime = Time.time;
                    Player.TurnShallowBack();
                }

                print("离开障碍球");
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
                ballSpawner.triggered = false;
            });
        }
    }

    public void SetFastMode()
    {
        Acce.HesEvent.AddListener(Acce.OutMaterial);
        Acce.HesEvent.AddListener(Acce.OpenCloseSti); // CloseSti set to be false after out
        if (Player != null)
        {
            //Acce.HesEvent.AddListener(ballSpawner.turnYellow);
            //Acce.InEvent.AddListener(ballSpawner.turnYellow);
            //Acce.InEvent.AddListener(Player.TurnYellow);
            //Acce.OutEvent.AddListener(Player.turnYellowBack);
        }
        if (ballSpawner != null)
        {
            //Acce.InEvent.AddListener();
            Acce.InEvent.AddListener(() => {
                if (!triggered)
                {

                    Player.TurnYellow();

                }
                else if (triggered)
                {
                    print("触发目标球后还没离开");
                    ballSpawner.area_type = 4;
                    ballSpawner.index = -1;
                }

                if (inBall == false && !triggered)
                {
                    source.Play();
                    inBall = true;
                    print("在障碍球中");
                    ballSpawner.area_type = 2;
                    ballSpawner.index = ballIndex;
                }
                else if (inBall && !triggered)
                {
                    ballSpawner.area_type = 2;
                    ballSpawner.index = ballIndex;
                }


            });
            //Acce.HesEvent.AddListener(Player.);
            //Acce.AfterOutEvent.AddListener(Player.TurnShallowBack);
            //Acce.InEvent.AddListener(ballSpawner.changeOpacity);
            //Acce.OutEvent.AddListener(ballSpawner.getNewTarget);
            Acce.AfterOutEvent.AddListener(() =>
            {
                print("离开障碍球");
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
                ballSpawner.errorInTimes++;
                ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
                //triggered = false;
                oldTime = Time.time;
                inBall = false;

                ballSpawner.triggered = false;
            });
        }
    }

    public void SetPressMode()
    {
        if (ballSpawner != null)
        {
            Acce.InEvent.AddListener(() =>
            {
                if (!handInBall2)
                {
                    ballSpawner.errorInTimes++;
                    Player.TurnShallow();
                    handInBall2 = true;

                    if (!triggered && Acce.v > ballSpawner.pressSpeedThershold)
                    {
                        Player.TurnYellow();
                        source.Play();
                        ballSpawner.triggerCondition = 'P';
                        ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
                        oldTime = Time.time;
                    }
                }

                if (!triggered)
                {
                    ballSpawner.area_type = 2;
                    ballSpawner.index = ballIndex;
                }
                else
                {
                    ballSpawner.area_type = 4;
                    ballSpawner.index = ballIndex;
                }
            });

            Acce.AfterOutEvent.AddListener(() =>
            {
                if (!triggered)
                {
                    Player.TurnShallowBack();
                }

                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
                handInBall2 = false;
                inBall = false;
                ballSpawner.triggered = false;
            });
        }
    }

    public void SetContact010Mode()
    {
        ResetContact010ObstacleState();

        if (ballSpawner != null)
        {
            Acce.InEvent.AddListener(() =>
            {
                if (!handInBall2)
                {
                    handInBall2 = true;
                    handInBall = true;
                    inBall = true;
                    Player.TurnShallow();
                    ballSpawner.errorInTimes++;
                    ConfigureContact010Frame();
                }

                if (!triggered)
                {
                    ballSpawner.area_type = 2;
                    ballSpawner.index = ballIndex;
                }
                else
                {
                    ballSpawner.area_type = 4;
                    ballSpawner.index = ballIndex;
                }
            });

            Acce.AfterOutEvent.AddListener(() =>
            {
                bool wasInside = handInBall2 || inBall || handInBall;
                bool exitInTriggerRegion = wasInside && IsContact010ExitInUserFacingFrame();

                if (wasInside && exitInTriggerRegion && !triggered)
                {
                    Player.TurnYellow();
                    source.Play();
                    ballSpawner.triggerCondition = 'C';
                    ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
                    oldTime = Time.time;
                }
                else if (!triggered)
                {
                    Player.TurnShallowBack();
                }

                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
                ResetContact010ObstacleState();
                ballSpawner.triggered = false;
            });
        }
    }

    public void SetDirection010Mode()
    {
        ResetDirection010ObstacleState();

        if (ballSpawner != null)
        {
            Acce.InEvent.AddListener(() =>
            {
                // 010-direction: 进入障碍球后检测朝向用户方向取消；取消后必须离开球体再重新进入。
                if (direction010ReentryRequired)
                {
                    return;
                }

                if (!handInBall2)
                {
                    // 首次进入障碍球时记录状态和进入次数，并显示浅色反馈。
                    handInBall2 = true;
                    handInBall = true;
                    inBall = true;
                    canceled = false;
                    direction010CancelTriggered = false;
                    Player.TurnShallow();
                    ballSpawner.errorInTimes++;
                }

                CaptureDirectionCancelEntryAxisIfNeeded();

                if (!triggered)
                {
                    ballSpawner.area_type = 2;
                    ballSpawner.index = ballIndex;
                }
                else
                {
                    ballSpawner.area_type = 4;
                    ballSpawner.index = ballIndex;
                }

                // 在球内每个 FixedUpdate 通过 InEvent 检测一次：可调窗口内的平滑方向接近朝向用户方向时取消误触。
                if (IsDirection010CancelMovement())
                {
                    TriggerDirection010ObstacleCancel();
                }
            });

            Acce.AfterOutEvent.AddListener(() =>
            {
                bool wasInside = handInBall2 || inBall || handInBall || direction010ReentryRequired;

                if (wasInside && !direction010CancelTriggered && !triggered)
                {
                    // 离开障碍球时，如果此前没有按朝向用户方向取消，则按障碍球误触记录。
                    Player.TurnYellow();
                    source.Play();
                    ballSpawner.triggerCondition = 'D';
                    ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
                    oldTime = Time.time;
                }
                else if (!triggered)
                {
                    Player.TurnShallowBack();
                }

                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
                ResetDirection010ObstacleState();
                ballSpawner.triggered = false;
            });
        }
    }

    public void SetDwellDirectionMode()
    {
        ResetDirection010ObstacleState();
        Acce.BeforeHesEvent.AddListener(Acce.SetInvoked);
        Acce.HesEvent.AddListener(Acce.OpenCloseSti);
        Acce.HesEvent.AddListener(Acce.showDebugger);
        Acce.AfterOutEvent.AddListener(Acce.UnInvoked);

        if (ballSpawner != null)
        {
            Acce.InEvent.AddListener(() =>
            {
                if (direction010ReentryRequired)
                {
                    return;
                }

                if (!handInBall2)
                {
                    Player.TurnShallow();
                    handInBall2 = true;
                    handInBall = true;
                    inBall = true;
                    canceled = false;
                    direction010CancelTriggered = false;
                    ResetDirection010ObstacleTrackingWindow();
                    if (!triggered)
                    {
                        ballSpawner.errorInTimes++;
                    }
                }

                CaptureDirectionCancelEntryAxisIfNeeded();

                if (!triggered)
                {
                    ballSpawner.area_type = 2;
                    ballSpawner.index = ballIndex;
                }
                else
                {
                    ballSpawner.area_type = 4;
                    ballSpawner.index = ballIndex;
                }

                if (!triggered && IsDirection010CancelMovement())
                {
                    TriggerDirection010ObstacleCancel();
                }
            });

            Acce.HesEvent.AddListener(() =>
            {
                if (direction010CancelTriggered || direction010ReentryRequired || canceled)
                {
                    return;
                }

                if (!triggered)
                {
                    Player.TurnYellow();
                    source.Play();
                    ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
                }
            });

            Acce.AfterOutEvent.AddListener(() =>
            {
                bool wasDirectionCanceled = direction010CancelTriggered || direction010ReentryRequired || canceled;

                handInBall2 = false;
                handInBall = false;
                inBall = false;

                if (!wasDirectionCanceled && !triggered)
                {
                    oldTime = Time.time;
                    Player.TurnShallowBack();
                }

                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
                ResetDirection010ObstacleState();
                ballSpawner.triggered = false;
            });
        }
    }

    public void SetPinchDirectionMode()
    {
        ResetPinchReleaseTriggerState();
        ResetPinchDirectionObstacleCancelState();

        Acce.InEvent.AddListener(() =>
        {
            HandlePinchDirectionObstacleEnter(true);
            CaptureDirectionCancelEntryAxisIfNeeded();

            if (pinchReleaseTriggerArmed
                && !pinchDirectionCancelTriggered
                && !pinchDirectionReentryRequired
                && !triggered
                && IsPinchDirectionCancelMovement())
            {
                pinchReleaseTriggerArmed = false;
                TriggerPinchDirectionObstacleCancel();
            }
        });

        Acce2.InEvent.AddListener(() =>
        {
            HandlePinchDirectionObstacleEnter(false);
        });

        pinchob.PinchStart.AddListener(() =>
        {
            if (pinchReleaseTriggerCompleted
                || pinchDirectionCancelTriggered
                || pinchDirectionReentryRequired)
            {
                return;
            }

            if (HasAnyPinchContact())
            {
                pinchReleaseTriggerArmed = true;
                ResetPinchDirectionObstacleTrackingWindow();
            }
        });

        pinchob.PinchEnd.AddListener(() =>
        {
            if (pinchReleaseTriggerCompleted)
            {
                return;
            }

            bool hasPinchContact = HasAnyPinchContact();
            if (pinchReleaseTriggerArmed
                && hasPinchContact
                && !triggered
                && !pinchDirectionCancelTriggered
                && !pinchDirectionReentryRequired)
            {
                TriggerPinchReleaseTriggerObstacle();
                ResetPinchDirectionObstacleCancelState();
            }
            else if (pinchReleaseTriggerArmed
                && !hasPinchContact
                && !triggered
                && !pinchDirectionCancelTriggered
                && !pinchDirectionReentryRequired)
            {
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
                pinchReleaseTriggerArmed = false;
                ResetPinchReleaseTriggerState();
                ResetPinchDirectionObstacleCancelState();
            }

            pinchReleaseTriggerArmed = false;
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            HandlePinchDirectionObstacleExit(true);
        });

        Acce2.AfterOutEvent.AddListener(() =>
        {
            HandlePinchDirectionObstacleExit(false);
        });
    }

    public void SetPinchReleaseTriggerMode()
    {
        ResetPinchReleaseTriggerState();

        Acce.InEvent.AddListener(() =>
        {
            HandlePinchReleaseTriggerEnter(true);
        });

        Acce2.InEvent.AddListener(() =>
        {
            HandlePinchReleaseTriggerEnter(false);
        });

        pinchob.PinchStart.AddListener(() =>
        {
            if (pinchReleaseTriggerCompleted)
            {
                return;
            }

            if (HasAnyPinchContact())
            {
                pinchReleaseTriggerArmed = true;
            }
        });

        pinchob.PinchEnd.AddListener(() =>
        {
            if (pinchReleaseTriggerCompleted)
            {
                return;
            }

            if (pinchReleaseTriggerArmed && HasAnyPinchContact() && !triggered)
            {
                TriggerPinchReleaseTriggerObstacle();
            }
            else if (pinchReleaseTriggerArmed && !HasAnyPinchContact())
            {
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
                ResetPinchReleaseTriggerState();
            }

            pinchReleaseTriggerArmed = false;
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            HandlePinchReleaseTriggerExit(true);
        });

        Acce2.AfterOutEvent.AddListener(() =>
        {
            HandlePinchReleaseTriggerExit(false);
        });
    }

    private void FixedUpdate()
    {
        if (UsesMagicTap2)
        {
            UpdateMagicTap2Obstacle();
        }
        else if (UsesIxDLDynamic)
        {
            UpdateDynamicStopObstacle();
        }
    }

    private void Update()
    {
        LogSpeedStudy("tracking_lost");
        if (modeSelected == (int)BallSpawner.Mode.dynamicStop)
        {
            UpdateDynamicStopObstacle();
            return;
        }
        if (modeSelected == 5
            && handInBall
            && !fastTapCancelTriggered
            && !fastTapReentryRequired
            && !fastTapCanceledUntilExit
            && HasFastTapTimedOut())
        {
            TriggerFastTapCancel();
        }
        if (modeSelected == 29
            && handInBall
            && !fastTapCancelTriggered
            && !fastTapReentryRequired
            && !fastTapCanceledUntilExit)
        {
            CheckSwing010Cancel();
        }

        if (modeSelected == 6 && handInBall && rainbowArcActive && !rainbowCancelTriggered && !rainbowConfirmBlocked)
        {
            CheckRainbowCancel();
        }

        if (modeSelected == 7
            && handInBall
            && !fastTapCancelTriggered
            && !fastTapReentryRequired
            && !fastTapCanceledUntilExit)
        {
            // While the hand stays inside the ReactTap region, an opening gesture cancels the attempt.
            CheckReleaseTapCancel();
        }

        if (modeSelected == 14
            && handInBall
            && reactReleaseSpeedAcceMet
            && !reactReleaseTriggered
            && !reactReleaseReentryRequired)
        {
            CheckReactReleaseCancel();
            CheckReactReleaseIndexTrigger();
        }

        if ((modeSelected == 15 || modeSelected == 20 || modeSelected == 21 || modeSelected == 22)
            && handInBall
            && !speedReleaseTriggered
            && !speedReleaseReentryRequired)
        {
            if (modeSelected == 20)
            {
                CheckSpeedWristTiltCancel();
            }
            else if (modeSelected == 15)
            {
                CheckSpeedReleaseCancel();
            }

            CheckSpeedReleaseIndexTrigger();

            if (modeSelected == 22
                && !speedReleaseTriggered
                && HasSpeedDwellTimedOut())
            {
                TriggerSpeedObstacleCancel(true);
            }
        }

        if (modeSelected == 11
            && dwellReleaseInside
            && !dwellReleaseCancelTriggered
            && !dwellReleaseReentryRequired
            && !HasDwellReleaseTimedOut())
        {
            CheckDwellReleaseCancel();
        }

        if (modeSelected == 12
            && dwellWithdrawInside
            && !dwellWithdrawCancelTriggered
            && !dwellWithdrawReentryRequired
            && !HasDwellWithdrawTimedOut())
        {
            CheckDwellWithdrawCancel();
        }

        if (modeSelected == 16
            && pinchThumbSwipeTracking
            && !pinchThumbSwipeCanceled
            && !pinchThumbSwipeReentryRequired)
        {
            UpdatePinchThumbSwipeContactDistance();
        }

        if (modeSelected == 17
            && pinchWristTiltInside
            && pinchWristTiltPinching
            && !pinchWristTiltTriggered
            && !pinchWristTiltCanceled
            && !pinchWristTiltReentryRequired)
        {
            CheckPinchWristTiltCancel();
        }
        if (modeSelected == 18
            && pinchWristTiltInside
            && !pinchWristTiltTriggered
            && !pinchWristTiltCanceled
            && !pinchWristTiltReentryRequired)
        {
            CheckPinchWristTiltCancel();
        }
        if (modeSelected == 19
            && pinchWristTiltInside
            && !pinchWristTiltTriggered
            && !pinchWristTiltCanceled
            && !pinchWristTiltReentryRequired)
        {
            UpdateDwellWristTiltObstacle();
        }

        if (Time.time - oldTime > 1 && !triggered && !inBall && !inBall2 && gameObject != ballSpawner.nextNextBall)
        {
            oldTime = Time.time;
            Player.TurnYellowBack();
        }
    }

    private void CheckSwing010Cancel()
    {
        if (swing010CancelTriggered
            || fastTapCancelTriggered
            || fastTapReentryRequired
            || fastTapCanceledUntilExit)
        {
            return;
        }

        CaptureSwing010EntryDirectionIfNeeded();
        if (!swing010HasEntryDirection)
        {
            return;
        }

        float threshold = ballSpawner != null ? Mathf.Max(0f, ballSpawner.swing010CancelAngleThreshold) : 35f;
        if (threshold > 180f)
        {
            return;
        }

        if (!TryGetSwing010Direction(out Vector3 currentDirection))
        {
            return;
        }

        if (Vector3.Angle(swing010EntryDirection, currentDirection) >= threshold)
        {
            TriggerSwing010Cancel();
        }
    }

    private void CaptureSwing010EntryDirectionIfNeeded()
    {
        if (swing010HasEntryDirection || !handInBall)
        {
            return;
        }

        swing010HasEntryDirection = TryGetSwing010Direction(out swing010EntryDirection);
    }

    private bool TryGetSwing010Direction(out Vector3 indexFromWristDirection)
    {
        indexFromWristDirection = Vector3.zero;

        bool hasIndex = HandJointUtils.TryGetJointPose(
            TrackedHandJoint.IndexTip,
            Handedness.Any,
            out MixedRealityPose indexTipPose);
        bool hasWrist = HandJointUtils.TryGetJointPose(
            TrackedHandJoint.Wrist,
            Handedness.Any,
            out MixedRealityPose wristPose);

        if (!hasIndex || !hasWrist)
        {
            return false;
        }

        Vector3 direction = indexTipPose.Position - wristPose.Position;
        if (direction.sqrMagnitude <= 0.000001f)
        {
            return false;
        }

        indexFromWristDirection = direction.normalized;
        return true;
    }

    private void TriggerSwing010Cancel()
    {
        if (swing010CancelTriggered || fastTapCancelTriggered)
        {
            return;
        }

        swing010CancelTriggered = true;
        TriggerFastTapCancel();
        ResetSwing010Tracking();
    }

    private void ResetSwing010State()
    {
        swing010CancelTriggered = false;
        ResetSwing010Tracking();
    }

    private void ResetSwing010Tracking()
    {
        swing010HasEntryDirection = false;
        swing010EntryDirection = Vector3.zero;
    }

    private void TriggerFastTapCancel()
    {
        if (fastTapCancelTriggered || !handInBall)
        {
            return;
        }

        fastTapCancelTriggered = true;
        fastTapReentryRequired = true;
        fastTapCanceledUntilExit = true;
        canceled = true;
        handInBall = false;
        ballSpawner.isReact = false;
        Player.hideCircle();
    }

    private bool HasFastTapTimedOut()
    {
        return fastTapReactTimestamp >= 0f
            && Time.time - fastTapReactTimestamp > fastTapCancelTime;
    }

    private void ResetFastTapTiming()
    {
        fastTapEntryTimestamp = -1f;
        fastTapReactTimestamp = -1f;
    }

    private void CheckReactReleaseIndexTrigger()
    {
        if (!reactReleaseSpeedAcceMet || reactReleaseTriggered || reactReleaseReentryRequired)
        {
            return;
        }

        if (reactTap.V_vec >= ballSpawner.reactReleaseIndexSpeedThreshold)
        {
            TriggerReactReleaseObstacle();
        }
    }

    private void CheckReactReleaseCancel()
    {
        if (EnsureReleaseGesture().CheckRelease())
        {
            TriggerReactReleaseCancel();
        }
    }

    private void TriggerReactReleaseObstacle()
    {
        if (reactReleaseTriggered || reactReleaseReentryRequired)
        {
            return;
        }

        reactReleaseTriggered = true;
        ballSpawner.triggerCondition = 'W';
        Player.TurnYellow();
        source.Play();
        ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
        ballSpawner.isReact = false;
        handInBall = false;
        handInBall2 = false;
        inBall = false;
        ResetReleaseGestureTracking();
        ResetFastTapTiming();
        oldTime = Time.time;
    }

    private void TriggerReactReleaseCancel()
    {
        if (reactReleaseTriggered || reactReleaseReentryRequired)
        {
            return;
        }

        reactReleaseReentryRequired = true;
        canceled = true;
        handInBall = false;
        handInBall2 = false;
        inBall = false;
        ballSpawner.isReact = false;
        Player.hideCircle();
        Player.TurnShallowBack();
        ResetReleaseGestureTracking();
        ResetFastTapTiming();
    }

    private void ResetReactReleaseState()
    {
        reactReleaseSpeedAcceMet = false;
        reactReleaseTriggered = false;
        reactReleaseReentryRequired = false;
        handInBall = false;
        handInBall2 = false;
        inBall = false;
        canceled = false;
    }

    private void LogSpeedStudy(string eventName, string reason = "", bool allowHighSpeedTrigger = true)
    {
        if (!SpeedStudyTelemetry.IsSpeedMode(modeSelected) || ballSpawner == null || Acce == null
            || logWriter3 == null || logWriter3.IsStateOnly
            || ballSpawner.executionMode != BallSpawner.ExecutionMode.Study3Experiment)
        {
            return;
        }
        if (speedStudyTelemetry == null)
        {
            if (eventName != "sample") return;
            speedStudyTelemetry = new SpeedStudyTelemetry(logWriter3.WriteSpeedStudyRow, logWriter3.NextSpeedEntrySequence);
        }
        SpeedStudySnapshot snapshot = SpeedStudySnapshot.Capture(this, ballSpawner, Acce, modeSelected,
            ballIndex, ballKind, true, "obstacle", GetCurrentIndexSpeed(), handInBall,
            speedReleaseMinSpeedMet, speedReleaseTriggered,
            speedReleaseReentryRequired || direction010ReentryRequired, allowHighSpeedTrigger);
        snapshot.ExperimentStarted = logWriter3.IsExperimentRunning;
        if (eventName == "sample") logWriter3.RegisterSpeedStudyObserver(speedStudyTelemetry);
        switch (eventName)
        {
            case "sample": speedStudyTelemetry.Inside(snapshot); break;
            case "check": speedStudyTelemetry.Check(snapshot); break;
            case "armed": speedStudyTelemetry.Armed(snapshot); break;
            case "exit": speedStudyTelemetry.Exit(snapshot); break;
            case "disabled": speedStudyTelemetry.Disable(snapshot); break;
            case "tracking_lost": speedStudyTelemetry.TrackingLost(snapshot); break;
            default: speedStudyTelemetry.Finish(snapshot, eventName, reason); break;
        }
    }

    private void CheckSpeedReleaseIndexTrigger(bool allowHighSpeedTrigger = true)
    {
        LogSpeedStudy("check", "", allowHighSpeedTrigger);
        if (speedReleaseTriggered || speedReleaseReentryRequired)
        {
            return;
        }

        float indexSpeed = GetCurrentIndexSpeed();
        if (!speedReleaseMinSpeedMet)
        {
            if (indexSpeed <= Mathf.Max(0f, ballSpawner.speedMinThreshold))
            {
                speedReleaseMinSpeedMet = true;
                LogSpeedStudy("armed");
                ResetReleaseGestureTracking();
            }

            return;
        }

        if (allowHighSpeedTrigger
            && indexSpeed >= Mathf.Max(0f, ballSpawner.speedMaxThreshold))
        {
            TriggerSpeedReleaseObstacle();
        }
    }

    private void CheckSpeedReleaseCancel()
    {
        if (!speedReleaseMinSpeedMet)
        {
            return;
        }

        if (EnsureReleaseGesture().CheckRelease())
        {
            TriggerSpeedReleaseCancel();
        }
    }

    private void CheckSpeedWristTiltCancel()
    {
        if (IsPinchWristTiltPastThreshold())
        {
            TriggerSpeedWristTiltCancel();
        }
    }

    private float GetCurrentIndexSpeed()
    {
        if (Acce != null && Acce.v != float.MaxValue)
        {
            return Mathf.Max(0f, Acce.v);
        }

        if (reactTap != null)
        {
            return Mathf.Max(0f, reactTap.V_vec);
        }

        return 0f;
    }

    private void TriggerSpeedReleaseObstacle()
    {
        if (speedReleaseTriggered || speedReleaseReentryRequired)
        {
            return;
        }

        speedReleaseTriggered = true;
        LogSpeedStudy("trigger", "obstacle_error");
        ballSpawner.triggerCondition = 'S';
        Player.TurnYellow();
        source.Play();
        ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
        handInBall = false;
        handInBall2 = false;
        inBall = false;
        ResetReleaseGestureTracking();
        ResetFastTapTiming();
        oldTime = Time.time;
    }

    private void TriggerSpeedReleaseCancel()
    {
        if (speedReleaseTriggered || speedReleaseReentryRequired)
        {
            return;
        }

        LogSpeedStudy("cancel", "release");
        speedReleaseReentryRequired = true;
        canceled = true;
        handInBall = false;
        handInBall2 = false;
        inBall = false;
        Player.TurnShallowBack();
        ResetReleaseGestureTracking();
        ResetFastTapTiming();
    }

    private bool HasSpeedDwellTimedOut()
    {
        return fastTapEntryTimestamp >= 0f
            && Time.time - fastTapEntryTimestamp >= Mathf.Max(0f, ballSpawner.pinchDwellCancelTime);
    }

    private void TriggerSpeedObstacleCancel(bool requireReentry = false)
    {
        if (speedReleaseTriggered || speedReleaseReentryRequired)
        {
            return;
        }

        LogSpeedStudy("cancel", requireReentry ? "dwell" : "contact transition");
        speedReleaseReentryRequired = requireReentry;
        canceled = true;
        handInBall = false;
        handInBall2 = false;
        inBall = false;
        Player.TurnShallowBack();
        ResetFastTapTiming();
    }

    private void TriggerSpeedWristTiltCancel()
    {
        if (speedReleaseTriggered || speedReleaseReentryRequired)
        {
            return;
        }

        LogSpeedStudy("cancel", "wrist-tilt");
        speedReleaseReentryRequired = true;
        CancelPinchWristTiltObstacle();
        ResetFastTapTiming();
    }

    private void ResetSpeedReleaseState()
    {
        speedReleaseMinSpeedMet = false;
        speedReleaseTriggered = false;
        speedReleaseReentryRequired = false;
        handInBall = false;
        handInBall2 = false;
        inBall = false;
        canceled = false;
    }

    private void HandlePinchReleaseTriggerEnter(bool enteredByIndex)
    {
        if (pinchReleaseTriggerCompleted)
        {
            return;
        }

        if (triggered)
        {
            ballSpawner.area_type = 4;
            ballSpawner.index = ballIndex;
            return;
        }

        if (enteredByIndex)
        {
            if (!inBall)
            {
                inBall = true;
                ballSpawner.errorInTimes++;
            }
        }
        else if (!inBall2)
        {
            inBall2 = true;
            ballSpawner.errorInTimes++;
        }

        Player.TurnShallow();
        ballSpawner.area_type = 2;
        ballSpawner.index = ballIndex;


    }

    private void HandlePinchReleaseTriggerExit(bool exitedByIndex)
    {
        if (exitedByIndex)
        {
            inBall = false;
        }
        else
        {
            inBall2 = false;
        }

        if (HasAnyPinchContact())
        {
            if (!triggered)
            {
                ballSpawner.area_type = 2;
                ballSpawner.index = ballIndex;
            }
            return;
        }

        if (pinchob != null
            && pinchob.isPinch
            && pinchReleaseTriggerArmed
            && !pinchReleaseTriggerCompleted
            && !triggered)
        {
            // Wait for PinchEnd before discarding an armed gesture. A transient
            // physics-frame exit may be followed by a valid final contact.
            ballSpawner.area_type = 3;
            ballSpawner.index = -1;
            return;
        }

        if (!triggered)
        {
            oldTime = Time.time;
            Player.TurnShallowBack();
        }

        ballSpawner.area_type = 3;
        ballSpawner.index = -1;
        ballSpawner.triggered = false;
        ResetPinchReleaseTriggerState();
    }

    private void TriggerPinchReleaseTriggerObstacle()
    {
        pinchReleaseTriggerCompleted = true;
        Player.TurnYellow();
        source.Play();
        ballSpawner.triggerCondition = 'P';
        ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
        oldTime = Time.time;
    }

    private void ResetPinchReleaseTriggerState()
    {
        pinchReleaseTriggerArmed = false;
        pinchReleaseTriggerCompleted = false;
        inBall = false;
        inBall2 = false;
    }

    private bool HasAnyPinchContact()
    {
        return inBall || inBall2;
    }

    private void HandlePinchDirectionObstacleEnter(bool enteredByIndex)
    {
        if (pinchReleaseTriggerCompleted || pinchDirectionReentryRequired)
        {
            return;
        }

        bool wasArmed = pinchReleaseTriggerArmed;
        HandlePinchReleaseTriggerEnter(enteredByIndex);
        if (!wasArmed && pinchReleaseTriggerArmed)
        {
            ResetPinchDirectionObstacleTrackingWindow();
        }
    }

    private void HandlePinchDirectionObstacleExit(bool exitedByIndex)
    {
        if (exitedByIndex)
        {
            inBall = false;
        }
        else
        {
            inBall2 = false;
        }

        if (HasAnyPinchContact())
        {
            if (!triggered && !pinchDirectionCancelTriggered && !pinchDirectionReentryRequired)
            {
                ballSpawner.area_type = 2;
                ballSpawner.index = ballIndex;
            }
            return;
        }

        if (!triggered)
        {
            oldTime = Time.time;
            Player.TurnShallowBack();
        }

        ballSpawner.area_type = 3;
        ballSpawner.index = -1;
        ballSpawner.triggered = false;
        ResetPinchReleaseTriggerState();
        ResetPinchDirectionObstacleCancelState();
    }

    private void TriggerPinchDirectionObstacleCancel()
    {
        if (pinchDirectionCancelTriggered)
        {
            return;
        }

        pinchDirectionCancelTriggered = true;
        pinchDirectionReentryRequired = true;
        pinchReleaseTriggerArmed = false;
        canceled = true;
        handInBall = false;
        handInBall2 = false;
        inBall = false;
        ballSpawner.area_type = 3;
        ballSpawner.index = -1;
        Player.TurnShallowBack();
    }
    private bool IsPinchDirectionCancelMovement()
    {
        if (ballSpawner == null)
        {
            return false;
        }

        bool entryDirectionAxisMode = IsEntryDirectionAxisMode();
        CaptureDirectionCancelEntryAxisIfNeeded();
        Collider collider = GetComponent<Collider>();
        Vector3 ballCenter = collider != null ? collider.bounds.center : transform.position;
        Vector3 viewerPosition = GetViewerPositionForDirectionDebug();
        Vector3 windowMovement = GetPinchDirectionWindowMovement(out float centerSpeed);
        bool speedGatePass = !entryDirectionAxisMode || UpdateEntryDirectionCancelSpeedGate(centerSpeed);
        if (windowMovement.sqrMagnitude <= 0.0000001f)
        {
            RecordPinchDirectionDebug(
                entryDirectionAxisMode && centerSpeed < GetEntryDirectionCancelLowSpeedThreshold()
                    ? "entry_direction_frame_skipped_low_speed"
                    : entryDirectionAxisMode && !speedGatePass
                        ? "direction_speed_below_threshold"
                    : pinchDirectionCenterPositionHistory.Count < GetPinchDirectionWindowFrameCount() ? "not_ready_window" : "zero_window_movement",
                false,
                windowMovement,
                Vector3.zero,
                false,
                entryDirectionAxisMode ? directionCancelEntryAxis : Vector3.zero,
                float.NaN,
                false,
                centerSpeed,
                ballCenter,
                viewerPosition);
            return false;
        }

        if (entryDirectionAxisMode)
        {
            if (!speedGatePass)
            {
                RecordPinchDirectionDebug(
                    "direction_speed_below_threshold",
                    false,
                    windowMovement,
                    Vector3.zero,
                    false,
                    directionCancelEntryAxis,
                    float.NaN,
                    false,
                    centerSpeed,
                    ballCenter,
                    viewerPosition);
                return false;
            }
        }
        else
        {
            float minSpeed = GetPinchDirectionMinSpeedThreshold();
            if (minSpeed > 0f && centerSpeed < minSpeed)
            {
                RecordPinchDirectionDebug(
                    "speed_too_low",
                    false,
                    windowMovement,
                    Vector3.zero,
                    false,
                    Vector3.zero,
                    float.NaN,
                    false,
                    centerSpeed,
                    ballCenter,
                    viewerPosition);
                return false;
            }
        }

        Vector3 referenceDirection = entryDirectionAxisMode
            ? directionCancelEntryAxis
            : -GetViewerBackDirection(ballCenter);
        if (referenceDirection.sqrMagnitude <= 0.0000001f)
        {
            RecordPinchDirectionDebug(
                entryDirectionAxisMode ? "no_entry_direction_axis" : "no_viewer_direction",
                false,
                windowMovement,
                Vector3.zero,
                false,
                referenceDirection,
                float.NaN,
                true,
                centerSpeed,
                ballCenter,
                viewerPosition);
            return false;
        }

        Vector3 currentDirection = entryDirectionAxisMode
            ? windowMovement.normalized
            : SmoothPinchDirectionDirection(windowMovement.normalized);
        Vector3 comparisonDirection = Vector3.zero;
        Vector3 comparisonReferenceDirection = Vector3.zero;
        if (entryDirectionAxisMode)
        {
            comparisonDirection = currentDirection.normalized;
            comparisonReferenceDirection = referenceDirection.normalized;
        }
        else
        {
            bool hasSmoothedHorizontal = TryProjectHorizontalDirection(currentDirection, out comparisonDirection);
            bool hasReferenceHorizontal = TryProjectHorizontalDirection(referenceDirection, out comparisonReferenceDirection);
            if (!hasSmoothedHorizontal || !hasReferenceHorizontal)
            {
                RecordPinchDirectionDebug(
                    "zero_horizontal_direction",
                    false,
                    windowMovement,
                    comparisonDirection,
                    true,
                    comparisonReferenceDirection,
                    float.NaN,
                    false,
                    centerSpeed,
                    ballCenter,
                    viewerPosition);
                return false;
            }
        }

        float angle = entryDirectionAxisMode
            ? GetDirectionAxisAngle(comparisonDirection, comparisonReferenceDirection)
            : Vector3.Angle(comparisonDirection, comparisonReferenceDirection);
        bool anglePass = entryDirectionAxisMode
            ? angle <= GetPinchDirectionAngleThreshold()
            : angle <= GetPinchDirectionAngleThreshold();
        RecordPinchDirectionDebug(
            entryDirectionAxisMode
                ? (anglePass ? "inside_cancel_cone" : "angle_outside_cancel_cone")
                : (anglePass ? "pass" : "angle_too_large"),
            anglePass,
            windowMovement,
            comparisonDirection,
            true,
            comparisonReferenceDirection,
            angle,
            anglePass,
            centerSpeed,
            ballCenter,
            viewerPosition);
        return anglePass;
    }

    private Vector3 GetPinchDirectionWindowMovement(out float centerSpeed)
    {
        centerSpeed = 0f;
        if (!TryGetPinchDirectionCenterPoint(out Vector3 currentCenter))
        {
            return Vector3.zero;
        }

        if (IsEntryDirectionAxisMode())
        {
            float entrySampleDeltaTime = Mathf.Max(Time.fixedDeltaTime, 0.000001f);
            if (!pinchDirectionHasRawPosition)
            {
                pinchDirectionPreviousRawPosition = currentCenter;
                pinchDirectionHasRawPosition = true;
                pinchDirectionLastAcceptedPosition = currentCenter;
                pinchDirectionHasAcceptedPosition = true;
                return Vector3.zero;
            }

            Vector3 rawMovement = currentCenter - pinchDirectionPreviousRawPosition;
            centerSpeed = rawMovement.magnitude / entrySampleDeltaTime;
            pinchDirectionPreviousRawPosition = currentCenter;

            if (centerSpeed < GetEntryDirectionCancelLowSpeedThreshold())
            {
                directionCancelLowSpeedMet = true;
                return Vector3.zero;
            }

            if (!pinchDirectionHasAcceptedPosition)
            {
                pinchDirectionLastAcceptedPosition = currentCenter;
                pinchDirectionHasAcceptedPosition = true;
                return Vector3.zero;
            }

            Vector3 acceptedMovement = currentCenter - pinchDirectionLastAcceptedPosition;
            pinchDirectionLastAcceptedPosition = currentCenter;
            if (acceptedMovement.sqrMagnitude <= 0.0000001f)
            {
                return Vector3.zero;
            }

            int entryWindowFrameCount = GetPinchDirectionWindowFrameCount();
            pinchDirectionCenterPositionHistory.Enqueue(currentCenter);
            while (pinchDirectionCenterPositionHistory.Count > entryWindowFrameCount)
            {
                pinchDirectionCenterPositionHistory.Dequeue();
            }

            if (pinchDirectionCenterPositionHistory.Count < entryWindowFrameCount)
            {
                return Vector3.zero;
            }

            Vector3[] entryPositions = pinchDirectionCenterPositionHistory.ToArray();
            Vector3 entryAverageDirection = Vector3.zero;
            int entryValidSegmentCount = 0;
            for (int i = 1; i < entryPositions.Length; i++)
            {
                Vector3 segment = entryPositions[i] - entryPositions[i - 1];
                if (segment.sqrMagnitude <= 0.0000001f)
                {
                    continue;
                }

                entryAverageDirection += segment.normalized;
                entryValidSegmentCount++;
            }

            return entryValidSegmentCount > 0
                ? entryAverageDirection / entryValidSegmentCount
                : Vector3.zero;
        }

        int windowFrameCount = GetPinchDirectionWindowFrameCount();
        pinchDirectionCenterPositionHistory.Enqueue(currentCenter);

        while (pinchDirectionCenterPositionHistory.Count > windowFrameCount)
        {
            pinchDirectionCenterPositionHistory.Dequeue();
        }

        Vector3[] positions = pinchDirectionCenterPositionHistory.ToArray();
        float currentSegmentSpeed = 0f;
        if (positions.Length >= 2)
        {
            Vector3 currentSegment = positions[positions.Length - 1] - positions[positions.Length - 2];
            currentSegmentSpeed = currentSegment.magnitude / Mathf.Max(Time.fixedDeltaTime, 0.000001f);
        }

        if (pinchDirectionCenterPositionHistory.Count < windowFrameCount)
        {
            if (IsEntryDirectionAxisMode())
            {
                centerSpeed = currentSegmentSpeed;
            }
            return Vector3.zero;
        }

        Vector3 averageDirection = Vector3.zero;
        float totalSegmentDistance = 0f;
        int validSegmentCount = 0;

        for (int i = 1; i < positions.Length; i++)
        {
            Vector3 segment = positions[i] - positions[i - 1];
            if (segment.sqrMagnitude <= 0.0000001f)
            {
                continue;
            }

            averageDirection += segment.normalized;
            totalSegmentDistance += segment.magnitude;
            validSegmentCount++;
        }

        if (validSegmentCount == 0)
        {
            return Vector3.zero;
        }

        float sampleDeltaTime = Mathf.Max(Time.fixedDeltaTime, 0.000001f);
        centerSpeed = IsEntryDirectionAxisMode()
            ? currentSegmentSpeed
            : (totalSegmentDistance / validSegmentCount) / sampleDeltaTime;
        return averageDirection / validSegmentCount;
    }

    private bool TryGetPinchDirectionCenterPoint(out Vector3 centerPoint)
    {
        centerPoint = Vector3.zero;
        bool hasIndexTip = HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Any, out MixedRealityPose indexTipPose);
        bool hasThumbTip = HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Any, out MixedRealityPose thumbTipPose);
        if (!hasIndexTip || !hasThumbTip)
        {
            return false;
        }

        centerPoint = (indexTipPose.Position + thumbTipPose.Position) * 0.5f;
        return true;
    }

    private Vector3 SmoothPinchDirectionDirection(Vector3 currentDirection)
    {
        if (!pinchDirectionHasSmoothedDirection)
        {
            pinchDirectionSmoothedDirection = currentDirection;
            pinchDirectionHasSmoothedDirection = true;
            return pinchDirectionSmoothedDirection;
        }

        float alpha = GetPinchDirectionSmoothingAlpha();
        pinchDirectionSmoothedDirection = Vector3.Slerp(pinchDirectionSmoothedDirection, currentDirection, alpha).normalized;
        return pinchDirectionSmoothedDirection;
    }

    private void RecordPinchDirectionDebug(
        string failReason,
        bool finalCancelPass,
        Vector3 windowMovement,
        Vector3 smoothedDirection,
        bool hasSmoothedDirection,
        Vector3 userFacingDirection,
        float angleDeg,
        bool anglePass,
        float centerSpeed,
        Vector3 ballCenter,
        Vector3 viewerPosition)
    {
        if (logWriter3 == null)
        {
            return;
        }

        int windowFrameCount = GetPinchDirectionWindowFrameCount();
        float minSpeed = GetPinchDirectionMinSpeedThreshold();
        bool speedPass = minSpeed <= 0f || centerSpeed >= minSpeed;
        bool windowReady = pinchDirectionCenterPositionHistory.Count >= windowFrameCount;
        Vector3 centerPosition = GetCurrentPinchDirectionCenterForDebug();

        logWriter3.WriteDirectionDebug(
            "S2Obstacles.pinchDirection",
            failReason,
            finalCancelPass,
            modeSelected,
            ballIndex,
            ballKind,
            handInBall,
            pinchDirectionReentryRequired,
            pinchDirectionCancelTriggered,
            pinchDirectionCenterPositionHistory.Count,
            windowFrameCount,
            windowReady,
            windowMovement,
            smoothedDirection,
            hasSmoothedDirection,
            userFacingDirection,
            angleDeg,
            GetPinchDirectionAngleThreshold(),
            anglePass,
            centerSpeed,
            minSpeed,
            speedPass,
            ballCenter,
            centerPosition,
            viewerPosition);
    }

    private Vector3 GetCurrentPinchDirectionCenterForDebug()
    {
        return TryGetPinchDirectionCenterPoint(out Vector3 centerPoint)
            ? centerPoint
            : Vector3.zero;
    }

    private int GetPinchDirectionWindowFrameCount()
    {
        if (ballSpawner == null)
        {
            return 10;
        }

        return Mathf.Max(2, ballSpawner.pinchDirectionWindowFrameCount);
    }

    private float GetPinchDirectionAngleThreshold()
    {
        if (IsEntryDirectionAxisMode())
        {
            return ballSpawner != null
                ? Mathf.Clamp(ballSpawner.entryDirectionConeHalfAngle, 0f, 180f)
                : 45f;
        }

        if (ballSpawner == null)
        {
            return 20f;
        }

        return Mathf.Clamp(ballSpawner.pinchDirectionCDAngleThreshold, 0f, 90f);
    }

    private float GetPinchDirectionSmoothingAlpha()
    {
        if (ballSpawner == null)
        {
            return 0.25f;
        }

        return Mathf.Clamp01(ballSpawner.pinchDirectionSmoothingAlpha);
    }

    private float GetPinchDirectionMinSpeedThreshold()
    {
        if (ballSpawner == null)
        {
            return 0.05f;
        }

        return Mathf.Max(0f, ballSpawner.pinchDirectionMinSpeedThreshold);
    }

    private bool IsEntryDirectionAxisMode()
    {
        if (ballSpawner == null || !ballSpawner.useEntryDirectionAxisForDirectionCancel)
        {
            return false;
        }

        return modeSelected == (int)BallSpawner.Mode.pinchDirection
            || modeSelected == (int)BallSpawner.Mode.dwellDirection
            || modeSelected == (int)BallSpawner.Mode.speedDirection
            || modeSelected == (int)BallSpawner.Mode.direction010
            || modeSelected == (int)BallSpawner.Mode.direction010Test;
    }

    private float GetEntryDirectionCancelLowSpeedThreshold()
    {
        return ballSpawner != null ? Mathf.Max(0f, ballSpawner.speedMinThreshold) : 0.05f;
    }

    private float GetEntryDirectionCancelHighSpeedThreshold()
    {
        if (ballSpawner == null)
        {
            return 0.05f;
        }

        return modeSelected == (int)BallSpawner.Mode.pinchDirection
            ? GetPinchDirectionMinSpeedThreshold()
            : GetDirection010MinSpeedThreshold();
    }

    private bool UpdateEntryDirectionCancelSpeedGate(float speed)
    {
        if (!IsEntryDirectionAxisMode())
        {
            return true;
        }

        return speed > GetEntryDirectionCancelHighSpeedThreshold();
    }

    private void CaptureDirectionCancelEntryAxisIfNeeded()
    {
        if (!IsEntryDirectionAxisMode()
            || directionCancelEntryAxisCaptured)
        {
            return;
        }

        // The Fitts ring is generated in the world XY plane at a fixed Z depth.
        // -Z is the one-sided normal from the ring toward the user.
        directionCancelEntryAxis = Vector3.back;
        directionCancelEntryAxisCaptured = true;
    }

    private static float GetDirectionAxisAngle(Vector3 movementDirection, Vector3 axisDirection)
    {
        return Vector3.Angle(movementDirection, axisDirection);
    }

    private void ResetPinchDirectionObstacleCancelState()
    {
        pinchDirectionCancelTriggered = false;
        pinchDirectionReentryRequired = false;
        directionCancelEntryAxisCaptured = false;
        directionCancelEntryAxis = Vector3.zero;
        ResetPinchDirectionObstacleTrackingWindow();
    }

    private void ResetPinchDirectionObstacleTrackingWindow()
    {
        pinchDirectionCenterPositionHistory.Clear();
        pinchDirectionHasSmoothedDirection = false;
        pinchDirectionSmoothedDirection = Vector3.zero;
        directionCancelLowSpeedMet = false;
        pinchDirectionHasRawPosition = false;
        pinchDirectionPreviousRawPosition = Vector3.zero;
        pinchDirectionHasAcceptedPosition = false;
        pinchDirectionLastAcceptedPosition = Vector3.zero;
    }

    private void ApplyDirectionCDArrowVisibility()
    {
        if (ballSpawner == null || !ballSpawner.showDirectionCDArrow || !IsDirectionCDArrowMode())
        {
            HideDirectionCDArrow();
            return;
        }

        if (directionCDArrowVisual == null)
        {
            directionCDArrowVisual = GetComponent<DirectionCDArrowVisual>();
            if (directionCDArrowVisual == null)
            {
                directionCDArrowVisual = gameObject.AddComponent<DirectionCDArrowVisual>();
            }
        }

        directionCDArrowVisual.Show(ballSpawner.transform);
    }

    private void HideDirectionCDArrow()
    {
        if (directionCDArrowVisual != null)
        {
            directionCDArrowVisual.Hide();
        }
    }

    private bool IsDirectionCDArrowMode()
    {
        return modeSelected == (int)BallSpawner.Mode.pinchDirection
            || modeSelected == (int)BallSpawner.Mode.dwellDirection
            || modeSelected == (int)BallSpawner.Mode.speedDirection
            || modeSelected == (int)BallSpawner.Mode.direction010
            || modeSelected == (int)BallSpawner.Mode.direction010Test;
    }

    private void HandlePinchThumbSwipeObstacleEnter(bool enteredByIndex)
    {
        if (pinchThumbSwipeReentryRequired || triggered)
        {
            ballSpawner.area_type = triggered ? 4 : 3;
            ballSpawner.index = -1;
            return;
        }

        bool firstContact = !HasAnyPinchContact();
        if (enteredByIndex)
        {
            inBall = true;
        }
        else
        {
            inBall2 = true;
        }

        if (firstContact)
        {
            Player.TurnShallow();
            if (!pinchThumbSwipeTracking)
            {
                ballSpawner.errorInTimes++;
            }
        }

        ballSpawner.area_type = 2;
        ballSpawner.index = ballIndex;


    }

    private void HandlePinchThumbSwipeObstacleExit(bool exitedByIndex)
    {
        if (exitedByIndex)
        {
            inBall = false;
        }
        else
        {
            inBall2 = false;
        }

        if (HasAnyPinchContact()
            && !pinchThumbSwipeReentryRequired
            && !triggered
            && !pinchThumbSwipeCanceled)
        {
            ballSpawner.area_type = 2;
            ballSpawner.index = ballIndex;
            return;
        }

        if (pinchThumbSwipeReentryRequired)
        {
            if (!HasAnyPinchContact())
            {
                // A canceled gesture is released only after both contact
                // points have left the obstacle.
                ClearPinchThumbSwipeReentry();
            }
            else
            {
                // Keep the canceled visual state while the other contact
                // point is still inside.
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
            }
            return;
        }

        if (triggered || pinchThumbSwipeCanceled)
        {
            ResetPinchThumbSwipeTracking();
            return;
        }

        ResetPinchThumbSwipeTracking();
        ballSpawner.area_type = 3;
        ballSpawner.index = -1;
        oldTime = Time.time;
        if (!triggered)
        {
            Player.TurnYellowBack();
        }
        ballSpawner.triggered = false;
    }

    private bool TryCapturePinchThumbSwipeStart()
    {
        if (!HasAnyPinchContact())
        {
            return false;
        }

        if (!TryGetPinchThumbSwipeData(out _, out Vector3 thumbPosition, out _, out _, out _, out float thumbProjectionOnIndex))
        {
            return false;
        }

        pinchThumbSwipeStartThumbPosition = thumbPosition;
        pinchThumbSwipeStartProjection = thumbProjectionOnIndex;
        UpdatePinchThumbSwipeContactDistance();
        return true;
    }

    private bool UpdatePinchThumbSwipeContactDistance()
    {
        if (!pinchThumbSwipeTracking)
        {
            return false;
        }

        if (!TryGetPinchThumbSwipeData(out _, out _, out _, out float contactDistance, out _, out _))
        {
            pinchThumbSwipeCurrentContactDistance = float.MaxValue;
            return false;
        }

        pinchThumbSwipeCurrentContactDistance = contactDistance;
        return true;
    }

    private void ResolvePinchThumbSwipeObstacleEnd()
    {
        if (!HasAnyPinchContact())
        {
            ResetPinchThumbSwipeTracking();
            ballSpawner.area_type = 3;
            ballSpawner.index = -1;
            oldTime = Time.time;
            if (!triggered)
            {
                Player.TurnYellowBack();
            }
            ballSpawner.triggered = false;
            return;
        }

        if (!UpdatePinchThumbSwipeContactDistance())
        {
            ResetPinchThumbSwipeTracking();
            return;
        }

        float contactThreshold = Mathf.Max(0f, ballSpawner.pinchThumbSwipeContactDistanceThreshold);
        if (pinchThumbSwipeCurrentContactDistance > contactThreshold)
        {
            TriggerPinchThumbSwipeObstacle();
        }
        else
        {
            CancelPinchThumbSwipeObstacle();
        }
    }

    private bool TryGetPinchThumbSwipeData(
        out Vector3 indexTip,
        out Vector3 thumbTip,
        out Vector3 indexAxis,
        out float thumbDistanceToIndex,
        out Vector3 closestPointOnIndex,
        out float thumbProjectionOnIndex)
    {
        indexTip = Vector3.zero;
        thumbTip = Vector3.zero;
        indexAxis = Vector3.zero;
        thumbDistanceToIndex = float.MaxValue;
        closestPointOnIndex = Vector3.zero;
        thumbProjectionOnIndex = 0f;

        bool hasIndexTip = HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Any, out MixedRealityPose indexTipPose);
        bool hasIndexDistal = HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexDistalJoint, Handedness.Any, out MixedRealityPose indexDistalPose);
        bool hasIndexMiddle = HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexMiddleJoint, Handedness.Any, out MixedRealityPose indexMiddlePose);
        bool hasThumbTip = HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Any, out MixedRealityPose thumbTipPose);

        if (!hasIndexTip || !hasThumbTip)
        {
            return false;
        }

        indexTip = indexTipPose.Position;
        thumbTip = thumbTipPose.Position;
        Vector3 indexBase = hasIndexDistal ? indexDistalPose.Position : (hasIndexMiddle ? indexMiddlePose.Position : indexTip);
        Vector3 indexSegment = indexTip - indexBase;
        if (indexSegment.sqrMagnitude < 0.000001f)
        {
            return false;
        }

        indexAxis = indexSegment.normalized;
        float segmentLength = indexSegment.magnitude;
        thumbProjectionOnIndex = Vector3.Dot(thumbTip - indexBase, indexAxis);
        // Extend the index axis past the distal joint toward the palm, while
        // keeping the projection capped at the fingertip end.
        float t = Mathf.Min(thumbProjectionOnIndex, segmentLength);
        closestPointOnIndex = indexBase + indexAxis * t;
        thumbDistanceToIndex = Vector3.Distance(thumbTip, closestPointOnIndex);
        return true;
    }

    private void TriggerPinchThumbSwipeObstacle()
    {
        if (triggered)
        {
            return;
        }

        pinchThumbSwipeReentryRequired = true;
        triggered = true;
        Player.TurnYellow();
        source.Play();
        ballSpawner.triggeredBy = inBall ? "index" : "thumb";
        ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
        ResetPinchThumbSwipeTracking();
    }

    private void CancelPinchThumbSwipeObstacle()
    {
        pinchThumbSwipeCanceled = true;
        pinchThumbSwipeReentryRequired = true;
        canceled = true;
        Acce.SetInvoked();
        Player.TurnShallowBack();
        ballSpawner.area_type = 3;
        ballSpawner.index = -1;
        ResetPinchThumbSwipeTracking();
    }

    private void ResetPinchThumbSwipeState()
    {
        pinchThumbSwipeTracking = false;
        pinchThumbSwipeCanceled = false;
        pinchThumbSwipeReentryRequired = false;
        pinchThumbSwipeStartThumbPosition = Vector3.zero;
        pinchThumbSwipeStartProjection = 0f;
        pinchThumbSwipeCurrentContactDistance = float.MaxValue;
        canceled = false;
    }

    private void ResetPinchThumbSwipeTracking()
    {
        pinchThumbSwipeTracking = false;
        pinchThumbSwipeStartThumbPosition = Vector3.zero;
        pinchThumbSwipeStartProjection = 0f;
        pinchThumbSwipeCurrentContactDistance = float.MaxValue;
    }

    private void ClearPinchThumbSwipeReentry()
    {
        pinchThumbSwipeCanceled = false;
        pinchThumbSwipeReentryRequired = false;
        canceled = false;
        Acce.UnInvoked();
        ResetPinchThumbSwipeTracking();
        Player.TurnYellowBack();
        ballSpawner.area_type = 3;
        ballSpawner.index = -1;
    }

    private void CheckPinchWristTiltCancel()
    {
        if (IsPinchWristTiltPastThreshold())
        {
            CancelPinchWristTiltObstacle();
        }
    }

    private void UpdateDwellWristTiltObstacle()
    {
        if (IsPinchWristTiltPastThreshold())
        {
            CancelPinchWristTiltObstacle();
            return;
        }

        if (HasDwellWristTiltTimedOut())
        {
            TriggerDwellWristTiltObstacle();
        }
    }

    private bool IsPinchWristTiltPastThreshold()
    {
        float threshold = Mathf.Max(0f, ballSpawner.pinchWristTiltCancelAngleThreshold);
        if (threshold > 180f || !pinchWristTiltHasEntryRotation)
        {
            return false;
        }

        if (!TryGetWristRotation(out Quaternion currentRotation))
        {
            return false;
        }

        return Quaternion.Angle(pinchWristTiltEntryRotation, currentRotation) >= threshold;
    }

    private bool TryGetWristRotation(out Quaternion wristRotation)
    {
        wristRotation = Quaternion.identity;

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Wrist, Handedness.Any, out MixedRealityPose wristPose))
        {
            wristRotation = wristPose.Rotation;
            return true;
        }

        return false;
    }

    private void TriggerPinchWristTiltObstacle()
    {
        TriggerPinchWristTiltObstacle('P');
    }

    private void TriggerWristTiltObstacle()
    {
        TriggerPinchWristTiltObstacle('W');
    }

    private void TriggerDwellWristTiltObstacle()
    {
        TriggerPinchWristTiltObstacle('D');
    }

    private void TriggerPinchWristTiltObstacle(char triggerCondition)
    {
        if (triggered)
        {
            return;
        }

        pinchWristTiltTriggered = true;
        pinchWristTiltReentryRequired = true;
        triggered = true;
        Player.TurnYellow();
        source.Play();
        ballSpawner.triggeredBy = "index";
        ballSpawner.triggerCondition = triggerCondition;
        ballSpawner.RegisterObstacleErrorTrigger(ballIndex);
        ResetPinchWristTiltTracking();
    }

    private void CancelPinchWristTiltObstacle()
    {
        if (pinchWristTiltCanceled || pinchWristTiltTriggered)
        {
            return;
        }

        pinchWristTiltCanceled = true;
        pinchWristTiltReentryRequired = true;
        canceled = true;
        Acce.SetInvoked();
        Player.TurnShallowBack();
        ballSpawner.area_type = 3;
        ballSpawner.index = -1;
        ResetPinchWristTiltTracking();
    }

    private void ResetPinchWristTiltState()
    {
        pinchWristTiltInside = false;
        pinchWristTiltTriggered = false;
        pinchWristTiltCanceled = false;
        pinchWristTiltReentryRequired = false;
        pinchWristTiltPinching = false;
        pinchWristTiltEntryTimestamp = -1f;
        pinchWristTiltEntryRotation = Quaternion.identity;
        pinchWristTiltHasEntryRotation = false;
        canceled = false;
    }

    private void ResetPinchWristTiltTracking()
    {
        pinchWristTiltPinching = false;
        pinchWristTiltEntryTimestamp = -1f;
        pinchWristTiltEntryRotation = Quaternion.identity;
        pinchWristTiltHasEntryRotation = false;
    }

    private void ClearPinchWristTiltReentry()
    {
        pinchWristTiltTriggered = false;
        pinchWristTiltCanceled = false;
        pinchWristTiltReentryRequired = false;
        ResetPinchWristTiltTracking();
        canceled = false;
        Acce.UnInvoked();
        Player.TurnYellowBack();
        ballSpawner.area_type = 3;
        ballSpawner.index = -1;
    }

    private bool HasDwellWristTiltTimedOut()
    {
        float waitTime = ballSpawner != null ? Mathf.Max(0f, ballSpawner.longPressTimeThreshold) : 0.5f;
        return pinchWristTiltEntryTimestamp >= 0f
            && Time.time - pinchWristTiltEntryTimestamp >= waitTime;
    }

    private ReleaseGesture EnsureReleaseGesture()
    {
        // Keep the runtime detector synchronized with the Inspector threshold on BallSpawner.
        if (releaseGesture == null)
        {
            releaseGesture = GetComponent<ReleaseGesture>();
            if (releaseGesture == null)
            {
                releaseGesture = gameObject.AddComponent<ReleaseGesture>();
            }
        }

        if (ballSpawner != null)
        {
            releaseGesture.distanceDeltaThreshold = ballSpawner.releaseDistanceDeltaThreshold;
        }

        return releaseGesture;
    }

    private void ApplyPinchSettings()
    {
        if (pinchob != null && ballSpawner != null)
        {
            pinchob.DeactivateDistance = Mathf.Max(pinchob.ActivateDistance, ballSpawner.pinchEndDistanceThreshold);
        }
    }

    private void ResetReleaseGestureTracking()
    {
        // Reset the previous-frame distance when a new entry/exit cycle starts.
        EnsureReleaseGesture().ResetGesture();
    }

    private bool HasDwellReleaseTimedOut()
    {
        float waitTime = Acce != null && Acce.WaitTime > 0f ? Acce.WaitTime : 0.5f;
        return dwellReleaseEntryTimestamp >= 0f
            && Time.time - dwellReleaseEntryTimestamp >= waitTime;
    }

    private void CheckDwellReleaseCancel()
    {
        if (EnsureReleaseGesture().CheckRelease())
        {
            TriggerDwellReleaseCancel();
        }
    }

    private void TriggerDwellReleaseCancel()
    {
        if (dwellReleaseCancelTriggered || dwellReleaseReentryRequired)
        {
            return;
        }

        dwellReleaseCancelTriggered = true;
        dwellReleaseReentryRequired = true;
        dwellReleaseInside = false;
        dwellReleaseEntryTimestamp = -1f;
        handInBall2 = false;
        canceled = true;

        Acce.SetInvoked();
        ResetReleaseGestureTracking();
        Player.TurnShallowBack();
        ballSpawner.area_type = 3;
        ballSpawner.index = -1;
    }

    private void ResetDwellReleaseState()
    {
        dwellReleaseCancelTriggered = false;
        dwellReleaseReentryRequired = false;
        dwellReleaseInside = false;
        dwellReleaseEntryTimestamp = -1f;
        canceled = false;
        Acce?.UnInvoked();
        ResetReleaseGestureTracking();
    }

    private bool HasDwellWithdrawTimedOut()
    {
        float waitTime = Acce != null && Acce.WaitTime > 0f ? Acce.WaitTime : 0.5f;
        return dwellWithdrawEntryTimestamp >= 0f
            && Time.time - dwellWithdrawEntryTimestamp >= waitTime;
    }

    private void CheckDwellWithdrawCancel()
    {
        Vector3 currentDirection = dwellWithdrawCurrentVelocityDirection;
        if (dwellWithdrawEntryVelocityDirection == Vector3.zero || currentDirection == Vector3.zero)
        {
            return;
        }

        if (ballSpawner.dwellWithdrawCancelAngleThreshold > 180f)
        {
            return;
        }

        float cancelAngle = Mathf.Max(0f, ballSpawner.dwellWithdrawCancelAngleThreshold);
        if (Vector3.Angle(dwellWithdrawEntryVelocityDirection, currentDirection) >= cancelAngle)
        {
            TriggerDwellWithdrawCancel();
        }
    }

    private void TriggerDwellWithdrawCancel()
    {
        if (dwellWithdrawCancelTriggered || dwellWithdrawReentryRequired)
        {
            return;
        }

        dwellWithdrawCancelTriggered = true;
        dwellWithdrawReentryRequired = true;
        dwellWithdrawInside = false;
        dwellWithdrawEntryTimestamp = -1f;
        dwellWithdrawEntryVelocityDirection = Vector3.zero;
        dwellWithdrawCurrentVelocityDirection = Vector3.zero;
        dwellWithdrawPositionHistory.Clear();
        handInBall2 = false;
        canceled = true;

        Acce.SetInvoked();
        Player.TurnShallowBack();
        ballSpawner.area_type = 3;
        ballSpawner.index = -1;
    }

    private void ResetDwellWithdrawState()
    {
        dwellWithdrawCancelTriggered = false;
        dwellWithdrawReentryRequired = false;
        dwellWithdrawInside = false;
        dwellWithdrawEntryTimestamp = -1f;
        dwellWithdrawEntryVelocityDirection = Vector3.zero;
        dwellWithdrawCurrentVelocityDirection = Vector3.zero;
        dwellWithdrawPositionHistory.Clear();
        canceled = false;
        Acce?.UnInvoked();
    }

    private void RecordDwellWithdrawFingerPosition()
    {
        if (Acce == null)
        {
            return;
        }

        Vector3 currentPosition = Acce.IndexTipPose2.Position;
        dwellWithdrawPositionHistory.Enqueue(currentPosition);

        while (dwellWithdrawPositionHistory.Count > 3)
        {
            dwellWithdrawPositionHistory.Dequeue();
        }

        if (dwellWithdrawPositionHistory.Count < 3)
        {
            dwellWithdrawCurrentVelocityDirection = Vector3.zero;
            return;
        }

        Vector3 oldestPosition = dwellWithdrawPositionHistory.Peek();
        Vector3 direction = currentPosition - oldestPosition;
        dwellWithdrawCurrentVelocityDirection = direction.sqrMagnitude > 0f
            ? direction.normalized
            : Vector3.zero;
    }

    private Vector3 GetCurrentIndexVelocityDirection()
    {
        if (Acce == null)
        {
            return Vector3.zero;
        }

        if (Acce.IndexVelocityDirection != Vector3.zero)
        {
            return Acce.IndexVelocityDirection;
        }

        Vector3 fallbackDelta = Acce.IndexTipPose2.Position - (Acce.HasSurfaceEntryPoint ? Acce.SurfaceEntryPoint : Acce.IndexTipPose1.Position);
        return fallbackDelta.sqrMagnitude > 0f ? fallbackDelta.normalized : Vector3.zero;
    }

    private Vector3 GetUserFacingDirection()
    {
        Transform viewerTransform = Camera.main != null ? Camera.main.transform : null;
        if (viewerTransform == null)
        {
            GameObject centerEyeAnchor = GameObject.Find("CenterEyeAnchor");
            if (centerEyeAnchor != null)
            {
                viewerTransform = centerEyeAnchor.transform;
            }
        }

        return viewerTransform != null ? viewerTransform.forward.normalized : Vector3.forward;
    }

    private void CheckReleaseTapCancel()
    {
        // ReleaseGesture returns true when the per-frame finger-distance increase exceeds the threshold.
        if (EnsureReleaseGesture().CheckRelease())
        {
            TriggerFastTapCancel();
        }
    }

    private void TriggerRainbowCancel()
    {
        if (rainbowCancelTriggered || !handInBall || !ballSpawner.enable_trigger)
        {
            return;
        }

        rainbowCancelTriggered = true;
        rainbowConfirmBlocked = true;
        rainbowReentryRequiresExit = true;
        canceled = true;
        handInBall = false;
        handInBall2 = false;
        inBall = false;
        rainbowArcInitialized = false;
        rainbowEntryCaptured = false;
        ballSpawner.isReact = false;
        Player.hideCircle();
        Player.ChangeOpacityBack();
        HideRainbowArc();
    }

    private void ConfigureContact010Frame()
    {
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            return;
        }

        Vector3 ballCenter = collider.bounds.center;
        float ballRadius = Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.y, collider.bounds.extents.z);
        Vector3 normal = GetViewerBackDirection(ballCenter);
        if (normal.sqrMagnitude < 0.000001f)
        {
            normal = Vector3.forward;
        }

        rainbowArcCenter = ballCenter;
        rainbowArcRadius = ballRadius;
        rainbowArcNormal = normal.normalized;

        Vector3 tangent = Vector3.Cross(rainbowArcNormal, Vector3.up);
        if (tangent.sqrMagnitude < 0.000001f)
        {
            tangent = Vector3.Cross(rainbowArcNormal, Vector3.right);
        }

        tangent.Normalize();
        rainbowFrameTangent = tangent;
        rainbowFrameBitangent = Vector3.Cross(rainbowArcNormal, tangent).normalized;
        rainbowFrameHalfExtent = rainbowArcRadius * 1.1f;
        rainbowArcInitialized = true;

        if (ballSpawner != null && ballSpawner.showContact010TriggerRegion)
        {
            ShowContact010TriggerRegion();
        }
        else
        {
            HideRainbowArc();
        }
    }

    private void ShowContact010TriggerRegion()
    {
        if (rainbowArcRadius <= 0f
            || rainbowArcNormal.sqrMagnitude < 0.000001f
            || rainbowFrameTangent.sqrMagnitude < 0.000001f
            || rainbowFrameBitangent.sqrMagnitude < 0.000001f)
        {
            return;
        }

        HideRainbowArc();
        rainbowArcActive = true;
        rainbowArcRoot = new GameObject("Contact010TriggerRegionLines");

        float shellRadius = rainbowArcRadius * 1.01f;
        Vector3 frameCenter = rainbowArcCenter + rainbowArcNormal * shellRadius;
        Vector3 right = rainbowFrameTangent * rainbowFrameHalfExtent;
        Vector3 up = rainbowFrameBitangent * rainbowFrameHalfExtent;
        Vector3 topRight = frameCenter + right + up;
        Vector3 topLeft = frameCenter - right + up;
        Vector3 bottomLeft = frameCenter - right - up;
        Vector3 bottomRight = frameCenter + right - up;
        Color lineColor = Color.green;

        CreateContact010TriggerRegionEdge("Contact010TriggerTop", topLeft, topRight, shellRadius, lineColor);
        CreateContact010TriggerRegionEdge("Contact010TriggerRight", topRight, bottomRight, shellRadius, lineColor);
        CreateContact010TriggerRegionEdge("Contact010TriggerBottom", bottomRight, bottomLeft, shellRadius, lineColor);
        CreateContact010TriggerRegionEdge("Contact010TriggerLeft", bottomLeft, topLeft, shellRadius, lineColor);
    }

    private void CreateContact010TriggerRegionEdge(string edgeName, Vector3 startPoint, Vector3 endPoint, float shellRadius, Color color)
    {
        if (rainbowArcRoot == null)
        {
            return;
        }

        const int SegmentCount = 24;
        GameObject edge = new GameObject(edgeName);
        edge.transform.SetParent(rainbowArcRoot.transform, true);

        LineRenderer lineRenderer = edge.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = SegmentCount + 1;
        lineRenderer.widthMultiplier = Mathf.Max(0.0015f, rainbowArcRadius * 0.025f);
        lineRenderer.numCornerVertices = 2;
        lineRenderer.numCapVertices = 2;

        Shader shader = Shader.Find("Unlit/Color");
        Material material = new Material(shader != null ? shader : Shader.Find("Standard"));
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        else
        {
            material.color = color;
        }
        lineRenderer.material = material;
        lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        for (int i = 0; i <= SegmentCount; i++)
        {
            float t = i / (float)SegmentCount;
            Vector3 framePoint = Vector3.Lerp(startPoint, endPoint, t);
            Vector3 direction = framePoint - rainbowArcCenter;
            Vector3 surfacePoint = direction.sqrMagnitude > 0.000001f
                ? rainbowArcCenter + direction.normalized * shellRadius
                : framePoint;
            lineRenderer.SetPosition(i, surfacePoint);
        }
    }

    private bool IsContact010ExitInUserFacingFrame()
    {
        if (!rainbowArcInitialized)
        {
            ConfigureContact010Frame();
        }

        if (rainbowArcRadius <= 0f || rainbowArcNormal.sqrMagnitude < 0.000001f)
        {
            return false;
        }

        float shellRadius = rainbowArcRadius * 1.01f;
        Vector3 currentFingerPosition = Acce.IndexTipPose2.Position;
        Vector3 previousFingerPosition = GetContact010PreviousIndexPosition(currentFingerPosition);
        Vector3 crossingDirection = GetContact010CrossingDirection(previousFingerPosition, currentFingerPosition, shellRadius);
        return IsRainbowCancelDirectionInFrame(crossingDirection, shellRadius);
    }

    private Vector3 GetContact010PreviousIndexPosition(Vector3 currentFingerPosition)
    {
        if (Acce.IndexVelocity.sqrMagnitude <= 0.0000001f)
        {
            return currentFingerPosition;
        }

        return currentFingerPosition - Acce.IndexVelocity * Time.fixedDeltaTime;
    }

    private Vector3 GetContact010CrossingDirection(Vector3 previousFingerPosition, Vector3 currentFingerPosition, float shellRadius)
    {
        Vector3 previousVector = previousFingerPosition - rainbowArcCenter;
        Vector3 currentVector = currentFingerPosition - rainbowArcCenter;
        float previousDistance = previousVector.magnitude;
        float currentDistance = currentVector.magnitude;

        if (!Mathf.Approximately(currentDistance, previousDistance))
        {
            float t = (shellRadius - previousDistance) / (currentDistance - previousDistance);
            if (t >= 0f && t <= 1f)
            {
                Vector3 crossingPoint = Vector3.Lerp(previousFingerPosition, currentFingerPosition, t);
                Vector3 crossingVector = crossingPoint - rainbowArcCenter;
                if (crossingVector.sqrMagnitude > 0.000001f)
                {
                    return crossingVector.normalized;
                }
            }
        }

        Vector3 fallbackVector = currentVector.sqrMagnitude > 0.000001f ? currentVector : previousVector;
        return fallbackVector.sqrMagnitude > 0.000001f ? fallbackVector.normalized : Vector3.zero;
    }

    private void ResetContact010ObstacleState()
    {
        handInBall = false;
        handInBall2 = false;
        inBall = false;
        canceled = false;
        ResetRainbowState();
    }

    private void TriggerDirection010ObstacleCancel()
    {
        if (direction010CancelTriggered)
        {
            return;
        }

        LogSpeedStudy("cancel", "direction");
        direction010CancelTriggered = true;
        direction010ReentryRequired = true;
        canceled = true;
        handInBall = false;
        handInBall2 = false;
        inBall = false;
        ballSpawner.area_type = 3;
        ballSpawner.index = -1;
        Player.TurnShallowBack();
    }
    private bool IsDirection010CancelMovement()
    {
        return IsDirection010CancelMovement(out _);
    }

    private bool IsDirection010CancelMovement(out bool directionDecisionReady)
    {
        directionDecisionReady = false;
        if (ballSpawner == null || Acce == null)
        {
            return false;
        }

        bool entryDirectionAxisMode = IsEntryDirectionAxisMode();
        CaptureDirectionCancelEntryAxisIfNeeded();
        Collider collider = GetComponent<Collider>();
        Vector3 ballCenter = collider != null ? collider.bounds.center : transform.position;
        Vector3 viewerPosition = GetViewerPositionForDirectionDebug();
        float currentSpeed = Acce.IndexVelocity.magnitude;
        Vector3 windowMovement = GetDirection010WindowMovement();
        bool speedGatePass = !entryDirectionAxisMode || UpdateEntryDirectionCancelSpeedGate(currentSpeed);
        if (windowMovement.sqrMagnitude <= 0.0000001f)
        {
            RecordDirection010Debug(
                entryDirectionAxisMode && currentSpeed < GetEntryDirectionCancelLowSpeedThreshold()
                    ? "entry_direction_frame_skipped_low_speed"
                    : entryDirectionAxisMode && !speedGatePass
                        ? "direction_speed_below_threshold"
                    : direction010PositionHistory.Count < GetDirection010WindowFrameCount() ? "not_ready_window" : "zero_window_movement",
                false,
                windowMovement,
                Vector3.zero,
                false,
                entryDirectionAxisMode ? directionCancelEntryAxis : Vector3.zero,
                float.NaN,
                false,
                ballCenter,
                viewerPosition);
            return false;
        }

        float minSpeed = GetDirection010MinSpeedThreshold();
        if (entryDirectionAxisMode)
        {
            if (!speedGatePass)
            {
                RecordDirection010Debug(
                    "direction_speed_below_threshold",
                    false,
                    windowMovement,
                    Vector3.zero,
                    false,
                    directionCancelEntryAxis,
                    float.NaN,
                    false,
                    ballCenter,
                    viewerPosition);
                return false;
            }
        }
        else if (minSpeed > 0f && Acce.IndexVelocity.sqrMagnitude < minSpeed * minSpeed)
        {
            RecordDirection010Debug(
                "speed_too_low",
                false,
                windowMovement,
                Vector3.zero,
                false,
                Vector3.zero,
                float.NaN,
                false,
                ballCenter,
                viewerPosition);
            return false;
        }

        Vector3 referenceDirection = entryDirectionAxisMode
            ? directionCancelEntryAxis
            : -GetViewerBackDirection(ballCenter);
        if (referenceDirection.sqrMagnitude <= 0.0000001f)
        {
            RecordDirection010Debug(
                entryDirectionAxisMode ? "no_entry_direction_axis" : "no_viewer_direction",
                false,
                windowMovement,
                Vector3.zero,
                false,
                referenceDirection,
                float.NaN,
                true,
                ballCenter,
                viewerPosition);
            return false;
        }

        Vector3 currentDirection = entryDirectionAxisMode
            ? windowMovement.normalized
            : SmoothDirection010Direction(windowMovement.normalized);
        Vector3 comparisonDirection = Vector3.zero;
        Vector3 comparisonReferenceDirection = Vector3.zero;
        if (entryDirectionAxisMode)
        {
            comparisonDirection = currentDirection.normalized;
            comparisonReferenceDirection = referenceDirection.normalized;
        }
        else
        {
            bool hasSmoothedHorizontal = TryProjectHorizontalDirection(currentDirection, out comparisonDirection);
            bool hasReferenceHorizontal = TryProjectHorizontalDirection(referenceDirection, out comparisonReferenceDirection);
            if (!hasSmoothedHorizontal || !hasReferenceHorizontal)
            {
                RecordDirection010Debug(
                    "zero_horizontal_direction",
                    false,
                    windowMovement,
                    comparisonDirection,
                    true,
                    comparisonReferenceDirection,
                    float.NaN,
                    false,
                    ballCenter,
                    viewerPosition);
                return false;
            }
        }

        float angle = entryDirectionAxisMode
            ? GetDirectionAxisAngle(comparisonDirection, comparisonReferenceDirection)
            : Vector3.Angle(comparisonDirection, comparisonReferenceDirection);
        directionDecisionReady = true;
        bool anglePass = entryDirectionAxisMode
            ? angle <= GetDirection010AngleThreshold()
            : angle <= GetDirection010AngleThreshold();
        RecordDirection010Debug(
            entryDirectionAxisMode
                ? (anglePass ? "inside_cancel_cone" : "angle_outside_cancel_cone")
                : (anglePass ? "pass" : "angle_too_large"),
            anglePass,
            windowMovement,
            comparisonDirection,
            true,
            comparisonReferenceDirection,
            angle,
            anglePass,
            ballCenter,
            viewerPosition);
        return anglePass;
    }

    private Vector3 SmoothDirection010Direction(Vector3 currentDirection)
    {
        if (!direction010HasSmoothedDirection)
        {
            direction010SmoothedDirection = currentDirection;
            direction010HasSmoothedDirection = true;
            return direction010SmoothedDirection;
        }

        float alpha = GetDirection010SmoothingAlpha();
        // alpha 越小越平滑但反应越慢；越大越接近当前窗口方向。
        direction010SmoothedDirection = Vector3.Slerp(direction010SmoothedDirection, currentDirection, alpha).normalized;
        return direction010SmoothedDirection;
    }

    private Vector3 GetDirection010WindowMovement()
    {
        Vector3 currentPosition = Acce.IndexTipPose2.Position;
        if (IsEntryDirectionAxisMode())
        {
            if (!direction010HasAcceptedPosition)
            {
                direction010LastAcceptedPosition = Acce.IndexTipPose1.Position;
                direction010HasAcceptedPosition = true;
            }

            if (Acce.IndexVelocity.magnitude < GetEntryDirectionCancelLowSpeedThreshold())
            {
                directionCancelLowSpeedMet = true;
                return Vector3.zero;
            }

            Vector3 currentMovement = currentPosition - direction010LastAcceptedPosition;
            direction010LastAcceptedPosition = currentPosition;
            if (currentMovement.sqrMagnitude <= 0.0000001f)
            {
                return Vector3.zero;
            }

            int entryWindowFrameCount = GetDirection010WindowFrameCount();
            direction010PositionHistory.Enqueue(currentPosition);
            while (direction010PositionHistory.Count > entryWindowFrameCount)
            {
                direction010PositionHistory.Dequeue();
            }

            if (direction010PositionHistory.Count < entryWindowFrameCount)
            {
                return Vector3.zero;
            }

            Vector3[] entryPositions = direction010PositionHistory.ToArray();
            Vector3 entryAverageDirection = Vector3.zero;
            int entryValidSegmentCount = 0;
            for (int i = 1; i < entryPositions.Length; i++)
            {
                Vector3 segment = entryPositions[i] - entryPositions[i - 1];
                if (segment.sqrMagnitude <= 0.0000001f)
                {
                    continue;
                }

                entryAverageDirection += segment.normalized;
                entryValidSegmentCount++;
            }

            return entryValidSegmentCount > 0
                ? entryAverageDirection / entryValidSegmentCount
                : Vector3.zero;
        }

        int windowFrameCount = GetDirection010WindowFrameCount();
        direction010PositionHistory.Enqueue(currentPosition);

        while (direction010PositionHistory.Count > windowFrameCount)
        {
            direction010PositionHistory.Dequeue();
        }

        if (direction010PositionHistory.Count < windowFrameCount)
        {
            return Vector3.zero;
        }

        // Average per-segment directions instead of relying on a single oldest/current endpoint pair.
        Vector3[] positions = direction010PositionHistory.ToArray();
        Vector3 averageDirection = Vector3.zero;
        int validSegmentCount = 0;

        for (int i = 1; i < positions.Length; i++)
        {
            Vector3 segment = positions[i] - positions[i - 1];
            if (segment.sqrMagnitude <= 0.0000001f)
            {
                continue;
            }

            averageDirection += segment.normalized;
            validSegmentCount++;
        }

        if (validSegmentCount == 0)
        {
            return Vector3.zero;
        }

        return averageDirection / validSegmentCount;
    }

    private void RecordDirection010Debug(
        string failReason,
        bool finalCancelPass,
        Vector3 windowMovement,
        Vector3 smoothedDirection,
        bool hasSmoothedDirection,
        Vector3 userFacingDirection,
        float angleDeg,
        bool anglePass,
        Vector3 ballCenter,
        Vector3 viewerPosition)
    {
        if (logWriter3 == null || Acce == null)
        {
            return;
        }

        int windowFrameCount = GetDirection010WindowFrameCount();
        float minSpeed = GetDirection010MinSpeedThreshold();
        float indexSpeed = Acce.IndexVelocity.magnitude;
        bool speedPass = minSpeed <= 0f || indexSpeed >= minSpeed;
        bool windowReady = direction010PositionHistory.Count >= windowFrameCount;
        Vector3 indexPosition = Acce.IndexTipPose2.Position;

        logWriter3.WriteDirectionDebug(
            "S2Obstacles",
            failReason,
            finalCancelPass,
            modeSelected,
            ballIndex,
            ballKind,
            handInBall,
            direction010ReentryRequired,
            direction010CancelTriggered,
            direction010PositionHistory.Count,
            windowFrameCount,
            windowReady,
            windowMovement,
            smoothedDirection,
            hasSmoothedDirection,
            userFacingDirection,
            angleDeg,
            GetDirection010AngleThreshold(),
            anglePass,
            indexSpeed,
            minSpeed,
            speedPass,
            ballCenter,
            indexPosition,
            viewerPosition);
    }

    private Vector3 GetViewerPositionForDirectionDebug()
    {
        Transform viewerTransform = Camera.main != null ? Camera.main.transform : null;
        if (viewerTransform == null)
        {
            GameObject centerEyeAnchor = GameObject.Find("CenterEyeAnchor");
            if (centerEyeAnchor != null)
            {
                viewerTransform = centerEyeAnchor.transform;
            }
        }

        return viewerTransform != null ? viewerTransform.position : Vector3.zero;
    }

    private bool TryProjectHorizontalDirection(Vector3 direction, out Vector3 horizontalDirection)
    {
        horizontalDirection = new Vector3(direction.x, 0f, direction.z);
        if (horizontalDirection.sqrMagnitude <= 0.0000001f)
        {
            horizontalDirection = Vector3.zero;
            return false;
        }

        horizontalDirection.Normalize();
        return true;
    }

    private int GetDirection010WindowFrameCount()
    {
        if (ballSpawner == null)
        {
            return 10;
        }

        return Mathf.Max(
            2,
            IsDirection010TestMode()
                ? ballSpawner.direction010TestWindowFrameCount
                : ballSpawner.direction010WindowFrameCount);
    }

    private float GetDirection010AngleThreshold()
    {
        if (IsEntryDirectionAxisMode())
        {
            return ballSpawner != null
                ? Mathf.Clamp(ballSpawner.entryDirectionConeHalfAngle, 0f, 180f)
                : 45f;
        }

        if (ballSpawner == null)
        {
            return 20f;
        }

        float threshold = IsDirection010TestMode()
            ? ballSpawner.direction010TestCDAngleThreshold
            : ballSpawner.direction010CDAngleThreshold;
        return Mathf.Clamp(threshold, 0f, 90f);
    }

    private float GetDirection010SmoothingAlpha()
    {
        if (ballSpawner == null)
        {
            return 0.25f;
        }

        return Mathf.Clamp01(
            IsDirection010TestMode()
                ? ballSpawner.direction010TestSmoothingAlpha
                : ballSpawner.direction010SmoothingAlpha);
    }

    private float GetDirection010MinSpeedThreshold()
    {
        if (ballSpawner == null)
        {
            return 0.05f;
        }

        return Mathf.Max(
            0f,
            IsDirection010TestMode()
                ? ballSpawner.direction010TestMinSpeedThreshold
                : ballSpawner.direction010MinSpeedThreshold);
    }

    private bool IsDirection010TestMode()
    {
        return modeSelected == (int)BallSpawner.Mode.direction010Test;
    }

    private void ResetDirection010ObstacleState()
    {
        handInBall = false;
        handInBall2 = false;
        inBall = false;
        canceled = false;
        direction010CancelTriggered = false;
        direction010ReentryRequired = false;
        directionCancelEntryAxisCaptured = false;
        directionCancelEntryAxis = Vector3.zero;
        directionCancelLowSpeedMet = false;
        ResetDirection010ObstacleTrackingWindow();
        ResetRainbowState();
    }

    private void ResetDirection010ObstacleTrackingWindow()
    {
        direction010PositionHistory.Clear();
        direction010HasSmoothedDirection = false;
        direction010SmoothedDirection = Vector3.zero;
        directionCancelLowSpeedMet = false;
        directionCancelEntryAxisCaptured = false;
        directionCancelEntryAxis = Vector3.zero;
        direction010HasAcceptedPosition = false;
        direction010LastAcceptedPosition = Vector3.zero;
    }

    private void ShowRainbowArc()
    {
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            return;
        }

        Vector3 ballCenter = collider.bounds.center;
        Vector3 referenceEntryPosition = rainbowEntryCaptured ? rainbowEntryPosition : reactTap.IndexTipPose2.Position;
        Vector3 entryDirection = referenceEntryPosition - ballCenter;
        if (entryDirection.sqrMagnitude < 0.000001f)
        {
            entryDirection = Vector3.forward;
        }

        float ballRadius = Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.y, collider.bounds.extents.z);
        rainbowArcNormal = GetRainbowArcNormal(ballCenter, entryDirection);
        rainbowArcCenter = ballCenter;
        rainbowArcRadius = ballRadius;
        HideRainbowArc();
        rainbowPrevFingerPosition = reactTap.IndexTipPose2.Position;
        rainbowArcActive = true;
        rainbowArcInitialized = true;

        rainbowArcRoot = new GameObject("RainbowCancelFrame");
        Color arcColor = Color.blue;
        Vector3 tangent = Vector3.Cross(rainbowArcNormal, Vector3.up);
        if (tangent.sqrMagnitude < 0.000001f)
        {
            tangent = Vector3.Cross(rainbowArcNormal, Vector3.right);
        }
        tangent.Normalize();
        Vector3 bitangent = Vector3.Cross(rainbowArcNormal, tangent).normalized;

        float frameHalfExtent = rainbowArcRadius * 1.1f;
        rainbowFrameTangent = tangent;
        rainbowFrameBitangent = bitangent;
        rainbowFrameHalfExtent = frameHalfExtent;
        CreateRainbowFrame(ballCenter + rainbowArcNormal * (rainbowArcRadius * 1.01f), tangent, bitangent, frameHalfExtent, arcColor);
    }

    private void HideRainbowArc()
    {
        rainbowArcActive = false;
        if (rainbowArcRoot != null)
        {
            Destroy(rainbowArcRoot);
            rainbowArcRoot = null;
        }
    }

    private void CreateRainbowFrame(
        Vector3 frameCenter,
        Vector3 tangent,
        Vector3 bitangent,
        float halfExtent,
        Color color)
    {
        if (rainbowArcRoot == null)
        {
            return;
        }

        CreateRainbowFrameEdge(
            "RainbowFrameTop",
            frameCenter + bitangent * halfExtent,
            tangent,
            bitangent,
            new Vector3(halfExtent * 2f, rainbowFrameDepth, rainbowArcThickness),
            color);
        CreateRainbowFrameEdge(
            "RainbowFrameBottom",
            frameCenter - bitangent * halfExtent,
            tangent,
            bitangent,
            new Vector3(halfExtent * 2f, rainbowFrameDepth, rainbowArcThickness),
            color);
        CreateRainbowFrameEdge(
            "RainbowFrameLeft",
            frameCenter - tangent * halfExtent,
            tangent,
            bitangent,
            new Vector3(rainbowArcThickness, rainbowFrameDepth, halfExtent * 2f),
            color);
        CreateRainbowFrameEdge(
            "RainbowFrameRight",
            frameCenter + tangent * halfExtent,
            tangent,
            bitangent,
            new Vector3(rainbowArcThickness, rainbowFrameDepth, halfExtent * 2f),
            color);
    }

    private void CreateRainbowFrameEdge(
        string edgeName,
        Vector3 position,
        Vector3 tangent,
        Vector3 bitangent,
        Vector3 scale,
        Color color)
    {
        GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        edge.name = edgeName;
        edge.transform.SetParent(rainbowArcRoot.transform, true);
        edge.transform.position = position;
        edge.transform.rotation = Quaternion.LookRotation(bitangent, rainbowArcNormal);
        edge.transform.localScale = scale;

        Collider edgeCollider = edge.GetComponent<Collider>();
        if (edgeCollider != null)
        {
            Destroy(edgeCollider);
        }

        MeshRenderer renderer = edge.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Unlit/Color");
            Material material = new Material(shader != null ? shader : Shader.Find("Standard"));
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
            else
            {
                material.color = color;
            }
            renderer.material = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private Vector3 GetRainbowArcNormal(Vector3 ballCenter, Vector3 entryDirection)
    {
        if (ballSpawner != null && ballSpawner.rainbowArcPlacement == BallSpawner.RainbowArcPlacement.OppositeUserFacing)
        {
            Vector3 viewerBackDirection = GetViewerBackDirection(ballCenter);
            if (viewerBackDirection.sqrMagnitude >= 0.000001f)
            {
                return viewerBackDirection.normalized;
            }
        }

        return -entryDirection.normalized;
    }

    private Vector3 GetViewerBackDirection(Vector3 ballCenter)
    {
        Transform viewerTransform = Camera.main != null ? Camera.main.transform : null;
        if (viewerTransform == null)
        {
            GameObject centerEyeAnchor = GameObject.Find("CenterEyeAnchor");
            if (centerEyeAnchor != null)
            {
                viewerTransform = centerEyeAnchor.transform;
            }
        }

        if (viewerTransform == null)
        {
            return Vector3.zero;
        }

        Vector3 userFacingDirection = (viewerTransform.position - ballCenter).normalized;
        return -userFacingDirection;
    }

    private void CheckRainbowCancel()
    {
        Vector3 currentFingerPosition = reactTap.IndexTipPose2.Position;
        Vector3 previousVector = rainbowPrevFingerPosition - rainbowArcCenter;
        Vector3 currentVector = currentFingerPosition - rainbowArcCenter;
        float previousDistance = previousVector.magnitude;
        float currentDistance = currentVector.magnitude;
        float shellRadius = rainbowArcRadius * 1.01f;
        bool crossedSurface = (previousDistance < shellRadius && currentDistance >= shellRadius) ||
            (previousDistance > shellRadius && currentDistance <= shellRadius);

        if (crossedSurface)
        {
            float denominator = currentDistance - previousDistance;
            float t = Mathf.Approximately(denominator, 0f) ? 0f : (shellRadius - previousDistance) / denominator;
            t = Mathf.Clamp01(t);
            Vector3 crossingPoint = Vector3.Lerp(rainbowPrevFingerPosition, currentFingerPosition, t);
            Vector3 crossingDirection = (crossingPoint - rainbowArcCenter).normalized;
            if (IsRainbowCancelDirectionInFrame(crossingDirection, shellRadius))
            {
                TriggerRainbowCancel();
                return;
            }
        }

        rainbowPrevFingerPosition = currentFingerPosition;
    }

    private void ResetRainbowState()
    {
        rainbowCancelTriggered = false;
        rainbowArcInitialized = false;
        rainbowEntryCaptured = false;
        rainbowConfirmBlocked = false;
        rainbowReentryRequiresExit = false;
        rainbowPrevFingerPosition = Vector3.zero;
        rainbowEntryPosition = Vector3.zero;
        rainbowFrameTangent = Vector3.zero;
        rainbowFrameBitangent = Vector3.zero;
        rainbowFrameHalfExtent = 0f;
        HideRainbowArc();
    }

    private bool IsRainbowCancelDirectionInFrame(Vector3 crossingDirection, float shellRadius)
    {
        float projection = Vector3.Dot(crossingDirection, rainbowArcNormal);
        if (projection <= 0.000001f)
        {
            return false;
        }

        Vector3 frameCenter = rainbowArcCenter + rainbowArcNormal * shellRadius;
        Vector3 projectedPoint = rainbowArcCenter + crossingDirection * (shellRadius / projection);
        Vector3 frameOffset = projectedPoint - frameCenter;
        float localX = Vector3.Dot(frameOffset, rainbowFrameTangent);
        float localY = Vector3.Dot(frameOffset, rainbowFrameBitangent);

        return Mathf.Abs(localX) <= rainbowFrameHalfExtent
            && Mathf.Abs(localY) <= rainbowFrameHalfExtent;
    }

    private void OnDisable()
    {
        LogSpeedStudy("disabled");
        logWriter3?.UnregisterSpeedStudyObserver(speedStudyTelemetry);
        dynamicStopMotion.Reset();
        ixdlDynamicMotion.Reset();
        magicTap2Motion.Reset();
        if (UsesDynamicMotion || UsesMagicTap2)
        {
            handInBall = handInBall2 = inBall = false;
        }
        HideDirectionCDArrow();
    }

}

