using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 바닥 패턴 Base 클래스 - 콜라이더 캐시 포함
/// 콜라이더도 메시처럼 한번 생성하고 재사용
/// </summary>
public abstract class FloorPatternBase : MonoBehaviour
{
    // ===== 시전자 정보 (주입받음) =====
    public class CasterInfo
    {
        public Vector3 position;        // 시전자 위치
        public Vector3 direction;       // 시전자 바라보는 방향
        public float damage;            // 기본 데미지
        public string[] statusEffects;  // 적용할 상태이상
        public float statusIntensity;   // 상태이상 강도

        public CasterInfo(Vector3 pos, Vector3 dir, float dmg,
            string[] effects = null, float intensity = 0.5f)
        {
            position = pos;
            direction = dir.normalized;
            damage = dmg;
            statusEffects = effects ?? new[] { "Slow" };
            statusIntensity = intensity;
        }
    }

    // ===== 패턴 설정 =====
    [Header("Pattern Settings")]
    [Tooltip("패턴이 표시되는 지속 시간")]
    public float patternDuration = 2f;

    [Tooltip("실제 피해를 입히기까지의 딜레이")]
    public float damageDelay = 1f;

    [Tooltip("매 틱마다 입히는 피해량")]
    public float damagePerTick = 10f;

    [Tooltip("피해 틱 간격")]
    public float tickInterval = 0.5f;

    [Header("Visualization")]
    [Tooltip("경고 단계의 색상")]
    public Color warningColor = Color.yellow * 0.5f;

    [Tooltip("활성화 단계의 색상")]
    public Color activeColor = Color.red * 0.5f;

    [Tooltip("메시 생성 여부")]
    public bool generateMesh = true;

    [Tooltip("콜라이더 자동 생성 여부 (프리팹에 미리 있으면 false)")]
    public bool autoCreateCollider = true;

    // ===== 내부 상태 =====
    protected CasterInfo casterInfo;
    protected float elapsedTime = 0f;
    protected float damageTickTimer = 0f;
    protected bool isDamageActive = false;
    protected HashSet<Health> affectedHealths = new HashSet<Health>();

    // ===== 렌더링 =====
    protected MeshFilter meshFilter;
    protected MeshRenderer meshRenderer;

    // ===== 이벤트 =====
    public delegate void PatternEventHandler();
    public event PatternEventHandler OnPatternStart;
    public event PatternEventHandler OnPatternEnd;

    protected virtual void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        // 콜라이더가 없으면 생성 (자동 생성 활성화 시)
        if (autoCreateCollider && GetComponent<Collider>() == null)
        {
            CreateCollider();
        }

        // 메시 필터가 없으면 생성
        if (meshFilter == null && generateMesh)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        // 메시 캐시 초기화 (앱 시작 시 한번만)
        FloorPatternMeshCache.Initialize();
    }

    /// <summary>
    /// 콜라이더 생성 (각 패턴에서 Override)
    /// </summary>
    protected abstract void CreateCollider();

    /// <summary>
    /// 패턴 초기화 - 시전자 정보 주입
    /// </summary>
    public virtual void Initialize(CasterInfo info)
    {
        if (info == null)
        {
            Debug.LogError("CasterInfo가 null입니다!");
            return;
        }

        casterInfo = info;

        // 위치 설정
        transform.position = casterInfo.position;

        // 방향 설정
        SetupDirection();

        // 메시 설정 (캐시에서 복제)
        if (generateMesh)
        {
            SetupPatternMesh();
        }

        // 콜라이더 범위 조정
        UpdateColliderSize();

        // 상태 초기화
        elapsedTime = 0f;
        damageTickTimer = 0f;
        isDamageActive = false;
        affectedHealths.Clear();

        // 비주얼 설정
        SetupVisualization();

        Debug.Log($"[패턴 생성] {gameObject.name} | 위치: {casterInfo.position} | 데미지: {casterInfo.damage}");
    }

    /// <summary>
    /// 패턴 메시 설정 (캐시에서 복제)
    /// </summary>
    protected virtual void SetupPatternMesh()
    {
        if (meshFilter != null)
        {
            // 기본: 원형 메시 (캐시에서 복제)
            meshFilter.mesh = FloorPatternMeshCache.GetCircleMesh();
        }
    }

    /// <summary>
    /// 콜라이더 크기 업데이트 (각 패턴에서 Override)
    /// </summary>
    protected abstract void UpdateColliderSize();

    /// <summary>
    /// 방향 설정 (Override 가능)
    /// </summary>
    protected virtual void SetupDirection()
    {
        if (casterInfo.direction.sqrMagnitude > 0)
        {
            transform.rotation = Quaternion.LookRotation(casterInfo.direction);
        }
    }

    /// <summary>
    /// 비주얼 설정
    /// </summary>
    protected virtual void SetupVisualization()
    {
        if (meshRenderer == null) return;

        Material mat = meshRenderer.material;
        mat.color = warningColor;
    }

    /// <summary>
    /// 패턴 업데이트 (매 프레임 호출)
    /// </summary>
    public virtual void UpdatePattern()
    {
        if (casterInfo == null) return;

        elapsedTime += Time.deltaTime;

        // ===== 경고 단계 =====
        if (elapsedTime < damageDelay)
        {
            UpdateWarningVisualization();
            return;
        }

        // ===== 활성화 단계 =====
        if (!isDamageActive)
        {
            isDamageActive = true;
            UpdateActiveVisualization();
            OnPatternStart?.Invoke();
            ApplyPatternDamage();
        }

        // ===== 지속 피해 =====
        damageTickTimer += Time.deltaTime;
        if (damageTickTimer >= tickInterval)
        {
            damageTickTimer -= tickInterval;
            ApplyPatternDamage();
        }

        // ===== 패턴 종료 =====
        if (elapsedTime >= patternDuration)
        {
            EndPattern();
        }
    }

    /// <summary>
    /// 경고 시각화 업데이트
    /// </summary>
    protected virtual void UpdateWarningVisualization()
    {
        if (meshRenderer == null) return;
        meshRenderer.material.color = warningColor;
    }

    /// <summary>
    /// 활성화 시각화 업데이트
    /// </summary>
    protected virtual void UpdateActiveVisualization()
    {
        if (meshRenderer == null) return;
        meshRenderer.material.color = activeColor;
    }

    /// <summary>
    /// 패턴 범위 내 콜라이더 반환 (각 패턴에서 Override)
    /// </summary>
    protected abstract Collider[] GetAffectedColliders();

    /// <summary>
    /// 거리 기반 감쇠 비율 계산 (Override 가능)
    /// </summary>
    protected virtual float CalculateDistanceRatio(Vector3 targetPosition)
    {
        float distance = Vector3.Distance(targetPosition, transform.position);
        return Mathf.Clamp01(1f - (distance / 10f));
    }

    /// <summary>
    /// 패턴 범위 내 플레이어에게 피해 적용
    /// </summary>
    protected virtual void ApplyPatternDamage()
    {
        Collider[] affectedColliders = GetAffectedColliders();

        foreach (var coll in affectedColliders)
        {
            Health health = coll.GetComponent<Health>();
            if (health != null)
            {
                float distanceRatio = CalculateDistanceRatio(coll.transform.position);
                float finalDamage = CalculateFinalDamage(distanceRatio);

                health.TakeDamage(finalDamage, gameObject);
                ApplyStatusEffects(health, distanceRatio);
            }
        }
    }

    /// <summary>
    /// 최종 데미지 계산 (Override 가능)
    /// </summary>
    protected virtual float CalculateFinalDamage(float distanceRatio)
    {
        return casterInfo.damage * damagePerTick * distanceRatio / 10f;
    }

    /// <summary>
    /// 상태이상 적용
    /// TODO: 상태이상 시스템 구현 후 활성화
    /// </summary>
    protected virtual void ApplyStatusEffects(Health health, float distanceRatio)
    {
        foreach (string effect in casterInfo.statusEffects)
        {
            float intensity = casterInfo.statusIntensity * distanceRatio;

            switch (effect)
            {
                // case "Slow":
                //     health.ApplyStatusEffect("Slow", 0.7f, tickInterval);
                //     break;

                // case "Burn":
                //     health.ApplyStatusEffect("Burn", intensity, tickInterval);
                //     break;

                // case "Poison":
                //     health.ApplyStatusEffect("Poison", intensity, tickInterval);
                //     break;

                // case "Stun":
                //     if (Random.value < intensity)
                //         health.ApplyStatusEffect("Stun", intensity, 0.5f);
                //     break;

                // case "Curse":
                //     health.ApplyStatusEffect("Curse", intensity, tickInterval);
                //     break;

                // case "Weakness":
                //     health.ApplyStatusEffect("Weakness", intensity, tickInterval);
                //     break;
            }
        }
    }

    /// <summary>
    /// 패턴 종료
    /// </summary>
    protected virtual void EndPattern()
    {
        affectedHealths.Clear();
        OnPatternEnd?.Invoke();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 패턴 진행률 반환 (0~1)
    /// </summary>
    public float GetProgress()
    {
        return Mathf.Clamp01(elapsedTime / patternDuration);
    }

    /// <summary>
    /// 패턴이 활성화되었는지 확인
    /// </summary>
    public bool IsActive()
    {
        return isDamageActive;
    }

    /// <summary>
    /// 디버그용 정보 출력
    /// </summary>
    public virtual void DebugPrintInfo()
    {
        Debug.Log($"=== [{gameObject.name}] ===\n" +
            $"지속시간: {patternDuration}s\n" +
            $"경고시간: {damageDelay}s\n" +
            $"기본피해: {casterInfo?.damage}\n" +
            $"진행률: {GetProgress():P}");
    }
}