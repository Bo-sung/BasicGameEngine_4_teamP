using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossMobile : EnemyMobile
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

    [Tooltip("쫄몹 생성 간격")]
    public float MinionSpawnInterval = 10f;

    [Tooltip("쫄몹 생성 위치 (여러 개 지정 가능)")]
    public Transform[] MinionSpawnPoints;

    [Tooltip("쫄몹 생성 효과")]
    public GameObject MinionSpawnVFX;

    [Tooltip("쫄몹 생성 사운드")]
    public AudioClip MinionSpawnSFX;

    [Header("보스 죽음 효과")]
    [Tooltip("보스 죽음 추가 VFX")]
    public GameObject BossDeathVFX;

    [Tooltip("보스 죽음 사운드")]
    public AudioClip BossDeathSFX;

    [Tooltip("보스 죽음 폭발 반경")]
    public float BossDeathExplosionRadius = 10f;

    [Tooltip("보스 죽음 폭발 데미지")]
    public float BossDeathExplosionDamage = 50f;

    [Header("공격 패턴 설정")]
    [Tooltip("사용할 공격 패턴 배열")]
    public BossAttackPattern[] AttackPatterns;

    [Tooltip("패턴 전환 쿨다운")]
    public float PatternCooldown = 3f;

    [Tooltip("패턴 사용 시 일반 무기 비활성화")]
    public bool DisableWeaponDuringPattern = true;

    // 쫄몹 생성 관련 변수
    private bool m_HasSpawnedMinions = false;
    private float m_LastMinionSpawnTime = 0f;
    private float m_OriginalMaxHealth;
    private List<GameObject> m_SpawnedMinions = new List<GameObject>();

    // 스케일링된 보스 값
    private float m_SpawnHealthThresholdValue;

    // 공격 패턴 관련 변수
    private int m_CurrentPatternIndex = 0;
    private float m_LastPatternTime = -999f;
    private bool m_IsExecutingPattern = false;
    private bool m_IsSpawningMinions = false;

    [Tooltip("보스 애니메이터 (인스펙터에서 할당)")]
    public Animator BossAnimator;

    protected override void SetupAnimator()
    {
        if (BossAnimator != null)
        {
            m_Animator = BossAnimator;
        }
        else
        {
            m_Animator = GetComponentInChildren<Animator>();
        }
    }

    protected override void Start()
    {
        base.Start();

        // 보스용 체력 조정
        if (IsBoss)
        {
            m_OriginalMaxHealth = m_Health.MaxHealth;
            m_Health.MaxHealth *= BossHealthMultiplier;
            m_Health.CurrentHealth = m_Health.MaxHealth;

            // 쫄몹 생성 체력 임계값 계산
            m_SpawnHealthThresholdValue = m_Health.MaxHealth * MinionSpawnHealthThreshold;

            // 죽음 이벤트 핸들러 추가
            m_Health.OnDie += OnBossDie;
        }

        if (MinionSpawnPoints.Length <= 0)
        {
            LayerMask obstacles = LayerMask.GetMask("Obstacle", "Wall"); // 장애물 레이어
            MinionSpawnPoints = GetMinionSpawnPositions(transform, 10f, MinionsPerSpawn, obstacles).ToArray();
        }

        // 공격 패턴 초기화
        InitializeAttackPatterns();
    }

    private void InitializeAttackPatterns()
    {
        if (AttackPatterns == null || AttackPatterns.Length == 0) return;

        // 각 패턴에 타겟 설정
        foreach (var pattern in AttackPatterns)
        {
            if (pattern != null)
            {
                pattern.BossTransform = transform;
            }
        }
    }

    protected override void Update()
    {
        base.Update();

        // 보스 체력 기반 상태 전환
        if (IsBoss)
        {
            CheckHealthBasedPatterns();
        }
    }

    private void CheckHealthBasedPatterns()
    {
        // 체력이 임계값 이하이고 아직 쫄몹을 소환하지 않았다면
        if (m_Health.CurrentHealth <= m_SpawnHealthThresholdValue && !m_HasSpawnedMinions)
        {
            // 쫄몹 생성
            StartCoroutine(SpawnMinionsSequence());
            m_HasSpawnedMinions = true;
        }
    }

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

        // 마지막 생성 시간 업데이트
        m_LastMinionSpawnTime = Time.time;
        m_IsSpawningMinions = false;
    }

    public List<Transform> GetMinionSpawnPositions(Transform bossTransform, float radius, int minionCount, LayerMask obstacleLayer)
    {
        List<Transform> spawnPositions = new List<Transform>();

        if (bossTransform == null)
        {
            Debug.LogWarning("보스 Transform이 null입니다.");
            return spawnPositions;
        }

        // 미니언의 크기를 고려한 충돌 검사 반경
        float minionRadius = 0.5f;

        for (int i = 0; i < minionCount; i++)
        {
            bool validPositionFound = false;
            Vector3 spawnPos = Vector3.zero;
            int maxAttempts = 30;

            for (int attempt = 0; attempt < maxAttempts && !validPositionFound; attempt++)
            {
                // 균등 분포를 위한 무작위 방향 및 거리 계산
                Vector3 randomDirection = Random.insideUnitSphere;
                randomDirection.y = 0;
                randomDirection.Normalize();

                // 균등 분포를 위해 제곱근 사용
                float randomDistance = radius * Mathf.Sqrt(Random.value);

                // 후보 위치 계산
                spawnPos = bossTransform.position + randomDirection * randomDistance;

                // 위치 유효성 검사
                validPositionFound = !Physics.CheckSphere(spawnPos, minionRadius, obstacleLayer);
            }

            if (validPositionFound)
            {
                // 유효한 위치에 빈 게임오브젝트 생성
                GameObject spawnMarker = new GameObject($"MinionSpawn_{i}");
                spawnMarker.transform.position = spawnPos;

                // 보스를 향해 방향 설정
                spawnMarker.transform.LookAt(new Vector3(bossTransform.position.x, spawnMarker.transform.position.y, bossTransform.position.z));

                spawnPositions.Add(spawnMarker.transform);
            }
        }

        return spawnPositions;
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
    /// AI 상태별 행동 오버라이드 (공격 패턴 시스템 사용)
    /// </summary>
    protected override void UpdateCurrentAiState()
    {
        base.UpdateCurrentAiState();
    }


    protected override void HandleStateAttack()
    {
        Debug.Log($"{this.gameObject.name} BossMobile HandleStateAttack");
        // Attack 상태: 패턴 기반 공격
        if (m_EnemyController.KnownDetectedTarget == null)
            return;

        // 타겟 방향 조준
        m_EnemyController.OrientTowards(m_EnemyController.KnownDetectedTarget.transform.position);
        m_EnemyController.OrientWeaponsTowards(m_EnemyController.KnownDetectedTarget.transform.position);

        // 제자리 정지 (원거리 보스)
        m_EnemyController.SetNavDestination(transform.position);

        // 공격 패턴 실행 (보스는 패턴만 사용)
        if (AttackPatterns != null && AttackPatterns.Length > 0)
        {
            ExecuteAttackPattern();
        }
        // 패턴이 없으면 아무것도 하지 않음 (보스는 기본 공격 없음)
    }
    /// <summary>
    /// 공격 패턴 실행
    /// </summary>
    private void ExecuteAttackPattern()
    {
        // 이미 패턴 실행 중이면 대기
        if (m_IsExecutingPattern || m_IsSpawningMinions) return;

        // 쿨다운 확인
        if (Time.time < m_LastPatternTime + PatternCooldown) return;

        // 타겟 설정 (매 프레임 업데이트)
        foreach (var pattern in AttackPatterns)
        {
            if (pattern != null)
            {
                pattern.Target = m_EnemyController.KnownDetectedTarget.transform;
            }
        }

        // 현재 패턴 가져오기
        BossAttackPattern currentPattern = AttackPatterns[m_CurrentPatternIndex];
        
        if (currentPattern != null && currentPattern.CanExecute())
        {
            m_IsExecutingPattern = true;
            m_LastPatternTime = Time.time;

            // 패턴 시작
            currentPattern.StartPattern();

            // 다음 패턴으로 전환
            m_CurrentPatternIndex = (m_CurrentPatternIndex + 1) % AttackPatterns.Length;

            // 패턴 완료 대기 코루틴
            StartCoroutine(WaitForPatternComplete(currentPattern));
        }
    }

    /// <summary>
    /// 패턴 완료 대기
    /// </summary>
    private IEnumerator WaitForPatternComplete(BossAttackPattern pattern)
    {
        // 패턴이 완료될 때까지 대기
        while (pattern.IsExecuting)
        {
            yield return null;
        }

        m_IsExecutingPattern = false;
    }

    // 보스 사망 처리
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

        // 폭발 효과 - 주변 오브젝트에 데미지
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, BossDeathExplosionRadius);
        foreach (Collider hitCollider in hitColliders)
        {
            Damageable damageable = hitCollider.GetComponent<Damageable>();
            if (damageable != null)
            {
                // 보스 자신은 제외
                if (hitCollider.gameObject != gameObject)
                {
                    damageable.InflictDamage(BossDeathExplosionDamage, true, gameObject);
                }
            }
        }

        // 생존한 쫄몹들 처리
        foreach (GameObject minion in m_SpawnedMinions)
        {
            if (minion != null)
            {
                Health minionHealth = minion.GetComponent<Health>();
                if (minionHealth != null)
                {
                    minionHealth.Kill();
                }
            }
        }

        // 게임 승리 조건 트리거
        AllObjectivesCompletedEvent victoryEvent = Events.AllObjectivesCompletedEvent;
        EventManager.Broadcast(victoryEvent);
    }

    // 기즈모 표시
    void OnDrawGizmos()
    {
        // 보스 죽음 폭발 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, BossDeathExplosionRadius);

        // 쫄몹 생성 위치 표시
        Gizmos.color = Color.green;
        if (MinionSpawnPoints != null)
        {
            foreach (Transform spawnPoint in MinionSpawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                    Gizmos.DrawLine(transform.position, spawnPoint.position);
                }
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
