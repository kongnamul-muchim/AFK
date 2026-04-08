/**
 * GameState - 게임 상태 관리 (단일 소스 오브 트루스)
 * 모든 게임 데이터는 이 객체를 통해 접근하고 수정해야 함
 */
import { gameEventBus, GAME_EVENTS } from './EventBus.js';

class GameState {
    constructor() {
        this.version = 1;
        this.player = {
            level: 1,
            exp: 0,
            maxExp: 100,
            stats: {
                str: 5,  // 힘 - 공격력
                agi: 5,  // 민첩 - 크리티컬
                int: 5,  // 지력 - 마나
                vit: 10  // 체력 - HP
            },
            statPoints: 0,
            currentHp: 100,
            maxHp: 100,
            equipment: {
                weapon: null,
                armor: null,
                accessory: null
            },
            derivedStats: {
                attack: 10,
                defense: 5,
                critChance: 0.05,
                critDamage: 1.5,
                maxHp: 100,
                moveSpeed: 100
            }
        };
        this.stage = {
            current: 1,
            max: 1,
            kills: 0,
            killsInStage: 0,
            autoRepeat: false
        };
        this.inventory = {
            items: new Map(),  // itemId -> { count, grade, itemId }
            discoveredItems: new Set(),  // 한 번이라도 획득한 itemId 집합 (영구 해제)
            gold: 0
        };
        this.settings = {
            soundVolume: 0.8,
            musicVolume: 0.6,
            vibration: true,
            notifications: true,
            mute: false
        };
        this.tutorial = {
            completed: false,
            step: 0  // 0: NOT_STARTED, 1-5: 진행중, 99: COMPLETED
        };
        this.achievements = [];  // { id, unlockedAt }
        this.stats = {
            playTime: 0,
            totalKills: 0,
            maxStage: 1,
            totalGold: 0,
            totalLevelups: 0
        };
        this.lastSaveTime = Date.now();
        this.lastLoginTime = Date.now();
    }

    /**
     * 상태 변경 감지 콜백 등록
     * @param {Function} callback 
     */
    onChange(callback) {
        gameEventBus.on('game:state_changed', callback);
    }

    /**
     * 상태 변경 알림
     * @param {string} path - 변경된 경로 (예: 'player.level')
     */
    notifyChange(path) {
        gameEventBus.emit('game:state_changed', { path, state: this });
    }

    /**
     * 상태 검증
     * @returns {{valid: boolean, errors: string[]}}
     */
    validateState() {
        const errors = [];

        // 플레이어 검증
        if (this.player.level < 1) errors.push('Invalid player level');
        if (this.player.exp < 0) errors.push('Invalid exp');
        if (this.player.currentHp < 0) errors.push('Invalid HP');
        
        // 스탯 검증
        const stats = this.player.stats;
        if (stats.str < 0 || stats.agi < 0 || stats.int < 0 || stats.vit < 0) {
            errors.push('Invalid stats');
        }
        if (this.player.statPoints < 0) errors.push('Invalid stat points');

        // 스테이지 검증
        if (this.stage.current < 1) errors.push('Invalid stage');

        // 골드 검증
        if (this.inventory.gold < 0) errors.push('Invalid gold');

        return {
            valid: errors.length === 0,
            errors
        };
    }

    /**
     * 플레이어 레벨업
     * @returns {boolean} 레벨업 성공 여부
     */
    addExp(amount) {
        this.player.exp += amount;
        this.stats.totalGold += amount; // TEMP: gold tracking
        
        let leveledUp = false;
        while (this.player.exp >= this.player.maxExp) {
            this.player.exp -= this.player.maxExp;
            this.player.level++;
            this.player.statPoints++;
            this.player.maxExp = Math.floor(this.player.maxExp * 1.2);
            this.stats.totalLevelups++;
            leveledUp = true;
            
            // HP 완전 회복
            this.player.currentHp = this.player.maxHp;
            
            gameEventBus.emit(GAME_EVENTS.PLAYER_LEVELUP, { level: this.player.level });
        }
        
        gameEventBus.emit(GAME_EVENTS.PLAYER_EXP_CHANGED, { 
            exp: this.player.exp, 
            maxExp: this.player.maxExp 
        });
        
        if (leveledUp) {
            this.notifyChange('player.level');
        }
        
        return leveledUp;
    }

    /**
     * 스탯 증가
     * @param {string} statType - 'str', 'agi', 'int', 'vit'
     * @returns {boolean} 성공 여부
     */
    increaseStat(statType) {
        if (this.player.statPoints <= 0) return false;
        if (!this.player.stats.hasOwnProperty(statType)) return false;

        this.player.stats[statType]++;
        this.player.statPoints--;
        
        // 파생 스탯 재계산
        this.recalculateDerivedStats();
        
        gameEventBus.emit(GAME_EVENTS.PLAYER_STAT_CHANGED, {
            statType,
            value: this.player.stats[statType],
            statPoints: this.player.statPoints
        });
        
        this.notifyChange(`player.stats.${statType}`);
        return true;
    }

    /**
     * 파생 스탯 재계산
     */
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

    /**
     * 데미지 입기
     * @param {number} damage - 입은 데미지
     * @returns {boolean} 생존 여부
     */
    takeDamage(damage) {
        // 방어력 적용 (최소 1 데미지)
        const actualDamage = Math.max(1, damage - this.player.derivedStats.defense);
        this.player.currentHp = Math.max(0, this.player.currentHp - actualDamage);
        
        gameEventBus.emit(GAME_EVENTS.PLAYER_HP_CHANGED, {
            currentHp: this.player.currentHp,
            maxHp: this.player.maxHp
        });
        
        const alive = this.player.currentHp > 0;
        if (!alive) {
            gameEventBus.emit(GAME_EVENTS.COMBAT_PLAYER_DIED);
        }
        
        return alive;
    }

    /**
     * 골드 추가
     * @param {number} amount 
     */
    addGold(amount) {
        this.inventory.gold += amount;
        gameEventBus.emit(GAME_EVENTS.INVENTORY_GOLD_CHANGED, { gold: this.inventory.gold });
        this.notifyChange('inventory.gold');
    }

    /**
     * 골드 사용
     * @param {number} amount 
     * @returns {boolean} 성공 여부
     */
    spendGold(amount) {
        if (this.inventory.gold < amount) return false;
        this.inventory.gold -= amount;
        gameEventBus.emit(GAME_EVENTS.INVENTORY_GOLD_CHANGED, { gold: this.inventory.gold });
        return true;
    }

    /**
     * 스테이지 진행
     */
    advanceStage() {
        this.stage.killsInStage = 0;
        this.stage.current++;
        this.stage.max = Math.max(this.stage.max, this.stage.current);
        this.stats.maxStage = Math.max(this.stats.maxStage, this.stage.current);
        
        gameEventBus.emit(GAME_EVENTS.STAGE_CHANGED, { 
            stage: this.stage.current,
            isBoss: this.stage.current % 10 === 0
        });
        
        if (this.stage.current % 10 === 0) {
            gameEventBus.emit(GAME_EVENTS.STAGE_BOSS_ENTER);
        }
        
        this.notifyChange('stage.current');
    }

    /**
     * 몬스터 처치
     */
    killMonster() {
        this.stage.kills++;
        this.stage.killsInStage++;
        this.stats.totalKills++;
        
        // 스테이지 클리어 조건 (일반: 10 마리, 보스: 1 마리)
        const isBossStage = this.stage.current % 10 === 0;
        const clearCondition = isBossStage ? 1 : 10;
        
        if (this.stage.killsInStage >= clearCondition) {
            this.advanceStage();
        }
    }

    /**
     * 직렬화 (JSON 저장용)
     * @returns {Object}
     */
    toJSON() {
        return {
            version: this.version,
            player: { ...this.player },
            stage: { ...this.stage },
            inventory: {
                items: Array.from(this.inventory.items.entries()),
                gold: this.inventory.gold,
                discoveredItems: Array.from(this.inventory.discoveredItems)  // 추가
            },
            settings: { ...this.settings },
            tutorial: { ...this.tutorial },
            achievements: [...this.achievements],
            stats: { ...this.stats },
            lastSaveTime: this.lastSaveTime,
            lastLoginTime: this.lastLoginTime
        };
    }

    /**
     * 역직렬화 (JSON 로드용)
     * @param {Object} data 
     */
    fromJSON(data) {
        if (data.version) this.version = data.version;
        if (data.player) Object.assign(this.player, data.player);
        if (data.stage) Object.assign(this.stage, data.stage);
        if (data.inventory) {
            this.inventory.gold = data.inventory.gold;
            this.inventory.items = new Map(data.inventory.items || []);
            // discoveredItems 복원
            if (data.inventory.discoveredItems) {
                this.inventory.discoveredItems = new Set(data.inventory.discoveredItems);
            }
        }
        if (data.settings) Object.assign(this.settings, data.settings);
        if (data.tutorial) Object.assign(this.tutorial, data.tutorial);
        if (data.achievements) this.achievements = data.achievements;
        if (data.stats) Object.assign(this.stats, data.stats);
        if (data.lastSaveTime) this.lastSaveTime = data.lastSaveTime;
        
        this.recalculateDerivedStats();
    }
}

export { GameState };
