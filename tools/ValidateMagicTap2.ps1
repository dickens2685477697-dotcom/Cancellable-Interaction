$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$testDirectory = Join-Path $projectRoot 'Temp\MagicTap2Validation'
[void](New-Item -ItemType Directory -Path $testDirectory -Force)
$stubs = @'
using System;
namespace UnityEngine {
    public struct Vector3 {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public static Vector3 zero => new Vector3();
        public static Vector3 right => new Vector3(1,0,0);
        public float magnitude => (float)Math.Sqrt(x*x+y*y+z*z);
        public static Vector3 operator -(Vector3 a,Vector3 b) => new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator /(Vector3 a,float b) => new Vector3(a.x/b,a.y/b,a.z/b);
        public static Vector3 operator *(Vector3 a,float b) => new Vector3(a.x*b,a.y*b,a.z*b);
    }
    public sealed class GameObject { public bool activeInHierarchy=true; }
    public struct Bounds {
        public Vector3 center,extents;
        public bool Contains(Vector3 p) => Math.Abs(p.x-center.x)<=extents.x
            && Math.Abs(p.y-center.y)<=extents.y && Math.Abs(p.z-center.z)<=extents.z;
    }
    public sealed class Collider {
        public bool enabled=true;
        public GameObject gameObject=new GameObject();
        public Bounds bounds;
    }
}
'@
$checks = @'
using System;
using UnityEngine;
public static class MagicTap2Checks {
    private static int checks;
    private static void Require(bool condition,string message) {
        checks++;if(!condition)throw new Exception(message);
    }
    private static void Near(float actual,float expected,string message) {
        Require(Math.Abs(actual-expected)<0.00001f,message+": "+actual+" != "+expected);
    }
    private static bool Sample(MagicTap2MotionTracker m,float x,bool inside=true,
        float speed=.2f,float acceleration=11.661f,float dt=.02f) {
        return m.Sample(Vector3.right*x,inside,dt,speed,acceleration);
    }
    public static int Run() {
        // Two accepted velocity samples are required to calculate acceleration.
        var m=new MagicTap2MotionTracker();
        Require(!Sample(m,0),"Initial pose selected");
        Require(!Sample(m,.002f) && !m.HasAcceleration,"Missing acceleration history selected");
        Require(Sample(m,.004f),"Both default gates should pass at constant 0.1 m/s");
        Near(m.Speed,.1f,"Speed uses fixed delta time");
        Near(m.Acceleration,0,"Constant velocity acceleration");

        // Selection latches for the whole contact, even if the gate fails later.
        Require(!Sample(m,.006f),"Repeated selection in the same contact");
        Require(!Sample(m,.106f),"Fast movement selected twice");
        Require(!Sample(m,.108f),"Slow movement selected twice");
        Require(!Sample(m,.110f,false),"Outside movement selected");
        Require(!m.IsSelected && !m.IsInside,"Exit did not rearm");
        Require(Sample(m,.112f),"Reentry did not rearm");

        // Equality fails at both thresholds (binary-exact values avoid rounding).
        m.Reset();Sample(m,0,dt:1);Sample(m,.125f,dt:1);
        Require(!Sample(m,.25f,speed:.125f,dt:1),"Speed threshold equality passed");
        Require(!m.SpeedConditionMet && m.AccelerationConditionMet,"Speed gate was not independent");
        m.Reset();Sample(m,0,dt:1);Sample(m,.125f,dt:1);
        Require(!Sample(m,.375f,speed:1,acceleration:.125f,dt:1),"Acceleration threshold equality passed");
        Require(m.SpeedConditionMet && !m.AccelerationConditionMet,"Acceleration gate was not independent");

        // Reversing direction at the same speed still has vector acceleration.
        m.Reset();Sample(m,0,dt:1);Sample(m,.125f,dt:1);
        Require(!Sample(m,0,speed:.5f,acceleration:.2f,dt:1),"Turn used scalar speed difference");
        Near(m.Acceleration,.25f,"Turn acceleration magnitude");
        Require(Sample(m,-.125f,speed:.5f,acceleration:.2f,dt:1),"Stable direction should pass");

        // Large deceleration is positive in magnitude and must also be blocked.
        m.Reset();Sample(m,0,dt:1);Sample(m,1,dt:1);
        Require(!Sample(m,1.125f,speed:.5f,acceleration:.5f,dt:1),"Signed deceleration incorrectly passed");
        Near(m.Acceleration,.875f,"Deceleration magnitude");
        Require(Sample(m,1.25f,speed:.5f,acceleration:.5f,dt:1),"Stable slow motion should pass");

        // React Tap's 1 mm filter accumulates displacement against the last accepted pose.
        m.Reset();Sample(m,0);
        Require(!Sample(m,.0004f),"Submillimetre displacement selected");
        Near(m.Speed,0,"Filtered sample advanced velocity");
        Require(!Sample(m,.0012f),"First accepted velocity selected");
        Near(m.Speed,.06f,"Filtered position history advanced");
        Require(Sample(m,.0024f),"Two accumulated displacements should select");

        // Inspector threshold changes affect the next accepted sample.
        m.Reset();Sample(m,0);Sample(m,.002f);
        Require(!Sample(m,.004f,speed:.05f),"Low speed threshold ignored");
        Require(Sample(m,.006f,speed:.2f),"Changed speed threshold ignored");

        // Invalid timing and tracking resets must discard old motion history.
        Require(!Sample(m,.008f,dt:0),"Zero dt selected");
        Require(!m.IsInside && !m.HasAcceleration && !m.IsSelected,"Invalid dt kept state");
        Require(!Sample(m,.01f),"Reset kept position history");
        Require(!Sample(m,.012f),"Reset kept velocity history");
        Require(Sample(m,.014f),"Fresh history did not recover");
        m.Reset();
        Require(!m.HasAcceleration && !m.SpeedConditionMet && !m.AccelerationConditionMet,"Tracking reset kept gate flags");
        Require(!Sample(m,.016f,dt:float.NaN),"NaN dt selected");
        Require(!Sample(m,.016f,dt:float.PositiveInfinity),"Infinite dt selected");

        // Either fingertip or distal joint inside the bounds establishes contact.
        var collider=new Collider {bounds=new Bounds {extents=new Vector3(1,1,1)}};
        Require(MagicTap2MotionTracker.ContainsFinger(collider,Vector3.right*2,Vector3.zero),"Distal contact ignored");
        Require(MagicTap2MotionTracker.ContainsFinger(collider,Vector3.zero,Vector3.right*2),"Tip contact ignored");
        Require(!MagicTap2MotionTracker.ContainsFinger(collider,Vector3.right*2,Vector3.right*3),"Outside joints counted as contact");
        collider.enabled=false;
        Require(!MagicTap2MotionTracker.ContainsFinger(collider,Vector3.zero,Vector3.zero),"Disabled collider accepted contact");
        collider.enabled=true;collider.gameObject.activeInHierarchy=false;
        Require(!MagicTap2MotionTracker.ContainsFinger(collider,Vector3.zero,Vector3.zero),"Inactive collider accepted contact");
        return checks;
    }
}
'@
$stubPath = Join-Path $testDirectory 'Stubs.cs'
$checksPath = Join-Path $testDirectory 'Checks.cs'
$stubs | Set-Content -LiteralPath $stubPath -Encoding utf8
$checks | Set-Content -LiteralPath $checksPath -Encoding utf8
Add-Type -Path @($stubPath, (Join-Path $projectRoot 'Assets\Scripts\Study3\MagicTap2MotionTracker.cs'), $checksPath)
$count = [MagicTap2Checks]::Run()
Write-Output "Magic Tap 2.0 validation passed: $count assertions (isolated motion tests; not a Unity scene run)."
