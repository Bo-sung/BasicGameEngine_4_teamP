
using UnityEngine;

/// <summary>
/// FloorPattern을 무기로 사용하는 BossPatternBase
/// Slash(부채꼴), Charge(돌진) 등의 바닥 패턴 공격
/// </summary>
public class FloorPatternWeapon : BossPatternBase
{
    [Header("바닥 패턴 설정")]
    [Tooltip("사용할 FloorPattern 이름 (예: Sector, Box)")]
    public string PatternName = "Sector";

    [Tooltip("패턴 퍼짐 각도 (부채꼴용)")]
    public float PatternSpreadAngle = 120f;

    [Tooltip("패턴 범위")]
    public float PatternRange = 3f;

    [Tooltip("상태이상 효과")]
    public string[] StatusEffects = new[] { "Slow" };

    [Tooltip("상태이상 강도")]
    public float StatusIntensity = 0.5f;

    [Header("참조")]
    [Tooltip("바닥 패턴 스포너")]
    public FloorPatternSpawner PatternSpawner;

    protected override void Start()
    {
        base.Start();

        // 패턴 스포너 자동 찾기
        if (PatternSpawner == null)
        {
            PatternSpawner = FindObjectOfType<FloorPatternSpawner>();
            if (PatternSpawner == null)
            {
                Debug.LogError($"[{gameObject.name}] FloorPatternSpawner not found in scene!", gameObject);
            }
        }
    }

    /// <summary>
    /// FloorPattern 실행
    /// </summary>
    protected override bool ExecutePattern()
    {
        if (PatternSpawner == null)
        {
            Debug.LogWarning($"[{gameObject.name}] PatternSpawner is not assigned!", gameObject);
            return false;
        }

        if (Owner == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Owner is not set!", gameObject);
            return false;
        }

        // 시전자 정보 생성
        var casterInfo = new FloorPatternBase.CasterInfo(
            pos: transform.position + transform.forward * (PatternRange / 2f),
            dir: transform.forward,
            dmg: PatternDamage,
            effects: StatusEffects,
            intensity: StatusIntensity
        );

        // 패턴 생성
        FloorPatternBase pattern = PatternSpawner.SpawnPattern(PatternName, casterInfo);

        return pattern != null;
    }

    /// <summary>
    /// 공격 애니메이션 재생 (자식 클래스에서 오버라이드 가능)
    /// </summary>
    protected override void PlayAttackAnimation()
    {
        base.PlayAttackAnimation();

        // 추가적인 시각 효과
        Debug.Log($"[{gameObject.name}] Spawning {PatternName} pattern at {transform.position}");
    }

    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    public override void DebugPrintInfo()
    {
        base.DebugPrintInfo();
        Debug.Log($"패턴: {PatternName}\n" +
            $"범위: {PatternRange}\n" +
            $"각도: {PatternSpreadAngle}°");
    }

    protected override void OnDrawGizmosSelected()
    {
        // 패턴 범위 시각화
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, PatternRange);

        // 발사 방향
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * PatternRange);
    }
}