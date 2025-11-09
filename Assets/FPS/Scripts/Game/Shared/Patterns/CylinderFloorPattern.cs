using UnityEngine;

/// <summary>
/// 원기둥 패턴 - 수직 범위 공격
/// 시전자 위치를 중심으로 원통형 범위의 모든 대상에게 피해
/// 원기둥 형태의 지진파, 충격파 효과에 적합
/// </summary>

[RequireComponent(typeof(CapsuleCollider))]
public class CylinderFloorPattern : FloorPatternBase
{
    [Header("Cylinder Settings")]
    [Tooltip("원기둥의 반지름")]
    public float cylinderRadius = 6f;

    [Tooltip("원기둥의 높이")]
    public float cylinderHeight = 3f;

    [Tooltip("원기둥 데미지 배수 (기본 1.2배)")]
    public float damageMupliplier = 1.2f;

    /// <summary>
    /// 원기둥 콜라이더 생성 (한번만)
    /// </summary>
    protected override void CreateCollider()
    {
        CapsuleCollider capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
        capsuleCollider.isTrigger = true;
        capsuleCollider.radius = 1f;      // 기본값 1, Initialize에서 업데이트
        capsuleCollider.height = 1f;      // 기본값 1, Initialize에서 업데이트
    }

    /// <summary>
    /// 원기둥 콜라이더 크기 업데이트
    /// </summary>
    protected override void UpdateColliderSize()
    {
        CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
        {
            capsuleCollider.radius = cylinderRadius;
            capsuleCollider.height = cylinderHeight;
        }
    }

    /// <summary>
    /// 원기둥 패턴의 메시 설정 (캐시에서 복제)
    /// </summary>
    protected override void SetupPatternMesh()
    {
        if (meshFilter != null)
        {
            meshFilter.mesh = FloorPatternMeshCache.GetCircleMesh();
            // 스케일 적용 (메시는 반지름 1로 기본 생성됨)
            transform.localScale = Vector3.one * cylinderRadius;
        }
    }

    /// <summary>
    /// 원기둥 범위 내의 모든 콜라이더 반환
    /// </summary>
    protected override Collider[] GetAffectedColliders()
    {
        // 원기둥은 높이 제한이 있음
        Collider[] allColliders = Physics.OverlapSphere(
            transform.position,
            cylinderRadius,
            LayerMask.GetMask("Player"),
            QueryTriggerInteraction.Ignore
        );

        // 높이 필터링
        System.Collections.Generic.List<Collider> cylinderColliders =
            new System.Collections.Generic.List<Collider>();

        foreach (var collider in allColliders)
        {
            float heightDiff = Mathf.Abs(collider.transform.position.y - transform.position.y);
            if (heightDiff <= cylinderHeight / 2f)
            {
                cylinderColliders.Add(collider);
            }
        }

        return cylinderColliders.ToArray();
    }

    /// <summary>
    /// 거리 기반 감쇠 (원기둥형)
    /// </summary>
    protected override float CalculateDistanceRatio(Vector3 targetPosition)
    {
        // 수평 거리만 계산 (Y축 무시)
        Vector3 horizontalDistance = targetPosition - transform.position;
        horizontalDistance.y = 0;
        float distance = horizontalDistance.magnitude;

        return Mathf.Clamp01(1f - (distance / cylinderRadius));
    }

    /// <summary>
    /// 원기둥 패턴의 데미지 증가
    /// </summary>
    protected override float CalculateFinalDamage(float distanceRatio)
    {
        float baseDamage = base.CalculateFinalDamage(distanceRatio);
        return baseDamage * damageMupliplier;
    }

    /// <summary>
    /// 원기둥 패턴 시각화 (Gizmo)
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isDamageActive ? activeColor : warningColor;

        // 원기둥 상하단 원 그리기
        DrawCylinder(transform.position, cylinderRadius, cylinderHeight);
    }

    /// <summary>
    /// 원기둥 모양 그리기
    /// </summary>
    private void DrawCylinder(Vector3 center, float radius, float height)
    {
        // 상단 원
        DrawCircle(center + Vector3.up * (height / 2f), radius, 32);

        // 하단 원
        DrawCircle(center - Vector3.up * (height / 2f), radius, 32);

        // 연결선
        for (int i = 0; i < 8; i++)
        {
            float angle = (i / 8f) * Mathf.PI * 2f;
            Vector3 point = new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
            Gizmos.DrawLine(center + Vector3.up * (height / 2f) + point,
                           center - Vector3.up * (height / 2f) + point);
        }
    }

    /// <summary>
    /// 원형 그리기 헬퍼 함수
    /// </summary>
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angle = 360f / segments;
        Vector3 lastPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float rad = angle * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(rad) * radius,
                0,
                Mathf.Sin(rad) * radius
            );
            Gizmos.DrawLine(lastPoint, newPoint);
            lastPoint = newPoint;
        }
    }
}