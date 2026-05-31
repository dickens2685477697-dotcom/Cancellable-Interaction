using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DrawCircle : MonoBehaviour
{
    public int segments = 50; // 圆环的平滑度，段数越多越圆
    public float radius = 0.6f; // 圆环的半径
    LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = segments + 1;
        lineRenderer.useWorldSpace = false; // 使用本地坐标，这样它会随父对象（球）移动
        CreatePoints();
    }

    void CreatePoints()
    {
        float x;
        float z;
        float angle = 20f;

        for (int i = 0; i < (segments + 1); i++)
        {
            x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

            lineRenderer.SetPosition(i, new Vector3(x, 0, z));

            angle += (360f / segments);
        }
    }
}