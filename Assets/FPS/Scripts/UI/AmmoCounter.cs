using TMPro;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    [RequireComponent(typeof(FillBarColorChange))]
    public class AmmoCounter : MonoBehaviour
    {
        [Tooltip("CanvasGroup to fade the ammo UI")]
        public CanvasGroup CanvasGroup;

        [Tooltip("Image for the weapon icon")]
        public Image WeaponImage;

        [Tooltip("Image component for the background")]
        public Image AmmoBackgroundImage;

        [Tooltip("Image component to display fill ratio")]
        public Image AmmoFillImage;

        [Tooltip("Text for Weapon index")]
        public TextMeshProUGUI WeaponIndexText;

        [Tooltip("Text for Bullet Counter")]
        public TextMeshProUGUI BulletCounter;

        [Tooltip("Reload Text for Weapons with physical bullets")]
        public RectTransform Reload;

        [Header("Selection")]
        [Range(0, 1)]
        [Tooltip("Opacity when weapon not selected")]
        public float UnselectedOpacity = 0.5f;

        [Tooltip("Scale when weapon not selected")]
        public Vector3 UnselectedScale = Vector3.one * 0.8f;

        [Tooltip("Root for the control keys")]
        public GameObject ControlKeysRoot;

        [Header("Feedback")]
        [Tooltip("Component to animate the color when empty or full")]
        public FillBarColorChange FillBarColorChange;

        [Tooltip("Sharpness for the fill ratio movements")]
        public float AmmoFillMovementSharpness = 20f;

        public int WeaponCounterIndex { get; set; }

        PlayerWeaponsManager m_PlayerWeaponsManager;
        Weapon m_WeaponController;
        MagazineBase m_Magazine;

        void Awake()
        {
            EventManager.AddListener<AmmoPickupEvent>(OnAmmoPickup);
        }

        void OnAmmoPickup(AmmoPickupEvent evt)
        {
            if (evt.Weapon == m_WeaponController)
            {
                UpdateBulletCounterText();
            }
        }

        public void Initialize(Weapon weaponController, int weaponIndex)
        {
            m_WeaponController = weaponController;
            m_Magazine = weaponController.GetMagazine();
            WeaponCounterIndex = weaponIndex;

            // 무기 아이콘 설정
            WeaponImage.sprite = weaponController.WeaponIcon;

            // Magazine 타입에 따른 UI 설정
            if (m_Magazine != null)
            {
                // 자동 재생성 타입은 탄약 카운터 숨김
                if (m_Magazine.reloadType == ReloadType.Automatic)
                {
                    BulletCounter.transform.parent.gameObject.SetActive(false);
                }
                else
                {
                    BulletCounter.transform.parent.gameObject.SetActive(true);
                    UpdateBulletCounterText();
                }
            }
            else
            {
                // Magazine이 없으면 기본값으로 표시
                UpdateBulletCounterText();
            }

            Reload.gameObject.SetActive(false);

            m_PlayerWeaponsManager = FindFirstObjectByType<PlayerWeaponsManager>();
            DebugUtility.HandleErrorIfNullFindObject<PlayerWeaponsManager, AmmoCounter>(m_PlayerWeaponsManager, this);

            WeaponIndexText.text = (WeaponCounterIndex + 1).ToString();

            // FillBarColorChange 초기화
            FillBarColorChange.Initialize(1f, m_WeaponController.GetAmmoNeededToShoot());

            // Magazine 이벤트 구독
            if (m_Magazine != null)
            {
                m_Magazine.OnAmmoChanged += OnAmmoChanged;
                m_Magazine.OnReloadStarted += OnReloadStarted;
                m_Magazine.OnReloadCompleted += OnReloadCompleted;
            }
        }

        void OnAmmoChanged(int currentAmmo, int carriedAmmo)
        {
            UpdateBulletCounterText();
        }

        void OnReloadStarted()
        {
            // 재장전 시작 시 UI 업데이트
            Reload.gameObject.SetActive(false);
        }

        void OnReloadCompleted()
        {
            // 재장전 완료 시 UI 업데이트
            UpdateBulletCounterText();
        }

        void UpdateBulletCounterText()
        {
            if (m_WeaponController != null)
            {
                BulletCounter.text = m_WeaponController.GetCarriedPhysicalBullets().ToString();
            }
        }

        void Update()
        {
            if (m_WeaponController == null) return;

            // 현재 탄약 비율로 Fill Image 업데이트
            float currentFillRatio = m_WeaponController.CurrentAmmoRatio;
            AmmoFillImage.fillAmount = Mathf.Lerp(AmmoFillImage.fillAmount, currentFillRatio,
                Time.deltaTime * AmmoFillMovementSharpness);

            // 탄약 카운터 업데이트 (매 프레임 - 실시간 변화 반영)
            UpdateBulletCounterText();

            // 활성 무기 여부 확인
            bool isActiveWeapon = m_WeaponController == m_PlayerWeaponsManager.GetActiveWeapon();

            // 활성 무기에 따른 UI 스케일링 및 투명도
            CanvasGroup.alpha = Mathf.Lerp(CanvasGroup.alpha, isActiveWeapon ? 1f : UnselectedOpacity,
                Time.deltaTime * 10);
            transform.localScale = Vector3.Lerp(transform.localScale, isActiveWeapon ? Vector3.one : UnselectedScale,
                Time.deltaTime * 10);

            // 컨트롤 키 표시 (비활성 무기에만)
            ControlKeysRoot.SetActive(!isActiveWeapon);

            // Fill Bar 색상 업데이트
            FillBarColorChange.UpdateVisual(currentFillRatio);

            // 재장전 UI 표시 조건
            bool shouldShowReload = false;

            if (m_Magazine != null)
            {
                // Magazine 시스템: 재장전 가능하고 현재 탄약이 부족할 때
                shouldShowReload = m_Magazine.CanReload() &&
                                  m_Magazine.CurrentAmmo == 0 &&
                                  m_Magazine.CarriedAmmo > 0 &&
                                  m_WeaponController.IsWeaponActive;
            }
            else
            {
                // 기존 시스템 호환
                shouldShowReload = m_WeaponController.GetCarriedPhysicalBullets() > 0 &&
                                  m_WeaponController.GetCurrentAmmo() == 0 &&
                                  m_WeaponController.IsWeaponActive;
            }

            Reload.gameObject.SetActive(shouldShowReload);
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<AmmoPickupEvent>(OnAmmoPickup);

            // Magazine 이벤트 구독 해제
            if (m_Magazine != null)
            {
                m_Magazine.OnAmmoChanged -= OnAmmoChanged;
                m_Magazine.OnReloadStarted -= OnReloadStarted;
                m_Magazine.OnReloadCompleted -= OnReloadCompleted;
            }
        }

        // 디버그용: Magazine 정보 표시
        public string GetMagazineDebugInfo()
        {
            return m_Magazine?.GetDebugInfo() ?? "No Magazine";
        }

        // 특정 Magazine 타입인지 확인
        public bool IsMagazineType<T>() where T : MagazineBase
        {
            return m_Magazine is T;
        }

        // Magazine 타입별 특수 UI 업데이트
        void UpdateSpecialUI()
        {
            if (m_Magazine == null) return;

            switch (m_Magazine.reloadType)
            {
                case ReloadType.Automatic:
                    // 자동 재생성: 오버히트 정보 표시 등
                    if (m_Magazine is AutoRegeneratingMagazine autoMag)
                    {
                        // 오버히트 상태에 따른 UI 변경
                        if (autoMag.IsOverheated)
                        {
                            // 빨간색 표시 등
                        }
                    }
                    break;

                case ReloadType.Individual:
                    // 개별 장전: 장전 진행 상황 표시 등
                    break;

                case ReloadType.BeltFed:
                    // 벨트 급탄: 벨트 길이 표시 등
                    if (m_Magazine is BeltFedMagazine beltMag)
                    {
                        // 벨트 상태 표시
                    }
                    break;
            }
        }
    }
}