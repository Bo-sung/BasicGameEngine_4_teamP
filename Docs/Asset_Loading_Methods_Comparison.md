# Unity 에셋 로딩 방식 비교

## 개요

유니티에서 런타임에 에셋을 로드하는 여러 방법을 비교하고, 각 방식의 장단점과 사용 사례를 설명합니다.

---

## 1. 직접 참조 (Direct Reference)

### 방식
```csharp
public class DirectReference : MonoBehaviour
{
    public GameObject weaponPrefab;  // Inspector에서 할당
    public Sprite iconSprite;

    void Start()
    {
        Instantiate(weaponPrefab);
    }
}
```

### 장점
✅ 가장 간단하고 직관적
✅ 컴파일 타임에 참조 검증
✅ 에디터에서 바로 확인 가능
✅ 별도 설정 불필요

### 단점
❌ 모든 에셋이 빌드에 포함됨 (앱 크기 증가)
❌ 동적 로딩 불가
❌ 런타임 업데이트 불가
❌ 메모리에 항상 상주

### 사용 사례
- 핵심 UI 요소
- 필수 게임 시스템
- 항상 사용되는 에셋
- 프로토타입/테스트

---

## 2. Resources 폴더

### 방식
```
Assets/
└── Resources/
    ├── Weapons/
    │   └── Sword.prefab
    └── UI/
        └── Icon.png
```

```csharp
public class ResourcesLoader : MonoBehaviour
{
    void Start()
    {
        // Resources 폴더 기준 경로
        GameObject weapon = Resources.Load<GameObject>("Weapons/Sword");
        Sprite icon = Resources.Load<Sprite>("UI/Icon");

        Instantiate(weapon);
    }

    void OnDestroy()
    {
        // 메모리 해제
        Resources.UnloadUnusedAssets();
    }
}
```

### 장점
✅ 설정 불필요 (폴더에 넣기만 하면 됨)
✅ 간단한 API
✅ 런타임 로딩 가능
✅ 추가 패키지 불필요

### 단점
❌ 모든 에셋이 빌드에 포함 (앱 크기 증가)
❌ 빌드 시간 증가
❌ 메모리 관리 어려움
❌ 원격 업데이트 불가
❌ 압축 옵션 제한적
❌ 유니티가 deprecated로 권장하지 않음

### 사용 사례
- 레거시 프로젝트 유지보수
- 매우 작은 프로젝트
- 프로토타입

---

## 3. StreamingAssets 폴더

### 방식
```
Assets/
└── StreamingAssets/
    ├── config.json
    ├── video.mp4
    └── assetbundle
```

```csharp
using System.IO;
using UnityEngine;

public class StreamingAssetsLoader : MonoBehaviour
{
    void Start()
    {
        // 플랫폼별 경로
        string path = Path.Combine(Application.streamingAssetsPath, "config.json");

        // 파일 읽기
        if (File.Exists(path))
        {
            string jsonData = File.ReadAllText(path);
            Debug.Log(jsonData);
        }

        // AssetBundle 로드
        string bundlePath = Path.Combine(Application.streamingAssetsPath, "assetbundle");
        AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
    }
}
```

### 장점
✅ 원본 파일 형태로 저장 (압축 없음)
✅ 빌드 후 직접 파일 접근 가능
✅ 텍스트, 비디오 등 모든 파일 형식 지원
✅ 런타임에 파일 조작 가능 (플랫폼 의존적)

### 단점
❌ 유니티 에셋 직접 로드 불가 (AssetBundle 등으로 감싸야 함)
❌ 플랫폼별 경로 처리 필요
❌ 압축 안됨 (용량 큼)
❌ 동적 업데이트 어려움

### 사용 사례
- 설정 파일 (JSON, XML)
- 비디오/오디오 스트리밍
- AssetBundle 파일 저장
- 사용자 데이터

---

## 4. AssetBundle

### 방식
```csharp
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class AssetBundleLoader : MonoBehaviour
{
    IEnumerator Start()
    {
        // 로컬 로드
        string path = Path.Combine(Application.streamingAssetsPath, "weapons");
        AssetBundle bundle = AssetBundle.LoadFromFile(path);

        // 또는 원격 로드
        string url = "https://cdn.example.com/weapons";
        UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(url);
        yield return request.SendWebRequest();
        bundle = DownloadHandlerAssetBundle.GetContent(request);

        // 에셋 사용
        GameObject weapon = bundle.LoadAsset<GameObject>("Sword");
        Instantiate(weapon);

        bundle.Unload(false);
    }
}
```

### 장점
✅ 앱 크기 최소화
✅ CDN/서버에서 다운로드 가능
✅ 플랫폼별 최적화 가능
✅ 압축 옵션 제어
✅ 완전한 로우레벨 제어

### 단점
❌ **수동 의존성 관리** (복잡함)
❌ **수동 버전 관리** 필요
❌ 메모리 관리 까다로움
❌ 에셋-번들 매핑 테이블 필요
❌ 디버깅 어려움
❌ 많은 보일러플레이트 코드

### 사용 사례
- 극도의 최적화가 필요한 경우
- 커스텀 에셋 시스템 구축
- 특수한 로딩 요구사항
- 레거시 시스템

---

## 5. Addressables (권장)

### 방식
```csharp
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;

public class AddressablesLoader : MonoBehaviour
{
    async void Start()
    {
        // 카탈로그 업데이트
        await Addressables.UpdateCatalogs().Task;

        // 다운로드 크기 확인
        long size = await Addressables.GetDownloadSizeAsync("Weapons/Sword").Task;
        if (size > 0)
        {
            Debug.Log($"Need to download: {size / 1024}KB");
            await Addressables.DownloadDependenciesAsync("Weapons/Sword").Task;
        }

        // 로드 (로컬 or 원격 자동 처리)
        GameObject weapon = await Addressables.LoadAssetAsync<GameObject>("Weapons/Sword").Task;
        Instantiate(weapon);

        // 자동 메모리 관리 (Release로 해제)
        // Addressables.Release(handle);
    }
}
```

### 장점
✅ **자동 의존성 관리**
✅ **자동 버전 관리** (Catalog 시스템)
✅ 로컬/원격 자동 전환 (코드 변경 없음)
✅ 레퍼런스 카운팅 (자동 메모리 관리)
✅ GUI 기반 그룹 관리
✅ Label 시스템으로 카테고리 관리
✅ 프로파일링 도구 내장
✅ AssetReference로 타입 안전성

### 단점
❌ Package Manager 설치 필요
❌ 초기 학습 곡선
❌ AssetBundle보다 약간의 오버헤드
❌ 프로젝트 구조 변경 필요

### 사용 사례
- **대부분의 현대적인 유니티 프로젝트**
- CDN 기반 콘텐츠 전달
- 테이블 기반 에셋 로딩
- DLC/패치 시스템
- 모바일 게임 (앱 크기 최적화)

---

## 종합 비교표

| 기능 | 직접 참조 | Resources | StreamingAssets | AssetBundle | Addressables |
|------|-----------|-----------|-----------------|-------------|--------------|
| **설정 난이도** | ⭐ | ⭐ | ⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |
| **코드 난이도** | ⭐ | ⭐ | ⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐ |
| **앱 크기 최적화** | ❌ | ❌ | ❌ | ✅ | ✅ |
| **원격 로딩** | ❌ | ❌ | ❌ | ✅ | ✅ |
| **의존성 관리** | 자동 | 자동 | 수동 | 수동 | 자동 |
| **버전 관리** | N/A | N/A | 수동 | 수동 | 자동 |
| **메모리 관리** | 자동 | 수동 | 수동 | 수동 | 반자동 |
| **빌드 속도** | 빠름 | 느림 | 빠름 | 보통 | 보통 |
| **런타임 업데이트** | ❌ | ❌ | 제한적 | ✅ | ✅ |
| **타입 안전성** | ✅ | 제한적 | ❌ | ❌ | ✅ |

---

## 테이블 기반 로딩 비교

### 시나리오
테이블에 무기 데이터를 저장하고, 에셋 경로를 통해 런타임에 로드

### Resources 방식
```csharp
[System.Serializable]
public class WeaponData
{
    public int weaponId;
    public string resourcePath = "Weapons/Sword_01";  // Resources 폴더 기준
}

public class WeaponLoader : MonoBehaviour
{
    public async Task<GameObject> LoadWeapon(WeaponData data)
    {
        // 동기 로딩만 가능
        GameObject prefab = Resources.Load<GameObject>(data.resourcePath);
        return prefab;
    }
}
```

**문제점:**
- 모든 무기가 빌드에 포함됨
- 업데이트 불가
- 메모리 해제 복잡

### AssetBundle 방식
```csharp
[System.Serializable]
public class WeaponData
{
    public int weaponId;
    public string assetName = "Sword_01";
    public string bundleName = "weapons_melee";  // 추가 정보 필요!
}

public class WeaponLoader : MonoBehaviour
{
    private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();

    public async Task<GameObject> LoadWeapon(WeaponData data)
    {
        // 1. 번들이 로드되어 있는지 확인
        if (!loadedBundles.ContainsKey(data.bundleName))
        {
            // 2. 의존성 확인 및 로드 (Manifest 필요)
            // ...복잡한 의존성 로직...

            // 3. 번들 로드
            string url = $"https://cdn.example.com/{data.bundleName}";
            UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(url);
            await request.SendWebRequest();
            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
            loadedBundles.Add(data.bundleName, bundle);
        }

        // 4. 에셋 로드
        return loadedBundles[data.bundleName].LoadAsset<GameObject>(data.assetName);
    }
}
```

**문제점:**
- 에셋 → 번들 매핑 정보 추가 필요
- 의존성 수동 관리
- 코드 복잡도 높음

### Addressables 방식 (권장)
```csharp
[System.Serializable]
public class WeaponData
{
    public int weaponId;
    public string assetAddress = "Weapons/Melee/Sword_01";  // 주소만 있으면 됨!
}

public class WeaponLoader : MonoBehaviour
{
    public async Task<GameObject> LoadWeapon(WeaponData data)
    {
        // 의존성, 캐싱, 다운로드 모두 자동 처리
        var handle = Addressables.LoadAssetAsync<GameObject>(data.assetAddress);
        return await handle.Task;
    }
}
```

**장점:**
- 테이블에 주소만 저장
- 번들 구조 몰라도 됨
- 의존성 자동 처리
- CDN 자동 처리

---

## CDN 사용 비교

### AssetBundle + CDN
```csharp
public class AssetBundleCDN : MonoBehaviour
{
    // 1. 버전 관리 시스템 구현
    private Dictionary<string, uint> bundleVersions;

    // 2. 캐싱 시스템 구현
    private void ConfigureCache() { /* ... */ }

    // 3. 다운로드 시스템 구현
    IEnumerator DownloadBundle(string bundleName)
    {
        string url = $"https://cdn.example.com/{bundleName}";
        uint version = bundleVersions[bundleName];

        UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(url, version, 0);

        // 4. 진행률 추적 구현
        while (!request.isDone)
        {
            UpdateProgressBar(request.downloadProgress);
            yield return null;
        }

        // 5. 에러 처리 구현
        if (request.result != UnityWebRequest.Result.Success)
        {
            RetryDownload(bundleName);
            yield break;
        }

        AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
    }

    // 6. 재시도 로직 구현
    void RetryDownload(string bundleName) { /* ... */ }

    // 7. 업데이트 확인 시스템 구현
    IEnumerator CheckForUpdates() { /* ... */ }
}
```

**필요한 구현:**
- 버전 관리 시스템
- 캐싱 시스템
- 다운로드 매니저
- 재시도 로직
- 업데이트 체크 시스템
- 진행률 UI

**총 코드량: 500~1000+ 줄**

### Addressables + CDN
```csharp
public class AddressablesCDN : MonoBehaviour
{
    async void Start()
    {
        // 1. 업데이트 확인 (자동)
        var catalogs = await Addressables.CheckForCatalogUpdates().Task;
        if (catalogs.Count > 0)
        {
            await Addressables.UpdateCatalogs().Task;
        }

        // 2. 다운로드 크기 확인 (자동)
        long size = await Addressables.GetDownloadSizeAsync("Weapons/Sword").Task;

        // 3. 다운로드 (캐싱 자동)
        if (size > 0)
        {
            var downloadHandle = Addressables.DownloadDependenciesAsync("Weapons/Sword");

            // 4. 진행률 (내장)
            while (!downloadHandle.IsDone)
            {
                UpdateProgressBar(downloadHandle.PercentComplete);
                await Task.Yield();
            }
        }

        // 5. 로드 (자동 캐시 사용)
        var weapon = await Addressables.LoadAssetAsync<GameObject>("Weapons/Sword").Task;
    }
}
```

**설정만 하면 됨:**
- Addressables Groups → Remote Group 생성
- Build Path: `ServerData/[BuildTarget]`
- Load Path: `https://your-cdn.com/[BuildTarget]`
- 빌드 후 ServerData 폴더를 CDN에 업로드

**총 코드량: 20~50 줄**

---

## 선택 가이드

### 직접 참조 선택
```
✅ 항상 사용되는 핵심 에셋
✅ 프로토타입/테스트
✅ 매우 작은 프로젝트
```

### Resources 선택
```
⚠️ 레거시 프로젝트 유지보수만
❌ 새 프로젝트에는 권장하지 않음
```

### StreamingAssets 선택
```
✅ JSON/XML 설정 파일
✅ 비디오/오디오 스트리밍
✅ 외부 데이터 파일
```

### AssetBundle 선택
```
✅ 극도의 최적화 필요
✅ 레거시 AssetBundle 시스템 존재
✅ 특수한 커스텀 요구사항
❌ 일반적인 경우 Addressables 권장
```

### Addressables 선택 (권장)
```
✅ 새 프로젝트
✅ 테이블 기반 에셋 관리
✅ CDN 콘텐츠 전달
✅ DLC/패치 시스템
✅ 모바일 게임 (앱 크기 최적화)
✅ 복잡한 의존성 구조
✅ 개발 생산성 중요
✅ 대부분의 일반적인 상황
```

---

## 실전 권장사항

### 소규모 프로젝트
```
핵심 에셋: 직접 참조
동적 에셋: Resources (간단히)
설정 파일: StreamingAssets
```

### 중대형 프로젝트
```
핵심 UI: 직접 참조 또는 Addressables (Local)
게임 콘텐츠: Addressables (Remote)
설정 파일: StreamingAssets
```

### 모바일 게임
```
필수 에셋: Addressables (Local, 최소화)
대부분 콘텐츠: Addressables (Remote, CDN)
초기 다운로드 크기 최소화가 핵심!
```

### 라이브 서비스 게임
```
모든 업데이트 가능 콘텐츠: Addressables
실시간 패치/이벤트: Addressables (Catalog 업데이트)
```

---

## 결론

**2024년 이후 유니티 프로젝트:**

1. **핵심 시스템**: 직접 참조
2. **모든 동적 콘텐츠**: Addressables
3. **외부 파일**: StreamingAssets
4. **AssetBundle**: 특수한 경우만

**Addressables는 AssetBundle을 내부적으로 사용하면서 복잡성을 숨겨주므로, 특별한 이유가 없다면 Addressables 사용을 강력 권장합니다.**
