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
                moveSpeed: 100,
                hpRegen: 0,
                attackSpeed: 100,
                decisiveChance: 0,
                decisiveDamage: 2,
                goldBonus: 0,
                expBonus: 0
            },
            goldUpgrades: {
                attack: 0,
                defense: 0,
                hp: 0,
                hpRegen: 0,
                attackSpeed: 0,
                critChance: 0,
                critDamage: 0,
                decisiveChance: 0,
                decisiveDamage: 0,
                goldBonus: 0,
                expBonus: 0
            },
            statUpgrades: {
                attack: 0,
                defense: 0,
                hp: 0,
                hpRegen: 0,
                attackSpeed: 0,
                critChance: 0,
                critDamage: 0
            }
        };
        this.stage = {
            current: 1,
            max: 1,
            kills: 0,
            killsInStage: 0,
            autoRepeat: false
        };
        
        // 전투 페이즈 상태
        this.combatPhase = {
            phase: 'IDLE', // 'IDLE', 'MOVING', 'ENCOUNTERING', 'COMBAT', 'VICTORY', 'DEFEATED'
            playerState: 'idle', // 'idle', 'moving', 'attacking', 'dead'
            monsterState: 'idle', // 'appearing', 'charging', 'idle', 'dead'
            phaseTimer: 0, // 현재 페이즈 경과 시간 (ms)
            moveProgress: 0, // 이동 진행도 (0~1)
            encounterTimer: 0, // 조우 대기 시간
            combatTimer: 0, // 전투 시작 후 경과 시간
            victoryTimer: 0, // 처치 후 대기 시간
            lastAttackTime: 0, // 마지막 공격 시간
            attackCooldown: 800, // 공격 쿨타임 (ms)
            hitDuration: 300, // 피격 애니메이션 지속 시간 (ms)
            victoryDuration: 1000, // 처치 후 다음 몬스터까지 대기 시간
            attackAnimStartTime: 0, // 공격 애니메이션 시작 시간
            hitAnimStartTime: 0, // 피격 애니메이션 시작 시간
            attackAnimDuration: 400, // 공격 애니메이션 지속 시간 (ms)
            playerHitDuration: 300, // 플레이어 피격 애니메이션 지속 시간 (ms)
            autoRepeat: false, // 자동반복 모드 여부
            defeatTimer: 0, // 패배 후 대기 시간
            attackCurrentFrame: 0, // 현재 공격 프레임 (0, 1, 2)
            monsterAppearProgress: 0 // 몬스터 등장 진행도
        };
        this.inventory = {
            items: new Map(),  // itemId -> { count, grade, itemId }
            discoveredItems: new Set(),  // 한 번이라도 획득한 itemId 집합 (영구 해제)
            gold: 0,
            gems: 0  // 보석
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
        this.dailyMissions = {
            missions: [],  // { id, type, target, progress, completed, claimed }
            lastReset: Date.now(),
            buffs: {
                attackDouble: 0,  // 공격력 2배 (종료 시간)
                defenseDouble: 0,  // 방어력 2배
                goldDouble: 0,  // 골드 2배 드롭
                expDouble: 0  // 경험치 2배
            }
        };
        this.rebirth = {
            count: 0,  // 환생 횟수
            bonusPoints: 0,  // 보너스 카운트
            minLevel: 50,  // 최소 환생 레벨
            upgrades: {
                dailyMissionBonus: 0,    // 1. 일일 미션 보상 증가
                goldDouble: 0,           // 2. 골드 2배
                dropRateIncrease: 0,     // 3. 아이템 드롭률 증가
                expDouble: 0,            // 4. 경험치 2배
                offlineBonus: 0,         // 5. 오프라인 보상 2배
                bossGoldBonus: 0,        // 6. 골드 보너스 (보스)
                synthesisMaster: 0,      // 7. 합성 마스터
                stageSkip: 0,            // 8. 스테이지 스킵
                upgradeDiscount: 0,      // 9. 업그레이드 할인
                expTriple: 0,            // 10. 경험치 3배
                goldTriple: 0            // 11. 골드 3배
            }
        };
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
     * 전투 페이즈 초기화 (IDLE 상태로)
     */
    resetCombatPhase() {
        this.combatPhase.phase = 'IDLE';
        this.combatPhase.playerState = 'idle';
        this.combatPhase.monsterState = 'idle';
        this.combatPhase.phaseTimer = 0;
        this.combatPhase.moveProgress = 0;
        this.combatPhase.encounterTimer = 0;
        this.combatPhase.combatTimer = 0;
        this.combatPhase.victoryTimer = 0;
        this.combatPhase.lastAttackTime = 0;
        this.combatPhase.attackAnimStartTime = 0;
        this.combatPhase.hitAnimStartTime = 0;
        this.combatPhase.attackCurrentFrame = 0;
        this.combatPhase.monsterAppearProgress = 0;
    }

    /**
     * 이동 페이즈 시작
     */
    startMoving() {
        this.combatPhase.phase = 'MOVING';
        this.combatPhase.playerState = 'moving';
        this.combatPhase.monsterState = 'idle';
        this.combatPhase.moveProgress = 0;
        this.combatPhase.phaseTimer = 0;
    }

    /**
     * 조우 페이즈 시작 (이동 완료)
     */
    startEncounter() {
        this.combatPhase.phase = 'ENCOUNTERING';
        this.combatPhase.playerState = 'idle';
        this.combatPhase.monsterState = 'charging'; // 돌진 상태로 시작
        this.combatPhase.encounterTimer = 0;
        this.combatPhase.phaseTimer = 0;
    }

    /**
     * 전투 페이즈 시작
     */
    startCombat() {
        this.combatPhase.phase = 'COMBAT';
        this.combatPhase.playerState = 'idle';
        this.combatPhase.monsterState = 'idle'; // 이제 진짜 Idle (4,6 프레임)
        this.combatPhase.combatTimer = 0;
        this.combatPhase.phaseTimer = 0;
    }

    /**
     * 패배 페이즈 시작
     */
    startDefeat() {
        this.combatPhase.phase = 'DEFEATED';
        this.combatPhase.playerState = 'dead';
        this.combatPhase.monsterState = 'idle';
        this.combatPhase.defeatTimer = 0;
        this.combatPhase.phaseTimer = 0;
    }

    /**
     * 플레이어 공격 상태 설정 (단순 상태 전환만)
     */
    playerAttack() {
        this.combatPhase.playerState = 'attacking';
    }

    /**
     * 몬스터 피격 상태 설정
     */
    monsterHit() {
        this.combatPhase.monsterState = 'hit';
        this.combatPhase.hitAnimStartTime = performance.now(); // 피격 애니메이션 시작 시간
    }

    /**
     * 승리 페이즈 시작
     */
    startVictory() {
        this.combatPhase.phase = 'VICTORY';
        this.combatPhase.playerState = 'idle';
        this.combatPhase.monsterState = 'dead';
        this.combatPhase.victoryTimer = 0;
        this.combatPhase.phaseTimer = 0;
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
        
        // 골드 업그레이드 + 스탯 업그레이드 합산
        const goldUp = this.player.goldUpgrades;
        const statUp = this.player.statUpgrades;
        
        // 기본 스탯 계산 + 양쪽 업그레이드 합산
        const baseAttack = 10 + this.player.stats.str * 2 + (goldUp.attack + statUp.attack) * 2;
        const baseDefense = 5 + this.player.stats.vit * 0.5 + (goldUp.defense + statUp.defense) * 1;
        const baseMaxHp = 100 + this.player.stats.vit * 10 + (goldUp.hp + statUp.hp) * 10;
        
        // % 보너스 적용
        this.player.derivedStats.attack = Math.floor(baseAttack * (1 + totalAttackBonus / 100));
        this.player.derivedStats.defense = Math.floor(baseDefense * (1 + totalDefenseBonus / 100));
        this.player.derivedStats.maxHp = Math.floor(baseMaxHp * (1 + totalHpBonus / 100));
        this.player.derivedStats.moveSpeed = 100 + totalMoveSpeed;
        
        // 크리티컬 (기존 + 양쪽 업그레이드 합산)
        this.player.derivedStats.critChance = 0.05 + this.player.stats.agi * 0.005 + (goldUp.critChance + statUp.critChance) * 0.002;
        this.player.derivedStats.critDamage = 1.5 + (goldUp.critDamage + statUp.critDamage) * 0.01;
        
        // 업그레이드 효과 (골드만)
        this.player.derivedStats.hpRegen = goldUp.hpRegen * 1;
        this.player.derivedStats.attackSpeed = 100 + goldUp.attackSpeed * 1;
        this.player.derivedStats.decisiveChance = goldUp.decisiveChance * 0.002;
        this.player.derivedStats.decisiveDamage = 2 + goldUp.decisiveDamage * 0.01;
        this.player.derivedStats.goldBonus = goldUp.goldBonus * 1;
        this.player.derivedStats.expBonus = goldUp.expBonus * 1;
        
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
     * 환생 가능 여부 확인
     * @returns {boolean}
     */
    canRebirth() {
        return this.player.level >= this.rebirth.minLevel;
    }

    /**
     * 환생 시 보너스 카운트 계산
     * @returns {number}
     */
    calculateRebirthBonus() {
        // 레벨 50부터 시작, 레벨 높을수록 증가
        const bonusLevel = Math.max(0, this.player.level - this.rebirth.minLevel + 1);
        // 레벨당 1 카운트 + 보너스 (높은 레벨일수록 더 많이)
        return Math.floor(bonusLevel * (1 + bonusLevel * 0.1));
    }

    /**
     * 환생 실행
     * @returns {number} 획득한 보너스 카운트
     */
    performRebirth() {
        if (!this.canRebirth()) return 0;

        const bonusPoints = this.calculateRebirthBonus();

        // 환생 횟수 증가
        this.rebirth.count++;
        this.rebirth.bonusPoints += bonusPoints;

        // 플레이어 초기화 (레벨 1)
        this.player.level = 1;
        this.player.exp = 0;
        this.player.maxExp = 100;
        this.player.statPoints = 0;
        this.player.stats = {
            str: 5,
            agi: 5,
            int: 5,
            vit: 10
        };

        // 스테이지 초기화
        this.stage.current = 1;
        this.stage.max = 1;
        this.stage.kills = 0;
        this.stage.killsInStage = 0;

        // 골드 초기화
        this.inventory.gold = 0;

        // 인벤토리 초기화 (발견된 아이템은 유지)
        this.inventory.items = new Map();

        // 스탯 업그레이드 초기화 (골드 업그레이드는 유지)
        this.player.statUpgrades = {
            attack: 0,
            defense: 0,
            hp: 0,
            hpRegen: 0,
            attackSpeed: 0,
            critChance: 0,
            critDamage: 0
        };

        // 파생 스탯 재계산
        this.recalculateDerivedStats();

        // 이벤트
        gameEventBus.emit(GAME_EVENTS.REBIRTH_PERFORMED, {
            rebirthCount: this.rebirth.count,
            bonusPoints,
            level: this.player.level
        });

        return bonusPoints;
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
            combatPhase: { ...this.combatPhase },
            inventory: {
                items: Array.from(this.inventory.items.entries()),
                gold: this.inventory.gold,
                discoveredItems: Array.from(this.inventory.discoveredItems)  // 추가
            },
            settings: { ...this.settings },
            tutorial: { ...this.tutorial },
            dailyMissions: { ...this.dailyMissions },
            rebirth: { ...this.rebirth },
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
        if (data.combatPhase) Object.assign(this.combatPhase, data.combatPhase);
        if (data.inventory) {
            this.inventory.gold = data.inventory.gold ?? 0;
            this.inventory.items = new Map(data.inventory.items || []);
            // discoveredItems 복원
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
        
        this.recalculateDerivedStats();
    }
}

export { GameState };
