using UnityEngine;

/// <summary>
/// 삼각형 패턴 - 방향성 공격
/// 시전자가 바라보는 방향으로 뻗어나가는 삼각형 범위 공격
/// 돌진, 전방향 공격에 적합
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class TriangleFloorPattern : FloorPatternBase
{
    [Header("Triangle Settings")]
    [Tooltip("삼각형의 밑변 너비")]
    public float triangleWidth = 6f;

    [Tooltip("삼각형의 높이")]
    public float triangleHeight = 12f;

    [Tooltip("삼각형 데미지 배수 (기본 1.5배)")]
    public float damageMupliplier = 1.5f;

    /// <summary>
    /// 삼각형 콜라이더 생성 (한번만, 박스로 근사)
    /// </summary>
    protected override void CreateCollider()
    {
        BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;
        boxCollider.size = Vector3.one;  // 기본값 1, Initialize에서 업데이트
        boxCollider.center = new Vector3(0, 0, 0.5f);
    }

    /// <summary>
    /// 삼각형 콜라이더 크기 업데이트
    /// </summary>
    protected override void UpdateColliderSize()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.size = new Vector3(triangleWidth, 1f, triangleHeight);
            boxCollider.center = new Vector3(0, 0, triangleHeight / 2f);
        }
    }

    /// <summary>
    /// 삼각형 패턴의 메시 설정 (캐시에서 복제)
    /// </summary>
    protected override void SetupPatternMesh()
    {
        if (meshFilter != null)
        {
            meshFilter.mesh = FloorPatternMeshCache.GetTriangleMesh();
            // 스케일 적용 (메시는 1x1로 기본 생성됨)
            transform.localScale = new Vector3(triangleWidth, 1f, triangleHeight);
        }
    }

    /// <summary>
    /// 삼각형 범위 내의 모든 콜라이더 반환
    /// </summary>
    protected override Collider[] GetAffectedColliders()
    {
        // 먼저 삼각형 범위의 대략적인 구 범위로 검색
        float maxDistance = triangleHeight;
        Collider[] allColliders = Physics.OverlapSphere(
            transform.position + transform.forward * (triangleHeight / 2f),
            Mathf.Max(triangleWidth, triangleHeight),
            LayerMask.GetMask("Player"),
            QueryTriggerInteraction.Ignore
        );

        // 삼각형 범위로 필터링
        System.Collections.Generic.List<Collider> triangleColliders =
            new System.Collections.Generic.List<Collider>();

        foreach (var collider in allColliders)
        {
            if (IsInTriangleRange(collider.transform.position))
            {
                triangleColliders.Add(collider);
            }
        }

        return triangleColliders.ToArray();
    }

    /// <summary>
    /// 점이 삼각형 범위 내에 있는지 확인
    /// </summary>
    private bool IsInTriangleRange(Vector3 targetPosition)
    {
        Vector3 localPosition = transform.worldToLocalMatrix.MultiplyPoint3x4(targetPosition);

        // Z축: 정면 방향으로의 거리
        if (localPosition.z < 0 || localPosition.z > triangleHeight)
            return false;

        // X축: 너비 (앞으로 갈수록 좁아짐 - 삼각형 모양)
        float widthAtZ = triangleWidth * (1f - (localPosition.z / triangleHeight)) / 2f;
        if (Mathf.Abs(localPosition.x) > widthAtZ)
            return false;

        return true;
    }

    /// <summary>
    /// 거리 기반 감쇠 (삼각형)
    /// 앞쪽일수록 강함
    /// </summary>
    protected override float CalculateDistanceRatio(Vector3 targetPosition)
    {
        Vector3 localPosition = transform.worldToLocalMatrix.MultiplyPoint3x4(targetPosition);

        // Z축 거리 (깊이)
        float depthRatio = Mathf.Clamp01(1f - (localPosition.z / triangleHeight));

        // X축 거리 (너비)
        float widthAtZ = triangleWidth * (1f - (localPosition.z / triangleHeight)) / 2f;
        float widthRatio = widthAtZ > 0 ?
            Mathf.Clamp01(1f - (Mathf.Abs(localPosition.x) / widthAtZ)) : 0;

        return Mathf.Min(depthRatio, widthRatio);
    }

    /// <summary>
    /// 삼각형 패턴의 데미지 증가
    /// </summary>
    protected override float CalculateFinalDamage(float distanceRatio)
    {
        float baseDamage = base.CalculateFinalDamage(distanceRatio);
        return baseDamage * damageMupliplier;
    }

    /// <summary>
    /// 삼각형 패턴 시각화 (Gizmo)
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isDamageActive ? activeColor : warningColor;

        // 삼각형의 3개 꼭짓점
        Vector3 front = transform.position + transform.forward * triangleHeight;
        Vector3 leftBack = transform.position - transform.right * (triangleWidth / 2f);
        Vector3 rightBack = transform.position + transform.right * (triangleWidth / 2f);

        // 삼각형 그리기
        Gizmos.DrawLine(front, leftBack);
        Gizmos.DrawLine(front, rightBack);
        Gizmos.DrawLine(leftBack, rightBack);

        // 밑면 그리기 (시전자 위치)
        Gizmos.DrawLine(transform.position, front);
    }
}