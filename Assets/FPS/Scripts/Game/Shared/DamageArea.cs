using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 폭발이나 범위 피해를 구현하는 클래스
/// 특정 범위 내의 모든 적에게 거리에 따라 감소하는 피해를 입힘
/// </summary>
public class DamageArea : MonoBehaviour
{
    // ===== 범위 피해 설정 =====
    [Tooltip("프로젝타일이 무언가에 충돌했을 때의 피해 범위")]
    public float AreaOfEffectDistance = 5f;

    [Tooltip("범위 피해의 거리에 따른 피해 배수 (애니메이션 곡선으로 세밀한 조정 가능)")]
    public AnimationCurve DamageRatioOverDistance;

    // ===== 디버그 설정 =====
    [Header("Debug")] 
    [Tooltip("범위 피해 반경을 시각화할 색상")]
    public Color AreaOfEffectColor = Color.red * 0.5f;

    /// <summary>
    /// 지정된 위치를 중심으로 범위 내의 모든 적에게 피해를 입힘
    /// </summary>
    /// <param name="damage">기본 피해량</param>
    /// <param name="center">범위 피해의 중심점</param>
    /// <param name="layers">피해를 입힐 대상의 레이어 마스크</param>
    /// <param name="interaction">트리거 콜라이더 포함 여부</param>
    /// <param name="owner">피해를 입힌 주체 (자신에게 피해를 입히지 않기 위해 사용)</param>
    public void InflictDamageInArea(float damage, Vector3 center, LayerMask layers,
        QueryTriggerInteraction interaction, GameObject owner)
    {
        // ===== 1단계: 범위 내 고유한 체력 컴포넌트 수집 =====
        // 같은 엔티티가 여러 콜라이더를 가진 경우 중복 피해를 방지하기 위해 Dictionary 사용
        Dictionary<Health, Damageable> uniqueDamagedHealths = new Dictionary<Health, Damageable>();

        // 중심점 기준으로 범위 내의 모든 콜라이더 검색
        // Physics.OverlapSphere는 범위 내 모든 콜라이더 배열을 반환
        Collider[] affectedColliders = Physics.OverlapSphere(center, AreaOfEffectDistance, layers, interaction);

        // 검색된 각 콜라이더에 대해 처리
        foreach (var coll in affectedColliders)
        {
            // 콜라이더에서 피해를 받을 수 있는 컴포넌트 찾기
            Damageable damageable = coll.GetComponent<Damageable>();
            if (damageable)
            {
                // Damageable을 소유한 Health 컴포넌트 찾기 (부모 오브젝트 포함)
                Health health = damageable.GetComponentInParent<Health>();
                
                // Health가 존재하고 아직 피해를 입지 않았다면 Dictionary에 추가
                // 같은 Health를 중복해서 추가하지 않음으로써 중복 피해 방지
                if (health && !uniqueDamagedHealths.ContainsKey(health))
                {
                    uniqueDamagedHealths.Add(health, damageable);
                }
            }
        }

        // ===== 2단계: 수집된 엔티티에 거리 감쇠 피해 적용 =====
        // 각각의 고유한 Damageable에 대해 거리 기반 피해 계산
        foreach (Damageable uniqueDamageable in uniqueDamagedHealths.Values)
        {
            // 피해 입은 대상과 폭발 중심 사이의 거리 계산
            float distance = Vector3.Distance(uniqueDamageable.transform.position, transform.position);
            
            // 거리를 정규화 (0 ~ 1 범위)하고 AnimationCurve로 피해 배수 계산
            // distance / AreaOfEffectDistance: 0(중심)에서 1(범위 끝) 사이의 값
            // DamageRatioOverDistance.Evaluate(): 거리에 따른 피해 비율 반환 (커스텀 곡선)
            // 예: 중심=1.0배, 범위 끝=0.0배 등으로 설정 가능
            uniqueDamageable.InflictDamage(
                damage * DamageRatioOverDistance.Evaluate(distance / AreaOfEffectDistance), 
                true, 
                owner);
        }
    }

    /// <summary>
    /// 에디터에서 선택되었을 때 범위 피해 반경을 시각화
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Gizmo 색상 설정 (빨간색 투명)
        Gizmos.color = AreaOfEffectColor;
        
        // 범위 피해 반경을 구(Sphere) 형태로 그림
        Gizmos.DrawSphere(transform.position, AreaOfEffectDistance);
    }
}