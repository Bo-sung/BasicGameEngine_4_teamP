
using UnityEngine;
using UnityEngine.AI;
using FPS.Common;

// ========== 적 베이스 클래스 ==========
/// <summary>
/// 데이터 테이블 연동이 가능한 적 베이스 클래스
/// EnemyController를 상속받아 기능 그대로 사용.
/// </summary>
public class EnemyBase : EnemyController
{
    [Header("=== 적 설정 방식 선택 ===")]
    [Tooltip("데이터 테이블 사용 여부")]
    public bool useDataTable = true;

    [Tooltip("데이터 테이블에서 사용할 적 ID")]
    public string enemyID = "enemy_basic";

    [Header("=== 직접 설정 (테이블 미사용시) ===")]
    public EnemyTableData directData;

    // ========== 내부 변수들 ==========
    protected EnemyTableData currentEnemyData;
    protected bool isInitialized = false;

    // ========== 현재 상태 정보 ==========
    public string EnemyName => currentEnemyData?.enemyName ?? "Unknown";
    public float MaxHealth => currentEnemyData?.maxHealth ?? 100f;

    // ========== Unity 생명주기 ==========
    protected virtual void Awake()
    {
        // 에디터 모드에서는 초기화하지 않음 (DontSave 오류 방지)
        if (!Application.isPlaying)
            return;

        InitializeEnemy();
    }

    // ========== 초기화 ==========
    private void InitializeEnemy()
    {
        // 데이터 로드
        LoadEnemyData();

        // 설정 적용
        ApplyEnemySettings();

        // 에셋 로드
        LoadEnemyAssets();

        // 커스텀 초기화
        OnEnemyInitialized();

        isInitialized = true;
    }

    [ContextMenu("InitNow")]
    void InitNow()
    {
        InitializeEnemy();
    }

    /// <summary>
    /// 데이터 세팅
    /// </summary>
    private void LoadEnemyData()
    {
        if (useDataTable)
        {
            // 테이블에서 적 데이터 로드
            currentEnemyData = Table.Enemy.GetData(enemyID);
            if (currentEnemyData == null)
            {
                Debug.LogError($"적 데이터를 찾을 수 없습니다: {enemyID}");
                // 기본값으로 대체
                currentEnemyData = CreateDefaultEnemyData();
            }
        }
        else
        {
            // 직접 설정된 데이터 사용
            if (directData != null)
            {
                currentEnemyData = directData;
            }
            else
            {
                Debug.LogWarning($"직접 데이터가 설정되지 않았습니다. 기본값을 사용합니다.");
                currentEnemyData = CreateDefaultEnemyData();
            }
        }
    }

    /// <summary>
    /// 적 세팅 적용
    /// </summary>
    private void ApplyEnemySettings()
    {
        // 이동 설정
        var navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = currentEnemyData.moveSpeed;
            navMeshAgent.angularSpeed = currentEnemyData.angularSpeed;
            navMeshAgent.acceleration = currentEnemyData.acceleration;
        }

        // 경로 찾기 설정
        PathReachingRadius = currentEnemyData.pathReachingRadius;
        SelfDestructYHeight = currentEnemyData.selfDestructYHeight;
        OrientationSpeed = currentEnemyData.orientationSpeed;

        // 탐지 설정
        var detectionModule = GetComponentInChildren<DetectionModule>();
        if (detectionModule != null)
        {
            detectionModule.DetectionRange = currentEnemyData.detectionRange;
            detectionModule.AttackRange = currentEnemyData.attackRange;
            detectionModule.KnownTargetTimeout = currentEnemyData.knownTargetTimeout;
        }

        // 무기 설정
        SwapToNextWeapon = currentEnemyData.swapToNextWeapon;
        DelayAfterWeaponSwap = currentEnemyData.delayAfterWeaponSwap;

        // 눈 색상 설정
        DefaultEyeColor = currentEnemyData.defaultEyeColor;
        AttackEyeColor = currentEnemyData.attackEyeColor;

        // 피격 효과
        FlashOnHitDuration = currentEnemyData.flashOnHitDuration;

        // 사망 설정
        DeathDuration = currentEnemyData.deathDuration;
        DropRate = currentEnemyData.dropRate;

        // 체력 설정
        var health = GetComponent<Health>();
        if (health != null)
        {
            health.MaxHealth = currentEnemyData.maxHealth;
            health.CurrentHealth = currentEnemyData.maxHealth;
        }
    }

    /// <summary>
    /// 적 에셋 로드
    /// </summary>
    private void LoadEnemyAssets()
    {
        // 사운드 로드
        if (!string.IsNullOrEmpty(currentEnemyData.damageTickSoundPath))
        {
            DamageTick = ResourceManager.LoadAudioClip(currentEnemyData.damageTickSoundPath);
        }

        // VFX 로드
        if (!string.IsNullOrEmpty(currentEnemyData.deathVfxPath))
        {
            DeathVfx = ResourceManager.LoadPrefab(currentEnemyData.deathVfxPath);
        }

        // 전리품 프리팹 로드
        if (!string.IsNullOrEmpty(currentEnemyData.lootPrefabPath))
        {
            LootPrefab = ResourceManager.LoadPrefab(currentEnemyData.lootPrefabPath);
        }

        // 무기 설정 (테이블에서 무기 ID로 로드)
        if (!string.IsNullOrEmpty(currentEnemyData.weaponID))
        {
            SetupWeaponFromTable(currentEnemyData.weaponID);
        }
    }

    /// <summary>
    /// 테이블에서 무기 설정
    /// </summary>
    private void SetupWeaponFromTable(string weaponID)
    {
        // 적이 가진 무기 컴포넌트 찾기
        var weapons = GetComponentsInChildren<WeaponBase>(true);

        if (weapons.Length > 0)
        {
            // 첫 번째 무기에 테이블 데이터 적용
            weapons[0].weaponID = weaponID;
            weapons[0].useDataTable = true;
        }
        else
        {
            Debug.LogWarning($"적 {name}에 무기가 없습니다. WeaponBase 컴포넌트를 추가해주세요.");
        }
    }

    /// <summary>
    /// 기본 적 데이터 생성
    /// </summary>
    private EnemyTableData CreateDefaultEnemyData()
    {
        return new EnemyTableData
        {
            enemyID = "enemy_default",
            enemyName = "기본 적",
            maxHealth = 100f,
            moveSpeed = 3.5f,
            angularSpeed = 120f,
            acceleration = 8f,
            orientationSpeed = 10f,
            pathReachingRadius = 2f,
            selfDestructYHeight = -20f,
            detectionRange = 20f,
            attackRange = 10f,
            knownTargetTimeout = 4f,
            weaponID = "pistol_basic",
            swapToNextWeapon = false,
            delayAfterWeaponSwap = 0f,
            defaultEyeColor = Color.cyan,
            attackEyeColor = Color.red,
            flashOnHitDuration = 0.5f,
            deathDuration = 0f,
            dropRate = 0.5f
        };
    }

    /// <summary>
    /// 커스텀 초기화 (상속 클래스에서 오버라이드)
    /// </summary>
    protected virtual void OnEnemyInitialized()
    {
        // 자식 클래스에서 추가 초기화 로직 구현
    }

    /// <summary>
    /// 런타임에 적 데이터 변경
    /// </summary>
    public void ChangeEnemyData(string newEnemyID)
    {
        enemyID = newEnemyID;
        useDataTable = true;
        InitializeEnemy();
    }

    /// <summary>
    /// 현재 적 데이터 반환
    /// </summary>
    public EnemyTableData GetCurrentEnemyData()
    {
        return currentEnemyData;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 디버그 정보 표시
    /// </summary>
    private void OnValidate()
    {
        if (useDataTable && !string.IsNullOrEmpty(enemyID))
        {
            // 에디터에서 ID 유효성 검사
            if (!Application.isPlaying && Table.Enemy.HasData(enemyID))
            {
                // 유효한 ID
            }
        }
    }
#endif
}