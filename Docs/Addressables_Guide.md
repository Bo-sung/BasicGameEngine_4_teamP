# Unity Addressables 가이드

## 개요

Addressables는 유니티의 최신 에셋 관리 시스템으로, 에셋을 문자열 주소로 참조하여 런타임에 로드할 수 있는 강력하고 유연한 방법을 제공합니다.

## 주요 특징

### 1. 문자열 기반 에셋 참조
```csharp
// 문자열 주소로 에셋 로드
Addressables.LoadAssetAsync<GameObject>("Weapons/Sword_01");
```

### 2. 자동 의존성 관리
- 에셋이 참조하는 Material, Texture 등을 자동으로 로드
- 중복 로드 방지 (레퍼런스 카운팅)
- 메모리 관리 자동화

### 3. 로컬/원격 전환 용이
- 개발 중: 로컬 에셋 사용
- 프로덕션: CDN에서 다운로드
- 코드 변경 없이 Profile 전환만으로 가능

### 4. 부분 업데이트 지원
- 변경된 에셋만 선택적으로 다운로드
- Catalog 시스템으로 버전 관리
- 효율적인 패치 배포

## 설치 방법

### Package Manager를 통한 설치
1. Unity 메뉴: `Window → Package Manager`
2. 좌측 상단 `+` 버튼 클릭
3. `Add package by name...` 선택
4. `com.unity.addressables` 입력
5. `Add` 클릭

### 초기 설정
1. `Window → Asset Management → Addressables → Groups`
2. 처음 열면 "Create Addressables Settings" 버튼 클릭
3. 기본 그룹이 자동 생성됨

## 기본 사용법

### 1. 에셋을 Addressable로 등록

#### 방법 A: Inspector에서 설정
1. Project 창에서 에셋 선택
2. Inspector 상단의 `Addressable` 체크박스 활성화
3. Address 이름 확인/수정 (기본값: 파일명)

#### 방법 B: Addressables Groups 창 사용
1. `Window → Asset Management → Addressables → Groups`
2. 에셋을 그룹으로 드래그 앤 드롭
3. Address 및 Label 설정

### 2. 런타임 로딩

#### 기본 로딩
```csharp
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AssetLoader : MonoBehaviour
{
    // 콜백 방식
    void Start()
    {
        Addressables.LoadAssetAsync<GameObject>("Weapons/Sword_01").Completed += OnLoadComplete;
    }

    void OnLoadComplete(AsyncOperationHandle<GameObject> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject prefab = handle.Result;
            Instantiate(prefab);
        }
        else
        {
            Debug.LogError("Failed to load asset");
        }
    }
}
```

#### Async/Await 방식 (권장)
```csharp
using System.Threading.Tasks;

public class ModernAssetLoader : MonoBehaviour
{
    async void Start()
    {
        GameObject prefab = await LoadAsset("Weapons/Sword_01");
        Instantiate(prefab);
    }

    async Task<GameObject> LoadAsset(string address)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(address);
        GameObject result = await handle.Task;
        return result;
    }
}
```

### 3. 메모리 해제
```csharp
public class AssetManager : MonoBehaviour
{
    private AsyncOperationHandle<GameObject> loadHandle;

    async void Start()
    {
        loadHandle = Addressables.LoadAssetAsync<GameObject>("MyAsset");
        GameObject obj = await loadHandle.Task;
        Instantiate(obj);
    }

    void OnDestroy()
    {
        // 핸들 해제
        Addressables.Release(loadHandle);
    }
}
```

## Addressables Groups 구조

### 기본 그룹 구성
```
📁 Addressables Groups
├── 📦 Built In Data (시스템 그룹)
│   └── 씬 에셋 등 내장 데이터
│
├── 📦 Default Local Group (로컬)
│   ├── Build Path: LocalBuildPath
│   ├── Load Path: LocalLoadPath
│   └── 빌드에 포함되는 에셋들
│
└── 📦 Remote Group (원격)
    ├── Build Path: RemoteBuildPath
    ├── Load Path: RemoteLoadPath
    └── CDN에서 다운로드할 에셋들
```

### 그룹 설정 방법
1. Groups 창에서 `Create → Group → Packed Assets`
2. 그룹 이름 변경 (예: "Weapons", "Enemies")
3. 그룹 설정 확인:
   - Build & Load Paths
   - Build Settings (Bundle Mode, Compression 등)

## Label 시스템

### Label 활용
```csharp
// Label로 여러 에셋 동시 로딩
public async Task LoadWeaponsByType(string weaponType)
{
    // "Melee", "Ranged" 등의 Label
    var handle = Addressables.LoadAssetsAsync<GameObject>(weaponType, null);
    var weapons = await handle.Task;

    foreach(var weapon in weapons)
    {
        Debug.Log($"Loaded: {weapon.name}");
    }
}
```

### Label 설정
1. Groups 창에서 에셋 선택
2. Labels 열에서 레이블 추가/선택
3. 새 레이블 생성: `Manage Labels` 버튼

## Profile 시스템

### Profile 개념
개발/테스트/프로덕션 환경별로 다른 경로를 사용하도록 설정

### Profile 설정
1. Groups 창 → `Tools → Profiles`
2. 기본 Profile:
   - `Default`: 로컬 경로 사용
   - 새 Profile 생성 가능

### 변수 설정 예시
```
Profile: Development
- LocalBuildPath: [UnityEngine.AddressableAssets.Addressables.BuildPath]/[BuildTarget]
- LocalLoadPath: {UnityEngine.AddressableAssets.Addressables.RuntimePath}/[BuildTarget]
- RemoteBuildPath: ServerData/[BuildTarget]
- RemoteLoadPath: http://localhost:8000/[BuildTarget]

Profile: Production
- RemoteLoadPath: https://cdn.example.com/[BuildTarget]
```

## CDN 연동

### 1. Remote 그룹 설정
```
Remote Group 설정:
- Build Path: RemoteBuildPath (로컬 빌드 출력 위치)
- Load Path: RemoteLoadPath (실제 CDN URL)
```

### 2. 빌드
1. Groups 창 → `Build → New Build → Default Build Script`
2. 빌드 완료 후 `ServerData/[Platform]` 폴더 생성됨

### 3. CDN 업로드
- `ServerData` 폴더 내용을 CDN에 업로드
- Load Path URL과 일치하도록 구성

### 4. 런타임 사용
```csharp
public class RemoteAssetLoader : MonoBehaviour
{
    async void Start()
    {
        // 카탈로그 업데이트 확인
        var catalogs = await Addressables.CheckForCatalogUpdates().Task;

        if (catalogs.Count > 0)
        {
            // 새 카탈로그 다운로드
            await Addressables.UpdateCatalogs().Task;
            Debug.Log("New content available");
        }

        // 다운로드 크기 확인
        var sizeHandle = Addressables.GetDownloadSizeAsync("Weapons/Sword_01");
        long downloadSize = await sizeHandle.Task;

        if (downloadSize > 0)
        {
            Debug.Log($"Need to download: {downloadSize / 1024}KB");

            // 다운로드 (캐시에 저장됨)
            await Addressables.DownloadDependenciesAsync("Weapons/Sword_01").Task;
        }

        // 로드 (캐시 or CDN)
        var weapon = await Addressables.LoadAssetAsync<GameObject>("Weapons/Sword_01").Task;
        Instantiate(weapon);
    }
}
```

## 고급 기능

### 1. 여러 에셋 일괄 로딩
```csharp
public async Task<List<GameObject>> LoadMultipleAssets(List<string> addresses)
{
    var tasks = addresses.Select(addr =>
        Addressables.LoadAssetAsync<GameObject>(addr).Task
    );

    var results = await Task.WhenAll(tasks);
    return results.ToList();
}
```

### 2. 사전 다운로드
```csharp
public async Task PreloadWeapons(List<string> weaponAddresses)
{
    // 실제 로드 없이 의존성만 다운로드
    var handles = new List<AsyncOperationHandle>();

    foreach (var addr in weaponAddresses)
    {
        var handle = Addressables.DownloadDependenciesAsync(addr);
        handles.Add(handle);
    }

    await Task.WhenAll(handles.Select(h => h.Task));

    Debug.Log("All weapons preloaded to cache");

    // 핸들 해제
    foreach (var handle in handles)
    {
        Addressables.Release(handle);
    }
}
```

### 3. AssetReference 사용
```csharp
using UnityEngine.AddressableAssets;

[Serializable]
public class WeaponData
{
    public string weaponName;
    public AssetReference weaponPrefab;  // Inspector에서 할당
}

public class WeaponManager : MonoBehaviour
{
    public WeaponData weaponData;

    async void Start()
    {
        // AssetReference로 로드
        var handle = weaponData.weaponPrefab.LoadAssetAsync<GameObject>();
        GameObject weapon = await handle.Task;
        Instantiate(weapon);
    }
}
```

## 베스트 프랙티스

### 1. 그룹 구성 전략
```
📦 Local_Core           - 필수 UI, 시스템
📦 Local_Common         - 자주 사용되는 공통 에셋
📦 Remote_Weapons       - 무기 에셋
📦 Remote_Enemies       - 적 에셋
📦 Remote_Stages        - 스테이지별 에셋
```

### 2. Address 네이밍 규칙
```
Good:
- "Weapons/Melee/Sword_Legendary_01"
- "UI/Icons/Weapon_Sword"
- "Audio/SFX/Attack_Sword"

Bad:
- "sword01"  (카테고리 불명확)
- "Sword_Legendary_01_v2_final"  (버전 정보 불필요)
```

### 3. Label 활용 예시
```
Labels:
- 타입: "Weapon", "Enemy", "UI"
- 등급: "Common", "Rare", "Epic", "Legendary"
- 플랫폼: "Mobile", "PC", "Console"
```

### 4. 메모리 관리
```csharp
// ❌ 나쁜 예: 핸들 관리 안함
async void LoadWeapon()
{
    var weapon = await Addressables.LoadAssetAsync<GameObject>("Sword").Task;
    Instantiate(weapon);
    // 핸들 해제 안함 → 메모리 누수
}

// ✅ 좋은 예: 핸들 관리
public class WeaponLoader : MonoBehaviour
{
    private List<AsyncOperationHandle> handles = new List<AsyncOperationHandle>();

    public async Task<GameObject> LoadWeapon(string address)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(address);
        handles.Add(handle);
        return await handle.Task;
    }

    void OnDestroy()
    {
        foreach (var handle in handles)
        {
            Addressables.Release(handle);
        }
        handles.Clear();
    }
}
```

## 트러블슈팅

### 문제: "InvalidKeyException" 발생
**원인**: Address가 존재하지 않거나 빌드되지 않음

**해결**:
1. Addressables Groups에서 해당 에셋 확인
2. Address 철자 확인
3. 빌드 재실행

### 문제: 원격 에셋이 로드되지 않음
**원인**: Load Path URL 잘못됨 또는 CDN 접근 불가

**해결**:
1. Profile의 RemoteLoadPath 확인
2. 웹 브라우저로 URL 직접 접근 테스트
3. CORS 설정 확인 (필요시)

### 문제: 메모리 사용량 과다
**원인**: Release 누락

**해결**:
```csharp
// 모든 로드 핸들을 추적하고 해제
Addressables.Release(handle);
```

## 참고 자료

- [공식 문서](https://docs.unity3d.com/Packages/com.unity.addressables@latest)
- [API Reference](https://docs.unity3d.com/Packages/com.unity.addressables@latest/api/index.html)
- [Addressables-Sample](https://github.com/Unity-Technologies/Addressables-Sample)
