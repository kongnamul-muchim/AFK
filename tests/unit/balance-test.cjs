// ========== AFK Idle RPG 실제 게임 로직 기반 밸런스 테스트 (1800게임) ==========

const fs = require('fs');
const path = require('path');

function parseCSV(text) {
    const lines = text.split('\n').filter(line => line.trim() && !line.startsWith('#'));
    const headers = lines[0].split(',').map(h => h.trim());
    return lines.slice(1).map(line => {
        const values = line.split(',').map(v => v.trim());
        const row = {};
        headers.forEach((h, i) => {
            let val = values[i];
            if (val && /^-?\d+(\.\d+)?$/.test(val)) val = parseFloat(val);
            if (val && typeof val === 'string' && val.startsWith('{')) {
                try { val = JSON.parse(val.replace(/""/g, '"')); } catch(e) {}
            }
            row[h] = val;
        });
        return row;
    });
}

const monstersRaw = parseCSV(fs.readFileSync(path.join(__dirname, '../../data/monsters.csv'), 'utf-8'));
const itemsRaw = parseCSV(fs.readFileSync(path.join(__dirname, '../../data/items.csv'), 'utf-8'));
const configRaw = parseCSV(fs.readFileSync(path.join(__dirname, '../../data/game_config.csv'), 'utf-8'));

function getCfg(cat, key, def) {
    const r = configRaw.find(c => c.category === cat && c.key === key);
    return r ? r.value : def;
}

const BASE_HP = getCfg('player', 'baseHp', 100);
const HP_PER_VIT = getCfg('player', 'hpPerVit', 10);
const CRIT_CHANCE = getCfg('combat', 'critChance', 0.05);
const CRIT_DMG_MULT = getCfg('combat', 'critDamage', 1.5);
const ATK_INTERVAL = getCfg('combat', 'attackInterval', 100);
const MONSTER_SCALE = getCfg('combat', 'monsterScalingMultiplier', 1.1);
const MIN_DMG = getCfg('combat', 'minDamage', 1);
const COMMON_RATE = getCfg('inventory', 'commonDropRate', 0.6);
const RARE_RATE = getCfg('inventory', 'rareDropRate', 0.3);
const EPIC_RATE = getCfg('inventory', 'epicDropRate', 0.09);
const LEGENDARY_RATE = getCfg('inventory', 'legendaryDropRate', 0.01);

const NORMAL_PROBS = [0.50, 0.30, 0.15, 0.05, 0];
const BOSS_PROBS = [0, 0.50, 0.35, 0.15, 0];
const RARITY_NAMES = ['common', 'rare', 'epic', 'legendary', 'mythic'];

function weightedRandomIndex(probs) {
    const roll = Math.random();
    let cum = 0;
    for (let i = 0; i < probs.length; i++) {
        cum += probs[i];
        if (roll < cum) return i;
    }
    return probs.length - 1;
}

// 아이템별 스탯 보너스 합산
function getItemStats(stats) {
    if (!stats || typeof stats !== 'object') return { attackBonus: 0, defenseBonus: 0, moveSpeed: 0, hpBonus: 0 };
    return {
        attackBonus: stats.attackBonus || 0,
        defenseBonus: stats.defenseBonus || 0,
        moveSpeed: stats.moveSpeed || 0,
        hpBonus: stats.hpBonus || 0
    };
}

// grade에 따른 equipment bonus
function calcItemBonus(grade) {
    return {
        attackBonus: Math.floor(grade * 2.5),
        defenseBonus: Math.floor(grade * 1.8),
        hpBonus: Math.floor(grade * 5)
    };
}

class SimPlayer {
    constructor() {
        this.level = 1;
        this.exp = 0;
        this.gold = 0;
        this.currentHp = 100;
        this.maxHp = 100;
        this.attack = 5;
        this.defense = 0;
        this.vit = 0;
        this.equipment = { weapon: null, armor: null, boots: null, accessory: null };
        this.inventory = [];
        this.equipGrades = { weapon: 1, armor: 1, boots: 1, accessory: 1 };
    }

    calcStats() {
        const baseHp = BASE_HP + this.vit * HP_PER_VIT;
        let atk = 5 + this.level * 2;
        let def = Math.floor(this.level * 1.5);
        let hpBonus = 0;

        for (const slot of ['weapon', 'armor', 'boots', 'accessory']) {
            if (this.equipment[slot]) {
                const eq = this.equipment[slot];
                const bonus = calcItemBonus(eq.grade);
                atk += bonus.attackBonus;
                def += bonus.defenseBonus;
                hpBonus += bonus.hpBonus;
            }
        }

        this.maxHp = baseHp + hpBonus;
        this.attack = Math.max(1, atk);
        this.defense = Math.max(0, def);
    }

    equipBest(items) {
        const types = ['weapon', 'armor', 'boots', 'accessory'];
        for (const type of types) {
            const available = items
                .filter(i => i.itemType === type && i.count > 0)
                .sort((a, b) => b.grade - a.grade);
            if (available.length > 0) {
                this.equipment[type] = available[0];
                this.equipGrades[type] = available[0].grade;
            }
        }
    }

    addItem(item) {
        const existing = this.inventory.find(i => i.itemId === item.itemId);
        if (existing) {
            existing.count += 1;
        } else {
            this.inventory.push({ ...item, count: 1 });
        }
    }

    synthesizeAll() {
        const types = ['weapon', 'armor', 'boots', 'accessory'];
        let changed = true;
        let iterations = 0;
        while (changed && iterations < 50) {
            changed = false;
            iterations++;
            for (const type of types) {
                const sameType = this.inventory.filter(i => i.itemType === type && i.count >= 5);
                for (const item of sameType) {
                    if (item.grade >= 15) continue;
                    const nextGrade = item.grade + 1;
                    const nextItemExists = itemsRaw.find(i => i.type === type && i.grade === nextGrade);
                    if (!nextItemExists) continue;
                    // 5개 소모 → 1개 상위
                    if (item.count >= 5) {
                        item.count -= 5;
                        if (item.count <= 0) {
                            this.inventory = this.inventory.filter(i => i !== item);
                        }
                        const existing = this.inventory.find(i => i.itemId === nextItemExists.id);
                        if (existing) {
                            existing.count += 1;
                        } else {
                            this.inventory.push({
                                itemId: nextItemExists.id,
                                itemType: type,
                                name: nextItemExists.name,
                                grade: nextGrade,
                                count: 1
                            });
                        }
                        changed = true;
                    }
                }
            }
        }
    }

    tryUpgradeEquip() {
        for (const type of ['weapon', 'armor', 'boots', 'accessory']) {
            const currentGrade = this.equipGrades[type];
            const better = this.inventory
                .filter(i => i.itemType === type && i.grade > currentGrade && i.count > 0)
                .sort((a, b) => b.grade - a.grade);
            if (better.length > 0) {
                this.equipment[type] = better[0];
                this.equipGrades[type] = better[0].grade;
            }
        }
    }
}

function getMonstersForStage(stage, isBoss) {
    if (isBoss) return monstersRaw.filter(m => m.stage === stage && m.isBoss);
    return monstersRaw.filter(m => m.stage <= stage && !m.isBoss);
}

function calcMonsterStats(monsterData, stage) {
    const mult = Math.pow(MONSTER_SCALE, stage - 1);
    return {
        maxHp: Math.floor(monsterData.hp_base * mult),
        attack: Math.floor(monsterData.atk_base * mult),
        exp: Math.floor((monsterData.exp_reward || monsterData.expReward || 10) * mult),
        gold: Math.floor((monsterData.gold_reward || monsterData.goldReward || 5) * mult)
    };
}

console.log('\n╔══════════════════════════════════════════════════════════╗');
console.log('║     AFK Idle RPG 밸런스 테스트 (장비 반영, 1800게임)     ║');
console.log('╚══════════════════════════════════════════════════════════╝');

const TOTAL_BATTLES = 1800;
const player = new SimPlayer();
let wins = 0;
let bossFights = 0;
let bossWins = 0;
let normalFights = 0;
let normalWins = 0;
let totalDrops = 0;
let dropRarityCount = { common: 0, rare: 0, epic: 0, legendary: 0, mythic: 0 };
let maxStage = 1;
let equipGradeAtEnd = { weapon: 1, armor: 1, boots: 1, accessory: 1 };

for (let battle = 0; battle < TOTAL_BATTLES; battle++) {
    const stage = Math.max(1, Math.floor(battle / 60) + 1);
    const isBoss = stage % 10 === 0;

    // 레벨업 (경험치 간소화)
    if (battle > 0 && battle % 40 === 0) {
        player.level++;
        player.vit = Math.floor(player.level * 0.4);
    }

    // 매 3배틀마다 HP 회복 (실제 게임에서는 스테이지 클리어 시)
    if (battle % 3 === 0) {
        player.currentHp = player.maxHp;
    }

    player.calcStats();

    const monsters = getMonstersForStage(stage, isBoss);
    if (monsters.length === 0) continue;
    const monsterData = monsters[Math.floor(Math.random() * monsters.length)];
    const mStats = calcMonsterStats(monsterData, stage);

    // 장비 업글 시도
    player.tryUpgradeEquip();
    player.calcStats();

    // === 전투 ===
    let pHp = player.currentHp;
    let mHp = mStats.maxHp;
    let turn = 0;
    const MAX_T = 300;
    let won = false;

    while (turn < MAX_T) {
        turn++;
        // 플레이어 공격
        const isCrit = Math.random() < CRIT_CHANCE;
        const pDmg = Math.max(MIN_DMG, player.attack * (isCrit ? CRIT_DMG_MULT : 1));
        mHp -= pDmg;
        if (mHp <= 0) { won = true; break; }

        // 몬스터 공격
        const mDmg = Math.max(MIN_DMG, mStats.attack - player.defense);
        pHp -= mDmg;
        // 회피율보정: 5% 확률로 회피
        if (Math.random() < 0.05) { pHp += mDmg; } // 회피
        if (pHp <= 0) { won = false; break; }
    }
    if (turn >= MAX_T) won = true;

    player.currentHp = pHp <= 0 ? 1 : pHp;

    if (won) {
        wins++;
        if (isBoss) bossWins++;
        else normalWins++;

        // 경험치/골드
        player.exp += mStats.exp;
        player.gold += mStats.gold;

        // 아이템 드롭
        const probs = isBoss ? BOSS_PROBS : NORMAL_PROBS;
        const gradeIdx = weightedRandomIndex(probs);
        const rarity = RARITY_NAMES[gradeIdx];
        const baseGrade = stage <= 90 ? Math.ceil(stage / 10) : 21;
        const dropGrades = [baseGrade, baseGrade+1, baseGrade+2, baseGrade+3, baseGrade+4];
        const selectedGrade = dropGrades[gradeIdx];
        const types = ['weapon', 'armor', 'boots', 'accessory'];
        const selectedType = types[Math.floor(Math.random() * types.length)];

        const matchingItem = itemsRaw.find(i => i.grade === selectedGrade && i.type === selectedType);
        if (matchingItem && rarity !== 'mythic') {
            player.addItem({ itemId: matchingItem.id, itemType: selectedType, name: matchingItem.name, grade: selectedGrade, rarity });
            totalDrops++;
            dropRarityCount[rarity]++;

            // 5개 이상 모이면 합성
            player.synthesizeAll();
        }

        maxStage = stage;
    } else {
        if (isBoss) bossFights++;
        else normalFights++;
    }
}

// === 결과 ===
const totalBoss = bossWins + bossFights;
const totalNormal = normalWins + normalFights;
const totalBattles = wins + bossFights + normalFights;
const wr = totalBattles > 0 ? ((wins / totalBattles) * 100).toFixed(1) : 'N/A';
const bossWR = totalBoss > 0 ? ((bossWins / (bossWins + bossFights)) * 100).toFixed(1) : 'N/A';

console.log(`\n📊 ${totalBattles}게임 시뮬레이션 결과`);
console.log('═'.repeat(55));

console.log(`\n⚔️  전투 통계:`);
console.log(`   전체 승률:  ${wins}/${totalBattles} (${wr}%)`);
console.log(`   일반 전투:  ${normalWins}승 / ${normalFights}패`);
console.log(`   보스 전투:  ${bossWins}승 / ${bossFights}패 (${bossWR}%)`);
console.log(`   최고 스테이지: ${maxStage}층`);
console.log(`   플레이어 레벨: ${player.level}`);
console.log(`   최종 장비: 무기${equipGradeAtEnd.weapon} / 방어구${equipGradeAtEnd.armor} / 신발${equipGradeAtEnd.boots} / 장신구${equipGradeAtEnd.accessory}`);

console.log(`\n🎁 아이템 드롭 (${totalDrops}개):`);
for (const [k, v] of Object.entries(dropRarityCount)) {
    const pct = totalDrops > 0 ? ((v / totalDrops) * 100).toFixed(2) : '0.00';
    console.log(`   ${k}: ${v}개 (${pct}%)`);
}

console.log(`\n💰 골드: ${player.gold.toLocaleString()}`);
console.log(`📦 인벤토리 아이템: ${player.inventory.length}종`);

console.log('\n✅ 테스트 완료');
