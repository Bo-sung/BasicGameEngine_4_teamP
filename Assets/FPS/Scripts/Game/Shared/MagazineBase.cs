using System;
using UnityEngine;

namespace Unity.FPS.Game
{
    public enum ReloadType
    {
        Magazine,           // 탄창 교체 (전체 재장전)
        Individual,         // 개별 탄약 장전 (샷건, 볼트액션)
        Automatic,          // 자동 재생성 (에너지 무기)
        ChargeableClip,     // 충전식 탄창
        BeltFed,           // 벨트 급탄식
        BreechLoading      // 약실 장전식 (단발)
    }

    public abstract class MagazineBase : MonoBehaviour
    {
        [Header("기본 탄약 설정")]
        [Tooltip("재장전 방식")]
        public ReloadType reloadType;

        [Tooltip("최대 탄약 수")]
        public int maxAmmo = 30;

        [Tooltip("시작 시 탄약 수")]
        public int startingAmmo = 30;

        [Tooltip("최대 보유 탄약 수")]
        public int maxCarriedAmmo = 90;

        [Tooltip("시작 시 보유 탄약 수")]
        public int startingCarriedAmmo = 90;

        [Header("재장전 설정")]
        [Tooltip("재장전 시간")]
        public float reloadTime = 2f;

        [Tooltip("빈 탄창 재장전 시간")]
        public float emptyReloadTime = 2.5f;

        [Header("오디오")]
        [Tooltip("재장전 시작 사운드")]
        public AudioClip reloadStartSound;

        [Tooltip("재장전 완료 사운드")]
        public AudioClip reloadCompleteSound;

        [Tooltip("탄약 소모 사운드")]
        public AudioClip ammoConsumeSound;

        // 현재 상태
        [SerializeField] protected int currentAmmo;
        [SerializeField] protected int carriedAmmo;

        // 이벤트
        public System.Action OnReloadStarted;
        public System.Action OnReloadCompleted;
        public System.Action<int, int> OnAmmoChanged; // (current, carried)
        public System.Action OnAmmoEmpty;

        // 상태 프로퍼티
        public bool IsReloading { get; protected set; }
        public bool IsEmpty => currentAmmo <= 0;
        public float CurrentAmmoRatio => maxAmmo > 0 ? (float)currentAmmo / maxAmmo : 0f;
        public int CurrentAmmo => currentAmmo;
        public int CarriedAmmo => carriedAmmo;
        public int MaxAmmo => maxAmmo;
        public int MaxCarriedAmmo => maxCarriedAmmo;

        protected Weapon weapon;
        protected AudioSource audioSource;

        protected virtual void Awake()
        {
            weapon = GetComponent<Weapon>();

            if (weapon == null)
            {
                Debug.LogError($"Weapon not found on {gameObject.name}. MagazineBase must be on the same GameObject as Weapon.");
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = weapon?.GetComponent<AudioSource>();
            }

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
            }
        }

        protected virtual void Start()
        {
            Initialize();
        }

        public virtual void Initialize()
        {
            currentAmmo = startingAmmo;
            carriedAmmo = startingCarriedAmmo;
            OnAmmoChanged?.Invoke(currentAmmo, carriedAmmo);
        }

        protected virtual void Update()
        {
            UpdateMagazine();
        }

        // 상속 클래스에서 구현할 업데이트 로직
        protected virtual void UpdateMagazine()
        {
            // 상속 클래스에서 구현
        }

        // 탄약 소모
        public virtual bool TryConsumeAmmo(int amount = 1)
        {
            if (currentAmmo >= amount && !IsReloading)
            {
                currentAmmo -= amount;
                OnAmmoChanged?.Invoke(currentAmmo, carriedAmmo);

                if (ammoConsumeSound && audioSource)
                {
                    audioSource.PlayOneShot(ammoConsumeSound);
                }

                if (currentAmmo <= 0)
                {
                    OnAmmoEmpty?.Invoke();
                }

                return true;
            }
            return false;
        }

        // 재장전 시도
        public virtual bool TryStartReload()
        {
            if (CanReload())
            {
                StartReload();
                return true;
            }
            return false;
        }

        // 재장전 가능 여부 확인
        public virtual bool CanReload()
        {
            return !IsReloading &&
                   currentAmmo < maxAmmo &&
                   carriedAmmo > 0;
        }

        // 재장전 시작 (추상 메서드)
        protected abstract void StartReload();

        // 재장전 완료
        protected virtual void CompleteReload()
        {
            IsReloading = false;
            OnReloadCompleted?.Invoke();
            OnAmmoChanged?.Invoke(currentAmmo, carriedAmmo);

            if (reloadCompleteSound && audioSource)
            {
                audioSource.PlayOneShot(reloadCompleteSound);
            }
        }

        // 재장전 시작 시 호출
        protected virtual void OnReloadStart()
        {
            IsReloading = true;
            OnReloadStarted?.Invoke();

            if (reloadStartSound && audioSource)
            {
                audioSource.PlayOneShot(reloadStartSound);
            }
        }

        // 탄약 추가 (픽업 등)
        public virtual void AddCarriedAmmo(int amount)
        {
            carriedAmmo = Mathf.Min(carriedAmmo + amount, maxCarriedAmmo);
            OnAmmoChanged?.Invoke(currentAmmo, carriedAmmo);
        }

        // 현재 탄약 설정
        public virtual void SetCurrentAmmo(int amount)
        {
            currentAmmo = Mathf.Clamp(amount, 0, maxAmmo);
            OnAmmoChanged?.Invoke(currentAmmo, carriedAmmo);
        }

        // 보유 탄약 설정
        public virtual void SetCarriedAmmo(int amount)
        {
            carriedAmmo = Mathf.Clamp(amount, 0, maxCarriedAmmo);
            OnAmmoChanged?.Invoke(currentAmmo, carriedAmmo);
        }

        // 강제 재장전 중단
        public virtual void CancelReload()
        {
            if (IsReloading)
            {
                IsReloading = false;
                StopAllCoroutines();
            }
        }

        // 무기 설정
        public virtual void SetWeapon(Weapon weaponComponent)
        {
            weapon = weaponComponent;
        }

        // 디버그 정보
        public virtual string GetDebugInfo()
        {
            return $"Ammo: {currentAmmo}/{maxAmmo}, Carried: {carriedAmmo}/{maxCarriedAmmo}, Reloading: {IsReloading}";
        }

        // Inspector에서 값 변경 시 검증
        protected virtual void OnValidate()
        {
            maxAmmo = Mathf.Max(1, maxAmmo);
            startingAmmo = Mathf.Clamp(startingAmmo, 0, maxAmmo);
            maxCarriedAmmo = Mathf.Max(0, maxCarriedAmmo);
            startingCarriedAmmo = Mathf.Clamp(startingCarriedAmmo, 0, maxCarriedAmmo);
            reloadTime = Mathf.Max(0.1f, reloadTime);
            emptyReloadTime = Mathf.Max(reloadTime, emptyReloadTime);

            if (Application.isPlaying)
            {
                currentAmmo = Mathf.Clamp(currentAmmo, 0, maxAmmo);
                carriedAmmo = Mathf.Clamp(carriedAmmo, 0, maxCarriedAmmo);
            }
        }

        // 게임 세이브/로드용 데이터 구조
        [System.Serializable]
        public struct MagazineData
        {
            public int currentAmmo;
            public int carriedAmmo;
            public bool isReloading;
        }

        // 현재 상태 저장
        public virtual MagazineData GetSaveData()
        {
            return new MagazineData
            {
                currentAmmo = this.currentAmmo,
                carriedAmmo = this.carriedAmmo,
                isReloading = this.IsReloading
            };
        }

        // 저장된 상태 로드
        public virtual void LoadSaveData(MagazineData data)
        {
            currentAmmo = data.currentAmmo;
            carriedAmmo = data.carriedAmmo;
            IsReloading = data.isReloading;
            OnAmmoChanged?.Invoke(currentAmmo, carriedAmmo);
        }
    }
}