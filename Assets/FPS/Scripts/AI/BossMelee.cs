using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 근접 적 - 무기 시스템으로 모든 패턴 관리
/// 
/// 무기 설정:
/// - 무기 0: FloorPatternWeapon (Slash - 부채꼴 근접공격)
/// - 무기 1: FloorPatternWeapon (Charge - 박스 범위 돌진)
/// - 무기 2: MinionSpawnWeapon (Summon - 쫄몹 소환)
/// 
/// 공격 결정:
/// - 거리 <= SlashRange: 무기 0 (Slash)
/// - 거리 <= ChargeRange: 무기 1 (Charge)
/// - 체력 <= 50%: 무기 2 (Summon) - 자동
/// </summary>
public class BossMelee : EnemyMobile
{
    [Header("보스 설정")]
    [Tooltip("보스 여부")]
    public bool IsBoss = true;

    [Tooltip("보스 체력 스케일")]
    public float BossHealthMultiplier = 5f;

    [Header("공격 거리 설정")]
    [Tooltip("Slash 공격 범위")]
    public float SlashRange = 3f;

    [Tooltip("Charge 공격 범위")]
    public float ChargeRange = 8f;

    [Header("돌진 공격 설정")]
    [Tooltip("돌진 거리")]
    public float ChargeDistance = 8f;

    [Tooltip("돌진 지속 시간")]
    public float ChargeDuration = 1f;

    [Header("보스 죽음 효과")]
    [Tooltip("보스 죽음 VFX")]
    public GameObject BossDeathVFX;

    [Tooltip("보스 죽음 사운드")]
    public AudioClip BossDeathSFX;

    [Tooltip("보스 죽음 폭발 반경")]
    public float BossDeathExplosionRadius = 10f;

    [Tooltip("보스 죽음 폭발 데미지")]
    public float BossDeathExplosionDamage = 50f;

    // 공격 상태
    private bool m_IsCharging = false;

    protected override void Start()
    {
        base.Start();

        // 보스용 체력 조정
        if (IsBoss)
        {
            m_Health.MaxHealth *= BossHealthMultiplier;
            m_Health.CurrentHealth = m_Health.MaxHealth;

            // 죽음 이벤트 핸들러 추가
            m_Health.OnDie += OnBossDie;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (m_EnemyController.KnownDetectedTarget == null)
            return;

        // 플레이어를 향해 회전
        FaceTarget();

        // 돌진 중이 아니면 다음 공격 결정
        if (!m_IsCharging)
        {
            DetermineNextAttackPattern();
        }
    }

    /// <summary>
    /// 플레이어를 향해 회전
    /// </summary>
    private void FaceTarget()
    {
        Vector3 directionToTarget = (m_EnemyController.KnownDetectedTarget.transform.position - transform.position).normalized;
        directionToTarget.y = 0;

        if (directionToTarget.sqrMagnitude > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    /// <summary>
    /// 다음 공격 패턴 결정 및 실행
    /// 무기 시스템 활용 (TryAtack 호출)
    /// </summary>
    private void DetermineNextAttackPattern()
    {
        if (m_EnemyController.KnownDetectedTarget == null)
            return;

        float distanceToTarget = Vector3.Distance(transform.position, m_EnemyController.KnownDetectedTarget.transform.position);

        // 무기 0: Slash 공격 (부채꼴 근접) - 거리 <= 3m
        if (distanceToTarget <= SlashRange)
        {
            m_EnemyController.TryAtack(m_EnemyController.KnownDetectedTarget.transform.position);
        }
        // 무기 1: Charge 공격 (돌진) - 거리 3~8m
        else if (distanceToTarget <= ChargeRange)
        {
            StartCoroutine(PerformChargeAttack());
        }
        // 플레이어 쪽으로 이동
        else
        {
            MoveTowardTarget();
        }
    }

    /// <summary>
    /// 박스 범위 돌진 공격 수행
    /// 돌진 중 주기적으로 공격 패턴 생성
    /// </summary>
    private IEnumerator PerformChargeAttack()
    {
        m_IsCharging = true;

        // 돌진 애니메이션 재생
        if (m_Animator != null)
        {
            m_Animator.SetTrigger("Charge");
            m_Animator.SetFloat("MoveSpeed", 0f);
        }

        // 돌진 시작 전 준비 시간
        yield return new WaitForSeconds(0.3f);

        // 돌진 실행
        float elapsedTime = 0f;
        Vector3 chargeDirection = (m_EnemyController.KnownDetectedTarget.transform.position - transform.position).normalized;
        chargeDirection.y = 0;

        while (elapsedTime < ChargeDuration)
        {
            elapsedTime += Time.deltaTime;

            // 돌진 이동
            Vector3 moveDirection = chargeDirection * (ChargeDistance / ChargeDuration);

            // NavMeshAgent 안전 확인
            if (m_EnemyController.NavMeshAgent != null && m_EnemyController.NavMeshAgent.isOnNavMesh)
            {
                m_EnemyController.NavMeshAgent.velocity = moveDirection;
            }

            // 돌진 중 주기적으로 공격 패턴 생성 (0.3초마다)
            if (elapsedTime > 0.2f && elapsedTime % 0.3f < Time.deltaTime)
            {
                // 무기 1의 공격 실행
                m_EnemyController.TryAtack(m_EnemyController.KnownDetectedTarget.transform.position);
            }

            yield return null;
        }

        m_IsCharging = false;
    }

    /// <summary>
    /// 플레이어를 향해 이동
    /// </summary>
    private void MoveTowardTarget()
    {
        if (m_EnemyController.KnownDetectedTarget == null || m_IsCharging)
            return;

        if (m_EnemyController.NavMeshAgent != null)
        {
            m_EnemyController.SetNavDestination(m_EnemyController.KnownDetectedTarget.transform.position);

            if (m_Animator != null)
            {
                m_Animator.SetFloat("MoveSpeed", m_EnemyController.NavMeshAgent.velocity.magnitude);
            }
        }
    }

    /// <summary>
    /// 보스 사망 처리
    /// </summary>
    private void OnBossDie()
    {
        // 보스 죽음 효과
        if (BossDeathVFX)
        {
            Instantiate(BossDeathVFX, transform.position, Quaternion.identity);
        }

        // 보스 죽음 사운드
        if (BossDeathSFX)
        {
            AudioUtility.CreateSFX(BossDeathSFX, transform.position, AudioUtility.AudioGroups.EnemyDetection, 1f);
        }

        // 폭발 데미지
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, BossDeathExplosionRadius);
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider == null || hitCollider.gameObject == gameObject)
                continue;

            Damageable damageable = hitCollider.GetComponent<Damageable>();
            if (damageable != null)
            {
                damageable.InflictDamage(BossDeathExplosionDamage, true, gameObject);
            }
        }

        // 생성된 쫄몹 정리 (무기 2에서 관리)
        MinionSpawnWeapon minionWeapon = GetComponent<MinionSpawnWeapon>();
        if (minionWeapon != null)
        {
            minionWeapon.ClearSpawnedMinions();
        }

        // 게임 승리 조건 트리거
        AllObjectivesCompletedEvent victoryEvent = Events.AllObjectivesCompletedEvent;
        EventManager.Broadcast(victoryEvent);
    }

    /// <summary>
    /// 기즈모 표시
    /// </summary>
    void OnDrawGizmos()
    {
        // Slash 공격 범위 (빨강)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, SlashRange);

        // Charge 공격 범위 (노랑)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, ChargeRange);

        // 보스 죽음 폭발 범위 (어두운 빨강)
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, BossDeathExplosionRadius);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (m_Health != null)
        {
            m_Health.OnDie -= OnBossDie;
        }
    }
}