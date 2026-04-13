/**
 * StatsTracker - 통계 기록 시스템
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameLogger } from '../core/Logger.js';

class StatsTracker {
    /**
     * @param {GameState} gameState 
     */
    constructor(gameState) {
        this.gameState = gameState;
        this.sessionStartTime = Date.now();
    }

    /**
     * 초기화
     */
    init() {
        // 이벤트 리스너 등록
        gameEventBus.on(GAME_EVENTS.COMBAT_MONSTER_KILLED, () => {
            this.gameState.stats.totalKills++;
        });
        
        gameEventBus.on(GAME_EVENTS.PLAYER_LEVELUP, () => {
            this.gameState.stats.totalLevelups++;
        });
        
        gameEventBus.on(GAME_EVENTS.STAGE_CHANGED, (data) => {
            this.gameState.stats.maxStage = Math.max(
                this.gameState.stats.maxStage, 
                data.stage
            );
            // totalClears는 GameState.advanceStage()에서 증가시킴
        });
        
        gameEventBus.on(GAME_EVENTS.INVENTORY_GOLD_CHANGED, (data) => {
            // 총 골드 획득은 별도 추적 (현재는 간단히 구현)
        });
        
        gameLogger.debug('StatsTracker initialized');
    }

    /**
     * 플레이 시간 업데이트 (초 단위)
     * @param {number} deltaMs 
     */
    updatePlayTime(deltaMs) {
        this.gameState.stats.playTime += deltaMs;
    }

    /**
     * 통계 조회
     * @returns {Object}
     */
    getStats() {
        const playTimeSeconds = Math.floor(this.gameState.stats.playTime / 1000);
        const hours = Math.floor(playTimeSeconds / 3600);
        const minutes = Math.floor((playTimeSeconds % 3600) / 60);
        const seconds = playTimeSeconds % 60;
        
        return {
            playTime: {
                total: playTimeSeconds,
                formatted: `${hours}시간 ${minutes}분 ${seconds}초`
            },
            totalKills: this.gameState.stats.totalKills,
            maxStage: this.gameState.stats.maxStage,
            totalLevelups: this.gameState.stats.totalLevelups,
            totalGold: this.gameState.inventory.gold // 현재 골드 (총 획득은 별도)
        };
    }

    /**
     * 세션 통계 (현재 플레이 세션)
     * @returns {Object}
     */
    getSessionStats() {
        const sessionTime = Date.now() - this.sessionStartTime;
        const minutes = Math.floor(sessionTime / 60000);
        
        return {
            sessionTime: `${minutes}분`,
            killsInSession: 0, // TODO: 세션 킬수 별도 추적
            goldInSession: 0   // TODO: 세션 골드 별도 추적
        };
    }

    /**
     * 정리
     */
    destroy() {
        // 정리할 작업 없음
    }
}

export { StatsTracker };
