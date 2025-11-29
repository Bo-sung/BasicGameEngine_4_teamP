using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using System.IO;

/// <summary>
/// Unity Editor 스크립트: Boss_SciFiWarrior 프리펩을 자동으로 생성합니다.
/// 사용법: Unity 메뉴 > Tools > Create Boss SciFiWarrior Prefab
/// </summary>
public class BossSciFiWarriorPrefabCreator : EditorWindow
{
    [MenuItem("Tools/Create Boss SciFiWarrior Prefab")]
    public static void CreateBossPrefab()
    {
        // 1. Enemy_HoverBot 프리펩 로드
        GameObject hoverBotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/FPS/Prefabs/Enemies/Enemy_HoverBot.prefab");
        
        if (hoverBotPrefab == null)
        {
            Debug.LogError("Enemy_HoverBot.prefab을 찾을 수 없습니다!");
            return;
        }

        // 2. HPCharacter 프리펩 로드
        GameObject hpCharacterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/SciFiWarriorPBRHPPolyart/Prefabs/HPCharacter.prefab");
        
        if (hpCharacterPrefab == null)
        {
            Debug.LogError("HPCharacter.prefab을 찾을 수 없습니다!");
            return;
        }

        // 3. 씬에 Enemy_HoverBot 인스턴스 생성
        GameObject bossInstance = PrefabUtility.InstantiatePrefab(hoverBotPrefab) as GameObject;
        bossInstance.name = "Boss_SciFiWarrior";

        // 4. EnemyMobile을 BossMobile로 교체
        EnemyMobile enemyMobile = bossInstance.GetComponent<EnemyMobile>();
        if (enemyMobile != null)
        {
            // 기존 설정 저장
            var health = bossInstance.GetComponent<Health>();
            var navMeshAgent = bossInstance.GetComponent<NavMeshAgent>();
            
            // EnemyMobile 제거 및 BossMobile 추가
            DestroyImmediate(enemyMobile);
            BossMobile bossMobile = bossInstance.AddComponent<BossMobile>();
            
            // BossMobile 설정
            bossMobile.IsBoss = true;
            bossMobile.BossHealthMultiplier = 5f;
            bossMobile.MinionSpawnHealthThreshold = 0.5f;
            bossMobile.MinionsPerSpawn = 3;
            bossMobile.MinionSpawnInterval = 10f;
            // PathReachingRadius, OrientationSpeed, SelfDestructYHeight는 EnemyController 속성이므로
            // Enemy_HoverBot 프리펩에서 이미 설정되어 있음
            
            // EyeColorMaterial 및 BodyMaterial 설정 (기존 Enemy_HoverBot 값 유지)
            Material eyeMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/FPS/Art/Materials/Enemies/Mat_Enemy_Eye.mat");
            Material bodyMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/FPS/Art/Materials/Enemies/Mat_Enemy_Body.mat");
            
            if (eyeMaterial != null)
            {
                SerializedObject so = new SerializedObject(bossMobile);
                so.FindProperty("EyeColorMaterial").objectReferenceValue = eyeMaterial;
                so.ApplyModifiedProperties();
            }
            
            if (bodyMaterial != null)
            {
                SerializedObject so = new SerializedObject(bossMobile);
                so.FindProperty("BodyMaterial").objectReferenceValue = bodyMaterial;
                so.ApplyModifiedProperties();
            }
        }

        // 5. 기존 모델 제거 (Prefab_MikeZ_Accessories)
        Transform oldModel = bossInstance.transform.Find("Prefab_MikeZ_Accessories");
        if (oldModel != null)
        {
            DestroyImmediate(oldModel.gameObject);
        }

        // 6. HPCharacter 모델 추가
        GameObject modelInstance = PrefabUtility.InstantiatePrefab(hpCharacterPrefab) as GameObject;
        modelInstance.transform.SetParent(bossInstance.transform);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;
        modelInstance.name = "HPCharacter_Model";

        // 7. Animator 찾기 및 설정
        Animator animator = modelInstance.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            BossMobile bossMobile = bossInstance.GetComponent<BossMobile>();
            if (bossMobile != null)
            {
                SerializedObject so = new SerializedObject(bossMobile);
                so.FindProperty("BossAnimator").objectReferenceValue = animator;
                so.ApplyModifiedProperties();
            }

            // DetectionModule에도 Animator 설정
            Transform detectionModule = bossInstance.transform.Find("DetectionModule");
            if (detectionModule != null)
            {
                DetectionModule detection = detectionModule.GetComponent<DetectionModule>();
                if (detection != null)
                {
                    SerializedObject so = new SerializedObject(detection);
                    so.FindProperty("Animator").objectReferenceValue = animator;
                    so.ApplyModifiedProperties();
                }
            }
        }

        // 8. WeaponRoot 위치 조정
        Transform weaponRoot = bossInstance.transform.Find("WeaponRoot");
        if (weaponRoot != null)
        {
            weaponRoot.localPosition = new Vector3(0, 1.5f, 0.3f);
            weaponRoot.localRotation = Quaternion.Euler(0, 180, 0);
            
            // Weapon 이름 변경
            Transform weapon = weaponRoot.Find("Weapon_EyeLazers");
            if (weapon != null)
            {
                weapon.name = "Weapon_AssaultRifle";
                
                // WeaponController 설정 (EyeLazers 스펙 유지)
                WeaponController weaponController = weapon.GetComponent<WeaponController>();
                if (weaponController != null)
                {
                    SerializedObject so = new SerializedObject(weaponController);
                    so.FindProperty("WeaponName").stringValue = "Boss Assault Rifle";
                    so.ApplyModifiedProperties();
                }
            }
        }

        // 9. HitBox 위치 조정
        Transform hitBox = bossInstance.transform.Find("HitBox");
        if (hitBox != null)
        {
            hitBox.localPosition = new Vector3(0, 1.5f, 0);
            
            SphereCollider collider = hitBox.GetComponent<SphereCollider>();
            if (collider != null)
            {
                collider.radius = 0.6f;
            }
        }

        // 10. HealthBarPivot 위치 조정
        Transform healthBarPivot = bossInstance.transform.Find("HealthBarPivot");
        if (healthBarPivot != null)
        {
            healthBarPivot.localPosition = new Vector3(0, 2.2f, 0);
        }

        // 11. ShadowProjector 위치 조정
        Transform shadowProjector = bossInstance.transform.Find("ShadowProjector");
        if (shadowProjector != null)
        {
            shadowProjector.localPosition = new Vector3(0, 0.1f, 0);
        }

        // 12. NavMeshAgent 설정
        NavMeshAgent navAgent = bossInstance.GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.height = 2.0f;
            navAgent.radius = 0.5f;
            navAgent.speed = 3.5f;
            navAgent.acceleration = 8f;
        }

        // 13. 프리펩으로 저장
        string prefabPath = "Assets/FPS/Prefabs/Enemies/Boss_SciFiWarrior.prefab";
        
        // 기존 프리펩이 있으면 삭제
        if (File.Exists(prefabPath))
        {
            AssetDatabase.DeleteAsset(prefabPath);
        }
        
        // 새 프리펩 생성
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bossInstance, prefabPath);
        
        // 씬에서 인스턴스 제거
        DestroyImmediate(bossInstance);
        
        // 에셋 새로고침
        AssetDatabase.Refresh();
        
        // 생성된 프리펩 선택
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        
        Debug.Log($"Boss_SciFiWarrior 프리펩이 성공적으로 생성되었습니다: {prefabPath}");
        EditorUtility.DisplayDialog("성공", 
            "Boss_SciFiWarrior 프리펩이 생성되었습니다!\n\n" +
            "다음 단계:\n" +
            "1. 프리펩을 열어 WeaponRoot와 GunMuzzle 위치를 미세 조정하세요.\n" +
            "2. HPCharacter 모델의 손 위치에 맞게 무기 위치를 조정하세요.\n" +
            "3. 테스트 씬에서 동작을 확인하세요.", 
            "확인");
    }
}
