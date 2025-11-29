using UnityEngine;
[RequireComponent(typeof(EnemyController))]
public abstract class AIBase : MonoBehaviour
{
    [Tooltip("랜덤 피격 데미지 효과")]
    public ParticleSystem[] RandomHitSparks;

    [Tooltip("감지 시 시각 효과")]
    public ParticleSystem[] OnDetectVfx;

    [Tooltip("감지 시 사운드 효과")]
    public AudioClip OnDetectSfx;

    // 애니메이터 상수
    protected const string k_AnimOnDamagedParameter = "OnDamaged";
    protected const string k_AnimIsActiveParameter = "IsActive";
    protected const string k_AnimAttackParameter = "Attack";
    protected const string k_AnimAlertedParameter = "Alerted";

    // 공통 컴포넌트 참조
    protected EnemyController m_EnemyController;
    protected Health m_Health;
    protected Animator m_Animator;

    // 현재 AI 상태
    public object CurrentAiState { get; protected set; }

    protected virtual void Awake()
    {
        // 컴포넌트 참조 획득
        m_EnemyController = GetComponent<EnemyController>();
        DebugUtility.HandleErrorIfNullGetComponent<EnemyController, AIBase>(m_EnemyController, this, gameObject);

        m_Health = GetComponent<Health>();
        DebugUtility.HandleErrorIfNullGetComponent<Health, AIBase>(m_Health, this, gameObject);

        // 하위 클래스에서 이 메서드를 재정의하여 애니메이터 참조를 설정
        SetupAnimator();
    }

    protected virtual void Start()
    {
        // 이벤트 리스너 등록
        m_EnemyController.onDetectedTarget += OnDetectedTarget;
        m_EnemyController.onLostTarget += OnLostTarget;
        m_EnemyController.onAttack += OnAttack;
        m_Health.OnDamaged += OnDamaged;

        // 초기 상태 설정
        SetInitialState();
    }

    protected virtual void Update()
    {
        // 상태 전환 검사 및 처리
        UpdateAiStateTransitions();

        // 현재 상태에 따른 행동 수행
        UpdateCurrentAiState();
    }

    // 애니메이터 설정 (하위 클래스에서 재정의)
    protected abstract void SetupAnimator();

    // 초기 상태 설정 (하위 클래스에서 재정의)
    protected abstract void SetInitialState();

    // 상태 전환 로직 (하위 클래스에서 재정의)
    protected abstract void UpdateAiStateTransitions();

    // 현재 상태 행동 로직 (하위 클래스에서 재정의)
    protected abstract void UpdateCurrentAiState();

    // 공통 이벤트 핸들러 (하위 클래스에서 필요에 따라 재정의)
    protected virtual void OnAttack()
    {
        if (m_Animator != null)
        {
            m_Animator.SetTrigger(k_AnimAttackParameter);
        }
    }

    protected virtual void OnDetectedTarget()
    {
        for (int i = 0; i < OnDetectVfx.Length; i++)
        {
            OnDetectVfx[i].Play();
        }

        if (OnDetectSfx)
        {
            AudioUtility.CreateSFX(OnDetectSfx, transform.position, AudioUtility.AudioGroups.EnemyDetection, 1f);
        }

        if (m_Animator != null)
        {
            m_Animator.SetBool(k_AnimAlertedParameter, true);
        }
    }

    protected virtual void OnLostTarget()
    {
        for (int i = 0; i < OnDetectVfx.Length; i++)
        {
            OnDetectVfx[i].Stop();
        }

        if (m_Animator != null)
        {
            m_Animator.SetBool(k_AnimAlertedParameter, false);
        }
    }

    protected virtual void OnDamaged(float damage, GameObject damageSource)
    {
        if (RandomHitSparks.Length > 0)
        {
            int n = Random.Range(0, RandomHitSparks.Length - 1);
            RandomHitSparks[n].Play();
        }

        if (m_Animator != null)
        {
            m_Animator.SetTrigger(k_AnimOnDamagedParameter);
        }
    }

    protected virtual void OnDestroy()
    {
        // 이벤트 리스너 제거
        if (m_EnemyController != null)
        {
            m_EnemyController.onDetectedTarget -= OnDetectedTarget;
            m_EnemyController.onLostTarget -= OnLostTarget;
            m_EnemyController.onAttack -= OnAttack;
        }

        if (m_Health != null)
        {
            m_Health.OnDamaged -= OnDamaged;
        }
    }
}