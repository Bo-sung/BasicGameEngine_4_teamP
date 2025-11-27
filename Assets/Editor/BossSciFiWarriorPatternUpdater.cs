using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BossSciFiWarriorPatternUpdater : MonoBehaviour
{
    [MenuItem("Tools/Update Boss SciFiWarrior Patterns")]
    public static void UpdateBossPrefab()
    {
        string prefabPath = "Assets/FPS/Prefabs/Enemies/Boss_SciFiWarrior.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"Prefab not found at {prefabPath}");
            return;
        }

        // 프리펩 인스턴스화
        GameObject bossInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (bossInstance == null)
        {
            Debug.LogError("Failed to instantiate prefab.");
            return;
        }

        try
        {
            UpdateBossPatterns(bossInstance);

            // 변경사항 저장
            PrefabUtility.SaveAsPrefabAsset(bossInstance, prefabPath);
            Debug.Log("Boss_SciFiWarrior prefab updated successfully with attack patterns!");
        }
        finally
        {
            DestroyImmediate(bossInstance);
        }
    }

    private static void UpdateBossPatterns(GameObject boss)
    {
        BossMobile bossMobile = boss.GetComponent<BossMobile>();
        if (bossMobile == null)
        {
            Debug.LogError("BossMobile component not found on prefab.");
            return;
        }

        // 1. AttackPatterns 자식 오브젝트 생성 또는 찾기
        Transform patternsTransform = boss.transform.Find("AttackPatterns");
        GameObject patternsObj;
        
        if (patternsTransform == null)
        {
            patternsObj = new GameObject("AttackPatterns");
            patternsObj.transform.SetParent(boss.transform, false);
        }
        else
        {
            patternsObj = patternsTransform.gameObject;
        }

        // 2. 공격 패턴 컴포넌트 추가 및 설정
        List<BossAttackPattern> patternsList = new List<BossAttackPattern>();

        // Laser Beam Attack
        LaserBeamAttack laserAttack = GetOrAddComponent<LaserBeamAttack>(patternsObj);
        ConfigureLaserAttack(laserAttack, boss);
        patternsList.Add(laserAttack);

        // Burst Fire Attack
        BurstFireAttack burstAttack = GetOrAddComponent<BurstFireAttack>(patternsObj);
        ConfigureBurstAttack(burstAttack, boss);
        patternsList.Add(burstAttack);

        // Guided Bombardment Attack
        GuidedBombardmentAttack bombAttack = GetOrAddComponent<GuidedBombardmentAttack>(patternsObj);
        ConfigureBombAttack(bombAttack, boss);
        patternsList.Add(bombAttack);

        // 3. BossMobile에 패턴 할당
        bossMobile.AttackPatterns = patternsList.ToArray();
        bossMobile.PatternCooldown = 3f;
        bossMobile.DisableWeaponDuringPattern = true;

        // 4. DetectionModule 범위 조정 (원거리 보스)
        var detectionModule = boss.GetComponentInChildren<DetectionModule>();
        if (detectionModule != null)
        {
            detectionModule.DetectionRange = 40f;
            detectionModule.AttackRange = 30f;
        }

        // 5. Minion Prefab 할당
        string minionPath = "Assets/FPS/Prefabs/Enemies/Minion_RadicalRobot.prefab";
        GameObject minionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(minionPath);
        if (minionPrefab != null)
        {
            bossMobile.MinionPrefab = minionPrefab;
            bossMobile.MinionsPerSpawn = 5; // 쫄몹 수 증가
            bossMobile.MinionSpawnHealthThreshold = 0.75f; // 더 일찍 소환
        }
        else
        {
            Debug.LogWarning($"Minion prefab not found at {minionPath}. Please create it first using 'Tools > Create Minion RadicalRobot Prefab'.");
        }
    }

    private static T GetOrAddComponent<T>(GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        if (component == null)
        {
            component = obj.AddComponent<T>();
        }
        return component;
    }

    private static void ConfigureLaserAttack(LaserBeamAttack attack, GameObject boss)
    {
        attack.PatternName = "Laser Beam";
        attack.Cooldown = 8f;
        attack.Damage = 30f;
        attack.BossTransform = boss.transform;
        
        attack.LaserRange = 50f;
        attack.LaserWidth = 0.5f;
        attack.LaserDuration = 1.5f;
        attack.WarningDuration = 2.0f;

        // 리소스 로드
        attack.LineTelegraphPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FPS/Prefabs/VFX/LineTelegraph.prefab");
        
        // LineRenderer 설정 (없으면 추가)
        if (attack.LaserLineRenderer == null)
        {
            // 자식 오브젝트로 LineRenderer 생성
            Transform lrTransform = attack.transform.Find("LaserRenderer");
            GameObject lrObj;
            if (lrTransform == null)
            {
                lrObj = new GameObject("LaserRenderer");
                lrObj.transform.SetParent(attack.transform, false);
            }
            else
            {
                lrObj = lrTransform.gameObject;
            }

            LineRenderer lr = GetOrAddComponent<LineRenderer>(lrObj);
            lr.useWorldSpace = true;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.red;
            lr.endColor = Color.red;
            lr.enabled = false;
            
            attack.LaserLineRenderer = lr;
        }
    }

    private static void ConfigureBurstAttack(BurstFireAttack attack, GameObject boss)
    {
        attack.PatternName = "Burst Fire";
        attack.Cooldown = 5f;
        attack.Damage = 10f; // 발당 데미지 (Projectile에서 처리되지만 참고용)
        attack.BossTransform = boss.transform;

        attack.FireDuration = 2.5f;
        attack.FireRate = 12f;
        attack.ProjectilesPerShot = 1;
        attack.SpreadAngle = 60f;
        attack.PitchAngle = 10f; // 10도 아래로 발사
        attack.Range = 25f;
        attack.WarningDuration = 1.5f;

        // 리소스 로드
        attack.FanTelegraphPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FPS/Prefabs/VFX/FanTelegraph.prefab");
        
        // 머신건 프로젝타일 사용
        string projectileGuid = "154bfb4989fe16d439e8f3fb7c20b72b"; // MachineGun Projectile
        string projectilePath = AssetDatabase.GUIDToAssetPath(projectileGuid);
        attack.ProjectilePrefab = AssetDatabase.LoadAssetAtPath<ProjectileBase>(projectilePath);

        // Muzzle 찾기
        Transform muzzle = RecursiveFindChild(boss.transform, "GunMuzzle");
        if (muzzle != null)
        {
            attack.MuzzleTransform = muzzle;
        }
    }

    private static void ConfigureBombAttack(GuidedBombardmentAttack attack, GameObject boss)
    {
        attack.PatternName = "Guided Bombardment";
        attack.Cooldown = 12f;
        attack.Damage = 40f;
        attack.BossTransform = boss.transform;

        attack.BombCount = 6;
        attack.ExplosionRadius = 4f;
        attack.ExplosionInterval = 0.2f;
        attack.SpreadRadius = 8f;
        attack.WarningDuration = 2.5f;
        attack.PredictionTime = 1.0f;

        // 리소스 로드
        attack.CircleTelegraphPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FPS/Prefabs/VFX/CircleTelegraph.prefab");
        
        // 폭발 VFX (기존 VFX 재사용 시도)
        // 임시로 BossDeathVFX 사용하거나 기본 파티클 사용
        // 여기서는 null로 두고 나중에 할당하거나 기본값 사용
    }

    private static Transform RecursiveFindChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }
            Transform found = RecursiveFindChild(child, childName);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }
}
