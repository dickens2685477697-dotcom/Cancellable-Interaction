using Oculus.Platform.Samples.VrHoops;
using System.Collections;
using System.Collections.Generic;
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
    private bool pinchHandledThisGesture = false;
    private const float FastTapCancelTime = 0.5f;
    private float fastTapEntryTimestamp = -1f;
    private float fastTapReactTimestamp = -1f;
    private bool fastTapReentryRequired = false;
    public bool isMineBall = false; // 是否为雷球
    private bool fastTapCancelTriggered = false; // FastTap模式下是否已触发取消
    private bool rainbowCancelTriggered = false;
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
    private float rainbowArcThickness = 0.003f;
    private float rainbowArcHalfAngle = 75f;
    private bool rainbowConfirmBlocked = false;
    private bool rainbowReentryRequiresExit = false;
    private float rainbowFrameDepth = 0.0015f;
    //public AudioSource source;
    //public AudioClip clip;

    void Update()
    {
        // FastTap模式:检查停留时间是否超过0.5秒
        if (modeSelected == 5 && handInBall && !fastTapCancelTriggered && !fastTapReentryRequired)
        {
            if (HasFastTapTimedOut())
            {
                TriggerFastTapCancel();
            }
        }
        if (modeSelected == 6 && handInBall && rainbowArcActive && !rainbowCancelTriggered)
        {
            CheckRainbowCancel();
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
        switch (modeSelected)
        {
            case 0:
                SetFastMode();
                break;
            case 1:
                Acce.Delay = true;
                Acce.WaitTime = 0.5F;
                SetHesMode();
                break;
            case 2:
                Acce.Acce = true;
                SetHesMode();
                break;
            case 3:
                SetReactTapMode();
                break;
            case 4:
                SetPinchMode();
                break;
            case 5:
                SetFastTapMode();
                break;
            case 6:
                SetRainbow();
                break;
        }

        //clip = Resources.Load<AudioClip>("Audio/Assets_MRTK_StandardAssets_Audio_MRTK_Manipulation_Start");
        //source = GetComponent<AudioSource>();
        //source.clip = clip;
    }
    
    public void SetPinchMode()
    {
        pinchob.PinchEnd.AddListener(() =>
        {
            pinchHandledThisGesture = false;
        });

        if (Player != null)
        {
            Acce.InEvent.AddListener(() =>
            {
                if (!inBall && !inBall2)
                {
                    useIndex = false;
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
            if (pinchHandledThisGesture)
            {
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
            }
        });
        Acce.AfterOutEvent.AddListener(() =>
        {
            if (isMineBall)
            {
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
        });

        if (Player != null)
        {
            Acce2.InEvent.AddListener(() =>
            {
                if (!inBall && !inBall2)
                {
                    useIndex = false;
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
            if (pinchHandledThisGesture)
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
            }
        });
        Acce2.AfterOutEvent.AddListener(() =>
        {
            if (isMineBall)
            {
                if (!ballSpawner.triggered && !useIndex)
                {
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
                Player.ChangeOpacityBack();
                ballSpawner.getNewTarget();
                ballSpawner.area_type = 3;
                ballSpawner.index = -1;
                
            }
            else
            {
                if (!ballSpawner.triggered)
                {
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
    

    

    public void SetFastTapMode()
    {
        Acce.InEvent.AddListener(() =>
        {
            if (isMineBall && ballSpawner.enable_trigger)
            {
                if (!handInBall2)
                {
                    Player.TurnYellowMineBall();
                    logWriter3.handInBall = true;
                    fastTapEntryTimestamp = Time.time;
                    fastTapReactTimestamp = -1f;
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
            bool surfaceExitCancel = isMineBall
                && ballSpawner.enable_trigger
                && handInBall2
                && !handInBall
                && !logWriter3.isReact
                && !fastTapCancelTriggered;

            if (surfaceExitCancel)
            {
                TriggerSurfaceExitCancel();
                ResetFastTapTiming();
            }
            else
            {
                logWriter3.handInBall = false;
                handInBall2 = false;
                if (!handInBall)
                {
                    fastTapReentryRequired = false;
                    ResetFastTapTiming();
                    logWriter3.triggerTime = 0f;
                }
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

        reactTap.CancelEvent.AddListener(TriggerFastTapCancel);

        reactTap.AfterOutEvent.AddListener(() =>
        {
            if (!ballSpawner.enable_trigger)
            {
                return;
            }

            if (handInBall && !fastTapCancelTriggered && !canceled && !fastTapReentryRequired && HasFastTapTimedOut())
            {
                TriggerFastTapCancel();
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

    private void TriggerFastTapCancel()
    {
        if (fastTapCancelTriggered || !handInBall || !ballSpawner.enable_trigger)
        {
            return;
        }

        fastTapCancelTriggered = true;
        fastTapReentryRequired = true;
        TriggerSurfaceExitCancel();
        ResetFastTapTiming();
    }

    private bool HasFastTapTimedOut()
    {
        return fastTapReactTimestamp >= 0f
            && Time.time - fastTapReactTimestamp > FastTapCancelTime;
    }

    private void ResetFastTapTiming()
    {
        fastTapEntryTimestamp = -1f;
        fastTapReactTimestamp = -1f;
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
            TriggerSurfaceExitCancel();
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

    private void TriggerSurfaceExitCancel()
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
        }
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
