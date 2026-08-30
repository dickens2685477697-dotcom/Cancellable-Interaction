using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class DirectionCDArrowVisual : MonoBehaviour
{
    private const int ConeSegments = 18;
    private const int ArrowCount = 1;
    private const float ArrowStartOffsetScale = 1.12f;
    private const float ArrowLengthScale = 0.72f;
    private const float ShaftWidthScale = 0.045f;
    private const float HeadLengthScale = 0.32f;
    private const float HeadRadiusScale = 0.14f;

    private Transform ringCenterTransform;
    private GameObject arrowRoot;
    private readonly LineRenderer[] shaftRenderers = new LineRenderer[ArrowCount];
    private readonly MeshFilter[] headMeshFilters = new MeshFilter[ArrowCount];
    private readonly MeshRenderer[] headRenderers = new MeshRenderer[ArrowCount];
    private Mesh coneMesh;
    private Material arrowMaterial;
    private bool visible;

    public void Show(Transform ringCenter)
    {
        ringCenterTransform = ringCenter;
        visible = ringCenterTransform != null;
        EnsureArrow();
        arrowRoot.SetActive(visible);

        if (visible)
        {
            UpdateArrowTransform();
        }
    }

    public void Hide()
    {
        visible = false;
        if (arrowRoot != null)
        {
            arrowRoot.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (visible)
        {
            UpdateArrowTransform();
        }
    }

    private void EnsureArrow()
    {
        if (arrowRoot != null)
        {
            return;
        }

        arrowRoot = new GameObject("DirectionCDArrow");
        arrowRoot.transform.SetParent(null, true);

        arrowMaterial = CreateArrowMaterial();

        for (int i = 0; i < ArrowCount; i++)
        {
            GameObject shaftObject = new GameObject($"DirectionCDArrowShaft{i + 1}");
            shaftObject.transform.SetParent(arrowRoot.transform, true);
            shaftRenderers[i] = shaftObject.AddComponent<LineRenderer>();
            shaftRenderers[i].useWorldSpace = true;
            shaftRenderers[i].positionCount = 2;
            shaftRenderers[i].numCapVertices = 4;
            shaftRenderers[i].shadowCastingMode = ShadowCastingMode.Off;
            shaftRenderers[i].receiveShadows = false;
            shaftRenderers[i].material = arrowMaterial;

            GameObject headObject = new GameObject($"DirectionCDArrowHead{i + 1}");
            headObject.transform.SetParent(arrowRoot.transform, true);
            headMeshFilters[i] = headObject.AddComponent<MeshFilter>();
            headRenderers[i] = headObject.AddComponent<MeshRenderer>();
            headRenderers[i].material = arrowMaterial;
            headRenderers[i].shadowCastingMode = ShadowCastingMode.Off;
            headRenderers[i].receiveShadows = false;
        }
    }

    private Material CreateArrowMaterial()
    {
        Shader shader = Shader.Find("Unlit/Color");
        Material material = new Material(shader != null ? shader : Shader.Find("Standard"));
        Color gray = new Color(0.55f, 0.55f, 0.55f, 1f);
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", gray);
        }
        else
        {
            material.color = gray;
        }

        return material;
    }

    private void UpdateArrowTransform()
    {
        if (ringCenterTransform == null)
        {
            Hide();
            return;
        }

        Collider collider = GetComponent<Collider>();
        Vector3 ballCenter = collider != null ? collider.bounds.center : transform.position;
        float ballRadius = collider != null
            ? Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.y, collider.bounds.extents.z)
            : Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z) * 0.5f;

        Vector3 userFacingDirection = GetUserFacingDirection(ballCenter);
        if (userFacingDirection.sqrMagnitude <= 0.000001f || ballRadius <= 0f)
        {
            Hide();
            return;
        }

        EnsureArrow();

        Vector3 arrowDirection = userFacingDirection.normalized;
        float headLength = Mathf.Max(0.001f, ballRadius * HeadLengthScale);
        float headRadius = Mathf.Max(0.001f, ballRadius * HeadRadiusScale);

        if (!arrowRoot.activeSelf)
        {
            arrowRoot.SetActive(true);
        }

        EnsureConeMesh(headRadius, headLength);

        Vector3 arrowUp = GetArrowUp(arrowDirection);
        UpdateSingleArrow(0, ballCenter, arrowDirection, arrowUp, ballRadius, headLength);
    }

    private void UpdateSingleArrow(int index, Vector3 ballCenter, Vector3 direction, Vector3 arrowUp, float ballRadius, float headLength)
    {
        float arrowLength = Mathf.Max(0.001f, ballRadius * ArrowLengthScale);
        Vector3 start = ballCenter + direction * (ballRadius * ArrowStartOffsetScale);
        Vector3 end = start + direction * arrowLength;

        shaftRenderers[index].widthMultiplier = Mathf.Max(0.001f, ballRadius * ShaftWidthScale);
        shaftRenderers[index].SetPosition(0, start);
        shaftRenderers[index].SetPosition(1, end - direction * (headLength * 0.7f));

        headMeshFilters[index].sharedMesh = coneMesh;
        headMeshFilters[index].transform.position = end;
        headMeshFilters[index].transform.rotation = Quaternion.LookRotation(direction, arrowUp);
    }

    private void EnsureConeMesh(float radius, float length)
    {
        if (coneMesh == null)
        {
            coneMesh = new Mesh
            {
                name = "DirectionCDArrowHeadMesh"
            };
            coneMesh.MarkDynamic();
        }

        Vector3[] vertices = new Vector3[ConeSegments + 2];
        int[] triangles = new int[ConeSegments * 6];
        vertices[0] = Vector3.zero;
        vertices[1] = new Vector3(0f, 0f, -length);

        for (int i = 0; i < ConeSegments; i++)
        {
            float angle = i / (float)ConeSegments * Mathf.PI * 2f;
            vertices[i + 2] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -length);
        }

        int triangleIndex = 0;
        for (int i = 0; i < ConeSegments; i++)
        {
            int current = i + 2;
            int next = ((i + 1) % ConeSegments) + 2;

            triangles[triangleIndex++] = 0;
            triangles[triangleIndex++] = current;
            triangles[triangleIndex++] = next;

            triangles[triangleIndex++] = 1;
            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = current;
        }

        coneMesh.Clear();
        coneMesh.vertices = vertices;
        coneMesh.triangles = triangles;
        coneMesh.RecalculateNormals();
        coneMesh.RecalculateBounds();
    }

    private static Vector3 GetArrowUp(Vector3 arrowDirection)
    {
        Vector3 up = Vector3.ProjectOnPlane(Vector3.up, arrowDirection);
        if (up.sqrMagnitude < 0.000001f)
        {
            up = Vector3.ProjectOnPlane(Vector3.forward, arrowDirection);
        }

        return up.sqrMagnitude > 0.000001f ? up.normalized : Vector3.forward;
    }

    private static Vector3 GetUserFacingDirection(Vector3 ballCenter)
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

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        if (arrowRoot != null)
        {
            Destroy(arrowRoot);
        }

        if (coneMesh != null)
        {
            Destroy(coneMesh);
        }

        if (arrowMaterial != null)
        {
            Destroy(arrowMaterial);
        }
    }
}
