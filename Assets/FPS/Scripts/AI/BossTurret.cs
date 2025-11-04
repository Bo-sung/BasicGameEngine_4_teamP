using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.AI
{
    public class BossTurret : EnemyTurret
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

            // 이미 쫄몹을 소환했고 소환 간격이 지났다면 추가 쫄몹 소환 
            if (m_HasSpawnedMinions && Time.time >= m_LastMinionSpawnTime + MinionSpawnInterval)
            {
                StartCoroutine(SpawnMinionsSequence());
            }
        }

        private IEnumerator SpawnMinionsSequence()
        {
            // 보스가 잠시 취약 상태가 되거나 공격을 멈추는 등의 효과 가능
            // 상태를 직접 수정하는 대신 이벤트를 활용
            // 공격을 일시 중지하기 위한 로직이 필요하면 다른 방법을 사용

            // 보스 특수 애니메이션 또는 효과
            if (m_Animator != null)
            {
                m_Animator.SetTrigger("SpawnMinions");
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

        private void SpawnMinion()
        {
            if (MinionPrefab == null || MinionSpawnPoints.Length == 0)
                return;

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
                // 즉시 플레이어를 감지하도록 하려면 DetectionModule의 타겟 설정 필요
                // 직접 OnDetectedTarget 호출 대신 KnownDetectedTarget 설정
                EnemyController enemyController = minion.GetComponent<EnemyController>();
                if (enemyController != null && m_EnemyController.KnownDetectedTarget != null)
                {
                    // 타겟 정보 전달 - DetectionModule이 자동으로 처리하도록 해야 함
                    // 이 부분은 게임 구조에 따라 조정 필요
                    // 예: 플레이어를 감지하도록 위치 설정
                    enemyController.DetectionModule.HandleTargetDetection(
                        m_EnemyController.KnownDetectedTarget.GetComponent<Actor>(),
                        enemyController.GetComponentsInChildren<Collider>());
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
                        // isExplosionDamage 매개변수 추가
                        damageable.InflictDamage(BossDeathExplosionDamage, true, gameObject);
                    }
                }
            }

            // 생존한 쫄몹들 처리 (선택적)
            foreach (GameObject minion in m_SpawnedMinions)
            {
                if (minion != null)
                {
                    Health minionHealth = minion.GetComponent<Health>();
                    if (minionHealth != null)
                    {
                        // 쫄몹들도 즉시 제거하거나 약화시킬 수 있음
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
}