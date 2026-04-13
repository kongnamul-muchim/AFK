# Item System Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 아이템 시스템 전면 개편 - 4 탭 × 5 종류 × 5 희귀도 = 100개 아이템, % 기반 스탯 시스템 도입

**Architecture:** items.csv 데이터 재생성, GameState에 moveSpeed 스탯 추가, % 계산 로직 구현, UI 툴팁 및 스탯 패널 수정

**Tech Stack:** JavaScript (ES6+), CSV data files, HTML/CSS

---

## File Structure

| 파일 | 역할 | 상태 |
|------|------|------|
| `data/items.csv` | 100개 아이템 데이터 | 수정 |
| `src/core/GameState.js` | moveSpeed 스탯 추가, % 계산 로직 | 수정 |
| `src/systems/InventorySystem.js` | 새 아이템 구조 대응 | 수정 |
| `src/ui/InventoryUI.js` | 툴팁, 스탯 패널 표시 변경 | 수정 |

---

## Task 1: items.csv 재생성 (100개 아이템)

**Files:**
- Modify: `data/items.csv`

- [ ] **Step 1: items.csv 백업**

```bash
cp data/items.csv data/items.csv.backup
```

- [ ] **Step 2: 새 items.csv 작성 (무기 25개)**

```csv
# items.csv - 아이템 데이터 (100개)
# id,name,grade,type,rarity,stats,dropRate
id,name,grade,type,rarity,stats,dropRate
1,bronze_sword,1,weapon,common,"{""attackBonus"":2}",0.30
2,bronze_sword,2,weapon,rare,"{""attackBonus"":2.3}",0.25
3,bronze_sword,3,weapon,epic,"{""attackBonus"":2.6}",0.15
4,bronze_sword,4,weapon,legendary,"{""attackBonus"":3}",0.05
5,bronze_sword,5,weapon,mythic,"{""attackBonus"":4}",0.01
6,iron_sword,6,weapon,common,"{""attackBonus"":12}",0.30
7,iron_sword,7,weapon,rare,"{""attackBonus"":13.8}",0.25
8,iron_sword,8,weapon,epic,"{""attackBonus"":15.6}",0.15
9,iron_sword,9,weapon,legendary,"{""attackBonus"":18}",0.05
10,iron_sword,10,weapon,mythic,"{""attackBonus"":24}",0.01
11,steel_sword,11,weapon,common,"{""attackBonus"":30}",0.30
12,steel_sword,12,weapon,rare,"{""attackBonus"":34.5}",0.25
13,steel_sword,13,weapon,epic,"{""attackBonus"":39}",0.15
14,steel_sword,14,weapon,legendary,"{""attackBonus"":45}",0.05
15,steel_sword,15,weapon,mythic,"{""attackBonus"":60}",0.01
16,gold_sword,16,weapon,common,"{""attackBonus"":70}",0.30
17,gold_sword,17,weapon,rare,"{""attackBonus"":80.5}",0.25
18,gold_sword,18,weapon,epic,"{""attackBonus"":91}",0.15
19,gold_sword,19,weapon,legendary,"{""attackBonus"":105}",0.05
20,gold_sword,20,weapon,mythic,"{""attackBonus"":140}",0.01
21,mythril_sword,21,weapon,common,"{""attackBonus"":150}",0.30
22,mythril_sword,22,weapon,rare,"{""attackBonus"":172.5}",0.25
23,mythril_sword,23,weapon,epic,"{""attackBonus"":195}",0.15
24,mythril_sword,24,weapon,legendary,"{""attackBonus"":225}",0.05
25,mythril_sword,25,weapon,mythic,"{""attackBonus"":300}",0.01
```

- [ ] **Step 3: 갑옷 25개 추가**

```csv
26,bronze_armor,1,armor,common,"{""defenseBonus"":2}",0.30
27,bronze_armor,2,armor,rare,"{""defenseBonus"":2.3}",0.25
28,bronze_armor,3,armor,epic,"{""defenseBonus"":2.6}",0.15
29,bronze_armor,4,armor,legendary,"{""defenseBonus"":3}",0.05
30,bronze_armor,5,armor,mythic,"{""defenseBonus"":4}",0.01
31,iron_armor,6,armor,common,"{""defenseBonus"":12}",0.30
32,iron_armor,7,armor,rare,"{""defenseBonus"":13.8}",0.25
33,iron_armor,8,armor,epic,"{""defenseBonus"":15.6}",0.15
34,iron_armor,9,armor,legendary,"{""defenseBonus"":18}",0.05
35,iron_armor,10,armor,mythic,"{""defenseBonus"":24}",0.01
36,steel_armor,11,armor,common,"{""defenseBonus"":30}",0.30
37,steel_armor,12,armor,rare,"{""defenseBonus"":34.5}",0.25
38,steel_armor,13,armor,epic,"{""defenseBonus"":39}",0.15
39,steel_armor,14,armor,legendary,"{""defenseBonus"":45}",0.05
40,steel_armor,15,armor,mythic,"{""defenseBonus"":60}",0.01
41,gold_armor,16,armor,common,"{""defenseBonus"":70}",0.30
42,gold_armor,17,armor,rare,"{""defenseBonus"":80.5}",0.25
43,gold_armor,18,armor,epic,"{""defenseBonus"":91}",0.15
44,gold_armor,19,armor,legendary,"{""defenseBonus"":105}",0.05
45,gold_armor,20,armor,mythic,"{""defenseBonus"":140}",0.01
46,mythril_armor,21,armor,common,"{""defenseBonus"":150}",0.30
47,mythril_armor,22,armor,rare,"{""defenseBonus"":172.5}",0.25
48,mythril_armor,23,armor,epic,"{""defenseBonus"":195}",0.15
49,mythril_armor,24,armor,legendary,"{""defenseBonus"":225}",0.05
50,mythril_armor,25,armor,mythic,"{""defenseBonus"":300}",0.01
```

- [ ] **Step 4: 신발 25개 추가**

```csv
51,bronze_boots,1,boots,common,"{""moveSpeed"":1}",0.30
52,bronze_boots,2,boots,rare,"{""moveSpeed"":1.15}",0.25
53,bronze_boots,3,boots,epic,"{""moveSpeed"":1.3}",0.15
54,bronze_boots,4,boots,legendary,"{""moveSpeed"":1.5}",0.05
55,bronze_boots,5,boots,mythic,"{""moveSpeed"":2}",0.01
56,iron_boots,6,boots,common,"{""moveSpeed"":6}",0.30
57,iron_boots,7,boots,rare,"{""moveSpeed"":6.9}",0.25
58,iron_boots,8,boots,epic,"{""moveSpeed"":7.8}",0.15
59,iron_boots,9,boots,legendary,"{""moveSpeed"":9}",0.05
60,iron_boots,10,boots,mythic,"{""moveSpeed"":12}",0.01
61,steel_boots,11,boots,common,"{""moveSpeed"":15}",0.30
62,steel_boots,12,boots,rare,"{""moveSpeed"":17.25}",0.25
63,steel_boots,13,boots,epic,"{""moveSpeed"":19.5}",0.15
64,steel_boots,14,boots,legendary,"{""moveSpeed"":22.5}",0.05
65,steel_boots,15,boots,mythic,"{""moveSpeed"":30}",0.01
66,gold_boots,16,boots,common,"{""moveSpeed"":35}",0.30
67,gold_boots,17,boots,rare,"{""moveSpeed"":40.25}",0.25
68,gold_boots,18,boots,epic,"{""moveSpeed"":45.5}",0.15
69,gold_boots,19,boots,legendary,"{""moveSpeed"":52.5}",0.05
70,gold_boots,20,boots,mythic,"{""moveSpeed"":70}",0.01
71,mythril_boots,21,boots,common,"{""moveSpeed"":75}",0.30
72,mythril_boots,22,boots,rare,"{""moveSpeed"":86.25}",0.25
73,mythril_boots,23,boots,epic,"{""moveSpeed"":97.5}",0.15
74,mythril_boots,24,boots,legendary,"{""moveSpeed"":112.5}",0.05
75,mythril_boots,25,boots,mythic,"{""moveSpeed"":150}",0.01
```

- [ ] **Step 5: 장신구 25개 추가**

```csv
76,bronze_ring,1,accessory,common,"{""hpBonus"":2}",0.30
77,bronze_ring,2,accessory,rare,"{""hpBonus"":2.3}",0.25
78,bronze_ring,3,accessory,epic,"{""hpBonus"":2.6}",0.15
79,bronze_ring,4,accessory,legendary,"{""hpBonus"":3}",0.05
80,bronze_ring,5,accessory,mythic,"{""hpBonus"":4}",0.01
81,iron_ring,6,accessory,common,"{""hpBonus"":12}",0.30
82,iron_ring,7,accessory,rare,"{""hpBonus"":13.8}",0.25
83,iron_ring,8,accessory,epic,"{""hpBonus"":15.6}",0.15
84,iron_ring,9,accessory,legendary,"{""hpBonus"":18}",0.05
85,iron_ring,10,accessory,mythic,"{""hpBonus"":24}",0.01
86,steel_ring,11,accessory,common,"{""hpBonus"":30}",0.30
87,steel_ring,12,accessory,rare,"{""hpBonus"":34.5}",0.25
88,steel_ring,13,accessory,epic,"{""hpBonus"":39}",0.15
89,steel_ring,14,accessory,legendary,"{""hpBonus"":45}",0.05
90,steel_ring,15,accessory,mythic,"{""hpBonus"":60}",0.01
91,gold_ring,16,accessory,common,"{""hpBonus"":70}",0.30
92,gold_ring,17,accessory,rare,"{""hpBonus"":80.5}",0.25
93,gold_ring,18,accessory,epic,"{""hpBonus"":91}",0.15
94,gold_ring,19,accessory,legendary,"{""hpBonus"":105}",0.05
95,gold_ring,20,accessory,mythic,"{""hpBonus"":140}",0.01
96,mythril_ring,21,accessory,common,"{""hpBonus"":150}",0.30
97,mythril_ring,22,accessory,rare,"{""hpBonus"":172.5}",0.25
98,mythril_ring,23,accessory,epic,"{""hpBonus"":195}",0.15
99,mythril_ring,24,accessory,legendary,"{""hpBonus"":225}",0.05
100,mythril_ring,25,accessory,mythic,"{""hpBonus"":300}",0.01
```

- [ ] **Step 6: Commit**

```bash
git add data/items.csv
git commit -m "feat: regenerate items.csv with 100 items (5 materials × 5 rarities × 4 types)"
```

---

## Task 2: GameState 확장 (moveSpeed 스탯 추가)

**Files:**
- Modify: `src/core/GameState.js`

- [ ] **Step 1: derivedStats에 moveSpeed 추가**

```javascript
// line 30-33 근처
this.player.derivedStats = {
    attack: 10,
    defense: 5,
    critChance: 0.05,
    critDamage: 1.5,
    maxHp: 100,
    moveSpeed: 100  // 추가
};
```

- [ ] **Step 2: recalculateDerivedStats() 수정 - % 계산 로직 추가**

```javascript
// line 180-205 교체
recalculateDerivedStats() {
    // 장비 보너스 합산
    let totalAttackBonus = 0;
    let totalDefenseBonus = 0;
    let totalHpBonus = 0;
    let totalMoveSpeed = 0;
    
    Object.values(this.player.equipment).forEach(item => {
        if (item && item.stats) {
            if (item.stats.attackBonus) totalAttackBonus += item.stats.attackBonus;
            if (item.stats.defenseBonus) totalDefenseBonus += item.stats.defenseBonus;
            if (item.stats.hpBonus) totalHpBonus += item.stats.hpBonus;
            if (item.stats.moveSpeed) totalMoveSpeed += item.stats.moveSpeed;
        }
    });
    
    // 기본 스탯 계산
    const baseAttack = 10 + this.player.stats.str * 2;
    const baseDefense = 5 + this.player.stats.vit * 0.5;
    const baseMaxHp = 100 + this.player.stats.vit * 10;
    
    // % 보너스 적용
    this.player.derivedStats.attack = Math.floor(baseAttack * (1 + totalAttackBonus / 100));
    this.player.derivedStats.defense = Math.floor(baseDefense * (1 + totalDefenseBonus / 100));
    this.player.derivedStats.maxHp = Math.floor(baseMaxHp * (1 + totalHpBonus / 100));
    this.player.derivedStats.moveSpeed = 100 + totalMoveSpeed;
    
    // 크리티컬 (기존 유지)
    this.player.derivedStats.critChance = 0.05 + this.player.stats.agi * 0.005;
    this.player.derivedStats.critDamage = 1.5;
    
    // HP 비율 유지
    if (this.player.currentHp > this.player.derivedStats.maxHp) {
        this.player.currentHp = this.player.derivedStats.maxHp;
    }
    
    gameEventBus.emit(GAME_EVENTS.PLAYER_HP_CHANGED, {
        currentHp: this.player.currentHp,
        maxHp: this.player.derivedStats.maxHp
    });
}
```

- [ ] **Step 3: Commit**

```bash
git add src/core/GameState.js
git commit -m "feat: add moveSpeed stat and % bonus calculation to GameState"
```

---

## Task 3: InventoryUI 수정 (툴팁 및 스탯 패널)

**Files:**
- Modify: `src/ui/InventoryUI.js`

- [ ] **Step 1: showTooltip() 수정 - 스탯 표시 변경**

```javascript
// line 255-261 교체
showTooltip(item, e) {
    if (!this.tooltip) return;
    
    const owned = this.gameState.inventory.items.get(item.id.toString());
    const discovered = this.gameState.inventory.discoveredItems.has(item.id.toString());
    const count = owned ? owned.count : 0;
    
    document.getElementById('tooltip-name').textContent = item.name;
    
    // 상태 텍스트
    let statusText;
    if (count > 0) {
        statusText = `x${count}`;
    } else if (discovered) {
        statusText = 'x0 (발견)';
    } else {
        statusText = '미획득';
    }
    
    document.getElementById('tooltip-grade').textContent = 
        `${this.getRarityName(item.rarity)} · ${statusText}`;
    
    // 스탯 정보 - % 표시
    const statsHtml = this.formatStatsWithPercent(item.stats);
    document.getElementById('tooltip-stats').innerHTML = statsHtml;
    
    this.tooltip.style.display = 'block';
    this.tooltip.style.left = `${e.clientX + 15}px`;
    this.tooltip.style.top = `${e.clientY + 15}px`;
}
```

- [ ] **Step 2: formatStatsWithPercent() 메서드 추가**

```javascript
// line 409 근처에 추가
/**
 * 스탯 포맷팅 (% 표시)
 * @param {Object} stats 
 * @returns {string}
 */
formatStatsWithPercent(stats) {
    if (!stats) return '<div>옵션 없음</div>';
    
    const lines = [];
    if (stats.attackBonus) lines.push(`<div>공격력 +${stats.attackBonus}%</div>`);
    if (stats.defenseBonus) lines.push(`<div>방어력 +${stats.defenseBonus}%</div>`);
    if (stats.moveSpeed) lines.push(`<div>이동속도 +${stats.moveSpeed}</div>`);
    if (stats.hpBonus) lines.push(`<div>체력 +${stats.hpBonus}%</div>`);
    
    return lines.join('') || '<div>옵션 없음</div>';
}
```

- [ ] **Step 3: updateStatsBonus() 수정 - % 보너스 표시**

```javascript
// line 350-371 교체
updateStatsBonus() {
    const equipment = this.gameState.player.equipment;
    let totalAttackBonus = 0;
    let totalDefenseBonus = 0;
    let totalHpBonus = 0;
    let totalMoveSpeed = 0;
    
    Object.values(equipment).forEach(item => {
        if (item && item.stats) {
            if (item.stats.attackBonus) totalAttackBonus += item.stats.attackBonus;
            if (item.stats.defenseBonus) totalDefenseBonus += item.stats.defenseBonus;
            if (item.stats.hpBonus) totalHpBonus += item.stats.hpBonus;
            if (item.stats.moveSpeed) totalMoveSpeed += item.stats.moveSpeed;
        }
    });
    
    document.getElementById('bonus-atk').textContent = `+${totalAttackBonus}%`;
    document.getElementById('bonus-def').textContent = `+${totalDefenseBonus}%`;
    document.getElementById('bonus-hp').textContent = `+${totalHpBonus}%`;
    document.getElementById('bonus-crit').textContent = `+${totalMoveSpeed}`;
}
```

- [ ] **Step 4: handleEquip() 수정 - stats 저장**

```javascript
// line 291-296 교체
handleEquip(item) {
    const owned = this.gameState.inventory.items.get(item.id.toString());
    const discovered = this.gameState.inventory.discoveredItems.has(item.id.toString());
    
    if (!discovered) {
        gameLogger.warn('Cannot equip: item not discovered');
        return;
    }
    
    // 장착
    this.gameState.player.equipment[item.type] = {
        itemId: item.id,
        name: item.name,
        stats: item.stats,  // { attackBonus: 2 } 또는 { moveSpeed: 1 }
        rarity: item.rarity
    };
    
    // 파생 스탯 재계산
    this.gameState.recalculateDerivedStats();
    
    // 이벤트
    gameEventBus.emit(GAME_EVENTS.PLAYER_STAT_CHANGED);
    
    gameLogger.info(`Equipped: ${item.name}`);
    
    // UI 업데이트
    this.renderInventory();
    this.updateEquipmentPanel();
    this.showToast(`${item.name} 장착!`);
}
```

- [ ] **Step 5: Commit**

```bash
git add src/ui/InventoryUI.js
git commit -m "feat: update InventoryUI for % stat display and new tooltip format"
```

---

## Task 4: 테스트 및 검증

**Files:**
- Create: `test-new-items.js`

- [ ] **Step 1: 테스트 스크립트 작성**

```javascript
// test-new-items.js
(function testNewItems() {
    console.clear();
    console.log('╔════════════════════════════════════════════════════════╗');
    console.log('║        새 아이템 시스템 테스트                         ║');
    console.log('╚════════════════════════════════════════════════════════╝\n');
    
    // 1. 데이터 로드 확인
    const items = game.dataLoader.get('items');
    console.log(`📦 아이템 데이터: ${items.length}개`);
    
    // 2. 각 타입별 확인
    const types = ['weapon', 'armor', 'boots', 'accessory'];
    types.forEach(type => {
        const typeItems = items.filter(i => i.type === type);
        console.log(`   - ${type}: ${typeItems.length}개`);
    });
    
    // 3. 테스트 아이템 지급
    console.log('\n🎁 테스트 아이템 지급');
    game.inventorySystem.addItem({
        itemId: 1,
        name: 'bronze_sword',
        count: 25,
        grade: 1,
        type: 'weapon',
        rarity: 'common',
        stats: { attackBonus: 2 }
    });
    
    // 4. 장착 테스트
    console.log('\n⚔️  장착 테스트');
    const bronzeSword = game.dataLoader.get('items').find(i => i.id === 1);
    game.inventoryUI.handleEquip(bronzeSword);
    
    console.log(`   공격력 보너스: ${game.gameState.player.equipment.weapon?.stats?.attackBonus}%`);
    console.log(`   파생 공격력: ${game.gameState.player.derivedStats.attack}`);
    
    // 5. UI 렌더링
    game.inventoryUI.renderInventory();
    game.inventoryUI.updateEquipmentPanel();
    
    console.log('\n✅ 테스트 완료! 인벤토리를 확인하세요.');
})();
```

- [ ] **Step 2: 브라우저에서 테스트**

1. http://localhost:8080/index.html 접속
2. 콘솔에서 테스트 스크립트 실행
3. 인벤토리 열어서 확인:
   - 4 탭 각각 5행 × 5열 표시
   - 아이템 이름: bronze/iron/steel/gold/mythril
   - 툴팁에 % 스탯 표시

- [ ] **Step 3: Commit**

```bash
git add test-new-items.js
git commit -m "test: add test script for new item system"
```

---

## Task 5: 최종 정리

- [ ] **Step 1: 백업 파일 삭제**

```bash
rm data/items.csv.backup
```

- [ ] **Step 2: CHANGELOG.md 업데이트**

```markdown
## [Unreleased] - 2026-04-08

### ✨ Added

- 아이템 시스템 전면 개편: 4 탭 × 5 종류 × 5 희귀도 = 100개
- 새 아이템 이름: bronze → iron → steel → gold → mythril
- % 기반 스탯 시스템: 공격력%, 방어력%, 체력%
- 이동속도 스탯 추가 (신발, 고정값)

### 🔧 Changed

- GameState: moveSpeed 스탯 추가, % 계산 로직 구현
- InventoryUI: 툴팁 % 표시, 스탯 패널 % 표시
- items.csv: 45개 → 100개 아이템으로 재생성
```

- [ ] **Step 3: 최종 Commit**

```bash
git add CHANGELOG.md
git commit -m "docs: update CHANGELOG for item system redesign"
```

---

## Future Work (전투 시스템 개편 시)

- 이동속도 전투 연동
- 공격 속도 = `attackInterval * (100 / moveSpeed)`
- 예: moveSpeed=200 → 공격 2배 빠름

---

*Plan Created: 2026-04-08*