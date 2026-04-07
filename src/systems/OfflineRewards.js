/**
 * OfflineRewards - 오프라인 보상 시스템
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameDataLoader } from '../data-parser/DataLoader.js';
import { gameLogger } from '../core/Logger.js';

class OfflineRewards {
    /**
     * @param {GameState} gameState 
     */
    constructor(gameState) {
        this.gameState = gameState;
    }

    /**
     * 초기화
     */
    init() {
        gameLogger.debug('OfflineRewards initialized');
    }

    /**
     * 오프라인 시간 계산 (초 단위)
     * @returns {number}
     */
    calculateOfflineSeconds() {
        const now = Date.now();
        const lastSaveTime = this.gameState.lastSaveTime;
        
        return Math.floor((now - lastSaveTime) / 1000);
    }

    /**
     * 오프라인 보상 계산
     * @param {number} seconds 
     * @returns {{exp: number, gold: number, hours: number}}
     */
    calculateReward(seconds) {
        const maxHours = gameDataLoader.getConfigNumber('offline', 'maxHours', 24);
        const expPerHour = gameDataLoader.getConfigNumber('offline', 'expPerHour', 100);
        const goldPerHour = gameDataLoader.getConfigNumber('offline', 'goldPerHour', 50);
        
        // 시간으로 변환 (최대 24 시간)
        const hours = Math.min(seconds / 3600, maxHours);
        
        // 보상 계산
        const expReward = Math.floor(hours * expPerHour);
        const goldReward = Math.floor(hours * goldPerHour);
        
        return {
            exp: expReward,
            gold: goldReward,
            hours: hours
        };
    }

    /**
     * 오프라인 보상 지급
     * @param {number} seconds 
     * @returns {{exp: number, gold: number, hours: number}}
     */
    claimReward(seconds) {
        const reward = this.calculateReward(seconds);
        
        // 보상 지급
        this.gameState.addExp(reward.exp);
        this.gameState.addGold(reward.gold);
        
        // 로그인 시간 업데이트
        this.gameState.lastLoginTime = Date.now();
        
        // 이벤트
        gameEventBus.emit(GAME_EVENTS.OFFLINE_REWARD, reward);
        
        gameLogger.info(`Offline reward claimed: ${reward.hours.toFixed(1)}h, ${reward.exp} exp, ${reward.gold} gold`);
        
        return reward;
    }

    /**
     * 오프라인 보상 표시용 텍스트 생성
     * @param {number} seconds 
     * @returns {string}
     */
    getOfflineDurationText(seconds) {
        const hours = seconds / 3600;
        
        if (hours < 1) {
            const minutes = Math.floor(seconds / 60);
            return `${minutes}분`;
        } else if (hours < 24) {
            return `${hours.toFixed(1)}시간`;
        } else {
            return '24 시간 (최대)';
        }
    }

    /**
     * 정리
     */
    destroy() {
        // 정리할 작업 없음
    }
}

export { OfflineRewards };
