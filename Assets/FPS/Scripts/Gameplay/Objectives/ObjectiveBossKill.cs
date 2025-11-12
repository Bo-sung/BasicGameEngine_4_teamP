using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class ObjectiveBossKill : Objective
    {
        [Tooltip("Boss object reference (optional)")]
        public GameObject BossReference;

        [Tooltip("Tag used to identify the boss if BossReference is not assigned")]
        public string BossTag = "Boss";

        bool m_IsCompleted = false;

        protected override void Start()
        {
            base.Start();
            Title = string.IsNullOrEmpty(Title) ? "Defeat the Boss" : Title;
            Description = "Boss is alive";
            EventManager.AddListener<EnemyKillEvent>(OnEnemyKilled);
        }

        void OnEnemyKilled(EnemyKillEvent evt)
        {
            if (m_IsCompleted) return;

            // Kiểm tra nếu enemy vừa chết là boss (죽은Enemy 검사---> 나머지 보스)
            if (BossReference != null)
            {
                if (evt.Enemy != BossReference) return;
            }
            else
            {
                if (!evt.Enemy.CompareTag(BossTag)) return;
            }

            CompleteBossObjective();
        }

        void CompleteBossObjective()
        {
            m_IsCompleted = true;
            Debug.Log("[BossDefeatObjective] Boss defeated!");

            // Cập nhật HUD(HUD 업데이드)
            UpdateObjective(string.Empty, "Boss defeated!", "Boss defeated!");

            // Tạo event hiển thị thông báo(이벤트 message)
            DisplayMessageEvent msg = Events.DisplayMessageEvent;
            msg.Message = "Boss defeated! Mission complete!";
            msg.DelayBeforeDisplay = 0f;
            EventManager.Broadcast(msg);

            // Nếu muốn trigger end game chỉ khi boss chết (보스 죽을때 endgame 불어)
            EventManager.Broadcast(Events.AllObjectivesCompletedEvent);
            
            
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<EnemyKillEvent>(OnEnemyKilled);
        }
    }
}
