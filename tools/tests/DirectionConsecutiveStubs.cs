using System;
namespace UnityEngine {
    public struct Vector3 {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public static Vector3 zero => new Vector3();
        public static Vector3 operator -(Vector3 a,Vector3 b) => new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static float Angle(Vector3 a,Vector3 b) {
            double dot=(double)a.x*b.x+(double)a.y*b.y+(double)a.z*b.z;
            double ma=Math.Sqrt((double)a.x*a.x+(double)a.y*a.y+(double)a.z*a.z);
            double mb=Math.Sqrt((double)b.x*b.x+(double)b.y*b.y+(double)b.z*b.z);
            return (float)(Math.Acos(Math.Max(-1,Math.Min(1,dot/(ma*mb))))*180/Math.PI);
        }
    }
}
