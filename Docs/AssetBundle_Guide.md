# Unity AssetBundle 가이드

## 개요

AssetBundle은 유니티가 제공하는 저수준 에셋 패키징 시스템으로, 에셋을 외부 파일로 빌드하여 런타임에 동적으로 로드할 수 있습니다.

## 주요 특징

### 1. 외부 파일로 에셋 관리
- 빌드와 별도로 에셋을 패키징
- 게임 업데이트 없이 콘텐츠만 교체 가능
- 초기 앱 크기 감소

### 2. 동적 로딩
- 필요할 때만 메모리에 로드
- 서버/CDN에서 다운로드 가능
- DLC, 패치 시스템 구현 가능

### 3. 플랫폼별 에셋 관리
- 플랫폼마다 최적화된 에셋 번들 생성
- 모바일/PC/콘솔 각각 다른 품질 설정 가능

## AssetBundle 생성

### 1. 에셋에 번들 태그 지정

#### Inspector를 통한 설정
1. Project 창에서 에셋 선택
2. Inspector 하단의 `AssetBundle` 섹션
3. `None` → 새 번들 이름 입력 또는 기존 번들 선택
4. Variant 설정 (선택사항, 예: "hd", "sd")

#### 예시
```
Prefabs/Weapons/Sword_01.prefab
└── AssetBundle: weapons
    └── Variant: none

Textures/Sword_Diffuse_HD.png
└── AssetBundle: textures
    └── Variant: hd

Textures/Sword_Diffuse_SD.png
└── AssetBundle: textures
    └── Variant: sd
```

### 2. 에디터 스크립트로 빌드

#### 기본 빌드 스크립트
```csharp
using UnityEditor;
using System.IO;

public class AssetBundleBuilder
{
    [MenuItem("Assets/Build AssetBundles")]
    static void BuildAllAssetBundles()
    {
        string assetBundleDirectory = "Assets/AssetBundles";

        // 출력 폴더 생성
        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }

        // AssetBundle 빌드
        BuildPipeline.BuildAssetBundles(
            assetBundleDirectory,
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows64
        );

        Debug.Log("AssetBundles built successfully!");
    }
}
```

#### 고급 빌드 옵션
```csharp
[MenuItem("Assets/Build AssetBundles (Advanced)")]
static void BuildAssetBundlesAdvanced()
{
    string outputPath = "Assets/AssetBundles";

    if (!Directory.Exists(outputPath))
    {
        Directory.CreateDirectory(outputPath);
    }

    // 빌드 옵션 설정
    BuildAssetBundleOptions options =
        BuildAssetBundleOptions.ChunkBasedCompression |  // LZ4 압축 (빠른 로딩)
        BuildAssetBundleOptions.StrictMode;              // 에러 발생 시 빌드 중단

    // 여러 플랫폼용 빌드
    BuildPipeline.BuildAssetBundles(
        outputPath,
        options,
        BuildTarget.StandaloneWindows64
    );

    AssetDatabase.Refresh();
}
```

### 3. 빌드 결과물
```
Assets/AssetBundles/
├── AssetBundles              (Manifest 파일)
├── AssetBundles.manifest     (전체 번들 정보)
├── weapons                   (무기 번들)
├── weapons.manifest          (무기 번들 정보)
├── enemies                   (적 번들)
└── enemies.manifest          (적 번들 정보)
```

## AssetBundle 로딩

### 1. 로컬 파일에서 로드

#### 동기 로딩
```csharp
using UnityEngine;
using System.IO;

public class LocalBundleLoader : MonoBehaviour
{
    void Start()
    {
        // 번들 경로
        string bundlePath = Path.Combine(Application.streamingAssetsPath, "weapons");

        // 번들 로드
        AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);

        if (bundle == null)
        {
            Debug.LogError("Failed to load AssetBundle!");
            return;
        }

        // 에셋 로드
        GameObject prefab = bundle.LoadAsset<GameObject>("Sword_01");
        Instantiate(prefab);

        // 번들 언로드 (에셋은 유지)
        bundle.Unload(false);
    }
}
```

#### 비동기 로딩
```csharp
using System.Collections;

public class AsyncBundleLoader : MonoBehaviour
{
    IEnumerator Start()
    {
        string bundlePath = Path.Combine(Application.streamingAssetsPath, "weapons");

        // 비동기 번들 로드
        AssetBundleCreateRequest bundleRequest = AssetBundle.LoadFromFileAsync(bundlePath);
        yield return bundleRequest;

        AssetBundle bundle = bundleRequest.assetBundle;

        // 비동기 에셋 로드
        AssetBundleRequest assetRequest = bundle.LoadAssetAsync<GameObject>("Sword_01");
        yield return assetRequest;

        GameObject prefab = assetRequest.asset as GameObject;
        Instantiate(prefab);

        bundle.Unload(false);
    }
}
```

### 2. 웹/CDN에서 로드

#### UnityWebRequest 사용
```csharp
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class RemoteBundleLoader : MonoBehaviour
{
    string cdnUrl = "https://your-cdn.com/assetbundles/weapons";

    IEnumerator Start()
    {
        // CDN에서 번들 다운로드
        UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(cdnUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to download: {request.error}");
            yield break;
        }

        // 번들 가져오기
        AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);

        // 에셋 로드
        GameObject prefab = bundle.LoadAsset<GameObject>("Sword_01");
        Instantiate(prefab);

        bundle.Unload(false);
    }
}
```

#### 캐싱 활용
```csharp
public class CachedBundleLoader : MonoBehaviour
{
    IEnumerator LoadBundleWithCache(string url, uint version)
    {
        // 버전 기반 캐싱
        UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(url, version, 0);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);

            // 에셋 사용
            GameObject obj = bundle.LoadAsset<GameObject>("MyAsset");
            Instantiate(obj);

            bundle.Unload(false);
        }
    }

    // 캐시 관리
    void ClearCache()
    {
        Caching.ClearCache();
        Debug.Log("Cache cleared");
    }
}
```

### 3. 다운로드 진행률 표시
```csharp
public class DownloadProgress : MonoBehaviour
{
    public UnityEngine.UI.Slider progressBar;

    IEnumerator DownloadWithProgress(string url)
    {
        UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(url);
        request.SendWebRequest();

        // 진행률 업데이트
        while (!request.isDone)
        {
            progressBar.value = request.downloadProgress;
            Debug.Log($"Download: {request.downloadProgress * 100:F2}%");
            yield return null;
        }

        if (request.result == UnityWebRequest.Result.Success)
        {
            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
            Debug.Log("Download complete!");
        }
    }
}
```

## 의존성 관리

### Manifest 파일 활용

#### 의존성 로딩
```csharp
public class DependencyLoader : MonoBehaviour
{
    private AssetBundleManifest manifest;
    private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();

    IEnumerator Start()
    {
        // Manifest 번들 로드
        string manifestPath = Path.Combine(Application.streamingAssetsPath, "AssetBundles");
        AssetBundle manifestBundle = AssetBundle.LoadFromFile(manifestPath);
        manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

        // 특정 번들과 의존성 로드
        yield return LoadBundleWithDependencies("weapons");

        // 에셋 사용
        GameObject sword = loadedBundles["weapons"].LoadAsset<GameObject>("Sword_01");
        Instantiate(sword);
    }

    IEnumerator LoadBundleWithDependencies(string bundleName)
    {
        // 의존성 먼저 로드
        string[] dependencies = manifest.GetAllDependencies(bundleName);

        foreach (string dependency in dependencies)
        {
            if (!loadedBundles.ContainsKey(dependency))
            {
                string depPath = Path.Combine(Application.streamingAssetsPath, dependency);
                AssetBundle depBundle = AssetBundle.LoadFromFile(depPath);
                loadedBundles.Add(dependency, depBundle);
            }
        }

        // 메인 번들 로드
        if (!loadedBundles.ContainsKey(bundleName))
        {
            string bundlePath = Path.Combine(Application.streamingAssetsPath, bundleName);
            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
            loadedBundles.Add(bundleName, bundle);
        }

        yield return null;
    }

    void OnDestroy()
    {
        // 모든 번들 언로드
        foreach (var bundle in loadedBundles.Values)
        {
            bundle.Unload(true);
        }
        loadedBundles.Clear();
    }
}
```

## 메모리 관리

### Unload 옵션

#### false: 번들만 언로드
```csharp
// 번들을 메모리에서 제거하지만, 로드된 에셋은 유지
bundle.Unload(false);

// 장점: 에셋 계속 사용 가능
// 단점: 다시 로드 불가 (참조만 남음)
```

#### true: 번들과 에셋 모두 언로드
```csharp
// 번들과 로드된 모든 에셋을 메모리에서 제거
bundle.Unload(true);

// 장점: 메모리 완전 해제
// 단점: 사용 중인 에셋도 사라짐 (주의!)
```

### 메모리 관리 패턴
```csharp
public class BundleManager : MonoBehaviour
{
    private Dictionary<string, AssetBundle> bundles = new Dictionary<string, AssetBundle>();
    private Dictionary<string, int> refCounts = new Dictionary<string, int>();

    public AssetBundle GetBundle(string bundleName)
    {
        if (bundles.ContainsKey(bundleName))
        {
            refCounts[bundleName]++;
            return bundles[bundleName];
        }

        // 새로 로드
        string path = Path.Combine(Application.streamingAssetsPath, bundleName);
        AssetBundle bundle = AssetBundle.LoadFromFile(path);

        bundles.Add(bundleName, bundle);
        refCounts.Add(bundleName, 1);

        return bundle;
    }

    public void ReleaseBundle(string bundleName)
    {
        if (!bundles.ContainsKey(bundleName))
            return;

        refCounts[bundleName]--;

        // 참조 카운트가 0이 되면 언로드
        if (refCounts[bundleName] <= 0)
        {
            bundles[bundleName].Unload(false);
            bundles.Remove(bundleName);
            refCounts.Remove(bundleName);
        }
    }
}
```

## 고급 기능

### Variant를 통한 품질 관리
```csharp
public class VariantLoader : MonoBehaviour
{
    void LoadByQuality()
    {
        string variant = QualitySettings.GetQualityLevel() > 2 ? "hd" : "sd";
        string bundleName = $"textures.{variant}";

        AssetBundle bundle = AssetBundle.LoadFromFile(
            Path.Combine(Application.streamingAssetsPath, bundleName)
        );

        Texture2D texture = bundle.LoadAsset<Texture2D>("Sword_Diffuse");
    }
}
```

### 암호화된 번들
```csharp
public class EncryptedBundleLoader : MonoBehaviour
{
    IEnumerator LoadEncrypted(string bundlePath, string password)
    {
        // 파일 읽기
        byte[] data = File.ReadAllBytes(bundlePath);

        // 복호화 (구현 필요)
        byte[] decrypted = DecryptData(data, password);

        // 메모리에서 로드
        AssetBundleCreateRequest request = AssetBundle.LoadFromMemoryAsync(decrypted);
        yield return request;

        AssetBundle bundle = request.assetBundle;
        // 에셋 사용
    }

    byte[] DecryptData(byte[] data, string password)
    {
        // 암호화 알고리즘 구현
        // 예: AES, XOR 등
        return data;
    }
}
```

## 실전 예제: 테이블 기반 로딩

### 번들 매핑 테이블
```csharp
[System.Serializable]
public class AssetBundleConfig
{
    [System.Serializable]
    public class AssetMapping
    {
        public string assetName;
        public string bundleName;
    }

    public List<AssetMapping> mappings;
}

public class TableBasedLoader : MonoBehaviour
{
    public AssetBundleConfig config;
    private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();

    public GameObject LoadAsset(string assetName)
    {
        // 테이블에서 번들 찾기
        var mapping = config.mappings.Find(m => m.assetName == assetName);
        if (mapping == null)
        {
            Debug.LogError($"Asset {assetName} not found in config");
            return null;
        }

        // 번들 로드 (캐시 확인)
        if (!loadedBundles.ContainsKey(mapping.bundleName))
        {
            string path = Path.Combine(Application.streamingAssetsPath, mapping.bundleName);
            AssetBundle bundle = AssetBundle.LoadFromFile(path);
            loadedBundles.Add(mapping.bundleName, bundle);
        }

        // 에셋 로드
        return loadedBundles[mapping.bundleName].LoadAsset<GameObject>(assetName);
    }
}
```

## 베스트 프랙티스

### 1. 번들 크기 관리
```
권장 크기:
- 모바일: 1~5MB
- PC: 5~50MB
- 너무 작으면: HTTP 요청 과다
- 너무 크면: 다운로드 시간 증가
```

### 2. 번들 그룹화 전략
```
Good:
✅ weapons_melee      (검, 도끼, 창 등)
✅ weapons_ranged     (활, 총 등)
✅ enemies_stage1     (스테이지별)
✅ textures_common    (공통 텍스처)

Bad:
❌ all_weapons        (너무 큼)
❌ sword_01           (너무 작음, 파일 개수 과다)
```

### 3. 압축 옵션
```csharp
// LZ4 (권장): 빠른 압축 해제, 약간 큰 크기
BuildAssetBundleOptions.ChunkBasedCompression

// LZMA: 작은 크기, 느린 압축 해제
BuildAssetBundleOptions.None

// 압축 없음: 큰 크기, 가장 빠름
BuildAssetBundleOptions.UncompressedAssetBundle
```

### 4. 버전 관리
```csharp
public class VersionManager
{
    [System.Serializable]
    public class BundleVersion
    {
        public string bundleName;
        public uint version;
        public string hash;
    }

    public List<BundleVersion> versions;

    public uint GetVersion(string bundleName)
    {
        var info = versions.Find(v => v.bundleName == bundleName);
        return info?.version ?? 0;
    }
}
```

## 트러블슈팅

### 문제: 의존성 에셋이 로드되지 않음
**원인**: 의존성 번들을 먼저 로드하지 않음

**해결**: Manifest 파일로 의존성 확인 후 순차 로드

### 문제: 메모리 누수
**원인**: Unload를 호출하지 않음

**해결**: 번들 사용 후 반드시 Unload 호출

### 문제: "AssetBundle cannot be loaded" 에러
**원인**: 플랫폼 불일치 (Windows용 번들을 Android에서 로드)

**해결**: 타겟 플랫폼에 맞게 빌드

## 주의사항

⚠️ **AssetBundle의 한계**
- 수동 의존성 관리 필요
- 복잡한 버전 시스템 직접 구현
- 메모리 관리 까다로움
- 디버깅 어려움

💡 **대안**: 대부분의 경우 **Addressables 사용을 권장**합니다.

## 참고 자료

- [공식 문서](https://docs.unity3d.com/Manual/AssetBundlesIntro.html)
- [API Reference](https://docs.unity3d.com/ScriptReference/AssetBundle.html)
