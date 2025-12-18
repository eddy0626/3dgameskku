# 🎮 분대 서바이벌 모드 구현 지침서 (Claude Code용)

## ⚠️ 최우선 원칙 - 반드시 준수

### 🚫 절대 금지 사항
1. **기존 UI 삭제/수정/교체 금지**
2. **기존 스크립트 구조 변경 금지**
3. **1인칭 시점 시스템 변경 금지**
4. **기존 무기/플레이어 시스템 덮어쓰기 금지**

### ✅ 허용 사항
1. **새로운 스크립트 추가** (기존 파일 수정 X)
2. **Canvas에 새로운 UI 요소 추가** (기존 요소 옆에)
3. **새로운 게임 모드 전용 컴포넌트 추가**
4. **가시성 토글로 모드 전환** (삭제가 아닌 SetActive)

---

## 📂 기존 UI 구조 (보존 필수)

```
Canvas
├── HealthBar (Background, Fill, DamageDelay) ⛔ 수정금지
├── StaminaBar (Background, Fill, RecoveryDelay) ⛔ 수정금지
├── GrenadeUI (GrenadeIcon, GrenadeCountText, CookingGaugeContainer) ⛔ 수정금지
├── Crosshair ⛔ 수정금지
├── AmmoUI (AmmoIcon, MagazineText, TotalAmmoText, ReloadingIndicator, FireModeText) ⛔ 수정금지
├── StateText ⛔ 수정금지
├── MinimapPanel (MinimapDisplay, MinimapBorder, PlayerIcon) ⛔ 수정금지
├── MinimapZoomIn ⛔ 수정금지
└── MinimapZoomOut ⛔ 수정금지
```

---

## 🆕 추가할 UI 요소 (Canvas 하위에 새로 생성)

```
Canvas
├── [기존 UI 모두 유지]
│
├── ── 분대 서바이벌 전용 (새로 추가) ──
├── SquadSurvivalUI (빈 오브젝트 - 컨테이너)
│   ├── WavePanel
│   │   ├── WaveText ("WAVE 1")
│   │   └── EnemyCountText ("적: 15/30")
│   │
│   ├── CoinUI
│   │   ├── CoinIcon
│   │   └── CoinText ("1,234")
│   │
│   ├── SquadStatusPanel
│   │   ├── SquadMember1 (아이콘, 체력바)
│   │   ├── SquadMember2
│   │   ├── SquadMember3
│   │   └── SquadMember4
│   │
│   ├── UpgradeButton (우측 하단)
│   │
│   └── GameOverPanel (기본 비활성)
│       ├── ResultText ("VICTORY" / "DEFEAT")
│       ├── StatsPanel (처치수, 획득코인 등)
│       ├── RestartButton
│       └── ExitButton
```

---

## 🎯 게임 모드 전환 시스템

### GameModeManager.cs (새로 생성)

```csharp
public enum GameMode
{
    FPS,            // 기존 1인칭 모드
    SquadSurvival   // 분대 서바이벌 모드
}

public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }
    
    [Header("Mode Settings")]
    public GameMode currentMode = GameMode.FPS;
    
    [Header("FPS Mode UI (기존 UI - 참조만)")]
    public GameObject healthBar;
    public GameObject staminaBar;
    public GameObject ammoUI;
    public GameObject grenadeUI;
    public GameObject crosshair;
    public GameObject minimapPanel;
    
    [Header("Squad Survival UI (새로 추가된 UI)")]
    public GameObject squadSurvivalUI;
    
    public void SwitchToFPSMode()
    {
        currentMode = GameMode.FPS;
        
        // 기존 FPS UI 활성화
        healthBar?.SetActive(true);
        staminaBar?.SetActive(true);
        ammoUI?.SetActive(true);
        grenadeUI?.SetActive(true);
        crosshair?.SetActive(true);
        minimapPanel?.SetActive(true);
        
        // 분대 서바이벌 UI 비활성화
        squadSurvivalUI?.SetActive(false);
    }
    
    public void SwitchToSquadSurvivalMode()
    {
        currentMode = GameMode.SquadSurvival;
        
        // 기존 FPS UI 유지 (1인칭이므로!)
        healthBar?.SetActive(true);      // 플레이어 체력 표시
        staminaBar?.SetActive(true);     // 플레이어 스태미나 표시
        ammoUI?.SetActive(true);         // 탄약 표시
        grenadeUI?.SetActive(true);      // 수류탄 표시
        crosshair?.SetActive(true);      // 조준점 표시
        minimapPanel?.SetActive(true);   // 미니맵 표시
        
        // 분대 서바이벌 UI 추가 활성화
        squadSurvivalUI?.SetActive(true);
    }
}
```

---

## 📁 스크립트 폴더 구조

```
Assets/02.Scripts/
├── [기존 폴더 모두 유지]
│
└── SquadSurvival/          ← 새 폴더 (여기에만 작업)
    ├── Core/
    │   ├── GameModeManager.cs
    │   ├── SquadSurvivalManager.cs
    │   └── WaveManager.cs
    │
    ├── Squad/
    │   ├── SquadMember.cs
    │   ├── SquadController.cs
    │   └── SquadAI.cs
    │
    ├── Economy/
    │   ├── CoinManager.cs
    │   ├── CoinPickup.cs
    │   └── UpgradeSystem.cs
    │
    └── UI/
        ├── SquadSurvivalUIManager.cs
        ├── WaveUI.cs
        ├── CoinUI.cs
        ├── SquadStatusUI.cs
        └── GameOverUI.cs
```

---

## 🎮 1인칭 시점 유지 규칙

### ✅ 유지해야 할 것
- **기존 PlayerController** - 이동, 점프 그대로
- **기존 PlayerLook** - 마우스 시점 그대로
- **기존 WeaponManager** - 무기 시스템 그대로
- **기존 카메라 시스템** - Main Camera 1인칭 그대로

### 🆕 분대 서바이벌에서 추가할 것
- **SquadController** - AI 분대원 제어 (플레이어는 1인칭 유지)
- **SquadMember** - AI 분대원 행동 (플레이어 주변 따라다님)
- **AutoAim 보조** - 1인칭에서 적 자동 조준 보조 (선택적)

### ⚠️ 중요: 플레이어는 1인칭!
```
플레이어: 1인칭 시점 (기존 그대로)
분대원 AI: 플레이어 주변에서 자동 전투
카메라: Main Camera (변경 없음)
```

---

## 🔧 구현 순서

### Phase 1: 기본 구조
1. `Assets/02.Scripts/SquadSurvival/` 폴더 생성
2. `GameModeManager.cs` 생성
3. `SquadSurvivalManager.cs` 생성

### Phase 2: UI 추가
4. Canvas에 `SquadSurvivalUI` 빈 오브젝트 추가
5. 하위에 WavePanel, CoinUI, SquadStatusPanel 추가
6. `SquadSurvivalUIManager.cs` 생성

### Phase 3: 웨이브 시스템
7. `WaveManager.cs` 생성
8. `WaveUI.cs` 생성
9. 적 스폰 로직 구현

### Phase 4: 분대 시스템
10. `SquadMember.cs` 생성
11. `SquadController.cs` 생성
12. AI NavMesh 이동 구현

### Phase 5: 경제 시스템
13. `CoinManager.cs` 생성
14. `CoinPickup.cs` 생성
15. `UpgradeSystem.cs` 생성

---

## 📋 체크리스트 (작업 전 확인)

### 스크립트 생성 전
- [ ] `Assets/02.Scripts/SquadSurvival/` 폴더에 생성하는가?
- [ ] 기존 스크립트를 수정하지 않는가?
- [ ] 새 클래스명이 기존과 충돌하지 않는가?

### UI 추가 전
- [ ] 기존 UI 요소를 삭제하지 않는가?
- [ ] `SquadSurvivalUI` 컨테이너 하위에 추가하는가?
- [ ] SetActive로 가시성만 제어하는가?

### 시스템 연동 전
- [ ] 기존 PlayerController 코드를 수정하지 않는가?
- [ ] 기존 WeaponManager 코드를 수정하지 않는가?
- [ ] 1인칭 카메라 설정을 변경하지 않는가?

---

## 🚨 금지 코드 패턴

### ❌ 하면 안 되는 것
```csharp
// 기존 UI 삭제
Destroy(GameObject.Find("HealthBar"));  // ❌ 절대 금지

// 기존 스크립트 수정
// PlayerController.cs에 코드 추가  // ❌ 절대 금지

// 카메라 변경
Camera.main.transform.rotation = ...;  // ❌ 분대 서바이벌에서 변경 금지
```

### ✅ 해야 하는 것
```csharp
// 새 UI 추가
var newUI = Instantiate(squadUIPrefab, canvas.transform);  // ✅ OK

// 새 스크립트에서 기존 참조
var player = GameObject.FindWithTag("Player");  // ✅ OK
var weaponManager = player.GetComponent<WeaponManager>();  // ✅ OK (참조만)

// 가시성 토글
squadSurvivalUI.SetActive(true);  // ✅ OK
```

---

## 📞 Claude Code 요청 형식

### 올바른 요청 예시
```
"SquadSurvival 폴더에 WaveManager.cs 새로 생성해줘"
"Canvas에 SquadSurvivalUI 빈 오브젝트 추가하고 하위에 WavePanel 만들어줘"
"기존 UI는 그대로 두고 CoinUI만 새로 추가해줘"
```

### 피해야 할 요청 예시
```
"HealthBar를 수정해서..." ❌
"PlayerController.cs에 분대 로직 추가해줘" ❌
"기존 UI 대신 새로운 UI로 교체해줘" ❌
```

---

## 📌 요약

| 항목 | 규칙 |
|------|------|
| 기존 UI | ⛔ 수정/삭제 금지, 참조만 허용 |
| 기존 스크립트 | ⛔ 수정 금지, 참조만 허용 |
| 1인칭 시점 | ✅ 유지 (변경 금지) |
| 새 스크립트 | ✅ SquadSurvival 폴더에만 생성 |
| 새 UI | ✅ SquadSurvivalUI 하위에만 추가 |
| 모드 전환 | ✅ SetActive로 가시성 토글 |

---

*이 지침서는 분대 서바이벌 모드 구현 완료까지 유효합니다.*
*Claude Code 작업 시 항상 이 문서를 참조하세요.*
