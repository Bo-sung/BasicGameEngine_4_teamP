using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MinionPrefabCreator : MonoBehaviour
{
    [MenuItem("Tools/Create Minion RadicalRobot Prefab")]
    public static void CreateMinionPrefab()
    {
        string referencePrefabPath = "Assets/FPS/Prefabs/Enemies/Enemy_HoverBot.prefab";
        string minionPrefabPath = "Assets/FPS/Prefabs/Enemies/Minion_RadicalRobot.prefab";
        string modelPath = "Assets/Tacko3D/RadicalRobotsMike/Art/Mesh/MikeZ.fbx";
        string baseControllerPath = "Assets/FPS/Animation/Controllers/HoverBot_AnimationController.controller";
        string overrideControllerPath = "Assets/FPS/Animation/Controllers/Minion_RadicalRobot.overrideController";

        // 1. Load reference and assets
        GameObject referencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(referencePrefabPath);
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        RuntimeAnimatorController baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(baseControllerPath);

        if (referencePrefab == null || modelPrefab == null || baseController == null)
        {
            Debug.LogError("Missing base assets!");
            return;
        }

        // 2. Create Animator Override Controller
        AnimatorOverrideController overrideController = new AnimatorOverrideController(baseController);
        
        AnimationClip idleClip = FindClip("Anim_ZMIKE_Idle");
        AnimationClip runClip = FindClip("Anim_ZMIKE_Run");
        AnimationClip attackClip = FindClip("Anim_ZMIKE_FireR");
        AnimationClip hurtClip = FindClip("Anim_ZMIKE_HitRegisterFront");
        AnimationClip deathClip = FindClip("Anim_ZMIKE_DeathState");

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(FindClip("HoverBot_Idle"), idleClip));
        overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(FindClip("HoverBot_Move_Forward"), runClip));
        overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(FindClip("HoverBot_Attack"), attackClip));
        overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(FindClip("HoverBot_GetHit"), hurtClip));
        overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(FindClip("HoverBot_Death"), deathClip));

        overrideController.ApplyOverrides(overrides);
        AssetDatabase.CreateAsset(overrideController, overrideControllerPath);

        // 3. Create new GameObject from scratch
        GameObject minion = new GameObject("Minion_RadicalRobot");

        // 4. Copy components from reference (but not Transform)
        CopyComponentsFromReference(referencePrefab, minion);

        // 5. Add MikeZ model
        GameObject model = Instantiate(modelPrefab);
        model.name = "Minion_Model";
        model.transform.SetParent(minion.transform, false);

        // 6. Setup Animator
        Animator animator = model.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            animator = model.AddComponent<Animator>();
        }
        animator.runtimeAnimatorController = overrideController;
        animator.applyRootMotion = false;

        // 7. Copy child objects structure (HitBox, WeaponRoot, etc.)
        CopyChildStructure(referencePrefab, minion);

        // 8. Adjust minion stats
        AdjustMinionStats(minion);

        // 9. Save as prefab
        PrefabUtility.SaveAsPrefabAsset(minion, minionPrefabPath);
        DestroyImmediate(minion);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Minion_RadicalRobot prefab created successfully from scratch!");
    }

    private static void CopyComponentsFromReference(GameObject source, GameObject target)
    {
        // Copy all components except Transform
        Component[] components = source.GetComponents<Component>();
        foreach (Component comp in components)
        {
            if (comp is Transform) continue;

            UnityEditorInternal.ComponentUtility.CopyComponent(comp);
            UnityEditorInternal.ComponentUtility.PasteComponentAsNew(target);
        }
    }

    private static void CopyChildStructure(GameObject source, GameObject target)
    {
        // Copy important child objects (skip model)
        foreach (Transform child in source.transform)
        {
            if (child.name.Contains("Model")) continue; // Skip model, we have our own

            GameObject childCopy = new GameObject(child.name);
            childCopy.transform.SetParent(target.transform, false);
            childCopy.transform.localPosition = child.localPosition;
            childCopy.transform.localRotation = child.localRotation;
            childCopy.transform.localScale = child.localScale;

            // Copy all components from child
            Component[] components = child.GetComponents<Component>();
            foreach (Component comp in components)
            {
                if (comp is Transform) continue;
                UnityEditorInternal.ComponentUtility.CopyComponent(comp);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(childCopy);
            }

            // Recursively copy grandchildren
            CopyChildrenRecursive(child, childCopy.transform);
        }
    }

    private static void CopyChildrenRecursive(Transform source, Transform target)
    {
        foreach (Transform child in source)
        {
            GameObject childCopy = new GameObject(child.name);
            childCopy.transform.SetParent(target, false);
            childCopy.transform.localPosition = child.localPosition;
            childCopy.transform.localRotation = child.localRotation;
            childCopy.transform.localScale = child.localScale;

            Component[] components = child.GetComponents<Component>();
            foreach (Component comp in components)
            {
                if (comp is Transform) continue;
                UnityEditorInternal.ComponentUtility.CopyComponent(comp);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(childCopy);
            }

            CopyChildrenRecursive(child, childCopy.transform);
        }
    }

    private static void AdjustMinionStats(GameObject minion)
    {
        // Health
        var health = minion.GetComponent<Health>();
        if (health != null)
        {
            health.MaxHealth = 50f;
        }

        // Speed
        var agent = minion.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = 8f;
            agent.acceleration = 16f;
        }

        // Weapon position adjustment
        Transform weaponRoot = minion.transform.Find("WeaponRoot");
        if (weaponRoot != null)
        {
            weaponRoot.localPosition = new Vector3(0.4f, 1.2f, 0.5f);
        }
    }

    private static AnimationClip FindClip(string clipName)
    {
        string[] guids = AssetDatabase.FindAssets(clipName + " t:AnimationClip");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }
        Debug.LogWarning($"Clip not found: {clipName}");
        return null;
    }
}
