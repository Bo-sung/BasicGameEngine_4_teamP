using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.AI
{
    public static partial class Table
    {
        public static class Enemy
        {
            private static Dictionary<string, EnemyTableData> enemyDataDict;

            static Enemy()
            {
                LoadEnemyDataTable();
            }

            private static void LoadEnemyDataTable()
            {
                enemyDataDict = new Dictionary<string, EnemyTableData>();

                // TODO: 실제로는 ScriptableObject나 CSV에서 로드
                // 지금은 하드코딩된 샘플 데이터

                // 기본 적 (약함)
                enemyDataDict["enemy_basic"] = new EnemyTableData
                {
                    enemyID = "enemy_basic",
                    enemyName = "기본 적",
                    maxHealth = 50f,
                    moveSpeed = 3.5f,
                    angularSpeed = 120f,
                    acceleration = 8f,
                    orientationSpeed = 10f,
                    pathReachingRadius = 2f,
                    detectionRange = 20f,
                    attackRange = 10f,
                    knownTargetTimeout = 4f,
                    weaponID = "pistol_basic",
                    swapToNextWeapon = false,
                    delayAfterWeaponSwap = 0f,
                    defaultEyeColor = Color.cyan,
                    attackEyeColor = Color.red,
                    flashOnHitDuration = 0.5f,
                    deathDuration = 0f,
                    dropRate = 0.3f,
                    damageTickSoundPath = "Audio/Enemy/Damage",
                    deathVfxPath = "Effects/Enemy_Death",
                    lootPrefabPath = "Prefabs/Pickups/HealthPickup"
                };

                // 강한 적 (높은 체력, 강한 무기)
                enemyDataDict["enemy_heavy"] = new EnemyTableData
                {
                    enemyID = "enemy_heavy",
                    enemyName = "헤비 적",
                    maxHealth = 150f,
                    moveSpeed = 2.5f,
                    angularSpeed = 90f,
                    acceleration = 6f,
                    orientationSpeed = 8f,
                    pathReachingRadius = 2f,
                    detectionRange = 25f,
                    attackRange = 15f,
                    knownTargetTimeout = 6f,
                    weaponID = "rifle_assault",
                    swapToNextWeapon = false,
                    delayAfterWeaponSwap = 0f,
                    defaultEyeColor = new Color(1f, 0.5f, 0f),
                    attackEyeColor = Color.red,
                    flashOnHitDuration = 0.5f,
                    deathDuration = 0f,
                    dropRate = 0.7f,
                    damageTickSoundPath = "Audio/Enemy/Damage_Heavy",
                    deathVfxPath = "Effects/Enemy_Death_Large",
                    lootPrefabPath = "Prefabs/Pickups/AmmoPickup"
                };

                // 빠른 적 (낮은 체력, 빠른 이동)
                enemyDataDict["enemy_fast"] = new EnemyTableData
                {
                    enemyID = "enemy_fast",
                    enemyName = "빠른 적",
                    maxHealth = 30f,
                    moveSpeed = 6f,
                    angularSpeed = 180f,
                    acceleration = 12f,
                    orientationSpeed = 15f,
                    pathReachingRadius = 1.5f,
                    detectionRange = 18f,
                    attackRange = 8f,
                    knownTargetTimeout = 3f,
                    weaponID = "pistol_basic",
                    swapToNextWeapon = false,
                    delayAfterWeaponSwap = 0f,
                    defaultEyeColor = Color.green,
                    attackEyeColor = Color.yellow,
                    flashOnHitDuration = 0.3f,
                    deathDuration = 0f,
                    dropRate = 0.2f,
                    damageTickSoundPath = "Audio/Enemy/Damage",
                    deathVfxPath = "Effects/Enemy_Death_Small",
                    lootPrefabPath = "Prefabs/Pickups/JetpackPickup"
                };

                // 터렛 (고정형 적)
                enemyDataDict["enemy_turret"] = new EnemyTableData
                {
                    enemyID = "enemy_turret",
                    enemyName = "터렛",
                    maxHealth = 80f,
                    moveSpeed = 0f,  // 터렛은 움직이지 않음
                    angularSpeed = 0f,
                    acceleration = 0f,
                    orientationSpeed = 5f,
                    pathReachingRadius = 0f,
                    detectionRange = 30f,
                    attackRange = 25f,
                    knownTargetTimeout = 5f,
                    weaponID = "rifle_assault",
                    swapToNextWeapon = false,
                    delayAfterWeaponSwap = 0f,
                    defaultEyeColor = Color.blue,
                    attackEyeColor = Color.red,
                    flashOnHitDuration = 0.5f,
                    deathDuration = 1f,
                    dropRate = 0.5f,
                    damageTickSoundPath = "Audio/Enemy/Damage_Turret",
                    deathVfxPath = "Effects/Enemy_Death_Turret",
                    lootPrefabPath = "Prefabs/Pickups/WeaponPickup"
                };
            }

            public static EnemyTableData GetData(string enemyID)
            {
                if (enemyDataDict.TryGetValue(enemyID, out EnemyTableData data))
                {
                    return data;
                }

                Debug.LogWarning($"적 데이터를 찾을 수 없습니다: {enemyID}");
                return null;
            }

            public static bool HasData(string enemyID)
            {
                return enemyDataDict.ContainsKey(enemyID);
            }

            public static IEnumerable<string> GetAllEnemyIDs()
            {
                return enemyDataDict.Keys;
            }
        }
    }
}
