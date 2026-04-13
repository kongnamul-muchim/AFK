## Context

방치형 (Idle) RPG 게임은 사용자가 적극적으로 조작하지 않아도 자동으로 진행되는 게임입니다. 2D 사이드뷰 시점에서 캐릭터가 탑을 오르며 몬스터와 전투합니다.

**핵심 설계 원칙: Unity 독립성**
- 게임 로직은 100% 순수 JavaScript (Unity API 호출 금지)
- Unity 는 렌더링, 입력, 오디오 출력만 담당
- 나중에 웹/네이티브 이식 시 게임 로직 재사용 가능

**프로토타입 제약사항:**
- 브라우저 기반 (PC/모바일 호환)
- 로컬 스토리지 사용 (서버 연동 제외)
- 상용화 수준의 코드 품질 및 구조
- Unity 래퍼 레이어 분리

## Goals / Non-Goals

**Goals:**
- 2D 사이드뷰 탑 오르기 시각화
- 자동 전투 및 성장 시스템 구현
- 스탯포인트 수동 분배 시스템
- 컬렉션 인벤토리 + 장비 합성 시스템
- 오프라인 시간 계산 및 보상 지급
- 로컬 스토리지를 이용한 세이브/로드 (버전 마이그레이션 포함)
- 직관적인 게임 UI 제공
- **상용화 수준 코드 구조**: 모듈화, 설정 분리, 로깅, 오류 처리
- **Unity 독립 아키텍처**: 게임 로직 100% JavaScript, Unity 는 View 만 담당
- **확장성**: 서버 연동, 과금, 광고, 웹/네이티브 이식 용이

**Non-Goals (프로토타입):**
- 멀티플레이어 기능
- 서버 기반 데이터 동기화
- 과금 시스템
- 광고 연동
- 소셜 기능 (친구, 길드 등)
- 복잡한 애니메이션 (간단한 스프라이트/도형 수준)

## Decisions

### 1. 아키텍처: 3 계층 분리 (Unity 독립)

```
┌─────────────────────────────────────────────────────────────┐
│  Presentation Layer (Unity 또는 Web)                        │
│  - Unity: MonoBehaviour, Input, Audio, Rendering            │
│  - Web: Canvas, DOM, WebAudio                               │
│  - 인터페이스: IRenderer, IInputHandler, IAudioOutput       │
├─────────────────────────────────────────────────────────────┤
│  Game Logic Layer (100% Pure JavaScript) ⭐핵심             │
│  - GameState: 게임 상태 (단일 소스 오브 트루스)             │
│  - Systems: Combat, Progression, Inventory, Stage...        │
│  - Events: EventEmitter 기반 상태 변경 알림                 │
│  - Unity 의존성 없음 - 웹/네이티브 이식 가능                │
├─────────────────────────────────────────────────────────────┤
│  Core Layer                                                 │
│  - EventBus: 이벤트 시스템                                  │
│  - StorageManager: localStorage 래퍼                        │
│  - Config: 게임 설정 (밸런스 값)                            │
│  - Logger: 로깅 시스템                                      │
└─────────────────────────────────────────────────────────────┘
```

**데이터 흐름:**
```
입력 (Unity/Web) 
  → IInputHandler 
  → Game Logic (상태 변경) 
  → EventEmitter 
  → IRenderer (상태 구독, 렌더링)
```

### 2. 기술 스택: Pure JavaScript (ES6+)

**게임 로직 (Unity 독립):**
- Vanilla JavaScript (ES6+): 클래스, 모듈, 이벤트
- HTML5 Canvas: 웹 프로토타입 렌더링
- WebAudio: 웹 프로토타입 오디오

**Unity 래퍼 (교체 가능):**
- C# MonoBehaviour: JavaScript → Unity 브릿지
- Unity Input System: 입력 처리
- Unity Audio: 오디오 출력
- Unity UI: UI 렌더링

**프로토타입은 웹 (Canvas) 으로 시작, Unity 이식 시:**
1. Unity 래퍼 레이어만 작성
2. 게임 로직은 그대로 재사용 (JSC 와 C# 간 브릿지 필요)

### 3. 데이터 저장: localStorage + 버전 마이그레이션

**데이터 구조:**
```javascript
{
  version: 1,  // 스키마 버전
  lastSaveTime: timestamp,
  gameData: {
    player: { level, exp, stats, statPoints, equipment },
    stage: { current, max, kills, autoRepeat },
    inventory: { items: [{id, count, grade}], gold },
    settings: { soundVolume, musicVolume, vibration, notifications },
    tutorial: { completed: boolean, step: number },
    achievements: [{ id, unlockedAt }],
    stats: { playTime, totalKills, maxStage, totalGold }
  }
}
```

**향후 확장:**
- 버전 2: 서버 연동 시 cloudSync 플래그 추가
- 백업/복원: JSON 내보내기/가져오기

### 4. 게임 루프: 고정 시간-step + 이벤트 기반

**구현:**
```
- 업데이트 루프: 고정 100ms (전투, 스폰, 로직)
- 렌더 루프: requestAnimationFrame (60fps, 웹) / Unity Update
- 저장 루프: 5 초마다 자동 저장
- 오프라인 계산: 로그인 시 한 번 계산
```

**이벤트 기반 상태 변경:**
```javascript
// 게임 로직에서
gameState.player.level = 5;
EventEmitter.emit('player:levelup', { level: 5 });

// Unity/Web 렌더러에서
EventEmitter.on('player:levelup', (data) => {
  renderer.ShowLevelUpEffect();
  ui.UpdateLevelText(data.level);
});
```

### 5. 게임 상태 관리: 단일 소스 오브 트루스

**GameState 구조:**
```javascript
class GameState {
  constructor() {
    this.version = 1;
    this.player = {
      level: 1,
      exp: 0,
      stats: { str: 1, agi: 1, int: 1, vit: 1 },
      statPoints: 0,
      equipment: { weapon: null, armor: null, accessory: null }
    };
    this.stage = { current: 1, max: 1, kills: 0, autoRepeat: false };
    this.inventory = {
      items: new Map(),  // itemId -> { count, grade }
      gold: 0
    };
    this.settings = {
      soundVolume: 0.8,
      musicVolume: 0.6,
      vibration: true,
      notifications: true
    };
    this.tutorial = {
      completed: false,
      step: 0
    };
    this.achievements = [];
    this.stats = {
      playTime: 0,
      totalKills: 0,
      maxStage: 1,
      totalGold: 0
    };
    this.lastSaveTime: Date.now()
  }
}
```

### 6. 모듈화 디렉토리 구조

```
src/
├── core/
│   ├── GameState.js       // 단일 게임 상태 객체
│   ├── EventBus.js        // 이벤트 시스템
│   ├── StorageManager.js  // 저장/로드/마이그레이션
│   └── Logger.js          // 로깅
├── systems/               // ⭐ Unity 독립 게임 로직
│   ├── CombatSystem.js    // 전투 로직
│   ├── ProgressionSystem.js // 성장 로직
│   ├── InventorySystem.js // 인벤토리/합성
│   ├── StageSystem.js     // 스테이지/보스
│   ├── OfflineRewards.js  // 오프라인 보상
│   ├── TutorialSystem.js  // 튜토리얼
│   ├── AchievementSystem.js // 업적
│   └── StatsTracker.js    // 통계 기록
├── config/
│   ├── GameConfig.js      // 밸런스 값
│   ├── ItemDatabase.js    // 아이템 데이터
│   └── MonsterDatabase.js // 몬스터 데이터
├── data/                  // ⭐ CSV 데이터 (External)
│   ├── items.csv          // 아이템 데이터
│   ├── monsters.csv       // 몬스터 데이터
│   ├── skills.csv         // 스킬 데이터
│   ├── stages.csv         // 스테이지 데이터
│   ├── achievements.csv   // 업적 데이터
│   ├── tutorial.csv       // 튜토리얼 데이터
│   ├── audio_definitions.csv // 사운드 ID 매핑
│   └── game_config.csv    // 밸런스 설정값
├── data-parser/           // ⭐ CSV 파서
│   ├── CSVParser.js       // CSV 파싱 유틸리티
│   └── DataLoader.js      // 데이터 로드/검증
├── audio/                 // ⭐ Unity 독립 오디오 관리
│   ├── AudioManager.js    // 사운드 관리
│   └── AudioDefinition.js // 사운드 정의 (ID 매핑)
├── adapters/              // ⭐ 플랫폼 어댑터 (교체 가능)
│   ├── IRenderer.js       // 렌더러 인터페이스
│   ├── IInputHandler.js   // 입력 인터페이스
│   ├── IAudioOutput.js    // 오디오 출력 인터페이스
│   ├── WebRenderer.js     // Canvas 렌더러 (웹)
│   └── UnityBridge.js     // Unity 브릿지 (준비)
├── ui/
│   ├── UIManager.js       // UI 관리
│   ├── LoadingScreen.js   // 로딩 화면
│   ├── SettingsUI.js      // 설정 화면
│   └── TutorialUI.js      // 튜토리얼 UI
└── utils/
    ├── MathUtils.js
    ├── StringUtils.js     // 숫자 포맷팅 (1.2K, 1.5M)
    ├── TimeUtils.js
    └── Migrator.js        // 데이터 버전 마이그레이션
```

### 7. 인터페이스 정의 (Unity 독립 핵심)

**IRenderer.js:**
```javascript
// 게임 로직이 의존하는 렌더러 인터페이스
class IRenderer {
  init() {}
  render(gameState, delta) {}  // gameState 만 받아서 렌더링
  showDamage(number, position) {}
  showLevelUp() {}
  destroy() {}
}
// Unity 구현 시: UnityRenderer.cs (C#)
// 웹 구현 시: WebRenderer.js (Canvas)
```

**IInputHandler.js:**
```javascript
// 입력을 게임 이벤트로 변환
class IInputHandler {
  onAttack(callback) {}
  onStatIncrease(statType, callback) {}
  onSynthesize(itemId, callback) {}
  dispose() {}
}
```

**IAudioOutput.js:**
```javascript
// 오디오 출력 인터페이스
class IAudioOutput {
  playSFX(soundId) {}  // soundId 만 전달 (구체적 구현 모름)
  playBGM(trackId) {}
  setVolume(type, value) {}
}
// AudioDefinition.js 에서 ID 매핑:
// { attack: 1, levelup: 2, getItem: 3, bgm_main: 10 }
```

### 8. CSV 데이터 관리 (Unity 독립) ⭐신규

**CSV 파일 구조:**

```csv
# items.csv - 아이템 데이터
id,name,grade,type,rarity,stats_min,stats_max,dropRate
1, rusty_sword,1,weapon,common,"{""str"":1}","{""str"":3}",0.3
2, iron_sword,2,weapon,common,"{""str"":3}","{""str"":5}",0.25
3, magic_sword,3,weapon,rare,"{""str"":5,""int"":2}","{""str"":8,""int"":4}",0.1
```

```csv
# monsters.csv - 몬스터 데이터
id,name,stage,hp_base,hp_scale,atk_base,atk_scale,exp_reward,gold_reward,isBoss
1,slime,1,50,10,5,1,10,5,false
2,goblin,1,70,12,8,1.5,15,8,false
10,boss_goblin,10,500,50,50,10,500,100,true
```

```csv
# game_config.csv - 밸런스 설정
category,key,value,description
player,baseExp,100,레벨 1→2 필요 경험치
player,expMultiplier,1.2,레벨당 경험치 증가율
combat,attackInterval,100,공격 속도 (ms)
inventory,synthesizeCount,5,합성 필요 개수
```

**CSV 파서 아키텍처:**

```javascript
// data-parser/CSVParser.js
class CSVParser {
  static parse(text) {
    // CSV 텍스트 → 객체 배열
    // 주석 (#) 처리, 큰따옴표 이스케이프 지원
  }
  
  static parseFile(url) {
    // fetch 로 CSV 로드 → parse
  }
}

// data-parser/DataLoader.js
class DataLoader {
  constructor() {
    this.cache = new Map();
  }
  
  async loadAll() {
    // 모든 CSV 병렬 로드
    await Promise.all([
      this.load('items'),
      this.load('monsters'),
      this.load('game_config')
    ]);
  }
  
  getItems() { return this.cache.get('items'); }
  getMonsters() { return this.cache.get('monsters'); }
  getConfig(category, key) { ... }
}
```

**데이터 흐름:**
```
1. 게임 시작
   ↓
2. DataLoader.loadAll() - 모든 CSV 로드
   ↓
3. 각 System 이 데이터 참조
   - CombatSystem → monsters.csv
   - InventorySystem → items.csv
   - Config → game_config.csv
   ↓
4. 런타임 중 변경 감지 (optional: hot-reload)
```

**CSV 장점:**
- Excel/Google Sheets 로 편집 가능 (비개발자도 밸런스 조정)
- git diff 로 변경 사항 추적 용이
- merge conflict 해결 쉬움
- 상용화 시 라이브 Ops 툴 연동 가능

### 9. 사운드 시스템 (Unity 독립)

**AudioManager (게임 로직):**
```javascript
class AudioManager {
  constructor(audioOutput, config) {
    this.output = audioOutput;  // IAudioOutput 구현체
    this.config = config;
    this.volumes = { sfx: 0.8, bgm: 0.6 };
  }

  playAttack() {
    this.output.playSFX('attack');  // ID 만 전달
  }

  playLevelUp() {
    this.output.playSFX('levelup');
  }

  setSFXVolume(value) {
    this.volumes.sfx = value;
    this.output.setVolume('sfx', value);
  }
}
```

**AudioDefinition:**
```javascript
export const AudioDefs = {
  SFX: {
    attack: 'sfx_attack_001',
    levelup: 'sfx_levelup_001',
    getItem: 'sfx_getitem_001',
    synthesize: 'sfx_synthesize_001'
  },
  BGM: {
    main: 'bgm_main_001',
    boss: 'bgm_boss_001'
  }
};
```

### 9. 튜토리얼 시스템

**상태 기계:**
```javascript
const TutorialSteps = {
  NOT_STARTED: 0,
  STEP1_FIRST_BLOOD: 1,    // 첫 몬스터 처치
  STEP2_LEVEL_UP: 2,       // 첫 레벨업
  STEP3_STAT_ALLOC: 3,     // 스탯 분배
  STEP4_SYNTHESIZE: 4,     // 첫 합성
  STEP5_BOSS: 5,           // 첫 보스 전투
  COMPLETED: 99
};
```

**튜토리얼 미션:**
```javascript
class TutorialSystem {
  checkCondition(event) {
    switch(this.gameState.tutorial.step) {
      case 1:
        if (event.type === 'monster_killed') this.advance();
        break;
      // ...
    }
  }

  advance() {
    this.gameState.tutorial.step++;
    EventEmitter.emit('tutorial:step', { step: this.gameState.tutorial.step });
  }
}
```

### 10. 설정 관리 (Config System)

**GameConfig:**
```javascript
export const GameConfig = {
  player: {
    baseExp: 100,
    expMultiplier: 1.2,
    statPointsPerLevel: 1,
    baseStats: { str: 5, agi: 5, int: 5, vit: 10 }
  },
  combat: {
    attackInterval: 100,
    monsterScalingMultiplier: 1.1,
    critChance: 0.1,
    critDamage: 1.5
  },
  inventory: {
    synthesizeCount: 5,
    dropRates: {
      common: 0.6,
      rare: 0.3,
      epic: 0.09,
      legendary: 0.01
    }
  },
  offline: {
    maxHours: 24,
    expPerHour: 100,
    goldPerHour: 50
  },
  tutorial: {
    enabled: true,
    skipAllowed: false
  }
};
```

### 11. 로깅 및 디버깅

```javascript
class Logger {
  static debug(msg, ...args) { if (window.DEBUG) console.log(msg, args); }
  static info(msg, ...args) { console.info(msg, args); }
  static warn(msg, ...args) { console.warn(msg, args); }
  static error(msg, ...args) {
    console.error(msg, args);
    this.reportError(msg);  // 추후 Crashlytics
  }

  static reportError(msg) {
    // 에러 수집 준비
  }
}
```

## Risks / Trade-offs

| Risk | Impact | Mitigation |
|------|--------|------------|
| localStorage 데이터 손실 | High | 버전 마이그레이션, JSON 백업/복원 |
| Unity 의존성 | High | 인터페이스 분리, 게임 로직 순수 JS 유지 |
| JavaScript ↔ Unity 브릿지 | Medium | 단순 메시지 passing, 직렬화 최소화 |
| 브라우저 호환성 | Medium | 표준 API 만 사용, Polyfill 준비 |
| 오프라인 시간 조작 | Medium | 최대 보상 제한 (24 시간) |
| 메모리 누수 | Medium | 이벤트 리스너 정리, dispose 패턴 |
| 코드 복잡도 | Medium | 모듈화, 명확한 인터페이스, 문서화 |
| Canvas 성능 (모바일) | Low | 간단한 도형/스프라이트, 객체 수 제한 |
| 밸런스 붕괴 | High | Config 분리, 외부 조정 가능 |

## Migration Plan

**프로토타입 → Unity 이식:**
1. Unity 프로젝트 생성
2. UnityBridge.cs 구현 (JS ↔ C# 브릿지)
3. UnityRenderer.cs, UnityInput.cs, UnityAudio.cs 작성
4. 게임 로직은 그대로 (JSC 에서 실행 또는 C# 변환)

**프로토타입 → 2 단계 (서버 연동):**
1. StorageManager 에 서버 어댑터 추가
2. 데이터 동기화 로직 구현
3. 계정 시스템 연동

**프로토타입 → 3 단계 (과금/광고):**
1. 결제 SDK 연동 (IAP)
2. 광고 SDK 연동 (AdMob)
3. 분석 도구 추가 (Google Analytics)

## Open Questions

1. 최대 스테이지 수는? (일단 1000 으로 설계)
2. 오프라인 보상 최대 시간? (일단 24 시간)
3. 장비 합성 필요 카운트? (일단 5 개)
4. 장비 등급 수? (일단 5 등급: 일반, 희귀, 영웅, 전설, 신화)
5. 스탯 종류? (힘, 민첩, 지력, 체력 - 4 종)
6. 튜토리얼 스킵 허용? (일단 불가, 상용화 시 검토)
7. Unity 이식 시 JavaScript 실행 방식? (JSC 또는 C# 변환)
