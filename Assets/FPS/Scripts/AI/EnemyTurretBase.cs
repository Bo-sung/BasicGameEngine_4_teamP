
using UnityEngine;

/// <summary>
/// EnemyBase를 사용하는 터렛 적 (EnemyTurret의 EnemyBase 버전)
/// EnemyBase를 상속받아 테이블 데이터로 초기화하며, 터렛 AI 로직을 통합
/// </summary>
public class EnemyTurretBase : EnemyBase
{
    public enum AIState
    {
        Idle,
        Attack,
    }

    public Transform TurretPivot;
    public Transform TurretAimPoint;
    public Animator Animator;
    public float AimRotationSharpness = 5f;
    public float LookAtRotationSharpness = 2.5f;
    public float DetectionFireDelay = 1f;
    public float AimingTransitionBlendTime = 1f;

    [Tooltip("The random hit damage effects")]
    public ParticleSystem[] RandomHitSparks;

    public ParticleSystem[] OnDetectVfx;
    public AudioClip OnDetectSfx;

    public AIState AiState { get; private set; }

    Quaternion m_RotationWeaponForwardToPivot;
    float m_TimeStartedDetection;
    float m_TimeLostDetection;
    Quaternion m_PreviousPivotAimingRotation;
    Quaternion m_PivotAimingRotation;

    const string k_AnimOnDamagedParameter = "OnDamaged";
    const string k_AnimIsActiveParameter = "IsActive";

    protected override void Awake()
    {
        base.Awake();
    }

    void Start()
    {
        // EnemyBase가 이미 Health를 가지고 있으므로 이벤트만 등록
        var health = GetComponent<Health>();
        if (health != null)
        {
            health.OnDamaged += OnDamaged;
        }

        onDetectedTarget += OnDetectedTarget;
        onLostTarget += OnLostTarget;

        // Remember the rotation offset between the pivot's forward and the weapon's forward
        m_RotationWeaponForwardToPivot = Quaternion.Inverse(GetCurrentWeapon().WeaponMuzzle.rotation) * TurretPivot.rotation;

        // Start with idle
        AiState = AIState.Idle;

        m_TimeStartedDetection = Mathf.NegativeInfinity;
        m_PreviousPivotAimingRotation = TurretPivot.rotation;
    }

    void Update()
    {
        UpdateCurrentAiState();
    }

    void LateUpdate()
    {
        UpdateTurretAiming();
    }

    void UpdateCurrentAiState()
    {
        // Handle logic 
        switch (AiState)
        {
            case AIState.Attack:
                bool mustShoot = Time.time > m_TimeStartedDetection + DetectionFireDelay;
                // Calculate the desired rotation of our turret (aim at target)
                Vector3 directionToTarget = (KnownDetectedTarget.transform.position - TurretAimPoint.position).normalized;
                Quaternion offsettedTargetRotation = Quaternion.LookRotation(directionToTarget) * m_RotationWeaponForwardToPivot;
                m_PivotAimingRotation = Quaternion.Slerp(m_PreviousPivotAimingRotation, offsettedTargetRotation, (mustShoot ? AimRotationSharpness : LookAtRotationSharpness) * Time.deltaTime);

                // shoot
                if (mustShoot)
                {
                    Vector3 correctedDirectionToTarget = (m_PivotAimingRotation * Quaternion.Inverse(m_RotationWeaponForwardToPivot)) * Vector3.forward;

                    TryAtack(TurretAimPoint.position + correctedDirectionToTarget);
                }

                break;
        }
    }

    void UpdateTurretAiming()
    {
        switch (AiState)
        {
            case AIState.Attack:
                TurretPivot.rotation = m_PivotAimingRotation;
                break;
            default:
                // Use the turret rotation of the animation
                TurretPivot.rotation = Quaternion.Slerp(m_PivotAimingRotation, TurretPivot.rotation, (Time.time - m_TimeLostDetection) / AimingTransitionBlendTime);
                break;
        }

        m_PreviousPivotAimingRotation = TurretPivot.rotation;
    }

    void OnDamaged(float dmg, GameObject source)
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

    void OnDetectedTarget()
    {
        if (AiState == AIState.Idle)
        {
            AiState = AIState.Attack;
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
            Animator.SetBool(k_AnimIsActiveParameter, true);
        }
        m_TimeStartedDetection = Time.time;
    }

    void OnLostTarget()
    {
        if (AiState == AIState.Attack)
        {
            AiState = AIState.Idle;
        }

        for (int i = 0; i < OnDetectVfx.Length; i++)
        {
            OnDetectVfx[i].Stop();
        }

        if (Animator != null)
        {
            Animator.SetBool(k_AnimIsActiveParameter, false);
        }
        m_TimeLostDetection = Time.time;
    }
}