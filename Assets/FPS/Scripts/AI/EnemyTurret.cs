
using UnityEngine;

public class EnemyTurret : AIBase
{
    public enum AIState
    {
        Idle,
        Attack,
    }

    [Header("터렛 설정")]
    public Transform TurretPivot;
    public Transform TurretAimPoint;
    public float AimRotationSharpness = 5f;
    public float LookAtRotationSharpness = 2.5f;
    public float DetectionFireDelay = 1f;
    public float AimingTransitionBlendTime = 1f;

    // 특성 재정의
    public new AIState CurrentAiState
    {
        get => (AIState)base.CurrentAiState;
        private set => base.CurrentAiState = value;
    }

    Quaternion m_RotationWeaponForwardToPivot;
    float m_TimeStartedDetection;
    float m_TimeLostDetection;
    Quaternion m_PreviousPivotAimingRotation;
    Quaternion m_PivotAimingRotation;

    protected override void SetupAnimator()
    {
        m_Animator = GetComponent<Animator>();
    }

    protected override void SetInitialState()
    {
        // 초기 상태를 Idle로 설정
        CurrentAiState = AIState.Idle;
        m_TimeStartedDetection = Mathf.NegativeInfinity;
        m_PreviousPivotAimingRotation = TurretPivot.rotation;

        // 무기 방향과 피봇 간의 회전 오프셋 계산
        m_RotationWeaponForwardToPivot =
            Quaternion.Inverse(m_EnemyController.GetCurrentWeapon().WeaponMuzzle.rotation) * TurretPivot.rotation;
    }

    protected override void UpdateAiStateTransitions()
    {
        // 터렛은 onDetectedTarget/onLostTarget 이벤트로만 상태 전환을 처리
        // 여기서는 추가 전환 로직 없음
    }

    protected override void UpdateCurrentAiState()
    {
        // 현재 상태에 따른 행동 수행
        switch (CurrentAiState)
        {
            case AIState.Attack:
                bool mustShoot = Time.time > m_TimeStartedDetection + DetectionFireDelay;

                // 타겟 방향으로 조준 회전 계산
                Vector3 directionToTarget =
                    (m_EnemyController.KnownDetectedTarget.transform.position - TurretAimPoint.position).normalized;
                Quaternion offsettedTargetRotation =
                    Quaternion.LookRotation(directionToTarget) * m_RotationWeaponForwardToPivot;
                m_PivotAimingRotation = Quaternion.Slerp(
                    m_PreviousPivotAimingRotation,
                    offsettedTargetRotation,
                    (mustShoot ? AimRotationSharpness : LookAtRotationSharpness) * Time.deltaTime);

                // 발사
                if (mustShoot)
                {
                    Vector3 correctedDirectionToTarget =
                        (m_PivotAimingRotation * Quaternion.Inverse(m_RotationWeaponForwardToPivot)) * Vector3.forward;

                    m_EnemyController.TryAtack(TurretAimPoint.position + correctedDirectionToTarget);
                }
                break;
        }
    }

    // Late Update에서 실제 포탑 회전 적용
    void LateUpdate()
    {
        UpdateTurretAiming();
    }

    void UpdateTurretAiming()
    {
        switch (CurrentAiState)
        {
            case AIState.Attack:
                TurretPivot.rotation = m_PivotAimingRotation;
                break;
            default:
                // 애니메이션의 포탑 회전 사용
                TurretPivot.rotation = Quaternion.Slerp(
                    m_PivotAimingRotation,
                    TurretPivot.rotation,
                    (Time.time - m_TimeLostDetection) / AimingTransitionBlendTime);
                break;
        }

        m_PreviousPivotAimingRotation = TurretPivot.rotation;
    }

    // 이벤트 핸들러 재정의
    protected override void OnDetectedTarget()
    {
        base.OnDetectedTarget();

        if (CurrentAiState == AIState.Idle)
        {
            CurrentAiState = AIState.Attack;
        }

        m_TimeStartedDetection = Time.time;
    }

    protected override void OnLostTarget()
    {
        base.OnLostTarget();

        if (CurrentAiState == AIState.Attack)
        {
            CurrentAiState = AIState.Idle;
        }

        m_TimeLostDetection = Time.time;
    }
}