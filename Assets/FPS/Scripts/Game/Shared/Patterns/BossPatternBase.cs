using UnityEngine;

/// <summary>
/// 보스 공격 패턴을 무기로 통합하는 기본 클래스
/// 
/// 상속받는 클래스:
/// - FloorPatternWeapon (FloorPatternSpawner 사용)
/// - MinionSpawnWeapon (MinionSpawner 사용)
/// - 기타 커스텀 패턴
/// </summary>
public abstract class BossPatternBase : MonoBehaviour
{
    [Header("패턴 기본 설정")]
    [Tooltip("패턴 데미지")]
    public float PatternDamage = 25f;

    [Tooltip("공격 재사용 대기시간 (쿨타임)")]
    public float FireRate = 2f;

    [Tooltip("패턴 시작 전 대기 시간")]
    public float PreAttackDelay = 0f;

    [Header("애니메이션")]
    [Tooltip("공격 애니메이션 트리거 이름")]
    public string AttackAnimationTrigger = "Attack";

    [Tooltip("공격 중에 이동할 수 있는지")]
    public bool CanMoveWhileAttacking = true;

    public GameObject Owner { get; set; }
    public Transform WeaponRoot => transform;

    protected float m_LastFireTime = Mathf.NegativeInfinity;
    protected bool m_IsVisible = true;
    protected Animator m_OwnerAnimator;

    protected virtual void Start()
    {
        // 오너의 애니메이터 찾기
        if (Owner != null)
        {
            m_OwnerAnimator = Owner.GetComponent<Animator>();
        }
    }

    /// <summary>
    /// 무기 발사 입력 처리 (WeaponController와 호환)
    /// </summary>
    public virtual bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
    {
        if (!inputHeld)
            return false;

        return TryExecutePattern();
    }

    /// <summary>
    /// 패턴 실행 시도 (각 서브클래스에서 구현)
    /// </summary>
    protected abstract bool ExecutePattern();
    protected virtual void OnEnable()
    {

    }

    /// <summary>
    /// 패턴 실행 (쿨타임 체크 포함)
    /// </summary>
    private bool TryExecutePattern()
    {
        // 쿨타임 확인
        if (Time.time < m_LastFireTime + FireRate)
            return false;

        // 패턴 실행
        bool success = ExecutePattern();

        if (success)
        {
            m_LastFireTime = Time.time;
            PlayAttackAnimation();
        }

        return success;
    }

    /// <summary>
    /// 공격 애니메이션 재생
    /// </summary>
    protected virtual void PlayAttackAnimation()
    {
        if (m_OwnerAnimator != null)
        {
            m_OwnerAnimator.SetTrigger(AttackAnimationTrigger);
            m_OwnerAnimator.SetFloat("MoveSpeed", CanMoveWhileAttacking ? 1f : 0f);
        }
    }

    /// <summary>
    /// 무기 시각화 (WeaponController 인터페이스 호환성)
    /// </summary>
    public virtual void ShowWeapon(bool show)
    {
        m_IsVisible = show;

        // 3D 모델이 있다면 표시/숨김
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = show;
        }
    }

    /// <summary>
    /// 현재 쿨타임 남은 시간 반환 (0 = 사용 가능)
    /// </summary>
    public float GetRemainingCooldown()
    {
        float remaining = (m_LastFireTime + FireRate) - Time.time;
        return Mathf.Max(0, remaining);
    }

    /// <summary>
    /// 쿨타임 진행률 반환 (0~1)
    /// </summary>
    public float GetCooldownProgress()
    {
        if (FireRate <= 0)
            return 1f;

        return GetRemainingCooldown() / FireRate;
    }

    /// <summary>
    /// 디버그용 정보 출력
    /// </summary>
    public virtual void DebugPrintInfo()
    {
        Debug.Log($"=== [{gameObject.name}] BossPattern ===\n" +
            $"데미지: {PatternDamage}\n" +
            $"쿨타임: {FireRate}s\n" +
            $"남은 쿨타임: {GetRemainingCooldown():F2}s");
    }

    protected virtual void OnDrawGizmosSelected()
    {
        // 서브클래스에서 구현
    }
}