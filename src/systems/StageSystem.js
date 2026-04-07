/**
 * StageSystem - 스테이지 진행 시스템
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameDataLoader } from '../data-parser/DataLoader.js';
import { gameLogger } from '../core/Logger.js';

class StageSystem {
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
        gameLogger.debug('StageSystem initialized');
    }

    /**
     * 스테이지 데이터 조회
     * @returns {Object|null}
     */
    getCurrentStageData() {
        const stage = this.gameState.stage.current;
        return gameDataLoader.getById('stages', stage);
    }

    /**
     * 보스 스테이지 여부 확인
     * @returns {boolean}
     */
    isBossStage() {
        return this.gameState.stage.current % 10 === 0;
    }

    /**
     * 스테이지 클리어 필요 처치 수
     * @returns {number}
     */
    getKillsNeeded() {
        const stageData = this.getCurrentStageData();
        return stageData ? stageData.monsterCount : 10;
    }

    /**
     * 자동 반복 모드 진입
     */
    enterAutoRepeat() {
        this.gameState.stage.autoRepeat = true;
        
        // 이전 10 층 단위로 이동
        const currentStage = this.gameState.stage.current;
        const repeatStage = Math.max(1, currentStage - 10);
        
        this.gameState.stage.current = repeatStage;
        this.gameState.stage.killsInStage = 0;
        
        gameEventBus.emit(GAME_EVENTS.STAGE_AUTO_REPEAT, {
            stage: repeatStage
        });
        
        gameEventBus.emit(GAME_EVENTS.COMBAT_LOG, {
            message: `자동 반복 모드 진입 - ${repeatStage}층에서 다시 시작합니다.`
        });
        
        gameLogger.info(`Auto-repeat: Stage ${currentStage} → ${repeatStage}`);
    }

    /**
     * 자동 반복 모드 해제
     */
    exitAutoRepeat() {
        this.gameState.stage.autoRepeat = false;
        gameLogger.debug('Auto-repeat exited');
    }

    /**
     * 재도전 가능 여부 확인
     * @returns {boolean}
     */
    canRetry() {
        // 플레이어 레벨이 보스 스테이지 평균 레벨보다 높으면 가능
        const bossStage = Math.floor(this.gameState.stage.current / 10) * 10;
        return this.gameState.player.level >= Math.floor(bossStage / 2);
    }

    /**
     * 정리
     */
    destroy() {
        // 정리할 작업 없음
    }
}

export { StageSystem };
