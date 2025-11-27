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

        // 1. 플레이어 방향 계산
        Vector3 directionToTarget = (Target.position - BossTransform.position).normalized;
        directionToTarget.y = 0;

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

        // 4. 지속 연사 (미스포춘 궁극기 스타일)
        float startTime = Time.time;
        float nextFireTime = 0f;

        while (Time.time < startTime + FireDuration)
        {
            // 타겟 방향 지속 업데이트 (선택 사항: 고정하려면 루프 밖으로 이동)
            if (Target != null)
            {
                directionToTarget = (Target.position - BossTransform.position).normalized;
                directionToTarget.y = 0;
            }

            if (Time.time >= nextFireTime)
            {
                FireBarrage(directionToTarget);
                
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

        OnPatternComplete();
    }

    void FireBarrage(Vector3 centerDirection)
    {
        if (ProjectilePrefab == null) return;

        Vector3 muzzlePos = MuzzleTransform != null ? MuzzleTransform.position : BossTransform.position + Vector3.up * 1.5f;

        for (int i = 0; i < ProjectilesPerShot; i++)
        {
            // 부채꼴 내 무작위 좌우 각도
            float yawAngle = Random.Range(-SpreadAngle / 2, SpreadAngle / 2);
            
            // 상하 각도 (Pitch) 적용 - 아래로 쏘려면 X축 회전
            Quaternion rotation = Quaternion.LookRotation(centerDirection) * Quaternion.Euler(PitchAngle, yawAngle, 0);
            
            Vector3 direction = rotation * Vector3.forward;

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
