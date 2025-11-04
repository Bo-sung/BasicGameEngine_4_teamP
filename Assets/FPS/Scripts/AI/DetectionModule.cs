using System.Linq;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.AI
{
    public class DetectionModule : MonoBehaviour
    {
        [Tooltip("적 AI가 타겟 감지를 위해 사용하는 Raycast의 시작 위치 (예: 눈 위치)")]
        public Transform DetectionSourcePoint;

        [Tooltip("적이 타겟을 감지할 수 있는 최대 거리")]
        public float DetectionRange = 20f;

        [Tooltip("적이 타겟을 공격할 수 있는 최대 거리")]
        public float AttackRange = 10f;

        [Tooltip("적이 더 이상 시야에 없는 타겟을 포기하기까지 걸리는 시간")]
        public float KnownTargetTimeout = 4f;

        [Tooltip("공격 시 애니메이션을 재생할 선택적 Animator")]
        public Animator Animator;

        // 타겟을 처음 감지했을 때 실행되는 이벤트
        public UnityAction onDetectedTarget;
        // 타겟을 놓쳤을 때 실행되는 이벤트
        public UnityAction onLostTarget;

        // 현재 인식하고 있는 타겟 (null이면 타겟 없음)
        public GameObject KnownDetectedTarget { get; private set; }
        // 현재 타겟이 공격 범위 내에 있는지 여부
        public bool IsTargetInAttackRange { get; private set; }
        // 현재 적이 타겟을 실제로 보고 있는지 여부
        public bool IsSeeingTarget { get; private set; }
        // 이전 프레임에 타겟을 알고 있었는지 여부 (타겟 감지/손실 이벤트 판별용)
        public bool HadKnownTarget { get; private set; }

        // 마지막으로 타겟을 본 시간 기록
        protected float TimeLastSeenTarget = Mathf.NegativeInfinity;

        // 게임 내의 모든 Actor(플레이어, 적 등)를 관리하는 매니저
        ActorsManager m_ActorsManager;

        // 애니메이터 파라미터 이름 상수
        const string k_AnimAttackParameter = "Attack";
        const string k_AnimOnDamagedParameter = "OnDamaged";

        protected virtual void Start()
        {
            // 씬에서 ActorsManager를 찾아 저장
            m_ActorsManager = FindAnyObjectByType<ActorsManager>();
            // 찾지 못하면 에러 출력
            DebugUtility.HandleErrorIfNullFindObject<ActorsManager, DetectionModule>(m_ActorsManager, this);
        }

        public virtual void HandleTargetDetection(Actor actor, Collider[] selfColliders)
        {
            // ▶ 현재 타겟 감지 시간 초과 시 타겟을 잊음
            if (KnownDetectedTarget && !IsSeeingTarget && (Time.time - TimeLastSeenTarget) > KnownTargetTimeout)
            {
                KnownDetectedTarget = null;
            }

            // ▶ 시야 안에 있는 가장 가까운 적대 대상 탐색
            float sqrDetectionRange = DetectionRange * DetectionRange;
            IsSeeingTarget = false;
            float closestSqrDistance = Mathf.Infinity;

            foreach (Actor otherActor in m_ActorsManager.Actors)
            {
                // 적대 관계일 때만 탐색 (자기 편 제외)
                if (otherActor.affiliation != actor.affiliation)
                {
                    float sqrDistance = (otherActor.transform.position - DetectionSourcePoint.position).sqrMagnitude;

                    // 감지 거리 이내이면서 지금까지 본 것 중 가장 가까운 대상일 경우
                    if (sqrDistance < sqrDetectionRange && sqrDistance < closestSqrDistance)
                    {
                        // ▶ 감지 대상과의 사이에 장애물이 있는지 Raycast로 확인
                        RaycastHit[] hits = Physics.RaycastAll(
                            DetectionSourcePoint.position,
                            (otherActor.aimPoint.position - DetectionSourcePoint.position).normalized,
                            DetectionRange,
                            -1,
                            QueryTriggerInteraction.Ignore);

                        RaycastHit closestValidHit = new RaycastHit();
                        closestValidHit.distance = Mathf.Infinity;
                        bool foundValidHit = false;

                        foreach (var hit in hits)
                        {
                            // 자기 자신의 콜라이더는 무시하고, 가장 가까운 충돌체를 찾음
                            if (!selfColliders.Contains(hit.collider) && hit.distance < closestValidHit.distance)
                            {
                                closestValidHit = hit;
                                foundValidHit = true;
                            }
                        }

                        // ▶ 유효한 충돌체가 존재하면 해당 Actor가 시야에 들어온 것인지 확인
                        if (foundValidHit)
                        {
                            Actor hitActor = closestValidHit.collider.GetComponentInParent<Actor>();
                            if (hitActor == otherActor)
                            {
                                // 해당 Actor를 실제로 보고 있음
                                IsSeeingTarget = true;
                                closestSqrDistance = sqrDistance;

                                // 마지막으로 본 시간과 현재 감지된 타겟을 갱신
                                TimeLastSeenTarget = Time.time;
                                KnownDetectedTarget = otherActor.aimPoint.gameObject;
                            }
                        }
                    }
                }
            }

            // ▶ 타겟이 공격 범위 안에 있는지 여부 계산
            IsTargetInAttackRange = KnownDetectedTarget != null &&
                                    Vector3.Distance(transform.position, KnownDetectedTarget.transform.position) <=
                                    AttackRange;

            // ▶ 감지 이벤트 처리
            // 이전에는 타겟이 없었는데 새로 감지한 경우
            if (!HadKnownTarget && KnownDetectedTarget != null)
            {
                OnDetect();
            }

            // 이전에는 타겟이 있었는데 지금은 잃은 경우
            if (HadKnownTarget && KnownDetectedTarget == null)
            {
                OnLostTarget();
            }

            // ▶ 다음 프레임을 위해 현재 타겟 존재 여부를 기록
            HadKnownTarget = KnownDetectedTarget != null;
        }

        // 타겟을 잃었을 때 호출되는 메서드 (이벤트 트리거)
        public virtual void OnLostTarget() => onLostTarget?.Invoke();

        // 타겟을 새로 감지했을 때 호출되는 메서드 (이벤트 트리거)
        public virtual void OnDetect() => onDetectedTarget?.Invoke();

        // 피격 시 호출되는 메서드
        public virtual void OnDamaged(GameObject damageSource)
        {
            // 공격한 대상을 즉시 인식
            TimeLastSeenTarget = Time.time;
            KnownDetectedTarget = damageSource;

            // Animator가 연결되어 있다면 피격 애니메이션 재생
            if (Animator)
            {
                Animator.SetTrigger(k_AnimOnDamagedParameter);
            }
        }

        // 공격 애니메이션 트리거
        public virtual void OnAttack()
        {
            if (Animator)
            {
                Animator.SetTrigger(k_AnimAttackParameter);
            }
        }
    }
}
