/**
 * Buff System Test - Node.js에서 실행 가능한 독립 테스트
 * 브라우저 의존성(window, document 등)을 제거하고 버프 시스템의 핵심 로직만 검증
 */

const LOG_LEVELS = { DEBUG: 0, INFO: 1, WARN: 2, ERROR: 3 };

class EventBus {
    constructor() { this.events = new Map(); }
    on(event, callback) {
        if (!this.events.has(event)) this.events.set(event, []);
        this.events.get(event).push(callback);
    }
    emit(event, data) {
        if (!this.events.has(event)) return;
        [...this.events.get(event)].forEach(cb => { try { cb(data); } catch(e) {} });
    }
}

const gameEventBus = new EventBus();
const GAME_EVENTS = {
    PLAYER_LEVELUP: 'player:levelup',
    PLAYER_EXP_CHANGED: 'player:exp_changed',
    INVENTORY_GOLD_CHANGED: 'inventory:gold_changed',
    COMBAT_MONSTER_KILLED: 'combat:monster_killed',
    BUFF_ACTIVATED: 'buff:activated'
};

const gameLogger = {
    debug() {}, info() {}, warn() {}, error() {}
};

class GameState {
    constructor() {
        this.version = 1;
        this.player = {
            level: 1, exp: 0, maxExp: 100,
            stats: { str: 5, agi: 5, int: 5, vit: 10 },
            statPoints: 0,
            currentHp: 100, maxHp: 100,
            equipment: { weapon: null, armor: null, accessory: null },
            derivedStats: {
                attack: 10, defense: 5, critChance: 0.05, critDamage: 1.5,
                maxHp: 100, moveSpeed: 100, hpRegen: 0, attackSpeed: 100,
                decisiveChance: 0, decisiveDamage: 2, goldBonus: 0, expBonus: 0,
                hpRegenAccumulator: 0
            },
            goldUpgrades: {
                attack: 0, defense: 0, hp: 0, hpRegen: 0, attackSpeed: 0,
                critChance: 0, critDamage: 0, decisiveChance: 0, decisiveDamage: 0,
                goldBonus: 0, expBonus: 0
            },
            statUpgrades: {
                attack: 0, defense: 0, hp: 0, hpRegen: 0, attackSpeed: 0,
                critChance: 0, critDamage: 0
            }
        };
        this.stage = { current: 1, max: 1, kills: 0, killsInStage: 0, autoRepeat: false };
        this.combatPhase = {
            phase: 'IDLE', playerState: 'idle', monsterState: 'idle',
            phaseTimer: 0, moveProgress: 0, encounterTimer: 0, combatTimer: 0,
            victoryTimer: 0, lastAttackTime: 0, attackCooldown: 800, hitDuration: 300,
            victoryDuration: 1000, attackAnimStartTime: 0, hitAnimStartTime: 0,
            attackAnimDuration: 400, playerHitDuration: 300, autoRepeat: false,
            defeatTimer: 0, attackCurrentFrame: 0, monsterAppearProgress: 0
        };
        this.inventory = { items: new Map(), discoveredItems: new Set(), gold: 0, gems: 0 };
        this.settings = { soundVolume: 0.8, musicVolume: 0.6, vibration: true, notifications: true, mute: false };
        this.tutorial = { completed: false, step: 0 };
        this.dailyMissions = {
            missions: [],
            lastReset: Date.now(),
            buffs: {
                attackDouble: 0,
                hpDouble: 0,
                goldDouble: 0,
                expDouble: 0
            }
        };
        this.rebirth = {
            count: 0, bonusPoints: 0, minLevel: 50,
            upgrades: {
                dailyMissionBonus: 0, goldDouble: 0, dropRateIncrease: 0, expDouble: 0,
                offlineBonus: 0, bossGoldBonus: 0, synthesisMaster: 0, stageSkip: 0,
                upgradeDiscount: 0, expTriple: 0, goldTriple: 0
            }
        };
        this.stats = { playTime: 0, totalKills: 0, maxStage: 1, totalGold: 0, totalLevelups: 0 };
        this.lastSaveTime = Date.now();
        this.lastLoginTime = Date.now();
    }

    notifyChange(path) { gameEventBus.emit('game:state_changed', { path, state: this }); }

    addExp(amount) {
        if (this.player.exp == null) this.player.exp = 0;
        this.player.exp += amount;
        let leveledUp = false;
        while (this.player.exp >= this.player.maxExp) {
            this.player.exp -= this.player.maxExp;
            this.player.level++;
            this.player.statPoints++;
            this.player.maxExp = Math.floor(this.player.maxExp * 1.2);
            this.stats.totalLevelups++;
            leveledUp = true;
            this.player.currentHp = this.player.maxHp;
            gameEventBus.emit(GAME_EVENTS.PLAYER_LEVELUP, { level: this.player.level });
        }
        gameEventBus.emit(GAME_EVENTS.PLAYER_EXP_CHANGED, { exp: this.player.exp, maxExp: this.player.maxExp });
        if (leveledUp) this.notifyChange('player.level');
        return leveledUp;
    }

    addGold(amount) {
        if (this.inventory.gold == null) this.inventory.gold = 0;
        this.inventory.gold += amount;
        gameEventBus.emit(GAME_EVENTS.INVENTORY_GOLD_CHANGED, { gold: this.inventory.gold });
        this.notifyChange('inventory.gold');
    }

    killMonster() {
        this.stage.kills++;
        this.stage.killsInStage++;
        this.stats.totalKills++;
        const isBossStage = this.stage.current % 10 === 0;
        const clearCondition = isBossStage ? 1 : 10;
        if (this.stage.killsInStage >= clearCondition) {
            this.stage.killsInStage = 0;
            this.stage.current++;
            this.stage.max = Math.max(this.stage.max, this.stage.current);
            this.stats.maxStage = Math.max(this.stats.maxStage, this.stage.current);
        }
    }

    hasActiveHpBuff() {
        const buffTime = this.dailyMissions.buffs.hpDouble;
        return buffTime > Date.now();
    }

    toJSON() {
        return {
            version: this.version, player: { ...this.player }, stage: { ...this.stage },
            combatPhase: { ...this.combatPhase },
            inventory: { items: Array.from(this.inventory.items.entries()), gold: this.inventory.gold, discoveredItems: Array.from(this.inventory.discoveredItems) },
            settings: { ...this.settings }, tutorial: { ...this.tutorial },
            dailyMissions: { ...this.dailyMissions }, rebirth: { ...this.rebirth },
            stats: { ...this.stats }, lastSaveTime: this.lastSaveTime, lastLoginTime: this.lastLoginTime
        };
    }

    fromJSON(data) {
        if (data.version) this.version = data.version;
        if (data.player) Object.assign(this.player, data.player);
        if (data.stage) Object.assign(this.stage, data.stage);
        if (data.combatPhase) Object.assign(this.combatPhase, data.combatPhase);
        if (data.inventory) {
            this.inventory.gold = data.inventory.gold ?? 0;
            const normalizedItems = new Map();
            (data.inventory.items || []).forEach(([key, value]) => {
                const strKey = key.toString();
                if (normalizedItems.has(strKey)) {
                    normalizedItems.get(strKey).count += value.count || 0;
                } else {
                    normalizedItems.set(strKey, value);
                }
            });
            this.inventory.items = normalizedItems;
            if (data.inventory.discoveredItems) {
                this.inventory.discoveredItems = new Set(data.inventory.discoveredItems);
            }
        }
        if (data.settings) Object.assign(this.settings, data.settings);
        if (data.tutorial) Object.assign(this.tutorial, data.tutorial);
        if (data.dailyMissions) Object.assign(this.dailyMissions, data.dailyMissions);
        if (data.rebirth) Object.assign(this.rebirth, data.rebirth);
        if (data.stats) Object.assign(this.stats, data.stats);
        if (data.lastSaveTime) this.lastSaveTime = data.lastSaveTime;
    }
}

class DailyMissionSystem {
    constructor(gameState) {
        this.gameState = gameState;
    }

    hasActiveBuff(buffType) {
        const buffTime = this.gameState.dailyMissions.buffs[buffType];
        return buffTime > Date.now();
    }

    activateBuff(buffType, durationMinutes) {
        const durationMs = durationMinutes * 60 * 1000;
        this.gameState.dailyMissions.buffs[buffType] = Date.now() + durationMs;
        gameEventBus.emit(GAME_EVENTS.BUFF_ACTIVATED, { buffType, duration: durationMinutes });
    }

    getBuffMultiplier(buffType) {
        if (this.hasActiveBuff(buffType)) {
            return 2.0;
        }
        return 1.0;
    }
}

class CombatSystem {
    constructor(gameState, dailyMissionSystem) {
        this.gameState = gameState;
        this._dailyMissionSystem = dailyMissionSystem;
        this.currentMonster = null;
    }

    setDailyMissionSystem(dailyMissionSystem) {
        this._dailyMissionSystem = dailyMissionSystem;
    }

    getBuffMultiplier(buffType) {
        if (this._dailyMissionSystem) {
            return this._dailyMissionSystem.getBuffMultiplier(buffType);
        }
        return 1.0;
    }

    simulateMonsterKill(expReward, goldReward) {
        const goldBuff = this.getBuffMultiplier('goldDouble');
        const expBuff = this.getBuffMultiplier('expDouble');

        const finalExp = Math.floor(expReward * expBuff);
        const finalGold = Math.floor(goldReward * goldBuff);

        this.gameState.addExp(finalExp);
        this.gameState.addGold(finalGold);

        return { exp: finalExp, gold: finalGold, expBuff, goldBuff };
    }
}

// ==================== TEST RUNNER ====================

let passCount = 0;
let failCount = 0;

function assertEqual(actual, expected, message) {
    if (actual === expected) {
        passCount++;
        console.log(`  ✓ ${message}`);
    } else {
        failCount++;
        console.error(`  ✗ ${message}`);
        console.error(`    Expected: ${expected}, Got: ${actual}`);
    }
}

function describe(name, fn) {
    console.log(`\n${name}`);
    fn();
}

function it(name, fn) {
    fn();
}

// ==================== TESTS ====================

describe('Buff System Tests', () => {
    it('1. 버프 없을 때 몬스터 처치 → 기본 goldReward, expReward 확인', () => {
        const gameState = new GameState();
        const dailyMissionSystem = new DailyMissionSystem(gameState);
        const combatSystem = new CombatSystem(gameState, dailyMissionSystem);

        const initialGold = gameState.inventory.gold;
        const initialExp = gameState.player.exp;

        const result = combatSystem.simulateMonsterKill(50, 50);

        assertEqual(result.exp, 50, '경험치 보상이 50이어야 함');
        assertEqual(result.gold, 50, '골드 보상이 50이어야 함');
        assertEqual(result.expBuff, 1.0, '경험치 배율이 1.0이어야 함');
        assertEqual(result.goldBuff, 1.0, '골드 배율이 1.0이어야 함');
        assertEqual(gameState.inventory.gold, initialGold + 50, '실제 골드가 50 증가해야 함');
        assertEqual(gameState.player.exp, initialExp + 50, '실제 경험치가 50 증가해야 함');
    });

    it('2. goldDouble 버프 활성화 후 몬스터 처치 → goldReward * 2 확인', () => {
        const gameState = new GameState();
        const dailyMissionSystem = new DailyMissionSystem(gameState);
        const combatSystem = new CombatSystem(gameState, dailyMissionSystem);

        dailyMissionSystem.activateBuff('goldDouble', 30);

        const result = combatSystem.simulateMonsterKill(100, 50);

        assertEqual(result.exp, 100, '경험치 보상은 그대로 100이어야 함');
        assertEqual(result.gold, 100, '골드 보상이 100(50*2)이어야 함');
        assertEqual(result.expBuff, 1.0, '경험치 배율은 1.0이어야 함');
        assertEqual(result.goldBuff, 2.0, '골드 배율이 2.0이어야 함');
    });

    it('3. expDouble 버프 활성화 후 몬스터 처치 → expReward * 2 확인', () => {
        const gameState = new GameState();
        const dailyMissionSystem = new DailyMissionSystem(gameState);
        const combatSystem = new CombatSystem(gameState, dailyMissionSystem);

        dailyMissionSystem.activateBuff('expDouble', 30);

        const result = combatSystem.simulateMonsterKill(100, 50);

        assertEqual(result.exp, 200, '경험치 보상이 200(100*2)이어야 함');
        assertEqual(result.gold, 50, '골드 보상은 그대로 50이어야 함');
        assertEqual(result.expBuff, 2.0, '경험치 배율이 2.0이어야 함');
        assertEqual(result.goldBuff, 1.0, '골드 배율은 1.0이어야 함');
    });

    it('4. 두 버프 동시 활성화 후 몬스터 처치 → 둘 다 2배 확인', () => {
        const gameState = new GameState();
        const dailyMissionSystem = new DailyMissionSystem(gameState);
        const combatSystem = new CombatSystem(gameState, dailyMissionSystem);

        dailyMissionSystem.activateBuff('goldDouble', 30);
        dailyMissionSystem.activateBuff('expDouble', 30);

        const result = combatSystem.simulateMonsterKill(100, 50);

        assertEqual(result.exp, 200, '경험치 보상이 200(100*2)이어야 함');
        assertEqual(result.gold, 100, '골드 보상이 100(50*2)이어야 함');
        assertEqual(result.expBuff, 2.0, '경험치 배율이 2.0이어야 함');
        assertEqual(result.goldBuff, 2.0, '골드 배율이 2.0이어야 함');
    });
});

console.log(`\n========================================`);
console.log(`Results: ${passCount} passed, ${failCount} failed`);
console.log(`========================================`);

if (failCount > 0) {
    process.exit(1);
}
