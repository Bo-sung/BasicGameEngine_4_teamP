using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent (typeof (Actor), typeof(Health))]
public class EnemyController : MonoBehaviour
{
    [System.Serializable]
    public struct RendererIndexData
    {
        public Renderer Renderer;
        public int MaterialIndex;

        public RendererIndexData(Renderer renderer, int index)
        {
            Renderer = renderer;
            MaterialIndex = index;
        }
    }

    [Header("매개변수")]
    // The Y height at which the enemy will be automatically killed (if it falls off of the level)
    [Tooltip("적이 자동으로 죽게 되는 Y 높이 (레벨에서 떨어졌을 때)")]
    public float SelfDestructYHeight = -20f;

    // The distance at which the enemy considers that it has reached its current path destination point
    [Tooltip("적이 현재 경로 목적지 지점에 도달했다고 간주하는 거리")]
    public float PathReachingRadius = 2f;

    // The speed at which the enemy rotates
    [Tooltip("적이 회전하는 속도")]
    public float OrientationSpeed = 10f;

    // Delay after death where the GameObject is destroyed (to allow for animation)
    [Tooltip("죽은 후 GameObject가 파괴되기까지의 지연 시간 (애니메이션을 위해)")]
    public float DeathDuration = 0f;

    [Header("무기 매개변수")]
    // Allow weapon swapping for this enemy
    [Tooltip("이 적의 무기 교체 허용")]
    public bool SwapToNextWeapon = false;

    // Time delay between a weapon swap and the next attack
    [Tooltip("무기 교체와 다음 공격 사이의 시간 지연")]
    public float DelayAfterWeaponSwap = 0f;

    [Header("눈 색상")]
    // Material for the eye color
    [Tooltip("눈 색상용 머티리얼")]
    public Material EyeColorMaterial;

    // The default color of the bot's eye
    [Tooltip("봇의 눈 기본 색상")]
    [ColorUsageAttribute(true, true)]
    public Color DefaultEyeColor;

    // The attack color of the bot's eye
    [Tooltip("봇의 눈 공격 색상")]
    [ColorUsageAttribute(true, true)]
    public Color AttackEyeColor;

    [Header("피격 시 섬광")]
    // The material used for the body of the hoverbot
    [Tooltip("호버봇의 몸체에 사용되는 머티리얼")]
    public Material BodyMaterial;

    // The gradient representing the color of the flash on hit
    [Tooltip("피격 시 섬광의 색상을 나타내는 그래디언트")]
    [GradientUsageAttribute(true)]
    public Gradient OnHitBodyGradient;

    // The duration of the flash on hit
    [Tooltip("피격 시 섬광의 지속 시간")]
    public float FlashOnHitDuration = 0.5f;

    [Header("사운드")]
    // Sound played when recieving damages
    [Tooltip("데미지를 받을 때 재생되는 사운드")]
    public AudioClip DamageTick;

    [Header("VFX")]
    // The VFX prefab spawned when the enemy dies
    [Tooltip("적이 죽을 때 생성되는 VFX 프리팹")]
    public GameObject DeathVfx;

    // The point at which the death VFX is spawned
    [Tooltip("죽음 VFX가 생성되는 지점")]
    public Transform DeathVfxSpawnPoint;

    [Header("전리품")]
    // The object this enemy can drop when dying
    [Tooltip("이 적이 죽을 때 드롭할 수 있는 오브젝트")]
    public GameObject LootPrefab;

    // The chance the object has to drop
    [Tooltip("오브젝트가 드롭될 확률")]
    [Range(0, 1)]
    public float DropRate = 1f;

    [Header("디버그 표시")]
    // Color of the sphere gizmo representing the path reaching range
    [Tooltip("경로 도달 범위를 나타내는 구체 기즈모의 색상")]
    public Color PathReachingRangeColor = Color.yellow;

    // Color of the sphere gizmo representing the attack range
    [Tooltip("공격 범위를 나타내는 구체 기즈모의 색상")]
    public Color AttackRangeColor = Color.red;

    // Color of the sphere gizmo representing the detection range
    [Tooltip("탐지 범위를 나타내는 구체 기즈모의 색상")]
    public Color DetectionRangeColor = Color.blue;

    public UnityAction onAttack;
    public UnityAction onDetectedTarget;
    public UnityAction onLostTarget;
    public UnityAction onDamaged;

    List<RendererIndexData> m_BodyRenderers = new List<RendererIndexData>();
    MaterialPropertyBlock m_BodyFlashMaterialPropertyBlock;
    float m_LastTimeDamaged = float.NegativeInfinity;

    RendererIndexData m_EyeRendererData;
    MaterialPropertyBlock m_EyeColorMaterialPropertyBlock;

    public PatrolPath PatrolPath { get; set; }
    public GameObject KnownDetectedTarget => DetectionModule.KnownDetectedTarget;
    public bool IsTargetInAttackRange => DetectionModule.IsTargetInAttackRange;
    public bool IsSeeingTarget => DetectionModule.IsSeeingTarget;
    public bool HadKnownTarget => DetectionModule.HadKnownTarget;
    public NavMeshAgent NavMeshAgent { get; private set; }
    public DetectionModule DetectionModule { get; private set; }

    int m_PathDestinationNodeIndex;
    EnemyManager m_EnemyManager;
    ActorsManager m_ActorsManager;
    Health m_Health;
    Actor m_Actor;
    Collider[] m_SelfColliders;
    GameFlowManager m_GameFlowManager;
    bool m_WasDamagedThisFrame;
    float m_LastTimeWeaponSwapped = Mathf.NegativeInfinity;
    int m_CurrentWeaponIndex;
    WeaponController m_CurrentWeapon;
    WeaponController[] m_Weapons;
    NavigationModule m_NavigationModule;

    void Start()
    {
        m_EnemyManager = FindAnyObjectByType<EnemyManager>();
        DebugUtility.HandleErrorIfNullFindObject<EnemyManager, EnemyController>(m_EnemyManager, this);

        m_ActorsManager = FindAnyObjectByType<ActorsManager>();
        DebugUtility.HandleErrorIfNullFindObject<ActorsManager, EnemyController>(m_ActorsManager, this);

        m_EnemyManager.RegisterEnemy(this);

        m_Health = GetComponent<Health>();
        DebugUtility.HandleErrorIfNullGetComponent<Health, EnemyController>(m_Health, this, gameObject);

        m_Actor = GetComponent<Actor>();
        DebugUtility.HandleErrorIfNullGetComponent<Actor, EnemyController>(m_Actor, this, gameObject);

        NavMeshAgent = GetComponent<NavMeshAgent>();
        m_SelfColliders = GetComponentsInChildren<Collider>();

        m_GameFlowManager = FindAnyObjectByType<GameFlowManager>();
        DebugUtility.HandleErrorIfNullFindObject<GameFlowManager, EnemyController>(m_GameFlowManager, this);

        // 데미지 및 죽음 액션에 구독
        m_Health.OnDie += OnDie;
        m_Health.OnDamaged += OnDamaged;


        var detectionModules = GetComponentsInChildren<DetectionModule>();
        DebugUtility.HandleErrorIfNoComponentFound<DetectionModule, EnemyController>(detectionModules.Length, this, gameObject);
        DebugUtility.HandleWarningIfDuplicateObjects<DetectionModule, EnemyController>(detectionModules.Length, this, gameObject);


        // 탐지 모듈 초기화
        DetectionModule = detectionModules[0];
        DetectionModule.onDetectedTarget += OnDetectedTarget;
        DetectionModule.onLostTarget += OnLostTarget;
        onAttack += DetectionModule.OnAttack;

        // 모든 무기 찾기 및 초기화
        FindAndInitializeAllWeapons();
        var weapon = GetCurrentWeapon();
        weapon.ShowWeapon(true);

        var navigationModules = GetComponentsInChildren<NavigationModule>();
        DebugUtility.HandleWarningIfDuplicateObjects<DetectionModule, EnemyController>(detectionModules.Length, this, gameObject);
        // 네비메시 에이전트 데이터 재정의
        if (navigationModules.Length > 0)
        {
            m_NavigationModule = navigationModules[0];
            NavMeshAgent.speed = m_NavigationModule.MoveSpeed;
            NavMeshAgent.angularSpeed = m_NavigationModule.AngularSpeed;
            NavMeshAgent.acceleration = m_NavigationModule.Acceleration;
        }

        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                if (renderer.sharedMaterials[i] == EyeColorMaterial)
                {
                    m_EyeRendererData = new RendererIndexData(renderer, i);
                }

                if (renderer.sharedMaterials[i] == BodyMaterial)
                {
                    m_BodyRenderers.Add(new RendererIndexData(renderer, i));
                }
            }
        }

        m_BodyFlashMaterialPropertyBlock = new MaterialPropertyBlock();

        // 이 적에 대한 눈 렌더러가 있는지 확인
        if (m_EyeRendererData.Renderer != null)
        {
            m_EyeColorMaterialPropertyBlock = new MaterialPropertyBlock();
            m_EyeColorMaterialPropertyBlock.SetColor("_EmissionColor", DefaultEyeColor);
            m_EyeRendererData.Renderer.SetPropertyBlock(m_EyeColorMaterialPropertyBlock, m_EyeRendererData.MaterialIndex);
        }
    }

    void Update()
    {
        EnsureIsWithinLevelBounds();

        DetectionModule.HandleTargetDetection(m_Actor, m_SelfColliders);

        Color currentColor = OnHitBodyGradient.Evaluate((Time.time - m_LastTimeDamaged) / FlashOnHitDuration);
        m_BodyFlashMaterialPropertyBlock.SetColor("_EmissionColor", currentColor);
        foreach (var data in m_BodyRenderers)
        {
            data.Renderer.SetPropertyBlock(m_BodyFlashMaterialPropertyBlock, data.MaterialIndex);
        }

        m_WasDamagedThisFrame = false;
    }

    void EnsureIsWithinLevelBounds()
    {
        // 매 프레임마다 적을 죽일 조건들을 테스트
        if (transform.position.y < SelfDestructYHeight)
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnLostTarget()
    {
        onLostTarget.Invoke();

        // 눈 렌더러가 설정되어 있다면 눈 공격 색상과 프로퍼티 블록 설정
        if (m_EyeRendererData.Renderer != null)
        {
            m_EyeColorMaterialPropertyBlock.SetColor("_EmissionColor", DefaultEyeColor);
            m_EyeRendererData.Renderer.SetPropertyBlock(m_EyeColorMaterialPropertyBlock, m_EyeRendererData.MaterialIndex);
        }
    }

    void OnDetectedTarget()
    {
        onDetectedTarget.Invoke();

        // 눈 렌더러가 설정되어 있다면 눈 기본 색상과 프로퍼티 블록 설정
        if (m_EyeRendererData.Renderer != null)
        {
            m_EyeColorMaterialPropertyBlock.SetColor("_EmissionColor", AttackEyeColor);
            m_EyeRendererData.Renderer.SetPropertyBlock(m_EyeColorMaterialPropertyBlock, m_EyeRendererData.MaterialIndex);
        }
    }

    public void OrientTowards(Vector3 lookPosition)
    {
        Vector3 lookDirection = Vector3.ProjectOnPlane(lookPosition - transform.position, Vector3.up).normalized;
        if (lookDirection.sqrMagnitude != 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * OrientationSpeed);
        }
    }

    bool IsPathValid()
    {
        return PatrolPath && PatrolPath.PathNodes.Count > 0;
    }

    public void ResetPathDestination()
    {
        m_PathDestinationNodeIndex = 0;
    }

    public void SetPathDestinationToClosestNode()
    {
        if (IsPathValid())
        {
            int closestPathNodeIndex = 0;
            for (int i = 0; i < PatrolPath.PathNodes.Count; i++)
            {
                float distanceToPathNode = PatrolPath.GetDistanceToNode(transform.position, i);
                if (distanceToPathNode < PatrolPath.GetDistanceToNode(transform.position, closestPathNodeIndex))
                {
                    closestPathNodeIndex = i;
                }
            }

            m_PathDestinationNodeIndex = closestPathNodeIndex;
        }
        else
        {
            m_PathDestinationNodeIndex = 0;
        }
    }

    public Vector3 GetDestinationOnPath()
    {
        if (IsPathValid())
        {
            return PatrolPath.GetPositionOfPathNode(m_PathDestinationNodeIndex);
        }
        else
        {
            return transform.position;
        }
    }

    public void SetNavDestination(Vector3 destination)
    {
        if (NavMeshAgent)
        {
            NavMeshAgent.SetDestination(destination);
        }
    }

    public void UpdatePathDestination(bool inverseOrder = false)
    {
        if (IsPathValid())
        {
            // 경로 목적지에 도달했는지 확인
            if ((transform.position - GetDestinationOnPath()).magnitude <= PathReachingRadius)
            {
                // 경로 목적지 인덱스 증가
                m_PathDestinationNodeIndex = inverseOrder ? (m_PathDestinationNodeIndex - 1) : (m_PathDestinationNodeIndex + 1);
                if (m_PathDestinationNodeIndex < 0)
                {
                    m_PathDestinationNodeIndex += PatrolPath.PathNodes.Count;
                }

                if (m_PathDestinationNodeIndex >= PatrolPath.PathNodes.Count)
                {
                    m_PathDestinationNodeIndex -= PatrolPath.PathNodes.Count;
                }
            }
        }
    }

    void OnDamaged(float damage, GameObject damageSource)
    {
        // 데미지 소스가 플레이어인지 테스트
        if (damageSource && !damageSource.GetComponent<EnemyController>())
        {
            // 플레이어 추적
            DetectionModule.OnDamaged(damageSource);

            onDamaged?.Invoke();
            m_LastTimeDamaged = Time.time;

            // 데미지 틱 사운드 재생
            if (DamageTick && !m_WasDamagedThisFrame)
                AudioUtility.CreateSFX(DamageTick, transform.position, AudioUtility.AudioGroups.DamageTick, 0f);

            m_WasDamagedThisFrame = true;
        }
    }

    void OnDie()
    {
        // 죽을 때 파티클 시스템 생성
        var vfx = Instantiate(DeathVfx, DeathVfxSpawnPoint.position, Quaternion.identity);
        Destroy(vfx, 5f);

        // 게임 플로우 매니저에게 적 파괴 처리 알림
        m_EnemyManager.UnregisterEnemy(this);

        // 오브젝트 전리품 드롭
        if (TryDropItem())
        {
            Instantiate(LootPrefab, transform.position, Quaternion.identity);
        }

        // 이것은 OnDestroy 함수를 호출할 것
        Destroy(gameObject, DeathDuration);
    }

    void OnDrawGizmosSelected()
    {
        // 경로 도달 범위
        Gizmos.color = PathReachingRangeColor;
        Gizmos.DrawWireSphere(transform.position, PathReachingRadius);

        if (DetectionModule != null)
        {
            // 탐지 범위
            Gizmos.color = DetectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, DetectionModule.DetectionRange);

            // 공격 범위
            Gizmos.color = AttackRangeColor;
            Gizmos.DrawWireSphere(transform.position, DetectionModule.AttackRange);
        }
    }

    public void OrientWeaponsTowards(Vector3 lookPosition)
    {
        for (int i = 0; i < m_Weapons.Length; i++)
        {
            // 플레이어 방향으로 무기 조준
            Vector3 weaponForward = (lookPosition - m_Weapons[i].WeaponRoot.transform.position).normalized;
            m_Weapons[i].transform.forward = weaponForward;
        }
    }

    public bool TryAtack(Vector3 enemyPosition)
    {
        if (m_GameFlowManager.GameIsEnding)
            return false;

        OrientWeaponsTowards(enemyPosition);

        if ((m_LastTimeWeaponSwapped + DelayAfterWeaponSwap) >= Time.time)
            return false;

        // 무기 발사
        bool didFire = GetCurrentWeapon().HandleShootInputs(false, true, false);

        if (didFire && onAttack != null)
        {
            onAttack.Invoke();

            if (SwapToNextWeapon && m_Weapons.Length > 1)
            {
                int nextWeaponIndex = (m_CurrentWeaponIndex + 1) % m_Weapons.Length;
                SetCurrentWeapon(nextWeaponIndex);
            }
        }
    
        return didFire;
    }

    public bool TryDropItem()
    {
        if (DropRate == 0 || LootPrefab == null)
            return false;
        else if (DropRate == 1)
            return true;
        else
            return (Random.value <= DropRate);
    }

    void FindAndInitializeAllWeapons()
    {
        // 이미 무기를 찾고 초기화했는지 확인
        if (m_Weapons == null)
        {
            m_Weapons = GetComponentsInChildren<WeaponController>();
            DebugUtility.HandleErrorIfNoComponentFound<WeaponController, EnemyController>(m_Weapons.Length, this, gameObject);

            for (int i = 0; i < m_Weapons.Length; i++)
            {
                m_Weapons[i].Owner = gameObject;
            }
        }
    }

    public WeaponController GetCurrentWeapon()
    {
        FindAndInitializeAllWeapons();
        // 현재 선택된 무기가 없는지 확인
        if (m_CurrentWeapon == null)
        {
            // 무기 목록의 첫 번째 무기를 현재 무기로 설정
            SetCurrentWeapon(0);
        }

        DebugUtility.HandleErrorIfNullGetComponent<WeaponController, EnemyController>(m_CurrentWeapon, this, gameObject);

        return m_CurrentWeapon;
    }

    void SetCurrentWeapon(int index)
    {
        m_CurrentWeaponIndex = index;
        if (m_Weapons.Length <= index)
            return;
        m_CurrentWeapon = m_Weapons[m_CurrentWeaponIndex];
        if (SwapToNextWeapon)
        {
            m_LastTimeWeaponSwapped = Time.time;
        }
        else
        {
            m_LastTimeWeaponSwapped = Mathf.NegativeInfinity;
        }
    }
}