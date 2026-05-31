using Oculus.Platform.Samples.VrHoops;
using System.Collections;
using System.Collections.Generic;
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
    private bool pinchHandledThisGesture = false;
    private bool fastTapCancelTriggered = false;
    private bool fastTapReentryRequired = false;
    private bool fastTapCanceledUntilExit = false;
    private float fastTapEntryTimestamp = -1f;
    private float fastTapReactTimestamp = -1f;
    private const float FastTapCancelTime = 0.5f;
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
                SetRainbowMode();
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
                SetRainbowMode();
                break;
        }
    }
    public void ClearMode()
    {
        ResetRainbowState();
        Acce?.InEvent.RemoveAllListeners();
        Acce?.HesEvent.RemoveAllListeners();
        Acce?.AfterOutEvent.RemoveAllListeners();
        Acce2?.InEvent.RemoveAllListeners();
        Acce2?.InEvent.RemoveAllListeners();

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

