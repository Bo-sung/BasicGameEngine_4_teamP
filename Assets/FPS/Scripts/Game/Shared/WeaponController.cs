using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Windows;
using FPS.Common;

[System.Serializable]
public struct CrosshairData
{
    // The image that will be used for this weapon's crosshair
    [Tooltip("이 무기의 조준점에 사용될 이미지")]
    public Sprite CrosshairSprite;

    // The size of the crosshair image
    [Tooltip("조준점 이미지의 크기")]
    public int CrosshairSize;

    // The color of the crosshair image
    [Tooltip("조준점 이미지의 색상")]
    public Color CrosshairColor;
}

[RequireComponent(typeof(AudioSource))]
public class WeaponController : MonoBehaviour
{
    [Header("정보")]
    // The name that will be displayed in the UI for this weapon
    [Tooltip("UI에서 이 무기에 대해 표시될 이름")]
    public string WeaponName;

    // The image that will be displayed in the UI for this weapon
    [Tooltip("UI에서 이 무기에 대해 표시될 이미지")]
    public Sprite WeaponIcon;

    // Default data for the crosshair
    [Tooltip("조준점의 기본 데이터")]
    public CrosshairData CrosshairDataDefault;

    // Data for the crosshair when targeting an enemy
    [Tooltip("적을 조준할 때의 조준점 데이터")]
    public CrosshairData CrosshairDataTargetInSight;

    [Header("내부 참조")]
    // The root object for the weapon, this is what will be deactivated when the weapon isn't active
    [Tooltip("무기의 루트 오브젝트, 무기가 비활성화될 때 비활성화되는 오브젝트")]
    public GameObject WeaponRoot;

    // Tip of the weapon, where the projectiles are shot
    [Tooltip("무기의 총구, 발사체가 나가는 지점")]
    public Transform WeaponMuzzle;

    [Header("사격 매개변수")]
    // The type of weapon wil affect how it shoots
    [Tooltip("무기 유형이 사격 방식에 영향을 줌")]
    public WeaponShootType ShootType;

    // The projectile prefab
    [Tooltip("발사체 프리팹")]
    public ProjectileBase ProjectilePrefab;

    // Minimum duration between two shots
    [Tooltip("두 발 사이의 최소 지연 시간")]
    public float DelayBetweenShots = 0.5f;

    // Angle for the cone in which the bullets will be shot randomly (0 means no spread at all)
    [Tooltip("총알이 무작위로 발사될 원뿔의 각도 (0은 확산 없음을 의미)")]
    public float BulletSpreadAngle = 0f;

    // Amount of bullets per shot
    [Tooltip("발당 총알 수")]
    public int BulletsPerShot = 1;

    // Force that will push back the weapon after each shot
    [Tooltip("각 발사 후 무기를 뒤로 밀어낼 힘")]
    [Range(0f, 2f)]
    public float RecoilForce = 1;

    // Ratio of the default FOV that this weapon applies while aiming
    [Tooltip("이 무기로 조준할 때 적용되는 기본 FOV의 비율")]
    [Range(0f, 1f)]
    public float AimZoomRatio = 1f;

    // Translation to apply to weapon arm when aiming with this weapon
    [Tooltip("이 무기로 조준할 때 무기 팔에 적용할 이동")]
    public Vector3 AimOffset;

    [Header("탄약 매개변수")]
    // Should the player manually reload
    [Tooltip("플레이어가 수동으로 재장전해야 함")]
    public bool AutomaticReload = true;

    // Has physical clip on the weapon and ammo shells are ejected when firing
    [Tooltip("무기에 물리적 탄창이 있고 발사 시 탄피가 배출됨")]
    public bool HasPhysicalBullets = false;

    // Number of bullets in a clip
    [Tooltip("탄창의 총알 수")]
    public int ClipSize = 30;

    // Bullet Shell Casing
    [Tooltip("총알 탄피")]
    public GameObject ShellCasing;

    // Weapon Ejection Port for physical ammo
    [Tooltip("물리적 탄약을 위한 무기 배출구")]
    public Transform EjectionPort;

    // Force applied on the shell
    [Tooltip("탄피에 가해지는 힘")]
    [Range(0.0f, 5.0f)]
    public float ShellCasingEjectionForce = 2.0f;

    // Maximum number of shell that can be spawned before reuse
    [Tooltip("재사용되기 전에 생성할 수 있는 최대 탄피 수")]
    [Range(1, 30)]
    public int ShellPoolSize = 1;

    // Amount of ammo reloaded per second
    [Tooltip("초당 재장전되는 탄약량")]
    public float AmmoReloadRate = 1f;

    // Delay after the last shot before starting to reload
    [Tooltip("마지막 발사 후 재장전 시작 전 지연 시간")]
    public float AmmoReloadDelay = 2f;

    // Maximum amount of ammo in the gun
    [Tooltip("총의 최대 탄약량")]
    public int MaxAmmo = 8;

    [Header("충전 매개변수 (충전 무기만)")]
    // Trigger a shot when maximum charge is reached
    [Tooltip("최대 충전에 도달할 때 발사 트리거")]
    public bool AutomaticReleaseOnCharged;

    // Duration to reach maximum charge
    [Tooltip("최대 충전에 도달하는 시간")]
    public float MaxChargeDuration = 2f;

    // Initial ammo used when starting to charge
    [Tooltip("충전 시작 시 사용되는 초기 탄약")]
    public float AmmoUsedOnStartCharge = 1f;

    // Additional ammo used when charge reaches its maximum
    [Tooltip("충전이 최대에 도달할 때 사용되는 추가 탄약")]
    public float AmmoUsageRateWhileCharging = 1f;

    [Header("오디오 & 비주얼")]
    // Optional weapon animator for OnShoot animations
    [Tooltip("발사 애니메이션을 위한 선택적 무기 애니메이터")]
    public Animator WeaponAnimator;

    // Prefab of the muzzle flash
    [Tooltip("총구 섬광 프리팹")]
    public GameObject MuzzleFlashPrefab;

    // Unparent the muzzle flash instance on spawn
    [Tooltip("생성 시 총구 섬광 인스턴스의 부모 해제")]
    public bool UnparentMuzzleFlash;

    // sound played when shooting
    [Tooltip("발사 시 재생되는 사운드")]
    public AudioClip ShootSfx;

    // Sound played when changing to this weapon
    [Tooltip("이 무기로 변경할 때 재생되는 사운드")]
    public AudioClip ChangeWeaponSfx;

    // Continuous Shooting Sound
    [Tooltip("연속 발사 사운드")]
    public bool UseContinuousShootSound = false;
    public AudioClip ContinuousShootStartSfx;
    public AudioClip ContinuousShootLoopSfx;
    public AudioClip ContinuousShootEndSfx;
    AudioSource m_ContinuousShootAudioSource = null;
    bool m_WantsToShoot = false;

    public UnityAction OnShoot;
    public event Action OnShootProcessed;

    int m_CarriedPhysicalBullets;
    float m_CurrentAmmo;
    float m_LastTimeShot = Mathf.NegativeInfinity;
    public float LastChargeTriggerTimestamp { get; private set; }
    Vector3 m_LastMuzzlePosition;

    public GameObject Owner { get; set; }
    public GameObject SourcePrefab { get; set; }
    public bool IsCharging { get; private set; }
    public float CurrentAmmoRatio { get; private set; }
    public bool IsWeaponActive { get; private set; }
    public bool IsCooling { get; private set; }
    public float CurrentCharge { get; private set; }
    public Vector3 MuzzleWorldVelocity { get; private set; }

    public float GetAmmoNeededToShoot() => (ShootType != WeaponShootType.Charge ?
        1f :
        Mathf.Max(1f, AmmoUsedOnStartCharge)) / (MaxAmmo * BulletsPerShot);

    public int GetCarriedPhysicalBullets() => m_CarriedPhysicalBullets;
    public int GetCurrentAmmo() => Mathf.FloorToInt(m_CurrentAmmo);

    AudioSource m_ShootAudioSource;

    public bool IsReloading { get; private set; }

    const string k_AnimAttackParameter = "Attack";

    private Queue<Rigidbody> m_PhysicalAmmoPool;

    void Awake()
    {
        m_CurrentAmmo = MaxAmmo;
        m_CarriedPhysicalBullets = HasPhysicalBullets ? ClipSize : 0;
        m_LastMuzzlePosition = WeaponMuzzle.position;

        m_ShootAudioSource = GetComponent<AudioSource>();
        DebugUtility.HandleErrorIfNullGetComponent<AudioSource, WeaponController>(m_ShootAudioSource, this, gameObject);

        if (UseContinuousShootSound)
        {
            m_ContinuousShootAudioSource = gameObject.AddComponent<AudioSource>();
            m_ContinuousShootAudioSource.playOnAwake = false;
            m_ContinuousShootAudioSource.clip = ContinuousShootLoopSfx;
            m_ContinuousShootAudioSource.outputAudioMixerGroup = AudioUtility.GetAudioGroup(AudioUtility.AudioGroups.WeaponShoot);
            m_ContinuousShootAudioSource.loop = true;
        }

        if (HasPhysicalBullets)
        {
            m_PhysicalAmmoPool = new Queue<Rigidbody>(ShellPoolSize);

            for (int i = 0; i < ShellPoolSize; i++)
            {
                GameObject shell = Instantiate(ShellCasing, transform);
                shell.SetActive(false);
                m_PhysicalAmmoPool.Enqueue(shell.GetComponent<Rigidbody>());
            }
        }
    }

    public void AddCarriablePhysicalBullets(int count) => m_CarriedPhysicalBullets = Mathf.Max(m_CarriedPhysicalBullets + count, MaxAmmo);

    void ShootShell()
    {
        Rigidbody nextShell = m_PhysicalAmmoPool.Dequeue();

        nextShell.transform.position = EjectionPort.transform.position;
        nextShell.transform.rotation = EjectionPort.transform.rotation;
        nextShell.gameObject.SetActive(true);
        nextShell.transform.SetParent(null);
        nextShell.collisionDetectionMode = CollisionDetectionMode.Continuous;
        nextShell.AddForce(nextShell.transform.up * ShellCasingEjectionForce, ForceMode.Impulse);

        m_PhysicalAmmoPool.Enqueue(nextShell);
    }

    void PlaySFX(AudioClip sfx) => AudioUtility.CreateSFX(sfx, transform.position, AudioUtility.AudioGroups.WeaponShoot, 0.0f);

    public void Reload()
    {
        if (m_CarriedPhysicalBullets > 0)
        {
            m_CurrentAmmo = Mathf.Min(m_CarriedPhysicalBullets, ClipSize);
        }

        IsReloading = false;
    }

    public void StartReloadAnimation()
    {
        if (m_CurrentAmmo < m_CarriedPhysicalBullets)
        {
            GetComponent<Animator>().SetTrigger("Reload");
            IsReloading = true;
        }
    }

    void Update()
    {
        UpdateAmmo();
        UpdateCharge();
        UpdateContinuousShootSound();

        if (Time.deltaTime > 0)
        {
            MuzzleWorldVelocity = (WeaponMuzzle.position - m_LastMuzzlePosition) / Time.deltaTime;
            m_LastMuzzlePosition = WeaponMuzzle.position;
        }
    }

    void UpdateAmmo()
    {
        if (AutomaticReload && m_LastTimeShot + AmmoReloadDelay < Time.time && m_CurrentAmmo < MaxAmmo && !IsCharging)
        {
            // 시간에 따라 무기 재장전
            m_CurrentAmmo += AmmoReloadRate * Time.deltaTime;

            // 탄약을 최대값으로 제한
            m_CurrentAmmo = Mathf.Clamp(m_CurrentAmmo, 0, MaxAmmo);

            IsCooling = true;
        }
        else
        {
            IsCooling = false;
        }

        if (MaxAmmo == Mathf.Infinity)
        {
            CurrentAmmoRatio = 1f;
        }
        else
        {
            CurrentAmmoRatio = m_CurrentAmmo / MaxAmmo;
        }
    }

    void UpdateCharge()
    {
        if (IsCharging)
        {
            if (CurrentCharge < 1f)
            {
                float chargeLeft = 1f - CurrentCharge;

                // 이번 프레임에 추가할 충전 비율 계산
                float chargeAdded = 0f;
                if (MaxChargeDuration <= 0f)
                {
                    chargeAdded = chargeLeft;
                }
                else
                {
                    chargeAdded = (1f / MaxChargeDuration) * Time.deltaTime;
                }

                chargeAdded = Mathf.Clamp(chargeAdded, 0f, chargeLeft);

                // 실제로 이 충전을 추가할 수 있는지 확인
                float ammoThisChargeWouldRequire = chargeAdded * AmmoUsageRateWhileCharging;
                if (ammoThisChargeWouldRequire <= m_CurrentAmmo)
                {
                    // 추가된 충전에 따라 탄약 사용
                    UseAmmo(ammoThisChargeWouldRequire);

                    // 현재 충전 비율 설정
                    CurrentCharge = Mathf.Clamp01(CurrentCharge + chargeAdded);
                }
            }
        }
    }

    void UpdateContinuousShootSound()
    {
        if (UseContinuousShootSound)
        {
            if (m_WantsToShoot && m_CurrentAmmo >= 1f)
            {
                if (!m_ContinuousShootAudioSource.isPlaying)
                {
                    m_ShootAudioSource.PlayOneShot(ShootSfx);
                    m_ShootAudioSource.PlayOneShot(ContinuousShootStartSfx);
                    m_ContinuousShootAudioSource.Play();
                }
            }
            else if (m_ContinuousShootAudioSource.isPlaying)
            {
                m_ShootAudioSource.PlayOneShot(ContinuousShootEndSfx);
                m_ContinuousShootAudioSource.Stop();
            }
        }
    }

    public void ShowWeapon(bool show)
    {
        WeaponRoot.SetActive(show);

        if (show && ChangeWeaponSfx)
        {
            m_ShootAudioSource.PlayOneShot(ChangeWeaponSfx);
        }

        IsWeaponActive = show;
    }

    public void UseAmmo(float amount)
    {
        m_CurrentAmmo = Mathf.Clamp(m_CurrentAmmo - amount, 0f, MaxAmmo);
        m_CarriedPhysicalBullets -= Mathf.RoundToInt(amount);
        m_CarriedPhysicalBullets = Mathf.Clamp(m_CarriedPhysicalBullets, 0, MaxAmmo);
        m_LastTimeShot = Time.time;
    }

    public bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
    {
        m_WantsToShoot = inputDown || inputHeld;
        switch (ShootType)
        {
            case WeaponShootType.Manual:
                if (inputDown)
                {
                    return TryShoot();
                }

                return false;

            case WeaponShootType.Automatic:
                if (inputHeld)
                {
                    return TryShoot();
                }

                return false;

            case WeaponShootType.Charge:
                if (inputHeld)
                {
                    TryBeginCharge();
                }

                // 충전을 해제했는지 또는 무기가 완전히 충전되었을 때 자동으로 발사하는지 확인
                if (inputUp || (AutomaticReleaseOnCharged && CurrentCharge >= 1f))
                {
                    return TryReleaseCharge();
                }

                return false;

            default:
                return false;
        }
    }

    bool TryShoot()
    {
        if (m_CurrentAmmo >= 1f
            && m_LastTimeShot + DelayBetweenShots < Time.time)
        {
            HandleShoot();
            m_CurrentAmmo -= 1f;

            return true;
        }

        return false;
    }

    bool TryBeginCharge()
    {
        if (!IsCharging
            && m_CurrentAmmo >= AmmoUsedOnStartCharge
            && Mathf.FloorToInt((m_CurrentAmmo - AmmoUsedOnStartCharge) * BulletsPerShot) > 0
            && m_LastTimeShot + DelayBetweenShots < Time.time)
        {
            UseAmmo(AmmoUsedOnStartCharge);

            LastChargeTriggerTimestamp = Time.time;
            IsCharging = true;

            return true;
        }

        return false;
    }

    bool TryReleaseCharge()
    {
        if (IsCharging)
        {
            HandleShoot();

            CurrentCharge = 0f;
            IsCharging = false;

            return true;
        }

        return false;
    }

    void HandleShoot()
    {
        int bulletsPerShotFinal = ShootType == WeaponShootType.Charge
            ? Mathf.CeilToInt(CurrentCharge * BulletsPerShot)
            : BulletsPerShot;

        // 무작위 방향으로 모든 총알 생성
        for (int i = 0; i < bulletsPerShotFinal; i++)
        {
            Vector3 shotDirection = GetShotDirectionWithinSpread(WeaponMuzzle);
            ProjectileBase newProjectile = Instantiate(ProjectilePrefab, WeaponMuzzle.position, Quaternion.LookRotation(shotDirection));
            newProjectile.Shoot(this);
        }

        // 총구 섬광
        if (MuzzleFlashPrefab != null)
        {
            GameObject muzzleFlashInstance = Instantiate(MuzzleFlashPrefab, WeaponMuzzle.position, WeaponMuzzle.rotation, WeaponMuzzle.transform);
            // muzzleFlashInstance의 부모 해제
            if (UnparentMuzzleFlash)
            {
                muzzleFlashInstance.transform.SetParent(null);
            }

            Destroy(muzzleFlashInstance, 2f);
        }

        if (HasPhysicalBullets)
        {
            ShootShell();
            m_CarriedPhysicalBullets--;
        }

        m_LastTimeShot = Time.time;

        // 발사 효과음 재생
        if (ShootSfx && !UseContinuousShootSound)
        {
            m_ShootAudioSource.PlayOneShot(ShootSfx);
        }

        // 공격 애니메이션 트리거 (있는 경우)
        if (WeaponAnimator)
        {
            WeaponAnimator.SetTrigger(k_AnimAttackParameter);
        }

        OnShoot?.Invoke();
        OnShootProcessed?.Invoke();
    }

    public Vector3 GetShotDirectionWithinSpread(Transform shootTransform)
    {
        float spreadAngleRatio = BulletSpreadAngle / 180f;
        Vector3 spreadWorldDirection = Vector3.Slerp(shootTransform.forward, UnityEngine.Random.insideUnitSphere, spreadAngleRatio);

        return spreadWorldDirection;
    }
}