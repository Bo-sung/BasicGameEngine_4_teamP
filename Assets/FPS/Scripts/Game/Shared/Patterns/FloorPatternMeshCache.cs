using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 바닥 패턴 메시 캐시
/// 메시를 한번만 생성하고 모든 패턴이 공유
/// </summary>
public static class FloorPatternMeshCache
{
    private static Dictionary<string, Mesh> meshCache = new Dictionary<string, Mesh>();
    private static bool isInitialized = false;

    /// <summary>
    /// 메시 캐시 초기화 (앱 시작 시 한번만 호출)
    /// </summary>
    public static void Initialize()
    {
        if (isInitialized)
            return;

        isInitialized = true;

        // 모든 메시 미리 생성
        meshCache["Circle"] = CreateCircleMesh(1f, 32);
        meshCache["Plane"] = CreatePlaneMesh(1f, 1f, 10, 10);
        meshCache["Triangle"] = CreateTriangleMesh(1f, 1f, 10);
        meshCache["Sector"] = CreateSectorMesh(1f, 120f, 32);

        Debug.Log("[메시 캐시] 메시 캐시 초기화 완료");
    }

    /// <summary>
    /// 캐시된 메시 가져오기
    /// </summary>
    public static Mesh GetCircleMesh()
    {
        Initialize();
        return meshCache["Circle"];
    }

    public static Mesh GetPlaneMesh()
    {
        Initialize();
        return meshCache["Plane"];
    }

    public static Mesh GetTriangleMesh()
    {
        Initialize();
        return meshCache["Triangle"];
    }

    public static Mesh GetSectorMesh()
    {
        Initialize();
        return meshCache["Sector"];
    }

    /// <summary>
    /// 평면 메시 생성 (기본 1x1, 스케일로 조정)
    /// </summary>
    private static Mesh CreatePlaneMesh(float width, float height, int widthSegments = 10, int heightSegments = 10)
    {
        Mesh mesh = new Mesh();
        mesh.name = "PlaneMesh";
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();

        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        // 정점 생성
        for (int y = 0; y <= heightSegments; y++)
        {
            for (int x = 0; x <= widthSegments; x++)
            {
                float posX = (x / (float)widthSegments - 0.5f) * width;
                float posZ = (y / (float)heightSegments - 0.5f) * height;

                vertices.Add(new Vector3(posX, 0, posZ));
                uv.Add(new Vector2(x / (float)widthSegments, y / (float)heightSegments));
            }
        }

        // 삼각형 생성
        for (int y = 0; y < heightSegments; y++)
        {
            for (int x = 0; x < widthSegments; x++)
            {
                int a = y * (widthSegments + 1) + x;
                int b = a + 1;
                int c = a + widthSegments + 1;
                int d = c + 1;

                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);

                triangles.Add(b);
                triangles.Add(c);
                triangles.Add(d);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// 원형 메시 생성 (반지름 1, 스케일로 조정)
    /// </summary>
    private static Mesh CreateCircleMesh(float radius, int segments = 32)
    {
        Mesh mesh = new Mesh();
        mesh.name = "CircleMesh";
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();

        vertices.Add(Vector3.zero);
        uv.Add(new Vector2(0.5f, 0.5f));

        float angleStep = 360f / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            vertices.Add(new Vector3(x, 0, z));
            uv.Add(new Vector2(
                0.5f + (Mathf.Cos(angle) * 0.5f),
                0.5f + (Mathf.Sin(angle) * 0.5f)
            ));
        }

        for (int i = 1; i <= segments; i++)
        {
            triangles.Add(0);
            triangles.Add(i + 1);
            triangles.Add(i);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// 삼각형 메시 생성 (기본 1x1, 스케일로 조정)
    /// </summary>
    private static Mesh CreateTriangleMesh(float width, float height, int segments = 10)
    {
        Mesh mesh = new Mesh();
        mesh.name = "TriangleMesh";
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();

        // 정점 생성 (뒤쪽부터 앞쪽으로)
        for (int i = 0; i <= segments; i++)
        {
            float depthRatio = i / (float)segments;
            float currentZ = depthRatio * height;
            float currentWidth = width * (1f - depthRatio);

            for (int j = 0; j <= segments; j++)
            {
                float widthRatio = j / (float)segments;
                float posX = (widthRatio - 0.5f) * currentWidth;

                vertices.Add(new Vector3(posX, 0, currentZ));
                uv.Add(new Vector2(widthRatio, depthRatio));
            }
        }

        // 삼각형 생성
        for (int i = 0; i < segments; i++)
        {
            for (int j = 0; j < segments; j++)
            {
                int stride = segments + 1;
                int a = i * stride + j;
                int b = a + 1;
                int c = a + stride;
                int d = c + 1;

                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);

                triangles.Add(b);
                triangles.Add(c);
                triangles.Add(d);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// 부채꼴 메시 생성 (반지름 1, 각도 기본값 120도, 스케일로 조정)
    /// </summary>
    private static Mesh CreateSectorMesh(float radius, float spreadAngle, int segments = 32)
    {
        Mesh mesh = new Mesh();
        mesh.name = "SectorMesh";
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();

        vertices.Add(Vector3.zero);
        uv.Add(new Vector2(0.5f, 0.5f));

        float angleStep = spreadAngle / segments;
        float startAngle = -spreadAngle / 2f;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (startAngle + (i * angleStep)) * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * radius;
            float z = Mathf.Cos(angle) * radius;

            vertices.Add(new Vector3(x, 0, z));

            float uvX = 0.5f + (Mathf.Sin(angle) * 0.5f);
            float uvY = 0.5f + (Mathf.Cos(angle) * 0.5f);
            uv.Add(new Vector2(uvX, uvY));
        }

        for (int i = 1; i <= segments; i++)
        {
            triangles.Add(0);
            triangles.Add(i + 1);
            triangles.Add(i);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// 캐시된 메시의 메모리 정보 출력 (디버그용)
    /// </summary>
    public static void DebugPrintCacheInfo()
    {
        Debug.Log("=== 메시 캐시 정보 ===");
        foreach (var kvp in meshCache)
        {
            if (kvp.Value != null)
            {
                int vertexCount = kvp.Value.vertexCount;
                int triangleCount = kvp.Value.triangles.Length / 3;
                Debug.Log($"{kvp.Key}: {vertexCount} 정점, {triangleCount} 삼각형");
            }
        }
    }
}