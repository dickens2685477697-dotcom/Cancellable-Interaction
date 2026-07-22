using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class FacingFeedbackRing : MonoBehaviour
{
    private const int SegmentCount = 96;
    private const float InnerRadiusScale = 1.18f;
    private const float OuterRadiusScale = 1.55f;
    private const float DepthScale = 0.1f;
    private GameObject ringObject;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh ringMesh;
    private Material ringMaterial;
    private bool visible;
    private float lastBallRadius = -1f;

    public void Show()
    {
        visible = true;
        EnsureRing();
        UpdateRingTransform();
        ringObject.SetActive(true);
    }

    public void Hide()
    {
        visible = false;
        if (ringObject != null)
        {
            ringObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (visible)
        {
            UpdateRingTransform();
        }
    }

    private void EnsureRing()
    {
        if (ringObject != null && meshFilter != null && meshRenderer != null)
        {
            return;
        }

        ringObject = new GameObject("ModeReadyFacingRing");
        ringObject.transform.SetParent(null, true);

        meshFilter = ringObject.AddComponent<MeshFilter>();
        meshRenderer = ringObject.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        Shader shader = Shader.Find("Unlit/Color");
        ringMaterial = new Material(shader != null ? shader : Shader.Find("Standard"));
        Color gray = new Color(0.55f, 0.55f, 0.55f, 1f);
        if (ringMaterial.HasProperty("_Color"))
        {
            ringMaterial.SetColor("_Color", gray);
        }
        else
        {
            ringMaterial.color = gray;
        }

        meshRenderer.material = ringMaterial;

        ringMesh = new Mesh
        {
            name = "ModeReadyFacingRingMesh"
        };
        ringMesh.MarkDynamic();
        meshFilter.sharedMesh = ringMesh;
    }

    private void UpdateRingTransform()
    {
        EnsureRing();

        Collider collider = GetComponent<Collider>();
        Vector3 ballCenter = collider != null ? collider.bounds.center : transform.position;
        float ballRadius = collider != null
            ? Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.y, collider.bounds.extents.z)
            : Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z) * 0.5f;

        Vector3 normal = GetUserFacingDirection(ballCenter);
        if (normal.sqrMagnitude < 0.000001f)
        {
            normal = -transform.forward;
        }
        normal.Normalize();

        ringObject.transform.position = ballCenter;
        ringObject.transform.rotation = Quaternion.LookRotation(normal, GetRingUp(normal));
        ringObject.transform.localScale = Vector3.one;

        if (Mathf.Abs(ballRadius - lastBallRadius) > 0.0001f)
        {
            BuildRingMesh(ballRadius);
            lastBallRadius = ballRadius;
        }
    }

    private void BuildRingMesh(float ballRadius)
    {
        float innerRadius = Mathf.Max(0.001f, ballRadius * InnerRadiusScale);
        float outerRadius = Mathf.Max(innerRadius + 0.001f, ballRadius * OuterRadiusScale);
        float halfDepth = Mathf.Max(0.001f, ballRadius * DepthScale * 0.5f);

        Vector3[] vertices = new Vector3[SegmentCount * 4];
        int[] triangles = new int[SegmentCount * 24];

        for (int i = 0; i < SegmentCount; i++)
        {
            float angle = (i / (float)SegmentCount) * Mathf.PI * 2f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            int vertexIndex = i * 4;

            vertices[vertexIndex] = new Vector3(cos * outerRadius, sin * outerRadius, halfDepth);
            vertices[vertexIndex + 1] = new Vector3(cos * innerRadius, sin * innerRadius, halfDepth);
            vertices[vertexIndex + 2] = new Vector3(cos * outerRadius, sin * outerRadius, -halfDepth);
            vertices[vertexIndex + 3] = new Vector3(cos * innerRadius, sin * innerRadius, -halfDepth);
        }

        int triangleIndex = 0;
        for (int i = 0; i < SegmentCount; i++)
        {
            int next = (i + 1) % SegmentCount;
            int outerFront = i * 4;
            int innerFront = outerFront + 1;
            int outerBack = outerFront + 2;
            int innerBack = outerFront + 3;
            int nextOuterFront = next * 4;
            int nextInnerFront = nextOuterFront + 1;
            int nextOuterBack = nextOuterFront + 2;
            int nextInnerBack = nextOuterFront + 3;

            AddTriangle(triangles, ref triangleIndex, outerFront, nextOuterFront, nextInnerFront);
            AddTriangle(triangles, ref triangleIndex, outerFront, nextInnerFront, innerFront);

            AddTriangle(triangles, ref triangleIndex, outerBack, innerBack, nextInnerBack);
            AddTriangle(triangles, ref triangleIndex, outerBack, nextInnerBack, nextOuterBack);

            AddTriangle(triangles, ref triangleIndex, outerFront, outerBack, nextOuterBack);
            AddTriangle(triangles, ref triangleIndex, outerFront, nextOuterBack, nextOuterFront);

            AddTriangle(triangles, ref triangleIndex, innerFront, nextInnerFront, nextInnerBack);
            AddTriangle(triangles, ref triangleIndex, innerFront, nextInnerBack, innerBack);
        }

        ringMesh.Clear();
        ringMesh.vertices = vertices;
        ringMesh.triangles = triangles;
        ringMesh.RecalculateNormals();
        ringMesh.RecalculateBounds();
    }

    private static void AddTriangle(int[] triangles, ref int index, int a, int b, int c)
    {
        triangles[index++] = a;
        triangles[index++] = b;
        triangles[index++] = c;
    }

    private Vector3 GetUserFacingDirection(Vector3 ballCenter)
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

        return viewerTransform != null
            ? (viewerTransform.position - ballCenter).normalized
            : Vector3.zero;
    }

    private static Vector3 GetRingUp(Vector3 normal)
    {
        Vector3 up = Vector3.ProjectOnPlane(Vector3.up, normal);
        if (up.sqrMagnitude < 0.000001f)
        {
            up = Vector3.ProjectOnPlane(Vector3.right, normal);
        }

        return up.normalized;
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        if (ringObject != null)
        {
            Destroy(ringObject);
        }

        if (ringMesh != null)
        {
            Destroy(ringMesh);
        }

        if (ringMaterial != null)
        {
            Destroy(ringMaterial);
        }
    }
}
