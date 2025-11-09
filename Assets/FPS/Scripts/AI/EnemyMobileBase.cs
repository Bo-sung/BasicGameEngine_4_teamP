
using UnityEngine;

/// <summary>
/// EnemyBase를 사용하는 모바일 적 (EnemyMobile의 EnemyBase 버전)
/// EnemyBase를 상속받아 테이블 데이터로 초기화하며, 모바일 AI 로직을 통합
/// </summary>
public class EnemyMobileBase : EnemyBase
{
    public enum AIState
    {
        Patrol,
        Follow,
        Attack,
    }

    [Header("Mobile Enemy Settings")]
    public Animator Animator;

    [Tooltip("Fraction of the enemy's attack range at which it will stop moving towards target while attacking")]
    [Range(0f, 1f)]
    public float AttackStopDistanceRatio = 0.5f;

    [Tooltip("The random hit damage effects")]
    public ParticleSystem[] RandomHitSparks;

    public ParticleSystem[] OnDetectVfx;
    public AudioClip OnDetectSfx;

    [Header("Sound")]
    public AudioClip MovementSound;
    public MinMaxFloat PitchDistortionMovementSpeed;

    public AIState AiState { get; protected set; }
    AudioSource m_AudioSource;

    const string k_AnimMoveSpeedParameter = "MoveSpeed";
    const string k_AnimAttackParameter = "Attack";
    const string k_AnimAlertedParameter = "Alerted";
    const string k_AnimOnDamagedParameter = "OnDamaged";

    protected override void Awake()
    {
        base.Awake();
    }

    void Start()
    {
        // EnemyBase가 이미 초기화되어 있으므로 이벤트만 등록
        onAttack += OnAttack;
        onDetectedTarget += OnDetectedTarget;
        onLostTarget += OnLostTarget;
        SetPathDestinationToClosestNode();
        onDamaged += OnDamaged;

        // Start patrolling
        AiState = AIState.Patrol;

        // adding a audio source to play the movement sound on it
        m_AudioSource = GetComponent<AudioSource>();
        DebugUtility.HandleErrorIfNullGetComponent<AudioSource, EnemyMobileBase>(m_AudioSource, this, gameObject);
        m_AudioSource.clip = MovementSound;
        m_AudioSource.Play();
    }

    protected virtual void Update()
    {
        UpdateAiStateTransitions();
        UpdateCurrentAiState();

        float moveSpeed = NavMeshAgent.velocity.magnitude;

        // Update animator speed parameter
        if (Animator != null)
        {
            Animator.SetFloat(k_AnimMoveSpeedParameter, moveSpeed);
        }

        // changing the pitch of the movement sound depending on the movement speed
        m_AudioSource.pitch = Mathf.Lerp(PitchDistortionMovementSpeed.Min, PitchDistortionMovementSpeed.Max,
            moveSpeed / NavMeshAgent.speed);
    }

    void UpdateAiStateTransitions()
    {
        // Handle transitions
        switch (AiState)
        {
            case AIState.Follow:
                // Transition to attack when there is a line of sight to the target
                if (IsSeeingTarget && IsTargetInAttackRange)
                {
                    AiState = AIState.Attack;
                    SetNavDestination(transform.position);
                }

                break;
            case AIState.Attack:
                // Transition to follow when no longer a target in attack range
                if (!IsTargetInAttackRange)
                {
                    AiState = AIState.Follow;
                }

                break;
        }
    }

    void UpdateCurrentAiState()
    {
        // Handle logic
        switch (AiState)
        {
            case AIState.Patrol:
                UpdatePathDestination();
                SetNavDestination(GetDestinationOnPath());
                break;
            case AIState.Follow:
                SetNavDestination(KnownDetectedTarget.transform.position);
                OrientTowards(KnownDetectedTarget.transform.position);
                OrientWeaponsTowards(KnownDetectedTarget.transform.position);
                break;
            case AIState.Attack:
                if (Vector3.Distance(KnownDetectedTarget.transform.position,
                        DetectionModule.DetectionSourcePoint.position)
                    >= (AttackStopDistanceRatio * DetectionModule.AttackRange))
                {
                    SetNavDestination(KnownDetectedTarget.transform.position);
                }
                else
                {
                    SetNavDestination(transform.position);
                }

                OrientTowards(KnownDetectedTarget.transform.position);
                TryAtack(KnownDetectedTarget.transform.position);
                break;
        }
    }

    void OnAttack()
    {
        if (Animator != null)
        {
            Animator.SetTrigger(k_AnimAttackParameter);
        }
    }

    void OnDetectedTarget()
    {
        if (AiState == AIState.Patrol)
        {
            AiState = AIState.Follow;
        }

        for (int i = 0; i < OnDetectVfx.Length; i++)
        {
            OnDetectVfx[i].Play();
        }

        if (OnDetectSfx)
        {
            AudioUtility.CreateSFX(OnDetectSfx, transform.position, AudioUtility.AudioGroups.EnemyDetection, 1f);
        }

        if (Animator != null)
        {
            Animator.SetBool(k_AnimAlertedParameter, true);
        }
    }

    void OnLostTarget()
    {
        if (AiState == AIState.Follow || AiState == AIState.Attack)
        {
            AiState = AIState.Patrol;
        }

        for (int i = 0; i < OnDetectVfx.Length; i++)
        {
            OnDetectVfx[i].Stop();
        }

        if (Animator != null)
        {
            Animator.SetBool(k_AnimAlertedParameter, false);
        }
    }

    void OnDamaged()
    {
        if (RandomHitSparks.Length > 0)
        {
            int n = Random.Range(0, RandomHitSparks.Length - 1);
            RandomHitSparks[n].Play();
        }

        if (Animator != null)
        {
            Animator.SetTrigger(k_AnimOnDamagedParameter);
        }
    }
}