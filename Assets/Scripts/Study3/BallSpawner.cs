using System;
 using System.Collections;
 using System.Collections.Generic;
 using System.Diagnostics;
 using UnityEngine;
 using UnityEngine.Events;
 using TMPro;
 using Microsoft.MixedReality.Toolkit.Utilities;
 using Microsoft.MixedReality.Toolkit.Input;
 using Random = UnityEngine.Random; // 明确指定使用UnityEngine的Random
 using Oculus.Platform.Samples.VrHoops;

 /// <summary>
 /// 核心控制器，负责管理实验流程、生成目标小球和障碍物。
 /// </summary>
 public class BallSpawner : MonoBehaviour
 {
     private struct ErrorTriggerSnapshot
     {
         public Vector3 indexLoc;
         public Vector3 thumbLoc;
         public int areaType;
         public int index;
         public Vector3 obstaclePosition;
     }

     #region Enums

     public enum Mode
     {
         Tap = 0,
         [InspectorName("Magic Tap")]
         magicTap = 2,
         [InspectorName("React Tap")]
         reactTap = 3,
         [InspectorName("React Release")]
         reactRelease = 14,
         [InspectorName("Safe Gate")]
         safeGate = 6,
         [InspectorName("Press")]
         press = 10,
         [InspectorName("Pinch")]
         pinch = 4,
         [InspectorName("Pinch-Wrist-Tilt")]
         pinchWristTilt = 17,
         [InspectorName("Dwell-Wrist-Tilt")]
         dwellWristTilt = 19,
         [InspectorName("Speed-Wrist-Tilt")]
         speedWristTilt = 20,
         [InspectorName("010-Wrist-Tilt")]
         wristTilt010 = 18,
         [InspectorName("Pinch Release Cancel")]
         pinchReleaseCancel = 8,
         [InspectorName("Dwell Withdraw")]
         dwellWithdraw = 12,
         [InspectorName("1. Pinch-Release-Trigger")]
         pinchReleaseTrigger = 13,
         [InspectorName("2. Pinch-Dwell")]
         pinchDwell = 9,
         [InspectorName("3.Pinch-Direction")]
         pinchDirection = 27,
         // Pinch-Thumb-Swipe: pinch starts tracking while the index is inside the ball;
         // a thumb swipe along the index finger cancels, otherwise pinch release selects.
         [InspectorName("4. Pinch-Thumb-Swipe")]
         pinchThumbSwipe = 16,
         [InspectorName("5. Dwell")]
         dwell = 1,
         [InspectorName("6. Dwell-Release")]
         dwellRelease = 11,
         [InspectorName("7.Dwell-Direction")]
         dwellDirection = 28,
         [InspectorName("8. Speed")]
         speed = 21,
         [InspectorName("9. Speed-Dwell")]
         speedDwell = 22,
         [InspectorName("10. Speed-Release")]
         speedRelease = 15,
         [InspectorName("11.Speed-Direction")]
         speedDirection = 26,
         // 010-Release: ReactTap entry logic + release gesture cancel + exit-to-confirm.
         [InspectorName("12. 010-Release")]
         release010 = 7,
         [InspectorName("13. 010-Time")]
         time010 = 5,
         [InspectorName("14.010-direction")]
         direction010 = 24,
         [InspectorName("010-direction-test")]
         direction010Test = 25,
         [InspectorName("15. 010-Contact")]
         contact010 = 23,
         [InspectorName("010-Swing")]
         swing010 = 29
     }

     public enum ID_kind
     {
         High = 0,
         Mid = 1,
         Low = 2,
         Change = 3
     }

     public enum IDLogicMode
     {
         Old = 0,
         New = 1
     }

     public enum RainbowArcPlacement
     {
         OppositeEntry = 0,
         OppositeUserFacing = 1
     }

     #endregion


     #region Serialized Fields & Public Variables

     [Header("实验模式与轮次")]
     public Mode modeSelected;
     public bool able_to_process = true;
     public bool enable_preview = true;
     public bool enableMineBall = false;
     [Tooltip("Mine Ball模式下，食指进入目标球后是否延迟显示黄球/绿球")]
     public bool enableMineBallRevealDelay = false;
     [Tooltip("Mine Ball模式下，延迟显示黄球/绿球的时间阈值，单位秒")]
     public float mineBallRevealDelayTime = 0.5f;
     [Tooltip("Safe Gate圆环位置：当前模式为按入球方向取反；OppositeUserFacing为放到用户朝向那一面的背侧")]
     public RainbowArcPlacement rainbowArcPlacement = RainbowArcPlacement.OppositeEntry;
     [UnityEngine.Serialization.FormerlySerializedAs("showContact010CancelRegion")]
     [Tooltip("010-Contact模式下是否用绿色线条显示Opposite User Facing映射到球背面的触发区域")]
     public bool showContact010TriggerRegion = false;
     [InspectorName("Show Mode Ready Ring")]
     [Tooltip("开启后，Study3部分模式达到可触发准备状态时，在球面向用户的一侧显示灰色圆环")]
     public bool showModeReadyRing = true;
     [UnityEngine.Serialization.FormerlySerializedAs("direction010ParallelAngleThreshold")]
     [UnityEngine.Serialization.FormerlySerializedAs("direction010PerpendicularAngleThreshold")]
     [Tooltip("010-direction取消阈值：球内移动方向与图中cd方向的最大偏差角，单位度")]
     public float direction010CDAngleThreshold = 20f;
     [Range(0f, 1f)]
     [Tooltip("010-direction方向平滑系数：越小越平滑但反应越慢，越大越灵敏")]
     public float direction010SmoothingAlpha = 0.25f;
     [Tooltip("010-direction取消速度阈值：食指速度低于该值时，即使方向接近cd也不取消，单位米/秒")]
     public float direction010MinSpeedThreshold = 0.05f;
     [Min(2)]
     [Tooltip("010-direction方向判断窗口帧数：越小越灵敏，越大越稳定；至少为2帧")]
     public int direction010WindowFrameCount = 10;
     [Tooltip("Pinch-Direction取消阈值：捏合后食指-拇指中心点移动方向与图中cd方向的最大偏差角，单位度")]
     public float pinchDirectionCDAngleThreshold = 20f;
     [Range(0f, 1f)]
     [Tooltip("Pinch-Direction中心点方向平滑系数：越小越平滑但反应越慢，越大越灵敏")]
     public float pinchDirectionSmoothingAlpha = 0.25f;
     [Tooltip("Pinch-Direction取消速度阈值：捏合后食指-拇指中心点速度低于该值时，即使方向接近cd也不取消，单位米/秒")]
     public float pinchDirectionMinSpeedThreshold = 0.05f;
     [Min(2)]
     [Tooltip("Pinch-Direction中心点方向判断窗口帧数：越小越灵敏，越大越稳定；至少为2帧")]
     public int pinchDirectionWindowFrameCount = 10;
     [Tooltip("010-direction-test取消阈值：球内移动方向与图中cd方向或朝向用户方向的最大偏差角，单位度")]
     public float direction010TestCDAngleThreshold = 20f;
     [Range(0f, 1f)]
     [Tooltip("010-direction-test方向平滑系数：越小越平滑但反应越慢，越大越灵敏")]
     public float direction010TestSmoothingAlpha = 0.25f;
     [Tooltip("010-direction-test取消速度阈值：食指速度低于该值时，即使方向接近取消方向也不取消，单位米/秒")]
     public float direction010TestMinSpeedThreshold = 0.05f;
     [Min(2)]
     [Tooltip("010-direction-test方向判断窗口帧数：越小越灵敏，越大越稳定；至少为2帧")]
     public int direction010TestWindowFrameCount = 10;
     [Tooltip("010-Swing取消阈值：进入010-Time触发阶段后，食指尖-手腕连线方向变化达到该角度即取消，单位度；大于180时禁用")]
     public float swing010CancelAngleThreshold = 35f;
     // Inspector threshold for 010-Release cancellation. The value is a delta per frame in meters.
     [Tooltip("010-Release取消阈值：大拇指到中指/无名指/小拇指平均距离的单帧增量，单位米")]
     public float releaseDistanceDeltaThreshold = 0.01f;
     [UnityEngine.Serialization.FormerlySerializedAs("reactReleaseWristSpeedThreshold")]
     [Tooltip("React Release触发阈值：React Tap速度/加速度条件满足后，食指速度达到该值才触发，单位米/秒")]
     public float reactReleaseIndexSpeedThreshold = 0.2f;
     [InspectorName("Speed-min-Threshold")]
     [Tooltip("Speed-Release准备阈值：食指进入球后，速度低于该值才进入待触发状态，单位米/秒")]
     public float speedMinThreshold = 0.05f;
     [InspectorName("Speed-Max-Threshold")]
     [Tooltip("Speed-Release触发阈值：进入待触发状态后，食指速度高于该值触发，单位米/秒")]
     public float speedMaxThreshold = 0.2f;
     [Tooltip("Dwell触发阈值：食指进入目标区域后需要停留的时间，单位秒")]
     public float longPressTimeThreshold = 0.5f;
     [Tooltip("Pinch Release Cancel取消阈值：食指尖到拇指尖张开速度，单位米/秒")]
     public float pinchReleaseOpeningSpeedThreshold = 0.6f;
     [Tooltip("PinchEnd阈值：食指尖到拇指尖距离大于该值时判定松开，单位米")]
     public float pinchEndDistanceThreshold = 0.05f;
     // Current Pinch-Thumb-Swipe resolves on PinchEnd:
     // contact distance greater than the threshold selects; contact distance within
     // the threshold cancels. The legacy travel/ratio fields are retained for
     // serialized scene compatibility but are not used by the current mode.
     [HideInInspector]
     [Tooltip("Pinch-Thumb-Swipe取消阈值：拇指沿食指方向累计滑动距离，单位米")]
     public float pinchThumbSwipeDistanceThreshold = 0.03f;
     [InspectorName("Pinch Thumb Swipe Contact Distance Threshold")]
     [Tooltip("Pinch-Thumb-Swipe贴合阈值：拇指到食指线段的最大距离，单位米")]
     public float pinchThumbSwipeContactDistanceThreshold = 0.018f;
     [HideInInspector]
     [Tooltip("Pinch-Thumb-Swipe方向阈值：拇指位移投影/总位移比例，0-1")]
     public float pinchThumbSwipeDirectionRatioThreshold = 0.75f;
     [Tooltip("Pinch-Dwell取消阈值：进入目标球后未捏合的最大停留时长，单位秒")]
     public float pinchDwellCancelTime = 0.5f;
     [Tooltip("Pinch-Wrist-Tilt取消阈值：捏合后、松开前允许的最大手腕旋转角度，单位度")]
     public float pinchWristTiltCancelAngleThreshold = 30f;
     [InspectorName("press speed thershold")]
     [Tooltip("Press触发阈值：食指进入目标区域后的移动速度，单位米/秒")]
     public float pressSpeedThershold = 0.2f;
     [Tooltip("Dwell Withdraw取消角度阈值：当前食指移动方向与用户面向方向的夹角达到该角度时取消，单位度；大于180时禁用movement取消")]
     public float dwellWithdrawCancelAngleThreshold = 120f;
     [Tooltip("Old: ID 1 - 9. New: ID 1 - 3 maps to old ID 1, 5, 9.")]
     public IDLogicMode idLogicMode = IDLogicMode.Old;
     [Tooltip("Old ID: 1 - 9. New ID: 1 - 3.")]
     public int ID = 1;
     [HideInInspector]
     public int currentRoundID = 1;
     [HideInInspector]
     public int currentCaseNumber = 1;
     
     [Header("是否启用障碍物")]
     public bool enabledOb = true; // 是否启用障碍物
     [Header("非目标球触发控制")]
     [InspectorName("Disable Non-Target Ball Triggers")]
     [Tooltip("开启后，除当前目标球之外的外圈球和随机障碍球不会触发交互/误触事件。")]
     public bool disableNonTargetBallTriggers = false;
     [Header("目标球生成规则")]
     [Tooltip("起始球的索引")]
     public int start;
     [Tooltip("旋转方向 (0或1)")]
     public int rotationDirection;

     [Header("场景预制件")]
     public GameObject ObstaclePrefab;
     public GameObject TargetPrefab;

     [Header("圆形布局属性")]
     public float CircleRadius;
     public int NumberOfBalls;

     [Header("小球属性")]
     public float BallSize = 0.06f;
     public int ObNum = 11;
     public Material ordinaryBall;
     public Material redBall;
     public Material pinkBall;
     public Material greenBall;
     public Material yellowBall;

     [Header("父对象 & UI")]
     public GameObject ParentObject;
     public GameObject Got, GoNext;

     [Header("音频")]
     public AudioSource sourceRight;
     public AudioSource sourceWrong;

     #endregion

     #region Public State Variables

     // 目标球相关列表和索引
     public List<GameObject> TargetObjects = new List<GameObject>();
     public List<GameObject> obstacleObjects = new List<GameObject>();
     public List<GameObject> allBalls = new List<GameObject>();
     public int TargetStart = 0, TargetEnd;
     public List<int> TargetTriggered, TargetList = new List<int>();
     public List<Vector3> tatgetPos = new List<Vector3>();

     public int expRound = 0;
     // 实验数据记录
     public float usedTime;
     public int errorTimes, errorInTimes, intargetTimes;
     public List<float> errorX;
     public List<float> errorY;
     public List<float> errorZ;
     public List<float> errorObstacleX;
     public List<float> errorObstacleY;
     public List<float> errorObstacleZ;
     public Vector3 indexLoc, thumbLoc;
     public char triggerCondition;
     public float minD;
     public string triggeredBy = "index";

     // 其他状态变量
     public bool triggered = false;
     public int initialBall;
     public int expTime = 0;
     public List<int> seeds;
     public int seed = 0;

     #endregion

     #region Unity Events

     public UnityEvent RoundOverEvent, ExpStartEvent, ExpEndEvent, HandInBallEvent;

     #endregion

     #region Private Fields

     private readonly string sArguments = @"SpawnBall.py"; // Python脚本文件名
     private bool expStarted = false;
     public List<Vector2> coordinates = new List<Vector2>();
     private float startTime;

     // 预计算的圆形位置
     private List<float> oriBallX = new List<float>();
     private List<float> oriBallY = new List<float>();

     public GameObject lastBall, nextNextBall;

     // 边界检测相关
     public Bounds bounds;
     public int inBound = 0;
     public Vector3 largeCirclePoint = Vector3.zero;

     // ----- [新增] 用于存储雷球索引的列表 -----
     public List<int> mineIndices = new List<int>();
     
     // ----- [新增] 对象池相关字段 -----
     public Queue<GameObject> obstaclePool = new Queue<GameObject>();
     private GameObject poolParent; // 用于存放非活动对象的父对象
     private int initialPoolSize = 200; // 对象池初始容量

     #endregion

     // -------------------------------------------------------------------------------------------
     public int area_type;
     public int index;
     public int caseTime = 0;
     public bool enable_trigger = true;
     public bool errorTrigered = false;
     public bool isReact = false;
     private readonly Queue<ErrorTriggerSnapshot> pendingErrorTriggerSnapshots = new Queue<ErrorTriggerSnapshot>();

     #region Unity Lifecycle Methods

    void Start()
     {
         EnsureErrorListsInitialized();
         ID = ClampID(ID);
         currentRoundID = ID;
         currentCaseNumber = 1;
         // 初始化UI和组件引用
         GameObject.Find("temp").GetComponent<Renderer>().enabled = false;
         GoNext = GameObject.Find("GoNext");
         Got = GameObject.Find("Got");

         // 预计算16个基础位置的坐标
         for (int i = 0; i < 16; i++)
         {
             float angle1 = i * (360f / 16);
             oriBallX.Add(CircleRadius * Mathf.Cos(angle1 * Mathf.Deg2Rad) + CircleRadius);
             oriBallY.Add(CircleRadius * Mathf.Sin(angle1 * Mathf.Deg2Rad) + CircleRadius);
         }
         
         // Obstacle pool is initialized lazily when obstacle balls are actually needed.
     }
     
     /// <summary>
     /// 初始化障碍物对象池
     /// </summary>
     private void InitializeObjectPool()
     {
         if (poolParent != null)
         {
             return;
         }

         if (ObstaclePrefab == null)
         {
             UnityEngine.Debug.LogWarning($"{name}: ObstaclePrefab is not assigned; obstacle pool initialization skipped.");
             return;
         }

         poolParent = new GameObject("ObstaclePool");
         poolParent.transform.SetParent(transform, false);
         for (int i = 0; i < initialPoolSize; i++)
         {
             GameObject obj = Instantiate(ObstaclePrefab);
             obj.transform.SetParent(poolParent.transform);
             obj.SetActive(false);
             obstaclePool.Enqueue(obj);
         }
     }

    void FixedUpdate()
    {

        // 持续追踪手部关节点位置
        Handedness handedness = Handedness.Any;
        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, handedness, out MixedRealityPose indexPose))
        {
            indexLoc = indexPose.Position;
        }

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, handedness, out MixedRealityPose thumbPose))
        {
            thumbLoc = thumbPose.Position;
        }
        if (lastBall != null)
        {
            if (Vector3.Distance(indexLoc, lastBall.transform.position) > 0.06f)
            {
                enable_trigger = true;
            }
            else
            {
                enable_trigger = false;
            }
        }
        
     }

     #endregion

     #region Experiment Flow & Control

     /// <summary>
     /// 重置并开始新一轮的实验。
     /// </summary>
     public void ResetExp()
     {
         EnsureErrorListsInitialized();
         ID = ClampID(ID);
         currentRoundID = ID;
         UpdateExperimentMode();
         currentCaseNumber = expRound * 15 + 1;
         ConfigureRoundSettings();

         ClearAllGameObjects();

         coordinates.Clear();
         expStarted = false;
         startTime = Time.time;

         coordinates = PoissonDiscSampling.GeneratePoints(minD, 0, new Vector2(1, 1), 30); // 生成随机点
         CheckBallFor16();
         GetStart();

         // ----- [修改] 在生成小球前确定雷球位置 -----
         if (enableMineBall) {
             DetermineMineBallIndices();
         }

         tatgetPos.Clear();
         largeCirclePoint = transform.position;

         SpawnOriBalls(CircleRadius, NumberOfBalls, transform.position.z);
         if (enabledOb)
         {
             SpawnRandomBallsWithPy();
         }

         expStarted = true;
         expRound++;
         ID = (ID % GetIDCycleMax()) + 1;
     }

     /// <summary>
     /// 当一个目标被成功选择后调用，生成新的目标和障碍物。
     /// </summary>
     public void getNewTarget()
     {
         EnsureErrorListsInitialized();
         usedTime = Time.time - startTime;
         RoundOverEvent.Invoke();

         ClearAllGameObjects();

         if (expTime < 15) // 实验总共16次 (0-15)
         {
             // 更新起始点和终点
             TargetTriggered.Add(TargetEnd);
             TargetStart = TargetEnd;

             if (rotationDirection == 0)
             {
                 start = (start + 7) % 16; // 顺时针
             }
             else
             {
                 start = (start - 7 + 16) % 16; // 逆时针
             }
             TargetEnd = start;
             
             // 重新生成所有球体
             tatgetPos.Clear();
             largeCirclePoint = transform.position;
             SpawnOriBalls(CircleRadius, NumberOfBalls, transform.position.z);

             coordinates.Clear();
             coordinates = PoissonDiscSampling.GeneratePoints(minD, seeds[expTime], new Vector2(1, 1), 30); // 生成随机点
             if (enabledOb)
             {
                 SpawnRandomBallsWithPy();
             }
             
             expTime++;
             currentCaseNumber = (expRound - 1) * 15 + expTime;
         }
         else // 完成一整轮 (15次)
         {
             ExpEndEvent.Invoke();
             expTime = 0;
             Got.SetActive(true);
             GoNext.SetActive(true);
         }

         // 重置单次记录
         startTime = Time.time;
         errorTimes = 0;
         errorInTimes = 0;
         intargetTimes = 0;
         errorX.Clear();
         errorY.Clear();
         errorZ.Clear();
         errorObstacleX.Clear();
         errorObstacleY.Clear();
         errorObstacleZ.Clear();
         triggerCondition = ' ';

         if (expTime == 1)
         {
             ExpStartEvent.Invoke();
         }
     }

     #endregion

     #region Ball Spawning

     // ----- [新增] 决定雷球位置的方法 -----
     /// <summary>
     /// 随机决定5个非目标球为雷球。
     /// </summary>
     private void DetermineMineBallIndices()
     {
         mineIndices.Clear();
         var candidateIndices = new List<int>();

         // 目标球 (TargetEnd) 不会是雷球，将其余15个球的索引加入候选列表
         for (int i = 0; i < NumberOfBalls; i++)
         {
             if (i != TargetEnd)
             {
                 candidateIndices.Add(i);
             }
         }

         // Fisher-Yates shuffle 算法，随机打乱候选索引列表
         for (int i = 0; i < candidateIndices.Count - 1; i++)
         {
             Random.InitState((int)Time.time);
             int randomIndex = Random.Range(i, candidateIndices.Count);
             (candidateIndices[i], candidateIndices[randomIndex]) = (candidateIndices[randomIndex], candidateIndices[i]);
         }

         // 从打乱后的列表中选取前5个作为雷球，确保每次都有5个
         for (int i = 0; i < 5; i++)
         {
             if (i < candidateIndices.Count)
             {
                 mineIndices.Add(candidateIndices[i]);
             }
         }
     }


     /// <summary>
     /// 生成外圈的16个固定位置的球。
     /// </summary>
     void SpawnOriBalls(float Radius, int BallCount, float CenterDepthCoordinate)
     {
         ParentObject = gameObject;
         Vector3 BallScale = new Vector3(BallSize, BallSize, BallSize);

         for (int i = 0; i < BallCount; i++)
         {
             // 计算每个球在圆周上的位置
             float angle = i * (360f / BallCount);
             float x = transform.position.x + Radius * Mathf.Cos(angle * Mathf.Deg2Rad);
             float y = transform.position.y + Radius * Mathf.Sin(angle * Mathf.Deg2Rad);
             Vector3 position = new Vector3(x, y, CenterDepthCoordinate);
             tatgetPos.Add(position);

             GameObject newBall;
             bool isTarget = (expStarted && TargetEnd == i) || (!expStarted && i == initialBall);
             bool isStartPoint = (expStarted && i == TargetStart);
             bool isNextNextTarget = ((rotationDirection == 0 && (i == (TargetEnd + 7) % 16)) || (rotationDirection == 1 && (i == (TargetEnd - 7 + 16) % 16)));
             
             if (isTarget)
             {
                 // 生成目标球 (红色)
                 newBall = Instantiate(TargetPrefab, position, Quaternion.identity);
                 newBall.GetComponent<S2AccMode>().ballIndex = i;
                 newBall.GetComponent<MeshRenderer>().material = redBall;
                 // ----- [修改] 检查是否为雷球并设置状态 -----
                 if (mineIndices.Contains(i))
                 {
                     // 尝试获取S2AccMode组件。请确保您的目标球和障碍球预制件上都有这个组件。
                     S2AccMode s2AccMode = newBall.GetComponent<S2AccMode>();
                     if (s2AccMode != null)
                     {
                         s2AccMode.isMineBall = true;
                     }
                     else
                     {
                         // 如果组件不存在，在控制台输出警告信息
                         print($"球 {i} 被指定为雷球, 但其预制件上缺少 S2AccMode 组件。");
                     }
                 }
             }
             else
             {
                 // 生成普通/障碍球 (默认材质)
                 newBall = Instantiate(ObstaclePrefab, position, Quaternion.identity);
                 newBall.GetComponent<S2Obstacles>().ballIndex = i;
                 if (isNextNextTarget)
                 {
                     nextNextBall = newBall;
                 }

                 // 实验开始前，禁用非目标球的交互脚本
                //  if (!expStarted)
                //  {
                //      newBall.GetComponent<AcceStudy3>().enabled = false;
                //      newBall.GetComponent<S2Obstacles>().enabled = false;
                //      newBall.GetComponent<S2Player>().enabled = false;
                //  }
             }
             
             newBall.transform.localScale = BallScale;
             newBall.transform.parent = ParentObject.transform;
             allBalls.Add(newBall);
             TargetObjects.Add(newBall);

             // 如果是起始球，则标记为已触发状态并变为绿色
             if (isStartPoint)
             {
               if (lastBall != null)
               {
                   lastBall.GetComponent<S2Obstacles>().triggered = false;
                   if (!disableNonTargetBallTriggers)
                   {
                       lastBall.GetComponent<AcceStudy3>().enabled = true;
                       lastBall.GetComponent<S2Player>().enabled = true;
                       lastBall.GetComponent<ReactTap>().enabled = true;
                       lastBall.GetComponent<AcceStudy3_2>().enabled = true;
                       lastBall.GetComponent<Pinch>().enabled = true;
                   }
                }
                
                 newBall.GetComponent<S2Obstacles>().triggered = true;
                 newBall.GetComponent<AcceStudy3>().enabled = false;
                 newBall.GetComponent<S2Player>().enabled = false;
                 newBall.GetComponent<ReactTap>().enabled = false;
                 newBall.GetComponent<AcceStudy3_2>().enabled = false;
                 newBall.GetComponent<Pinch>().enabled = false;
                 if (mineIndices.Contains(i))
                 {
                     newBall.GetComponent<MeshRenderer>().material = yellowBall;
                 }
                 else
                 {
                     newBall.GetComponent<MeshRenderer>().material = greenBall;
                 }
                 triggered = true;
                 lastBall = newBall;
             }
             else if (!isTarget)
             {
                 ApplyNonTargetBallTriggerInspector(newBall);
             }
         }
     }

     /// <summary>
     /// 根据Python脚本返回的坐标生成内部的随机障碍球。
     /// </summary>
     void SpawnRandomBallsWithPy()
     {
         if (ObstaclePrefab == null)
         {
             UnityEngine.Debug.LogWarning($"{name}: ObstaclePrefab is not assigned; random obstacle spawning skipped.");
             return;
         }

         InitializeObjectPool();

         Vector3 BallScale = new Vector3(BallSize, BallSize, BallSize);
         float lenth = 1F;

         // 计算生成区域的左下角基准点
         Vector3 temp = transform.position;
         Vector3 inLeftDown = new Vector3(temp.x, temp.y, temp.z);
         inLeftDown.x -= lenth / 2;
         inLeftDown.y -= lenth / 2;
         inLeftDown.z -= lenth / 2 * 0.3F;

         for (int i = 0; i < coordinates.Count; i += 1)
         {
             // Z轴上随机分布，大部分在前，少数在后
             float randomZ;
             int[] nums = { 0, 0, 0, 0, 0, 0, 0, 1, 1, 1 }; // 70%概率在前，30%在后
             int randomIndex = Random.Range(0, nums.Length);
             if (nums[randomIndex] == 0)
             {
                 randomZ = Random.Range(0, 0.15F); // 前半部分
             }
             else
             {
                 randomZ = Random.Range(0.15F, 0.3F); // 后半部分
             }

             Vector3 position = new Vector3(inLeftDown.x + coordinates[i].x, inLeftDown.y + coordinates[i].y, inLeftDown.z + randomZ);

             // 检查是否与外圈的目标球碰撞，如果是则跳过
             if (collisionWithTarget(TargetObjects, position, BallSize))
             {
                 continue;
             }
             
             // ----- [修改] 使用对象池生成障碍球 -----
             GameObject obstacle;
            if (obstaclePool.Count > 0)
            {
                // 从池中获取对象
                obstacle = obstaclePool.Dequeue();
                obstacle.transform.position = position;
                obstacle.transform.rotation = Quaternion.identity;
                obstacle.transform.SetParent(ParentObject.transform);
                obstacle.SetActive(true);
                SetNonTargetBallTriggerComponentsEnabled(obstacle, true);
                PrepareNonTargetObstacleForMode(obstacle);
             }
            else
            {
                // 如果池已空，则实例化新对象
                obstacle = Instantiate(ObstaclePrefab, position, Quaternion.identity);
                obstacle.transform.SetParent(ParentObject.transform);
            }

             obstacle.transform.localScale = BallScale;
             allBalls.Add(obstacle);
             obstacleObjects.Add(obstacle);
             ApplyNonTargetBallTriggerInspector(obstacle);

             // 实验未开始时，禁用所有随机障碍球的脚本
            //  if (!expStarted)
            //  {
            //      obstacle.GetComponent<AcceStudy3>().enabled = false;
            //      obstacle.GetComponent<S2Obstacles>().enabled = false;
            //      obstacle.GetComponent<S2Player>().enabled = false;
            //  }
         }
     }

     #endregion

     #region Helper & Utility Methods

     private int GetIDCycleMax()
     {
         return idLogicMode == IDLogicMode.New ? 3 : 9;
     }

     private int ClampID(int id)
     {
         return Mathf.Clamp(id, 1, GetIDCycleMax());
     }

     private int GetLegacyIDForSettings(int id)
     {
         int clampedID = ClampID(id);
         if (idLogicMode == IDLogicMode.Old)
         {
             return clampedID;
         }

         switch (clampedID)
         {
             case 1:
                 return 1;
             case 2:
                 return 5;
             case 3:
                 return 9;
             default:
                 return 1;
         }
     }

     /// <summary>
     /// 根据当前实验轮次(expRound)配置相应的参数。
     /// </summary>
     private void ConfigureRoundSettings()
     {
         int idIndex = 9 - GetLegacyIDForSettings(currentRoundID);
         switch (idIndex)
         {
             case 0:
                 seeds = new List<int> { 0, 4, 6, 8, 9, 20, 30, 32, 34, 4, 0, 42, 43, 45, 46, 50 };
                 minD = 0.07f;
                 BallSize = 0.03F;
                 CircleRadius = 0.35F;
                 TargetPrefab.GetComponent<AcceStudy3>().triggerDis = 0.015f;
                 ObstaclePrefab.GetComponent<AcceStudy3>().triggerDis = 0.015f;
                 ObNum = 11;
                 break;
             case 1:
                 seeds = new List<int> { 1, 3, 5, 6, 7, 9, 11, 12, 15, 18, 24, 25, 28, 29, 33 };
                 minD = 0.075f;
                 BallSize = 0.03F;
                 CircleRadius = 0.30F;
                 ObNum = 5;
                 TargetPrefab.GetComponent<AcceStudy3>().triggerDis = 0.015f;
                 ObstaclePrefab.GetComponent<AcceStudy3>().triggerDis = 0.015f;
                 break;
             case 2:
                 seeds = new List<int> { 1, 5, 11, 13, 14, 18, 21, 26, 28, 31, 32, 33, 36, 38, 40 };
                 minD = 0.08f;
                 BallSize = 0.03F;
                 CircleRadius = 0.25F;
                 TargetPrefab.GetComponent<AcceStudy3>().triggerDis = 0.015f;
                 ObstaclePrefab.GetComponent<AcceStudy3>().triggerDis = 0.015f;
                 ObNum = 3;
                 break;
             case 3:
                 seeds = new List<int> { 0, 4, 6, 8, 9, 20, 30, 32, 34, 4, 0, 42, 43, 45, 46, 50 };
                 minD = 0.085f;
                 BallSize = 0.06F;
                 CircleRadius = 0.35F;
                 TargetPrefab.GetComponent<AcceStudy3>().triggerDis = 0.03f;
                 ObstaclePrefab.GetComponent<AcceStudy3>().triggerDis = 0.03f;
                 ObNum = 11;
                 break;
             case 4:
                 seeds = new List<int> { 1, 3, 5, 6, 7, 9, 11, 12, 15, 18, 24, 25, 28, 29, 33 };
                 minD = 0.09f;
                 BallSize = 0.06F;
                 CircleRadius = 0.30F;
                 ObNum = 5;
                 TargetPrefab.GetComponent<AcceStudy3>().triggerDis = 0.03f;
                 ObstaclePrefab.GetComponent<AcceStudy3>().triggerDis = 0.03f;
                 break;
             case 5:
                 seeds = new List<int> { 1, 5, 11, 13, 14, 18, 21, 26, 28, 31, 32, 33, 36, 38, 40 };
                 minD = 0.095f;
                 BallSize = 0.06F;
                 CircleRadius = 0.25F;
                 TargetPrefab.GetComponent<AcceStudy3>().triggerDis = 0.03f;
                 ObstaclePrefab.GetComponent<AcceStudy3>().triggerDis = 0.03f;
                 ObNum = 3;
                 break;
             case 6:
                 seeds = new List<int> { 0, 4, 6, 8, 9, 20, 30, 32, 34, 4, 0, 42, 43, 45, 46, 50 };
                 minD = 0.10f;
                 BallSize = 0.09F;
                 CircleRadius = 0.35F;
                 TargetPrefab.GetComponent<AcceStudy3>().triggerDis = 0.045f;
                 ObstaclePrefab.GetComponent<AcceStudy3>().triggerDis = 0.045f;
                 ObNum = 11;
                 break;
             case 7:
                 seeds = new List<int> { 1, 3, 5, 6, 7, 9, 11, 12, 15, 18, 24, 25, 28, 29, 33 };
                 minD = 0.105f;
                 BallSize = 0.09F;
                 CircleRadius = 0.30F;
                 ObNum = 5;
                 TargetPrefab.GetComponent<AcceStudy3>().triggerDis = 0.045f;
                 ObstaclePrefab.GetComponent<AcceStudy3>().triggerDis = 0.045f;
                 break;
             case 8:
                 seeds = new List<int> { 1, 5, 11, 13, 14, 18, 21, 26, 28, 31, 32, 33, 36, 38, 40 };
                 minD = 0.11f;
                 BallSize = 0.09F;
                 CircleRadius = 0.25F;
                 TargetPrefab.GetComponent<AcceStudy3>().triggerDis = 0.045f;
                 ObstaclePrefab.GetComponent<AcceStudy3>().triggerDis = 0.045f;
                 ObNum = 3;
                 break;
             case 9:
                 seeds = new List<int> { 1, 5, 11, 13, 14, 18, 21, 26, 28, 31, 32, 33, 36, 38, 40 };
                 minD = 0.11f;
                 BallSize = 0.09F;
                 CircleRadius = 0.25F;
                 TargetPrefab.GetComponent<AcceStudy3>().triggerDis = 0.045f;
                 ObstaclePrefab.GetComponent<AcceStudy3>().triggerDis = 0.045f;
                 ObNum = 3;
                 break;
         }
     }

     /// <summary>
     /// 根据实验进度更新交互模式和UI提示文本。
     /// </summary>
     private void UpdateExperimentMode()
     {
         if (expRound >= 9)
         {
             expRound = 0; // 重置轮次计数器
         }

         if (expRound == 8)
         {
             switch (modeSelected)
             {
                 case Mode.Tap:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"Dwell\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.dwell:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"Magic Tap\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.magicTap:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"React Tap\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.reactRelease:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"React Release\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.pinch:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"React Tap\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.pinchReleaseCancel:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"Pinch Release Cancel\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.pinchDwell:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"Pinch-Dwell\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.pinchDirection:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"Pinch-Direction\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.direction010:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"010-direction\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.direction010Test:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"010-direction-test\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.press:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"Press\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.dwellRelease:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"Dwell-Release\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.dwellDirection:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"Dwell-Direction\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.dwellWithdraw:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"Dwell Withdraw\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.pinchReleaseTrigger:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"Pinch-Release-Trigger\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.speedRelease:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"Speed-Release\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.speedDirection:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"Speed-Direction\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.pinchThumbSwipe:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"Pinch-Thumb-Swipe\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.pinchWristTilt:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"Pinch-Wrist-Tilt\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.wristTilt010:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"010-Wrist-Tilt\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.dwellWristTilt:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"Dwell-Wrist-Tilt\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.speed:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"Speed\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.speedDwell:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"Speed-Dwell\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.speedWristTilt:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"Speed-Wrist-Tilt\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.contact010:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"010-Contact\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
                 case Mode.swing010:
                     Got.GetComponentInChildren<TMP_Text>().text = "In this task, selection technique is \"010-Swing\".\r\nPlease complete the task quickly and accurately.\r\n";
                     break;
             }
         }
     }
     
     /// <summary>
     /// 禁用并清空所有小球相关的列表。
     /// </summary>
     private void OnValidate()
     {
         ID = ClampID(ID);
         if (!Application.isPlaying)
         {
             currentRoundID = ID;
         }
         else
         {
             ApplyNonTargetBallTriggerSettingToExistingBalls();
         }
     }

     public void RegisterObstacleErrorTrigger(int obstacleIndex)
     {
         EnsureErrorListsInitialized();
         errorTimes++;
         errorTrigered = true;
         area_type = 5;
         index = obstacleIndex;

         Vector3 obstaclePosition = Vector3.zero;
         if (obstacleIndex >= 0 && obstacleIndex < obstacleObjects.Count && obstacleObjects[obstacleIndex] != null)
         {
             obstaclePosition = obstacleObjects[obstacleIndex].transform.position;
         }

         errorX.Add(indexLoc.x);
         errorY.Add(indexLoc.y);
         errorZ.Add(indexLoc.z);
         errorObstacleX.Add(obstaclePosition.x);
         errorObstacleY.Add(obstaclePosition.y);
         errorObstacleZ.Add(obstaclePosition.z);

         pendingErrorTriggerSnapshots.Enqueue(new ErrorTriggerSnapshot
         {
             indexLoc = indexLoc,
             thumbLoc = thumbLoc,
             areaType = 5,
             index = obstacleIndex,
             obstaclePosition = obstaclePosition
         });
     }

     public bool TryGetPendingErrorTriggerSnapshot(out Vector3 snapshotIndexLoc, out Vector3 snapshotThumbLoc, out int snapshotAreaType, out int snapshotIndex, out Vector3 snapshotObstaclePosition)
     {
         if (pendingErrorTriggerSnapshots.Count > 0)
         {
             ErrorTriggerSnapshot snapshot = pendingErrorTriggerSnapshots.Peek();
             snapshotIndexLoc = snapshot.indexLoc;
             snapshotThumbLoc = snapshot.thumbLoc;
             snapshotAreaType = snapshot.areaType;
             snapshotIndex = snapshot.index;
             snapshotObstaclePosition = snapshot.obstaclePosition;
             return true;
         }

         snapshotIndexLoc = Vector3.zero;
         snapshotThumbLoc = Vector3.zero;
         snapshotAreaType = area_type;
         snapshotIndex = index;
         snapshotObstaclePosition = Vector3.zero;
         return false;
     }

     public void ConsumePendingErrorTriggerSnapshot()
     {
         if (pendingErrorTriggerSnapshots.Count > 0)
         {
             pendingErrorTriggerSnapshots.Dequeue();
         }

         errorTrigered = pendingErrorTriggerSnapshots.Count > 0;
     }

     private void EnsureErrorListsInitialized()
     {
         if (errorX == null) errorX = new List<float>();
         if (errorY == null) errorY = new List<float>();
         if (errorZ == null) errorZ = new List<float>();
         if (errorObstacleX == null) errorObstacleX = new List<float>();
         if (errorObstacleY == null) errorObstacleY = new List<float>();
         if (errorObstacleZ == null) errorObstacleZ = new List<float>();
     }

     private void ApplyNonTargetBallTriggerInspector(GameObject ball)
     {
         if (!disableNonTargetBallTriggers || ball == null)
         {
             return;
         }

         S2Obstacles obstacles = ball.GetComponent<S2Obstacles>();
         if (obstacles != null)
         {
             obstacles.ClearMode();
             obstacles.triggered = true;
         }

         SetNonTargetBallTriggerComponentsEnabled(ball, false);
     }

     private void SetNonTargetBallTriggerComponentsEnabled(GameObject ball, bool enabled)
     {
         if (ball == null)
         {
             return;
         }

         AcceStudy3 acce = ball.GetComponent<AcceStudy3>();
         if (acce != null) acce.enabled = enabled;

         AcceStudy3_2 acce2 = ball.GetComponent<AcceStudy3_2>();
         if (acce2 != null) acce2.enabled = enabled;

         ReactTap reactTap = ball.GetComponent<ReactTap>();
         if (reactTap != null) reactTap.enabled = enabled;

         Pinch pinch = ball.GetComponent<Pinch>();
         if (pinch != null) pinch.enabled = enabled;

         S2Obstacles obstacles = ball.GetComponent<S2Obstacles>();
         if (obstacles != null) obstacles.enabled = enabled;
     }

     private void PrepareNonTargetObstacleForMode(GameObject obstacle)
     {
         S2Obstacles obstacleMode = obstacle.GetComponent<S2Obstacles>();
         if (obstacleMode == null)
         {
             return;
         }

         obstacleMode.triggered = false;
         obstacleMode.inBall = false;
         obstacleMode.inBall2 = false;
         obstacleMode.handInBall = false;
         obstacleMode.handInBall2 = false;
         obstacleMode.canceled = false;
         obstacleMode.useIndex = false;
         obstacleMode.ClearMode();
         obstacleMode.SetMode((int)modeSelected);
     }

     private void ApplyNonTargetBallTriggerSettingToExistingBalls()
     {
         foreach (GameObject ball in allBalls)
         {
             if (ball == null || ball.GetComponent<S2AccMode>() != null)
             {
                 continue;
             }

             if (disableNonTargetBallTriggers)
             {
                 ApplyNonTargetBallTriggerInspector(ball);
                 continue;
             }

             if (ball == lastBall)
             {
                 SetNonTargetBallTriggerComponentsEnabled(ball, false);
                 continue;
             }

             SetNonTargetBallTriggerComponentsEnabled(ball, true);
             PrepareNonTargetObstacleForMode(ball);
         }
     }

     private void ClearAllGameObjects()
     {
         // ----- [修改] 将障碍物返回对象池，并销毁目标球 -----

         // 1. 将使用中的障碍物对象返回池中
         foreach (GameObject obs in obstacleObjects)
         {
             if (obs == null)
             {
                 continue;
             }

             if (poolParent != null)
             {
                 obs.transform.SetParent(poolParent.transform);
                 obs.SetActive(false);
                 obstaclePool.Enqueue(obs);
             }
             else
             {
                 Destroy(obs);
             }
         }
         
         // 2. 销毁非池化的目标球对象
         foreach (GameObject target in TargetObjects)
         {
             Destroy(target);
         }
         
         // 3. 清空所有列表
         TargetObjects.Clear();
         obstacleObjects.Clear();
         allBalls.Clear();
     }


     /// <summary>
     /// 随机化并设置初始的起始球和目标球。
     /// </summary>
     void GetStart()
     {
         TargetStart = start;

         if (rotationDirection == 0)
         {
             start = (start + 7) % 16; // 顺时针跳7个位置
         }
         else
         {
             start = (start - 7 + 16) % 16; // 逆时针跳7个位置
         }

         TargetEnd = start;
         initialBall = TargetEnd;
     }

     /// <summary>
     /// 检查一个点是否在一个扇形区域内。
     /// </summary>
     bool inSector(Vector3 startBall, Vector3 endBall, Vector3 toTest)
     {
         float distance = Vector3.Distance(startBall, endBall);

         if (distance < Vector3.Distance(startBall, toTest))
         {
             return false;
         }
         if (Vector3.Angle(endBall - startBall, toTest - startBall) > 10)
         {
             return false;
         }
         return true;
     }

     /// <summary>
     /// 检查一个新位置是否与现有目标列表中的任何球发生碰撞（2D平面）。
     /// </summary>
     bool collisionWithTarget(List<GameObject> TargetList, Vector3 toTest, float radius)
     {
         foreach (var target in TargetList)
         {
             Vector3 temp1 = target.transform.position;
             temp1.z = 0; // 忽略Z轴进行2D距离比较
             Vector3 temp2 = toTest;
             temp2.z = 0;
             if (Vector3.Distance(temp1, temp2) < radius)
             {
                 return true;
             }
         }
         return false;
     }

     /// <summary>
     /// 检查并计算在16次连续选择中，有多少障碍物会落入选择路径的扇形区域。
     /// </summary>
     public void CheckBallFor16()
     {
         int tempStart = initialBall;
         for (int j = 0; j < 15; j++)
         {
             inBound = 0;
             int tempEnd;
             if (rotationDirection == 0)
             {
                 tempEnd = (tempStart + 7) % 16;
             }
             else
             {
                 tempEnd = (tempStart - 7 + 16) % 16;
             }

             // 计算起始球和结束球的3D位置
             float angle_start = tempStart * (360f / 16);
             float x_start = transform.position.x + CircleRadius * Mathf.Cos(angle_start * Mathf.Deg2Rad);
             float y_start = transform.position.y + CircleRadius * Mathf.Sin(angle_start * Mathf.Deg2Rad);
             Vector3 startPosition = new Vector3(x_start, y_start, transform.position.z);

             float angle_end = tempEnd * (360f / 16);
             float x_end = transform.position.x + CircleRadius * Mathf.Cos(angle_end * Mathf.Deg2Rad);
             float y_end = transform.position.y + CircleRadius * Mathf.Sin(angle_end * Mathf.Deg2Rad);
             Vector3 endPosition = new Vector3(x_end, y_end, transform.position.z);

             // 计算障碍物生成区域的基准点
             float lenth = 1F;
             Vector3 temp = GameObject.Find("ObstacletDesignPrototypeB4CM").transform.position;
             Vector3 inLeftDown = new Vector3(temp.x, temp.y, temp.z);
             inLeftDown.x -= lenth / 2;
             inLeftDown.y -= lenth / 2;

             // 遍历所有由python生成的坐标，检查是否在扇区内
             for (int i = 0; i < coordinates.Count; i += 1)
             {
                 Vector3 position = new Vector3(inLeftDown.x + coordinates[i].x, inLeftDown.y + coordinates[i].y, inLeftDown.z);
                 if (inSector(startPosition, endPosition, position))
                 {
                     inBound++;
                 }
             }
             tempStart = tempEnd;
         }
     }

     /// <summary>
     /// 播放音效。
     /// </summary>
     public void PlaySound2()
     {
         if (sourceRight != null)
         {
             sourceRight.Play();
         }
     }

     public void PlaySoundWrong()
     {
         if (sourceWrong != null)
         {
             sourceWrong.Play();
         }
     }

     #endregion
 }
 
  /// <summary>
 /// 泊松盘采样算法的静态实现类
 /// </summary>
 public static class PoissonDiscSampling
 {
     /// <summary>
     /// 使用 Bridson 算法生成泊松盘采样点。
     /// </summary>
     /// <param name="minRadius">任意两点间的最小距离 (r)。</param>
     /// <param name="sampleRegionSize">采样的区域大小 (宽, 高)。</param>
     /// <param name="numSamplesBeforeRejection">在拒绝一个活跃点前，围绕它尝试生成新候选点的次数 (k)。默认值为 30。</param>
     /// <returns>一个包含所有生成点的 Vector2 列表。</returns>
     public static List<Vector2> GeneratePoints(float minRadius,int seed, Vector2 sampleRegionSize, int numSamplesBeforeRejection = 30)
     {
         Random.InitState(seed); // 初始化随机数种子
         // 步骤 0: 计算网格参数
         // 为了保证每个单元格最多只包含一个采样点，单元格的边长需小于等于 r/√2
         float cellSize = minRadius / Mathf.Sqrt(2);
         // 创建一个二维网格来存储点在列表中的索引，用于加速邻居查找
         int[,] grid = new int[Mathf.CeilToInt(sampleRegionSize.x / cellSize), Mathf.CeilToInt(sampleRegionSize.y / cellSize)];
         
         // 步骤 1: 初始化列表
         var points = new List<Vector2>();       // 存储最终所有采样点的列表
         var spawnPoints = new List<Vector2>();  // 存储“活跃”点的列表，新点将围绕这些点生成

         // 步骤 2: 生成第一个点
         // 随机选择一个初始点，并将其添加到活跃列表和最终点列表中
         Vector2 initialPoint = new Vector2(Random.Range(0, sampleRegionSize.x), Random.Range(0, sampleRegionSize.y));
         spawnPoints.Add(initialPoint);
         points.Add(initialPoint);
         
         // 在网格中记录第一个点的位置（存储索引+1，这样0就表示单元格为空）
         grid[(int)(initialPoint.x / cellSize), (int)(initialPoint.y / cellSize)] = points.Count;

         // 步骤 3: 主循环
         while (spawnPoints.Count > 0)
         {
             // 随机从活跃列表中选择一个点作为生成新点的中心
             int spawnIndex = Random.Range(0, spawnPoints.Count);
             Vector2 spawnCentre = spawnPoints[spawnIndex];
             bool candidateAccepted = false;

             // 围绕该中心点生成 k 个候选点
             for (int i = 0; i < numSamplesBeforeRejection; i++)
             {
                 // 在半径为 [r, 2r] 的圆环内随机生成一个候选点
                 float angle = Random.value * Mathf.PI * 2;
                 float radius = Random.Range(minRadius, 2 * minRadius);
                 Vector2 direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                 Vector2 candidate = spawnCentre + direction * radius;

                 // 验证该候选点是否有效
                 if (IsValid(candidate, sampleRegionSize, cellSize, minRadius, points, grid))
                 {
                     points.Add(candidate);
                     spawnPoints.Add(candidate);
                     grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = points.Count;
                     candidateAccepted = true;
                     break; // 找到有效点，跳出内层循环
                 }
             }

             // 如果 k 次尝试后仍未找到有效的候选点，则将该中心点从活跃列表中移除
             if (!candidateAccepted)
             {
                 spawnPoints.RemoveAt(spawnIndex);
             }
         }

         return points;
     }

     /// <summary>
     /// 检查候选点是否有效。
     /// </summary>
     private static bool IsValid(Vector2 candidate, Vector2 sampleRegionSize, float cellSize, float minRadius, List<Vector2> points, int[,] grid)
     {
         // 1. 检查候选点是否在采样区域内
         if (candidate.x < 0 || candidate.x >= sampleRegionSize.x || candidate.y < 0 || candidate.y >= sampleRegionSize.y)
         {
             return false;
         }

         // 2. 检查候选点与周围邻居点的距离
         int cellX = (int)(candidate.x / cellSize);
         int cellY = (int)(candidate.y / cellSize);
         float sqrMinRadius = minRadius * minRadius;

         // 遍历候选点所在单元格周围 5x5 的邻居单元格
         int searchStartX = Mathf.Max(0, cellX - 2);
         int searchEndX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);
         int searchStartY = Mathf.Max(0, cellY - 2);
         int searchEndY = Mathf.Min(cellY + 2, grid.GetLength(1) - 1);

         for (int x = searchStartX; x <= searchEndX; x++)
         {
             for (int y = searchStartY; y <= searchEndY; y++)
             {
                 int pointIndex = grid[x, y] - 1; // 从网格获取点的索引
                 
                 // 如果邻居单元格内有一个已存在的点
                 if (pointIndex != -1)
                 {
                     // 计算候选点与该点的距离的平方
                     float sqrDst = (candidate - points[pointIndex]).sqrMagnitude;
                     // 如果距离小于最小半径，则该候选点无效
                     if (sqrDst < sqrMinRadius)
                     {
                         return false;
                     }
                 }
             }
         }

         // 如果所有检查都通过，则该候选点有效
         return true;
     }
 }

