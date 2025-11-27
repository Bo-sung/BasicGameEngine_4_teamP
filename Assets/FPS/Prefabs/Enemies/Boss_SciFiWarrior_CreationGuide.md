# Boss_SciFiWarrior 프리펩 생성 가이드

이 가이드는 Enemy_HoverBot을 기반으로 BossMobile.cs를 사용하고 HPCharacter 모델을 통합한 Boss_SciFiWarrior 프리펩을 Unity 에디터에서 생성하는 방법을 설명합니다.

## 1단계: Enemy_HoverBot 복제

1. Unity 에디터에서 `Assets/FPS/Prefabs/Enemies/Enemy_HoverBot.prefab` 선택
2. Ctrl+D로 복제
3. 이름을 `Boss_SciFiWarrior`로 변경

## 2단계: 기본 구조 수정

### 루트 GameObject 설정
1. Boss_SciFiWarrior 프리펩을 더블클릭하여 Prefab 모드로 진입
2. 루트 GameObject 선택
3. Inspector에서 다음 컴포넌트 수정:

#### EnemyMobile 스크립트를 BossMobile로 교체
1. EnemyMobile 컴포넌트 제거 (우클릭 > Remove Component)
2. Add Component > Scripts > BossMobile 추가
3. BossMobile 설정:
   - **IsBoss**: ✓ (체크)
   - **BossHealthMultiplier**: 5.0
   - **MinionSpawnHealthThreshold**: 0.5
   - **MinionsPerSpawn**: 3
   - **MinionSpawnInterval**: 10.0
   - **PathReachingRadius**: 2
   - **OrientationSpeed**: 10
   - **SelfDestructYHeight**: -20

## 3단계: HPCharacter 모델 통합

### 기존 모델 제거
1. Hierarchy에서 `Prefab_MikeZ_Accessories` 자식 오브젝트 삭제

### HPCharacter 모델 추가
1. Project 창에서 `Assets/SciFiWarriorPBRHPPolyart/Prefabs/HPCharacter.prefab` 찾기
2. HPCharacter 프리펩을 Boss_SciFiWarrior의 자식으로 드래그
3. HPCharacter의 Transform 설정:
   - Position: (0, 0, 0)
   - Rotation: (0, 0, 0)
   - Scale: (1, 1, 1)

### Animator 설정
1. HPCharacter 내부의 Animator 컴포넌트 찾기
2. Boss_SciFiWarrior 루트의 BossMobile 컴포넌트에서:
   - **BossAnimator** 필드에 HPCharacter의 Animator 드래그하여 할당
3. DetectionModule 컴포넌트에서:
   - **Animator** 필드에 동일한 Animator 할당

## 4단계: 무기 설정 (AssaultRifle with EyeLazers Stats)

### WeaponRoot 위치 조정
1. `WeaponRoot` GameObject 선택
2. Transform 위치를 HPCharacter의 손 위치에 맞게 조정:
   - Position: (0, 1.5, 0.3) (대략적인 값, 필요시 조정)
   - Rotation: (0, 180, 0)

### Weapon 스크립트 설정 (EyeLazers 스펙 적용)
1. `Weapon_EyeLazers` GameObject 이름을 `Weapon_AssaultRifle`로 변경
2. WeaponController 컴포넌트 설정:
   - **WeaponName**: "Boss Assault Rifle"
   - **ShootType**: Automatic (1)
   - **ProjectilePrefab**: 기존 EyeLazers 프로젝타일 유지
   - **DelayBetweenShots**: 0.8
   - **BulletSpreadAngle**: 0
   - **BulletsPerShot**: 1
   - **RecoilForce**: 1
   - **ClipSize**: 30
   - **AutomaticReload**: ✓
   - **MaxAmmo**: 100

### GunMuzzle 위치 조정
1. `GunMuzzle` GameObject 선택
2. HPCharacter의 AssaultRifle 총구 위치에 맞게 Transform 조정
3. HPCharacter 내부의 Trigger_Right 본을 참고하여 위치 설정

## 5단계: DetectionModule 설정

1. `DetectionModule` GameObject 선택
2. DetectionModule 컴포넌트 설정:
   - **DetectionSourcePoint**: WeaponRoot Transform 할당
   - **DetectionRange**: 20
   - **AttackRange**: 10
   - **KnownTargetTimeout**: 12
   - **Animator**: HPCharacter의 Animator 할당 (3단계에서 설정)

## 6단계: HitBox 조정

1. `HitBox` GameObject 선택
2. Transform 위치를 HPCharacter의 중심부에 맞게 조정:
   - Position: (0, 1.5, 0) (캐릭터 가슴 높이)
3. SphereCollider 설정:
   - **Radius**: 0.6 (HPCharacter 크기에 맞게)

## 7단계: HealthBar 위치 조정

1. `HealthBarPivot` GameObject 선택
2. Transform 위치를 HPCharacter의 머리 위로 조정:
   - Position: (0, 2.2, 0) (캐릭터 머리 위)

## 8단계: NavMeshAgent 설정

1. 루트 GameObject의 NavMeshAgent 컴포넌트 설정:
   - **Radius**: 0.5
   - **Speed**: 3.5
   - **Acceleration**: 8
   - **Height**: 2.0 (HPCharacter 높이에 맞게)
   - **Base Offset**: 0

## 9단계: Health 설정

1. Health 컴포넌트 설정:
   - **MaxHealth**: 100 (BossMobile이 자동으로 5배 증가시킴 = 500)
   - **CriticalHealthRatio**: 0.3

## 10단계: 추가 설정

### Actor 컴포넌트
- **Affiliation**: Enemy (0)
- **AimPoint**: 필요시 HPCharacter의 Head 본 할당

### ShadowProjector
- 위치를 HPCharacter 발 아래로 조정:
  - Position: (0, 0.1, 0)

## 11단계: 테스트

1. Prefab 모드에서 나가기 (상단의 < 버튼 클릭)
2. 테스트 씬을 열기
3. Boss_SciFiWarrior 프리펩을 씬에 배치
4. Play 모드로 진입하여 다음 확인:
   - ✓ 보스가 올바르게 생성되는지
   - ✓ HPCharacter 모델이 표시되는지
   - ✓ 애니메이션이 재생되는지
   - ✓ 플레이어를 감지하고 추적하는지
   - ✓ AssaultRifle로 공격하는지
   - ✓ 체력이 50% 이하일 때 쫄몹 생성되는지
   - ✓ 사망 시 폭발 효과가 발생하는지

## 문제 해결

### 모델이 보이지 않는 경우
- HPCharacter의 SkinnedMeshRenderer가 활성화되어 있는지 확인
- Material이 올바르게 할당되어 있는지 확인

### 애니메이션이 재생되지 않는 경우
- BossMobile의 BossAnimator 필드가 올바르게 할당되었는지 확인
- Animator Controller가 SciFiWarrior.controller로 설정되어 있는지 확인

### 무기가 이상한 위치에 있는 경우
- WeaponRoot와 GunMuzzle의 Transform을 HPCharacter의 손과 총구 위치에 맞게 조정
- Scene 뷰에서 직접 위치를 조정하면서 확인

### NavMesh에서 이동하지 않는 경우
- NavMeshAgent의 Height와 Radius가 HPCharacter 크기에 맞게 설정되었는지 확인
- 씬에 NavMesh가 Bake되어 있는지 확인
