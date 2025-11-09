
using UnityEngine;

public class EnemyMobile : AIBase
{
    public enum AIState
    {
        Patrol,  // 순찰
        Follow,  // 추적
        Attack,  // 공격
    }

    [Header("이동 설정")]
    [Tooltip("공격 중 타겟을 향해 멈추는 적의 공격 범위 비율")]
    [Range(0f, 1f)]
    public float AttackStopDistanceRatio = 0f;

    [Header("사운드")]
    public AudioClip MovementSound;
    public MinMaxFloat PitchDistortionMovementSpeed;

    // 특성 재정의
    public new AIState CurrentAiState
    {
        get => (AIState)base.CurrentAiState;
        private set => base.CurrentAiState = value;
    }

    // 컴포넌트 참조
    private AudioSource m_AudioSource;

    // 애니메이션 상수
    private const string k_AnimMoveSpeedParameter = "MoveSpeed";

    protected override void SetupAnimator()
    {
        m_Animator = GetComponent<Animator>();
    }

    protected override void SetInitialState()
    {
        // 초기 상태를 Patrol로 설정
        CurrentAiState = AIState.Patrol;

        // 초기 경로 설정
        m_EnemyController.SetPathDestinationToClosestNode();

        // 이동 사운드 설정
        m_AudioSource = GetComponent<AudioSource>();
        DebugUtility.HandleErrorIfNullGetComponent<AudioSource, EnemyMobile>(m_AudioSource, this, gameObject);

        m_AudioSource.clip = MovementSound;
        m_AudioSource.Play();
    }

    protected override void Update()
    {
        base.Update();

        // 이동 속도에 따른 애니메이션 및 사운드 업데이트
        float moveSpeed = m_EnemyController.NavMeshAgent.velocity.magnitude;

        // 애니메이터 속도 매개변수 업데이트
        if (m_Animator != null)
        {
            m_Animator.SetFloat(k_AnimMoveSpeedParameter, moveSpeed);
        }

        // 이동 속도에 따른 사운드 피치 변경
        if (m_AudioSource != null)
        {
            m_AudioSource.pitch = Mathf.Lerp(
                PitchDistortionMovementSpeed.Min,
                PitchDistortionMovementSpeed.Max,
                moveSpeed / m_EnemyController.NavMeshAgent.speed);
        }
    }

    protected override void UpdateAiStateTransitions()
    {
        // 상태 전환 로직
        switch (CurrentAiState)
        {
            case AIState.Follow:
                // 타겟에 대한 시야가 있고 공격 범위 내에 있을 때 공격으로 전환
                if (m_EnemyController.IsSeeingTarget && m_EnemyController.IsTargetInAttackRange)
                {
                    CurrentAiState = AIState.Attack;
                    m_EnemyController.SetNavDestination(transform.position);
                }
                break;

            case AIState.Attack:
                // 더 이상 공격 범위 내에 타겟이 없을 때 추적으로 전환
                if (!m_EnemyController.IsTargetInAttackRange)
                {
                    CurrentAiState = AIState.Follow;
                }
                break;
        }
    }

    protected override void UpdateCurrentAiState()
    {
        // 현재 상태에 따른 행동 수행
        switch (CurrentAiState)
        {
            case AIState.Patrol:
                // 순찰 로직
                m_EnemyController.UpdatePathDestination();
                m_EnemyController.SetNavDestination(m_EnemyController.GetDestinationOnPath());
                break;

            case AIState.Follow:
                // 추적 로직
                m_EnemyController.SetNavDestination(m_EnemyController.KnownDetectedTarget.transform.position);
                m_EnemyController.OrientTowards(m_EnemyController.KnownDetectedTarget.transform.position);
                m_EnemyController.OrientWeaponsTowards(m_EnemyController.KnownDetectedTarget.transform.position);
                break;

            case AIState.Attack:
                // 공격 로직
                if (Vector3.Distance(
                    m_EnemyController.KnownDetectedTarget.transform.position,
                    m_EnemyController.DetectionModule.DetectionSourcePoint.position)
                    >= (AttackStopDistanceRatio * m_EnemyController.DetectionModule.AttackRange))
                {
                    // 적절한 공격 거리가 아니면 이동
                    m_EnemyController.SetNavDestination(m_EnemyController.KnownDetectedTarget.transform.position);
                }
                else
                {
                    // 적절한 공격 거리면 제자리 정지
                    m_EnemyController.SetNavDestination(transform.position);
                }

                // 타겟 방향 조준 및 공격
                m_EnemyController.OrientTowards(m_EnemyController.KnownDetectedTarget.transform.position);
                m_EnemyController.TryAtack(m_EnemyController.KnownDetectedTarget.transform.position);
                break;
        }
    }

    // 이벤트 핸들러 재정의
    protected override void OnDetectedTarget()
    {
        base.OnDetectedTarget();

        if (CurrentAiState == AIState.Patrol)
        {
            CurrentAiState = AIState.Follow;
        }
    }

    protected override void OnLostTarget()
    {
        base.OnLostTarget();

        if (CurrentAiState == AIState.Follow || CurrentAiState == AIState.Attack)
        {
            CurrentAiState = AIState.Patrol;
        }
    }
}