using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 공격 패턴 기본 클래스
/// 모든 보스 공격 패턴은 이 클래스를 상속받아 구현합니다.
/// </summary>
public abstract class BossAttackPattern : MonoBehaviour
{
    [Header("패턴 기본 설정")]
    [Tooltip("패턴 이름")]
    public string PatternName = "Attack Pattern";

    [Tooltip("패턴 쿨다운 (초)")]
    public float Cooldown = 5f;

    [Tooltip("패턴 데미지")]
    public float Damage = 20f;

    [Header("참조")]
    [Tooltip("보스 Transform")]
    public Transform BossTransform;

    [Tooltip("타겟 (플레이어)")]
    public Transform Target;

    protected bool m_IsExecuting = false;
    protected float m_LastExecuteTime = -999f;

    protected virtual void Awake()
    {
        if (BossTransform == null)
        {
            BossTransform = transform.parent;
        }
    }

    /// <summary>
    /// 패턴 실행 가능 여부 확인
    /// </summary>
    public virtual bool CanExecute()
    {
        return !m_IsExecuting && Time.time >= m_LastExecuteTime + Cooldown && Target != null;
    }

    /// <summary>
    /// 패턴 시작
    /// </summary>
    public virtual void StartPattern()
    {
        if (!CanExecute())
        {
            Debug.LogWarning($"{PatternName} cannot execute yet. Cooldown: {Cooldown - (Time.time - m_LastExecuteTime)}s");
            return;
        }

        m_IsExecuting = true;
        m_LastExecuteTime = Time.time;

        StartCoroutine(ExecutePatternCoroutine());
    }

    /// <summary>
    /// 패턴 실행 코루틴
    /// </summary>
    protected abstract IEnumerator ExecutePatternCoroutine();

    /// <summary>
    /// 패턴 완료 처리
    /// </summary>
    protected virtual void OnPatternComplete()
    {
        m_IsExecuting = false;
    }

    /// <summary>
    /// 패턴 실행 중인지 확인
    /// </summary>
    public bool IsExecuting => m_IsExecuting;

    /// <summary>
    /// 쿨다운 남은 시간
    /// </summary>
    public float RemainingCooldown => Mathf.Max(0, Cooldown - (Time.time - m_LastExecuteTime));
}
