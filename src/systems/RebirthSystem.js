/**
 * RebirthSystem - 환생 시스템
 * 레벨 50 이상에서 환생 가능, 보너스 카운트 획득
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameLogger } from '../core/Logger.js';

class RebirthSystem {
    constructor(gameState) {
        this.gameState = gameState;
        
        // 환생 업그레이드 정의 (순차적 해금)
        this.upgradeDefinitions = {
            dailyMissionBonus: {
                name: '일일 미션 보상 증가',
                description: '일일 미션 보상 +10%/level',
                maxLevel: 10,
                costPerLevel: 1,
                effect: (level) => ({ missionBonus: level * 10 }),
                unlocks: ['goldDouble', 'expDouble']  // 만렙 시 해금
            },
            goldDouble: {
                name: '골드 2배',
                description: '골드 획득량 +10%/level',
                maxLevel: 10,
                costPerLevel: 1,
                effect: (level) => ({ goldMultiplier: 1 + level * 0.1 }),
                requires: ['dailyMissionBonus'],  // 해금 조건
                unlocks: ['dropRateIncrease', 'bossGoldBonus']
            },
            dropRateIncrease: {
                name: '아이템 드롭률 증가',
                description: '아이템 드롭 확률 +10%/level (곱연산)',
                maxLevel: 10,
                costPerLevel: 1,
                effect: (level) => ({ dropRateMultiplier: 1 + level * 0.1 }),
                requires: ['goldDouble'],
                unlocks: ['upgradeDiscount']
            },
            expDouble: {
                name: '경험치 2배',
                description: '경험치 획득량 +10%/level',
                maxLevel: 10,
                costPerLevel: 1,
                effect: (level) => ({ expMultiplier: 1 + level * 0.1 }),
                requires: ['dailyMissionBonus'],
                unlocks: ['offlineBonus', 'synthesisMaster']
            },
            offlineBonus: {
                name: '오프라인 보상 2배',
                description: '오프라인 보상 +10%/level',
                maxLevel: 10,
                costPerLevel: 1,
                effect: (level) => ({ offlineMultiplier: 1 + level * 0.1 }),
                requires: ['expDouble'],
                unlocks: ['stageSkip']
            },
            bossGoldBonus: {
                name: '골드 보너스',
                description: '보스 처치 시 골드 +10%/level',
                maxLevel: 10,
                costPerLevel: 1,
                effect: (level) => ({ bossGoldMultiplier: 1 + level * 0.1 }),
                requires: ['goldDouble'],
                unlocks: ['stageSkip']
            },
            synthesisMaster: {
                name: '합성 마스터',
                description: '합성 시 추가 아이템 생성 확률 +10%/level',
                maxLevel: 10,
                costPerLevel: 1,
                effect: (level) => ({ synthesisBonusChance: level * 0.1 }),
                requires: ['expDouble'],
                unlocks: ['upgradeDiscount']
            },
            stageSkip: {
                name: '스테이지 스킵',
                description: '스테이지 클리어 시 1%확률로 1개 스킵/level',
                maxLevel: 10,
                costPerLevel: 1,
                effect: (level) => ({ stageSkipChance: level * 0.01 }),
                requires: ['offlineBonus', 'bossGoldBonus'],
                unlocks: []
            },
            upgradeDiscount: {
                name: '업그레이드 할인',
                description: '골드 업그레이드 비용 -2%/level',
                maxLevel: 10,
                costPerLevel: 1,
                effect: (level) => ({ upgradeDiscount: level * 0.02 }),
                requires: ['dropRateIncrease', 'synthesisMaster'],
                unlocks: []
            },
            expTriple: {
                name: '경험치 3배',
                description: '경험치 획득량 +10%/level (고급)',
                maxLevel: 10,
                costPerLevel: 2,
                effect: (level) => ({ expMultiplier: 1 + level * 0.1 }),
                requires: [],  // 모든 업그레이드 만렙 시 해금
                isHidden: true,
                unlocks: []
            },
            goldTriple: {
                name: '골드 3배',
                description: '골드 획득량 +10%/level (고급)',
                maxLevel: 10,
                costPerLevel: 2,
                effect: (level) => ({ goldMultiplier: 1 + level * 0.1 }),
                requires: [],  // 모든 업그레이드 만렙 시 해금
                isHidden: true,
                unlocks: []
            }
        };
    }

    /**
     * 초기화
     */
    init() {
        // 초기화 작업 없음 (GameState에서 직접 관리)
    }

    /**
     * 환생 가능 여부 확인
     */
    canRebirth() {
        return this.gameState.canRebirth();
    }

    /**
     * 필요한 레벨 확인
     */
    getRequiredLevel() {
        return this.gameState.rebirth.minLevel;
    }

    /**
     * 현재 레벨 확인
     */
    getCurrentLevel() {
        return this.gameState.player.level;
    }

    /**
     * 환생 시 얻을 보너스 카운트 확인
     */
    getBonusPointsOnRebirth() {
        return this.gameState.calculateRebirthBonus();
    }

    /**
     * 현재 보너스 카운트 확인
     */
    getBonusPoints() {
        return this.gameState.rebirth.bonusPoints;
    }

    /**
     * 환생 횟수 확인
     */
    getRebirthCount() {
        return this.gameState.rebirth.count;
    }

    /**
     * 환생 실행
     */
    performRebirth() {
        if (!this.canRebirth()) {
            gameLogger.warn('Cannot rebirth: level too low');
            return false;
        }

        const bonusPoints = this.gameState.performRebirth();
        gameLogger.info(`Rebirth #${this.gameState.rebirth.count} performed. Gained ${bonusPoints} bonus points.`);
        
        return true;
    }

    /**
     * 업그레이드 정의 가져오기
     */
    getUpgradeDefinition(key) {
        return this.upgradeDefinitions[key];
    }

    /**
     * 모든 업그레이드 정의 가져오기
     */
    getAllUpgradeDefinitions() {
        return Object.entries(this.upgradeDefinitions).map(([key, def]) => {
            const currentLevel = this.gameState.rebirth.upgrades[key] || 0;
            const isUnlocked = this.isUpgradeUnlocked(key);
            
            return {
                key,
                ...def,
                currentLevel,
                isUnlocked
            };
        });
    }

    /**
     * 업그레이드 해금 여부 확인
     */
    isUpgradeUnlocked(key) {
        const def = this.upgradeDefinitions[key];
        if (!def) return false;
        
        // requires가 비어있고 isHidden이면 모든 업그레이드 만렙 확인
        if (def.requires && def.requires.length === 0 && def.isHidden) {
            return this.areAllUpgradesMaxedExcept(key);
        }
        
        // requires가 있으면 해당 업그레이드들이 만렙인지 확인
        if (def.requires && def.requires.length > 0) {
            return def.requires.every(reqKey => {
                const reqLevel = this.gameState.rebirth.upgrades[reqKey] || 0;
                const reqDef = this.upgradeDefinitions[reqKey];
                return reqLevel >= reqDef.maxLevel;
            });
        }
        
        // requires가 없는 기본 업그레이드는 항상 해금
        return true;
    }

    /**
     * 해당 업그레이드를 제외한 모든 업그레이드가 만렙인지 확인
     */
    areAllUpgradesMaxedExcept(exceptKey) {
        return Object.entries(this.upgradeDefinitions).every(([key, def]) => {
            if (key === exceptKey) return true;
            if (def.isHidden) return true; // hidden 업그레이드는 제외
            const level = this.gameState.rebirth.upgrades[key] || 0;
            return level >= def.maxLevel;
        });
    }

    /**
     * 업그레이드 구매 가능 여부
     */
    canPurchaseUpgrade(key) {
        const def = this.upgradeDefinitions[key];
        if (!def) return false;
        
        // 해금되지 않았으면 구매 불가
        if (!this.isUpgradeUnlocked(key)) return false;
        
        const currentLevel = this.gameState.rebirth.upgrades[key] || 0;
        if (currentLevel >= def.maxLevel) return false;
        
        const cost = def.costPerLevel;
        if (this.gameState.rebirth.bonusPoints < cost) return false;
        
        return true;
    }

    /**
     * 업그레이드 구매
     */
    purchaseUpgrade(key) {
        if (!this.canPurchaseUpgrade(key)) {
            return false;
        }

        const def = this.upgradeDefinitions[key];
        const cost = def.costPerLevel;

        this.gameState.rebirth.bonusPoints -= cost;
        this.gameState.rebirth.upgrades[key] = (this.gameState.rebirth.upgrades[key] || 0) + 1;

        gameEventBus.emit(GAME_EVENTS.REBIRTH_UPGRADE_PURCHASED, {
            key,
            level: this.gameState.rebirth.upgrades[key]
        });

        gameLogger.info(`Purchased rebirth upgrade: ${def.name} to level ${this.gameState.rebirth.upgrades[key]}`);

        return true;
    }

    /**
     * 업그레이드 효과 가져오기 (통합)
     */
    getCombinedEffects() {
        const effects = {
            missionBonus: 0,
            goldMultiplier: 1,
            dropRateMultiplier: 1,
            expMultiplier: 1,
            offlineMultiplier: 1,
            bossGoldMultiplier: 1,
            synthesisBonusChance: 0,
            stageSkipChance: 0,
            upgradeDiscount: 0
        };

        Object.entries(this.gameState.rebirth.upgrades).forEach(([key, level]) => {
            if (level > 0 && this.upgradeDefinitions[key]) {
                const upgradeEffects = this.upgradeDefinitions[key].effect(level);
                Object.entries(upgradeEffects).forEach(([effectKey, value]) => {
                    if (typeof value === 'number') {
                        if (effectKey.endsWith('Multiplier')) {
                            effects[effectKey] *= value;
                        } else if (effectKey.endsWith('Chance') || effectKey.endsWith('Discount')) {
                            effects[effectKey] += value;
                        } else {
                            effects[effectKey] += value;
                        }
                    }
                });
            }
        });

        return effects;
    }

    /**
     * [테스트용] 레벨 강제 설정
     */
    setLevel(level) {
        this.gameState.player.level = level;
        this.gameState.player.exp = 0;
        this.gameState.player.maxExp = 100;
    }

    /**
     * [테스트용] 보너스 포인트 추가
     */
    addBonusPoints(amount) {
        this.gameState.rebirth.bonusPoints += amount;
        gameLogger.info(`[TEST] Added ${amount} bonus points`);
    }
}

export { RebirthSystem };
