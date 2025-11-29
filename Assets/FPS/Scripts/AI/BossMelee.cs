using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 근접 적 - 패턴 기반 공격 시스템
/// 
/// 공격 패턴:
/// - ConsecutiveChargeAttack: 연속 돌진
/// - GroundShockwaveAttack: 바닥 충격파
/// - SlamAttack: 내려찍기
/// - 쫄몹 소환: 체력 기반 트리거
/// </summary>
public class BossMelee : EnemyMobile
{
    [Header("보스 설정")]
    [Tooltip("보스 여부")]
    public bool IsBoss = true;

    [Tooltip("보스 체력 스케일")]
    public float BossHealthMultiplier = 5f;

    [Header("쫄몹 생성 설정")]
    [Tooltip("쫄몹 생성 체력 비율 임계값 (0.0 ~ 1.0)")]
    [Range(0.0f, 1.0f)]
    public float MinionSpawnHealthThreshold = 0.5f;

    [Tooltip("생성할 쫄몹 프리팹")]
    public GameObject MinionPrefab;

    [Tooltip("한 번에 생성할 쫄몹 수")]
    public int MinionsPerSpawn = 3;

    [Tooltip("쫄몹 생성 간격 (초)")]
    public float MinionSpawnInterval = 10f;

    [Tooltip("쫄몹 생성 위치 (여러 개 지정 가능)")]
    public Transform[] MinionSpawnPoints;

    [Tooltip("쫄몹 생성 VFX")]
    public GameObject MinionSpawnVFX;

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

    [Header("공격 패턴")]
    [Tooltip("사용할 공격 패턴 목록")]
    public BossAttackPattern[] AttackPatterns;

    [Tooltip("패턴 쿨다운 (초)")]
    public float PatternCooldown = 3f;

    [Tooltip("보스 애니메이터")]
    public Animator BossAnimator;

    // 내부 상태
    private bool m_IsExecutingPattern = false;
    private bool m_IsSpawningMinions = false;
    private float m_LastPatternTime = -999f;
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

        // 애니메이터 자동 찾기
        if (BossAnimator == null)
        {
            BossAnimator = GetComponent<Animator>();
            if (BossAnimator == null)
                BossAnimator = GetComponentInChildren<Animator>();
        }

        // 패턴 초기화
        if (AttackPatterns != null)
        {
            foreach (var pattern in AttackPatterns)
            {
                if (pattern != null)
                {
                    pattern.Target = null; // Update에서 설정
                    pattern.BossTransform = transform;
                }
            }
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

        // 패턴 타겟 업데이트
        if (AttackPatterns != null)
        {
            foreach (var pattern in AttackPatterns)
            {
                if (pattern != null)
                {
                    pattern.Target = m_EnemyController.KnownDetectedTarget.transform;
                }
            }
        }

        // 보스 체력 기반 상태 전환 (쫄몹 소환)
        if (IsBoss)
        {
            CheckHealthBasedPatterns();
        }
    }

    protected override void UpdateCurrentAiState()
    {
        base.UpdateCurrentAiState();
    }

    protected override void HandleStateAttack()
    {
        if (m_EnemyController.KnownDetectedTarget == null)
            return;

        // 타겟 방향 조준
        m_EnemyController.OrientTowards(m_EnemyController.KnownDetectedTarget.transform.position);

        // 제자리 정지
        m_EnemyController.SetNavDestination(transform.position);

        // 공격 패턴 실행
        if (AttackPatterns != null && AttackPatterns.Length > 0)
        {
            ExecuteAttackPattern();
        }
    }

    /// <summary>
    /// 공격 패턴 실행
    /// </summary>
    private void ExecuteAttackPattern()
    {
        // 패턴 실행 중이거나 쿨다운 중이면 스킵
        if (m_IsExecutingPattern || m_IsSpawningMinions)
            return;

        if (Time.time < m_LastPatternTime + PatternCooldown)
            return;

        // 사용 가능한 패턴 찾기
        List<BossAttackPattern> availablePatterns = new List<BossAttackPattern>();
        foreach (var pattern in AttackPatterns)
        {
            if (pattern != null && pattern.CanExecute())
            {
                availablePatterns.Add(pattern);
            }
        }

        if (availablePatterns.Count > 0)
        {
            // 랜덤 패턴 선택
            BossAttackPattern selectedPattern = availablePatterns[Random.Range(0, availablePatterns.Count)];
            
            m_IsExecutingPattern = true;
            m_LastPatternTime = Time.time;
            
            // 디버그 로그
            Debug.Log($"<color=cyan>[BossMelee] 패턴 시작: {selectedPattern.GetType().Name}</color>");
            
            StartCoroutine(ExecutePatternWithCallback(selectedPattern));
        }
    }

    private IEnumerator ExecutePatternWithCallback(BossAttackPattern pattern)
    {
        // NavMeshAgent 비활성화 (이동 방지)
        if (m_EnemyController != null && m_EnemyController.NavMeshAgent != null)
        {
            m_EnemyController.NavMeshAgent.isStopped = true;
        }

        pattern.StartPattern();

        // 패턴이 완료될 때까지 대기
        while (pattern.IsExecuting)
        {
            yield return null;
        }

        // 디버그 로그
        Debug.Log($"<color=green>[BossMelee] 패턴 완료: {pattern.GetType().Name}</color>");

        // NavMeshAgent 재활성화
        if (m_EnemyController != null && m_EnemyController.NavMeshAgent != null)
        {
            m_EnemyController.NavMeshAgent.isStopped = false;
        }

        m_IsExecutingPattern = false;
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
        m_IsSpawningMinions = true;

        // 보스 특수 애니메이션 또는 효과
        if (m_Animator != null)
        {
            m_Animator.SetTrigger("SpawnMinions");
            m_Animator.SetFloat("MoveSpeed", 0f);
        }

        // 생성 사운드 재생
        if (MinionSpawnSFX)
        {
            AudioUtility.CreateSFX(MinionSpawnSFX, transform.position, AudioUtility.AudioGroups.EnemyAttack, 1f);
        }

        // 약간의 지연 시간
        yield return new WaitForSeconds(1.5f);

        // 쫄몹 생성
        for (int i = 0; i < MinionsPerSpawn; i++)
        {
            SpawnMinion();
            // 각 생성 사이에 약간의 지연
            yield return new WaitForSeconds(0.2f);
        }

        m_IsSpawningMinions = false;
    }

    private void SpawnMinion()
    {
        if (MinionPrefab == null)
            return;

        Vector3 spawnPosition;
        Quaternion spawnRotation;

        if (MinionSpawnPoints.Length == 0)
        {
            // 스폰 포인트가 없으면 보스 주변 랜덤 위치에 생성
            float spawnRadius = 5f;
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            spawnPosition = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            spawnRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        }
        else
        {
            // 랜덤 생성 위치 선택
            Transform spawnPoint = MinionSpawnPoints[Random.Range(0, MinionSpawnPoints.Length)];
            spawnPosition = spawnPoint.position;
            spawnRotation = spawnPoint.rotation;
        }

        // 생성 효과 표시
        if (MinionSpawnVFX)
        {
            Instantiate(MinionSpawnVFX, spawnPosition, Quaternion.identity);
        }

        // 쫄몹 생성
        GameObject minion = Instantiate(MinionPrefab, spawnPosition, spawnRotation);

        // 생성된 쫄몹 추적
        m_SpawnedMinions.Add(minion);

        // EnemyManager에 등록되도록 확인
        EnemyMobile enemyMobile = minion.GetComponent<EnemyMobile>();
        if (enemyMobile != null)
        {
            EnemyController enemyController = minion.GetComponent<EnemyController>();
            if (enemyController != null && m_EnemyController.KnownDetectedTarget != null)
            {
                // 필요시 타겟 정보 전달
            }
        }
    }

    /// <summary>
    /// 보스 죽음 처리
    /// </summary>
    private void OnBossDie()
    {
        Debug.Log("<color=red>[BossMelee] 보스 사망</color>");

        // VFX
        if (BossDeathVFX != null)
        {
            GameObject vfx = Instantiate(BossDeathVFX, transform.position, Quaternion.identity);
            Destroy(vfx, 5f);
        }

        // 사운드
        if (BossDeathSFX)
        {
            AudioUtility.CreateSFX(BossDeathSFX, transform.position, AudioUtility.AudioGroups.EnemyAttack, 1f);
        }

        // 폭발 데미지
        Collider[] hits = Physics.OverlapSphere(transform.position, BossDeathExplosionRadius);
        foreach (var hit in hits)
        {
            Damageable damageable = hit.GetComponent<Damageable>();
            if (damageable != null && hit.gameObject != gameObject)
            {
                damageable.InflictDamage(BossDeathExplosionDamage, false, gameObject);
            }
        }

        // 생성한 쫄몹 제거
        foreach (var minion in m_SpawnedMinions)
        {
            if (minion != null)
            {
                Destroy(minion);
            }
        }
    }

    /// <summary>
    /// 쫄몹 생성 위치 계산
    /// </summary>
    private List<Transform> GetMinionSpawnPositions(Transform center, float radius, int count, LayerMask obstacles)
    {
        List<Transform> positions = new List<Transform>();

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Vector3 position = center.position + offset;

            // 장애물 체크
            if (!Physics.CheckSphere(position, 1f, obstacles))
            {
                GameObject spawnPoint = new GameObject($"MinionSpawnPoint_{i}");
                spawnPoint.transform.position = position;
                spawnPoint.transform.parent = center;
                positions.Add(spawnPoint.transform);
            }
        }

        return positions;
    }
}