/**
 * OfflineRewards - 오프라인 보상 시스템
 * Unity OfflineRewardSystem.cs와 통합된 수식 사용
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameConfig } from '../config/GameConfig.js';
import { gameLogger } from '../core/Logger.js';

const OFFLINE_ITEM_DROP_PER_HOUR = 2;    // Unity: OfflineItemDropPerHour
const BASE_REWARD_MULTIPLIER = 0.1;      // Unity: OfflineRewardMultiplier = 0.1f
const GEM_BONUS_PER_LEVEL = 0.02;        // Unity: OfflineRewardBonusPerLevel = 0.02f
const MIN_OFFLINE_SECONDS = 60;          // Unity: 최소 오프라인 시간 60초

const BASE_DROP_RATES = [
    { rarity: 'common',    rate: 0.70 },
    { rarity: 'rare',      rate: 0.20 },
    { rarity: 'epic',      rate: 0.07 },
    { rarity: 'legendary', rate: 0.025 },
    { rarity: 'mythic',    rate: 0.005 }
];

class OfflineRewards {
    constructor(gameState) {
        this.gameState = gameState;
    }

    init() {
        gameLogger.debug('OfflineRewards initialized');
    }

    /**
     * 오프라인 시간 계산 (초) - Unity: CalculateOfflineTime()
     */
    calculateOfflineSeconds() {
        const now = Date.now();
        const lastSaveTime = this.gameState.lastSaveTime || now;
        return Math.floor((now - lastSaveTime) / 1000);
    }

    /**
     * 오프라인 보상 배율 - Unity: GetOfflineRewardMultiplier()
     * Base 0.1 + gem upgrade bonus 2%/level
     */
    getOfflineRewardMultiplier() {
        let multiplier = BASE_REWARD_MULTIPLIER;
        const gemUp = this.gameState.gemUpgrades;
        if (gemUp && gemUp.offlineBonus && (gemUp.offlineBonus.unlocked || gemUp.offlineBonus.level > 0)) {
            multiplier *= (1 + (gemUp.offlineBonus.level || 0) * GEM_BONUS_PER_LEVEL);
        }
        return multiplier;
    }

    /**
     * 아이템 드롭 생성 - Unity: CalculateRewards() items loop
     */
    generateOfflineItems(hours, multiplier) {
        const items = [];
        const dropCount = Math.floor(hours * OFFLINE_ITEM_DROP_PER_HOUR * multiplier);
        const maxStage = this.gameState.stage.maxStage || 1;

        for (let i = 0; i < dropCount; i++) {
            const roll = Math.random();
            let cumulative = 0;
            let selectedRarity = 'common';
            for (const dr of BASE_DROP_RATES) {
                cumulative += dr.rate;
                if (roll < cumulative) { selectedRarity = dr.rarity; break; }
            }
            const grade = Math.min(Math.max(1, Math.floor(Math.random() * Math.min(maxStage + 1, 6))), 5);
            items.push({
                itemId: `offline_item_${Date.now()}_${i}`,
                name: `오프라인 보상 (${selectedRarity})`,
                grade: grade,
                rarity: selectedRarity,
                type: ['weapon', 'armor', 'accessory', 'boots'][Math.floor(Math.random() * 4)],
                stats: {}
            });
        }
        return items;
    }

    /**
     * 오프라인 보상 계산 - Unity: CalculateRewards()
     * Gold: maxStage × 100 × multiplier × hours
     * Exp:  maxStage × 50  × multiplier × hours
     * Items: hours × 2 × multiplier
     */
    calculateReward(offlineSeconds) {
        if (offlineSeconds <= 0) {
            return { gold: 0, exp: 0, hours: 0, items: [] };
        }

        const maxHours = gameConfig.offline?.maxHours || 24;
        const hours = Math.min(offlineSeconds / 3600, maxHours);
        const multiplier = this.getOfflineRewardMultiplier();
        const maxStage = this.gameState.stage.maxStage || 1;

        const goldReward = Math.floor(maxStage * 100 * multiplier * hours);
        const expReward = Math.floor(maxStage * 50 * multiplier * hours);
        const items = this.generateOfflineItems(hours, multiplier);

        gameLogger.info(
            `오프라인 보상 계산: 골드 ${goldReward.toLocaleString()}, ` +
            `경험치 ${expReward.toLocaleString()}, 아이템 ${items.length}개`
        );

        return { gold: goldReward, exp: expReward, hours, items };
    }

    /**
     * 오프라인 보상 청구 - Unity: ClaimRewards()
     */
    claimReward(offlineSeconds) {
        const reward = this.calculateReward(offlineSeconds);

        if (reward.gold <= 0 && reward.exp <= 0 && reward.items.length === 0) {
            return reward;
        }

        this.gameState.addExp(reward.exp);
        this.gameState.addGold(reward.gold);
        reward.items.forEach(item => {
            if (this.gameState.inventory && this.gameState.inventory.addItem) {
                this.gameState.inventory.addItem(item);
            }
        });

        this.gameState.lastSaveTime = Date.now();

        gameEventBus.emit(GAME_EVENTS.OFFLINE_REWARD, reward);
        gameLogger.info(`오프라인 보상 청구 완료: ${reward.hours.toFixed(1)}h 경과`);

        return reward;
    }

    /**
     * 오프라인 보상 청구 가능 여부
     */
    canClaimRewards() {
        return this.calculateOfflineSeconds() >= MIN_OFFLINE_SECONDS;
    }

    getOfflineDurationText(seconds) {
        const hours = seconds / 3600;
        if (hours < 1) return `${Math.floor(seconds / 60)}분`;
        if (hours < 24) return `${hours.toFixed(1)}시간`;
        return '24시간 (최대)';
    }

    destroy() {}
}

export { OfflineRewards };
