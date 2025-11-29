using UnityEngine;

// ========== 리소스 매니저 ==========
public static class ResourceManager
{
    // 캐시된 리소스들
    private static System.Collections.Generic.Dictionary<string, AudioClip> audioCache =
        new System.Collections.Generic.Dictionary<string, AudioClip>();
    private static System.Collections.Generic.Dictionary<string, GameObject> prefabCache =
        new System.Collections.Generic.Dictionary<string, GameObject>();

    /// <summary>
    /// 경로로부터 AudioClip 로드
    /// </summary>
    public static AudioClip LoadAudioClip(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        if (audioCache.ContainsKey(path))
            return audioCache[path];

        AudioClip clip = Resources.Load<AudioClip>(path);
        if (clip != null)
            audioCache[path] = clip;

        return clip;
    }

    /// <summary>
    /// 경로로부터 GameObject 프리팹 로드
    /// </summary>
    public static GameObject LoadPrefab(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        if (prefabCache.ContainsKey(path))
            return prefabCache[path];

        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab != null)
            prefabCache[path] = prefab;

        return prefab;
    }
}