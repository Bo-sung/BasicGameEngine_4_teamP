using UnityEngine;

/// <summary>
/// 바닥 경고(테레그래프) 표시 시스템
/// Quad Mesh를 사용하여 공격 전 경고를 표시합니다.
/// </summary>
public class TelegraphIndicator : MonoBehaviour
{
    public enum TelegraphType
    {
        Line,       // 직선 (레이저)
        Fan,        // 부채꼴 (산탄)
        Circle      // 원형 (폭격)
    }

    [Header("테레그래프 설정")]
    [Tooltip("경고 타입")]
    public TelegraphType Type = TelegraphType.Circle;

    [Tooltip("경고 표시 지속 시간")]
    public float Duration = 2f;

    [Tooltip("경고 크기 (Line: 길이, Fan/Circle: 반경)")]
    public float Size = 5f;

    [Tooltip("부채꼴 각도 (Fan 타입만)")]
    [Range(0f, 360f)]
    public float FanAngle = 60f;

    [Tooltip("직선 너비 (Line 타입만)")]
    public float LineWidth = 1f;

    [Header("색상 설정")]
    [Tooltip("경고 색상")]
    public Color WarningColor = new Color(1f, 0f, 0f, 0.5f);

    [Tooltip("공격 직전 색상")]
    public Color PreAttackColor = new Color(1f, 0.5f, 0f, 0.7f);

    [Tooltip("점멸 속도")]
    public float BlinkSpeed = 3f;

    [Header("참조")]
    [Tooltip("메시 렌더러")]
    public MeshRenderer MeshRenderer;

    [Tooltip("메시 필터")]
    public MeshFilter MeshFilter;

    private Material m_Material;
    private float m_ElapsedTime = 0f;
    private bool m_IsActive = false;

    void Awake()
    {
        // 컴포넌트 자동 생성
        if (MeshRenderer == null)
        {
            MeshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        if (MeshFilter == null)
        {
            MeshFilter = gameObject.AddComponent<MeshFilter>();
        }

        // Unlit 머티리얼 생성
        m_Material = new Material(Shader.Find("Unlit/Color"));
        m_Material.color = WarningColor;
        MeshRenderer.material = m_Material;

        // 초기에는 비활성화
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 테레그래프 표시 시작
    /// </summary>
    public void Show(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
        
        m_ElapsedTime = 0f;
        m_IsActive = true;

        // 타입에 맞는 메시 생성
        GenerateMesh();

        gameObject.SetActive(true);
    }

    /// <summary>
    /// 테레그래프 숨기기
    /// </summary>
    public void Hide()
    {
        m_IsActive = false;
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!m_IsActive) return;

        m_ElapsedTime += Time.deltaTime;

        // 진행도 계산 (0 ~ 1)
        float progress = Mathf.Clamp01(m_ElapsedTime / Duration);

        // 색상 보간 및 점멸 효과
        Color currentColor = Color.Lerp(WarningColor, PreAttackColor, progress);
        
        // 점멸 효과
        float blink = Mathf.Sin(m_ElapsedTime * BlinkSpeed * Mathf.PI) * 0.5f + 0.5f;
        currentColor.a *= blink;

        m_Material.color = currentColor;

        // 지속 시간 종료 시 자동 숨김
        if (progress >= 1f)
        {
            Hide();
        }
    }

    /// <summary>
    /// 타입에 맞는 메시 생성
    /// </summary>
    void GenerateMesh()
    {
        Mesh mesh = new Mesh();

        switch (Type)
        {
            case TelegraphType.Line:
                mesh = GenerateLineMesh();
                break;
            case TelegraphType.Fan:
                mesh = GenerateFanMesh();
                break;
            case TelegraphType.Circle:
                mesh = GenerateCircleMesh();
                break;
        }

        MeshFilter.mesh = mesh;
    }

    /// <summary>
    /// 직선 메시 생성
    /// </summary>
    Mesh GenerateLineMesh()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(-LineWidth / 2, 0.01f, 0);
        vertices[1] = new Vector3(LineWidth / 2, 0.01f, 0);
        vertices[2] = new Vector3(-LineWidth / 2, 0.01f, Size);
        vertices[3] = new Vector3(LineWidth / 2, 0.01f, Size);

        int[] triangles = new int[] { 0, 2, 1, 1, 2, 3 };

        Vector2[] uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();

        return mesh;
    }

    /// <summary>
    /// 부채꼴 메시 생성
    /// </summary>
    Mesh GenerateFanMesh()
    {
        Mesh mesh = new Mesh();

        int segments = 20;
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        // 중심점
        vertices[0] = new Vector3(0, 0.01f, 0);

        // 부채꼴 호 생성
        float angleStep = FanAngle / segments;
        float startAngle = -FanAngle / 2;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (startAngle + angleStep * i) * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * Size;
            float z = Mathf.Cos(angle) * Size;
            vertices[i + 1] = new Vector3(x, 0.01f, z);
        }

        // 삼각형 생성
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    /// <summary>
    /// 원형 메시 생성
    /// </summary>
    Mesh GenerateCircleMesh()
    {
        Mesh mesh = new Mesh();

        int segments = 32;
        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];

        // 중심점
        vertices[0] = new Vector3(0, 0.01f, 0);

        // 원 둘레 생성
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2;
            float x = Mathf.Cos(angle) * Size;
            float z = Mathf.Sin(angle) * Size;
            vertices[i + 1] = new Vector3(x, 0.01f, z);
        }

        // 삼각형 생성
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % segments + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    /// <summary>
    /// 경고가 활성화 중인지 확인
    /// </summary>
    public bool IsActive => m_IsActive;

    /// <summary>
    /// 경고 진행도 (0~1)
    /// </summary>
    public float Progress => Mathf.Clamp01(m_ElapsedTime / Duration);
}
