# 몬스터 처치 장비획득 시스템 대개편 (v2)

**날짜**: 2026-04-09  
**커밋**: `6f4ca4f`

---

## 📋 개요

기존 몬스터 처치 시 장비 아이템 드롭 시스템의 근본적인 문제점을 해결하기 위해 대대적인 리팩토링을 수행했습니다.

### 🔍 기존 문제점

1. **Grade 매칭 불일치**
   - `rollItemDrop()`에서 `targetGrade = Math.ceil(stage / 10)` 계산
   - Stage 11-20 → grade 2 아이템 탐색
   - 그러나 items.csv에서 grade 2는 `bronze_sword(rare)`만 존재
   - Iron 아이템은 grade 6부터 시작 → **매칭 실패**

2. **희귀도 제한**
   - mythic 희귀도가 items.csv에 존재하지만 드롭 로직에서 제외됨

3. **Stage 50+ 한계**
   - Stage 51 이상에서 targetGrade=6 → 매칭 아이템 없음

---

## 🔧 변경 사항

### 1. items.csv ID 체계 리팩토링

**이전**:
```csv
id,name,grade,type,rarity,stats
1,bronze_sword,1,weapon,common,"{""attackBonus"":2}"
2,bronze_sword,2,weapon,rare,"{""attackBonus"":4}"
...
6,iron_sword,6,weapon,common,"{""attackBonus"":14}"
```

**이후** (Type별 ID 범위 할당):
```csv
id,name,grade,type,rarity,stats
1001,bronze_sword,1,weapon,common,"{""attackBonus"":2}"
1002,bronze_sword,2,weapon,rare,"{""attackBonus"":4}"
...
1006,iron_sword,6,weapon,common,"{""attackBonus"":14}"
...
2001,bronze_armor,1,armor,common,"{""defenseBonus"":1}"
...
3001,bronze_boots,1,boots,common,"{""moveSpeed"":2}"
...
4001,bronze_ring,1,accessory,common,"{""hpBonus"":3}"
```

| 타입 | ID 범위 | 아이템 수 |
|------|---------|----------|
| Weapon | 1001-1025 | 25개 (grade 1-25) |
| Armor | 2001-2025 | 25개 (grade 1-25) |
| Boots | 3001-3025 | 25개 (grade 1-25) |
| Accessory | 4001-4025 | 25개 (grade 1-25) |

**총 아이템 수**: 100개 (변경 없음)

---

### 2. 드롭 로직 대개편 (`CombatSystem.rollItemDrop()`)

#### Grade 범위 드롭 시스템

| 스테이지 | 드롭 Grade 범위 | 포함 티어 |
|---------|----------------|----------|
| 1-10 | 1, 2, 3, 4, 5 | Bronze (common~mythic) |
| 11-20 | 2, 3, 4, 5, 6 | Bronze (rare~mythic) + Iron (common) |
| 21-30 | 3, 4, 5, 6, 7 | Bronze (epic~mythic) + Iron (common~rare) + Steel (common) |
| 31-40 | 4, 5, 6, 7, 8 | ... |
| 41-50 | 5, 6, 7, 8, 9 | ... |
| 51-60 | 6, 7, 8, 9, 10 | Iron (common~mythic) |
| 61-70 | 7, 8, 9, 10, 11 | ... |
| 91+ | 21, 22, 23, 24, 25 | Mythril (common~mythic, 최대 티어 고정) |

#### 희귀도 확률 (선형 감소)

| 상대적 위치 | 희귀도 | 확률 |
|------------|--------|------|
| 가장 낮음 | common | 70% |
| 그 다음 | rare | 20% |
| 중간 | epic | 7% |
| 그 다음 | legendary | 2.5% |
| 가장 높음 | mythic | 0.5% |
| **합계** | | **100%** |

예시 (Stage 1-10):
- Grade 1 + common → 70%
- Grade 2 + rare → 20%
- Grade 3 + epic → 7%
- Grade 4 + legendary → 2.5%
- Grade 5 + mythic → 0.5%

#### 타입 분포

- weapon: 25%
- armor: 25%
- boots: 25%
- accessory: 25%

---

### 3. 코드 변경 사항

#### CombatSystem.js

```javascript
// 추가된 함수: 가중치 랜덤 인덱스 선택
weightedRandomIndex(probabilities) {
    const roll = Math.random();
    let cumulative = 0;
    for (let i = 0; i < probabilities.length; i++) {
        cumulative += probabilities[i];
        if (roll < cumulative) {
            return i;
        }
    }
    return probabilities.length - 1;
}

// 수정된 rollItemDrop()
rollItemDrop() {
    const stage = this.gameState.stage.current;
    
    // Grade 범위 계산
    let baseGrade;
    if (stage >= 91) {
        baseGrade = 21; // Mythril 고정
    } else {
        baseGrade = Math.ceil(stage / 10);
    }
    
    const dropGrades = [baseGrade, baseGrade + 1, baseGrade + 2, baseGrade + 3, baseGrade + 4];
    const rarityMap = ['common', 'rare', 'epic', 'legendary', 'mythic'];
    const gradeProbabilities = [0.70, 0.20, 0.07, 0.025, 0.005];
    
    const selectedGradeIndex = this.weightedRandomIndex(gradeProbabilities);
    const selectedGrade = dropGrades[selectedGradeIndex];
    const selectedRarity = rarityMap[selectedGradeIndex];
    
    const types = ['weapon', 'armor', 'boots', 'accessory'];
    const selectedType = types[Math.floor(Math.random() * types.length)];
    
    // items.csv에서 grade + type 매칭
    const items = gameDataLoader.filter('items', item => 
        item.grade === selectedGrade && item.type === selectedType
    );
    
    // ... 아이템 추가 로직
}
```

#### GameState.js

```javascript
// 추가된 함수: 세이브 데이터 하드리셋
hardResetInventory() {
    gameLogger.info('Hard resetting inventory due to ID system refactoring');
    
    // 인벤토리 아이템 완전 초기화
    this.inventory.items = new Map();
    
    // 발견된 아이템도 초기화 (도감 리셋)
    this.inventory.discoveredItems = new Set();
    
    // 장비 해제
    this.player.equipment = {
        weapon: null,
        armor: null,
        accessory: null,
        boots: null
    };
    
    // 파생 스탯 재계산
    this.recalculateDerivedStats();
    
    gameLogger.info('Inventory hard reset completed');
}
```

#### main.js

```javascript
// 세이브 데이터 로드 직후 하드리셋 호출
if (savedData) {
    this.gameState = new GameState();
    this.gameState.fromJSON(savedData);
    
    // items.csv ID 체계 리팩토링(v2)으로 인한 세이브 데이터 하드리셋
    gameLogger.info('Performing hard reset of inventory due to ID system refactoring (v2)');
    this.gameState.hardResetInventory();
    
    // ...
}
```

---

### 4. 세이브 데이터 처리

기존 세이브 데이터의 아이템 ID(1-100)가 새 체계(1000-4025)와 호환되지 않으므로:

- **기존 세이브 로드 시**: 인벤토리 완전 초기화 (하드리셋)
- **신규 게임**: 정상 시작
- **환생 시**: 인벤토리 초기화 유지 (기존 동작)

---

## ✅ 테스트 체크리스트

- [ ] Stage 1에서 bronze_sword(common) 드롭 확인
- [ ] Stage 5에서 bronze_sword(mythic) 드롭 확인 (0.5% 확률)
- [ ] Stage 15에서 iron_sword(common) 드롭 확인
- [ ] Stage 50에서 gold_sword(common) 드롭 확인
- [ ] Stage 91에서 mythril_sword(common) 드롭 확인
- [ ] Stage 100에서 mythril_sword(mythic) 드롭 확인
- [ ] 4가지 타입(weapon/armor/boots/accessory) 균등 드롭 확인
- [ ] 합성 시스템 정상 동작 확인 (grade+1 → 다음 ID)
- [ ] 기존 세이브 로드 시 인벤토리 초기화 확인
- [ ] 인벤토리 UI 정상 표시 확인

---

## 🔄 영향 받는 파일

| 파일 | 변경 내용 |
|------|----------|
| `data/items.csv` | ID 체계 재구성 (100개 아이템) |
| `src/systems/CombatSystem.js` | rollItemDrop() 대개편, weightedRandomIndex() 추가 |
| `src/core/GameState.js` | hardResetInventory() 추가 |
| `src/main.js` | 세이브 로드 시 하드리셋 호출 |

---

## 📝 비고

- InventorySystem과 InventoryUI는 ID 체계 변경에 영향을 받지 않음 (id 기반 조회)
- 합성 시스템은 grade+1 로직으로 다음 ID를 자동으로 찾음 (변경 없음)
- mythic 희귀도가 처음으로 드롭 테이블에 추가됨
