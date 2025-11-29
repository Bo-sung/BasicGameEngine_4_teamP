using System.Collections;
using UnityEngine;

/// <summary>
/// 직선 레이저 공격 패턴
/// 1. 플레이어 방향으로 직선 경고 표시
/// 2. 경고 후 강력한 레이저 발사
/// </summary>
public class LaserBeamAttack : BossAttackPattern
{
    [Header("레이저 설정")]
    [Tooltip("레이저 사거리")]
    public float LaserRange = 50f;

    [Tooltip("레이저 너비")]
    public float LaserWidth = 1f;

    [Tooltip("레이저 지속 시간")]
    public float LaserDuration = 1f;

    [Tooltip("경고 지속 시간")]
    public float WarningDuration = 2f;

    [Header("프리펩")]
    [Tooltip("직선 경고 프리펩")]
    public GameObject LineTelegraphPrefab;

    [Tooltip("레이저 LineRenderer")]
    public LineRenderer LaserLineRenderer;

    [Header("VFX/SFX")]
    [Tooltip("레이저 발사 사운드")]
    public AudioClip LaserFireSfx;

    [Tooltip("레이저 충전 사운드")]
    public AudioClip LaserChargeSfx;

    private TelegraphIndicator m_CurrentTelegraph;

    protected override IEnumerator ExecutePatternCoroutine()
    {
        if (Target == null) yield break;

        // 애니메이션 처리
        Animator animator = BossTransform.GetComponent<Animator>();
        if (animator == null) animator = BossTransform.GetComponentInChildren<Animator>();
        
        if (animator != null)
        {
            animator.SetTrigger("LaserBeam");
            animator.SetFloat("MoveSpeed", 0f);
        }

        // 1. 플레이어 방향 계산
        Vector3 directionToTarget = (Target.position - BossTransform.position).normalized;
        directionToTarget.y = 0; // 수평 방향만

        // 2. 경고 표시
        Vector3 telegraphPosition = BossTransform.position + Vector3.up * 0.1f;
        Quaternion telegraphRotation = Quaternion.LookRotation(directionToTarget);

        if (LineTelegraphPrefab != null)
        {
            GameObject telegraphObj = Instantiate(LineTelegraphPrefab, telegraphPosition, telegraphRotation);
            m_CurrentTelegraph = telegraphObj.GetComponent<TelegraphIndicator>();
            
            if (m_CurrentTelegraph != null)
            {
                m_CurrentTelegraph.Size = LaserRange;
                m_CurrentTelegraph.LineWidth = LaserWidth;
                m_CurrentTelegraph.Duration = WarningDuration;
                m_CurrentTelegraph.Show(telegraphPosition, telegraphRotation);
            }
        }

        // 충전 사운드
        if (LaserChargeSfx)
        {
            AudioUtility.CreateSFX(LaserChargeSfx, BossTransform.position, AudioUtility.AudioGroups.EnemyAttack, 0f);
        }

        // 3. 경고 시간 대기
        yield return new WaitForSeconds(WarningDuration);

        // 4. 레이저 발사 및 지속 데미지 처리
        float elapsedTime = 0f;
        if (LaserLineRenderer != null) LaserLineRenderer.enabled = true;

        // 발사 사운드
        if (LaserFireSfx)
        {
            AudioUtility.CreateSFX(LaserFireSfx, BossTransform.position, AudioUtility.AudioGroups.EnemyAttack, 0f);
        }

        while (elapsedTime < LaserDuration)
        {
            elapsedTime += Time.deltaTime;

            // 매 프레임 레이저 방향 및 충돌 계산
            UpdateLaser(directionToTarget);

            yield return null;
        }

        // 5. 레이저 종료
        if (LaserLineRenderer != null)
        {
            LaserLineRenderer.enabled = false;
        }

        // 경고 정리
        if (m_CurrentTelegraph != null)
        {
            Destroy(m_CurrentTelegraph.gameObject);
        }

        OnPatternComplete();
    }

    void UpdateLaser(Vector3 direction)
    {
        Vector3 startPos = BossTransform.position + Vector3.up * 1.5f; // 보스 가슴 높이
        Vector3 endPos = startPos + direction * LaserRange;

        // Raycast로 레이저 충돌 감지
        RaycastHit hit;
        if (Physics.Raycast(startPos, direction, out hit, LaserRange, ~LayerMask.GetMask("Enemy")))
        {
            endPos = hit.point;

            // 플레이어 피격 처리 (지속 데미지)
            Damageable damageable = hit.collider.GetComponent<Damageable>();
            if (damageable != null)
            {
                // 초당 데미지로 환산하여 적용
                float damagePerFrame = (Damage / LaserDuration) * Time.deltaTime;
                damageable.InflictDamage(damagePerFrame, true, BossTransform.gameObject);
            }
        }

        // LineRenderer로 레이저 시각화 업데이트
        if (LaserLineRenderer != null)
        {
            LaserLineRenderer.SetPosition(0, startPos);
            LaserLineRenderer.SetPosition(1, endPos);
            LaserLineRenderer.startWidth = LaserWidth;
            LaserLineRenderer.endWidth = LaserWidth;
        }
    }
}
