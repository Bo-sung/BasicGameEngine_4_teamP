using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Gameplay
{
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(PlayerCharacterController))]
    public class PlayerWeaponsManager : MonoBehaviour
    {
        // 무기 교체 에니메이션 현재 상태 관리
        public enum WeaponSwitchState
        {
            Up,
            Down,
            PutDownPrevious,
            PutUpNew,
        }

        [Header("Starting Weapons")]
        [Tooltip("플레이어 시작 무기")]
        public List<Weapon> StartingWeapons = new List<Weapon>();

        [Header("References")]
        [Tooltip("무기 전용 렌더링 카메라 (지오메트리 관통 방지용)")]
        public Camera WeaponCamera;

        [Tooltip("무기 장착될 소켓")]
        public Transform WeaponParentSocket;

        [Tooltip("무기 기본 위치")]
        public Transform DefaultWeaponPosition;

        [Tooltip("조준시 무기 위치")]
        public Transform AimingWeaponPosition;

        [Tooltip("무기 비활성화시 위치")]
        public Transform DownWeaponPosition;

        [Header("Weapon Bob")]
        [Tooltip("플레이어 이동 시 무기 흔들림 빈도")]
        public float BobFrequency = 10f;

        [Tooltip("무기 흔들림 적용 속도 (값이 클수록 빠름)")]
        public float BobSharpness = 10f;

        [Tooltip("비조준시 무기 흔들림")]
        public float DefaultBobAmount = 0.05f;

        [Tooltip("조준시 무기 흔들림")]
        public float AimingBobAmount = 0.02f;

        [Header("Weapon Recoil")]
        [Tooltip("무기 반동 이동 속도 (값이 클수록 빠름)")]
        public float RecoilSharpness = 50f;

        [Tooltip("무기 반동 최대 거리")]
        public float MaxRecoilDistance = 0.5f;

        [Tooltip("반동 후 무기 복귀 속도")]
        public float RecoilRestitutionSharpness = 10f;

        [Header("Misc")]
        [Tooltip("조준 애니메이션 재생 속도")]
        public float AimingAnimationSpeed = 10f;

        [Tooltip("기본 시야각")]
        public float DefaultFov = 60f;

        [Tooltip("무기 카메라 시야각 비율 (기본 FOV 대비)")]
        public float WeaponFovMultiplier = 1f;

        [Tooltip("무기 교체 지연 시간 (마우스 휠 중복 입력 방지)")]
        public float WeaponSwitchDelay = 1f;

        [Tooltip("FPS 무기용 레이어")]
        public LayerMask FpsWeaponLayer;

        // 프로퍼티
        public bool IsAiming { get; private set; }
        public bool IsPointingAtEnemy { get; private set; }
        public int ActiveWeaponIndex { get; private set; }

        // 이벤트 (System.Action 사용)
        public System.Action<Weapon> OnSwitchedToWeapon;
        public System.Action<Weapon, int> OnAddedWeapon;
        public System.Action<Weapon, int> OnRemovedWeapon;

        // 컴포넌트
        Weapon[] m_WeaponSlots = new Weapon[9]; // 기본 9슬롯
        PlayerInputHandler m_InputHandler; // 입력 핸들러
        PlayerCharacterController m_PlayerCharacterController; // 플레이어 컨트롤러

        // 변수
        float m_WeaponBobFactor;
        Vector3 m_LastCharacterPosition;
        Vector3 m_WeaponMainLocalPosition; // 무기 위치값
        Vector3 m_WeaponBobLocalPosition;
        Vector3 m_WeaponRecoilLocalPosition;
        Vector3 m_AccumulatedRecoil;
        float m_TimeStartedWeaponSwitch;
        WeaponSwitchState m_WeaponSwitchState;
        int m_WeaponSwitchNewWeaponIndex;

        void Start()
        {
            ActiveWeaponIndex = -1;
            m_WeaponSwitchState = WeaponSwitchState.Down;

            // 컴포넌트 세팅
            m_InputHandler = GetComponent<PlayerInputHandler>();
            DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, PlayerWeaponsManager>(m_InputHandler, this, gameObject);

            m_PlayerCharacterController = GetComponent<PlayerCharacterController>();
            DebugUtility.HandleErrorIfNullGetComponent<PlayerCharacterController, PlayerWeaponsManager>(m_PlayerCharacterController, this, gameObject);

            // FOV 초기화
            SetFov(DefaultFov);

            // 이벤트 세팅
            OnSwitchedToWeapon += OnWeaponSwitched;

            // 시작 무기 세팅
            foreach (var weapon in StartingWeapons)
            {
                AddWeapon(weapon);
            }

            // 무기 변경.
            SwitchWeapon(true);
        }

        void Update()
        {
            Weapon activeWeapon = GetActiveWeapon();

            // 장전중이면 패스
            if (activeWeapon != null && activeWeapon.IsReloading)
                return;

            // 무기가 활성화 상태면
            if (activeWeapon != null && m_WeaponSwitchState == WeaponSwitchState.Up)
            {
                // 장전 처리
                // 수동 장전 AND 장전버튼 눌림 AND 장전 가능
                if (m_InputHandler.GetReloadButtonDown() && activeWeapon.CanReload())
                {
                    // 조준 해제하고 장전 시작
                    IsAiming = false;
                    activeWeapon.TryStartReload();
                    return;
                }

                // 조준 여부 세팅
                IsAiming = m_InputHandler.GetAimInputHeld();

                // 발사 처리
                bool hasFired = activeWeapon.HandleShootInputs(
                    m_InputHandler.GetFireInputDown(),
                    m_InputHandler.GetFireInputHeld(),
                    m_InputHandler.GetFireInputReleased());

                // 반동 누적 처리
                if (hasFired)
                {
                    m_AccumulatedRecoil += Vector3.back * activeWeapon.RecoilForce;
                    m_AccumulatedRecoil = Vector3.ClampMagnitude(m_AccumulatedRecoil, MaxRecoilDistance);
                }
            }

            // 무기 교체 처리
            // 조준중 AND 차징중이 아님
            if (!IsAiming
                && (activeWeapon == null || !activeWeapon.IsCharging)
                && (m_WeaponSwitchState == WeaponSwitchState.Up || m_WeaponSwitchState == WeaponSwitchState.Down))
            {
                int switchWeaponInput = m_InputHandler.GetSwitchWeaponInput();
                // 무기 인풋 체크
                if (switchWeaponInput != 0)
                {
                    // 무기 교체
                    bool switchUp = switchWeaponInput > 0;
                    SwitchWeapon(switchUp);
                }
                else
                {
                    // 현재 선택된 무기로 설정
                    switchWeaponInput = m_InputHandler.GetSelectWeaponInput();
                    if (switchWeaponInput != 0)
                    {
                        if (GetWeaponAtSlotIndex(switchWeaponInput - 1) != null)
                            SwitchToWeaponIndex(switchWeaponInput - 1);
                    }
                }
            }

            // 상대 조준 체크
            IsPointingAtEnemy = false;
            if (activeWeapon)
            {
                // 레이 발사해서 상대 있는지 체크 로직
                if (Physics.Raycast(WeaponCamera.transform.position, WeaponCamera.transform.forward, out RaycastHit hit, 1000, -1, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.GetComponentInParent<Health>() != null)
                    {
                        IsPointingAtEnemy = true;
                    }
                }
            }
        }

        void LateUpdate()
        {
            // LateUpdate에서 다양한 애니메이션 기능을 업데이트
            UpdateWeaponAiming();
            UpdateWeaponBob();
            UpdateWeaponRecoil();
            UpdateWeaponSwitching();

            // 모든 결합된 애니메이션 영향을 바탕으로 최종 무기 소켓 위치 설정
            WeaponParentSocket.localPosition = m_WeaponMainLocalPosition + m_WeaponBobLocalPosition + m_WeaponRecoilLocalPosition;
        }

        /// <summary>
        /// 메인카메라와 무기 카메라 FOV 세팅
        /// </summary>
        /// <param name="fov"></param>
        public void SetFov(float fov)
        {
            m_PlayerCharacterController.PlayerCamera.fieldOfView = fov;
            WeaponCamera.fieldOfView = fov * WeaponFovMultiplier;
        }

        /// <summary>
        /// 무기 스위칭
        /// 유효한 무기로 스위칭 하기 위해서 무기 슬롯 순환
        /// </summary>
        /// <param name="ascendingOrder"> 검색 순서 </param>
        public void SwitchWeapon(bool ascendingOrder)
        {
            // 무기 슬롯 순환하며 다음 무기 찾기
            int newWeaponIndex = -1;
            int closestSlotDistance = m_WeaponSlots.Length;
            for (int i = 0; i < m_WeaponSlots.Length; i++)
            {
                // 이 슬롯의 무기가 유효하면, 활성 슬롯 인덱스로부터의 "거리"를 계산하고 지금까지 가장 가까운 거리라면 선택
                if (i != ActiveWeaponIndex && GetWeaponAtSlotIndex(i) != null)
                {
                    int distanceToActiveIndex = GetDistanceBetweenWeaponSlots(ActiveWeaponIndex, i, ascendingOrder);

                    if (distanceToActiveIndex < closestSlotDistance)
                    {
                        closestSlotDistance = distanceToActiveIndex;
                        newWeaponIndex = i;
                    }
                }
            }

            // 찾은 인덱스로 스위칭
            SwitchToWeaponIndex(newWeaponIndex);
        }

        /// <summary>
        /// 새 인덱스가 현재 무기와 다른 유효한 무기라면 무기 슬롯의 해당 무기 인덱스로 전환
        /// </summary>
        /// <param name="newWeaponIndex"></param>
        /// <param name="force"></param>
        public void SwitchToWeaponIndex(int newWeaponIndex, bool force = false)
        {
            if (force || (newWeaponIndex != ActiveWeaponIndex && newWeaponIndex >= 0))
            {
                // 무기 교체 애니메이션 관련 데이터 저장
                m_WeaponSwitchNewWeaponIndex = newWeaponIndex;
                m_TimeStartedWeaponSwitch = Time.time;

                // 처음으로 유효한 무기로 전환하는 경우 처리
                if (GetActiveWeapon() == null)
                {
                    m_WeaponMainLocalPosition = DownWeaponPosition.localPosition;
                    m_WeaponSwitchState = WeaponSwitchState.PutUpNew;
                    ActiveWeaponIndex = m_WeaponSwitchNewWeaponIndex;

                    Weapon newWeapon = GetWeaponAtSlotIndex(m_WeaponSwitchNewWeaponIndex);
                    OnSwitchedToWeapon?.Invoke(newWeapon);
                }
                // 그렇지 않으면 다음 무기로 전환하기 위해 현재 무기를 내림
                else
                {
                    m_WeaponSwitchState = WeaponSwitchState.PutDownPrevious;
                }
            }
        }

        /// <summary>
        /// WeaponController 가져오기
        /// </summary>
        /// <param name="weaponPrefab"></param>
        /// <returns></returns>
        public Weapon GetWeapon(Weapon weaponPrefab)
        {
            // 지정된 프리팹에서 나온 무기를 이미 가지고 있는지 확인
            for (var index = 0; index < m_WeaponSlots.Length; index++)
            {
                var w = m_WeaponSlots[index];
                if (w != null && w.SourcePrefab == weaponPrefab.gameObject)
                {
                    return w;
                }
            }

            return null;
        }

        /// <summary>
        /// 조준 전환을 위해 무기 위치와 카메라 FoV 업데이트
        /// </summary>
        void UpdateWeaponAiming()
        {
            if (m_WeaponSwitchState == WeaponSwitchState.Up)
            {
                Weapon activeWeapon = GetActiveWeapon();
                if (IsAiming && activeWeapon)   // 조준중일때
                {
                    // 무기위치 -> 조준 위치로 이동
                    m_WeaponMainLocalPosition = Vector3.Lerp(m_WeaponMainLocalPosition,
                        AimingWeaponPosition.localPosition + activeWeapon.AimOffset,
                        AimingAnimationSpeed * Time.deltaTime);
                    // 카메라 FOV 값 변경 (줌인)
                    SetFov(Mathf.Lerp(m_PlayerCharacterController.PlayerCamera.fieldOfView,
                        activeWeapon.AimZoomRatio * DefaultFov,
                        AimingAnimationSpeed * Time.deltaTime));
                }
                else
                {
                    // 무기위치 -> 기본 위치로 이동
                    m_WeaponMainLocalPosition = Vector3.Lerp(m_WeaponMainLocalPosition,
                        DefaultWeaponPosition.localPosition,
                        AimingAnimationSpeed * Time.deltaTime);
                    // 카메라 FOV 값 변경 (기본값)
                    SetFov(Mathf.Lerp(m_PlayerCharacterController.PlayerCamera.fieldOfView,
                        DefaultFov,
                        AimingAnimationSpeed * Time.deltaTime));
                }
            }
        }

        /// <summary>
        /// 캐릭터 속도를 기반으로 무기 흔들림 애니메이션 업데이트
        /// </summary>
        void UpdateWeaponBob()
        {
            if (Time.deltaTime > 0f)
            {
                // 현재 프레임과 이전 프레임 위치 차이로 실제 속도 계산
                Vector3 playerCharacterVelocity = (m_PlayerCharacterController.transform.position - m_LastCharacterPosition) / Time.deltaTime;

                // 이동 강도 계산 (0~1 범위)
                float characterMovementFactor = 0f;

                // 땅에 있을 때만 흔들림 적용
                if (m_PlayerCharacterController.IsGrounded)
                {
                    characterMovementFactor = Mathf.Clamp01(
                        playerCharacterVelocity.magnitude /
                        (m_PlayerCharacterController.MaxSpeedOnGround * m_PlayerCharacterController.SprintSpeedModifier)
                    );
                }

                // 현재 흔들림 팩터를 목표값으로 부드럽게 보간
                m_WeaponBobFactor = Mathf.Lerp(m_WeaponBobFactor, characterMovementFactor, BobSharpness * Time.deltaTime);

                // 조준 상태에 따른 흔들림 크기 설정
                float bobAmount = IsAiming ? AimingBobAmount : DefaultBobAmount;
                float frequency = BobFrequency;

                // 좌우 흔들림: 기본 사인파 사용
                float hBobValue = Mathf.Sin(Time.time * frequency) * bobAmount * m_WeaponBobFactor;

                // 상하 흔들림: 2배 빠른 주파수 + 항상 양수로 변환
                float vBobValue = ((Mathf.Sin(Time.time * frequency * 2f) * 0.5f) + 0.5f) * bobAmount * m_WeaponBobFactor;

                // 계산된 흔들림 값을 무기 위치에 적용
                m_WeaponBobLocalPosition.x = hBobValue;
                m_WeaponBobLocalPosition.y = Mathf.Abs(vBobValue);

                // 다음 프레임을 위해 현재 위치 저장
                m_LastCharacterPosition = m_PlayerCharacterController.transform.position;
            }
        }

        /// <summary>
        /// 무기 반동 애니메이션 업데이트
        /// </summary>
        void UpdateWeaponRecoil()
        {
            if (m_WeaponRecoilLocalPosition.z >= m_AccumulatedRecoil.z * 0.99f)
            {
                m_WeaponRecoilLocalPosition = Vector3.Lerp(m_WeaponRecoilLocalPosition, m_AccumulatedRecoil,
                    RecoilSharpness * Time.deltaTime);
            }
            else
            {
                m_WeaponRecoilLocalPosition = Vector3.Lerp(m_WeaponRecoilLocalPosition, Vector3.zero,
                    RecoilRestitutionSharpness * Time.deltaTime);
                m_AccumulatedRecoil = m_WeaponRecoilLocalPosition;
            }
        }

        /// <summary>
        /// 무기 교체 애니메이션 업데이트
        /// </summary>
        void UpdateWeaponSwitching()
        {
            // 무기 교체 트리거 이후 시간 비율 계산 (0 to 1)
            float switchingTimeFactor = 0f;
            if (WeaponSwitchDelay == 0f)
            {
                switchingTimeFactor = 1f;
            }
            else
            {
                switchingTimeFactor = Mathf.Clamp01((Time.time - m_TimeStartedWeaponSwitch) / WeaponSwitchDelay);
            }

            // 새로운 교체 상태로 전환 처리
            if (switchingTimeFactor >= 1f)
            {
                if (m_WeaponSwitchState == WeaponSwitchState.PutDownPrevious)
                {
                    // 이전 무기 비활성화
                    Weapon oldWeapon = GetWeaponAtSlotIndex(ActiveWeaponIndex);
                    if (oldWeapon != null)
                    {
                        oldWeapon.ShowWeapon(false);
                    }

                    ActiveWeaponIndex = m_WeaponSwitchNewWeaponIndex;
                    switchingTimeFactor = 0f;

                    // 새 무기 활성화
                    Weapon newWeapon = GetWeaponAtSlotIndex(ActiveWeaponIndex);
                    OnSwitchedToWeapon?.Invoke(newWeapon);

                    if (newWeapon)
                    {
                        m_TimeStartedWeaponSwitch = Time.time;
                        m_WeaponSwitchState = WeaponSwitchState.PutUpNew;
                    }
                    else
                    {
                        m_WeaponSwitchState = WeaponSwitchState.Down;
                    }
                }
                else if (m_WeaponSwitchState == WeaponSwitchState.PutUpNew)
                {
                    m_WeaponSwitchState = WeaponSwitchState.Up;
                }
            }

            // 애니메이션된 무기 교체를 위한 무기 소켓 위치 이동 처리
            if (m_WeaponSwitchState == WeaponSwitchState.PutDownPrevious)
            {
                m_WeaponMainLocalPosition = Vector3.Lerp(DefaultWeaponPosition.localPosition,
                    DownWeaponPosition.localPosition, switchingTimeFactor);
            }
            else if (m_WeaponSwitchState == WeaponSwitchState.PutUpNew)
            {
                m_WeaponMainLocalPosition = Vector3.Lerp(DownWeaponPosition.localPosition,
                    DefaultWeaponPosition.localPosition, switchingTimeFactor);
            }
        }

        /// <summary>
        /// 인벤토리에 무기 추가
        /// </summary>
        /// <param name="weaponPrefab"></param>
        /// <returns></returns>
        public bool AddWeapon(Weapon weaponPrefab)
        {
            // 이미 같은 타입의 무기를 가지고 있다면 추가하지 않음
            if (GetWeapon(weaponPrefab) != null)
            {
                return false;
            }

            // 빈 무기 슬롯을 찾아서 무기 할당
            for (int i = 0; i < m_WeaponSlots.Length; i++)
            {
                if (m_WeaponSlots[i] == null)
                {
                    // 무기 프리팹을 무기 소켓의 자식으로 생성
                    Weapon weaponInstance = Instantiate(weaponPrefab, WeaponParentSocket);
                    weaponInstance.transform.localPosition = Vector3.zero;
                    weaponInstance.transform.localRotation = Quaternion.identity;

                    // 소유자 설정
                    weaponInstance.Owner = gameObject;
                    weaponInstance.SourcePrefab = weaponPrefab.gameObject;
                    weaponInstance.ShowWeapon(false);

                    // FPS 무기 레이어 할당
                    int layerIndex = Mathf.RoundToInt(Mathf.Log(FpsWeaponLayer.value, 2));
                    foreach (Transform t in weaponInstance.gameObject.GetComponentsInChildren<Transform>(true))
                    {
                        t.gameObject.layer = layerIndex;
                    }

                    m_WeaponSlots[i] = weaponInstance;

                    OnAddedWeapon?.Invoke(weaponInstance, i);

                    // 현재 활성 무기가 없다면 자동으로 교체
                    if (GetActiveWeapon() == null)
                    {
                        SwitchWeapon(true);
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 무기 제거
        /// </summary>
        /// <param name="weaponInstance"></param>
        /// <returns></returns>
        public bool RemoveWeapon(Weapon weaponInstance)
        {
            // 슬롯에서 해당 무기 찾기
            for (int i = 0; i < m_WeaponSlots.Length; i++)
            {
                if (m_WeaponSlots[i] == weaponInstance)
                {
                    m_WeaponSlots[i] = null;

                    OnRemovedWeapon?.Invoke(weaponInstance, i);

                    Destroy(weaponInstance.gameObject);

                    // 활성 무기를 제거하는 경우 다음 무기로 교체
                    if (i == ActiveWeaponIndex)
                    {
                        SwitchWeapon(true);
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 활성화 된 무기 가져오기
        /// </summary>
        public Weapon GetActiveWeapon()
        {
            return GetWeaponAtSlotIndex(ActiveWeaponIndex);
        }

        /// <summary>
        /// 특정 슬롯 인덱스의 무기 가져오기
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Weapon GetWeaponAtSlotIndex(int index)
        {
            if (index >= 0 && index < m_WeaponSlots.Length)
            {
                return m_WeaponSlots[index];
            }

            return null;
        }

        /// <summary>
        /// 두 무기 슬롯 인덱스 간의 "거리" 계산
        /// </summary>
        /// <param name="fromSlotIndex"></param>
        /// <param name="toSlotIndex"></param>
        /// <param name="ascendingOrder"></param>
        /// <returns></returns>
        int GetDistanceBetweenWeaponSlots(int fromSlotIndex, int toSlotIndex, bool ascendingOrder)
        {
            int distanceBetweenSlots = 0;

            if (ascendingOrder)
            {
                distanceBetweenSlots = toSlotIndex - fromSlotIndex;
            }
            else
            {
                distanceBetweenSlots = -1 * (toSlotIndex - fromSlotIndex);
            }

            if (distanceBetweenSlots < 0)
            {
                distanceBetweenSlots = m_WeaponSlots.Length + distanceBetweenSlots;
            }

            return distanceBetweenSlots;
        }

        /// <summary>
        /// 무기 교체 시 호출되는 이벤트 핸들러
        /// </summary>
        /// <param name="newWeapon"></param>
        void OnWeaponSwitched(Weapon newWeapon)
        {
            if (newWeapon != null)
            {
                newWeapon.ShowWeapon(true);
            }
        }
    }
}