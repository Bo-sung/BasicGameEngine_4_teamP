# Boss_SciFiWarrior 무기 발사 문제 진단 가이드

보스가 총을 한 발만 발사하는 문제를 해결하기 위한 체크리스트입니다.

## 1단계: 프리펩 확인

### Boss_SciFiWarrior.prefab 열기
1. Unity 에디터에서 `Assets/FPS/Prefabs/Enemies/Boss_SciFiWarrior.prefab` 선택
2. 프리펩 모드로 진입 (더블클릭)

### Weapon_AssaultRifle 설정 확인
1. Hierarchy에서 `Boss_SciFiWarrior > WeaponRoot > Weapon_AssaultRifle` 선택
2. Inspector에서 WeaponController 컴포넌트 확인:

**필수 설정 값:**
- ✅ **ShootType**: `Automatic` (값: 1)
- ✅ **DelayBetweenShots**: `0.8`
- ✅ **ProjectilePrefab**: 할당되어 있어야 함
- ✅ **WeaponMuzzle**: GunMuzzle Transform 할당
- ✅ **MaxAmmo**: `100`
- ✅ **AutomaticReload**: ✓ (체크)

## 2단계: 실시간 디버깅

### 씬에서 테스트
1. 테스트 씬에 Boss_SciFiWarrior 배치
2. Play 모드 진입
3. 보스가 플레이어를 감지하고 공격 범위에 들어왔을 때:
   - Hierarchy에서 Boss_SciFiWarrior 선택
   - Inspector에서 실시간으로 다음 확인:

**EnemyMobile (BossMobile) 컴포넌트:**
- `CurrentAiState`: `Attack` (2)인지 확인

**WeaponController 컴포넌트:**
- `Current Ammo`: 탄약이 있는지 확인 (0보다 커야 함)
- `Is Weapon Active`: true인지 확인
- `Last Time Shot`: 값이 계속 업데이트되는지 확인

## 3단계: 일반적인 문제 및 해결책

### 문제 1: ShootType이 Manual로 설정됨
**증상**: 한 발만 발사하고 멈춤  
**해결**: WeaponController의 ShootType을 `Automatic`으로 변경

### 문제 2: DelayBetweenShots가 너무 큼
**증상**: 발사 간격이 너무 길어서 연속 발사처럼 보이지 않음  
**해결**: DelayBetweenShots를 `0.8` 또는 더 작은 값으로 설정

### 문제 3: MaxAmmo가 1로 설정됨
**증상**: 한 발 발사 후 탄약 소진  
**해결**: MaxAmmo를 `100`으로 설정하고 AutomaticReload를 체크

### 문제 4: ProjectilePrefab이 할당되지 않음
**증상**: 발사 시도는 하지만 발사체가 생성되지 않음  
**해결**: ProjectilePrefab 필드에 EyeLazers 프로젝타일 할당
- GUID: `ec753d54f333bb944b8b04884ad63828`

### 문제 5: WeaponMuzzle이 할당되지 않음
**증상**: 발사체 생성 위치 오류  
**해결**: WeaponMuzzle 필드에 GunMuzzle Transform 할당

### 문제 6: AI 상태가 Attack으로 전환되지 않음
**증상**: 보스가 플레이어를 추적만 하고 공격하지 않음  
**원인**: DetectionModule의 AttackRange 설정 문제  
**해결**: 
- DetectionModule > AttackRange: `10`
- DetectionModule > DetectionRange: `20`
- DetectionSourcePoint가 올바르게 할당되었는지 확인

## 4단계: 수동 수정 방법

프리펩을 직접 수정하려면:

1. **Boss_SciFiWarrior.prefab** 열기
2. **Weapon_AssaultRifle** 선택
3. **WeaponController** 컴포넌트에서:
   ```
   ShootType: Automatic
   DelayBetweenShots: 0.8
   ProjectilePrefab: [EyeLazers Projectile]
   MaxAmmo: 100
   AutomaticReload: ✓
   ```
4. **Apply** 클릭하여 변경사항 저장

## 5단계: 코드 레벨 확인 (고급)

만약 위의 모든 설정이 올바른데도 문제가 지속된다면:

### Console 로그 확인
Play 모드에서 Console 창을 열고 에러 메시지 확인

### BossMobile.cs 디버그
`BossMobile.cs`의 `UpdateCurrentAiState()` 함수에 디버그 로그 추가:

```csharp
case AIState.Attack:
    Debug.Log($"Boss attacking! Distance: {Vector3.Distance(m_EnemyController.KnownDetectedTarget.transform.position, transform.position)}");
    // 기존 코드...
    bool didFire = m_EnemyController.TryAtack(m_EnemyController.KnownDetectedTarget.transform.position);
    Debug.Log($"Did fire: {didFire}");
    break;
```

### WeaponController.cs 디버그
`WeaponController.cs`의 `TryShoot()` 함수에 디버그 로그 추가:

```csharp
bool TryShoot()
{
    Debug.Log($"TryShoot - Ammo: {m_CurrentAmmo}, LastShot: {m_LastTimeShot}, Time: {Time.time}, Delay: {DelayBetweenShots}");
    
    if (m_CurrentAmmo >= 1f
        && m_LastTimeShot + DelayBetweenShots < Time.time)
    {
        HandleShoot();
        m_CurrentAmmo -= 1f;
        return true;
    }
    
    return false;
}
```

## 6단계: 프리펩 재생성

모든 설정이 올바른데도 문제가 지속된다면, 프리펩을 재생성하세요:

1. 기존 `Boss_SciFiWarrior.prefab` 삭제
2. Unity 메뉴 > **Tools > Create Boss SciFiWarrior Prefab** 실행
3. 생성된 프리펩의 무기 설정 확인
4. 필요시 WeaponRoot와 GunMuzzle 위치 조정

## 예상 결과

올바르게 설정되면:
- 보스가 공격 범위(10m) 내에 플레이어가 있을 때 Attack 상태로 전환
- 0.8초마다 자동으로 발사체 발사
- 탄약이 자동으로 재장전됨 (AutomaticReload)
- 연속적으로 공격 지속
