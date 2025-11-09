using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Windows;
using FPS.Common;

[RequireComponent(typeof(AudioSource))]
public class Weapon : MonoBehaviour
{
    [Header("정보")]
    [Tooltip("UI에서 이 무기에 대해 표시될 이름")]
    public string WeaponName;

    [Tooltip("UI에서 이 무기에 대해 표시될 이미지")]
    public Sprite WeaponIcon;

    [Tooltip("조준점의 기본 데이터")]
    public CrosshairData CrosshairDataDefault;

    [Tooltip("적을 조준할 때의 조준점 데이터")]
    public CrosshairData CrosshairDataTargetInSight;

    [Header("내부 참조")]
    [Tooltip("무기의 루트 오브젝트, 무기가 비활성화될 때 비활성화되는 오브젝트")]
    public GameObject WeaponRoot;

    [Tooltip("무기의 총구, 발사체가 나가는 지점")]
    public Transform WeaponMuzzle;

    [Header("사격 매개변수")]
    [Tooltip("무기 유형이 사격 방식에 영향을 줌")]
    public WeaponShootType ShootType;

    [Tooltip("발사체 프리팹")]
    public ProjectileBase ProjectilePrefab;

    [Tooltip("두 발 사이의 최소 지연 시간")]
    public float DelayBetweenShots = 0.5f;

    [Tooltip("총알이 무작위로 발사될 원뿔의 각도 (0은 확산 없음을 의미)")]
    public float BulletSpreadAngle = 0f;

    [Tooltip("발당 총알 수")]
    public int BulletsPerShot = 1;

    [Tooltip("각 발사 후 무기를 뒤로 밀어낼 힘")]
    [Range(0f, 2f)]
    public float RecoilForce = 1;

    [Tooltip("이 무기로 조준할 때 적용되는 기본 FOV의 비율")]
    [Range(0f, 1f)]
    public float AimZoomRatio = 1f;

    [Tooltip("이 무기로 조준할 때 무기 팔에 적용할 이동")]
    public Vector3 AimOffset;

    [Header("충전 매개변수 (충전 무기만)")]
    [Tooltip("최대 충전에 도달할 때 발사 트리거")]
    public bool AutomaticReleaseOnCharged;

    [Tooltip("최대 충전에 도달하는 시간")]
    public float MaxChargeDuration = 2f;

    [Tooltip("충전 시작 시 사용되는 초기 탄약")]
    public float AmmoUsedOnStartCharge = 1f;

    [Tooltip("충전이 최대에 도달할 때 사용되는 추가 탄약")]
    public float AmmoUsageRateWhileCharging = 1f;

    [Header("오디오 & 비주얼")]
    [Tooltip("발사 애니메이션을 위한 선택적 무기 애니메이터")]
    public Animator WeaponAnimator;

    [Tooltip("총구 섬광 프리팹")]
    public GameObject MuzzleFlashPrefab;

    [Tooltip("생성 시 총구 섬광 인스턴스의 부모 해제")]
    public bool UnparentMuzzleFlash;

    [Tooltip("발사 시 재생되는 사운드")]
    public AudioClip ShootSfx;

    [Tooltip("이 무기로 변경할 때 재생되는 사운드")]
    public AudioClip ChangeWeaponSfx;

    [Tooltip("연속 발사 사운드")]
    public bool UseContinuousShootSound = false;
    public AudioClip ContinuousShootStartSfx;
    public AudioClip ContinuousShootLoopSfx;
    public AudioClip ContinuousShootEndSfx;

    // 이벤트
    public UnityAction OnShoot;
    public event Action OnShootProcessed;

    // Magazine 컴포넌트 참조
    private MagazineBase magazine;

    // 내부 변수들
    AudioSource m_ContinuousShootAudioSource = null;
    bool m_WantsToShoot = false;
    float m_LastTimeShot = Mathf.NegativeInfinity;
    Vector3 m_LastMuzzlePosition;

    // 프로퍼티들
    public GameObject Owner { get; set; }
    public GameObject SourcePrefab { get; set; }
    public bool IsCharging { get; private set; }
    public bool IsWeaponActive { get; private set; }
    public bool IsCooling { get; private set; }
    public float CurrentCharge { get; private set; }
    public Vector3 MuzzleWorldVelocity { get; private set; }
    public float LastChargeTriggerTimestamp { get; private set; }

    // 외부 호환성을 위한 프로퍼티들
    public float CurrentAmmoRatio => magazine?.CurrentAmmoRatio ?? 0f;
    public bool IsReloading => magazine?.IsReloading ?? false;

    // 외부 호환성을 위한 메서드들 (기존 메서드명 유지)
    public float GetAmmoNeededToShoot() => (ShootType != WeaponShootType.Charge ?
        1f : Mathf.Max(1f, AmmoUsedOnStartCharge)) / ((magazine?.MaxAmmo ?? 1) * BulletsPerShot);

    public int GetCarriedPhysicalBullets() => magazine?.CarriedAmmo ?? 0;
    public int GetCurrentAmmo() => magazine?.CurrentAmmo ?? 0;

    AudioSource m_ShootAudioSource;
    const string k_AnimAttackParameter = "Attack";

    void Awake()
    {
        // Magazine 컴포넌트 필수
        magazine = GetComponent<MagazineBase>();

        if (magazine == null)
        {
            Debug.LogError($"MagazineBase component is required on {gameObject.name}. Please add a Magazine component.");
            return;
        }

        // Magazine 이벤트 연결
        magazine.OnReloadStarted += HandleReloadStarted;
        magazine.OnReloadCompleted += HandleReloadCompleted;
        magazine.OnAmmoChanged += HandleAmmoChanged;
        magazine.OnAmmoEmpty += HandleAmmoEmpty;

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
    }

    // Magazine 이벤트 핸들러들
    void HandleReloadStarted()
    {
        // 재장전 시작 시 추가 로직
    }

    void HandleReloadCompleted()
    {
        // 재장전 완료 시 추가 로직
    }

    void HandleAmmoChanged(int currentAmmo, int carriedAmmo)
    {
        // 탄약 변경 시 UI 업데이트 등
    }

    void HandleAmmoEmpty()
    {
        // 탄약 소진 시 추가 로직
    }

    // 외부 호환성을 위한 메서드들 (기존 메서드명 유지)
    public void AddCarriablePhysicalBullets(int count) => magazine?.AddCarriedAmmo(count);
    public void UseAmmo(float amount) => magazine?.TryConsumeAmmo(Mathf.CeilToInt(amount));

    // 재장전 관련 (기존 메서드명 유지)
    public void Reload() => magazine?.TryStartReload();
    public void StartReloadAnimation() => magazine?.TryStartReload();

    void Update()
    {
        UpdateCharge();
        UpdateContinuousShootSound();

        if (Time.deltaTime > 0)
        {
            MuzzleWorldVelocity = (WeaponMuzzle.position - m_LastMuzzlePosition) / Time.deltaTime;
            m_LastMuzzlePosition = WeaponMuzzle.position;
        }
    }

    void UpdateCharge()
    {
        if (IsCharging)
        {
            if (CurrentCharge < 1f)
            {
                float chargeLeft = 1f - CurrentCharge;
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
                float ammoThisChargeWouldRequire = chargeAdded * AmmoUsageRateWhileCharging;

                if (GetCurrentAmmo() >= ammoThisChargeWouldRequire)
                {
                    magazine.TryConsumeAmmo(Mathf.CeilToInt(ammoThisChargeWouldRequire));
                    CurrentCharge = Mathf.Clamp01(CurrentCharge + chargeAdded);
                }
            }
        }
    }

    void UpdateContinuousShootSound()
    {
        if (UseContinuousShootSound)
        {
            if (m_WantsToShoot && GetCurrentAmmo() >= 1)
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
        if (GetCurrentAmmo() >= 1 && m_LastTimeShot + DelayBetweenShots < Time.time)
        {
            if (magazine.TryConsumeAmmo(1))
            {
                HandleShoot();
                return true;
            }
        }
        return false;
    }

    bool TryBeginCharge()
    {
        if (!IsCharging && GetCurrentAmmo() >= AmmoUsedOnStartCharge &&
            m_LastTimeShot + DelayBetweenShots < Time.time)
        {
            if (magazine.TryConsumeAmmo(Mathf.CeilToInt(AmmoUsedOnStartCharge)))
            {
                LastChargeTriggerTimestamp = Time.time;
                IsCharging = true;
                return true;
            }
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

        for (int i = 0; i < bulletsPerShotFinal; i++)
        {
            Vector3 shotDirection = GetShotDirectionWithinSpread(WeaponMuzzle);
            ProjectileBase newProjectile = Instantiate(ProjectilePrefab, WeaponMuzzle.position, Quaternion.LookRotation(shotDirection));
            newProjectile.Shoot(this);
        }

        if (MuzzleFlashPrefab != null)
        {
            GameObject muzzleFlashInstance = Instantiate(MuzzleFlashPrefab, WeaponMuzzle.position, WeaponMuzzle.rotation, WeaponMuzzle.transform);
            if (UnparentMuzzleFlash)
            {
                muzzleFlashInstance.transform.SetParent(null);
            }
            Destroy(muzzleFlashInstance, 2f);
        }

        m_LastTimeShot = Time.time;

        if (ShootSfx && !UseContinuousShootSound)
        {
            m_ShootAudioSource.PlayOneShot(ShootSfx);
        }

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

    // 추가 Magazine 관련 메서드들 (새로운 기능)
    public bool TryStartReload() => magazine?.TryStartReload() ?? false;
    public bool CanReload() => magazine?.CanReload() ?? false;
    public void CancelReload() => magazine?.CancelReload();
    public void AddCarriedAmmo(int amount) => magazine?.AddCarriedAmmo(amount);
    public void SetCurrentAmmo(int amount) => magazine?.SetCurrentAmmo(amount);
    public void SetCarriedAmmo(int amount) => magazine?.SetCarriedAmmo(amount);
    public MagazineBase GetMagazine() => magazine;

    // 디버그용
    public string GetMagazineDebugInfo() => magazine?.GetDebugInfo() ?? "No Magazine Component";

    private void OnDestroy()
    {
        if (magazine != null)
        {
            magazine.OnReloadStarted -= HandleReloadStarted;
            magazine.OnReloadCompleted -= HandleReloadCompleted;
            magazine.OnAmmoChanged -= HandleAmmoChanged;
            magazine.OnAmmoEmpty -= HandleAmmoEmpty;
        }
    }
}