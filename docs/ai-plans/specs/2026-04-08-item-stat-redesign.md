# 아이템 및 스탯 시스템 재설계

**날짜:** 2026-04-08  
**상태:** 설계 완료, 구현 대기

---

## 개요

인벤토리 아이템을 4탭 × 5종류 × 5희귀도 = 100개로 확장하고, 스탯 시스템을 % 기반으로 개편한다.

### 핵심 변경사항

1. 아이템 종류 확장 (2→5개/탭)
2. 스탯 시스템 % 기반 변경
3. 이동속도 스탯 추가
4. 밸런스 재조정

---

## 결정 사항

| 항목 | 결정 |
|------|------|
| 아이템 구조 | 4 탭 × 5 종류 × 5 희귀도 = 100개 |
| 아이템 이름 | bronze → iron → steel → gold → mythril |
| 스탯 구조 | 무기: 공%, 갑옷: 방%, 신발: 이동속도+, 장신구: 체% |
| 밸런스 | 수정 B: 재질 기본값 × 희귀도 배수 |
| 구현 방식 | 데이터 + 최소 코드 수정 |
| 추후 작업 | 이동속도 전투 연동 → 전투 시스템 개편 시 |

---

## 아이템 데이터 구조

### items.csv 새 구조

```csv
id,name,grade,type,rarity,stats,dropRate
1,bronze_sword,1,weapon,common,"{""attackBonus"":2}",0.30
2,bronze_sword,2,weapon,rare,"{""attackBonus"":2.3}",0.25
...
5,bronze_sword,5,weapon,mythic,"{""attackBonus"":4}",0.01
...
21,steel_sword,11,weapon,common,"{""attackBonus"":30}",0.30
...
25,mythril_sword,25,weapon,mythic,"{""attackBonus"":300}",0.01
26,bronze_armor,1,armor,common,"{""defenseBonus"":2}",0.30
...
50,mythril_armor,25,armor,mythic,"{""defenseBonus"":300}",0.01
51,bronze_boots,1,boots,common,"{""moveSpeed"":1}",0.30
...
75,mythril_boots,25,boots,mythic,"{""moveSpeed"":150}",0.01
76,bronze_ring,1,accessory,common,"{""hpBonus"":2}",0.30
...
100,mythril_ring,25,accessory,mythic,"{""hpBonus"":300}",0.01
```

### 스탯 필드

| 타입 | stats 필드 | 값 예시 |
|------|------------|---------|
| weapon | attackBonus | 2, 2.3, 2.6, 3, 4 (bronze) |
| armor | defenseBonus | 2, 2.3, 2.6, 3, 4 (bronze) |
| boots | moveSpeed | 1, 1.15, 1.3, 1.5, 2 (bronze) |
| accessory | hpBonus | 2, 2.3, 2.6, 3, 4 (bronze) |

---

## 밸런스 수치

### 재질별 기본값

| 재질 | 기본 % | common | rare | epic | legendary | mythic |
|------|--------|--------|------|------|-----------|--------|
| bronze | 2% | 2% | 2.3% | 2.6% | 3% | 4% |
| iron | 12% | 12% | 13.8% | 15.6% | 18% | 24% |
| steel | 30% | 30% | 34.5% | 39% | 45% | 60% |
| gold | 70% | 70% | 80.5% | 91% | 105% | 140% |
| mythril | 150% | 150% | 172.5% | 195% | 225% | 300% |

### 희귀도 배수

| 희귀도 | 배수 |
|--------|------|
| common | ×1 |
| rare | ×1.15 |
| epic | ×1.3 |
| legendary | ×1.5 |
| mythic | ×2 |

### 이동속도 (고정값)

| 재질 | common | rare | epic | legendary | mythic |
|------|--------|------|------|-----------|--------|
| bronze | 1 | 1.15 | 1.3 | 1.5 | 2 |
| iron | 6 | 6.9 | 7.8 | 9 | 12 |
| steel | 15 | 17.25 | 19.5 | 22.5 | 30 |
| gold | 35 | 40.25 | 45.5 | 52.5 | 70 |
| mythril | 75 | 86.25 | 97.5 | 112.5 | 150 |

### 검증 (조건: 다음 단계 common > 이전 단계 mythic)

- bronze mythic (4%) < iron common (12%) ✓
- iron mythic (24%) < steel common (30%) ✓
- steel mythic (60%) < gold common (70%) ✓
- gold mythic (140%) < mythril common (150%) ✓

---

## GameState 변경사항

### player.derivedStats 확장

```javascript
this.player = {
    derivedStats: {
        attack: 10,           // 기본 공격력
        defense: 5,           // 기본 방어력
        critChance: 0.05,
        critDamage: 1.5,
        maxHp: 100,           // 기본 최대 HP
        moveSpeed: 100        // 기본 이동속도 (추가)
    },
    equipment: {
        weapon: null,         // { itemId, name, attackBonus, rarity }
        armor: null,          // { itemId, name, defenseBonus, rarity }
        boots: null,          // { itemId, name, moveSpeed, rarity }
        accessory: null       // { itemId, name, hpBonus, rarity }
    }
};
```

---

## % 계산 로직

### recalculateDerivedStats() 수정

```javascript
recalculateDerivedStats() {
    // 장비 보너스 합산
    let totalAttackBonus = 0;
    let totalDefenseBonus = 0;
    let totalHpBonus = 0;
    let totalMoveSpeed = 0;
    
    Object.values(this.player.equipment).forEach(item => {
        if (item) {
            if (item.attackBonus) totalAttackBonus += item.attackBonus;
            if (item.defenseBonus) totalDefenseBonus += item.defenseBonus;
            if (item.hpBonus) totalHpBonus += item.hpBonus;
            if (item.moveSpeed) totalMoveSpeed += item.moveSpeed;
        }
    });
    
    // 기본 스탯 → 파생 스탯
    const baseAttack = 10 + this.player.stats.str * 2;
    const baseDefense = 5 + this.player.stats.vit * 0.5;
    const baseMaxHp = 100 + this.player.stats.vit * 10;
    
    // % 보너스 적용
    this.player.derivedStats.attack = Math.floor(baseAttack * (1 + totalAttackBonus / 100));
    this.player.derivedStats.defense = Math.floor(baseDefense * (1 + totalDefenseBonus / 100));
    this.player.derivedStats.maxHp = Math.floor(baseMaxHp * (1 + totalHpBonus / 100));
    this.player.derivedStats.moveSpeed = 100 + totalMoveSpeed;  // 고정값 추가
    
    // 크리티컬 (기존 유지)
    this.player.derivedStats.critChance = 0.05 + this.player.stats.agi * 0.005;
    this.player.derivedStats.critDamage = 1.5;
}
```

---

## 합성 시스템

### 합성 규칙

- 5개 → 다음 등급 1개
- 같은 아이템명 + 다음 희귀도

### 합성 경로

```
bronze_sword common x5 → bronze_sword rare x1
bronze_sword rare x5 → bronze_sword epic x1
bronze_sword epic x5 → bronze_sword legendary x1
bronze_sword legendary x5 → bronze_sword mythic x1
bronze_sword mythic x5 → iron_sword common x1 (베이스 전환)
...
mythril_sword mythic → 최대 등급 (합성 불가)
```

### grade 매핑

| grade | 아이템 위치 |
|-------|------------|
| 1-5 | bronze |
| 6-10 | iron |
| 11-15 | steel |
| 16-20 | gold |
| 21-25 | mythril |

---

## UI 변경사항

### 인벤토리 UI

- 4 탭 유지 (무기/갑옷/신발/장신구)
- 각 탭: 5 종류 × 5 희귀도 = 25개 아이템
- 5행 (종류) × 5열 (희귀도) 레이아웃 유지

### 스탯 가중치 패널

```javascript
// 장비에서 % 보너스 합산 후 표시
bonusAtk = totalAttackBonus (예: +12%)
bonusDef = totalDefenseBonus (예: +8%)
bonusHp = totalHpBonus (예: +15%)
bonusMoveSpeed = totalMoveSpeed (예: +5)
```

### 툴팁

```
bronze_sword
일반 · x5
공격력 +2%

좌클릭: 장착
우클릭: 합성
```

---

## 추후 작업 (전투 시스템 개편 시)

### 이동속도 전투 연동

```javascript
// 공격 속도 = attackInterval × (100 / moveSpeed)
// 예: moveSpeed=200 → 공격 2배 빠름
this.attackInterval = baseInterval * (100 / this.player.derivedStats.moveSpeed);
```

---

## 수정 필요 파일 목록

### 데이터
- `data/items.csv` - 100개 아이템로 재생성

### 코드
- `src/core/GameState.js` - moveSpeed 추가, recalculateDerivedStats() 수정
- `src/systems/InventorySystem.js` - 새 아이템 ID 범위 대응
- `src/ui/InventoryUI.js` - 스탯 표시 변경
- `src/config/GameConfig.js` - grade 범위 수정 (1-25)

---

## 테스트 체크리스트

- [ ] items.csv 100개 아이템 로드 확인
- [ ] 각 탭 5종류 × 5희귀도 표시 확인
- [ ] 장착 시 % 보너스 적용 확인
- [ ] 이동속도 스탯 표시 확인
- [ ] 합성 경로 정상 동작 확인
- [ ] bronze mythic < iron common 조건 확인

---

*작성일: 2026-04-08*