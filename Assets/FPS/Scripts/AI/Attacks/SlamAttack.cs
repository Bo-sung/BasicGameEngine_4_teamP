using System.Collections;
using UnityEngine;

/// <summary>
/// 내려찍기 공격 패턴
/// 1. 플레이어 예측 위치에 대형 원형 경고 표시
/// 2. 경고 후 강력한 내려찍기 공격
/// </summary>
public class SlamAttack : BossAttackPattern
{
    [Header("내려찍기 설정")]
    [Tooltip("내려찍기 반경")]
    public float SlamRadius = 5f;

    [Tooltip("경고 지속 시간")]
    public float WarningDuration = 2f;

    [Tooltip("플레이어 이동 예측 시간")]
    public float PredictionTime = 1f;

    [Tooltip("내려찍기 높이")]
    public float SlamHeight = 5f;

    [Tooltip("내려찍기 속도")]
    public float SlamSpeed = 20f;

    [Header("프리팹")]
    [Tooltip("원형 경고 프리팹")]
    public GameObject CircleTelegraphPrefab;

    [Tooltip("내려찍기 VFX")]
    public GameObject SlamVfxPrefab;

    [Header("VFX/SFX")]
    [Tooltip("내려찍기 사운드")]
    public AudioClip SlamSfx;

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
            animator.SetTrigger("Slam");
            animator.SetFloat("MoveSpeed", 0f);
        }

        // 1. 플레이어 위치 예측
        Vector3 predictedPosition = PredictPlayerPosition();

        // 2. 경고 표시
        Vector3 telegraphPosition = predictedPosition + Vector3.up * 0.1f;

        if (CircleTelegraphPrefab != null)
        {
            GameObject telegraphObj = Instantiate(CircleTelegraphPrefab, telegraphPosition, Quaternion.identity);
            m_CurrentTelegraph = telegraphObj.GetComponent<TelegraphIndicator>();
            
            if (m_CurrentTelegraph != null)
            {
                m_CurrentTelegraph.Size = SlamRadius;
                m_CurrentTelegraph.Duration = WarningDuration;
                m_CurrentTelegraph.Show(telegraphPosition, Quaternion.identity);
            }
        }

        // 경고 사운드
        if (WarningSfx)
        {
            AudioUtility.CreateSFX(WarningSfx, BossTransform.position, AudioUtility.AudioGroups.EnemyAttack, 0f);
        }

        // 3. 경고 시간 대기
        yield return new WaitForSeconds(WarningDuration);

        // 4. 내려찍기 실행
        yield return StartCoroutine(PerformSlam(predictedPosition));

        // 경고 정리
        if (m_CurrentTelegraph != null)
        {
            Destroy(m_CurrentTelegraph.gameObject);
        }

        OnPatternComplete();
    }

    Vector3 PredictPlayerPosition()
    {
        if (Target == null) return Vector3.zero;

        // 플레이어 속도 추정
        Rigidbody targetRb = Target.GetComponent<Rigidbody>();
        Vector3 velocity = targetRb != null ? targetRb.linearVelocity : Vector3.zero;

        // 예측 위치 계산
        Vector3 predictedPos = Target.position + velocity * PredictionTime;
        predictedPos.y = Target.position.y; // Y축 고정

        return predictedPos;
    }

    IEnumerator PerformSlam(Vector3 targetPosition)
    {
        // 보스를 위로 올림
        Vector3 originalPosition = BossTransform.position;
        Vector3 raisedPosition = originalPosition + Vector3.up * SlamHeight;

        // 올라가기
        float riseTime = 0.3f;
        float elapsedTime = 0f;

        while (elapsedTime < riseTime)
        {
            elapsedTime += Time.deltaTime;
            BossTransform.position = Vector3.Lerp(originalPosition, raisedPosition, elapsedTime / riseTime);
            yield return null;
        }

        // 내려찍기 위치로 이동
        targetPosition.y = raisedPosition.y;
        BossTransform.position = targetPosition;

        // 내려찍기
        if (SlamSfx)
        {
            AudioUtility.CreateSFX(SlamSfx, targetPosition, AudioUtility.AudioGroups.EnemyAttack, 0f);
        }

        elapsedTime = 0f;
        Vector3 groundPosition = new Vector3(targetPosition.x, originalPosition.y, targetPosition.z);

        while (elapsedTime < 0.2f)
        {
            elapsedTime += Time.deltaTime;
            BossTransform.position = Vector3.Lerp(targetPosition, groundPosition, elapsedTime / 0.2f);
            yield return null;
        }

        BossTransform.position = groundPosition;

        // VFX 생성
        if (SlamVfxPrefab != null)
        {
            GameObject vfx = Instantiate(SlamVfxPrefab, groundPosition, Quaternion.identity);
            Destroy(vfx, 5f);
        }

        // 범위 내 데미지 적용
        Collider[] hits = Physics.OverlapSphere(groundPosition, SlamRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject == BossTransform.gameObject) continue;

            Damageable damageable = hit.GetComponent<Damageable>();
            if (damageable != null)
            {
                Actor actor = hit.GetComponent<Actor>();
                Actor bossActor = BossTransform.GetComponent<Actor>();
                
                if (actor != null && bossActor != null && actor.affiliation != bossActor.affiliation)
                {
                    damageable.InflictDamage(Damage, false, BossTransform.gameObject);
                }
            }
        }
    }
}
