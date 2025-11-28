using System.Collections;
using UnityEngine;

/// <summary>
/// 연사 산탄 공격 패턴
/// 1. 부채꼴 경고 표시
/// 2. 부채꼴 범위 내 다수 발사체 연사
/// </summary>
public class BurstFireAttack : BossAttackPattern
{
    [Header("산탄 설정")]
    [Tooltip("전체 발사 지속 시간")]
    public float FireDuration = 3f;

    [Tooltip("초당 발사 횟수")]
    public float FireRate = 10f;

    [Tooltip("한 번 발사 시 발사체 수")]
    public int ProjectilesPerShot = 1;

    [Tooltip("부채꼴 각도")]
    [Range(0f, 180f)]
    public float SpreadAngle = 60f;

    [Tooltip("발사 상하 각도 (양수: 아래로, 음수: 위로)")]
    public float PitchAngle = 15f;

    [Tooltip("발사 거리")]
    public float Range = 25f;

    [Tooltip("경고 지속 시간")]
    public float WarningDuration = 1.5f;

    [Header("프리펩")]
    [Tooltip("부채꼴 경고 프리펩")]
    public GameObject FanTelegraphPrefab;

    [Tooltip("발사체 프리펩")]
    public ProjectileBase ProjectilePrefab;

    [Tooltip("발사 위치")]
    public Transform MuzzleTransform;

    [Header("VFX/SFX")]
    [Tooltip("발사 사운드")]
    public AudioClip FireSfx;

    private TelegraphIndicator m_CurrentTelegraph;

    protected override IEnumerator ExecutePatternCoroutine()
    {
        if (Target == null) yield break;

        // 1. 플레이어 방향 계산 (초기 방향 고정)
        Vector3 directionToTarget = (Target.position - BossTransform.position).normalized;
        directionToTarget.y = 0;

        // 애니메이션 처리
        Animator animator = BossTransform.GetComponent<Animator>();
        if (animator == null) animator = BossTransform.GetComponentInChildren<Animator>();
        
        if (animator != null)
        {
            animator.SetTrigger("BurstFire");
            animator.SetBool("IsActive", true);
            animator.SetFloat("MoveSpeed", 0f); // 이동 애니메이션 방지
        }

        // 2. 경고 표시
        Vector3 telegraphPosition = BossTransform.position + Vector3.up * 0.1f;
        Quaternion telegraphRotation = Quaternion.LookRotation(directionToTarget);

        if (FanTelegraphPrefab != null)
        {
            GameObject telegraphObj = Instantiate(FanTelegraphPrefab, telegraphPosition, telegraphRotation);
            m_CurrentTelegraph = telegraphObj.GetComponent<TelegraphIndicator>();
            
            if (m_CurrentTelegraph != null)
            {
                m_CurrentTelegraph.Size = Range;
                m_CurrentTelegraph.FanAngle = SpreadAngle;
                m_CurrentTelegraph.Duration = WarningDuration;
                m_CurrentTelegraph.Show(telegraphPosition, telegraphRotation);
            }
        }

        // 3. 경고 시간 대기
        yield return new WaitForSeconds(WarningDuration);

        // 4. 지속 연사 (Sweeping Pattern: Left -> Right)
        float startTime = Time.time;
        float nextFireTime = 0f;

        while (Time.time < startTime + FireDuration)
        {
            // 타겟 방향 지속 업데이트 제거 (초기 방향 directionToTarget 유지)
            // 보스가 회전하지 않도록 고정하거나, 필요시 보스 몸체만 회전시킬 수 있음
            // 여기서는 경고된 범위로만 쏘도록 함

            if (Time.time >= nextFireTime)
            {
                // 현재 진행률 (0.0 ~ 1.0)
                float t = (Time.time - startTime) / FireDuration;
                
                // 현재 각도 계산 (Left -> Right)
                float currentYawAngle = Mathf.Lerp(-SpreadAngle / 2f, SpreadAngle / 2f, t);

                FireBarrage(directionToTarget, currentYawAngle);
                
                if (FireSfx)
                {
                    AudioUtility.CreateSFX(FireSfx, BossTransform.position, AudioUtility.AudioGroups.EnemyAttack, 0f);
                }

                nextFireTime = Time.time + (1f / FireRate);
            }

            yield return null;
        }

        // 경고 정리
        if (m_CurrentTelegraph != null)
        {
            Destroy(m_CurrentTelegraph.gameObject);
        }

        // 애니메이션 종료
        if (animator != null)
        {
            animator.SetBool("IsActive", false);
        }

        OnPatternComplete();
    }

    void FireBarrage(Vector3 centerDirection, float yawAngle)
    {
        if (ProjectilePrefab == null) return;

        Vector3 muzzlePos = MuzzleTransform != null ? MuzzleTransform.position : BossTransform.position + Vector3.up * 1.5f;

        for (int i = 0; i < ProjectilesPerShot; i++)
        {
            // 다중 발사체일 경우, 현재 각도 주변으로 약간의 산탄 적용 (선택 사항)
            float spreadOffset = 0f;
            if (ProjectilesPerShot > 1)
            {
                spreadOffset = Random.Range(-5f, 5f); // 5도 내외의 좁은 산탄
            }

            // 최종 각도 계산
            float finalYaw = yawAngle + spreadOffset;
            
            // 상하 각도 (Pitch) 적용
            Quaternion rotation = Quaternion.LookRotation(centerDirection) * Quaternion.Euler(PitchAngle, finalYaw, 0);
            
            // 발사체 생성
            ProjectileBase projectile = Instantiate(ProjectilePrefab, muzzlePos, rotation);
            
            if (projectile != null)
            {
                // 수동 발사
                projectile.Shoot(BossTransform.gameObject, Vector3.zero, 0f);
            }
        }
    }
}
