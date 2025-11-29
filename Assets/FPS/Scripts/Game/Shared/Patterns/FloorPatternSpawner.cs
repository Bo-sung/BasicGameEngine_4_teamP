using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 바닥 패턴 스포너
/// 보스가 이 매니저를 통해 바닥 패턴을 동적으로 생성
/// 시전자 정보(CasterInfo)만 주입하면 각 패턴이 자체 설정값으로 동작
/// </summary>
public class FloorPatternSpawner : MonoBehaviour
{
    [System.Serializable]
    public class PatternPrefabConfig
    {
        [Tooltip("패턴 타입 이름 (예: Sphere, Square, Triangle 등)")]
        public string patternName;

        [Tooltip("패턴 프리팹")]
        public FloorPatternBase patternPrefab;
    }

    // ===== 프리펩 설정 =====
    [Tooltip("패턴별 프리팹 목록")]
    public List<PatternPrefabConfig> patternPrefabs = new List<PatternPrefabConfig>();

    [Tooltip("생성된 패턴을 관리할 부모 오브젝트")]
    public Transform patternContainer;

    [Tooltip("동시에 활성화될 최대 패턴 수")]
    public int maxActivePatterns = 5;

    // ===== 패턴 풀 =====
    private Dictionary<string, Queue<FloorPatternBase>> patternPools =
        new Dictionary<string, Queue<FloorPatternBase>>();

    private List<FloorPatternBase> activePatterns = new List<FloorPatternBase>();

    // ===== 이벤트 =====
    public delegate void PatternEventHandler(FloorPatternBase pattern);
    public event PatternEventHandler OnPatternSpawned;
    public event PatternEventHandler OnPatternEnded;

    void Awake()
    {
            if (patternContainer == null)
                patternContainer = transform;

            // 패턴 풀 초기화
            InitializePatternPools();
    }

    void Update()
    {
        UpdatePatterns();
    }

    /// <summary>
    /// 패턴 풀 초기화
    /// </summary>
    private void InitializePatternPools()
    {
        foreach (var config in patternPrefabs)
        {
            if (config.patternPrefab == null)
            {
                Debug.LogWarning($"패턴 '{config.patternName}'의 프리팹이 null입니다!");
                continue;
            }

            Queue<FloorPatternBase> pool = new Queue<FloorPatternBase>();
            patternPools[config.patternName] = pool;

            // 각 패턴별 5개씩 미리 생성
            for (int i = 0; i < 5; i++)
            {
                FloorPatternBase pattern = Instantiate(config.patternPrefab, patternContainer);
                pattern.gameObject.SetActive(false);
                pool.Enqueue(pattern);
            }

            Debug.Log($"[패턴 풀] '{config.patternName}' 풀 생성됨");
        }
    }

    /// <summary>
    /// 패턴 생성 (시전자 정보만 전달)
    /// </summary>
    public FloorPatternBase SpawnPattern(string patternName, FloorPatternBase.CasterInfo casterInfo)
    {
        // 최대 개수 확인
        if (activePatterns.Count >= maxActivePatterns)
        {
            Debug.LogWarning($"활성 패턴이 최대({maxActivePatterns})에 도달했습니다!");
            return null;
        }

        // 풀에서 패턴 가져오기
        FloorPatternBase pattern = GetPatternFromPool(patternName);
        if (pattern == null)
            return null;

        // 시전자 정보 주입
        pattern.Initialize(casterInfo);
        pattern.gameObject.SetActive(true);

        // 활성 리스트에 추가
        activePatterns.Add(pattern);

        // 이벤트 발생
        OnPatternSpawned?.Invoke(pattern);

        return pattern;
    }

    /// <summary>
    /// 풀에서 패턴 가져오기
    /// </summary>
    private FloorPatternBase GetPatternFromPool(string patternName)
    {
        if (!patternPools.ContainsKey(patternName))
        {
            Debug.LogError($"패턴 풀 '{patternName}'을 찾을 수 없습니다!");
            return null;
        }

        Queue<FloorPatternBase> pool = patternPools[patternName];

        // 풀에 패턴이 있으면 반환
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }

        // 풀이 비어있으면 새로 생성
        Debug.LogWarning($"[패턴 풀] '{patternName}' 풀이 비었으므로 새로 생성합니다!");

        var prefab = patternPrefabs.Find(p => p.patternName == patternName)?.patternPrefab;
        if (prefab == null)
            return null;

        FloorPatternBase newPattern = Instantiate(prefab, patternContainer);
        return newPattern;
    }

    /// <summary>
    /// 활성 패턴 업데이트 (매 프레임 호출)
    /// </summary>
    private void UpdatePatterns()
    {
        for (int i = activePatterns.Count - 1; i >= 0; i--)
        {
            FloorPatternBase pattern = activePatterns[i];
            pattern.UpdatePattern();

            // 패턴이 비활성화되면 풀로 반환
            if (!pattern.gameObject.activeSelf)
            {
                activePatterns.RemoveAt(i);
                ReturnPatternToPool(pattern);
                OnPatternEnded?.Invoke(pattern);
            }
        }
    }

    /// <summary>
    /// 패턴을 풀로 반환
    /// </summary>
    private void ReturnPatternToPool(FloorPatternBase pattern)
    {
        string patternTypeName = pattern.GetType().Name;

        // 타입 이름에서 "FloorPattern" 제거
        string poolName = patternTypeName.Replace("FloorPattern", "");

        if (patternPools.ContainsKey(poolName))
        {
            patternPools[poolName].Enqueue(pattern);
        }
        else
        {
            Debug.LogWarning($"패턴 풀 '{poolName}'을 찾을 수 없습니다!");
            Destroy(pattern.gameObject);
        }
    }

    /// <summary>
    /// 모든 활성 패턴 즉시 종료
    /// </summary>
    public void StopAllPatterns()
    {
        foreach (var pattern in activePatterns)
        {
            pattern.gameObject.SetActive(false);
        }
        activePatterns.Clear();
    }

    /// <summary>
    /// 활성 패턴 개수 반환
    /// </summary>
    public int GetActivePatternCount()
    {
        return activePatterns.Count;
    }

    /// <summary>
    /// 활성 패턴이 있는지 확인
    /// </summary>
    public bool HasActivePatterns()
    {
        return activePatterns.Count > 0;
    }

    /// <summary>
    /// 디버그: 현재 활성 패턴 출력
    /// </summary>
    public void DebugPrintActivePatterns()
    {
        Debug.Log($"=== 활성 패턴 ({activePatterns.Count}/{maxActivePatterns}) ===");
        foreach (var pattern in activePatterns)
        {
            pattern.DebugPrintInfo();
        }
    }

    /// <summary>
    /// 디버그: 풀 정보 출력
    /// </summary>
    public void DebugPrintPoolInfo()
    {
        Debug.Log("=== 패턴 풀 정보 ===");
        foreach (var kvp in patternPools)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value.Count}개");
        }
        Debug.Log($"활성 패턴: {activePatterns.Count}개");
    }
}