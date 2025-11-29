using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 바닥 충격파 공격 패턴
/// 1. 보스 주변 원형 경고 표시
/// 2. 경고 후 전방위 충격파 확산
/// </summary>
public class GroundShockwaveAttack : BossAttackPattern
{
    [Header("충격파 설정")]
    [Tooltip("충격파 최대 반경")]
    public float ShockwaveRadius = 8f;

    [Tooltip("충격파 확산 속도")]
    public float ShockwaveSpeed = 10f;

    [Tooltip("경고 지속 시간")]
    public float WarningDuration = 1.5f;

    [Tooltip("충격파 지속 시간")]
    public float ShockwaveDuration = 1f;

    [Header("프리팹")]
    [Tooltip("원형 경고 프리팹")]
    public GameObject CircleTelegraphPrefab;

    [Tooltip("충격파 VFX")]
    public GameObject ShockwaveVfxPrefab;

    [Header("VFX/SFX")]
    [Tooltip("충격파 사운드")]
    public AudioClip ShockwaveSfx;

    [Tooltip("경고 사운드")]
    public AudioClip WarningSfx;

    private TelegraphIndicator m_CurrentTelegraph;

    protected override IEnumerator ExecutePatternCoroutine()
    {
        if (Target == null) yield break;

        // 애니메이션 처리
        Animator animator = BossTransform.GetComponent<Animator>();
        if (animator == null) animator = BossTransform.GetComponentInChildren<Animator>();
        
        if (animator != null)
        {
            animator.SetTrigger("Shockwave");
            animator.SetFloat("MoveSpeed", 0f);
        }

        // 1. 경고 표시
        Vector3 telegraphPosition = BossTransform.position + Vector3.up * 0.1f;

        if (CircleTelegraphPrefab != null)
        {
            GameObject telegraphObj = Instantiate(CircleTelegraphPrefab, telegraphPosition, Quaternion.identity);
            m_CurrentTelegraph = telegraphObj.GetComponent<TelegraphIndicator>();
            
            if (m_CurrentTelegraph != null)
            {
                m_CurrentTelegraph.Size = ShockwaveRadius;
                m_CurrentTelegraph.Duration = WarningDuration;
                m_CurrentTelegraph.Show(telegraphPosition, Quaternion.identity);
            }
        }

        // 경고 사운드
        if (WarningSfx)
        {
            AudioUtility.CreateSFX(WarningSfx, BossTransform.position, AudioUtility.AudioGroups.EnemyAttack, 0f);
        }

        // 2. 경고 시간 대기
        yield return new WaitForSeconds(WarningDuration);

        // 3. 충격파 발생
        if (ShockwaveSfx)
        {
            AudioUtility.CreateSFX(ShockwaveSfx, BossTransform.position, AudioUtility.AudioGroups.EnemyAttack, 0f);
        }

        // VFX 생성
        if (ShockwaveVfxPrefab != null)
        {
            GameObject vfx = Instantiate(ShockwaveVfxPrefab, BossTransform.position, Quaternion.identity);
            Destroy(vfx, 5f);
        }

        // 4. 충격파 확산 데미지
        yield return StartCoroutine(ExpandShockwave());

        // 경고 정리
        if (m_CurrentTelegraph != null)
        {
            Destroy(m_CurrentTelegraph.gameObject);
        }

        OnPatternComplete();
    }

    IEnumerator ExpandShockwave()
    {
        float elapsedTime = 0f;
        float currentRadius = 0f;
        HashSet<GameObject> damagedTargets = new HashSet<GameObject>();

        while (elapsedTime < ShockwaveDuration)
        {
            elapsedTime += Time.deltaTime;
            currentRadius = Mathf.Lerp(0f, ShockwaveRadius, elapsedTime / ShockwaveDuration);

            // 현재 반경 내 모든 대상에게 데미지
            Collider[] hits = Physics.OverlapSphere(BossTransform.position, currentRadius);
            foreach (var hit in hits)
            {
                if (hit.gameObject == BossTransform.gameObject) continue;
                if (damagedTargets.Contains(hit.gameObject)) continue;

                Damageable damageable = hit.GetComponent<Damageable>();
                if (damageable != null)
                {
                    Actor actor = hit.GetComponent<Actor>();
                    Actor bossActor = BossTransform.GetComponent<Actor>();
                    
                    if (actor != null && bossActor != null && actor.affiliation != bossActor.affiliation)
                    {
                        damageable.InflictDamage(Damage, false, BossTransform.gameObject);
                        damagedTargets.Add(hit.gameObject);
                    }
                }
            }

            yield return null;
        }
    }
}
