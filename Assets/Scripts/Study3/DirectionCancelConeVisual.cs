using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class DirectionCancelConeVisual : MonoBehaviour
{
    private const int ConeSegments = 36;
    private const int SideRayCount = 8;

    private GameObject coneRoot;
    private MeshFilter coneMeshFilter;
    private MeshRenderer coneMeshRenderer;
    private LineRenderer rimRenderer;
    private LineRenderer axisRenderer;
    private readonly LineRenderer[] sideRayRenderers = new LineRenderer[SideRayCount];
    private Mesh coneMesh;
    private Material fillMaterial;
    private Material lineMaterial;
    private readonly Vector3[] coneVertices = new Vector3[ConeSegments + 1];
    private readonly int[] coneTriangles = new int[ConeSegments * 3];

    public void Show(Vector3 fingertipPosition, Vector3 axisDirection, float halfAngleDegrees, float length)
    {
        if (axisDirection.sqrMagnitude <= 0.000001f)
        {
            Hide();
            return;
        }

        EnsureVisuals();

        float safeLength = Mathf.Max(0.01f, length);
        float safeHalfAngle = Mathf.Clamp(halfAngleDegrees, 0f, 180f);
        Vector3 axis = axisDirection.normalized;
        coneRoot.transform.SetPositionAndRotation(
            fingertipPosition,
            Quaternion.LookRotation(axis, GetConeUp(axis)));
        coneRoot.transform.localScale = Vector3.one;
        coneRoot.SetActive(true);

        UpdateGeometry(safeHalfAngle, safeLength);
    }

    public void Hide()
    {
        if (coneRoot != null)
        {
            coneRoot.SetActive(false);
        }
    }

    private void EnsureVisuals()
    {
        if (coneRoot != null)
        {
            return;
        }

        coneRoot = new GameObject("DirectionCancelConeAtFingertip");
        coneRoot.transform.SetParent(null, true);

        fillMaterial = CreateMaterial(new Color(1f, 0.08f, 0.03f, 0.12f));
        lineMaterial = CreateMaterial(new Color(1f, 0.18f, 0.05f, 0.95f));

        GameObject surfaceObject = new GameObject("DirectionCancelConeSurface");
        surfaceObject.transform.SetParent(coneRoot.transform, false);
        coneMeshFilter = surfaceObject.AddComponent<MeshFilter>();
        coneMeshRenderer = surfaceObject.AddComponent<MeshRenderer>();
        coneMeshRenderer.sharedMaterial = fillMaterial;
        coneMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        coneMeshRenderer.receiveShadows = false;

        coneMesh = new Mesh
        {
            name = "DirectionCancelConeMesh"
        };
        coneMesh.MarkDynamic();
        coneMeshFilter.sharedMesh = coneMesh;

        rimRenderer = CreateLineRenderer("DirectionCancelConeRim", ConeSegments + 1);
        axisRenderer = CreateLineRenderer("DirectionCancelConeAxis", 2);

        for (int i = 0; i < SideRayCount; i++)
        {
            sideRayRenderers[i] = CreateLineRenderer($"DirectionCancelConeRay{i + 1}", 2);
        }
    }

    private LineRenderer CreateLineRenderer(string objectName, int positionCount)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(coneRoot.transform, false);
        LineRenderer renderer = lineObject.AddComponent<LineRenderer>();
        renderer.useWorldSpace = false;
        renderer.positionCount = positionCount;
        renderer.loop = false;
        renderer.numCapVertices = 4;
        renderer.numCornerVertices = 2;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sharedMaterial = lineMaterial;
        return renderer;
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = color;
        material.renderQueue = (int)RenderQueue.Transparent;
        if (material.HasProperty("_Cull"))
        {
            material.SetInt("_Cull", (int)CullMode.Off);
        }

        return material;
    }

    private void UpdateGeometry(float halfAngleDegrees, float length)
    {
        float halfAngleRadians = halfAngleDegrees * Mathf.Deg2Rad;
        float axialDistance = Mathf.Cos(halfAngleRadians) * length;
        float rimRadius = Mathf.Sin(halfAngleRadians) * length;
        float lineWidth = Mathf.Max(0.0005f, length * 0.012f);

        coneVertices[0] = Vector3.zero;

        for (int i = 0; i < ConeSegments; i++)
        {
            float azimuth = i / (float)ConeSegments * Mathf.PI * 2f;
            Vector3 rimPoint = new Vector3(
                Mathf.Cos(azimuth) * rimRadius,
                Mathf.Sin(azimuth) * rimRadius,
                axialDistance);
            coneVertices[i + 1] = rimPoint;
            rimRenderer.SetPosition(i, rimPoint);

            int triangleIndex = i * 3;
            coneTriangles[triangleIndex] = 0;
            coneTriangles[triangleIndex + 1] = i + 1;
            coneTriangles[triangleIndex + 2] = ((i + 1) % ConeSegments) + 1;
        }

        rimRenderer.SetPosition(ConeSegments, coneVertices[1]);
        rimRenderer.widthMultiplier = lineWidth;

        axisRenderer.SetPosition(0, Vector3.zero);
        axisRenderer.SetPosition(1, Vector3.forward * length);
        axisRenderer.widthMultiplier = lineWidth * 1.35f;

        for (int i = 0; i < SideRayCount; i++)
        {
            int rimIndex = Mathf.RoundToInt(i / (float)SideRayCount * ConeSegments) % ConeSegments;
            sideRayRenderers[i].SetPosition(0, Vector3.zero);
            sideRayRenderers[i].SetPosition(1, coneVertices[rimIndex + 1]);
            sideRayRenderers[i].widthMultiplier = lineWidth;
        }

        coneMesh.Clear();
        coneMesh.vertices = coneVertices;
        coneMesh.triangles = coneTriangles;
        coneMesh.RecalculateNormals();
        coneMesh.RecalculateBounds();
    }

    private static Vector3 GetConeUp(Vector3 axisDirection)
    {
        Vector3 up = Vector3.ProjectOnPlane(Vector3.up, axisDirection);
        if (up.sqrMagnitude <= 0.000001f)
        {
            up = Vector3.ProjectOnPlane(Vector3.right, axisDirection);
        }

        return up.sqrMagnitude > 0.000001f ? up.normalized : Vector3.up;
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        if (coneRoot != null)
        {
            Destroy(coneRoot);
        }

        if (coneMesh != null)
        {
            Destroy(coneMesh);
        }

        if (fillMaterial != null)
        {
            Destroy(fillMaterial);
        }

        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }
    }
}
