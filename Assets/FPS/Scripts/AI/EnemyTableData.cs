using UnityEngine;

namespace Unity.FPS.AI
{
    // ========== 적 데이터 구조체 ==========
    [System.Serializable]
    public class EnemyTableData
    {
        [Header("기본 정보")]
        public string enemyID;
        public string enemyName;

        [Header("체력 설정")]
        public float maxHealth = 100f;

        [Header("이동 설정")]
        public float moveSpeed = 3.5f;
        public float angularSpeed = 120f;
        public float acceleration = 8f;
        public float orientationSpeed = 10f;

        [Header("경로 찾기")]
        public float pathReachingRadius = 2f;
        public float selfDestructYHeight = -20f;

        [Header("탐지 설정")]
        public float detectionRange = 20f;
        public float attackRange = 10f;
        public float knownTargetTimeout = 4f;

        [Header("공격 설정")]
        public string weaponID = "pistol_basic";
        public bool swapToNextWeapon = false;
        public float delayAfterWeaponSwap = 0f;

        [Header("눈 색상")]
        public Color defaultEyeColor = Color.cyan;
        public Color attackEyeColor = Color.red;

        [Header("피격 효과")]
        public float flashOnHitDuration = 0.5f;

        [Header("사망 설정")]
        public float deathDuration = 0f;
        public float dropRate = 0.5f;

        [Header("에셋 경로")]
        public string damageTickSoundPath;      // "Audio/Enemy/Damage"
        public string deathVfxPath;             // "Effects/Enemy_Death"
        public string lootPrefabPath;           // "Prefabs/Pickups/HealthPickup"
    }
}
