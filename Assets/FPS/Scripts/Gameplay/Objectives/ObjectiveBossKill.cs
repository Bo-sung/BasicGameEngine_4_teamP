using UnityEngine;
using Unity.FPS.Game;

namespace Unity.FPS.Gameplay
{
    public class ObjectiveBossKill : Objective
    {
        [Tooltip("여기 Boss 넣습니다")]
        public GameObject Boss;
        
        bool m_BossDead = false;

        protected override void Start()
        {
            base.Start();

            if (string.IsNullOrEmpty(Title))
                Title = "Defeat the Boss";

            if (string.IsNullOrEmpty(Description))
                Description = "Eliminate the final boss to complete the mission.";
        }

        void Update()
        {
            if (IsCompleted)
                return;

            if (Boss == null)
                return; // 지금 보스가 없으면 패스

            var bossHealth = Boss.GetComponent<Health>();
            if (bossHealth != null && bossHealth.CurrentHealth <= 0 && !m_BossDead)
            {
                m_BossDead = true;
                CompleteObjective("", "Boss defeated!", "Mission complete!");
            }
        }
    }
}
