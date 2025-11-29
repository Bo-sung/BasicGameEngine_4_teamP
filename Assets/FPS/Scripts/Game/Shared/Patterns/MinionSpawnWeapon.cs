using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 쫄몹을 소환하는 BossPatternBase
/// 체력 임계값 또는 공격 명령으로 쫄몹 소환
/// </summary>
public class MinionSpawnWeapon : BossPatternBase
{
    [Header("쫄몹 소환 설정")]
    [Tooltip("생성할 쫄몹 프리팹")]
    public GameObject MinionPrefab;

    [Tooltip("한 번에 생성할 쫄몹 수")]
    public int MinionsPerSpawn = 3;

    [Tooltip("쫄몹 생성 위치")]
    public Transform[] MinionSpawnPoints;

    [Tooltip("쫄몹 생성 사운드")]
    public AudioClip MinionSpawnSFX;

    [Header("소환 조건")]
    [Tooltip("체력 기반 자동 소환 활성화")]
    public bool UseHealthThreshold = true;

    [Tooltip("체력 비율 임계값 (체력이 이 이하일 때 자동 소환)")]
    [Range(0.0f, 1.0f)]
    public float HealthThreshold = 0.5f;

    [Tooltip("소환 전 대기 시간")]
    public float PreSpawnDelay = 1.5f;

    private Health m_OwnerHealth;
    private bool m_HasSpawnedMinions = false;
    private float m_SpawnHealthThresholdValue = 0f;
    private List<GameObject> m_SpawnedMinions = new List<GameObject>();
    private EnemyController m_OwnerController;

    protected override void Start()
    {
        base.Start();

        if (Owner != null)
        {
            m_OwnerHealth = Owner.GetComponent<Health>();
            m_OwnerController = Owner.GetComponent<EnemyController>();

            if (m_OwnerHealth != null && UseHealthThreshold)
            {
                m_SpawnHealthThresholdValue = m_OwnerHealth.MaxHealth * HealthThreshold;
            }
        }

        // 쫄몹 생성 위치 자동 설정
        if (MinionSpawnPoints.Length == 0)
        {
            LayerMask obstacles = LayerMask.GetMask("Obstacle", "Wall");
            MinionSpawnPoints = GetMinionSpawnPositions(Owner.transform, 10f, MinionsPerSpawn, obstacles).ToArray();
        }
    }

    protected override void OnEnable()
    {
        // 프레임마다 체력 임계값 확인
        if (UseHealthThreshold && m_OwnerHealth != null && !m_HasSpawnedMinions)
        {
            // Update에서 체크하기 위해 LateUpdate 사용
        }
    }

    void LateUpdate()
    {
        // 체력 기반 자동 소환
        if (UseHealthThreshold && m_OwnerHealth != null && !m_HasSpawnedMinions)
        {
            if (m_OwnerHealth.CurrentHealth <= m_SpawnHealthThresholdValue)
            {
                // 자동으로 공격 실행 (쿨타임 확인)
                if (Time.time >= m_LastFireTime + FireRate)
                {
                    HandleShootInputs(false, true, false);
                }
            }
        }
    }

    /// <summary>
    /// 쫄몹 소환 패턴 실행
    /// </summary>
    protected override bool ExecutePattern()
    {
        if (MinionPrefab == null || MinionSpawnPoints.Length == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] MinionPrefab or SpawnPoints not configured!", gameObject);
            return false;
        }

        if (m_HasSpawnedMinions)
        {
            Debug.LogWarning($"[{gameObject.name}] Minions already spawned!", gameObject);
            return false;
        }

        // 쫄몹 소환 코루틴 시작
        StartCoroutine(SpawnMinionsCoroutine());
        return true;
    }

    /// <summary>
    /// 쫄몹 소환 코루틴
    /// </summary>
    private IEnumerator SpawnMinionsCoroutine()
    {
        m_HasSpawnedMinions = true;

        // 소환 전 대기
        yield return new WaitForSeconds(PreSpawnDelay);

        // 쫄몹 생성 사운드 재생
        if (MinionSpawnSFX && Owner != null)
        {
            AudioUtility.CreateSFX(MinionSpawnSFX, Owner.transform.position, AudioUtility.AudioGroups.EnemyAttack, 1f);
        }

        // 쫄몹 생성
        for (int i = 0; i < MinionsPerSpawn; i++)
        {
            SpawnMinion();
            yield return new WaitForSeconds(0.2f);
        }
    }

    /// <summary>
    /// 개별 쫄몹 생성
    /// </summary>
    private void SpawnMinion()
    {
        if (MinionPrefab == null || MinionSpawnPoints.Length == 0)
            return;

        Transform spawnPoint = MinionSpawnPoints[Random.Range(0, MinionSpawnPoints.Length)];
        GameObject minion = Instantiate(MinionPrefab, spawnPoint.position, spawnPoint.rotation);
        m_SpawnedMinions.Add(minion);

        // 플레이어 타겟 전달
        if (m_OwnerController != null && m_OwnerController.KnownDetectedTarget != null)
        {
            EnemyController minionController = minion.GetComponent<EnemyController>();
            if (minionController != null)
            {
                DetectionModule minionDetection = minionController.DetectionModule;
                if (minionDetection != null)
                {
                    minionDetection.OnDamaged(m_OwnerController.KnownDetectedTarget);
                }
                else
                {
                    Debug.LogError($"[{minion.name}] DetectionModule not found!", minion);
                }
            }
        }
    }

    /// <summary>
    /// 쫄몹 생성 위치 자동 계산
    /// </summary>
    private List<Transform> GetMinionSpawnPositions(Transform bossTransform, float radius, int minionCount, LayerMask obstacleLayer)
    {
        List<Transform> spawnPositions = new List<Transform>();

        if (bossTransform == null)
            return spawnPositions;

        float minionRadius = 0.5f;

        for (int i = 0; i < minionCount; i++)
        {
            bool validPositionFound = false;
            Vector3 spawnPos = Vector3.zero;

            for (int attempt = 0; attempt < 30 && !validPositionFound; attempt++)
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
    /// 모든 생성된 쫄몹 처리
    /// </summary>
    public void ClearSpawnedMinions()
    {
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
        m_SpawnedMinions.Clear();
    }

    /// <summary>
    /// 생성된 쫄몹 개수 반환
    /// </summary>
    public int GetSpawnedMinionCount()
    {
        return m_SpawnedMinions.Count;
    }

    /// <summary>
    /// 공격 애니메이션 재생
    /// </summary>
    protected override void PlayAttackAnimation()
    {
        if (m_OwnerAnimator != null)
        {
            m_OwnerAnimator.SetTrigger("Summon");  // "Attack" 대신 "Summon"
        }
    }

    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    public override void DebugPrintInfo()
    {
        base.DebugPrintInfo();
        Debug.Log($"생성된 쫄몹: {GetSpawnedMinionCount()}/{MinionsPerSpawn}\n" +
            $"임계값: {HealthThreshold:P}\n" +
            $"임계값 체력: {m_SpawnHealthThresholdValue:F0}");
    }

    protected override void OnDrawGizmosSelected()
    {
        // 쫄몹 생성 위치 시각화
        Gizmos.color = Color.green;
        if (MinionSpawnPoints != null)
        {
            foreach (Transform spawnPoint in MinionSpawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                    Gizmos.DrawLine(Owner != null ? Owner.transform.position : transform.position, spawnPoint.position);
                }
            }
        }
    }
}