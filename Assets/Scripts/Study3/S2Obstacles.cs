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
            case 14:
                SetReactReleaseMode();
                break;
            case 15:
                SetSpeedReleaseMode();
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
            case 12:
                Acce.Delay = true;
                Acce.WaitTime = Mathf.Max(0f, ballSpawner.longPressTimeThreshold);
                SetDwellWithdrawMode();
                break;
            case 13:
                SetPinchReleaseTriggerMode();
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


        }
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
            case 14:
                SetReactReleaseMode();
                break;
            case 15:
                SetSpeedReleaseMode();
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
            case 12:
                Acce.Delay = true;
                Acce.WaitTime = Mathf.Max(0f, ballSpawner.longPressTimeThreshold);
                SetDwellWithdrawMode();
                break;
            case 13:
                SetPinchReleaseTriggerMode();
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
        }
    }
    public void ClearMode()
    {
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

        reactTap.CancelEvent.AddListener(TriggerFastTapCancel);

        reactTap.AfterOutEvent.AddListener(() =>
        {
            if (handInBall && !fastTapCancelTriggered && !canceled && HasFastTapTimedOut())
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

        // Obstacle flow mirrors the selection mode: index entry arms the obstacle,
        // PinchStart enables contact tracking, and PinchEnd resolves trigger/cancel
        // from thumb-to-index distance.
        Acce.InEvent.AddListener(() =>
        {
            if (pinchThumbSwipeReentryRequired || triggered)
            {
                ballSpawner.area_type = triggered ? 4 : 3;
                ballSpawner.index = -1;
                return;
            }

            if (!inBall)
            {
                Player.TurnShallow();
                inBall = true;
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
            if (pinchThumbSwipeCanceled || pinchThumbSwipeReentryRequired || triggered)
            {
                return;
            }

            if (inBall && TryCapturePinchThumbSwipeStart())
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
            inBall = false;
            if (pinchThumbSwipeReentryRequired)
            {
                ClearPinchThumbSwipeReentry();
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
                // 010-direction: 进入障碍球后检测 cd 方向取消；取消后必须离开球体再重新进入。
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

                // 在球内每个 FixedUpdate 通过 InEvent 检测一次：可调窗口内的平滑方向接近 cd 时取消误触。
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
                    // 离开障碍球时，如果此前没有按 cd 方向取消，则按障碍球误触记录。
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

            if (inBall || inBall2)
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

            if (pinchReleaseTriggerArmed && (inBall || inBall2) && !triggered)
            {
                TriggerPinchReleaseTriggerObstacle();
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

    private void Update()
    {
        if (modeSelected == 5
            && handInBall
            && !fastTapCancelTriggered
            && !fastTapReentryRequired
            && !fastTapCanceledUntilExit
            && HasFastTapTimedOut())
        {
            TriggerFastTapCancel();
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

    private void CheckSpeedReleaseIndexTrigger()
    {
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
                ResetReleaseGestureTracking();
            }

            return;
        }

        if (indexSpeed >= Mathf.Max(0f, ballSpawner.speedMaxThreshold))
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

        if (inBall || inBall2)
        {
            if (!triggered)
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

    private bool TryCapturePinchThumbSwipeStart()
    {
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
        float t = Mathf.Clamp(thumbProjectionOnIndex, 0f, segmentLength);
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
        ballSpawner.triggeredBy = "index";
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
        if (ballSpawner == null || Acce == null)
        {
            return false;
        }

        // 用可调窗口内的累计位移表示“实际移动方向”，避免单帧手部追踪抖动直接触发取消。
        Vector3 windowMovement = GetDirection010WindowMovement();
        if (windowMovement.sqrMagnitude <= 0.0000001f)
        {
            return false;
        }

        float minSpeed = GetDirection010MinSpeedThreshold();
        if (minSpeed > 0f && Acce.IndexVelocity.sqrMagnitude < minSpeed * minSpeed)
        {
            return false;
        }

        Collider collider = GetComponent<Collider>();
        Vector3 ballCenter = collider != null ? collider.bounds.center : transform.position;
        Vector3 ringCenter = ballSpawner.transform.position;
        Vector3 abDirection = new Vector3(ballCenter.x - ringCenter.x, ballCenter.y - ringCenter.y, 0f);
        if (abDirection.sqrMagnitude <= 0.0000001f)
        {
            return false;
        }

        // 图中的 cd 方向：在菲茨环平面内，垂直于 a(环心)->b(球心) 的方向；正反方向都视为同一条 cd 线。
        Vector3 cdDirection = new Vector3(-abDirection.y, abDirection.x, 0f).normalized;
        // 在窗口方向上再做 Slerp 平滑，过滤方向跳变。
        Vector3 smoothedDirection = SmoothDirection010Direction(windowMovement.normalized);
        float angle = Vector3.Angle(smoothedDirection, cdDirection);
        float cdAngleDelta = Mathf.Min(angle, 180f - angle);
        if (cdAngleDelta <= GetDirection010AngleThreshold())
        {
            return true;
        }

        return IsDirection010TestMode()
            && IsDirection010UserFacingCancelMovement(smoothedDirection, ballCenter);
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

    private bool IsDirection010UserFacingCancelMovement(Vector3 smoothedDirection, Vector3 ballCenter)
    {
        Vector3 oppositeUserFacingDirection = GetViewerBackDirection(ballCenter);
        if (oppositeUserFacingDirection.sqrMagnitude <= 0.000001f)
        {
            return false;
        }

        // OppositeUserFacing is the ball's back side, so toward the user-facing
        // side is the inverse direction.
        Vector3 userFacingDirection = -oppositeUserFacingDirection.normalized;
        float angle = Vector3.Angle(smoothedDirection, userFacingDirection);
        return angle <= GetDirection010AngleThreshold();
    }

    private Vector3 GetDirection010WindowMovement()
    {
        Vector3 currentPosition = Acce.IndexTipPose2.Position;
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

        // 队列满指定帧数后，用当前点减最早点得到窗口累计位移方向。
        return currentPosition - direction010PositionHistory.Peek();
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
        direction010PositionHistory.Clear();
        direction010HasSmoothedDirection = false;
        direction010SmoothedDirection = Vector3.zero;
        ResetRainbowState();
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

}

