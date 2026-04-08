# Item System Redesign - Design Spec

**Date:** 2026-04-08
**Status:** Approved for Implementation

---

## Overview

아이템 시스템 전면 개편: 4 탭 × 5 종류 × 5 희귀도 = 100개 아이템, % 기반 스탯 시스템 도입.

---

## Decisions Summary

| 항목 | 결정 |
|------|------|
| 아이템 구조 | 4 탭 × 5 종류 × 5 희귀도 = 100개 |
| 아이템 이름 | bronze → iron → steel → gold → mythril |
| 스탯 구조 | 무기: 공%, 갑옷: 방%, 신발: 이동속도+, 장신구: 체% |
| 밸런스 | 재질 기본값 × 희귀도 배수 |
| 구현 방식 | 데이터 + 최소 코드 수정 |

---

## Item Data Structure

### Item Names per Tab

| 탭 | 아이템 종류 (5개) |
|---|---|
| 무기 (weapon) | bronze_sword, iron_sword, steel_sword, gold_sword, mythril_sword |
| 갑옷 (armor) | bronze_armor, iron_armor, steel_armor, gold_armor, mythril_armor |
| 신발 (boots) | bronze_boots, iron_boots, steel_boots, gold_boots, mythril_boots |
| 장신구 (accessory) | bronze_ring, iron_ring, steel_ring, gold_ring, mythril_ring |

### Stat Fields

| 타입 | stats 필드 | 값 타입 |
|------|------------|---------|
| weapon | attackBonus | % (예: 2, 2.3, 300) |
| armor | defenseBonus | % (예: 2, 2.3, 300) |
| boots | moveSpeed | 고정값 (예: 1, 1.15, 30) |
| accessory | hpBonus | % (예: 2, 2.3, 300) |

### Grade Mapping

| grade | 아이템 위치 |
|-------|------------|
| 1-5 | bronze |
| 6-10 | iron |
| 11-15 | steel |
| 16-20 | gold |
| 21-25 | mythril |

---

## Balance Values

### Material Base Values

| 재질 | 기본 % |
|------|--------|
| bronze | 2% |
| iron | 12% |
| steel | 30% |
| gold | 70% |
| mythril | 150% |

### Rarity Multipliers

| 희귀도 | 배수 |
|--------|------|
| common | ×1 |
| rare | ×1.15 |
| epic | ×1.3 |
| legendary | ×1.5 |
| mythic | ×2 |

### Final Values Table

| 재질 | common | rare | epic | legendary | mythic |
|------|--------|------|------|-----------|--------|
| bronze | 2% | 2.3% | 2.6% | 3% | 4% |
| iron | 12% | 13.8% | 15.6% | 18% | 24% |
| steel | 30% | 34.5% | 39% | 45% | 60% |
| gold | 70% | 80.5% | 91% | 105% | 140% |
| mythril | 150% | 172.5% | 195% | 225% | 300% |

### Balance Rule

- bronze mythic (4%) < iron common (12%) ✓
- iron mythic (24%) < steel common (30%) ✓
- steel mythic (60%) < gold common (70%) ✓
- gold mythic (140%) < mythril common (150%) ✓

---

## GameState Changes

### New Stats Structure

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

### % Calculation Logic

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
    
    // % 보너스 적용
    this.player.derivedStats.attack = Math.floor(10 * (1 + totalAttackBonus / 100));
    this.player.derivedStats.defense = Math.floor(5 * (1 + totalDefenseBonus / 100));
    this.player.derivedStats.maxHp = Math.floor(100 * (1 + totalHpBonus / 100));
    this.player.derivedStats.moveSpeed = 100 + totalMoveSpeed;
}
```

---

## Synthesis System

### Rules

- 5개 → 다음 등급 1개
- 같은 아이템명 + 다음 희귀도

### Synthesis Path

```
bronze_sword common x5 → bronze_sword rare x1
bronze_sword rare x5 → bronze_sword epic x1
bronze_sword epic x5 → bronze_sword legendary x1
bronze_sword legendary x5 → bronze_sword mythic x1
bronze_sword mythic x5 → iron_sword common x1 (베이스 전환)
...
mythril_sword mythic → 최대 등급 (합성 불가)
```

---

## UI Changes

### Inventory UI

- 4 탭 유지 (무기/갑옷/신발/장신구)
- 각 탭: 5 종류 × 5 희귀도 = 25개 표시
- 레이아웃 변경 없음

### Tooltip

```
bronze_sword
일반 · x5
공격력 +2%

좌클릭: 장착
우클릭: 합성
```

### Stats Panel

```javascript
bonusAtk = totalAttackBonus (예: +12%)
bonusDef = totalDefenseBonus (예: +8%)
bonusHp = totalHpBonus (예: +15%)
bonusMoveSpeed = totalMoveSpeed (예: +5)
```

---

## Files to Modify

| 파일 | 변경 내용 |
|------|----------|
| `data/items.csv` | 100개 아이템 데이터 재생성 |
| `src/core/GameState.js` | moveSpeed 스탯 추가, % 계산 로직 |
| `src/systems/InventorySystem.js` | 새 아이템 구조 대응 |
| `src/ui/InventoryUI.js` | 툴팁, 스탯 패널 표시 변경 |
| `css/style.css` | 변경 없음 |

---

## Future Work

- 이동속도 전투 연동 → 전투 시스템 개편 시 구현
- 공격 속도 = `attackInterval * (100 / moveSpeed)`

---

## Implementation Approach

**Approach 1: 데이터 + 최소 코드 수정**

1. items.csv 재생성 (100개)
2. GameState 확장 (moveSpeed 추가)
3. % 계산 로직 추가
4. UI 툴팁/스탯 패널 수정

---

*Last Updated: 2026-04-08*