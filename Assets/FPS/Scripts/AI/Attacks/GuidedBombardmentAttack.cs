using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유도 폭격 공격 패턴
/// 1. 플레이어 주변 다수 위치에 원형 경고 표시
/// 2. 경고 위치에 순차적으로 폭발 발생
/// </summary>
public class GuidedBombardmentAttack : BossAttackPattern
{
    [Header("폭격 설정")]
    [Tooltip("폭격 지점 수")]
    public int BombCount = 6;

    [Tooltip("폭발 반경")]
    public float ExplosionRadius = 3f;

    [Tooltip("폭발 간격")]
    public float ExplosionInterval = 0.3f;

    [Tooltip("플레이어 주변 범위")]
    public float SpreadRadius = 10f;

    [Tooltip("경고 지속 시간")]
    public float WarningDuration = 3f;

    [Tooltip("플레이어 이동 예측 시간")]
    public float PredictionTime = 1f;

    [Header("프리펩")]
    [Tooltip("원형 경고 프리펩")]
    public GameObject CircleTelegraphPrefab;

    [Tooltip("폭발 VFX")]
    public GameObject ExplosionVfxPrefab;

    [Header("VFX/SFX")]
    [Tooltip("폭발 사운드")]
    public AudioClip ExplosionSfx;

    [Tooltip("경고 사운드")]
    public AudioClip WarningSfx;

    private List<TelegraphIndicator> m_Telegraphs = new List<TelegraphIndicator>();
    private List<Vector3> m_ExplosionPositions = new List<Vector3>();

    protected override IEnumerator ExecutePatternCoroutine()
    {
        if (Target == null) yield break;

        // 애니메이션 처리
        Animator animator = BossTransform.GetComponent<Animator>();
        if (animator == null) animator = BossTransform.GetComponentInChildren<Animator>();
        
        if (animator != null)
        {
            animator.SetTrigger("Bombardment");
            animator.SetBool("IsActive", true);
            animator.SetFloat("MoveSpeed", 0f);
        }

        m_Telegraphs.Clear();
        m_ExplosionPositions.Clear();

        // 1. 플레이어 이동 예측
        Vector3 predictedPosition = PredictPlayerPosition();

        // 2. 폭격 지점 계산 및 경고 표시
        for (int i = 0; i < BombCount; i++)
        {
            Vector3 bombPosition = CalculateBombPosition(predictedPosition, i);
            m_ExplosionPositions.Add(bombPosition);

            // 경고 표시
            if (CircleTelegraphPrefab != null)
            {
                Vector3 telegraphPos = bombPosition + Vector3.up * 0.1f;
                GameObject telegraphObj = Instantiate(CircleTelegraphPrefab, telegraphPos, Quaternion.identity);
                TelegraphIndicator telegraph = telegraphObj.GetComponent<TelegraphIndicator>();
                
                if (telegraph != null)
                {
                    telegraph.Size = ExplosionRadius;
                    telegraph.Duration = WarningDuration;
                    telegraph.Show(telegraphPos, Quaternion.identity);
                    m_Telegraphs.Add(telegraph);
                }
            }
        }

        // 경고 사운드
        if (WarningSfx)
        {
            AudioUtility.CreateSFX(WarningSfx, BossTransform.position, AudioUtility.AudioGroups.EnemyAttack, 0f);
        }

        // 3. 경고 시간 대기
        yield return new WaitForSeconds(WarningDuration);

        // 4. 순차 폭발
        for (int i = 0; i < m_ExplosionPositions.Count; i++)
        {
            Explode(m_ExplosionPositions[i]);

            if (ExplosionSfx)
            {
                AudioUtility.CreateSFX(ExplosionSfx, m_ExplosionPositions[i], AudioUtility.AudioGroups.EnemyAttack, 0f);
            }

            if (i < m_ExplosionPositions.Count - 1)
            {
                yield return new WaitForSeconds(ExplosionInterval);
            }
        }

        // 경고 정리
        foreach (var telegraph in m_Telegraphs)
        {
            if (telegraph != null)
            {
                Destroy(telegraph.gameObject);
            }
        }

        // 애니메이션 종료
        if (animator != null)
        {
            animator.SetBool("IsActive", false);
        }

        OnPatternComplete();
    }

    /// <summary>
    /// 플레이어 위치 예측
    /// </summary>
    Vector3 PredictPlayerPosition()
    {
        if (Target == null) return Vector3.zero;

        // 플레이어 속도 추정
        Rigidbody targetRb = Target.GetComponent<Rigidbody>();
        Vector3 velocity = targetRb != null ? targetRb.linearVelocity : Vector3.zero;

        // 예측 위치 계산
        Vector3 predictedPos = Target.position + velocity * PredictionTime;
        predictedPos.y = Target.position.y; // Y축 고정

        return predictedPos;
    }

    /// <summary>
    /// 폭격 지점 계산
    /// </summary>
    Vector3 CalculateBombPosition(Vector3 centerPosition, int index)
    {
        // 플레이어 주변에 원형으로 배치
        float angle = (360f / BombCount) * index * Mathf.Deg2Rad;
        float randomRadius = Random.Range(SpreadRadius * 0.5f, SpreadRadius);
        
        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * randomRadius,
            0,
            Mathf.Sin(angle) * randomRadius
        );

        Vector3 position = centerPosition + offset;
        position.y = 0.1f; // 바닥 높이

        return position;
    }

    /// <summary>
    /// 폭발 실행
    /// </summary>
    void Explode(Vector3 position)
    {
        // VFX 생성
        if (ExplosionVfxPrefab != null)
        {
            GameObject vfx = Instantiate(ExplosionVfxPrefab, position, Quaternion.identity);
            Destroy(vfx, 5f);
        }

        // 범위 내 데미지 적용
        Collider[] hitColliders = Physics.OverlapSphere(position, ExplosionRadius);
        foreach (Collider col in hitColliders)
        {
            Damageable damageable = col.GetComponent<Damageable>();
            if (damageable != null && col.gameObject.layer != LayerMask.NameToLayer("Enemy"))
            {
                damageable.InflictDamage(Damage, true, BossTransform.gameObject);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // 폭발 범위 시각화
        Gizmos.color = Color.red;
        foreach (var pos in m_ExplosionPositions)
        {
            Gizmos.DrawWireSphere(pos, ExplosionRadius);
        }
    }
}
