# Idle RPG 인벤토리 시스템 검토 보고서

**작성일:** 2025-04-07  
**상태:** 🟡 부분적 완료 (아직 문제 존재)

---

## 📋 현재 구현된 기능

### ✅ 완료된 기능

1. **인벤토리 UI**
   - 4 개 탭 (무기/갑옷/신발/장신구)
   - 1 행 5 열 그리드 레이아웃
   - 장착 패널 (상단)
   - 스탯 가중치 표시

2. **도감 시스템**
   - 한 번 획득한 아이템 영구 해제
   - count=0 이어도 활성화 (x0 표시)
   - 미획득 아이템 잠금

3. **합성 시스템**
   - 5 개 → 다음 등급 1 개
   - 베이스 아이템 전환 (rusty→iron→steel)
   - 타입별 최대 등급 (weapon:15, armor/boots/accessory:10)

4. **아이템 데이터**
   - CSV 기반 (items.csv)
   - 45 개 아이템 (15 무기 + 10 갑옷 + 10 신발 + 10 장신구)

---

## 🔍 현재 문제점

### 🚨 문제 1: iron_sword legendary 합성 불가

**증상:**
```
iron_sword grade.9 (legendary) x5 합성 시도
→ iron_sword grade.10 (mythic) 찾기 실패
→ "No next grade item found" 경고
```

**원인 분석:**
```javascript
// CSV 데이터 확인
9,iron_sword,9,weapon,legendary,...
10,iron_sword,10,weapon,mythic,...

// 문제: findNextGradeItem() 이 grade.10 을 못 찾음
// 아마도 item.type 비교 또는 grade 타입 불일치
```

**해결 방안:**
1. findNextGradeItem() 디버깅 로그 추가
2. CSV 의 grade 가 number 인지 string 인지 확인
3. items.find() 조건 재검토

---

### 🚨 문제 2: 갑옷/신발/장신구 합성 불가

**증상:**
```
rusty_armor grade.1 합성 → 실패
leather_boots grade.1 합성 → 실패
copper_ring grade.1 합성 → 실패
```

**원인 분석:**
```javascript
// getMaxGradeByType() 구현됨:
weapon: 15, armor: 10, boots: 10, accessory: 10

// 하지만 아직 합성 로직에서 타입 체크가 제대로 안 될 수 있음
// 또는 findNextGradeItem() 이 armor/boots/accessory 를 못 찾을 수 있음
```

**해결 방안:**
1. findNextGradeItem() 에서 type 비교 로그 추가
2. CSV 에서 armor/boots/accessory 데이터 확인
3. 실제로 합성 테스트 해보기

---

### 🚨 문제 3: discoveredItems 상태 저장 안 됨

**증상:**
```
게임 재시작 → discoveredItems 초기화
→ 모든 아이템 다시 잠김
```

**원인 분석:**
```javascript
// GameState.constructor() 에서:
this.inventory = {
    discoveredItems: new Set()  // 새 Set 매번 생성
};

// 문제: Set 이 localStorage 에 저장되지 않음
// toJSON() 에서 discoveredItems 직렬화 안 함
```

**해결 방안:**
```javascript
// GameState.toJSON() 에 추가:
discoveredItems: Array.from(this.inventory.discoveredItems)

// GameState.fromJSON() 에 추가:
this.inventory.discoveredItems = new Set(data.discoveredItems || [])
```

---

## 🔧 수정 필요 코드

### 1. InventorySystem.js - findNextGradeItem() 디버깅

```javascript
findNextGradeItem(currentName, type, nextGrade) {
    const items = gameDataLoader.get('items');
    
    console.log(`[DEBUG] Searching for: type=${type}, grade=${nextGrade}`);
    console.log(`[DEBUG] All ${type} items:`, 
        items.filter(i => i.type === type).map(i => `${i.name}(g.${i.grade})`));
    
    // 1. 같은 이름 + 다음 등급 찾기
    let nextItem = items.find(i => {
        const match = i.name === currentName && i.grade === nextGrade;
        if (match) console.log(`[DEBUG] Found same name: ${i.name} g.${i.grade}`);
        return match;
    });
    
    if (nextItem) return nextItem;
    
    // 2. 같은 타입 + 다음 등급 찾기 (베이스 아이템 전환)
    nextItem = items.find(i => {
        const match = i.type === type && i.grade === nextGrade;
        if (match) console.log(`[DEBUG] Found type match: ${i.name} g.${i.grade}`);
        return match;
    });
    
    if (!nextItem) {
        console.warn(`[DEBUG] No item found for type=${type}, grade=${nextGrade}`);
    }
    
    return nextItem;
}
```

---

### 2. GameState.js - discoveredItems 직렬화

```javascript
// toJSON() 에 추가:
toJSON() {
    return {
        // ...기존 필드...
        discoveredItems: Array.from(this.inventory.discoveredItems)
    };
}

// fromJSON() 에 추가:
fromJSON(data) {
    // ...기존 코드...
    if (data.discoveredItems) {
        this.inventory.discoveredItems = new Set(data.discoveredItems);
    }
}
```

---

### 3. StorageManager.js - discoveredItems 저장

```javascript
// 이미 toJSON/fromJSON 을 사용하므로 자동 저장됨
// 하지만 확인 필요:
save(gameState) {
    const data = {
        version: CURRENT_VERSION,
        timestamp: Date.now(),
        gameData: gameState.toJSON()  // discoveredItems 포함
    };
    // ...
}
```

---

## 📊 CSV 데이터 확인

### weapons (15 개)
```
grade 1-5:   rusty_sword (common→mythic)
grade 6-10:  iron_sword (common→mythic)
grade 11-15: steel_sword (common→mythic)
```

### armor (10 개)
```
grade 1-5:  rusty_armor (common→mythic)
grade 6-10: iron_armor (common→mythic)
```

### boots (10 개)
```
grade 1-5:  leather_boots (common→mythic)
grade 6-10: iron_boots (common→mythic)
```

### accessory (10 개)
```
grade 1-5:  copper_ring (common→mythic)
grade 6-10: silver_ring (common→mythic)
```

---

## 🎯 다음 단계

### 우선순위 1: discoveredItems 저장 구현
- GameState.toJSON()/fromJSON() 수정
- StorageManager 테스트
- 게임 재시작 후에도 발견 상태 유지 확인

### 우선순위 2: findNextGradeItem() 디버깅
- 콘솔 로그 추가
- iron_sword grade.9→10 합성 테스트
- armor/boots/accessory 합성 테스트

### 우선순위 3: 자동화 테스트
- 시뮬레이션 스크립트 개선
- 모든 아이템 합성 경로 테스트
- 에러 자동 감지

---

## 📝 테스트 체크리스트

- [ ] rusty_sword grade.1 → 15 (전체 경로)
- [ ] iron_sword grade.9 → 10 (legendary → mythic)
- [ ] steel_sword grade.15 (최대 등급 확인)
- [ ] rusty_armor grade.1 → 10
- [ ] leather_boots grade.1 → 10
- [ ] copper_ring grade.1 → 10
- [ ] discoveredItems 게임 재시작 후 유지
- [ ] count=0 아이템도 활성화 표시
- [ ] 합성 후 UI 즉시 갱신

---

## 🔗 관련 파일

- `src/systems/InventorySystem.js` - 합성 로직
- `src/core/GameState.js` - 상태 관리 (discoveredItems)
- `src/ui/InventoryUI.js` - UI 렌더링
- `src/core/StorageManager.js` - 저장/로드
- `data/items.csv` - 아이템 데이터
- `simulation-standalone.js` - 합성 시뮬레이션

---

*마지막 업데이트: 2025-04-07*
