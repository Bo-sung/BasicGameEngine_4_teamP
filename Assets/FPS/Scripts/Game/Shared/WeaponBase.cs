using UnityEngine;
using FPS.Common;

namespace Unity.FPS.Game
{

    // ========== 무기 베이스 클래스 ==========
    /// <summary>
    /// 데이터 테이블 연동이 가능한 무기 베이스 클래스
    /// WeaponController를 상속받아 기능 그대로 사용.
    /// </summary>
    public class WeaponBase : WeaponController
    {
        [Header("=== 무기 설정 방식 선택 ===")]
        [Tooltip("데이터 테이블 사용 여부")]
        public bool useDataTable = true;

        [Tooltip("데이터 테이블에서 사용할 무기 ID")]
        public string weaponID = "pistol_basic";

        [Header("=== 직접 설정 (테이블 미사용시) ===")]
        public WeaponTableData directData;

        // ========== 내부 변수들 ==========
        protected WeaponTableData currentWeaponData;
        protected ProjectileTableData currentProjectileData;
        protected bool isInitialized = false;

        // ========== 현재 상태 정보 ==========
        public int CurrentAmmo => GetCurrentAmmo();
        public bool CanShoot => CurrentAmmo > 0 && !IsReloading;

        // ========== Unity 생명주기 ==========
        protected virtual void Start()
        {
            InitializeWeapon();
        }

        // ========== 초기화 ==========
        private void InitializeWeapon()
        {
            // 데이터 로드
            LoadWeaponData();

            // 설정 적용
            ApplyWeaponSettings();

            // 발사체 설정
            SetupProjectile();

            // 에셋 로드
            LoadWeaponAssets();

            // 커스텀 초기화
            OnWeaponInitialized();

            isInitialized = true;
        }

        [ContextMenu("InitNow")]
        void InitNow()
        {
            // 데이터 로드
            LoadWeaponData();

            // 설정 적용
            ApplyWeaponSettings();

            // 발사체 설정
            SetupProjectile();

            // 에셋 로드
            LoadWeaponAssets();

            // 커스텀 초기화
            OnWeaponInitialized();
        }

        /// <summary>
        /// 데이터 세팅
        /// </summary>
        private void LoadWeaponData()
        {
            if (useDataTable)
            {
                // 테이블에서 무기 데이터 로드
                currentWeaponData = Table.Weapon.GetData(weaponID);
                if (currentWeaponData == null)
                {
                    Debug.LogError($"무기 데이터를 찾을 수 없습니다: {weaponID}");
                    // 기본값으로 대체
                    currentWeaponData = CreateDefaultWeaponData();
                }

                // 발사체 데이터도 로드
                currentProjectileData = ProjectileDataTable.GetProjectileData(currentWeaponData.projectileID);

            }
            else
            {
                // 직접 설정된 데이터 사용
                currentWeaponData = directData;
                currentProjectileData = CreateDefaultProjectileData();
            }
        }

        /// <summary>
        /// 무기 세팅 적용
        /// </summary>
        private void ApplyWeaponSettings()
        {
            WeaponName = currentWeaponData.weaponName;
            ShootType = currentWeaponData.shootType;
            DelayBetweenShots = currentWeaponData.fireRate;
            BulletsPerShot = currentWeaponData.bulletsPerShot;
            BulletSpreadAngle = currentWeaponData.spread;
            MaxAmmo = currentWeaponData.maxAmmo;
            RecoilForce = currentWeaponData.recoil;

            // 재장전 관련
            AutomaticReload = currentWeaponData.autoReload;
            AmmoReloadRate = currentWeaponData.maxAmmo / currentWeaponData.reloadSpeed;
            AmmoReloadDelay = currentWeaponData.reloadDelay;
        }

        /// <summary>
        /// 발사체 셋업
        /// </summary>
        private void SetupProjectile()
        {
            GameObject projectilePrefab = null;

            if (currentProjectileData != null && !string.IsNullOrEmpty(currentProjectileData.prefabPath))
            {
                // 테이블에서 지정된 발사체 프리팹 로드
                projectilePrefab = ResourceManager.LoadPrefab(currentProjectileData.prefabPath);
            }

            if (projectilePrefab != null)
            {
                // 프리팹에서 ProjectileBase 컴포넌트 가져오기
                ProjectileBase projectileComponent = projectilePrefab.GetComponent<ProjectileBase>();
                if (projectileComponent != null)
                {
                    ProjectilePrefab = projectileComponent;
                }
                else
                {
                    Debug.LogError($"발사체 프리팹에 ProjectileBase 컴포넌트가 없습니다: {currentProjectileData.prefabPath}");
                    CreateDefaultProjectile();
                }
            }
            else
            {
                // 발사체를 찾을 수 없으면 런타임에 기본 발사체 생성
                CreateDefaultProjectile();
            }
        }

        /// <summary>
        /// 발사체 없을때 대비. 기본값 생성
        /// </summary>
        private void CreateDefaultProjectile()
        {
            // 런타임에 기본 발사체 생성
            GameObject defaultProjectile = new GameObject($"{currentWeaponData.weaponName}_Projectile");
            var projectileComponent = defaultProjectile.AddComponent<DefaultProjectile>();

            // 데이터 테이블 값 또는 기본값 적용
            if (currentProjectileData != null)
            {
                projectileComponent.Speed = currentProjectileData.speed;
                projectileComponent.MaxLifeTime = currentProjectileData.lifetime;
                projectileComponent.Damage = currentProjectileData.damage;
            }
            else
            {
                projectileComponent.Speed = currentWeaponData.projectileSpeed;
                projectileComponent.MaxLifeTime = currentWeaponData.projectileLifetime;
                projectileComponent.Damage = currentWeaponData.damage;
            }

            // 프리팹으로 만들기 위해 비활성화
            defaultProjectile.SetActive(false);

            ProjectilePrefab = projectileComponent;
        }

        /// <summary>
        /// 무기 에셋 로드
        /// </summary>
        private void LoadWeaponAssets()
        {
            // 사운드 로드
            if (!string.IsNullOrEmpty(currentWeaponData.fireSoundPath))
            {
                ShootSfx = ResourceManager.LoadAudioClip(currentWeaponData.fireSoundPath);
            }

            // 총구 섬광 이펙트 로드
            if (!string.IsNullOrEmpty(currentWeaponData.muzzleFlashPath))
            {
                MuzzleFlashPrefab = ResourceManager.LoadPrefab(currentWeaponData.muzzleFlashPath);
            }
        }

        // ========== 무기 사용 함수들 ==========

        /// <summary>
        /// 무기 발사 시도
        /// </summary>
        public virtual bool TryFire()
        {
            if (!CanShoot) return false;

            bool fired = HandleShootInputs(true, false, false);

            if (fired)
            {
                OnWeaponFired();
            }

            return fired;
        }

        /// <summary>
        /// 연사 무기용 - 계속 발사
        /// </summary>
        public virtual bool TryFireContinuous()
        {
            if (!CanShoot) return false;

            bool fired = HandleShootInputs(false, true, false);

            if (fired)
            {
                OnWeaponFired();
            }

            return fired;
        }

        /// <summary>
        /// 발사 중단
        /// </summary>
        public virtual void StopFiring()
        {
            HandleShootInputs(false, false, true);
        }

        /// <summary>
        /// 수동 재장전
        /// </summary>
        public virtual void Reload()
        {
            StartReloadAnimation();
        }

        /// <summary>
        /// 무기 활성화/비활성화
        /// </summary>
        public virtual void SetActive(bool active)
        {
            ShowWeapon(active);
        }

        /// <summary>
        /// 런타임에 무기 변경
        /// </summary>
        public virtual void ChangeWeapon(string newWeaponID)
        {
            weaponID = newWeaponID;
            useDataTable = true;

            if (isInitialized)
            {
                InitializeWeapon();
            }
        }

        // ========== 데이터 생성 헬퍼 함수들 ==========

        /// <summary>
        /// 무기 데이터 초기값
        /// </summary>
        private WeaponTableData CreateDefaultWeaponData()
        {
            return new WeaponTableData
            {
                weaponID = "default",
                weaponName = "기본 무기",
                shootType = WeaponShootType.Manual,
                fireRate = 1f,
                bulletsPerShot = 1,
                spread = 0f,
                maxAmmo = 30,
                recoil = 0.5f,
                damage = 25f,
                projectileID = "default",
                projectileSpeed = 100f,
                projectileLifetime = 5f
            };
        }

        /// <summary>
        /// 발사체 데이터 초기값
        /// </summary>
        /// <returns></returns>
        private ProjectileTableData CreateDefaultProjectileData()
        {
            return new ProjectileTableData
            {
                projectileID = "default",
                projectileName = "기본 총알",
                speed = 100f,
                lifetime = 5f,
                damage = 25f
            };
        }

        // ========== 상속받은 클래스에서 구현할 함수들 ==========

        /// <summary>
        /// 무기 초기화 후 호출되는 함수
        /// </summary>
        protected virtual void OnWeaponInitialized()
        {
            Debug.Log($"{WeaponName} 초기화 완료!");
        }

        /// <summary>
        /// 무기 발사 시마다 호출되는 함수
        /// </summary>
        protected virtual void OnWeaponFired()
        {
            // 예: 특별한 이펙트, 화면 흔들림 등
        }

        // ========== 디버그 정보 ==========
        private void OnDrawGizmosSelected()
        {
            if (WeaponMuzzle != null)
            {
                // 발사 방향 표시
                Gizmos.color = Color.red;
                Gizmos.DrawRay(WeaponMuzzle.position, WeaponMuzzle.forward * 10f);

                // 탄 퍼짐 범위 표시
                if (currentWeaponData != null && currentWeaponData.spread > 0)
                {
                    Gizmos.color = Color.yellow;
                    Vector3 spreadLeft = Quaternion.AngleAxis(-currentWeaponData.spread, Vector3.up) * WeaponMuzzle.forward * 10f;
                    Vector3 spreadRight = Quaternion.AngleAxis(currentWeaponData.spread, Vector3.up) * WeaponMuzzle.forward * 10f;
                    Gizmos.DrawRay(WeaponMuzzle.position, spreadLeft);
                    Gizmos.DrawRay(WeaponMuzzle.position, spreadRight);
                }
            }
        }
    }
}