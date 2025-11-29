using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 스테이지 설정 데이터
/// </summary>
[CreateAssetMenu(fileName = "StageData", menuName = "Game/StageData", order = 0)]
public class StageData : ScriptableObject
{
    [Header("Weapons")]
    [SerializeField]
    private List<Weapon> stageWeapons = new List<Weapon>();

    [Header("Boss")]
    [SerializeField]
    private EnemyController stageBoss = null;

    [Header("Objectives")]
    [SerializeField]
    private List<Objective> stageObjectives = new List<Objective>();

    // Public Properties (읽기 전용)
    public IReadOnlyList<Weapon> StageWeapons => stageWeapons;
    public EnemyController StageBoss => stageBoss;
    public IReadOnlyList<Objective> StageObjectives => stageObjectives;

    // 유효성 검증
    public bool IsValid()
    {
        if (stageWeapons == null || stageWeapons.Count == 0)
        {
            Debug.LogWarning($"[{name}] StageWeapons is empty!");
            return false;
        }

        if (stageObjectives == null || stageObjectives.Count == 0)
        {
            Debug.LogWarning($"[{name}] StageObjectives is empty!");
            return false;
        }

        return true;
    }

    // Editor 전용 유효성 검사
#if UNITY_EDITOR
    private void OnValidate()
    {
        // null 체크
        stageWeapons?.RemoveAll(w => w == null);
        stageObjectives?.RemoveAll(o => o == null);

        //// 레벨 범위 제한
        //if (stageLevel < 1)
        //{
        //    stageLevel = 1;
        //}
    }
#endif
}
