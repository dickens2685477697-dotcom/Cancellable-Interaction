using Oculus.Platform.Samples.VrHoops;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
// For all, Out-Enter-Out will have material change be like Out-in-out coded in AcceStudy3.cs
public class S2AccMode : MonoBehaviour
{
    public AcceStudy3 Acce;
    public ReactTap reactTap;
    public AcceStudy3_2 Acce2;
    public S2Player Player;
    public BallSpawner ballSpawner;
    public GameObject TargetPrefab;
    public int modeSelected;
    bool inBall = false, inBall2 = false;
    public int ballIndex = 0;
    public int ballKind = 1;
    public Pinch pinchob;
    public bool useIndex = false; 
    public bool handInBall = false;
    public bool canceled = false;
    public LogWriter3 logWriter3;
    public LogWriter2 logWriter2;
    public bool handInBall2 = false;
    // Release Tap gesture detector attached to the current target ball.
    public ReleaseGesture releaseGesture;
    private bool pinchHandledThisGesture = false;
    private bool pinchReleaseCancelTriggered = false;
    private bool pinchReleaseReentryRequired = false;
    private bool pinchReleaseWaitForIndexExit = false;
    private bool pinchReleaseWaitForThumbExit = false;
    private float pinchDwellEntryTimestamp = -1f;
    [SerializeField, Min(0f)] private float fastTapCancelTime = 0.5f;
    private float fastTapEntryTimestamp = -1f;
    private float fastTapReactTimestamp = -1f;
    private bool fastTapReentryRequired = false;
    private bool reactReleaseSpeedAcceMet = false;
    private bool reactReleaseTriggered = false;
    private bool reactReleaseReentryRequired = false;
    private bool speedReleaseMinSpeedMet = false;
    private bool speedReleaseTriggered = false;
    private bool speedReleaseReentryRequired = false;
    public bool isMineBall = false; // 是否为雷球
    private bool fastTapCancelTriggered = false; // FastTap模式下是否已触发取消
    private bool rainbowCancelTriggered = false;
    private bool rainbowArcActive = false;
    private bool rainbowArcInitialized = false;
    private bool pressTriggered = false;
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
    private float pinchReleaseTriggerEntryTimestamp = -1f;
    private bool pinchThumbSwipeTracking = false;
    private bool pinchThumbSwipeCompleted = false;
    private bool pinchThumbSwipeCanceled = false;
    private bool pinchThumbSwipeReentryRequired = false;
    private float pinchThumbSwipeEntryTimestamp = -1f;
    private Vector3 pinchThumbSwipeStartThumbPosition = Vector3.zero;
    private Vector3 pinchThumbSwipeLastThumbPosition = Vector3.zero;
    private float pinchThumbSwipeStartProjection = 0f;
    private float pinchThumbSwipeCurrentContactDistance = float.MaxValue;
    private bool pinchWristTiltInside = false;
    private bool pinchWristTiltCompleted = false;
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
    private float rainbowArcThickness = 0.003f;
    private float rainbowArcHalfAngle = 75f;
    private bool rainbowConfirmBlocked = false;
    private bool rainbowReentryRequiresExit = false;
    private float rainbowFrameDepth = 0.0015f;
    private FacingFeedbackRing modeReadyRing;
    //public AudioSource source;
    //public AudioClip clip;

    void Update()
    {
        // FastTap模式:检查停留时间是否超过0.5秒
        if (modeSelected == 5 && handInBall && !fastTapCancelTriggered && !fastTapReentryRequired)
        {
            if (HasFastTapTimedOut())
            {
                TriggerFastTapCancel("Dwell");
            }
        }
        if (modeSelected == 29 && handInBall && !fastTapCancelTriggered && !fastTapReentryRequired)
        {
            CheckSwing010Cancel();
        }
        if (modeSelected == 6 && handInBall && rainbowArcActive && !rainbowCancelTriggered)
        {
            CheckRainbowCancel();
        }
        if (modeSelected == 7 && handInBall && !fastTapCancelTriggered && !fastTapReentryRequired)
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
                TriggerSpeedCancel("dwell", true);
            }
        }
        if (modeSelected == 8
            && (inBall || inBall2)
            && !pinchob.isPinch
            && !pinchReleaseCancelTriggered
            && !pinchReleaseReentryRequired)
        {
            CheckPinchReleaseCancel();
        }
        if (modeSelected == 9
            && inBall
            && pinchob.isPinch
            && !pinchReleaseCancelTriggered
            && !pinchReleaseReentryRequired
            && HasPinchDwellTimedOut())
        {
            TriggerPinchReleaseCancel("Dwell");
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
            && !pinchThumbSwipeCompleted
            && !pinchThumbSwipeCanceled)
        {
            UpdatePinchThumbSwipeContactDistance();
        }
        if (modeSelected == 17
            && pinchWristTiltInside
            && pinchWristTiltPinching
            && !pinchWristTiltCompleted
            && !pinchWristTiltCanceled)
        {
            CheckPinchWristTiltCancel();
        }
        if (modeSelected == 18
            && pinchWristTiltInside
            && !pinchWristTiltCompleted
            && !pinchWristTiltCanceled)
        {
            CheckPinchWristTiltCancel();
        }
        if (modeSelected == 19
            && pinchWristTiltInside
            && !pinchWristTiltCompleted
            && !pinchWristTiltCanceled)
        {
            UpdateDwellWristTiltSelection();
        }
    }

    public void Start()
    {
        Acce = GetComponent<AcceStudy3>();
        Acce2 = GetComponent<AcceStudy3_2>();
        reactTap = GetComponent<ReactTap>();
        Player = GetComponentInChildren<S2Player>();
        ballSpawner = GameObject.Find("Spawner").GetComponent<BallSpawner>();
        logWriter3 = GameObject.Find("Spawner").GetComponent<LogWriter3>();
        logWriter2 = GameObject.Find("Spawner").GetComponent<LogWriter2>();
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
                SetRainbow();
                break;
            case 7:
                SetReleaseTapMode();
                break;
            case 8:
                SetPinchReleaseMode();
                break;
            case 9:
                SetPinchDwellMode();
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
        }

        //clip = Resources.Load<AudioClip>("Audio/Assets_MRTK_StandardAssets_Audio_MRTK_Manipulation_Start");
        //source = GetComponent<AudioSource>();
        //source.clip = clip;
    }
    
    public void SetPinchReleaseMode()
    {
        ResetPinchReleaseState();
        SetPinchMode();
    }

    public void SetPinchDwellMode()
    {
        ResetPinchReleaseState();
        SetPinchMode();
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
            if (!ballSpawner.enable_trigger || pinchReleaseTriggerCompleted)
            {
                return;
            }

            if (inBall || inBall2)
            {
                pinchReleaseTriggerArmed = true;
                ShowModeReadyRing();
            }
        });

        pinchob.PinchEnd.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger || pinchReleaseTriggerCompleted)
            {
                return;
            }

            if (pinchReleaseTriggerArmed && (inBall || inBall2))
            {
                TriggerPinchReleaseTriggerSelection();
            }

            pinchReleaseTriggerArmed = false;
            HideModeReadyRing();
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

    public void SetPinchDirectionMode()
    {
        ResetPinchReleaseTriggerState();
        ResetPinchDirectionCancelState();

        Acce.InEvent.AddListener(() =>
        {
            HandlePinchDirectionEnter(true);

            if (pinchReleaseTriggerArmed
                && !pinchDirectionCancelTriggered
                && !pinchDirectionReentryRequired
                && IsPinchDirectionCancelMovement())
            {
                TriggerPinchDirectionCancel();
            }
        });

        Acce2.InEvent.AddListener(() =>
        {
            HandlePinchDirectionEnter(false);
        });

        pinchob.PinchStart.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger
                || pinchReleaseTriggerCompleted
                || pinchDirectionCancelTriggered
                || pinchDirectionReentryRequired)
            {
                return;
            }

            if (inBall || inBall2)
            {
                pinchReleaseTriggerArmed = true;
                ResetPinchDirectionTrackingWindow();
                ShowModeReadyRing();
            }
        });

        pinchob.PinchEnd.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger || pinchReleaseTriggerCompleted)
            {
                return;
            }

            if (pinchReleaseTriggerArmed
                && (inBall || inBall2)
                && !pinchDirectionCancelTriggered
                && !pinchDirectionReentryRequired)
            {
                TriggerPinchReleaseTriggerSelection();
                ResetPinchDirectionCancelState();
            }

            pinchReleaseTriggerArmed = false;
            HideModeReadyRing();
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            HandlePinchDirectionExit(true);
        });

        Acce2.AfterOutEvent.AddListener(() =>
        {
            HandlePinchDirectionExit(false);
        });
    }

    public void SetPinchThumbSwipeMode()
    {
        ResetPinchThumbSwipeState();

        // Selection flow: index entry arms the target, PinchStart enables contact
        // tracking, and PinchEnd resolves trigger/cancel from thumb-to-index distance.
        Acce.InEvent.AddListener(() =>
        {
            HandlePinchThumbSwipeIndexEnter();
        });

        pinchob.PinchStart.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger || pinchThumbSwipeCompleted || pinchThumbSwipeCanceled)
            {
                return;
            }

            if (inBall && TryCapturePinchThumbSwipeStart())
            {
                pinchThumbSwipeTracking = true;
                ShowModeReadyRing();
            }
        });

        pinchob.PinchEnd.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger || pinchThumbSwipeCompleted || pinchThumbSwipeCanceled)
            {
                ResetPinchThumbSwipeTracking();
                return;
            }

            if (!pinchThumbSwipeTracking)
            {
                return;
            }

            ResolvePinchThumbSwipeEnd();
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            HandlePinchThumbSwipeIndexExit();
        });
    }

    public void SetPinchWristTiltMode()
    {
        ResetPinchWristTiltState();

        Acce.InEvent.AddListener(HandlePinchWristTiltIndexEnter);

        pinchob.PinchStart.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger
                || pinchWristTiltCompleted
                || pinchWristTiltCanceled
                || pinchWristTiltReentryRequired)
            {
                return;
            }

            if (!pinchWristTiltInside)
            {
                return;
            }

            pinchWristTiltPinching = true;
            pinchWristTiltHasEntryRotation = TryGetWristRotation(out pinchWristTiltEntryRotation);
            ShowModeReadyRing();
        });

        pinchob.PinchEnd.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger
                || pinchWristTiltCompleted
                || pinchWristTiltCanceled
                || pinchWristTiltReentryRequired)
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
                CancelPinchWristTilt("wrist-tilt");
                return;
            }

            TriggerPinchWristTiltSelection();
        });

        Acce.AfterOutEvent.AddListener(HandlePinchWristTiltIndexExit);
    }

    public void SetWristTiltMode()
    {
        ResetPinchWristTiltState();

        Acce.InEvent.AddListener(HandleWristTiltIndexEnter);
        Acce.AfterOutEvent.AddListener(HandleWristTiltIndexExit);
    }

    public void SetDwellWristTiltMode()
    {
        ResetPinchWristTiltState();

        Acce.InEvent.AddListener(HandleDwellWristTiltIndexEnter);
        Acce.AfterOutEvent.AddListener(HandleDwellWristTiltIndexExit);
    }

    public void SetContact010Mode()
    {
        ResetContact010State();

        Acce.InEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger)
            {
                return;
            }

            if (!handInBall2)
            {
                handInBall2 = true;
                handInBall = true;
                inBall = true;
                canceled = false;
                ballSpawner.intargetTimes++;
                logWriter3.handInBall = true;
                logWriter3.isMineBall = isMineBall;
                ConfigureContact010Frame();
            }

            if (isMineBall)
            {
                Player.TurnShallowYellowMineBall();
            }
            else
            {
                Player.TurnShallowGreenMineBall();
            }

            ballSpawner.area_type = 1;
            ballSpawner.index = ballIndex;
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger)
            {
                ResetContact010State();
                return;
            }

            bool wasInside = handInBall2 || inBall || handInBall;
            if (!wasInside)
            {
                ResetContact010State();
                return;
            }

            bool exitInTriggerRegion = IsContact010ExitInUserFacingFrame();
            if (exitInTriggerRegion)
            {
                TriggerContact010Selection();
            }
            else
            {
                TriggerSurfaceExitCancel("contact transition");
                if (!ballSpawner.triggered)
                {
                    ballSpawner.area_type = 3;
                    ballSpawner.index = -1;
                }
            }

            ResetContact010State();
        });
    }

    public void SetDirection010Mode()
    {
        ResetDirection010State();

        Acce.InEvent.AddListener(() =>
        {
            // 010-direction: 手指进入目标球后才开始检测取消方向；取消后需要先离球再重新进入。
            if (!ballSpawner.enable_trigger || direction010ReentryRequired)
            {
                return;
            }

            if (!handInBall2)
            {
                // 记录本次进入目标球的状态，并让日志按“手在目标球内”处理。
                handInBall2 = true;
                handInBall = true;
                inBall = true;
                canceled = false;
                direction010CancelTriggered = false;
                ballSpawner.intargetTimes++;
                logWriter3.handInBall = true;
                logWriter3.isMineBall = isMineBall;
            }

            if (isMineBall)
            {
                Player.TurnShallowYellowMineBall();
            }
            else
            {
                Player.TurnShallowGreenMineBall();
            }

            ballSpawner.area_type = 1;
            ballSpawner.index = ballIndex;

            // 在球内每个 FixedUpdate 通过 InEvent 检测一次：可调窗口内的平滑方向接近 cd 时取消。
            if (IsDirection010CancelMovement())
            {
                TriggerDirection010Cancel();
            }
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger)
            {
                ResetDirection010State();
                return;
            }

            bool wasInside = handInBall2 || inBall || handInBall || direction010ReentryRequired;
            if (!wasInside)
            {
                ResetDirection010State();
                return;
            }

            if (!direction010CancelTriggered)
            {
                // 离开目标球时，如果此前没有满足 cd 方向取消条件，则确认触发。
                ballSpawner.triggerCondition = 'D';
                TriggerContact010Selection();
            }
            else if (!ballSpawner.triggered)
            {
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
            }

            ResetDirection010State();
        });
    }

    public void SetDwellDirectionMode()
    {
        ResetDirection010State();

        if (ballSpawner != null)
        {
            Acce.InEvent.AddListener(() =>
            {
                if (!ballSpawner.enable_trigger || direction010ReentryRequired)
                {
                    return;
                }

                if (!handInBall2)
                {
                    handInBall2 = true;
                    handInBall = true;
                    inBall = true;
                    canceled = false;
                    direction010CancelTriggered = false;
                    ResetDirection010TrackingWindow();
                    ballSpawner.intargetTimes++;
                }

                if (isMineBall)
                {
                    Player.TurnShallowYellowMineBall();
                }
                else
                {
                    Player.TurnShallowGreenMineBall();
                }

                ballSpawner.area_type = 1;
                ballSpawner.index = ballIndex;
                logWriter3.isMineBall = isMineBall;
                logWriter3.handInBall = true;

                if (IsDirection010CancelMovement())
                {
                    TriggerDirection010Cancel();
                }
            });

            Acce.HesEvent.AddListener(() =>
            {
                if (direction010CancelTriggered || direction010ReentryRequired || canceled)
                {
                    return;
                }

                if (isMineBall)
                {
                    logWriter3.successfully_Canceled = false;
                    ballSpawner.caseTime++;
                    ballSpawner.PlaySoundWrong();
                    ballSpawner.getNewTarget();
                    ballSpawner.triggerCondition = Acce.condition;
                }
                else
                {
                    ballSpawner.caseTime++;
                    ballSpawner.PlaySound2();
                    ballSpawner.getNewTarget();
                    ballSpawner.triggerCondition = Acce.condition;
                }

                logWriter3.handInBall = false;
                logWriter3.triggerTime = 0f;
            });
        }

        Acce.AfterOutEvent.AddListener(() =>
        {
            bool wasDirectionCanceled = direction010CancelTriggered || direction010ReentryRequired || canceled;

            if (wasDirectionCanceled)
            {
                ResetDirection010State();
                return;
            }

            handInBall2 = false;
            handInBall = false;
            inBall = false;

            Player.ChangeOpacityBack();
            if (!ballSpawner.triggered)
            {
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
            }

            logWriter3.handInBall = false;
            logWriter3.triggerTime = 0f;
            useIndex = false;
            ResetDirection010TrackingWindow();
        });
    }

    public void SetPinchMode()
    {
        pinchob.PinchEnd.AddListener(() =>
        {
            if (IsPinchDwellMode())
            {
                TryTriggerPinchDwellSelection();
            }

            pinchHandledThisGesture = false;
        });

        if (Player != null)
        {
            Acce.InEvent.AddListener(() =>
            {
                if (IsPinchReleaseBlocked())
                {
                    ballSpawner.area_type = 3;
                    ballSpawner.index = -1;
                    return;
                }

                if (!inBall && !inBall2)
                {
                    useIndex = false;
                    ResetPinchReleaseTracking();
                    ResetPinchDwellTracking();
                }
                if (isMineBall)
                {
                    Player.TurnShallowYellowMineBall();
                    ballSpawner.area_type = 1;
                    ballSpawner.index = ballIndex;
                    if (inBall == false)
                    {
                        if (!inBall2)
                        {
                            ballSpawner.intargetTimes++;
                        }
                        inBall = true;
                    }
                }
                else
                {
                    ballSpawner.area_type = 1;
                    ballSpawner.index = ballIndex;
                    print("��Ŀ������");
                    if (inBall == false)
                    {
                        if (!inBall2)
                        {
                            ballSpawner.intargetTimes++;
                        }
                        inBall = true;
                        //Player.ChangeOpacity();
                        Player.TurnShallowGreenMineBall();
                        //print(111);
                    }
                }
                logWriter3.isMineBall = isMineBall;
                logWriter3.handInBall = true;

            });
        }
        pinchob.PinchStart.AddListener(() =>
        {
            if (pinchHandledThisGesture || IsPinchReleaseBlocked())
            {
                return;
            }

            if (IsPinchDwellMode())
            {
                if (inBall)
                {
                    StartPinchDwellTracking();
                }

                return;
            }

            bool handledByIndex = false;
            if (isMineBall)
            {
                if (inBall)
                {
                    logWriter3.successfully_Canceled = false;
                    useIndex = true;
                    handledByIndex = true;
                    ballSpawner.caseTime++;
                    print("����111");
                    print(Acce.IndexTipPose2.Position.x);
                    ballSpawner.PlaySoundWrong();
                    ballSpawner.getNewTarget();
                }
            }
            else
            {
                if (inBall)
                {
                    useIndex = true;
                    handledByIndex = true;
                    ballSpawner.caseTime++;
                    print("����111");
                    print(Acce.IndexTipPose2.Position.x);
                    ballSpawner.PlaySound2();
                    ballSpawner.getNewTarget();
                    //ballSpawner.triggeredBy = "index";

                }
            }
            if (handledByIndex)
            {
                pinchHandledThisGesture = true;
                logWriter3.handInBall = false;
                logWriter3.triggerTime = 0f;
                inBall = false;
                useIndex = false;
                ResetPinchDwellTracking();
            }
        });
        Acce.AfterOutEvent.AddListener(() =>
        {
            if (IsPinchReleaseBlocked())
            {
                inBall = false;
                pinchReleaseWaitForIndexExit = false;
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
                Player.ChangeOpacityBack();
                ClearPinchReleaseReentryIfReady();
                return;
            }

            inBall = false;
            if (IsPinchDwellMode())
            {
                ResetPinchDwellTracking();
            }

            if (UpdatePinchTargetExitVisualState())
            {
                return;
            }

            if (IsPinchDwellMode())
            {
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
                Player.ChangeOpacityBack();
                logWriter3.handInBall = false;
                logWriter3.triggerTime = 0f;
                ResetPinchReleaseTracking();
                ResetPinchDwellTracking();
                return;
            }

            if (isMineBall)
            {
                logWriter3.RecordCancelReason("contact transition");
                logWriter3.successfully_Canceled = true;
                ballSpawner.PlaySound2();
                ballSpawner.getNewTarget();
                Player.ChangeOpacityBack();
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
                inBall = false;
            }
            else
            {
                if (!ballSpawner.triggered)
                {
                    logWriter3.RecordCancelReason("contact transition");
                    Player.ChangeOpacityBack();
                    print("������ʧ��");
                    ballSpawner.area_type = 3;
                    ballSpawner.index = -1;
                    print(555);
                    inBall = false;
                }
            }
            ballSpawner.area_type = 3;
            ballSpawner.index = -1;
            inBall = false;
            Player.ChangeOpacityBack();
            logWriter3.handInBall = false;
            logWriter3.triggerTime = 0f;
            ResetPinchReleaseTracking();
            ResetPinchDwellTracking();
        });

        if (Player != null)
        {
            Acce2.InEvent.AddListener(() =>
            {
                if (IsPinchReleaseBlocked())
                {
                    ballSpawner.area_type = 3;
                    ballSpawner.index = -1;
                    return;
                }

                if (!inBall && !inBall2)
                {
                    useIndex = false;
                    ResetPinchReleaseTracking();
                    ResetPinchDwellTracking();
                }
                if (isMineBall)
                {
                    Player.TurnShallowYellowMineBall();
                    ballSpawner.area_type = 1;
                    ballSpawner.index = ballIndex;
                    print("thumb��Ŀ������");
                    ballSpawner.triggeredBy = "thumb";
                    if (inBall2 == false)
                    {
                        if (!inBall)
                        {
                            ballSpawner.intargetTimes++;
                        }
                        inBall2 = true;
                    }
                }
                else
                {
                    ballSpawner.area_type = 1;
                    ballSpawner.index = ballIndex;
                    print("thumb��Ŀ������");
                    ballSpawner.triggeredBy = "thumb";
                    if (inBall2 == false)
                    {
                        if (!inBall)
                        {
                            ballSpawner.intargetTimes++;
                        }
                        inBall2 = true;
                        //Player.ChangeOpacity();
                        Player.TurnShallowGreenMineBall();
                        //print(111);
                    }
                }
                logWriter3.isMineBall = isMineBall;
                logWriter3.handInBall = true;

            });
        }
        pinchob.PinchStart.AddListener(() =>
        {
            if (pinchHandledThisGesture || IsPinchReleaseBlocked())
            {
                return;
            }

            if (IsPinchDwellMode())
            {
                return;
            }

            bool handledByThumb = false;
            if (isMineBall)
            {
                if (inBall2 && !useIndex)
                {
                    logWriter3.successfully_Canceled = false;
                    print("����111by thumb");
                    handledByThumb = true;
                    print(Acce2.ThumbTipPose2.Position.x);
                    ballSpawner.caseTime++;
                    ballSpawner.PlaySoundWrong();
                    ballSpawner.getNewTarget();

                }
            }
            else
            {
                if (inBall2 && !useIndex)
                {
                    print("����111by thumb");
                    print(Acce2.ThumbTipPose2.Position.x);
                    handledByThumb = true;
                    ballSpawner.caseTime++;
                    ballSpawner.PlaySound2();
                    ballSpawner.getNewTarget();

                }
            }
            if (handledByThumb)
            {
                pinchHandledThisGesture = true;
                inBall2 = false;
                logWriter3.triggerTime = 0f;
                logWriter3.handInBall = false;
                useIndex = false;
                ResetPinchDwellTracking();
            }
        });
        Acce2.AfterOutEvent.AddListener(() =>
        {
            if (IsPinchReleaseBlocked())
            {
                inBall2 = false;
                pinchReleaseWaitForThumbExit = false;
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
                ballSpawner.triggeredBy = "index";
                Player.ChangeOpacityBack();
                ClearPinchReleaseReentryIfReady();
                return;
            }

            inBall2 = false;
            if (UpdatePinchTargetExitVisualState())
            {
                return;
            }

            if (IsPinchDwellMode())
            {
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
                inBall2 = false;
                ballSpawner.triggeredBy = "index";
                Player.ChangeOpacityBack();
                logWriter3.handInBall = false;
                logWriter3.triggerTime = 0f;
                useIndex = false;
                ResetPinchReleaseTracking();
                ResetPinchDwellTracking();
                return;
            }

            if (isMineBall)
            {
                if (!ballSpawner.triggered && !useIndex)
                {
                    logWriter3.RecordCancelReason("contact transition");
                    logWriter3.successfully_Canceled = true;
                    ballSpawner.getNewTarget();
                    Player.ChangeOpacityBack();
                    ballSpawner.area_type = 3;
                    ballSpawner.index = -1;
                    inBall2 = false;
                    ballSpawner.triggeredBy = "index";
                }
            }
            else
            {
                if (!ballSpawner.triggered)
                {
                    logWriter3.RecordCancelReason("contact transition");
                    Player.ChangeOpacityBack();
                    print("������ʧ��");
                    ballSpawner.area_type = 3;
                    ballSpawner.index = -1;
                    print(555);
                    inBall2 = false;
                    ballSpawner.triggeredBy = "index";
                }
            }
            ballSpawner.area_type = 3;
            ballSpawner.index = -1;
            print(555);
            inBall2 = false;
            ballSpawner.triggeredBy = "index";
            Player.ChangeOpacityBack();
            logWriter3.handInBall = false;
            logWriter3.triggerTime = 0f;
            useIndex = false;
            ResetPinchReleaseTracking();
            ResetPinchDwellTracking();
        });


    }
    public void SetHesMode()
    {
        if (ballSpawner != null)
        {

            Acce.InEvent.AddListener(() =>
            {
                if (isMineBall)
                {
                    Player.TurnShallowYellowMineBall();
                    ballSpawner.area_type = 1;
                    ballSpawner.index = ballIndex;
                    print("��Ŀ������");
                    if (!handInBall2)
                    {
                        ballSpawner.intargetTimes++;
                        handInBall2 = true;
                    }
                }
                else
                {
                    //Player.ChangeOpacity();
                    Player.TurnShallowGreenMineBall();
                    ballSpawner.area_type = 1;
                    ballSpawner.index = ballIndex;
                    print("��Ŀ������");
                    if (!handInBall2)
                    {
                        ballSpawner.intargetTimes++;
                        handInBall2 = true;
                    }
                }
                logWriter3.isMineBall = isMineBall;
                logWriter3.handInBall = true;

            });
            Acce.HesEvent.AddListener(() =>
            {
                if (isMineBall)
                {
                    logWriter3.successfully_Canceled = false;
                    ballSpawner.caseTime++;
                    ballSpawner.PlaySoundWrong();
                    ballSpawner.getNewTarget();
                    print("����111");
                    print(Acce.IndexTipPose2.Position.x);
                    print(Acce.condition);
                    ballSpawner.triggerCondition = Acce.condition;
                }
                else
                {
                    ballSpawner.caseTime++;
                    ballSpawner.PlaySound2();
                    ballSpawner.getNewTarget();
                    print("����111");
                    print(Acce.IndexTipPose2.Position.x);
                    print(Acce.condition);
                    ballSpawner.triggerCondition = Acce.condition;
                }
                logWriter3.handInBall = false;
                logWriter3.triggerTime = 0f;
            });

        }
        Acce.AfterOutEvent.AddListener(() =>
        {
            handInBall2 = false;
            inBall = false;
            if (isMineBall)
            {
                ballSpawner.PlaySound2();
                logWriter3.successfully_Canceled = true;
                logWriter3.RecordCancelReason("contact transition");
                Player.ChangeOpacityBack();
                ballSpawner.getNewTarget();
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
                
            }
            else
            {
                if (!ballSpawner.triggered)
                {
                    logWriter3.RecordCancelReason("contact transition");
                    Player.ChangeOpacityBack();
                    print("������ʧ��");
                    ballSpawner.area_type = 3;
                    ballSpawner.index = -1;
                    print(555);
                }
            }
            
            Player.ChangeOpacityBack();
            logWriter3.handInBall = false;
            logWriter3.triggerTime = 0f;
            useIndex = false;
        });
    }

    public void SetDwellReleaseMode()
    {
        EnsureReleaseGesture();

        if (ballSpawner != null)
        {
            Acce.InEvent.AddListener(() =>
            {
                if (dwellReleaseReentryRequired)
                {
                    return;
                }

                if (!handInBall2)
                {
                    dwellReleaseInside = true;
                    dwellReleaseEntryTimestamp = Time.time;
                    ResetReleaseGestureTracking();
                    ballSpawner.intargetTimes++;
                    handInBall2 = true;
                }

                RecordDwellWithdrawFingerPosition();

                if (isMineBall)
                {
                    Player.TurnShallowYellowMineBall();
                }
                else
                {
                    Player.TurnShallowGreenMineBall();
                }

                ballSpawner.area_type = 1;
                ballSpawner.index = ballIndex;
                logWriter3.isMineBall = isMineBall;
                logWriter3.handInBall = true;
            });

            Acce.HesEvent.AddListener(() =>
            {
                if (dwellReleaseCancelTriggered || dwellReleaseReentryRequired)
                {
                    return;
                }

                if (isMineBall)
                {
                    logWriter3.successfully_Canceled = false;
                    ballSpawner.caseTime++;
                    ballSpawner.PlaySoundWrong();
                    ballSpawner.getNewTarget();
                    ballSpawner.triggerCondition = Acce.condition;
                }
                else
                {
                    ballSpawner.caseTime++;
                    ballSpawner.PlaySound2();
                    ballSpawner.getNewTarget();
                    ballSpawner.triggerCondition = Acce.condition;
                }

                logWriter3.handInBall = false;
                logWriter3.triggerTime = 0f;
                ResetDwellReleaseState();
            });
        }

        Acce.AfterOutEvent.AddListener(() =>
        {
            handInBall2 = false;
            inBall = false;
            dwellReleaseInside = false;
            dwellReleaseEntryTimestamp = -1f;
            dwellReleaseCancelTriggered = false;
            dwellReleaseReentryRequired = false;
            Acce.UnInvoked();
            ResetReleaseGestureTracking();

            if (!ballSpawner.triggered)
            {
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
            }

            Player.ChangeOpacityBack();
            logWriter3.handInBall = false;
            logWriter3.triggerTime = 0f;
            useIndex = false;
        });
    }

    public void SetDwellWithdrawMode()
    {
        if (ballSpawner != null)
        {
            Acce.InEvent.AddListener(() =>
            {
                if (dwellWithdrawReentryRequired)
                {
                    return;
                }

                if (!handInBall2)
                {
                    dwellWithdrawInside = true;
                    dwellWithdrawEntryTimestamp = Time.time;
                    dwellWithdrawEntryVelocityDirection = GetUserFacingDirection();
                    dwellWithdrawCurrentVelocityDirection = Vector3.zero;
                    dwellWithdrawPositionHistory.Clear();
                    ballSpawner.intargetTimes++;
                    handInBall2 = true;
                }

                if (isMineBall)
                {
                    Player.TurnShallowYellowMineBall();
                }
                else
                {
                    Player.TurnShallowGreenMineBall();
                }

                ballSpawner.area_type = 1;
                ballSpawner.index = ballIndex;
                logWriter3.isMineBall = isMineBall;
                logWriter3.handInBall = true;
            });

            Acce.HesEvent.AddListener(() =>
            {
                if (dwellWithdrawCancelTriggered || dwellWithdrawReentryRequired)
                {
                    return;
                }

                if (isMineBall)
                {
                    logWriter3.successfully_Canceled = false;
                    ballSpawner.caseTime++;
                    ballSpawner.PlaySoundWrong();
                    ballSpawner.getNewTarget();
                    ballSpawner.triggerCondition = Acce.condition;
                }
                else
                {
                    ballSpawner.caseTime++;
                    ballSpawner.PlaySound2();
                    ballSpawner.getNewTarget();
                    ballSpawner.triggerCondition = Acce.condition;
                }

                logWriter3.handInBall = false;
                logWriter3.triggerTime = 0f;
                ResetDwellWithdrawState();
            });
        }

        Acce.AfterOutEvent.AddListener(() =>
        {
            bool wasBlocked = dwellWithdrawReentryRequired;

            handInBall2 = false;
            inBall = false;
            dwellWithdrawInside = false;
            dwellWithdrawEntryTimestamp = -1f;
            dwellWithdrawCancelTriggered = false;
            dwellWithdrawReentryRequired = false;
            dwellWithdrawEntryVelocityDirection = Vector3.zero;
            dwellWithdrawCurrentVelocityDirection = Vector3.zero;
            dwellWithdrawPositionHistory.Clear();
            Acce.UnInvoked();

            if (isMineBall && !wasBlocked)
            {
                ballSpawner.PlaySound2();
                logWriter3.successfully_Canceled = true;
                logWriter3.RecordCancelReason("contact transition");
                Player.ChangeOpacityBack();
                ballSpawner.getNewTarget();
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
            }
            else if (!wasBlocked)
            {
                if (!ballSpawner.triggered)
                {
                    logWriter3.RecordCancelReason("contact transition");
                    Player.ChangeOpacityBack();
                    ballSpawner.area_type = 3;
                    ballSpawner.index = -1;
                }
            }
            else
            {
                Player.ChangeOpacityBack();
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
            }

            Player.ChangeOpacityBack();
            logWriter3.handInBall = false;
            logWriter3.triggerTime = 0f;
            useIndex = false;
        });
    }

    public void SetFastTapMode()
    {
        Acce.InEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger || fastTapReentryRequired)
            {
                return;
            }

            if (isMineBall)
            {
                if (!handInBall2)
                {
                    Player.TurnShallowYellowMineBall();
                    logWriter3.handInBall = true;
                    fastTapEntryTimestamp = Time.time;
                    fastTapReactTimestamp = -1f;
                    handInBall2 = true;
                    ballSpawner.intargetTimes++;
                }
            }
            else
            {
                if (!handInBall2)
                {
                    Player.TurnShallowGreenMineBall();
                    logWriter3.handInBall = true;
                    fastTapEntryTimestamp = Time.time;
                    fastTapReactTimestamp = -1f;
                    handInBall2 = true;
                    ballSpawner.intargetTimes++;
                }
            }

            if (!rainbowEntryCaptured)
            {
                rainbowEntryPosition = Acce.HasSurfaceEntryPoint ? Acce.SurfaceEntryPoint : Acce.IndexTipPose2.Position;
                rainbowEntryCaptured = true;
            }
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            logWriter3.handInBall = false;
            handInBall2 = false;
            if (!handInBall)
            {
                fastTapReentryRequired = false;
                ResetFastTapTiming();
                logWriter3.triggerTime = 0f;
            }
        });

        reactTap.InEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger || fastTapReentryRequired)
            {
                return;
            }

            if (!handInBall2)
            {
                handInBall2 = true;
                ballSpawner.intargetTimes++;
                logWriter3.handInBall = true;
                fastTapEntryTimestamp = Time.time;
                fastTapReactTimestamp = -1f;

                if (isMineBall)
                {
                    Player.TurnShallowYellowMineBall();
                }
                else
                {
                    Player.TurnShallowGreenMineBall();
                }
            }

            if (!logWriter3.isReact)
            {
                reactTap.initialAngle = reactTap.CalculateAngle_between_finger_wrist();
                ballSpawner.isReact = true;
                logWriter3.isReact = true;
                logWriter3.reactFrequency++;
                fastTapReactTimestamp = Time.time;
                logWriter3.reactTime = fastTapEntryTimestamp >= 0f
                    ? fastTapReactTimestamp - fastTapEntryTimestamp
                    : 0f;

                if (isMineBall)
                {
                    Player.TurnShallowYellowMineBall();
                }
                else
                {
                    Player.TurnShallowGreenMineBall();
                }
            }

            handInBall = true;
            canceled = false;
            fastTapCancelTriggered = false;
            logWriter3.handInBall = true;
            ballSpawner.area_type = 1;
            ballSpawner.index = ballIndex;

            if (!inBall)
            {
                inBall = true;
            }

            if (ballSpawner.lastBall != null)
            {
                ballSpawner.lastBall.GetComponent<MeshRenderer>().material = ballSpawner.ordinaryBall;
            }

            if (ballSpawner.enable_preview && ballSpawner.nextNextBall != null)
            {
                ballSpawner.nextNextBall.GetComponent<MeshRenderer>().material = ballSpawner.redBall;
            }
        });

        if (modeSelected != (int)BallSpawner.Mode.swing010)
        {
            reactTap.CancelEvent.AddListener(() => TriggerFastTapCancel("Dwell"));
        }

        reactTap.AfterOutEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger)
            {
                return;
            }

            if (modeSelected != (int)BallSpawner.Mode.swing010
                && handInBall
                && !fastTapCancelTriggered
                && !canceled
                && !fastTapReentryRequired
                && HasFastTapTimedOut())
            {
                    TriggerFastTapCancel("Dwell");
            }

            bool shouldConfirm = handInBall && !fastTapCancelTriggered && !canceled && !fastTapReentryRequired;

            handInBall = false;
            inBall = false;
            canceled = false;
            fastTapCancelTriggered = false;
            logWriter3.handInBall = false;
            logWriter3.isReact = false;
            ballSpawner.isReact = false;
            Player.ChangeOpacityBack();
            Player.hideCircle();

            if (!shouldConfirm)
            {
                if (!ballSpawner.triggered)
                {
                    ballSpawner.area_type = 3;
                    ballSpawner.index = -1;
                }
                ResetFastTapTiming();
                return;
            }

            if (isMineBall)
            {
                logWriter3.successfully_Canceled = false;
                logWriter3.isMineBall = true;
                ballSpawner.caseTime++;
                ballSpawner.PlaySoundWrong();
            }
            else
            {
                logWriter3.successfully_Canceled = false;
                logWriter3.isMineBall = false;
                ballSpawner.caseTime++;
                ballSpawner.PlaySound2();
            }

            ballSpawner.getNewTarget();
            ResetFastTapTiming();
            logWriter3.triggerTime = 0f;
            logWriter3.reactTime = 0f;
            logWriter3.cancelTime = 0f;
            logWriter3.cancelTimes = 0;
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
        // Target-ball Release Tap:
        // 1. Enter ReactTap using the same front-end gates as ReactTap.
        // 2. If the folded fingers open while inside, cancel like Fast Tap.
        // 3. If the hand leaves the target ball without cancellation, confirm the hit.
        EnsureReleaseGesture();

        Acce.InEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger || fastTapReentryRequired)
            {
                return;
            }

            if (isMineBall)
            {
                if (!handInBall2)
                {
                    Player.TurnShallowYellowMineBall();
                    logWriter3.handInBall = true;
                    fastTapEntryTimestamp = Time.time;
                    fastTapReactTimestamp = -1f;
                    handInBall2 = true;
                    ballSpawner.intargetTimes++;
                }
            }
            else
            {
                if (!handInBall2)
                {
                    Player.TurnShallowGreenMineBall();
                    logWriter3.handInBall = true;
                    fastTapEntryTimestamp = Time.time;
                    fastTapReactTimestamp = -1f;
                    handInBall2 = true;
                    ballSpawner.intargetTimes++;
                }
            }
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            logWriter3.handInBall = false;
            handInBall2 = false;
            if (!handInBall)
            {
                fastTapReentryRequired = false;
                ResetFastTapTiming();
                ResetReleaseGestureTracking();
                logWriter3.triggerTime = 0f;
            }
        });

        reactTap.InEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger || fastTapReentryRequired)
            {
                return;
            }

            bool enteredReactTap = !handInBall;

            if (!handInBall2)
            {
                handInBall2 = true;
                ballSpawner.intargetTimes++;
                logWriter3.handInBall = true;
                fastTapEntryTimestamp = Time.time;
                fastTapReactTimestamp = -1f;

                if (isMineBall)
                {
                    Player.TurnShallowYellowMineBall();
                }
                else
                {
                    Player.TurnShallowGreenMineBall();
                }
            }

            if (!logWriter3.isReact)
            {
                // React stage is still driven by ReactTap's angle/movement checks.
                reactTap.initialAngle = reactTap.CalculateAngle_between_finger_wrist();
                ballSpawner.isReact = true;
                logWriter3.isReact = true;
                logWriter3.reactFrequency++;
                fastTapReactTimestamp = Time.time;
                logWriter3.reactTime = fastTapEntryTimestamp >= 0f
                    ? fastTapReactTimestamp - fastTapEntryTimestamp
                    : 0f;

                if (isMineBall)
                {
                    Player.TurnShallowYellowMineBall();
                }
                else
                {
                    Player.TurnShallowGreenMineBall();
                }
            }

            handInBall = true;
            canceled = false;
            fastTapCancelTriggered = false;
            logWriter3.handInBall = true;
            ballSpawner.area_type = 1;
            ballSpawner.index = ballIndex;

            if (enteredReactTap)
            {
                ResetReleaseGestureTracking();
            }

            if (!inBall)
            {
                inBall = true;
            }

            if (ballSpawner.lastBall != null)
            {
                ballSpawner.lastBall.GetComponent<MeshRenderer>().material = ballSpawner.ordinaryBall;
            }

            if (ballSpawner.enable_preview && ballSpawner.nextNextBall != null)
            {
                ballSpawner.nextNextBall.GetComponent<MeshRenderer>().material = ballSpawner.redBall;
            }
        });

        reactTap.AfterOutEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger)
            {
                return;
            }

            // Leaving the target ball confirms only if no release-cancel or other cancel happened.
            bool shouldConfirm = handInBall && !fastTapCancelTriggered && !canceled && !fastTapReentryRequired;

            handInBall = false;
            inBall = false;
            canceled = false;
            fastTapCancelTriggered = false;
            logWriter3.handInBall = false;
            logWriter3.isReact = false;
            ballSpawner.isReact = false;
            Player.ChangeOpacityBack();
            Player.hideCircle();
            ResetReleaseGestureTracking();

            if (!shouldConfirm)
            {
                if (!ballSpawner.triggered)
                {
                    ballSpawner.area_type = 3;
                    ballSpawner.index = -1;
                }
                ResetFastTapTiming();
                return;
            }

            if (isMineBall)
            {
                logWriter3.successfully_Canceled = false;
                logWriter3.isMineBall = true;
                ballSpawner.caseTime++;
                ballSpawner.PlaySoundWrong();
            }
            else
            {
                logWriter3.successfully_Canceled = false;
                logWriter3.isMineBall = false;
                ballSpawner.caseTime++;
                ballSpawner.PlaySound2();
            }

            ballSpawner.getNewTarget();
            ResetFastTapTiming();
            logWriter3.triggerTime = 0f;
            logWriter3.reactTime = 0f;
            logWriter3.cancelTime = 0f;
            logWriter3.cancelTimes = 0;
        });
    }

    public void SetReactReleaseMode()
    {
        EnsureReleaseGesture();
        ResetReactReleaseState();

        Acce.InEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger || reactReleaseReentryRequired)
            {
                return;
            }

            if (!handInBall2)
            {
                handInBall2 = true;
                ballSpawner.intargetTimes++;
                logWriter3.handInBall = true;
                fastTapEntryTimestamp = Time.time;
                fastTapReactTimestamp = -1f;
                ResetReleaseGestureTracking();
            }

            if (isMineBall)
            {
                Player.TurnShallowYellowMineBall();
            }
            else
            {
                Player.TurnShallowGreenMineBall();
            }

            ballSpawner.area_type = 1;
            ballSpawner.index = ballIndex;
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger)
            {
                return;
            }

            bool shouldCancel = handInBall2 && !reactReleaseTriggered;
            if (shouldCancel)
            {
                logWriter3.RecordCancelReason(reactReleaseSpeedAcceMet ? "contact transition" : "contact transition before react");
            }

            ResetReactReleaseState();
            ResetReleaseGestureTracking();
            ResetFastTapTiming();
            logWriter3.handInBall = false;
            logWriter3.isReact = false;
            ballSpawner.isReact = false;
            Player.ChangeOpacityBack();

            if (!ballSpawner.triggered)
            {
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
            }
        });

        reactTap.InEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger || reactReleaseReentryRequired || reactReleaseTriggered)
            {
                return;
            }

            if (!handInBall2)
            {
                handInBall2 = true;
                ballSpawner.intargetTimes++;
                logWriter3.handInBall = true;
                fastTapEntryTimestamp = Time.time;
                if (isMineBall)
                {
                    Player.TurnShallowYellowMineBall();
                }
                else
                {
                    Player.TurnShallowGreenMineBall();
                }
            }

            if (!reactReleaseSpeedAcceMet)
            {
                reactTap.initialAngle = reactTap.CalculateAngle_between_finger_wrist();
                ballSpawner.isReact = true;
                logWriter3.isReact = true;
                logWriter3.reactFrequency++;
                fastTapReactTimestamp = Time.time;
                logWriter3.reactTime = fastTapEntryTimestamp >= 0f
                    ? fastTapReactTimestamp - fastTapEntryTimestamp
                    : reactTap.UpdateT + 0.02f;
                ResetReleaseGestureTracking();
            }

            reactReleaseSpeedAcceMet = true;
            handInBall = true;
            inBall = true;
            canceled = false;
            ballSpawner.area_type = 1;
            ballSpawner.index = ballIndex;

            CheckReactReleaseIndexTrigger();
        });
    }

    public void SetSpeedReleaseMode()
    {
        EnsureReleaseGesture();
        ResetSpeedReleaseState();

        Acce.InEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger || speedReleaseReentryRequired)
            {
                return;
            }

            if (!handInBall)
            {
                handInBall = true;
                handInBall2 = true;
                inBall = true;
                canceled = false;
                speedReleaseMinSpeedMet = false;
                ballSpawner.intargetTimes++;
                logWriter3.handInBall = true;
                fastTapEntryTimestamp = Time.time;
                ResetReleaseGestureTracking();
            }

            if (isMineBall)
            {
                Player.TurnShallowYellowMineBall();
            }
            else
            {
                Player.TurnShallowGreenMineBall();
            }

            ballSpawner.area_type = 1;
            ballSpawner.index = ballIndex;
            CheckSpeedReleaseIndexTrigger();
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            ResetSpeedReleaseState();
            ResetReleaseGestureTracking();
            ResetFastTapTiming();
            logWriter3.handInBall = false;
            Player.ChangeOpacityBack();

            if (!ballSpawner.triggered)
            {
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
            }
        });
    }

    public void SetSpeedMode()
    {
        ResetSpeedReleaseState();

        Acce.InEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger || speedReleaseReentryRequired)
            {
                return;
            }

            if (!handInBall)
            {
                handInBall = true;
                handInBall2 = true;
                inBall = true;
                canceled = false;
                speedReleaseMinSpeedMet = false;
                ballSpawner.intargetTimes++;
                logWriter3.handInBall = true;
                fastTapEntryTimestamp = Time.time;
            }

            if (isMineBall)
            {
                Player.TurnShallowYellowMineBall();
            }
            else
            {
                Player.TurnShallowGreenMineBall();
            }

            ballSpawner.area_type = 1;
            ballSpawner.index = ballIndex;
            CheckSpeedReleaseIndexTrigger();
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            if (handInBall && !speedReleaseTriggered)
            {
                TriggerSpeedCancel(speedReleaseMinSpeedMet ? "contact transition" : "contact transition before speed-min");
            }

            ResetSpeedReleaseState();
            ResetFastTapTiming();
            logWriter3.handInBall = false;
            Player.ChangeOpacityBack();

            if (!ballSpawner.triggered)
            {
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
            }
        });
    }

    public void SetSpeedDirectionMode()
    {
        ResetSpeedReleaseState();
        ResetDirection010State();

        Acce.InEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger || speedReleaseReentryRequired || direction010ReentryRequired)
            {
                return;
            }

            if (!handInBall)
            {
                handInBall = true;
                handInBall2 = true;
                inBall = true;
                canceled = false;
                speedReleaseMinSpeedMet = false;
                direction010CancelTriggered = false;
                direction010PositionHistory.Clear();
                direction010HasSmoothedDirection = false;
                direction010SmoothedDirection = Vector3.zero;
                ballSpawner.intargetTimes++;
                logWriter3.handInBall = true;
                fastTapEntryTimestamp = Time.time;
            }

            if (isMineBall)
            {
                Player.TurnShallowYellowMineBall();
            }
            else
            {
                Player.TurnShallowGreenMineBall();
            }

            ballSpawner.area_type = 1;
            ballSpawner.index = ballIndex;

            CheckSpeedReleaseIndexTrigger();

            if (speedReleaseMinSpeedMet
                && !speedReleaseTriggered
                && !speedReleaseReentryRequired
                && IsDirection010CancelMovement())
            {
                HideModeReadyRing();
                TriggerDirection010Cancel();
            }
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            if (handInBall && !speedReleaseTriggered && !direction010CancelTriggered)
            {
                TriggerSpeedCancel(speedReleaseMinSpeedMet ? "contact transition" : "contact transition before speed-min");
            }

            ResetSpeedReleaseState();
            ResetDirection010State();
            ResetFastTapTiming();
            logWriter3.handInBall = false;
            Player.ChangeOpacityBack();

            if (!ballSpawner.triggered)
            {
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
            }
        });
    }

    public void SetSpeedDwellMode()
    {
        ResetSpeedReleaseState();

        Acce.InEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger || speedReleaseReentryRequired)
            {
                return;
            }

            if (!handInBall)
            {
                handInBall = true;
                handInBall2 = true;
                inBall = true;
                canceled = false;
                speedReleaseMinSpeedMet = false;
                ballSpawner.intargetTimes++;
                logWriter3.handInBall = true;
                fastTapEntryTimestamp = Time.time;
            }

            if (isMineBall)
            {
                Player.TurnShallowYellowMineBall();
            }
            else
            {
                Player.TurnShallowGreenMineBall();
            }

            ballSpawner.area_type = 1;
            ballSpawner.index = ballIndex;
            CheckSpeedReleaseIndexTrigger();
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            ResetSpeedReleaseState();
            ResetFastTapTiming();
            logWriter3.handInBall = false;
            Player.ChangeOpacityBack();

            if (!ballSpawner.triggered)
            {
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
            }
        });
    }

    public void SetSpeedWristTiltMode()
    {
        ResetSpeedReleaseState();
        ResetPinchWristTiltState();

        Acce.InEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger || speedReleaseReentryRequired)
            {
                return;
            }

            if (!handInBall)
            {
                handInBall = true;
                handInBall2 = true;
                inBall = true;
                pinchWristTiltInside = true;
                pinchWristTiltPinching = true;
                pinchWristTiltEntryTimestamp = Time.time;
                pinchWristTiltHasEntryRotation = TryGetWristRotation(out pinchWristTiltEntryRotation);
                canceled = false;
                speedReleaseMinSpeedMet = false;
                ballSpawner.intargetTimes++;
                logWriter3.handInBall = true;
                fastTapEntryTimestamp = Time.time;
            }

            if (isMineBall)
            {
                Player.TurnShallowYellowMineBall();
            }
            else
            {
                Player.TurnShallowGreenMineBall();
            }

            ballSpawner.area_type = 1;
            ballSpawner.index = ballIndex;
            CheckSpeedWristTiltCancel();
            CheckSpeedReleaseIndexTrigger();
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            bool shouldCancel = handInBall && !speedReleaseTriggered;
            if (shouldCancel)
            {
                logWriter3.RecordCancelReason(speedReleaseMinSpeedMet ? "contact transition" : "contact transition before speed-min");
            }

            ResetSpeedReleaseState();
            ResetPinchWristTiltState();
            ResetFastTapTiming();
            logWriter3.handInBall = false;
            Player.ChangeOpacityBack();

            if (!ballSpawner.triggered)
            {
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
            }
        });
    }

    public void SetRainbow()
    {
        ResetRainbowState();

        Acce.InEvent.AddListener(() =>
        {
            if (rainbowReentryRequiresExit)
            {
                return;
            }

            if (isMineBall && ballSpawner.enable_trigger)
            {
                if (!handInBall2)
                {
                    Player.TurnYellowMineBall();
                    logWriter3.handInBall = true;
                    handInBall2 = true;
                    ballSpawner.intargetTimes++;
                }
            }
            else if (ballSpawner.enable_trigger)
            {
                if (!handInBall2)
                {
                    Player.TurnGreenMineBall();
                    logWriter3.handInBall = true;
                    handInBall2 = true;
                    ballSpawner.intargetTimes++;
                }
            }

            if (!rainbowEntryCaptured)
            {
                rainbowEntryPosition = Acce.HasSurfaceEntryPoint ? Acce.SurfaceEntryPoint : Acce.IndexTipPose2.Position;
                rainbowEntryCaptured = true;
            }
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            bool surfaceExitCancel = isMineBall
                && ballSpawner.enable_trigger
                && handInBall2
                && !handInBall
                && !logWriter3.isReact
                && !rainbowCancelTriggered;

            if (surfaceExitCancel)
            {
                TriggerSurfaceExitCancel();
                ResetRainbowState();
            }
            else
            {
                if (ballSpawner.enable_trigger && handInBall2 && !handInBall && !logWriter3.isReact && !ballSpawner.triggered)
                {
                    logWriter3.RecordCancelReason("contact transition");
                }

                logWriter3.handInBall = false;
                handInBall2 = false;
                if (!handInBall)
                {
                    logWriter3.triggerTime = 0f;
                }
            }

            rainbowReentryRequiresExit = false;
        });

        reactTap.InEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger)
            {
                return;
            }

            if (rainbowReentryRequiresExit)
            {
                return;
            }

            if (!handInBall2)
            {
                handInBall2 = true;
                ballSpawner.intargetTimes++;
                logWriter3.handInBall = true;

                if (isMineBall)
                {
                    Player.TurnYellowMineBall();
                }
                else
                {
                    Player.TurnGreenMineBall();
                }
            }

            if (!logWriter3.isReact)
            {
                reactTap.initialAngle = reactTap.CalculateAngle_between_finger_wrist();
                ballSpawner.isReact = true;
                logWriter3.isReact = true;
                logWriter3.reactFrequency++;
                logWriter3.reactTime = reactTap.UpdateT + 0.02f;

                if (isMineBall)
                {
                    Player.TurnShallowYellowMineBall();
                }
                else
                {
                    Player.TurnShallowGreenMineBall();
                }
            }

            handInBall = true;
            canceled = false;
            rainbowCancelTriggered = false;
            rainbowConfirmBlocked = false;
            logWriter3.handInBall = true;
            ballSpawner.area_type = 1;
            ballSpawner.index = ballIndex;

            if (!inBall)
            {
                inBall = true;
            }

            if (ballSpawner.lastBall != null)
            {
                ballSpawner.lastBall.GetComponent<MeshRenderer>().material = ballSpawner.ordinaryBall;
            }

            if (ballSpawner.enable_preview && ballSpawner.nextNextBall != null)
            {
                ballSpawner.nextNextBall.GetComponent<MeshRenderer>().material = ballSpawner.redBall;
            }

            if (!rainbowArcInitialized)
            {
                ShowRainbowArc();
            }
        });

        reactTap.AfterOutEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger)
            {
                HideRainbowArc();
                return;
            }

            if (handInBall && rainbowArcActive && !rainbowCancelTriggered && !rainbowConfirmBlocked)
            {
                CheckRainbowCancel();
            }

            bool shouldConfirm = handInBall && !rainbowCancelTriggered && !canceled && !rainbowConfirmBlocked;

            handInBall = false;
            inBall = false;
            rainbowCancelTriggered = false;
            rainbowArcInitialized = false;
            rainbowEntryCaptured = false;
            logWriter3.handInBall = false;
            logWriter3.isReact = false;
            ballSpawner.isReact = false;
            Player.ChangeOpacityBack();
            Player.hideCircle();
            HideRainbowArc();

            if (!shouldConfirm)
            {
                if (!ballSpawner.triggered)
                {
                    ballSpawner.area_type = 3;
                    ballSpawner.index = -1;
                }
                rainbowConfirmBlocked = false;
                canceled = false;
                return;
            }

            if (isMineBall)
            {
                logWriter3.successfully_Canceled = false;
                logWriter3.isMineBall = true;
                ballSpawner.caseTime++;
                ballSpawner.PlaySoundWrong();
            }
            else
            {
                logWriter3.successfully_Canceled = false;
                logWriter3.isMineBall = false;
                ballSpawner.caseTime++;
                ballSpawner.PlaySound2();
            }

            rainbowConfirmBlocked = false;
            canceled = false;
            ballSpawner.getNewTarget();
            logWriter3.triggerTime = 0f;
            logWriter3.reactTime = 0f;
            logWriter3.cancelTime = 0f;
            logWriter3.cancelTimes = 0;
        });
    }
    public void SetReactTapMode()
    {
        reactTap.ChangeEvent.AddListener(() =>
        {
            logWriter2.currentInBallDistance = Vector3.Distance(reactTap.speedAcceMetPosition, reactTap.IndexTipPose2.Position);;

        });
        Acce.InEvent.AddListener(() =>
        {
            if (isMineBall && ballSpawner.enable_trigger)
            {
                if (!handInBall2)
                {
                    Player.TurnYellowMineBall();
                    print("��͸��");
                    logWriter3.handInBall = true;
                    handInBall2 = true;
                    ballSpawner.intargetTimes++;

                }
            }
            else if (ballSpawner.enable_trigger)
            {
                if (!handInBall2)
                {
                    Player.TurnGreenMineBall();
                    print("��͸��");
                    logWriter3.handInBall = true;
                    handInBall2 = true;
                    ballSpawner.intargetTimes++;
                }
            }

        });
        Acce.AfterOutEvent.AddListener(() =>
        {
            if (isMineBall && ballSpawner.enable_trigger)
            {

                if (!logWriter3.isReact)
                {
                    logWriter3.cancelTimes += 1;
                    logWriter3.RecordCancelReason("contact transition");
                    canceled = true;
                    //Player.hideCircle();
                    logWriter3.isReact = false;
                    ballSpawner.isReact = false;
                    logWriter3.handInBall = false;
                    print("cancel__2");
                    //��ʱӦ�ü�¼ȡ������ʱ��
                    logWriter3.cancelTime = reactTap.UpdateT + 0.02f;
                    logWriter3.successfully_Canceled = true;
                    ballSpawner.caseTime++;
                    logWriter3.isMineBall = true;
                    ballSpawner.PlaySound2();
                    ballSpawner.getNewTarget();
                    logWriter3.cancelTime = 0f;
                    logWriter3.triggerTime = 0f;
                    logWriter3.cancelTimes = 0;
                }


                logWriter3.handInBall = false;
                handInBall2 = false;
                if (!logWriter3.isReact)
                {
                    logWriter3.triggerTime = 0f;
                    //logWriter3.reactTime = 0f;
                }
            }
            else if (ballSpawner.enable_trigger)
            {
                if (handInBall2 && !logWriter3.isReact)
                {
                    logWriter3.RecordCancelReason("contact transition");
                }

                logWriter3.handInBall = false;
                handInBall2 = false;
                if (!logWriter3.isReact)
                {
                    logWriter3.triggerTime = 0f;
                    //logWriter3.reactTime = 0f;
                }
            }


        });
        print("���뷴Ӧʽ���ģʽ");
        reactTap.InEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger)
            {
                return;
            }
            if (!handInBall2)
            {
                handInBall2 = true;
                ballSpawner.intargetTimes++;
                logWriter3.handInBall = true;
                if (isMineBall)
                {
                    Player.TurnYellowMineBall();
                }
                else
                {
                    Player.TurnGreenMineBall();
                }
            }

            if (isMineBall && ballSpawner.enable_trigger)
            {
                print("in����");
                if (!logWriter3.isReact)
                {
                    // ��¼��ʼ�н�
                    reactTap.initialAngle = reactTap.CalculateAngle_between_finger_wrist(); // ���� CalculateAngle_between_finger_wrist �������ڼ���н�
                    Debug.Log("initial angle:" + reactTap.initialAngle);
                    ballSpawner.isReact = true;
                    logWriter3.isReact = true;
                    logWriter3.reactFrequency++;
                    Player.TurnShallowYellowMineBall();
                    logWriter3.reactTime = reactTap.UpdateT + 0.02f;
                }
            }
            else if (ballSpawner.enable_trigger)
            {
                //print("in����");
                if (!logWriter3.isReact)
                {
                    // ��¼��ʼ�н�
                    reactTap.initialAngle = reactTap.CalculateAngle_between_finger_wrist(); // ���� CalculateAngle_between_finger_wrist �������ڼ���н�
                    Debug.Log("initial angle:" + reactTap.initialAngle);
                    ballSpawner.isReact = true;
                    logWriter3.isReact = true;
                    logWriter3.reactFrequency++;
                    Player.TurnShallowGreenMineBall();
                    logWriter3.reactTime = reactTap.UpdateT + 0.02f;
                }

            }
            handInBall = true;
            ballSpawner.area_type = 1;
            ballSpawner.index = ballIndex;
            //print("��Ŀ������");
            if (inBall == false)
            {

                inBall = true;
            }
            if (ballSpawner.lastBall != null)
            {
                ballSpawner.lastBall.GetComponent<MeshRenderer>().material = ballSpawner.ordinaryBall;
            }
            if (ballSpawner.enable_preview)
            {
                if (ballSpawner.nextNextBall != null)
                {
                    ballSpawner.nextNextBall.GetComponent<MeshRenderer>().material = ballSpawner.redBall;
                }
            }

        });

        // reactTap.HesEvent.AddListener(() =>
        // {
        //     if (isMineBall)
        //     {
        //         canceled = true;
        //         Player.hideCircle();
        //         logWriter3.isReact = false;
        //         logWriter3.handInBall = false;
        //         //��ʱӦ�ü�¼ȡ������ʱ��
        //     }
        //     else
        //     {
        //         canceled = true;
        //         Player.hideCircle();
        //         logWriter3.isReact = false;
        //         logWriter3.handInBall = false;
        //         logWriter3.triggerTime = 0f;
        //         //logWriter3.reactTime = 0f;
        //     }

        // });

        reactTap.CancelEvent.AddListener(() =>
        {
            if (isMineBall && ballSpawner.enable_trigger)
            {
                logWriter3.cancelTimes += 1;
                canceled = true;
                //Player.hideCircle();
                logWriter3.isReact = false;
                ballSpawner.isReact = false;
                logWriter3.handInBall = false;
                print("cancel__");
                //��ʱӦ�ü�¼ȡ������ʱ��
                logWriter3.cancelTime = reactTap.UpdateT + 0.02f;
                logWriter3.successfully_Canceled = true;
                ballSpawner.caseTime++;
                logWriter3.isMineBall = true;
                ballSpawner.PlaySound2();
                ballSpawner.getNewTarget();
                logWriter3.cancelTime = 0f;
                logWriter3.triggerTime = 0f;
                logWriter3.cancelTimes = 0;
            }
            else if (ballSpawner.enable_trigger)
            {
                if (ballSpawner.enableMineBall)
                {
                    ballSpawner.PlaySoundWrong();
                }
                if (ballSpawner.enable_preview)
                {
                    if (ballSpawner.nextNextBall != null)
                    {
                        ballSpawner.nextNextBall.GetComponent<MeshRenderer>().material = ballSpawner.ordinaryBall;
                    }
                }
                canceled = true;
                Player.hideCircle();
                logWriter3.isReact = false;
                ballSpawner.isReact = false;
                logWriter3.handInBall = false;
                logWriter3.triggerTime = 0f;
                logWriter3.cancelTime = 0f;
                logWriter3.cancelTimes += 1;
                //logWriter3.reactTime = 0f;
            }

        });

        reactTap.AfterOutEvent.AddListener(() =>
        {
            if (isMineBall && ballSpawner.enable_trigger)
            {

                canceled = false;
                handInBall = false;
                Player.ChangeOpacityBack();
                print("��ز�͸��");
                if (!ballSpawner.triggered)
                {
                    Player.ChangeOpacityBack();
                    print("������ʧ��");
                    ballSpawner.area_type = 3;
                    ballSpawner.index = -1;
                    print(555);
                }
                logWriter3.isReact = false;
                ballSpawner.isReact = false;
                Player.hideCircle();

                
            }
            else if (ballSpawner.enable_trigger)
            {
                //Player2.TurnShallowBack();
                canceled = false;
                handInBall = false;
                Player.ChangeOpacityBack();
                print("��ز�͸��");
                if (!ballSpawner.triggered)
                {
                    Player.ChangeOpacityBack();
                    print("������ʧ��");
                    ballSpawner.area_type = 3;
                    ballSpawner.index = -1;
                    print(555);
                }
                logWriter3.isReact = false;
                ballSpawner.isReact = false;
                // logWriter3.triggerTime = 0f;
                //logWriter3.reactTime = 0f;
                Player.hideCircle();
            }
            logWriter3.handInBall = false;
            
        });

        reactTap.OutEvent.AddListener(() =>
        {
            if (isMineBall && ballSpawner.enable_trigger)
            {
                if (canceled)
                {



                }
                else
                {
                    logWriter3.successfully_Canceled = false;
                    logWriter3.isMineBall = true;
                    ballSpawner.caseTime++;
                    ballSpawner.PlaySoundWrong();
                    ballSpawner.getNewTarget();                
                }
            }
            else if (ballSpawner.enable_trigger)
            {
                print("out����");
                if (!canceled)
                {
                    logWriter3.isMineBall = false;
                    ballSpawner.caseTime++;
                    ballSpawner.PlaySound2();
                    ballSpawner.getNewTarget();

                }

                //����������ʱ�����ж��������棬���������Ҫȡ����˼�� lyx
                //Player.TurnYellow();
                print("��ɫ");
            }
            logWriter3.triggerTime = 0f;
            logWriter3.reactTime = 0f;
            logWriter3.isReact = false;
            ballSpawner.isReact = false;
            handInBall = false;
            logWriter3.cancelTime = 0f;
            logWriter3.handInBall = false;
            logWriter3.cancelTimes = 0;
        });
    }
    public void SetFastMode()
    {
        if (Player != null)
        {
            //Acce.InEvent.AddListener(Player.ChangeOpacity);
            //Acce.HesEvent.AddListener(ballSpawner.changeOpacity);
            Acce.InEvent.AddListener(() =>
            {
                ballSpawner.area_type = ballKind;
                ballSpawner.index = ballIndex;
                print("��Ŀ������");
                if (inBall == false)
                {
                    ballSpawner.caseTime++;
                    print("����111");
                    print(Acce.IndexTipPose2.Position.x);
                    ballSpawner.PlaySound2();
                    ballSpawner.getNewTarget();

                    inBall = true;
                    //print(111);
                }

            });
            //Acce.OutEvent.AddListener(ballSpawner.getNewTarget);
        }
        Acce.AfterOutEvent.AddListener(() =>
        {
            if (!ballSpawner.triggered)
            {
                print("������ʧ��");
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
                print(555);
            }
        });
    }

    public void SetPressMode()
    {
        if (Player != null)
        {
            Acce.InEvent.AddListener(() =>
            {
                if (!ballSpawner.enable_trigger || pressTriggered)
                {
                    return;
                }

                ballSpawner.area_type = ballKind;
                ballSpawner.index = ballIndex;
                ballSpawner.triggeredBy = "index";
                logWriter3.isMineBall = isMineBall;
                logWriter3.handInBall = true;

                if (!handInBall2)
                {
                    handInBall2 = true;
                    canceled = false;
                    ballSpawner.intargetTimes++;
                    if (isMineBall)
                    {
                        Player.TurnShallowYellowMineBall();
                    }
                    else
                    {
                        Player.TurnShallowGreenMineBall();
                    }
                }

                if (Acce.v > ballSpawner.pressSpeedThershold)
                {
                    TriggerPressMode();
                }
            });
        }

        Acce.AfterOutEvent.AddListener(() =>
        {
            if (!pressTriggered)
            {
                if (handInBall2)
                {
                    canceled = true;
                    logWriter3.cancelTime = 0f;
                    logWriter3.cancelTimes += 1;
                    logWriter3.isMineBall = isMineBall;
                    logWriter3.successfully_Canceled = isMineBall;
                    logWriter3.RecordCancelReason("contact transition");
                    if (isMineBall)
                    {
                        ballSpawner.caseTime++;
                        ballSpawner.PlaySound2();
                        Player.ChangeOpacityBack();
                        ballSpawner.area_type = 3;
                        ballSpawner.index = -1;
                        logWriter3.handInBall = false;
                        logWriter3.triggerTime = 0f;
                        ballSpawner.getNewTarget();
                        logWriter3.cancelTime = 0f;
                        logWriter3.cancelTimes = 0;
                        return;
                    }

                    PlayNonMineCancelSound();

                    Player.ChangeOpacityBack();
                    ballSpawner.area_type = 3;
                    ballSpawner.index = -1;
                    logWriter3.handInBall = false;
                    logWriter3.triggerTime = 0f;
                }

                Player.ChangeOpacityBack();
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
            }

            handInBall2 = false;
            inBall = false;
            pressTriggered = false;
            logWriter3.handInBall = false;
            logWriter3.triggerTime = 0f;
        });
    }

    private void TriggerPressMode()
    {
        pressTriggered = true;
        ballSpawner.triggerCondition = 'P';

        if (isMineBall)
        {
            logWriter3.successfully_Canceled = false;
            logWriter3.isMineBall = true;
            ballSpawner.caseTime++;
            ballSpawner.PlaySoundWrong();
        }
        else
        {
            logWriter3.isMineBall = false;
            ballSpawner.caseTime++;
            ballSpawner.PlaySound2();
        }

        logWriter3.handInBall = false;
        logWriter3.triggerTime = 0f;
        ballSpawner.getNewTarget();
    }

    private void HandlePinchReleaseTriggerEnter(bool enteredByIndex)
    {
        if (!ballSpawner.enable_trigger || pinchReleaseTriggerCompleted)
        {
            return;
        }

        if (enteredByIndex)
        {
            if (inBall)
            {
                return;
            }

            inBall = true;
            ballSpawner.triggeredBy = "index";
        }
        else
        {
            if (inBall2)
            {
                return;
            }

            inBall2 = true;
            ballSpawner.triggeredBy = "thumb";
        }

        if (!handInBall2)
        {
            handInBall2 = true;
            pinchReleaseTriggerEntryTimestamp = Time.time;
            logWriter3.triggerTime = 0f;
            ballSpawner.intargetTimes++;
        }

        ballSpawner.area_type = ballKind;
        ballSpawner.index = ballIndex;
        logWriter3.isMineBall = isMineBall;
        logWriter3.handInBall = true;

        if (isMineBall)
        {
            Player.TurnShallowYellowMineBall();
        }
        else
        {
            Player.TurnShallowGreenMineBall();
        }
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
            ballSpawner.area_type = ballKind;
            ballSpawner.index = ballIndex;
            logWriter3.handInBall = true;
            return;
        }

        if (!pinchReleaseTriggerCompleted && (pinchReleaseTriggerArmed || pinchob.isPinch))
        {
            CancelPinchReleaseTrigger("hold-contact-transition");
        }
        else
        {
            ResetPinchReleaseTriggerVisuals();
        }

        ResetPinchReleaseTriggerState();
    }

    private void TriggerPinchReleaseTriggerSelection()
    {
        pinchReleaseTriggerCompleted = true;
        ballSpawner.triggerCondition = 'P';

        if (isMineBall)
        {
            logWriter3.successfully_Canceled = false;
            logWriter3.isMineBall = true;
            ballSpawner.caseTime++;
            ballSpawner.PlaySoundWrong();
        }
        else
        {
            logWriter3.successfully_Canceled = false;
            logWriter3.isMineBall = false;
            ballSpawner.caseTime++;
            ballSpawner.PlaySound2();
        }

        logWriter3.triggerTime = GetPinchReleaseTriggerElapsedTime();
        logWriter3.handInBall = false;
        ballSpawner.getNewTarget();
        ResetPinchReleaseTriggerState();
    }

    private void CancelPinchReleaseTrigger(string cancelReason)
    {
        canceled = true;
        float elapsedTime = GetPinchReleaseTriggerElapsedTime();
        logWriter3.triggerTime = elapsedTime;
        logWriter3.cancelTime = elapsedTime;
        logWriter3.cancelTimes += 1;
        logWriter3.isMineBall = isMineBall;
        logWriter3.successfully_Canceled = isMineBall;
        logWriter3.RecordCancelReason(cancelReason);

        if (isMineBall)
        {
            ballSpawner.caseTime++;
            ballSpawner.PlaySound2();
            ResetPinchReleaseTriggerVisuals();
            ballSpawner.getNewTarget();
            logWriter3.cancelTime = 0f;
            logWriter3.cancelTimes = 0;
            return;
        }

        PlayNonMineCancelSound();
        ResetPinchReleaseTriggerVisuals();
    }

    private void ResetPinchReleaseTriggerVisuals()
    {
        Player.ChangeOpacityBack();
        ballSpawner.area_type = 3;
        ballSpawner.index = -1;
        logWriter3.handInBall = false;
    }

    private void ResetPinchReleaseTriggerState()
    {
        HideModeReadyRing();
        pinchReleaseTriggerArmed = false;
        pinchReleaseTriggerCompleted = false;
        pinchReleaseTriggerEntryTimestamp = -1f;
        handInBall2 = false;
        inBall = false;
        inBall2 = false;
        canceled = false;
    }

    private float GetPinchReleaseTriggerElapsedTime()
    {
        if (pinchReleaseTriggerEntryTimestamp < 0f)
        {
            return 0f;
        }

        return Mathf.Max(0f, Time.time - pinchReleaseTriggerEntryTimestamp);
    }

    private void HandlePinchDirectionEnter(bool enteredByIndex)
    {
        if (!ballSpawner.enable_trigger
            || pinchReleaseTriggerCompleted
            || pinchDirectionReentryRequired)
        {
            return;
        }

        HandlePinchReleaseTriggerEnter(enteredByIndex);
    }

    private void HandlePinchDirectionExit(bool exitedByIndex)
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
            if (!pinchDirectionCancelTriggered && !pinchDirectionReentryRequired)
            {
                ballSpawner.area_type = ballKind;
                ballSpawner.index = ballIndex;
                logWriter3.handInBall = true;
            }
            return;
        }

        if (pinchDirectionCancelTriggered || pinchDirectionReentryRequired)
        {
            ResetPinchReleaseTriggerVisuals();
            ResetPinchReleaseTriggerState();
            ResetPinchDirectionCancelState();
            return;
        }

        ResetPinchReleaseTriggerVisuals();
        ResetPinchReleaseTriggerState();
        ResetPinchDirectionCancelState();
    }

    private void TriggerPinchDirectionCancel()
    {
        if (pinchDirectionCancelTriggered)
        {
            return;
        }

        pinchDirectionCancelTriggered = true;
        pinchDirectionReentryRequired = true;
        pinchReleaseTriggerArmed = false;
        HideModeReadyRing();
        TriggerSurfaceExitCancel("direction");
        ballSpawner.area_type = 3;
        ballSpawner.index = -1;
    }

    private bool IsPinchDirectionCancelMovement()
    {
        if (ballSpawner == null)
        {
            return false;
        }

        Vector3 windowMovement = GetPinchDirectionWindowMovement(out float centerSpeed);
        if (windowMovement.sqrMagnitude <= 0.0000001f)
        {
            return false;
        }

        float minSpeed = GetPinchDirectionMinSpeedThreshold();
        if (minSpeed > 0f && centerSpeed < minSpeed)
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

        Vector3 cdDirection = new Vector3(-abDirection.y, abDirection.x, 0f).normalized;
        Vector3 smoothedDirection = SmoothPinchDirectionDirection(windowMovement.normalized);
        float angle = Vector3.Angle(smoothedDirection, cdDirection);
        float cdAngleDelta = Mathf.Min(angle, 180f - angle);
        return cdAngleDelta <= GetPinchDirectionAngleThreshold();
    }

    private Vector3 GetPinchDirectionWindowMovement(out float centerSpeed)
    {
        centerSpeed = 0f;
        if (!TryGetPinchDirectionCenterPoint(out Vector3 currentCenter))
        {
            return Vector3.zero;
        }

        int windowFrameCount = GetPinchDirectionWindowFrameCount();
        pinchDirectionCenterPositionHistory.Enqueue(currentCenter);

        while (pinchDirectionCenterPositionHistory.Count > windowFrameCount)
        {
            pinchDirectionCenterPositionHistory.Dequeue();
        }

        if (pinchDirectionCenterPositionHistory.Count < windowFrameCount)
        {
            return Vector3.zero;
        }

        Vector3[] positions = pinchDirectionCenterPositionHistory.ToArray();
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
        centerSpeed = (totalSegmentDistance / validSegmentCount) / sampleDeltaTime;
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

    private void ResetPinchDirectionCancelState()
    {
        pinchDirectionCancelTriggered = false;
        pinchDirectionReentryRequired = false;
        ResetPinchDirectionTrackingWindow();
    }

    private void ResetPinchDirectionTrackingWindow()
    {
        pinchDirectionCenterPositionHistory.Clear();
        pinchDirectionHasSmoothedDirection = false;
        pinchDirectionSmoothedDirection = Vector3.zero;
    }

    private void HandlePinchThumbSwipeIndexEnter()
    {
        if (!ballSpawner.enable_trigger || pinchThumbSwipeCompleted || pinchThumbSwipeCanceled || pinchThumbSwipeReentryRequired)
        {
            return;
        }

        if (!inBall)
        {
            inBall = true;
            ballSpawner.intargetTimes++;
            pinchThumbSwipeEntryTimestamp = Time.time;
            logWriter3.triggerTime = 0f;
        }

        ballSpawner.area_type = ballKind;
        ballSpawner.index = ballIndex;
        ballSpawner.triggeredBy = "index";
        logWriter3.isMineBall = isMineBall;
        logWriter3.handInBall = true;

        if (isMineBall)
        {
            Player.TurnShallowYellowMineBall();
        }
        else
        {
            Player.TurnShallowGreenMineBall();
        }
    }

    private void HandlePinchThumbSwipeIndexExit()
    {
        inBall = false;

        if (pinchThumbSwipeReentryRequired)
        {
            ClearPinchThumbSwipeReentry();
            return;
        }

        if (pinchThumbSwipeCompleted || pinchThumbSwipeCanceled)
        {
            return;
        }

        if (!pinchThumbSwipeTracking && !pinchob.isPinch)
        {
            ResetPinchThumbSwipeVisuals();
            ResetPinchThumbSwipeState();
        }
    }

    private bool TryCapturePinchThumbSwipeStart()
    {
        if (!inBall)
        {
            return false;
        }

        if (!TryGetPinchThumbSwipeData(out _, out Vector3 thumbPosition, out _, out _, out _, out float thumbProjectionOnIndex))
        {
            return false;
        }

        pinchThumbSwipeStartThumbPosition = thumbPosition;
        pinchThumbSwipeLastThumbPosition = thumbPosition;
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

        if (!TryGetPinchThumbSwipeData(out _, out Vector3 thumbPosition, out _, out float contactDistance, out _, out _))
        {
            pinchThumbSwipeCurrentContactDistance = float.MaxValue;
            return false;
        }

        pinchThumbSwipeCurrentContactDistance = contactDistance;
        pinchThumbSwipeLastThumbPosition = thumbPosition;
        return true;
    }

    private void ResolvePinchThumbSwipeEnd()
    {
        if (!UpdatePinchThumbSwipeContactDistance())
        {
            ResetPinchThumbSwipeVisuals();
            ResetPinchThumbSwipeState();
            return;
        }

        float contactThreshold = Mathf.Max(0f, ballSpawner.pinchThumbSwipeContactDistanceThreshold);
        if (pinchThumbSwipeCurrentContactDistance > contactThreshold)
        {
            TriggerPinchThumbSwipeSelection();
        }
        else
        {
            CancelPinchThumbSwipe("thumb-contact");
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

    private void TriggerPinchThumbSwipeSelection()
    {
        pinchThumbSwipeCompleted = true;
        pinchThumbSwipeReentryRequired = true;
        ballSpawner.triggerCondition = 'P';

        if (isMineBall)
        {
            logWriter3.successfully_Canceled = false;
            logWriter3.isMineBall = true;
            ballSpawner.caseTime++;
            ballSpawner.PlaySoundWrong();
        }
        else
        {
            logWriter3.successfully_Canceled = false;
            logWriter3.isMineBall = false;
            ballSpawner.caseTime++;
            ballSpawner.PlaySound2();
        }

        logWriter3.triggerTime = GetPinchThumbSwipeElapsedTime();
        logWriter3.handInBall = false;
        ballSpawner.getNewTarget();
        ResetPinchThumbSwipeTracking();
    }

    private void CancelPinchThumbSwipe(string cancelReason)
    {
        if (pinchThumbSwipeCanceled || pinchThumbSwipeCompleted || !ballSpawner.enable_trigger)
        {
            return;
        }

        pinchThumbSwipeCanceled = true;
        pinchThumbSwipeReentryRequired = true;
        canceled = true;
        float elapsedTime = GetPinchThumbSwipeElapsedTime();
        logWriter3.triggerTime = elapsedTime;
        logWriter3.cancelTime = elapsedTime;
        logWriter3.cancelTimes += 1;
        logWriter3.isMineBall = isMineBall;
        logWriter3.successfully_Canceled = isMineBall;
        logWriter3.RecordCancelReason(cancelReason);

        if (isMineBall)
        {
            ballSpawner.caseTime++;
            ballSpawner.PlaySound2();
            ResetPinchThumbSwipeVisuals();
            ballSpawner.getNewTarget();
            logWriter3.cancelTime = 0f;
            logWriter3.cancelTimes = 0;
            ResetPinchThumbSwipeTracking();
            return;
        }

        PlayNonMineCancelSound();
        ResetPinchThumbSwipeVisuals();
        ResetPinchThumbSwipeTracking();
    }

    private void ResetPinchThumbSwipeVisuals()
    {
        Player.ChangeOpacityBack();
        ballSpawner.area_type = 3;
        ballSpawner.index = -1;
        logWriter3.handInBall = false;
    }

    private void ResetPinchThumbSwipeState()
    {
        HideModeReadyRing();
        pinchThumbSwipeTracking = false;
        pinchThumbSwipeCompleted = false;
        pinchThumbSwipeCanceled = false;
        pinchThumbSwipeReentryRequired = false;
        pinchThumbSwipeEntryTimestamp = -1f;
        pinchThumbSwipeStartThumbPosition = Vector3.zero;
        pinchThumbSwipeLastThumbPosition = Vector3.zero;
        pinchThumbSwipeStartProjection = 0f;
        pinchThumbSwipeCurrentContactDistance = float.MaxValue;
        inBall = false;
        inBall2 = false;
        handInBall2 = false;
        canceled = false;
    }

    private void ResetPinchThumbSwipeTracking()
    {
        HideModeReadyRing();
        pinchThumbSwipeTracking = false;
        pinchThumbSwipeStartThumbPosition = Vector3.zero;
        pinchThumbSwipeLastThumbPosition = Vector3.zero;
        pinchThumbSwipeStartProjection = 0f;
        pinchThumbSwipeCurrentContactDistance = float.MaxValue;
    }

    private void ClearPinchThumbSwipeReentry()
    {
        pinchThumbSwipeReentryRequired = false;
        pinchThumbSwipeCompleted = false;
        pinchThumbSwipeCanceled = false;
        canceled = false;
        ResetPinchThumbSwipeVisuals();
        ResetPinchThumbSwipeTracking();
    }

    private float GetPinchThumbSwipeElapsedTime()
    {
        if (pinchThumbSwipeEntryTimestamp < 0f)
        {
            return 0f;
        }

        return Mathf.Max(0f, Time.time - pinchThumbSwipeEntryTimestamp);
    }

    private void HandlePinchWristTiltIndexEnter()
    {
        if (!ballSpawner.enable_trigger
            || pinchWristTiltCompleted
            || pinchWristTiltCanceled
            || pinchWristTiltReentryRequired)
        {
            return;
        }

        if (!pinchWristTiltInside)
        {
            pinchWristTiltInside = true;
            pinchWristTiltEntryTimestamp = Time.time;
            ballSpawner.intargetTimes++;
            logWriter3.triggerTime = 0f;
        }

        ballSpawner.area_type = ballKind;
        ballSpawner.index = ballIndex;
        ballSpawner.triggeredBy = "index";
        logWriter3.isMineBall = isMineBall;
        logWriter3.handInBall = true;

        if (isMineBall)
        {
            Player.TurnShallowYellowMineBall();
        }
        else
        {
            Player.TurnShallowGreenMineBall();
        }
    }

    private void HandlePinchWristTiltIndexExit()
    {
        pinchWristTiltInside = false;

        if (pinchWristTiltReentryRequired)
        {
            ClearPinchWristTiltReentry();
            return;
        }

        if (pinchWristTiltCompleted || pinchWristTiltCanceled)
        {
            return;
        }

        ResetPinchWristTiltVisuals();
        ResetPinchWristTiltState();
    }

    private void HandleWristTiltIndexEnter()
    {
        if (!ballSpawner.enable_trigger
            || pinchWristTiltCompleted
            || pinchWristTiltCanceled
            || pinchWristTiltReentryRequired)
        {
            return;
        }

        if (!pinchWristTiltInside)
        {
            pinchWristTiltInside = true;
            pinchWristTiltPinching = true;
            pinchWristTiltEntryTimestamp = Time.time;
            pinchWristTiltHasEntryRotation = TryGetWristRotation(out pinchWristTiltEntryRotation);
            ballSpawner.intargetTimes++;
            logWriter3.triggerTime = 0f;
        }

        ballSpawner.area_type = ballKind;
        ballSpawner.index = ballIndex;
        ballSpawner.triggeredBy = "index";
        logWriter3.isMineBall = isMineBall;
        logWriter3.handInBall = true;

        if (isMineBall)
        {
            Player.TurnShallowYellowMineBall();
        }
        else
        {
            Player.TurnShallowGreenMineBall();
        }
    }

    private void HandleWristTiltIndexExit()
    {
        bool wasInside = pinchWristTiltInside;
        pinchWristTiltInside = false;

        if (pinchWristTiltReentryRequired)
        {
            ClearPinchWristTiltReentry();
            return;
        }

        if (!wasInside || pinchWristTiltCompleted || pinchWristTiltCanceled)
        {
            return;
        }

        if (IsPinchWristTiltPastThreshold())
        {
            CancelPinchWristTilt("wrist-tilt");
            return;
        }

        TriggerWristTiltSelection();
    }

    private void HandleDwellWristTiltIndexEnter()
    {
        if (!ballSpawner.enable_trigger
            || pinchWristTiltCompleted
            || pinchWristTiltCanceled
            || pinchWristTiltReentryRequired)
        {
            return;
        }

        if (!pinchWristTiltInside)
        {
            pinchWristTiltInside = true;
            pinchWristTiltPinching = true;
            pinchWristTiltEntryTimestamp = Time.time;
            pinchWristTiltHasEntryRotation = TryGetWristRotation(out pinchWristTiltEntryRotation);
            ballSpawner.intargetTimes++;
            logWriter3.triggerTime = 0f;
        }

        ballSpawner.area_type = ballKind;
        ballSpawner.index = ballIndex;
        ballSpawner.triggeredBy = "index";
        logWriter3.isMineBall = isMineBall;
        logWriter3.handInBall = true;

        if (isMineBall)
        {
            Player.TurnShallowYellowMineBall();
        }
        else
        {
            Player.TurnShallowGreenMineBall();
        }
    }

    private void HandleDwellWristTiltIndexExit()
    {
        bool wasInside = pinchWristTiltInside;
        pinchWristTiltInside = false;

        if (pinchWristTiltReentryRequired)
        {
            ClearPinchWristTiltReentry();
            return;
        }

        if (!wasInside || pinchWristTiltCompleted || pinchWristTiltCanceled)
        {
            return;
        }

        ResetPinchWristTiltVisuals();
        ResetPinchWristTiltState();
    }

    private void UpdateDwellWristTiltSelection()
    {
        if (IsPinchWristTiltPastThreshold())
        {
            CancelPinchWristTilt("wrist-tilt");
            return;
        }

        if (HasDwellWristTiltTimedOut())
        {
            TriggerDwellWristTiltSelection();
        }
    }

    private void CheckPinchWristTiltCancel()
    {
        if (IsPinchWristTiltPastThreshold())
        {
            CancelPinchWristTilt("wrist-tilt");
        }
    }

    private void CheckSpeedWristTiltCancel()
    {
        if (IsPinchWristTiltPastThreshold())
        {
            TriggerSpeedWristTiltCancel();
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

    private void TriggerPinchWristTiltSelection()
    {
        TriggerPinchWristTiltSelection('P');
    }

    private void TriggerWristTiltSelection()
    {
        TriggerPinchWristTiltSelection('W');
    }

    private void TriggerDwellWristTiltSelection()
    {
        TriggerPinchWristTiltSelection('D');
    }

    private void TriggerPinchWristTiltSelection(char triggerCondition)
    {
        if (pinchWristTiltCompleted || pinchWristTiltCanceled || !ballSpawner.enable_trigger)
        {
            return;
        }

        pinchWristTiltCompleted = true;
        pinchWristTiltReentryRequired = true;
        ballSpawner.triggerCondition = triggerCondition;

        if (isMineBall)
        {
            logWriter3.successfully_Canceled = false;
            logWriter3.isMineBall = true;
            ballSpawner.caseTime++;
            ballSpawner.PlaySoundWrong();
        }
        else
        {
            logWriter3.successfully_Canceled = false;
            logWriter3.isMineBall = false;
            ballSpawner.caseTime++;
            ballSpawner.PlaySound2();
        }

        logWriter3.triggerTime = GetPinchWristTiltElapsedTime();
        logWriter3.handInBall = false;
        ballSpawner.getNewTarget();
        ResetPinchWristTiltTracking();
    }

    private void CancelPinchWristTilt(string cancelReason)
    {
        if (pinchWristTiltCanceled || pinchWristTiltCompleted || !ballSpawner.enable_trigger)
        {
            return;
        }

        pinchWristTiltCanceled = true;
        pinchWristTiltReentryRequired = true;
        canceled = true;
        float elapsedTime = GetPinchWristTiltElapsedTime();
        logWriter3.triggerTime = elapsedTime;
        logWriter3.cancelTime = elapsedTime;
        logWriter3.cancelTimes += 1;
        logWriter3.isMineBall = isMineBall;
        logWriter3.successfully_Canceled = isMineBall;
        logWriter3.RecordCancelReason(cancelReason);

        if (isMineBall)
        {
            ballSpawner.caseTime++;
            ballSpawner.PlaySound2();
            ResetPinchWristTiltVisuals();
            ballSpawner.getNewTarget();
            logWriter3.cancelTime = 0f;
            logWriter3.cancelTimes = 0;
            ResetPinchWristTiltTracking();
            return;
        }

        PlayNonMineCancelSound();
        ResetPinchWristTiltVisuals();
        ResetPinchWristTiltTracking();
    }

    private void PlayNonMineCancelSound()
    {
        if (ballSpawner != null && ballSpawner.enableMineBall)
        {
            ballSpawner.PlaySoundWrong();
        }
    }

    private void ResetPinchWristTiltVisuals()
    {
        Player.ChangeOpacityBack();
        ballSpawner.area_type = 3;
        ballSpawner.index = -1;
        logWriter3.handInBall = false;
    }

    private void ResetPinchWristTiltState()
    {
        HideModeReadyRing();
        pinchWristTiltInside = false;
        pinchWristTiltCompleted = false;
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
        HideModeReadyRing();
        pinchWristTiltInside = false;
        pinchWristTiltPinching = false;
        pinchWristTiltEntryRotation = Quaternion.identity;
        pinchWristTiltHasEntryRotation = false;
    }

    private void ClearPinchWristTiltReentry()
    {
        pinchWristTiltCompleted = false;
        pinchWristTiltCanceled = false;
        pinchWristTiltReentryRequired = false;
        pinchWristTiltPinching = false;
        canceled = false;
        ResetPinchWristTiltVisuals();
        ResetPinchWristTiltTracking();
    }

    private float GetPinchWristTiltElapsedTime()
    {
        if (pinchWristTiltEntryTimestamp < 0f)
        {
            return 0f;
        }

        return Mathf.Max(0f, Time.time - pinchWristTiltEntryTimestamp);
    }

    private bool HasDwellWristTiltTimedOut()
    {
        float waitTime = ballSpawner != null ? Mathf.Max(0f, ballSpawner.longPressTimeThreshold) : 0.5f;
        return pinchWristTiltEntryTimestamp >= 0f
            && Time.time - pinchWristTiltEntryTimestamp >= waitTime;
    }

    private void CheckSwing010Cancel()
    {
        if (swing010CancelTriggered || fastTapCancelTriggered || fastTapReentryRequired)
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
        TriggerFastTapCancel("swing");
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

    private void TriggerFastTapCancel(string cancelReason)
    {
        if (fastTapCancelTriggered || !handInBall || !ballSpawner.enable_trigger)
        {
            return;
        }

        fastTapCancelTriggered = true;
        fastTapReentryRequired = true;
        TriggerSurfaceExitCancel(cancelReason);
        ResetFastTapTiming();
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
        if (!reactReleaseSpeedAcceMet || reactReleaseTriggered || reactReleaseReentryRequired || !ballSpawner.enable_trigger)
        {
            return;
        }

        if (reactTap.V_vec >= ballSpawner.reactReleaseIndexSpeedThreshold)
        {
            TriggerReactRelease();
        }
    }

    private void CheckReactReleaseCancel()
    {
        if (EnsureReleaseGesture().CheckRelease())
        {
            TriggerReactReleaseCancel();
        }
    }

    private void TriggerReactRelease()
    {
        if (reactReleaseTriggered || reactReleaseReentryRequired || !ballSpawner.enable_trigger)
        {
            return;
        }

        reactReleaseTriggered = true;
        ballSpawner.triggerCondition = 'W';
        logWriter3.handInBall = false;
        logWriter3.isReact = false;
        ballSpawner.isReact = false;

        if (isMineBall)
        {
            logWriter3.successfully_Canceled = false;
            logWriter3.isMineBall = true;
            ballSpawner.caseTime++;
            ballSpawner.PlaySoundWrong();
        }
        else
        {
            logWriter3.successfully_Canceled = false;
            logWriter3.isMineBall = false;
            ballSpawner.caseTime++;
            ballSpawner.PlaySound2();
        }

        ballSpawner.getNewTarget();
        ResetReactReleaseState();
        ResetReleaseGestureTracking();
        ResetFastTapTiming();
        logWriter3.triggerTime = 0f;
        logWriter3.reactTime = 0f;
        logWriter3.cancelTime = 0f;
        logWriter3.cancelTimes = 0;
    }

    private void TriggerReactReleaseCancel()
    {
        if (reactReleaseTriggered || reactReleaseReentryRequired || !ballSpawner.enable_trigger)
        {
            return;
        }

        reactReleaseReentryRequired = true;
        TriggerSurfaceExitCancel("release");
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
        if (speedReleaseTriggered || speedReleaseReentryRequired || !ballSpawner.enable_trigger)
        {
            return;
        }

        float indexSpeed = GetCurrentIndexSpeed();
        if (!speedReleaseMinSpeedMet)
        {
            if (indexSpeed <= Mathf.Max(0f, ballSpawner.speedMinThreshold))
            {
                speedReleaseMinSpeedMet = true;
                ShowModeReadyRing();
                ResetReleaseGestureTracking();
            }

            return;
        }

        if (indexSpeed >= Mathf.Max(0f, ballSpawner.speedMaxThreshold))
        {
            TriggerSpeedRelease();
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

    private void TriggerSpeedRelease()
    {
        if (speedReleaseTriggered || speedReleaseReentryRequired || !ballSpawner.enable_trigger)
        {
            return;
        }

        speedReleaseTriggered = true;
        ballSpawner.triggerCondition = 'S';
        logWriter3.handInBall = false;

        if (isMineBall)
        {
            logWriter3.successfully_Canceled = false;
            logWriter3.isMineBall = true;
            ballSpawner.caseTime++;
            ballSpawner.PlaySoundWrong();
        }
        else
        {
            logWriter3.successfully_Canceled = false;
            logWriter3.isMineBall = false;
            ballSpawner.caseTime++;
            ballSpawner.PlaySound2();
        }

        ballSpawner.getNewTarget();
        ResetSpeedReleaseState();
        ResetReleaseGestureTracking();
        ResetFastTapTiming();
        logWriter3.triggerTime = 0f;
        logWriter3.reactTime = 0f;
        logWriter3.cancelTime = 0f;
        logWriter3.cancelTimes = 0;
    }

    private void TriggerSpeedReleaseCancel()
    {
        if (speedReleaseTriggered || speedReleaseReentryRequired || !ballSpawner.enable_trigger)
        {
            return;
        }

        HideModeReadyRing();
        speedReleaseReentryRequired = true;
        TriggerSurfaceExitCancel("release");
        ResetReleaseGestureTracking();
        ResetFastTapTiming();
    }

    private bool HasSpeedDwellTimedOut()
    {
        return fastTapEntryTimestamp >= 0f
            && Time.time - fastTapEntryTimestamp >= Mathf.Max(0f, ballSpawner.pinchDwellCancelTime);
    }

    private void TriggerSpeedCancel(string cancelReason, bool requireReentry = false)
    {
        if (speedReleaseTriggered || speedReleaseReentryRequired || !ballSpawner.enable_trigger)
        {
            return;
        }

        HideModeReadyRing();
        speedReleaseReentryRequired = requireReentry;
        TriggerSurfaceExitCancel(cancelReason);
        ResetFastTapTiming();
    }

    private void TriggerSpeedWristTiltCancel()
    {
        if (speedReleaseTriggered || speedReleaseReentryRequired || !ballSpawner.enable_trigger)
        {
            return;
        }

        HideModeReadyRing();
        speedReleaseReentryRequired = true;
        CancelPinchWristTilt("wrist-tilt");
        ResetFastTapTiming();
    }

    private void ResetSpeedReleaseState()
    {
        HideModeReadyRing();
        speedReleaseMinSpeedMet = false;
        speedReleaseTriggered = false;
        speedReleaseReentryRequired = false;
        handInBall = false;
        handInBall2 = false;
        inBall = false;
        canceled = false;
    }

    private bool IsPinchReleaseMode()
    {
        return modeSelected == 8;
    }

    private bool IsPinchDwellMode()
    {
        return modeSelected == 9;
    }

    private bool IsPinchCancelMode()
    {
        return IsPinchReleaseMode() || IsPinchDwellMode();
    }

    private bool IsPinchModeFamily()
    {
        return modeSelected == 4 || modeSelected == 8 || modeSelected == 9;
    }

    private bool UpdatePinchTargetExitVisualState()
    {
        if (!IsPinchModeFamily() || (!inBall && !inBall2))
        {
            return false;
        }

        ballSpawner.area_type = 1;
        ballSpawner.index = ballIndex;
        logWriter3.handInBall = true;

        if (isMineBall)
        {
            Player.TurnShallowYellowMineBall();
        }
        else
        {
            Player.TurnShallowGreenMineBall();
        }

        return true;
    }

    private bool IsPinchReleaseBlocked()
    {
        return IsPinchCancelMode() && pinchReleaseReentryRequired;
    }

    private void ResetPinchReleaseState()
    {
        pinchReleaseCancelTriggered = false;
        pinchReleaseReentryRequired = false;
        pinchReleaseWaitForIndexExit = false;
        pinchReleaseWaitForThumbExit = false;
        ResetPinchReleaseTracking();
        ResetPinchDwellTracking();
    }

    private void ResetPinchReleaseTracking()
    {
        if (!IsPinchReleaseMode())
        {
            return;
        }

        EnsureReleaseGesture().ResetGesture();
    }

    private void StartPinchDwellTracking()
    {
        if (!IsPinchDwellMode())
        {
            return;
        }

        pinchDwellEntryTimestamp = Time.time;
        ShowModeReadyRing();
    }

    private void ResetPinchDwellTracking()
    {
        if (IsPinchDwellMode())
        {
            HideModeReadyRing();
        }

        pinchDwellEntryTimestamp = -1f;
    }

    private bool HasPinchDwellTimedOut()
    {
        return IsPinchDwellMode()
            && pinchDwellEntryTimestamp >= 0f
            && Time.time - pinchDwellEntryTimestamp >= ballSpawner.pinchDwellCancelTime;
    }

    private float GetPinchDwellElapsedTime()
    {
        if (pinchDwellEntryTimestamp < 0f)
        {
            return 0f;
        }

        return Mathf.Max(0f, Time.time - pinchDwellEntryTimestamp);
    }

    private void TryTriggerPinchDwellSelection()
    {
        if (!IsPinchDwellMode()
            || pinchHandledThisGesture
            || pinchReleaseCancelTriggered
            || pinchReleaseReentryRequired
            || !ballSpawner.enable_trigger)
        {
            ResetPinchDwellTracking();
            return;
        }

        if (pinchDwellEntryTimestamp < 0f || !inBall)
        {
            ResetPinchDwellTracking();
            return;
        }

        if (HasPinchDwellTimedOut())
        {
            TriggerPinchReleaseCancel("Dwell");
            return;
        }

        if (isMineBall)
        {
            logWriter3.successfully_Canceled = false;
            ballSpawner.caseTime++;
            ballSpawner.PlaySoundWrong();
        }
        else
        {
            ballSpawner.caseTime++;
            ballSpawner.PlaySound2();
        }

        pinchHandledThisGesture = true;
        logWriter3.handInBall = false;
        logWriter3.triggerTime = GetPinchDwellElapsedTime();
        inBall = false;
        inBall2 = false;
        useIndex = false;
        ResetPinchDwellTracking();
        ballSpawner.getNewTarget();
    }

    private void CheckPinchReleaseCancel()
    {
        if (EnsureReleaseGesture().CheckIndexThumbRelease(ballSpawner.pinchReleaseOpeningSpeedThreshold))
        {
            TriggerPinchReleaseCancel("release");
        }
    }

    private void TriggerPinchReleaseCancel(string cancelReason)
    {
        if (pinchReleaseCancelTriggered || pinchReleaseReentryRequired || !ballSpawner.enable_trigger)
        {
            return;
        }

        if (!inBall && !inBall2)
        {
            return;
        }

        pinchReleaseCancelTriggered = true;
        pinchReleaseReentryRequired = true;
        pinchReleaseWaitForIndexExit = inBall;
        pinchReleaseWaitForThumbExit = inBall2;
        pinchHandledThisGesture = true;
        canceled = true;
        inBall = false;
        inBall2 = false;
        useIndex = false;

        logWriter3.handInBall = false;
        logWriter3.triggerTime = 0f;
        logWriter3.cancelTime = 0f;
        logWriter3.cancelTimes += 1;
        logWriter3.successfully_Canceled = false;
        logWriter3.isMineBall = isMineBall;
        logWriter3.RecordCancelReason(cancelReason);
        ballSpawner.area_type = 3;
        ballSpawner.index = -1;

        Player.ChangeOpacityBack();

        if (isMineBall)
        {
            logWriter3.cancelTime = 0f;
            logWriter3.successfully_Canceled = true;
            logWriter3.isMineBall = true;
            ballSpawner.caseTime++;
            ballSpawner.PlaySound2();
            ballSpawner.getNewTarget();
            logWriter3.triggerTime = 0f;
            logWriter3.cancelTimes = 0;
            ResetPinchReleaseState();
            return;
        }

        if (ballSpawner.enableMineBall)
        {
            ballSpawner.PlaySoundWrong();
        }

        if (ballSpawner.enable_preview && ballSpawner.nextNextBall != null)
        {
            ballSpawner.nextNextBall.GetComponent<MeshRenderer>().material = ballSpawner.ordinaryBall;
        }

        ResetPinchReleaseTracking();
        ResetPinchDwellTracking();
    }

    private void ClearPinchReleaseReentryIfReady()
    {
        if (!IsPinchReleaseBlocked() || pinchReleaseWaitForIndexExit || pinchReleaseWaitForThumbExit)
        {
            return;
        }

        pinchReleaseCancelTriggered = false;
        pinchReleaseReentryRequired = false;
        pinchHandledThisGesture = false;
        canceled = false;
        ResetPinchReleaseTracking();
        ResetPinchDwellTracking();
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

    private void ShowModeReadyRing()
    {
        if (ballSpawner == null || !ballSpawner.showModeReadyRing)
        {
            HideModeReadyRing();
            return;
        }

        if (modeReadyRing == null)
        {
            modeReadyRing = GetComponent<FacingFeedbackRing>();
            if (modeReadyRing == null)
            {
                modeReadyRing = gameObject.AddComponent<FacingFeedbackRing>();
            }
        }

        modeReadyRing.Show();
    }

    private void HideModeReadyRing()
    {
        if (modeReadyRing != null)
        {
            modeReadyRing.Hide();
        }
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
        if (dwellReleaseCancelTriggered || dwellReleaseReentryRequired || !ballSpawner.enable_trigger)
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

        logWriter3.handInBall = false;
        logWriter3.triggerTime = 0f;
        logWriter3.cancelTime = 0f;
        logWriter3.cancelTimes += 1;
        logWriter3.successfully_Canceled = false;
        logWriter3.isMineBall = isMineBall;
        logWriter3.RecordCancelReason("release");
        ballSpawner.area_type = 3;
        ballSpawner.index = -1;
        Player.ChangeOpacityBack();

        if (isMineBall)
        {
            logWriter3.successfully_Canceled = true;
            ballSpawner.caseTime++;
            ballSpawner.PlaySound2();
            ballSpawner.getNewTarget();
            logWriter3.cancelTime = 0f;
            logWriter3.triggerTime = 0f;
            logWriter3.cancelTimes = 0;
            ResetDwellReleaseState();
            return;
        }

        if (ballSpawner.enableMineBall)
        {
            ballSpawner.PlaySoundWrong();
        }

        if (ballSpawner.enable_preview && ballSpawner.nextNextBall != null)
        {
            ballSpawner.nextNextBall.GetComponent<MeshRenderer>().material = ballSpawner.ordinaryBall;
        }
    }

    private void ResetDwellReleaseState()
    {
        dwellReleaseCancelTriggered = false;
        dwellReleaseReentryRequired = false;
        dwellReleaseInside = false;
        dwellReleaseEntryTimestamp = -1f;
        Acce.UnInvoked();
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
        if (dwellWithdrawCancelTriggered || dwellWithdrawReentryRequired || !ballSpawner.enable_trigger)
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

        logWriter3.handInBall = false;
        logWriter3.triggerTime = 0f;
        logWriter3.cancelTime = 0f;
        logWriter3.cancelTimes += 1;
        logWriter3.successfully_Canceled = false;
        logWriter3.isMineBall = isMineBall;
        logWriter3.RecordCancelReason("movement");
        ballSpawner.area_type = 3;
        ballSpawner.index = -1;
        Player.ChangeOpacityBack();

        if (isMineBall)
        {
            logWriter3.successfully_Canceled = true;
            ballSpawner.caseTime++;
            ballSpawner.PlaySound2();
            ballSpawner.getNewTarget();
            logWriter3.cancelTime = 0f;
            logWriter3.triggerTime = 0f;
            logWriter3.cancelTimes = 0;
            ResetDwellWithdrawState();
            return;
        }

        if (ballSpawner.enableMineBall)
        {
            ballSpawner.PlaySoundWrong();
        }

        if (ballSpawner.enable_preview && ballSpawner.nextNextBall != null)
        {
            ballSpawner.nextNextBall.GetComponent<MeshRenderer>().material = ballSpawner.ordinaryBall;
        }
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
        Acce.UnInvoked();
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
            TriggerFastTapCancel("release");
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
        rainbowArcInitialized = false;
        rainbowEntryCaptured = false;
        HideRainbowArc();

        if (isMineBall)
        {
            TriggerSurfaceExitCancel("contact transition");
            return;
        }

        canceled = true;
        handInBall = false;
        handInBall2 = false;
        inBall = false;
        rainbowReentryRequiresExit = true;
        logWriter3.isReact = false;
        ballSpawner.isReact = false;
        logWriter3.handInBall = false;
        logWriter3.successfully_Canceled = false;
        logWriter3.isMineBall = false;
        logWriter3.cancelTimes += 1;
        logWriter3.triggerTime = 0f;
        logWriter3.cancelTime = 0f;
        Player.hideCircle();
        Player.ChangeOpacityBack();

        if (ballSpawner.enableMineBall)
        {
            ballSpawner.PlaySoundWrong();
        }

        if (ballSpawner.enable_preview && ballSpawner.nextNextBall != null)
        {
            ballSpawner.nextNextBall.GetComponent<MeshRenderer>().material = ballSpawner.ordinaryBall;
        }
    }

    private void TriggerSurfaceExitCancel(string cancelReason = "contact transition")
    {
        canceled = true;
        handInBall = false;
        handInBall2 = false;
        inBall = false;
        logWriter3.isReact = false;
        ballSpawner.isReact = false;
        logWriter3.handInBall = false;
        Player.hideCircle();
        Player.ChangeOpacityBack();

        if (isMineBall)
        {
            logWriter3.cancelTimes += 1;
            logWriter3.cancelTime = reactTap.UpdateT + 0.02f;
            logWriter3.successfully_Canceled = true;
            logWriter3.isMineBall = true;
            logWriter3.RecordCancelReason(cancelReason);
            ballSpawner.caseTime++;
            ballSpawner.PlaySound2();
            ballSpawner.getNewTarget();
            logWriter3.cancelTime = 0f;
            logWriter3.triggerTime = 0f;
            logWriter3.cancelTimes = 0;
        }
        else
        {
            if (ballSpawner.enableMineBall)
            {
                ballSpawner.PlaySoundWrong();
            }

            if (ballSpawner.enable_preview && ballSpawner.nextNextBall != null)
            {
                ballSpawner.nextNextBall.GetComponent<MeshRenderer>().material = ballSpawner.ordinaryBall;
            }

            logWriter3.triggerTime = 0f;
            logWriter3.cancelTime = 0f;
            logWriter3.cancelTimes += 1;
            logWriter3.RecordCancelReason(cancelReason);
        }
    }

    private void TriggerContact010Selection()
    {
        if (!ballSpawner.enable_trigger)
        {
            return;
        }

        canceled = false;
        handInBall = false;
        handInBall2 = false;
        inBall = false;
        logWriter3.handInBall = false;
        logWriter3.successfully_Canceled = false;
        logWriter3.isMineBall = isMineBall;
        Player.ChangeOpacityBack();
        Player.hideCircle();

        if (isMineBall)
        {
            ballSpawner.caseTime++;
            ballSpawner.PlaySoundWrong();
        }
        else
        {
            ballSpawner.caseTime++;
            ballSpawner.PlaySound2();
        }

        ballSpawner.getNewTarget();
        logWriter3.triggerTime = 0f;
        logWriter3.reactTime = 0f;
        logWriter3.cancelTime = 0f;
        logWriter3.cancelTimes = 0;
    }

    private void TriggerDirection010Cancel()
    {
        if (direction010CancelTriggered)
        {
            return;
        }

        direction010CancelTriggered = true;
        direction010ReentryRequired = true;
        TriggerSurfaceExitCancel("direction");
        ballSpawner.area_type = 3;
        ballSpawner.index = -1;
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

    private void ResetContact010State()
    {
        handInBall = false;
        handInBall2 = false;
        inBall = false;
        canceled = false;
        logWriter3.handInBall = false;
        ResetRainbowState();
    }

    private void ResetDirection010State()
    {
        handInBall = false;
        handInBall2 = false;
        inBall = false;
        canceled = false;
        direction010CancelTriggered = false;
        direction010ReentryRequired = false;
        ResetDirection010TrackingWindow();
        logWriter3.handInBall = false;
        logWriter3.isReact = false;
        ballSpawner.isReact = false;
        Player.ChangeOpacityBack();
        Player.hideCircle();
    }

    private void ResetDirection010TrackingWindow()
    {
        direction010PositionHistory.Clear();
        direction010HasSmoothedDirection = false;
        direction010SmoothedDirection = Vector3.zero;
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
        HideModeReadyRing();
    }



}
