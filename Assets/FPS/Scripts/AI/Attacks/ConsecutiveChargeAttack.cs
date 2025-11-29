using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 연속 돌진 공격 패턴
/// 1. 플레이어 방향으로 직선 경고 표시
/// 2. 경고 후 빠른 돌진 (2-3회 연속)
/// </summary>
public class ConsecutiveChargeAttack : BossAttackPattern
{
    [Header("연속 돌진 설정")]
    [Tooltip("돌진 횟수")]
    public int ChargeCount = 3;

    [Tooltip("돌진 거리")]
    public float ChargeDistance = 10f;

    [Tooltip("돌진 지속 시간")]
    public float ChargeDuration = 0.8f;

    [Tooltip("돌진 데미지 반경")]
    public float ChargeDamageRadius = 2f;

    [Tooltip("돌진 간 대기 시간")]
    public float DelayBetweenCharges = 0.5f;

    [Tooltip("경고 지속 시간")]
    public float WarningDuration = 1f;

    [Header("프리팹")]
    [Tooltip("직선 경고 프리팹")]
    public GameObject LineTelegraphPrefab;

    [Header("VFX/SFX")]
    [Tooltip("돌진 사운드")]
    public AudioClip ChargeSfx;

    private TelegraphIndicator m_CurrentTelegraph;

    protected override IEnumerator ExecutePatternCoroutine()
    {
        if (Target == null) yield break;

        // 애니메이션 처리
        Animator animator = BossTransform.GetComponent<Animator>();
        if (animator == null) animator = BossTransform.GetComponentInChildren<Animator>();
        
        if (animator != null)
        {
            animator.SetTrigger("ConsecutiveCharge");
            animator.SetFloat("MoveSpeed", 0f);
        }

        // 연속 돌진
        for (int i = 0; i < ChargeCount; i++)
        {
            // 1. 플레이어 방향 계산
            Vector3 directionToTarget = (Target.position - BossTransform.position).normalized;
            directionToTarget.y = 0;

            // 2. 경고 표시
            Vector3 telegraphPosition = BossTransform.position + Vector3.up * 0.1f;
            Quaternion telegraphRotation = Quaternion.LookRotation(directionToTarget);

            if (LineTelegraphPrefab != null)
            {
                GameObject telegraphObj = Instantiate(LineTelegraphPrefab, telegraphPosition, telegraphRotation);
                m_CurrentTelegraph = telegraphObj.GetComponent<TelegraphIndicator>();
                
                if (m_CurrentTelegraph != null)
                {
                    m_CurrentTelegraph.Size = ChargeDistance;
                    m_CurrentTelegraph.LineWidth = ChargeDamageRadius * 2;
                    m_CurrentTelegraph.Duration = WarningDuration;
                    m_CurrentTelegraph.Show(telegraphPosition, telegraphRotation);
                }
            }

            // 3. 경고 시간 대기
            yield return new WaitForSeconds(WarningDuration);

            // 4. 돌진 실행
            yield return StartCoroutine(PerformCharge(directionToTarget));

            // 경고 정리
            if (m_CurrentTelegraph != null)
            {
                Destroy(m_CurrentTelegraph.gameObject);
            }

            // 5. 다음 돌진 전 대기 (마지막 돌진 제외)
            if (i < ChargeCount - 1)
            {
                yield return new WaitForSeconds(DelayBetweenCharges);
            }
        }

        OnPatternComplete();
    }

    IEnumerator PerformCharge(Vector3 direction)
    {
        // 돌진 사운드
        if (ChargeSfx)
        {
            AudioUtility.CreateSFX(ChargeSfx, BossTransform.position, AudioUtility.AudioGroups.EnemyAttack, 0f);
        }

        float elapsedTime = 0f;
        Vector3 startPosition = BossTransform.position;
        List<GameObject> damagedTargets = new List<GameObject>();

        while (elapsedTime < ChargeDuration)
        {
            elapsedTime += Time.deltaTime;

            // 이동
            Vector3 moveStep = direction * (ChargeDistance / ChargeDuration) * Time.deltaTime;
            BossTransform.position += moveStep;

            // 충돌 감지 및 데미지
            Collider[] hits = Physics.OverlapSphere(BossTransform.position + Vector3.up, ChargeDamageRadius);
            foreach (var hit in hits)
            {
                if (hit.gameObject == BossTransform.gameObject) continue;

                Damageable damageable = hit.GetComponent<Damageable>();
                if (damageable != null && !damagedTargets.Contains(hit.gameObject))
                {
                    Actor actor = hit.GetComponent<Actor>();
                    if (actor != null && actor.affiliation != BossTransform.GetComponent<Actor>().affiliation)
                    {
                        damageable.InflictDamage(Damage, false, BossTransform.gameObject);
                        damagedTargets.Add(hit.gameObject);
                    }
                }
            }

            yield return null;
        }
    }
}
