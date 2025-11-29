using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 구 패턴 - 구 범위 공격
/// 시전자가 바라보는 방향을 중심으로 구 범위 내의 모든 대상에게 피해
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class CircleFloorPattern : FloorPatternBase
{
    [Header("Circle Settings")]
    [Tooltip("원 반지름")]
    public float Radius = 10f;

    protected override void CreateCollider()
    {
        SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        sphereCollider.radius = Radius;
    }

    protected override void UpdateColliderSize()
    {
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            sphereCollider.radius = Radius;
        }
    }

    protected override void SetupPatternMesh()
    {
        if (meshFilter != null)
        {
            meshFilter.mesh = FloorPatternMeshCache.GetCircleMesh();
            transform.localScale = Vector3.one * Radius;
        }
    }

    protected override Collider[] GetAffectedColliders()
    {
        return Physics.OverlapSphere(
            transform.position,
            Radius,
            LayerMask.GetMask("Player"),
            QueryTriggerInteraction.Ignore
        );
    }

    protected override float CalculateDistanceRatio(Vector3 targetPosition)
    {
        float distance = Vector3.Distance(targetPosition, transform.position);
        return Mathf.Clamp01(1f - (distance / Radius));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Radius);
    }
}