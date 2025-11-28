using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 근접 적 - 무기 시스템으로 모든 패턴 관리
/// 
/// 무기 설정:
/// - 무기 0: FloorPatternWeapon (Slash - 부채꼴 근접공격)
/// - 무기 1: FloorPatternWeapon (Charge - 박스 범위 돌진)
/// 
/// 공격 결정:
/// - 거리 <= SlashRange: 무기 0 (Slash)
/// - 거리 <= ChargeRange: 무기 1 (Charge)
/// - 체력 <= 50%: 쫄몹 소환 (무기 사용 X, 직접 소환)
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

    [Tooltip("돌진 경고 프리팹 (없으면 기본 생성)")]
    public GameObject ChargeWarningPrefab;

    [Tooltip("돌진 데미지")]
    public float ChargeDamage = 30f;

    [Tooltip("돌진 데미지 반경")]
    public float ChargeDamageRadius = 1.5f;

    [Header("쫄몹 생성 설정")]
    [Tooltip("쫄몹 생성 체력 비율 임계값 (0.0 ~ 1.0)")]
    [Range(0.0f, 1.0f)]
    public float MinionSpawnHealthThreshold = 0.5f;

    [Tooltip("생성할 쫄몹 프리팹")]
    public GameObject MinionPrefab;

    [Tooltip("한 번에 생성할 쫄몹 수")]
    public int MinionsPerSpawn = 3;

    [Tooltip("쫄몹 생성 위치 (여러 개 지정 가능)")]
    public Transform[] MinionSpawnPoints;

    [Tooltip("쫄몹 생성 사운드")]
    public AudioClip MinionSpawnSFX;

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

    // 쫄몹 생성 관련 변수
    private bool m_HasSpawnedMinions = false;
    private float m_SpawnHealthThresholdValue;
    private List<GameObject> m_SpawnedMinions = new List<GameObject>();

    protected override void Start()
    {
        base.Start();

        // 보스용 체력 조정
        if (IsBoss)
        {
            m_Health.MaxHealth *= BossHealthMultiplier;
            m_Health.CurrentHealth = m_Health.MaxHealth;

            // 쫄몹 생성 체력 임계값 계산
            m_SpawnHealthThresholdValue = m_Health.MaxHealth * MinionSpawnHealthThreshold;

            // 죽음 이벤트 핸들러 추가
            m_Health.OnDie += OnBossDie;
        }

        // 쫄몹 생성 위치 자동 계산 (설정되지 않은 경우)
        if (MinionSpawnPoints == null || MinionSpawnPoints.Length <= 0)
        {
            LayerMask obstacles = LayerMask.GetMask("Obstacle", "Wall", "Default");
            MinionSpawnPoints = GetMinionSpawnPositions(transform, 5f, MinionsPerSpawn, obstacles).ToArray();
        }
    }

    protected override void Update()
    {
        base.Update();

        if (m_EnemyController.KnownDetectedTarget == null)
            return;

        // 플레이어를 향해 회전
        FaceTarget();

        // 보스 체력 기반 상태 전환 (쫄몹 소환)
        if (IsBoss)
        {
            CheckHealthBasedPatterns();
        }

        // 돌진 중이 아니고 소환 중이 아니면 다음 공격 결정
        // (소환 코루틴이 돌고 있어도 Update는 계속 되므로, 상태 플래그 관리가 필요할 수 있음.
        //  여기서는 간단히 소환은 독립적으로 실행되게 둠 - 필요시 m_IsSpawning 플래그 추가 가능)
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
        // 돌진 중에는 회전하지 않음
        if (m_IsCharging) return;

        Vector3 directionToTarget = (m_EnemyController.KnownDetectedTarget.transform.position - transform.position).normalized;
        directionToTarget.y = 0;

        if (directionToTarget.sqrMagnitude > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    /// <summary>
    /// 체력 기반 패턴 체크 (쫄몹 소환)
    /// </summary>
    private void CheckHealthBasedPatterns()
    {
        // 체력이 임계값 이하이고 아직 쫄몹을 소환하지 않았다면
        if (m_Health.CurrentHealth <= m_SpawnHealthThresholdValue && !m_HasSpawnedMinions)
        {
            StartCoroutine(SpawnMinionsSequence());
            m_HasSpawnedMinions = true;
        }
    }

    /// <summary>
    /// 쫄몹 소환 시퀀스
    /// </summary>
    private IEnumerator SpawnMinionsSequence()
    {
        // 애니메이션 (있다면)
        if (m_Animator != null)
        {
            m_Animator.SetTrigger("SpawnMinions"); // 애니메이터에 해당 트리거가 있어야 함
        }

        // 사운드
        if (MinionSpawnSFX)
        {
            AudioUtility.CreateSFX(MinionSpawnSFX, transform.position, AudioUtility.AudioGroups.EnemyAttack, 1f);
        }

        // 대기
        yield return new WaitForSeconds(1.0f);

        // 소환
        for (int i = 0; i < MinionsPerSpawn; i++)
        {
            SpawnMinion();
            yield return new WaitForSeconds(0.2f);
        }
    }

    /// <summary>
    /// 단일 쫄몹 소환
    /// </summary>
    private void SpawnMinion()
    {
        if (MinionPrefab == null || MinionSpawnPoints.Length == 0) return;

        Transform spawnPoint = MinionSpawnPoints[Random.Range(0, MinionSpawnPoints.Length)];
        GameObject minion = Instantiate(MinionPrefab, spawnPoint.position, spawnPoint.rotation);
        m_SpawnedMinions.Add(minion);

        // 소환된 쫄몹이 플레이어를 즉시 인지하도록 설정 (선택 사항)
        EnemyController enemyController = minion.GetComponent<EnemyController>();
        if (enemyController != null && m_EnemyController.KnownDetectedTarget != null)
        {
            // 필요시 타겟 전달 로직 추가
        }
    }

    /// <summary>
    /// 다음 공격 패턴 결정 및 실행
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
    /// 경고 표시 -> 돌진 -> 데미지
    /// </summary>
    private IEnumerator PerformChargeAttack()
    {
        m_IsCharging = true;

        // 1. 방향 설정 및 경고
        Vector3 targetPosition = m_EnemyController.KnownDetectedTarget.transform.position;
        Vector3 chargeDirection = (targetPosition - transform.position).normalized;
        chargeDirection.y = 0;
        
        // 정확한 회전
        transform.rotation = Quaternion.LookRotation(chargeDirection);

        // 경고 표시 (바닥)
        GameObject warningObj = null;
        Vector3 warningPos = transform.position + chargeDirection * (ChargeDistance * 0.5f) + Vector3.up * 0.1f;
        
        if (ChargeWarningPrefab != null)
        {
            warningObj = Instantiate(ChargeWarningPrefab, warningPos, Quaternion.LookRotation(chargeDirection));
            // 스케일 조정 (길이: ChargeDistance, 폭: ChargeDamageRadius * 2)
            warningObj.transform.localScale = new Vector3(ChargeDamageRadius * 2, 1f, ChargeDistance);
        }
        else
        {
            // 프리팹 없으면 임시 큐브 생성
            warningObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            warningObj.transform.position = warningPos;
            warningObj.transform.rotation = Quaternion.LookRotation(chargeDirection);
            warningObj.transform.localScale = new Vector3(ChargeDamageRadius * 2, 0.1f, ChargeDistance);
            warningObj.GetComponent<Collider>().enabled = false;
            var renderer = warningObj.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(1f, 0f, 0f, 0.3f);
        }

        // 돌진 준비 애니메이션
        if (m_Animator != null)
        {
            m_Animator.SetTrigger("Charge");
            m_Animator.SetFloat("MoveSpeed", 0f);
        }

        // 경고 시간 (1초)
        yield return new WaitForSeconds(1.0f);

        // 경고 제거
        if (warningObj != null) Destroy(warningObj);

        // 2. 돌진 실행
        float elapsedTime = 0f;
        
        // 데미지 중복 적용 방지용 리스트
        List<GameObject> damagedTargets = new List<GameObject>();

        while (elapsedTime < ChargeDuration)
        {
            elapsedTime += Time.deltaTime;

            // 돌진 이동
            Vector3 moveStep = chargeDirection * (ChargeDistance / ChargeDuration) * Time.deltaTime;
            
            if (m_EnemyController.NavMeshAgent != null && m_EnemyController.NavMeshAgent.isOnNavMesh)
            {
                m_EnemyController.NavMeshAgent.Move(moveStep);
            }
            else
            {
                transform.position += moveStep;
            }

            // 충돌 감지 및 데미지
            Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up, ChargeDamageRadius);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue; // 자기 자신 제외

                Damageable damageable = hit.GetComponent<Damageable>();
                if (damageable != null && !damagedTargets.Contains(hit.gameObject))
                {
                    // 플레이어인지 확인 (아군 공격 방지 로직이 필요하다면 추가)
                    Actor actor = hit.GetComponent<Actor>();
                    if (actor != null && actor.affiliation != GetComponent<Actor>().affiliation)
                    {
                        damageable.InflictDamage(ChargeDamage, false, gameObject);
                        damagedTargets.Add(hit.gameObject);
                        // 넉백 효과 등을 줄 수도 있음
                    }
                }
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

        // 생성된 쫄몹 정리
        foreach (var minion in m_SpawnedMinions)
        {
            if (minion != null)
            {
                Health h = minion.GetComponent<Health>();
                if (h != null) h.Kill();
                else Destroy(minion);
            }
        }

        // 게임 승리 조건 트리거
        AllObjectivesCompletedEvent victoryEvent = Events.AllObjectivesCompletedEvent;
        EventManager.Broadcast(victoryEvent);
    }

    /// <summary>
    /// 쫄몹 생성 위치 계산 (BossTurret에서 가져옴)
    /// </summary>
    public List<Transform> GetMinionSpawnPositions(Transform bossTransform, float radius, int minionCount, LayerMask obstacleLayer)
    {
        List<Transform> spawnPositions = new List<Transform>();
        if (bossTransform == null) return spawnPositions;

        float minionRadius = 0.5f;

        for (int i = 0; i < minionCount; i++)
        {
            bool validPositionFound = false;
            Vector3 spawnPos = Vector3.zero;
            int maxAttempts = 30;

            for (int attempt = 0; attempt < maxAttempts && !validPositionFound; attempt++)
            {
                Vector3 randomDirection = Random.insideUnitSphere;
                randomDirection.y = 0;
                randomDirection.Normalize();
                float randomDistance = radius * Mathf.Sqrt(Random.value);
                spawnPos = bossTransform.position + randomDirection * randomDistance;
                validPositionFound = !Physics.CheckSphere(spawnPos, minionRadius, obstacleLayer);
            }

            if (validPositionFound)
            {
                GameObject spawnMarker = new GameObject($"MinionSpawn_{i}");
                spawnMarker.transform.position = spawnPos;
                spawnMarker.transform.LookAt(new Vector3(bossTransform.position.x, spawnMarker.transform.position.y, bossTransform.position.z));
                spawnPositions.Add(spawnMarker.transform);
            }
        }
        return spawnPositions;
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

        // 쫄몹 생성 위치
        if (MinionSpawnPoints != null)
        {
            Gizmos.color = Color.green;
            foreach (var point in MinionSpawnPoints)
            {
                if (point != null) Gizmos.DrawWireSphere(point.position, 0.5f);
            }
        }
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