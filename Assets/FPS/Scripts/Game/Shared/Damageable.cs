using UnityEngine;

public class Damageable : MonoBehaviour
{
    /// <summary>
    /// 받는 피해 배율
    /// </summary>
    [Tooltip("Multiplier to apply to the received damage")]
    public float DamageMultiplier = 1f;

    /// <summary>
    /// 자가 피해 배율
    /// </summary>
    [Range(0, 1)] [Tooltip("Multiplier to apply to self damage")]
    public float SensibilityToSelfdamage = 0.5f;

    public Health Health { get; private set; }

    void Awake()
    {
        // 체력 컴포넌트 찾아서 적용하기
        Health = GetComponent<Health>();
        if (!Health)
        {
            Health = GetComponentInParent<Health>();
        }
    }

    public void InflictDamage(float damage, bool isExplosionDamage, GameObject damageSource)
    {
        if (Health)
        {
            var totalDamage = damage;

            // 폭팔 데미지면 크리티컬 미적용
            if (!isExplosionDamage)
            {
                totalDamage *= DamageMultiplier;
            }

            // 자가 피해시 데미지 감소율 적용
            if (Health.gameObject == damageSource)
            {
                totalDamage *= SensibilityToSelfdamage;
            }

            // 데미지 적용
            Health.TakeDamage(totalDamage, damageSource);
        }
    }
}