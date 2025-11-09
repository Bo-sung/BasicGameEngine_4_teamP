using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 체력. 데미지 처리 컴포넌트
/// </summary>
public class Health : MonoBehaviour
{
    /// <summary>
    /// 최대 체력
    /// </summary>
    [Tooltip("Maximum amount of health")]
    public float MaxHealth = 10f;

    /// <summary>
    /// 위험 체력 기준점
    /// </summary>
    [Tooltip("Health ratio at which the critical health vignette starts appearing")]
    public float CriticalHealthRatio = 0.3f;

    //----------이벤트----------//
    /// <summary>
    /// 데미지 받았을때
    /// </summary>
    public System.Action<float, GameObject> OnDamaged;
    /// <summary>
    /// 회복 했을때
    /// </summary>
    public System.Action<float> OnHealed;
    /// <summary>
    /// 죽었을때
    /// </summary>
    public System.Action OnDie;

    public float CurrentHealth { get; set; }
    public bool Invincible { get; set; }
    public bool CanPickup() => CurrentHealth < MaxHealth;

    public float GetRatio() => CurrentHealth / MaxHealth;
    public bool IsCritical() => GetRatio() <= CriticalHealthRatio;

    bool m_IsDead;

    void Start()
    {
        CurrentHealth = MaxHealth;
    }

    /// <summary>
    /// 회복
    /// </summary>
    /// <param name="healAmount"></param>
    public void Heal(float healAmount)
    {
        float healthBefore = CurrentHealth;
        CurrentHealth += healAmount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);

        float trueHealAmount = CurrentHealth - healthBefore;

        // 회복 이벤트 호출
        if (trueHealAmount > 0f)
        {
            OnHealed?.Invoke(trueHealAmount);
        }
    }

    /// <summary>
    /// 데미지 적용
    /// </summary>
    /// <param name="damage"></param>
    /// <param name="damageSource"></param>
    public void TakeDamage(float damage, GameObject damageSource)
    {
        // 무적이면 끝
        if (Invincible)
            return;

        float healthBefore = CurrentHealth;
        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);


        float trueDamageAmount = healthBefore - CurrentHealth;

        // 데미지 받았을때 이벤트 호출
        if (trueDamageAmount > 0f)
        {
            OnDamaged?.Invoke(trueDamageAmount, damageSource);
        }

        HandleDeath();
    }

    /// <summary>
    /// 강제 죽임
    /// </summary>
    public void Kill()
    {
        CurrentHealth = 0f;

        // 사망시 이벤트 호출
        OnDamaged?.Invoke(MaxHealth, null);

        HandleDeath();
    }

    /// <summary>
    /// 사망 처리
    /// </summary>
    void HandleDeath()
    {
        if (m_IsDead)
            return;

        // call OnDie action
        if (CurrentHealth <= 0f)
        {
            m_IsDead = true;
            OnDie?.Invoke();
        }
    }
}