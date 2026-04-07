# CombatSystem - 전투 시스템

## 개요

Idle RPG 의 자동 전투 시스템입니다. 플레이어가 개입하지 않아도 자동으로 몬스터와 전투하며, 데미지 계산, 아이템 드롭, 보상 지급을 처리합니다.

**핵심 특징:**
- 100ms 간격 자동 공격
- 크리티컬 시스템 (확률 × 데미지)
- 몬스터 스폰 (스테이지 기반 난이도)
- 아이템 드롭 (희귀도별 확률)
- Unity 독립적 순수 JavaScript 구현

---

## 아키텍처

```
┌─────────────────────────────────────────────────────────┐
│                    CombatSystem                         │
├─────────────────────────────────────────────────────────┤
│  - attackTimer: setInterval (100ms)                     │
│  - currentMonster: 현재 전투 중인 몬스터 객체          │
│  - isAttacking: 전투 상태 플래그                        │
├─────────────────────────────────────────────────────────┤
│  + startCombat()                                        │
│  + spawnMonster()                                       │
│  + playerAttack()                                       │
│  + killMonster()                                        │
│  + rollItemDrop()                                       │
└─────────────────────────────────────────────────────────┘
                          ↓
        ┌─────────────────┴─────────────────┐
        ↓                   ↓               ↓
   GameState          GameEventBus      GameDataLoader
   (상태 변경)         (이벤트 발행)      (데이터 조회)
```

---

## 데이터 흐름

```
1. startCombat()
   ↓
2. spawnMonster() - 몬스터 데이터 로드 및 스폰
   ↓
3. startAttackLoop() - 100ms setInterval 시작
   ↓
4. playerAttack() - 매 100ms 마다 반복
   ├─ 크리티컬 판정
   ├─ 데미지 계산
   ├─ 몬스터 HP 감소
   └─ CombatDamage 이벤트 발행
   ↓
5. killMonster() - 몬스터 HP ≤ 0
   ├─ 경험치/골드 보상
   ├─ 아이템 드롭
   ├─ StageSystem 에 진행 알림
   └─ 새 몬스터 스폰
```

---

## 주요 클래스

### CombatSystem

**역할:** 자동 전투 로직 관리

**속성:**
| 속성 | 타입 | 설명 |
|------|------|------|
| `gameState` | GameState | 게임 상태 객체 |
| `attackTimer` | number | setInterval ID |
| `isAttacking` | boolean | 전투 중 여부 |
| `currentMonster` | Object | 현재 몬스터 객체 |
| `attackInterval` | number | 공격 속도 (ms, 기본 100) |

**메서드:**
| 메서드 | 설명 |
|--------|------|
| `init()` | 전투 시스템 초기화 (Config 에서 attackInterval 로드) |
| `startCombat()` | 전투 시작, 몬스터 스폰, 공격 루프 시작 |
| `stopCombat()` | 전투 중지, 타이머 정리 |
| `spawnMonster()` | 현재 스테이지에 맞는 몬스터 스폰 |
| `playerAttack()` | 플레이어 공격 실행 (데미지 계산, 크리티컬) |
| `killMonster()` | 몬스터 처치 처리 (보상, 드롭, 진행) |
| `rollItemDrop()` | 아이템 드롭 판정 (희귀도별 확률) |
| `getCurrentMonster()` | 현재 몬스터 객체 반환 |

---

## 데미지 계산 공식

```javascript
// 크리티컬 판정
const isCrit = Math.random() < player.derivedStats.critChance;
const critMultiplier = isCrit ? player.derivedStats.critDamage : 1;

// 데미지 계산
const minDamage = 1; // Config 에서 읽음
let damage = Math.max(minDamage, player.attack - monster.defense);
damage = Math.floor(damage * critMultiplier);
```

**변수:**
- `player.attack`: 플레이어 공격력 (힘 × 2 + 기본 10)
- `monster.defense`: 몬스터 방어력 (고정 5)
- `critChance`: 크리티컬 확률 (기본 5% + 민첩 × 0.5%)
- `critDamage`: 크리티컬 데미지 배수 (기본 150%)

---

## 몬스터 스폰 로직

```javascript
spawnMonster() {
    const stage = gameState.stage.current;
    const isBoss = stage % 10 === 0;
    
    // 보스 스테이지면 보스 몬스터, 아니면 일반 몬스터
    let monsterData = isBoss 
        ? getBossMonster(stage) 
        : getRandomMonster(stage);
    
    // 스테이지 기반 스탯 스케일링
    const scalingMultiplier = 1.1 ^ (stage - 1);
    
    currentMonster = {
        maxHp: monsterData.hp_base * scalingMultiplier,
        attack: monsterData.atk_base * scalingMultiplier,
        expReward: monsterData.exp_reward * scalingMultiplier,
        goldReward: monsterData.gold_reward * scalingMultiplier
    };
}
```

---

## 아이템 드롭 확률

| 희귀도 | 확률 | 색상 |
|--------|------|------|
| 일반 (Common) | 60% | 회색 |
| 희귀 (Rare) | 30% | 파랑 |
| 영웅 (Epic) | 9% | 보라 |
| 전설 (Legendary) | 1% | 주황 |

```javascript
rollItemDrop() {
    const roll = Math.random();
    
    if (roll < 0.01) return 'legendary';
    if (roll < 0.10) return 'epic';
    if (roll < 0.40) return 'rare';
    return 'common';
}
```

---

## 이벤트

### 발행하는 이벤트

| 이벤트 | 데이터 | 설명 |
|--------|--------|------|
| `combat:attack` | `{ attacker, damage, isCrit }` | 공격 발생 |
| `combat:damage` | `{ target, damage, currentHp, maxHp }` | 데미지 입음 |
| `combat:monster_killed` | `{ monsterId, exp, gold }` | 몬스터 처치 |
| `combat:log` | `{ message }` | 전투 로그 메시지 |
| `inventory:item_added` | `{ itemId, name, rarity }` | 아이템 획득 |

### 구독하는 이벤트

| 이벤트 | 처리 |
|--------|------|
| `stage:changed` | 새 스테이지에 맞는 몬스터 스폰 |
| `stage:boss_enter` | 보스 몬스터 스폰 |

---

## 사용 예시

```javascript
// Game 클래스에서
class Game {
    init() {
        // ...다른 초기화
        
        this.combatSystem = new CombatSystem(this.gameState);
        this.combatSystem.init();
    }
    
    update(dt) {
        // 전투 시스템 자동 실행
        if (!this.combatSystem.isAttacking) {
            this.combatSystem.startCombat();
        }
    }
}

// UI 에서 몬스터 정보 표시
gameEventBus.on(GAME_EVENTS.COMBAT_DAMAGE, (data) => {
    ui.updateMonsterHpBar(data.currentHp, data.maxHp);
});

// 전리품 notification
gameEventBus.on(GAME_EVENTS.INVENTORY_ITEM_ADDED, (data) => {
    ui.showGetItemPopup(data.name, data.rarity);
});
```

---

## Config 설정값

`data/game_config.csv` 에서 다음 값들을 조정할 수 있습니다:

| category | key | default | 설명 |
|----------|-----|---------|------|
| `combat` | `attackInterval` | 100 | 공격 속도 (ms) |
| `combat` | `monsterScalingMultiplier` | 1.1 | 몬스터 스탯 증가율 |
| `combat` | `critChance` | 0.05 | 기본 크리티컬 확률 |
| `combat` | `critDamage` | 1.5 | 크리티컬 데미지 배수 |
| `combat` | `minDamage` | 1 | 최소 데미지 |
| `inventory` | `commonDropRate` | 0.6 | 일반 아이템 드롭률 |
| `inventory` | `rareDropRate` | 0.3 | 희귀 아이템 드롭률 |
| `inventory` | `epicDropRate` | 0.09 | 영웅 아이템 드롭률 |
| `inventory` | `legendaryDropRate` | 0.01 | 전설 아이템 드롭률 |

---

## 확장 포인트

### 1. 스킬 시스템

```javascript
// CombatSystem 확장
class CombatSystem {
    useSkill(skillId) {
        const skill = this.getSkill(skillId);
        if (skill.cooldown <= 0 && this.player.mana >= skill.manaCost) {
            this.executeSkill(skill);
            skill.cooldown = skill.maxCooldown;
        }
    }
}
```

### 2. 속성 시스템

```javascript
// 데미지 계산에 속성 상성 추가
calculateDamage(attacker, defender, element) {
    const multiplier = getElementMultiplier(element, defender.element);
    return baseDamage * multiplier;
}
```

### 3. 파티 전투

```javascript
// 여러 캐릭터 동시 전투
class CombatSystem {
    constructor(gameState) {
        this.partyMembers = []; // 파티원 목록
    }
    
    playerAttack() {
        this.partyMembers.forEach(member => {
            this.memberAttack(member);
        });
    }
}
```

---

## 성능 최적화

### 현재 구현
- `setInterval(100ms)` - 초당 10 회 공격 계산
- 몬스터 1 마리만 관리

### 향후 최적화 (고레벨 콘텐츠)
- 객체 풀링 (몬스터/이펙트 재사용)
- 배치 처리 (여러 몬스터 동시 계산)
- Web Worker 오프로딩

---

## 테스트 방법

```javascript
// 단위 테스트 예시
describe('CombatSystem', () => {
    it('should calculate damage correctly', () => {
        const player = { derivedStats: { attack: 50, critChance: 0.1, critDamage: 1.5 } };
        const monster = { defense: 20 };
        
        const damage = Math.max(1, player.attack - monster.defense);
        expect(damage).toBe(30);
    });
    
    it('should drop items according to rates', () => {
        // 10000 회 드롭 시 확률 분포 확인
        const drops = Array(10000).fill(0).map(() => rollItemDrop());
        const legendaryCount = drops.filter(d => d === 'legendary').length;
        
        expect(legendaryCount).toBeGreaterThan(50);  // 0.5% 이상
        expect(legendaryCount).toBeLessThan(150);   // 1.5% 이하
    });
});
```

---

## 의존성

| 시스템 | 의존 목적 |
|--------|-----------|
| `GameState` | 플레이어 스탯, 인벤토리, 스테이지 상태 |
| `StageSystem` | 현재 스테이지 정보, 보스 여부 |
| `InventorySystem` | 아이템 획득 처리 |
| `GameDataLoader` | 몬스터/아이템/설정 데이터 조회 |
| `EventBus` | 이벤트 발행/구독 |

---

*마지막 업데이트: 2025-04-07*
