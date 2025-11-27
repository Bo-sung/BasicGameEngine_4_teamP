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

    // 쫄몹 생성 관련 변수
    private bool m_HasSpawnedMinions = false;
    private float m_LastMinionSpawnTime = 0f;
    private float m_OriginalMaxHealth;
    private List<GameObject> m_SpawnedMinions = new List<GameObject>();

    // 스케일링된 보스 값
    private float m_SpawnHealthThresholdValue;

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
        // 보스 특수 애니메이션 또는 효과
        if (m_Animator != null)
        {
            // 애니메이터에 SpawnMinions 트리거가 있다면 사용
            // m_Animator.SetTrigger("SpawnMinions");
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

        if (MinionSpawnPoints.Length == 0)
        {
            // 스폰 포인트가 없으면 현재 위치 주변에 생성 시도
            return;
        }

        // 랜덤 생성 위치 선택
        Transform spawnPoint = MinionSpawnPoints[Random.Range(0, MinionSpawnPoints.Length)];

        // 생성 효과 표시
        if (MinionSpawnVFX)
        {
            Instantiate(MinionSpawnVFX, spawnPoint.position, Quaternion.identity);
        }

        // 쫄몹 생성
        GameObject minion = Instantiate(MinionPrefab, spawnPoint.position, spawnPoint.rotation);

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
