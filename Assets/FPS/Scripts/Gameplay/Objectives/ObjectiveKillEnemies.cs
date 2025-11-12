using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class ObjectiveKillEnemies : Objective
    {
        [Tooltip("Chose whether you need to kill every enemies or only a minimum amount")]
        public bool MustKillAllEnemies = true;

        [Tooltip("If MustKillAllEnemies is false, this is the amount of enemy kills required")]
        public int KillsToCompleteObjective = 5;

        [Tooltip("Start sending notification about remaining enemies when this amount of enemies is left")]
        public int NotificationEnemiesRemainingThreshold = 3;

        int m_KillTotal;

        protected override void Start()
        {
            base.Start();

            EventManager.AddListener<EnemyKillEvent>(OnEnemyKilled);

            // Chỉ tính enemy thường, bỏ boss("Enemy"가 붙은 태그만 찾습니다.)
            var allEnemies = GameObject.FindGameObjectsWithTag("Enemy"); 
            KillsToCompleteObjective = allEnemies.Length;

            // set a title và description
            Title = string.IsNullOrEmpty(Title) 
                ? $"Eliminate {KillsToCompleteObjective} enemies" 
                : Title;

            Description = GetUpdatedCounterAmount();
        }

        void OnEnemyKilled(EnemyKillEvent evt)
        {
            if (evt.Enemy.CompareTag("Boss")) return; // bỏ qua boss(보스 트그 빼다)
            if (IsCompleted) return;

            m_KillTotal++;

            int targetRemaining = MustKillAllEnemies ? KillsToCompleteObjective - m_KillTotal : KillsToCompleteObjective - m_KillTotal;

            if (targetRemaining <= 0)
                CompleteObjective(string.Empty, GetUpdatedCounterAmount(), $"Objective complete: {Title}");
            else
            {
                string notificationText = NotificationEnemiesRemainingThreshold >= targetRemaining
                    ? $"{targetRemaining} enemies left"
                    : string.Empty;

                UpdateObjective(string.Empty, GetUpdatedCounterAmount(), notificationText);
            }
        }

        string GetUpdatedCounterAmount()
        {
            return m_KillTotal + " / " + KillsToCompleteObjective;
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<EnemyKillEvent>(OnEnemyKilled);
        }
    }
}
