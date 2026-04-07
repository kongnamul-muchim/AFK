# Unity 연동 가이드

## 개요

이 문서는 JavaScript 로 작성된 게임 로직을 Unity 와 연동하는 방법을 설명합니다.

**핵심 원칙:**
- 게임 로직은 100% 순수 JavaScript (Unity 의존성 없음)
- Unity 는 렌더링, 입력, 오디오 출력만 담당
- UnityBridge 를 통해 양방향 통신

---

## 아키텍처

```
┌─────────────────────────────────────────────────────────────┐
│                    Unity (C#)                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐ │
│  │ UnityBridge │  │  Renderer   │  │  Input/Audio        │ │
│  │    .cs      │  │  (Canvas)   │  │  (Unity System)     │ │
│  └──────┬──────┘  └─────────────┘  └─────────────────────┘ │
│         │                                                   │
│         ▼                                                   │
│  SendMessage() / EvaluateJS()                               │
└─────────────────────────────────────────────────────────────┘
         ↕ (양방향 통신)
┌─────────────────────────────────────────────────────────────┐
│              JavaScript (WebGL)                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐ │
│  │ UnityBridge │  │  GameState  │  │  Systems            │ │
│  │    .js      │  │  (상태)     │  │  (전투,인벤토리 등) │ │
│  └─────────────┘  └─────────────┘  └─────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

---

## 1. UnityBridge.js (JavaScript 측)

**위치:** `src/adapters/UnityBridge.js`

### 주요 기능

```javascript
class UnityBridge {
    // Unity 에서 호출 가능한 함수 등록
    registerUnityFunctions() {
        window.UnityBridge = {
            init: () => this.handleUnityInit(),
            onStatPointUsed: (statType) => this.handleStatPointUsed(statType),
            onItemSynthesized: (itemId) => this.handleItemSynthesized(itemId),
            pause: () => this.handlePause(),
            resume: () => this.handleResume(),
            save: () => this.handleSave(),
            load: () => this.handleLoad()
        };
    }
    
    // Unity 에 데이터 전송
    sendToUnity(functionName, data) {
        if (this.isUnityConnected) {
            // Unity WebGL: GameObject.SendMessage()
            if (window.GameObject && window.GameObject.SendMessage) {
                window.GameObject.SendMessage(functionName, data);
            }
        }
    }
}
```

### Unity 로부터 받는 메시지

| 함수 | 파라미터 | 설명 |
|------|----------|------|
| `init()` | - | Unity 초기화 완료 |
| `onStatPointUsed(statType)` | 'str', 'agi', 'int', 'vit' | 스탯 포인트 사용 |
| `onItemSynthesized(itemId)` | number | 아이템 합성 요청 |
| `onSettingsChanged(settings)` | JSON string | 설정 변경 |
| `pause()` | - | 게임 일시정지 |
| `resume()` | - | 게임 재개 |
| `save()` | - | 게임 저장 |
| `load()` | - | 게임 로드 |

### Unity 로 보내는 메시지

| 이벤트 | 데이터 | 설명 |
|--------|--------|------|
| `OnGameStateUpdate` | GameState JSON | 게임 전체 상태 |
| `OnPlayerLevelUp` | `{ level }` | 플레이어 레벨업 |
| `OnPlayerHpChanged` | `{ currentHp, maxHp }` | HP 변경 |
| `OnPlayerExpChanged` | `{ exp, maxExp }` | 경험치 변경 |
| `OnGoldChanged` | `{ gold }` | 골드 변경 |
| `OnItemAdded` | `{ itemId, name, rarity }` | 아이템 획득 |
| `OnStageChanged` | `{ stage, isBoss }` | 스테이지 변경 |
| `OnCombatLog` | `{ message }` | 전투 로그 |

---

## 2. UnityBridge.cs (Unity 측)

### 기본 구현

```csharp
using UnityEngine;
using System.Collections.Generic;
using System;

public class UnityBridge : MonoBehaviour
{
    public static UnityBridge Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // JavaScript 호출 (WebGL)
    public void CallJS(string functionName, string data = "")
    {
#if PLATFORM_WEBGL
        ExternalInterface.Call("UnityBridge." + functionName, data);
#else
        Debug.Log($"[JS] {functionName}: {data}");
#endif
    }
    
    // Unity 초기화 완료 알림
    public void NotifyUnityReady()
    {
        CallJS("init");
    }
    
    // ========== JavaScript 로부터 받는 메시지 ==========
    
    // 게임 상태 업데이트
    [ContextMenu("Send Game State")]
    public void OnGameStateUpdate(string data)
    {
        var state = JsonUtility.FromJson<GameStateData>(data);
        Debug.Log($"Game State: Level {state.player.level}");
        
        // UI 업데이트 등
        UIManager.Instance.UpdatePlayerInfo(state.player);
    }
    
    // 플레이어 레벨업
    public void OnPlayerLevelUp(string data)
    {
        var levelData = JsonUtility.FromJson<LevelData>(data);
        Debug.Log($"Level Up! Now {levelData.level}");
        
        // 레벨업 이펙트, 사운드 재생
        AudioManager.Instance.PlaySFX("levelup");
        UIManager.Instance.ShowLevelUpEffect();
    }
    
    // HP 변경
    public void OnPlayerHpChanged(string data)
    {
        var hpData = JsonUtility.FromJson<HpData>(data);
        UIManager.Instance.UpdateHpBar(hpData.currentHp, hpData.maxHp);
    }
    
    // 골드 변경
    public void OnGoldChanged(string data)
    {
        var goldData = JsonUtility.FromJson<GoldData>(data);
        UIManager.Instance.UpdateGold(goldData.gold);
    }
    
    // 아이템 획득
    public void OnItemAdded(string data)
    {
        var itemData = JsonUtility.FromJson<ItemData>(data);
        UIManager.Instance.ShowGetItemPopup(itemData.name, itemData.rarity);
    }
    
    // 스테이지 변경
    public void OnStageChanged(string data)
    {
        var stageData = JsonUtility.FromJson<StageData>(data);
        UIManager.Instance.UpdateStage(stageData.stage);
        
        if (stageData.isBoss)
        {
            AudioManager.Instance.PlaySFX("bossWarning");
        }
    }
    
    // 전투 로그
    public void OnCombatLog(string data)
    {
        var logData = JsonUtility.FromJson<LogData>(data);
        UIManager.Instance.AddCombatLog(logData.message);
    }
    
    // 테스트 연결
    public void OnTestConnection(string data)
    {
        Debug.Log($"[Unity] Connection test received: {data}");
        // 응답 전송
        CallJS("handleUnityMessage", "{\"type\":\"test_response\",\"success\":true}");
    }
    
    // ========== Unity 에서 JavaScript 로 보내기 ==========
    
    // 스탯 포인트 사용
    public void UseStatPoint(string statType)
    {
        CallJS("onStatPointUsed", statType);
    }
    
    // 아이템 합성
    public void SynthesizeItem(int itemId)
    {
        CallJS("onItemSynthesized", itemId.ToString());
    }
    
    // 설정 변경
    public void UpdateSettings(float sfxVolume, float bgmVolume)
    {
        var settings = new SettingsData { soundVolume = sfxVolume, musicVolume = bgmVolume };
        CallJS("onSettingsChanged", JsonUtility.ToJson(settings));
    }
    
    // 게임 저장
    public void SaveGame()
    {
        CallJS("save");
    }
    
    // 게임 로드
    public void LoadGame()
    {
        CallJS("load");
    }
}

// ========== 데이터 클래스 ==========

[Serializable]
public class GameStateData
{
    public PlayerData player;
    public StageData stage;
    public InventoryData inventory;
}

[Serializable]
public class PlayerData
{
    public int level;
    public int exp;
    public int maxExp;
    public int currentHp;
    public int maxHp;
    public StatData stats;
    public int statPoints;
}

[Serializable]
public class StatData
{
    public int str;
    public int agi;
    public int int;
    public int vit;
}

[Serializable]
public class LevelData { public int level; }
[Serializable]
public class HpData { public int currentHp; public int maxHp; }
[Serializable]
public class GoldData { public int gold; }
[Serializable]
public class ItemData { public int itemId; public string name; public string rarity; }
[Serializable]
public class StageData { public int stage; public int max; public int kills; public bool isBoss; }
[Serializable]
public class LogData { public string message; }
[Serializable]
public class SettingsData { public float soundVolume; public float musicVolume; }
```

---

## 3. WebGL 설정

### Build Settings

1. **File → Build Settings**
2. **Platform:** WebGL
3. **Compression Format:** Disabled (디버깅 용이)
4. **Exception Support:** Explicitly Thrown
5. **Enable Exceptions:** None

### WebGL Templates

Unity 의 기본 템플릿 대신 커스텀 템플릿 사용 가능:

```
Assets/
└── WebGLTemplates/
    └── IdleRPG/
        ├── index.html
        ├── TemplateData/
        │   └── style.css
        └── thumbnail.png
```

---

## 4. 웹에서 테스트 (Unity 없이)

### 테스트 페이지

**파일:** `unity-test.html`

이 페이지는 Unity 없이 JavaScript 측 브릿지를 테스트할 수 있습니다.

**실행 방법:**
```bash
# Python 간단 서버
python -m http.server 8000

# 또는 Live Server (VSCode) 사용
```

**URL:** `http://localhost:8000/unity-test.html`

### 테스트 항목

1. **연결 테스트** - "Test Connection" 버튼
2. **게임 상태 전송** - "Send Game State" 버튼
3. **이벤트 시뮬레이션** - Combat/Inventory 테스트 버튼
4. **설정 변경** - 볼륨 슬라이더
5. **로그 모니터링** - JS ↔ Unity 메시지 로그

---

## 5. Unity 연동 단계

### 단계 1: 웹 테스트 (완료)

```
✅ 1. unity-test.html 에서 브릿지 테스트
✅ 2. JavaScript 게임 로직 검증
✅ 3. 이벤트 흐름 확인
```

### 단계 2: Unity 프로젝트 설정

```bash
# Unity 프로젝트 생성
unity -createProject IdleRPG_Unity

# 또는 Unity Hub 에서 새 프로젝트 생성
```

**필요한 파일:**
- `UnityBridge.cs` (Unity 측 브릿지)
- `DataClasses.cs` (JSON 데이터 클래스)
- `UIManager.cs` (UI 관리)
- `AudioManager.cs` (오디오 관리)

### 단계 3: WebGL 빌드

```
File → Build Settings → WebGL → Build
```

생성된 폴더:
```
Build/
├── index.html
├── Build/
│   ├── UnityLoader.js
│   └── [게임명].data/.wasm
└── TemplateData/
```

### 단계 4: 통합 테스트

1. Unity WebGL 빌드를 웹 서버에 배포
2. JavaScript 게임 로직과 통신 테스트
3. 렌더링/입력/오디오 연동 확인

---

## 6. 문제 해결

### Unity 에서 JavaScript 호출 안 됨

**증상:** `ExternalInterface.Call` 이 작동하지 않음

**해결:**
```csharp
// WebGL 플랫폼인지 확인
#if PLATFORM_WEBGL
    ExternalInterface.Call("UnityBridge.init");
#else
    Debug.Log("[WebGL only] ExternalInterface.Call");
#endif
```

### JavaScript 에서 Unity SendMessage 안 됨

**증상:** `GameObject.SendMessage` 이 작동하지 않음

**해결:**
```javascript
// Unity GameObject 이름 확인
window.GameObject = UnityInstance.FindObjectsByName('UnityBridge')[0];

// 또는 고정 이름 사용
const gameObjectName = 'UnityBridge';
window.GameObject.SendMessage('OnGameStateUpdate', data);
```

### CORS 에러

**증상:** Unity WebGL 이 JavaScript 를 호출할 때 CORS 에러

**해결:**
- 로컬 테스트: `--allow-file-access-from-files` 플래그 사용
- 프로덕션: 올바른 CORS 헤더 설정

---

## 7. 성능 최적화

### 메시지 배칭

자주 발생하는 이벤트는 배칭해서 전송:

```javascript
// 나쁜 예: 매 프레임 전송
sendToUnity('OnFrameUpdate', JSON.stringify({ time }));

// 좋은 예: 초당 10 회 제한
if (Date.now() - this.lastSend > 100) {
    sendToUnity('OnFrameUpdate', JSON.stringify({ time }));
    this.lastSend = Date.now();
}
```

### 데이터 압축

대량 데이터는 압축해서 전송:

```javascript
// LZString 등 라이브러리 사용
const compressed = LZString.compress(JSON.stringify(gameState));
sendToUnity('OnGameStateUpdate', compressed);
```

---

## 8. 보안 고려사항

### 입력 검증

JavaScript 로부터 받는 모든 입력은 검증:

```csharp
public void OnStatPointUsed(string data)
{
    // 유효성 검사
    if (string.IsNullOrEmpty(data) || !IsValidStatType(data))
    {
        Debug.LogError("Invalid stat type received");
        return;
    }
    
    // 처리
    UseStatPoint(data);
}
```

### 치트 방지

중요한 로직은 서버에서 검증 (2 단계):

```
[프로토타입] JavaScript 만 사용
[상용화] 서버에서 데미지/보상 검증
```

---

## 체크리스트

### 웹 테스트
- [ ] `unity-test.html` 실행
- [ ] 연결 테스트 성공
- [ ] 게임 상태 전송 확인
- [ ] 이벤트 시뮬레이션 작동
- [ ] 로그 정상 표시

### Unity 연동
- [ ] UnityBridge.cs 구현
- [ ] 데이터 클래스 정의
- [ ] UIManager 연동
- [ ] AudioManager 연동
- [ ] WebGL 빌드 성공
- [ ] 실제 통신 테스트

---

*마지막 업데이트: 2025-04-07*
