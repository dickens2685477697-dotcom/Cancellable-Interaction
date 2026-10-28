using System;
using System.Collections.Generic;
namespace UnityEngine {
 public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} }
 public static class Time { public static int frameCount; public static double timeAsDouble,fixedTimeAsDouble,realtimeSinceStartupAsDouble; public static float fixedDeltaTime=.02f,timeScale=1; }
 public static class Application { public static string version="test",buildGUID="test-guid",unityVersion="stub",platform="test"; }
 public static class SystemInfo { public static string deviceModel="test-device",operatingSystem="test-os"; }
 public class TextAsset { public string text; }
 public static class Resources { public static T Load<T>(string name) where T:class {return null;} }
}
public static class Study3AttributionCapture { public static object Vector(UnityEngine.Vector3 v) => new object[]{v.x,v.y,v.z}; }
public static class Study3MotionTracker { public const string TimestampKind = "mrtk_update_received_realtime"; }
public class BallSpawner {
 public enum Mode {pinchDwell=9} public enum TargetSelectionMode {Fitts16Targets=1} public enum IDLogicMode {New=1}
 public enum ExecutionMode {Study3Experiment=0} public enum RainbowArcPlacement {OppositeEntry=0}
 public float BallSize;
 public float CircleRadius;
 public int ID;
 public string LogSessionTimestamp;
 public int NumberOfBalls;
 public int ObNum;
 public bool able_to_process;
 public float direction010CDAngleThreshold;
 public float direction010MinSpeedThreshold;
 public float direction010SmoothingAlpha;
 public float direction010TestCDAngleThreshold;
 public float direction010TestMinSpeedThreshold;
 public float direction010TestSmoothingAlpha;
 public int direction010TestWindowFrameCount;
 public int direction010WindowFrameCount;
 public float directionCancelConeLength;
 public bool disableNonTargetBallTriggers;
 public float dwellWithdrawCancelAngleThreshold;
 public float dynamicStopAccelGain;
 public float dynamicStopAccelMax;
 public float dynamicStopAccelMin;
 public float dynamicStopHistorySeconds;
 public float dynamicStopSampleMs;
 public float dynamicStopSpeedGain;
 public float dynamicStopSpeedMax;
 public float dynamicStopSpeedMin;
 public bool enableMineBall;
 public bool enableMineBallRevealDelay;
 public bool enable_preview;
 public bool enabledOb;
 public float entryDirectionConeHalfAngle;
 public ExecutionMode executionMode;
 public IDLogicMode idLogicMode;
 public float ixdlAccelerationThreshold;
 public float ixdlInputSpeedMax;
 public float ixdlInputSpeedMin;
 public float ixdlSpeedThresholdMax;
 public float ixdlSpeedThresholdMin;
 public float longPressTimeThreshold;
 public float magicTap2AccelerationThreshold;
 public float magicTap2SpeedThreshold;
 public float mineBallRevealDelayTime;
 public Mode modeSelected;
 public float pinchDirectionCDAngleThreshold;
 public float pinchDirectionMinSpeedThreshold;
 public float pinchDirectionSmoothingAlpha;
 public int pinchDirectionWindowFrameCount;
 public float pinchDwellCancelTime;
 public float pinchEndDistanceThreshold;
 public float pinchReleaseOpeningSpeedThreshold;
 public float pinchThumbSwipeContactDistanceThreshold;
 public float pinchThumbSwipeDirectionRatioThreshold;
 public float pinchThumbSwipeDistanceThreshold;
 public float pinchWristTiltCancelAngleThreshold;
 public float pressSpeedThershold;
 public RainbowArcPlacement rainbowArcPlacement;
 public float reactReleaseIndexSpeedThreshold;
 public float releaseDistanceDeltaThreshold;
 public int rotationDirection;
 public float samplingRateHz;
 public int seed;
 public bool showContact010TriggerRegion;
 public bool showDirectionCDArrow;
 public bool showDirectionCancelCone;
 public bool showModeReadyRing;
 public float speedMaxThreshold;
 public float speedMinThreshold;
 public int start;
 public string studyBlockLabel;
 public string studyDeclaredIntent;
 public string studyParticipantId;
 public string studyProtocolVersion;
 public string studyTaskCondition;
 public float swing010CancelAngleThreshold;
 public TargetSelectionMode targetSelectionMode;
 public bool useEntryDirectionAxisForDirectionCancel;
 public int currentCaseNumber;
 public int currentRoundID;
 public int TargetEnd;
 public UnityEngine.Vector3 largeCirclePoint;
 public List<UnityEngine.Vector3> tatgetPos=new List<UnityEngine.Vector3>();
 public List<int> mineIndices=new List<int>(),seeds=new List<int>();
}
