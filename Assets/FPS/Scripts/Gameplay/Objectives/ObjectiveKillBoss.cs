using UnityEngine;

public class ObjectiveKillBoss : Objective
{
    [Tooltip("보스 적 게임 오브젝트 참조")]
    public GameObject BossEnemy;

    [Tooltip("보스 이름 (알림 및 설명에 사용)")]
    public string BossName = "Boss";

    protected override void Start()
    {
        base.Start();

        EventManager.AddListener<EnemyKillEvent>(OnEnemyKilled);

        // 기본 제목과 설명 설정
        if (string.IsNullOrEmpty(Title))
            Title = "Defeat " + BossName;

        if (string.IsNullOrEmpty(Description))
            Description = "Find and eliminate " + BossName;

        // 목표 업데이트
        UpdateObjective(string.Empty, "Not Defeated", string.Empty);
    }

    void OnEnemyKilled(EnemyKillEvent evt)
    {
        if (IsCompleted)
            return;

        // 죽은 적이 보스인지 확인
        if (evt.Enemy == BossEnemy &&
            (evt.Enemy.GetComponent<EnemyController>()))
        {
            // 보스 처치 목표 완료
            CompleteObjective(string.Empty, "Defeated!", BossName + " has been defeated!");

            // 게임 승리 이벤트 발생
            // 모든 목표가 완료되었다고 알림 (GameFlowManager에서 이 이벤트를 수신)
            AllObjectivesCompletedEvent allObjectivesEvent = Events.AllObjectivesCompletedEvent;
            EventManager.Broadcast(allObjectivesEvent);
        }
    }

    void OnDestroy()
    {
        EventManager.RemoveListener<EnemyKillEvent>(OnEnemyKilled);
    }
}