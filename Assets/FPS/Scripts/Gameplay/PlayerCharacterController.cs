
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Gameplay
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputHandler), typeof(AudioSource))]
    public class PlayerCharacterController : MonoBehaviour
    {
        [Header("참조")]
        // Reference to the main camera used for the player
        [Tooltip("플레이어에 사용되는 메인 카메라 참조")]
        public Camera PlayerCamera;

        // Audio source for footsteps, jump, etc...
        [Tooltip("발소리, 점프 등을 위한 오디오 소스")]
        public AudioSource AudioSource;

        [Header("일반")]
        // Force applied downward when in the air
        [Tooltip("공중에 있을 때 아래로 가해지는 힘")]
        public float GravityDownForce = 20f;

        // Physic layers checked to consider the player grounded
        [Tooltip("플레이어가 지면에 있는지 확인하기 위해 체크할 물리 레이어")]
        public LayerMask GroundCheckLayers = -1;

        // distance from the bottom of the character controller capsule to test for grounded
        [Tooltip("캐릭터 컨트롤러 캡슐 하단에서 지면 테스트를 위한 거리")]
        public float GroundCheckDistance = 0.05f;

        [Header("이동")]
        // Max movement speed when grounded (when not sprinting)
        [Tooltip("지면에 있을 때 최대 이동 속도 (질주하지 않을 때)")]
        public float MaxSpeedOnGround = 10f;

        // Sharpness for the movement when grounded, a low value will make the player accelerate and decelerate slowly, a high value will do the opposite
        [Tooltip("지면에서의 이동 민감도, 낮은 값은 플레이어를 천천히 가속/감속시키고, 높은 값은 그 반대")]
        public float MovementSharpnessOnGround = 15;

        // Max movement speed when crouching
        [Tooltip("웅크릴 때 최대 이동 속도")]
        [Range(0, 1)]
        public float MaxSpeedCrouchedRatio = 0.5f;

        // Max movement speed when not grounded
        [Tooltip("지면에 있지 않을 때 최대 이동 속도")]
        public float MaxSpeedInAir = 10f;

        // Acceleration speed when in the air
        [Tooltip("공중에서의 가속 속도")]
        public float AccelerationSpeedInAir = 25f;

        // Multiplicator for the sprint speed (based on grounded speed)
        [Tooltip("질주 속도 배율 (지면 속도 기준)")]
        public float SprintSpeedModifier = 2f;

        // Height at which the player dies instantly when falling off the map
        [Tooltip("맵에서 떨어졌을 때 플레이어가 즉시 죽는 높이")]
        public float KillHeight = -50f;

        [Header("회전")]
        // Rotation speed for moving the camera
        [Tooltip("카메라 이동을 위한 회전 속도")]
        public float RotationSpeed = 200f;

        // Rotation speed multiplier when aiming
        [Range(0.1f, 1f)]
        [Tooltip("조준 시 회전 속도 배율")]
        public float AimingRotationMultiplier = 0.4f;

        [Header("점프")]
        // Force applied upward when jumping
        [Tooltip("점프 시 위로 가해지는 힘")]
        public float JumpForce = 9f;

        [Header("자세")]
        // Ratio (0-1) of the character height where the camera will be at
        [Tooltip("카메라가 위치할 캐릭터 높이의 비율 (0-1)")]
        public float CameraHeightRatio = 0.9f;

        // Height of character when standing
        [Tooltip("서 있을 때 캐릭터 높이")]
        public float CapsuleHeightStanding = 1.8f;

        // Height of character when crouching
        [Tooltip("웅크릴 때 캐릭터 높이")]
        public float CapsuleHeightCrouching = 0.9f;

        // Speed of crouching transitions
        [Tooltip("웅크리기 전환 속도")]
        public float CrouchingSharpness = 10f;

        [Header("오디오")]
        // Amount of footstep sounds played when moving one meter
        [Tooltip("1미터 이동 시 재생되는 발소리 수")]
        public float FootstepSfxFrequency = 1f;

        // Amount of footstep sounds played when moving one meter while sprinting
        [Tooltip("질주 중 1미터 이동 시 재생되는 발소리 수")]
        public float FootstepSfxFrequencyWhileSprinting = 1f;

        // Sound played for footsteps
        [Tooltip("발소리로 재생되는 사운드")]
        public AudioClip FootstepSfx;

        // Sound played when jumping
        [Tooltip("점프 시 재생되는 사운드")]
        public AudioClip JumpSfx;

        // Sound played when landing
        [Tooltip("착지 시 재생되는 사운드")]
        public AudioClip LandSfx;

        // Sound played when taking damage froma fall
        [Tooltip("낙하 데미지를 받을 때 재생되는 사운드")]
        public AudioClip FallDamageSfx;

        [Header("낙하 데미지")]
        // Whether the player will recieve damage when hitting the ground at high speed
        [Tooltip("플레이어가 높은 속도로 지면에 부딪힐 때 데미지를 받을지 여부")]
        public bool RecievesFallDamage;

        // Minimun fall speed for recieving fall damage
        [Tooltip("낙하 데미지를 받는 최소 낙하 속도")]
        public float MinSpeedForFallDamage = 10f;

        // Fall speed for recieving th emaximum amount of fall damage
        [Tooltip("최대 낙하 데미지를 받는 낙하 속도")]
        public float MaxSpeedForFallDamage = 30f;

        // Damage recieved when falling at the mimimum speed
        [Tooltip("최소 속도로 낙하 시 받는 데미지")]
        public float FallDamageAtMinSpeed = 10f;

        // Damage recieved when falling at the maximum speed
        [Tooltip("최대 속도로 낙하 시 받는 데미지")]
        public float FallDamageAtMaxSpeed = 50f;

        public UnityAction<bool> OnStanceChanged;

        public Vector3 CharacterVelocity { get; set; }
        public bool IsGrounded { get; private set; }
        public bool HasJumpedThisFrame { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsCrouching { get; private set; }

        public float RotationMultiplier
        {
            get
            {
                if (m_WeaponsManager.IsAiming)
                {
                    return AimingRotationMultiplier;
                }

                return 1f;
            }
        }

        Health m_Health;
        PlayerInputHandler m_InputHandler;
        CharacterController m_Controller;
        PlayerWeaponsManager m_WeaponsManager;
        Actor m_Actor;
        Vector3 m_GroundNormal;
        Vector3 m_CharacterVelocity;
        Vector3 m_LatestImpactSpeed;
        float m_LastTimeJumped = 0f;
        float m_CameraVerticalAngle = 0f;
        float m_FootstepDistanceCounter;
        float m_TargetCharacterHeight;

        const float k_JumpGroundingPreventionTime = 0.2f;
        const float k_GroundCheckDistanceInAir = 0.07f;

        void Awake()
        {
            ActorsManager actorsManager = FindFirstObjectByType<ActorsManager>();
            if (actorsManager != null)
                actorsManager.SetPlayer(gameObject);
        }

        void Start()
        {
            // 같은 게임오브젝트의 컴포넌트 가져오기
            m_Controller = GetComponent<CharacterController>();
            DebugUtility.HandleErrorIfNullGetComponent<CharacterController, PlayerCharacterController>(m_Controller, this, gameObject);

            m_InputHandler = GetComponent<PlayerInputHandler>();
            DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, PlayerCharacterController>(m_InputHandler, this, gameObject);

            m_WeaponsManager = GetComponent<PlayerWeaponsManager>();
            DebugUtility.HandleErrorIfNullGetComponent<PlayerWeaponsManager, PlayerCharacterController>(m_WeaponsManager, this, gameObject);

            m_Health = GetComponent<Health>();
            DebugUtility.HandleErrorIfNullGetComponent<Health, PlayerCharacterController>(m_Health, this, gameObject);

            m_Actor = GetComponent<Actor>();
            DebugUtility.HandleErrorIfNullGetComponent<Actor, PlayerCharacterController>(m_Actor, this, gameObject);

            m_Controller.enableOverlapRecovery = true;

            m_Health.OnDie += OnDie;

            // 시작 시 웅크림 상태를 false로 강제 설정
            SetCrouchingState(false, true);
            UpdateCharacterHeight(true);
        }

        void Update()
        {
            // Y 킬 체크
            if (!IsDead && transform.position.y < KillHeight)
            {
                m_Health.Kill();
            }

            HasJumpedThisFrame = false;

            bool wasGrounded = IsGrounded;
            GroundCheck();

            // 착지
            if (IsGrounded && !wasGrounded)
            {
                // 낙하 데미지
                float fallSpeed = -Mathf.Min(CharacterVelocity.y, m_LatestImpactSpeed.y);
                float fallSpeedRatio = (fallSpeed - MinSpeedForFallDamage) / (MaxSpeedForFallDamage - MinSpeedForFallDamage);
                if (RecievesFallDamage && fallSpeedRatio > 0f)
                {
                    float dmgFromFall = Mathf.Lerp(FallDamageAtMinSpeed, FallDamageAtMaxSpeed, fallSpeedRatio);
                    m_Health.TakeDamage(dmgFromFall, null);

                    // 낙하 데미지 효과음
                    AudioSource.PlayOneShot(FallDamageSfx);
                }
                else
                {
                    // 착지 효과음
                    AudioSource.PlayOneShot(LandSfx);
                }
            }

            // 웅크리기
            if (m_InputHandler.GetCrouchInputDown())
            {
                SetCrouchingState(!IsCrouching, false);
            }

            UpdateCharacterHeight(false);

            HandleCharacterMovement();
        }

        void OnDie()
        {
            IsDead = true;

            // 무기 매니저에게 존재하지 않는 무기로 전환하도록 지시하여 무기를 내림
            m_WeaponsManager.SwitchToWeaponIndex(-1, true);

            EventManager.Broadcast(Events.PlayerDeathEvent);
        }

        void GroundCheck()
        {
            // 이미 공중에 있을 때의 지면 체크 거리는 매우 작게 만들어 갑자기 지면에 스냅되는 것을 방지
            float chosenGroundCheckDistance = IsGrounded ? (m_Controller.skinWidth + GroundCheckDistance) : k_GroundCheckDistanceInAir;

            // 지면 체크 전 값 초기화
            IsGrounded = false;
            m_GroundNormal = Vector3.up;

            // 마지막 점프 이후 짧은 시간이 지났을 때만 지면 감지 시도; 그렇지 않으면 점프 후 즉시 지면에 스냅될 수 있음
            if (Time.time >= m_LastTimeJumped + k_JumpGroundingPreventionTime)
            {
                // 지면에 있다면, 캐릭터 캡슐을 나타내는 하향 캡슐 캐스트로 지면 법선에 대한 정보 수집
                if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(m_Controller.height), m_Controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, GroundCheckLayers, QueryTriggerInteraction.Ignore))
                {
                    // 발견된 표면의 위쪽 방향 저장
                    m_GroundNormal = hit.normal;

                    // 지면 법선이 캐릭터 위쪽 방향과 같은 방향이고 경사각이 캐릭터 컨트롤러의 제한보다 낮을 때만 유효한 지면 히트로 간주
                    if (Vector3.Dot(hit.normal, transform.up) > 0f &&
                        IsNormalUnderSlopeLimit(m_GroundNormal))
                    {
                        IsGrounded = true;

                        // 지면에 스냅 처리
                        if (hit.distance > m_Controller.skinWidth)
                        {
                            m_Controller.Move(Vector3.down * hit.distance);
                        }
                    }
                }
            }
        }

        void HandleCharacterMovement()
        {
            // 수평 캐릭터 회전
            {
                // 로컬 Y축 주위로 입력 속도로 트랜스폼 회전
                transform.Rotate(new Vector3(0f, (m_InputHandler.GetLookInputsHorizontal() * RotationSpeed * RotationMultiplier), 0f), Space.Self);
            }

            // 수직 카메라 회전
            {
                // 카메라의 수직 각도에 수직 입력 추가
                m_CameraVerticalAngle += m_InputHandler.GetLookInputsVertical() * RotationSpeed * RotationMultiplier;

                // 카메라의 수직 각도를 최소/최대로 제한
                m_CameraVerticalAngle = Mathf.Clamp(m_CameraVerticalAngle, -89f, 89f);

                // 수직 각도를 카메라 트랜스폼의 오른쪽 축을 따라 로컬 회전으로 적용 (위아래로 회전하게 함)
                PlayerCamera.transform.localEulerAngles = new Vector3(m_CameraVerticalAngle, 0, 0);
            }

            // 캐릭터 이동 처리
            bool isSprinting = m_InputHandler.GetSprintInputHeld();
            {
                if (isSprinting)
                {
                    isSprinting = SetCrouchingState(false, false);
                }

                float speedModifier = isSprinting ? SprintSpeedModifier : 1f;

                // 이동 입력을 캐릭터의 트랜스폼 방향을 기반으로 월드스페이스 벡터로 변환
                Vector3 worldspaceMoveInput = transform.TransformVector(m_InputHandler.GetMoveInput());

                // 지면 이동 처리
                if (IsGrounded)
                {
                    // 입력, 최대 속도 및 현재 경사로부터 원하는 속도 계산
                    Vector3 targetVelocity = worldspaceMoveInput * MaxSpeedOnGround * speedModifier;
                    // 웅크림 중이면 웅크림 속도 비율만큼 속도 감소
                    if (IsCrouching)
                        targetVelocity *= MaxSpeedCrouchedRatio;
                    targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, m_GroundNormal) * targetVelocity.magnitude;

                    // 가속 속도에 따라 현재 속도와 목표 속도 사이를 부드럽게 보간
                    CharacterVelocity = Vector3.Lerp(CharacterVelocity, targetVelocity, MovementSharpnessOnGround * Time.deltaTime);

                    // 점프
                    if (IsGrounded && m_InputHandler.GetJumpInputDown())
                    {
                        // 웅크림 상태를 false로 강제 설정
                        if (SetCrouchingState(false, false))
                        {
                            // 속도의 수직 성분 취소로 시작
                            CharacterVelocity = new Vector3(CharacterVelocity.x, 0f, CharacterVelocity.z);

                            // 그런 다음 위쪽으로 점프 속도 값 추가
                            CharacterVelocity += Vector3.up * JumpForce;

                            // 사운드 재생
                            AudioSource.PlayOneShot(JumpSfx);

                            // 짧은 시간 동안 지면 스냅을 방지해야 하므로 마지막 점프 시간 기억
                            m_LastTimeJumped = Time.time;
                            HasJumpedThisFrame = true;

                            // 지면 상태를 false로 강제 설정
                            IsGrounded = false;
                            m_GroundNormal = Vector3.up;
                        }
                    }

                    // 발소리
                    float chosenFootstepSfxFrequency = (isSprinting ? FootstepSfxFrequencyWhileSprinting : FootstepSfxFrequency);
                    if (m_FootstepDistanceCounter >= 1f / chosenFootstepSfxFrequency)
                    {
                        m_FootstepDistanceCounter = 0f;
                        AudioSource.PlayOneShot(FootstepSfx);
                    }

                    // 발소리를 위해 이동 거리 추적
                    m_FootstepDistanceCounter += CharacterVelocity.magnitude * Time.deltaTime;
                }
                // 공중 이동 처리
                else
                {
                    // 공중 가속도 추가
                    CharacterVelocity += worldspaceMoveInput * AccelerationSpeedInAir * Time.deltaTime;

                    // 공중 속도를 최대로 제한하되, 수평으로만 제한
                    float verticalVelocity = CharacterVelocity.y;
                    Vector3 horizontalVelocity = Vector3.ProjectOnPlane(CharacterVelocity, Vector3.up);
                    horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, MaxSpeedInAir * speedModifier);
                    CharacterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

                    // 속도에 중력 적용
                    CharacterVelocity += Vector3.down * GravityDownForce * Time.deltaTime;
                }
            }

            // 최종 계산된 속도 값을 캐릭터 이동으로 적용
            Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
            Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(m_Controller.height);
            m_Controller.Move(CharacterVelocity * Time.deltaTime);

            // 장애물을 감지하여 그에 따라 속도 조정
            m_LatestImpactSpeed = Vector3.zero;
            if (Physics.CapsuleCast(
                capsuleBottomBeforeMove,
                capsuleTopBeforeMove,
                m_Controller.radius,
                CharacterVelocity.normalized,
                out RaycastHit hit,
                CharacterVelocity.magnitude * Time.deltaTime,
                -1,
                QueryTriggerInteraction.Ignore))
            {
                // 낙하 데미지 로직에서 필요할 수 있으므로 마지막 충돌 속도 기억
                m_LatestImpactSpeed = CharacterVelocity;

                CharacterVelocity = Vector3.ProjectOnPlane(CharacterVelocity, hit.normal);
            }
        }

        // 주어진 법선으로 표현되는 경사각이 캐릭터 컨트롤러의 경사각 제한 아래인 경우 true 반환
        bool IsNormalUnderSlopeLimit(Vector3 normal)
        {
            return Vector3.Angle(transform.up, normal) <= m_Controller.slopeLimit;
        }

        // 캐릭터 컨트롤러 캡슐의 하단 반구 중심점 가져오기
        Vector3 GetCapsuleBottomHemisphere()
        {
            return transform.position + (transform.up * m_Controller.radius);
        }

        // 캐릭터 컨트롤러 캡슐의 상단 반구 중심점 가져오기
        Vector3 GetCapsuleTopHemisphere(float atHeight)
        {
            return transform.position + (transform.up * (atHeight - m_Controller.radius));
        }

        // 주어진 경사면에 접하는 방향으로 재정렬된 방향 가져오기
        public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
        {
            Vector3 directionRight = Vector3.Cross(direction, transform.up);
            return Vector3.Cross(slopeNormal, directionRight).normalized;
        }

        void UpdateCharacterHeight(bool force)
        {
            // 높이를 즉시 업데이트
            if (force)
            {
                m_Controller.height = m_TargetCharacterHeight;
                m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
                PlayerCamera.transform.localPosition = Vector3.up * m_TargetCharacterHeight * CameraHeightRatio;
                m_Actor.aimPoint.transform.localPosition = m_Controller.center;
            }
            // 높이를 부드럽게 업데이트
            else if (m_Controller.height != m_TargetCharacterHeight)
            {
                // 캡슐 크기 조정 및 카메라 위치 조정
                m_Controller.height = Mathf.Lerp(m_Controller.height, m_TargetCharacterHeight, CrouchingSharpness * Time.deltaTime);
                m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
                PlayerCamera.transform.localPosition = Vector3.Lerp(PlayerCamera.transform.localPosition, Vector3.up * m_TargetCharacterHeight * CameraHeightRatio, CrouchingSharpness * Time.deltaTime);
                m_Actor.aimPoint.transform.localPosition = m_Controller.center;
            }
        }

        // 장애물이 있으면 false 반환
        bool SetCrouchingState(bool crouched, bool ignoreObstructions)
        {
            // 적절한 높이 설정
            if (crouched)
            {
                m_TargetCharacterHeight = CapsuleHeightCrouching;
            }
            else
            {
                // 장애물 감지
                if (!ignoreObstructions)
                {
                    Collider[] standingOverlaps = Physics.OverlapCapsule(
                        GetCapsuleBottomHemisphere(),
                        GetCapsuleTopHemisphere(CapsuleHeightStanding),
                        m_Controller.radius,
                        -1,
                        QueryTriggerInteraction.Ignore);
                    foreach (Collider c in standingOverlaps)
                    {
                        if (c != m_Controller)
                        {
                            return false;
                        }
                    }
                }

                m_TargetCharacterHeight = CapsuleHeightStanding;
            }

            if (OnStanceChanged != null)
            {
                OnStanceChanged.Invoke(crouched);
            }

            IsCrouching = crouched;
            return true;
        }
    }
}