using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 부채꼴 패턴 - 부채꼴 범위 공격
/// 시전자가 바라보는 방향을 중심으로 부채꼴 범위 내의 모든 대상에게 피해
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class CircularSectorFloorPattern : FloorPatternBase
{
    [Header("Sector Settings")]
    [Tooltip("부채꼴의 퍼짐 각도 (예: 120도)")]
    public float spreadAngle = 120f;

    [Tooltip("부채꼴의 반지름")]
    public float sectorRadius = 10f;

    [Tooltip("부채꼴 데미지 배수 (기본 1.3배)")]
    public float damageMultiplier = 1.3f;

    /// <summary>
    /// 부채꼴 콜라이더 생성 (한번만)
    /// </summary>
    protected override void CreateCollider()
    {
        SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        sphereCollider.radius = 1f;  // 기본값 1, Initialize에서 업데이트
    }

    /// <summary>
    /// 부채꼴 콜라이더 크기 업데이트
    /// </summary>
    protected override void UpdateColliderSize()
    {
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            sphereCollider.radius = sectorRadius;
        }
    }

    /// <summary>
    /// 부채꼴 패턴의 메시 설정 (캐시에서 복제)
    /// </summary>
    protected override void SetupPatternMesh()
    {
        if (meshFilter != null)
        {
            meshFilter.mesh = FloorPatternMeshCache.GetSectorMesh();
            // 스케일 적용 (메시는 반지름 1로 기본 생성됨)
            transform.localScale = Vector3.one * sectorRadius;
        }
    }

    /// <summary>
    /// 부채꼴 범위 내의 모든 콜라이더 반환
    /// </summary>
    protected override Collider[] GetAffectedColliders()
    {
        // 먼저 구 범위로 거친 검색
        Collider[] nearbyColliders = Physics.OverlapSphere(
            transform.position,
            sectorRadius,
            LayerMask.GetMask("Player"),
            QueryTriggerInteraction.Ignore
        );

        // 부채꼴 범위로 세밀한 필터링
        List<Collider> sectorColliders = new List<Collider>();

        foreach (var collider in nearbyColliders)
        {
            if (IsInSectorRange(collider.transform.position))
            {
                sectorColliders.Add(collider);
            }
        }

        return sectorColliders.ToArray();
    }

    /// <summary>
    /// 점이 부채꼴 범위 내에 있는지 확인
    /// </summary>
    private bool IsInSectorRange(Vector3 targetPosition)
    {
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;

        // 각도 계산
        float angleDifference = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

        // 부채꼴 범위 내 확인
        return Mathf.Abs(angleDifference) <= (spreadAngle / 2f);
    }

    /// <summary>
    /// 거리 기반 감쇠 (부채꼴)
    /// </summary>
    protected override float CalculateDistanceRatio(Vector3 targetPosition)
    {
        float distance = Vector3.Distance(targetPosition, transform.position);
        return Mathf.Clamp01(1f - (distance / sectorRadius));
    }

    /// <summary>
    /// 부채꼴 패턴의 데미지 증가
    /// </summary>
    protected override float CalculateFinalDamage(float distanceRatio)
    {
        float baseDamage = base.CalculateFinalDamage(distanceRatio);
        return baseDamage * damageMultiplier;
    }

    /// <summary>
    /// 부채꼴 패턴 시각화 (Gizmo)
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isDamageActive ? activeColor : warningColor;

        // 부채꼴 호 그리기
        float segments = 32f;
        float angleStep = spreadAngle / segments;
        float startAngle = -(spreadAngle / 2f);

        Vector3 prevPoint = transform.position + GetSectorPoint(startAngle);

        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            Vector3 nextPoint = transform.position + GetSectorPoint(currentAngle);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }

        // 부채꼴 변 그리기
        Vector3 leftPoint = transform.position + GetSectorPoint(-(spreadAngle / 2f));
        Vector3 rightPoint = transform.position + GetSectorPoint(spreadAngle / 2f);

        Gizmos.DrawLine(transform.position, leftPoint);
        Gizmos.DrawLine(transform.position, rightPoint);
    }

    /// <summary>
    /// 부채꼴 상의 점 계산
    /// </summary>
    private Vector3 GetSectorPoint(float angleInDegrees)
    {
        float angleInRadians = angleInDegrees * Mathf.Deg2Rad;
        Vector3 right = Vector3.Cross(Vector3.up, transform.forward).normalized;

        return (Mathf.Sin(angleInRadians) * right + Mathf.Cos(angleInRadians) * transform.forward) * sectorRadius;
    }
}