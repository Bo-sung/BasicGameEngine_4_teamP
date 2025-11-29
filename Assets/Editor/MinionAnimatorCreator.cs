using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

public class MinionAnimatorCreator : MonoBehaviour
{
    [MenuItem("Tools/Create Minion Animator Controller")]
    public static void CreateMinionAnimator()
    {
        string baseControllerPath = "Assets/FPS/Animation/Controllers/HoverBot_AnimationController.controller";
        string overrideControllerPath = "Assets/FPS/Animation/Controllers/Minion_RadicalRobot.overrideController";

        // Load base controller
        AnimatorController baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(baseControllerPath);
        if (baseController == null)
        {
            Debug.LogError($"Base controller not found at {baseControllerPath}");
            return;
        }

        // Create override controller
        AnimatorOverrideController overrideController = new AnimatorOverrideController(baseController);

        // Find MikeZ animations
        AnimationClip idleClip = FindClip("Anim_ZMIKE_Idle");
        AnimationClip runClip = FindClip("Anim_ZMIKE_Run");
        AnimationClip attackClip = FindClip("Anim_ZMIKE_FireR");
        AnimationClip hurtClip = FindClip("Anim_ZMIKE_HitRegisterFront");
        AnimationClip deathClip = FindClip("Anim_ZMIKE_DeathState");

        // Find HoverBot animations to override
        AnimationClip hoverIdleClip = FindClip("HoverBot_Idle");
        AnimationClip hoverMoveClip = FindClip("HoverBot_Move_Forward");
        AnimationClip hoverAttackClip = FindClip("HoverBot_Attack");
        AnimationClip hoverHurtClip = FindClip("HoverBot_GetHit");
        AnimationClip hoverDeathClip = FindClip("HoverBot_Death");

        // Create override list
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        
        if (hoverIdleClip != null && idleClip != null)
            overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(hoverIdleClip, idleClip));
        
        if (hoverMoveClip != null && runClip != null)
            overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(hoverMoveClip, runClip));
        
        if (hoverAttackClip != null && attackClip != null)
            overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(hoverAttackClip, attackClip));
        
        if (hoverHurtClip != null && hurtClip != null)
            overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(hoverHurtClip, hurtClip));
        
        if (hoverDeathClip != null && deathClip != null)
            overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(hoverDeathClip, deathClip));

        // Apply overrides
        overrideController.ApplyOverrides(overrides);

        // Save asset
        AssetDatabase.CreateAsset(overrideController, overrideControllerPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Minion Animator Override Controller created at {overrideControllerPath}");
        Debug.Log($"Applied {overrides.Count} animation overrides");
    }

    private static AnimationClip FindClip(string clipName)
    {
        string[] guids = AssetDatabase.FindAssets(clipName + " t:AnimationClip");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null)
            {
                Debug.Log($"Found clip: {clipName} at {path}");
                return clip;
            }
        }
        Debug.LogWarning($"Clip not found: {clipName}");
        return null;
    }
}
