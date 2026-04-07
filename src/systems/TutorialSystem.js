/**
 * TutorialSystem - 튜토리얼 가이드 시스템
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameDataLoader } from '../data-parser/DataLoader.js';
import { gameLogger } from '../core/Logger.js';

class TutorialSystem {
    /**
     * @param {GameState} gameState 
     */
    constructor(gameState) {
        this.gameState = gameState;
        this.currentStep = 0;
        this.tutorialData = [];
    }

    /**
     * 초기화
     */
    init() {
        // 튜토리얼 데이터 로드
        this.tutorialData = gameDataLoader.get('tutorial') || [];
        
        // 저장된 튜토리얼 상태 복원
        this.currentStep = this.gameState.tutorial.step;
        
        // 이벤트 리스너 등록
        this.setupEventListeners();
        
        gameLogger.debug('TutorialSystem initialized');
    }

    /**
     * 이벤트 리스너 설정
     */
    setupEventListeners() {
        // 몬스터 처치 이벤트
        gameEventBus.on(GAME_EVENTS.COMBAT_MONSTER_KILLED, () => {
            this.checkCondition('kill_count');
        });
        
        // 레벨업 이벤트
        gameEventBus.on(GAME_EVENTS.PLAYER_LEVELUP, () => {
            this.checkCondition('level');
        });
        
        // 합성 이벤트
        gameEventBus.on(GAME_EVENTS.INVENTORY_SYNTHESIZE, () => {
            this.checkCondition('synthesize');
        });
        
        // 보스 처치 이벤트
        gameEventBus.on(GAME_EVENTS.STAGE_BOSS_DEFEATED, () => {
            this.checkCondition('boss_defeat');
        });
    }

    /**
     * 튜토리얼 시작
     */
    start() {
        if (this.gameState.tutorial.completed) {
            gameLogger.debug('Tutorial already completed');
            return;
        }
        
        if (this.currentStep === 0) {
            this.advanceToStep(1);
        }
    }

    /**
     * 조건 확인
     * @param {string} conditionType 
     */
    checkCondition(conditionType) {
        if (this.gameState.tutorial.completed) return;
        
        const currentTutorial = this.tutorialData.find(t => t.step === this.currentStep);
        if (!currentTutorial) return;
        
        if (currentTutorial.condition_type === conditionType) {
            const conditionValue = parseInt(currentTutorial.condition_value);
            let currentValue = 0;
            
            // 현재 값 조회
            switch (conditionType) {
                case 'kill_count':
                    currentValue = this.gameState.stage.kills;
                    break;
                case 'level':
                    currentValue = this.gameState.player.level;
                    break;
                case 'boss_defeat':
                    currentValue = 1; // 보스 처치 시 1 로 간주
                    break;
            }
            
            // 조건 달성 확인
            if (currentValue >= conditionValue) {
                this.advanceToStep(this.currentStep + 1);
            }
        }
    }

    /**
     * 다음 단계로 진행
     * @param {number} step 
     */
    advanceToStep(step) {
        const tutorial = this.tutorialData.find(t => t.step === step);
        
        if (!tutorial) {
            // 튜토리얼 완료
            this.complete();
            return;
        }
        
        this.currentStep = step;
        this.gameState.tutorial.step = step;
        
        // 보상 지급
        if (tutorial.reward) {
            this.giveReward(tutorial.reward);
        }
        
        // UI 에 가이드 표시
        gameEventBus.emit(GAME_EVENTS.TUTORIAL_STEP, {
            step,
            message: tutorial.guide_message,
            reward: tutorial.reward
        });
        
        gameLogger.info(`Tutorial step ${step}: ${tutorial.guide_message}`);
    }

    /**
     * 보상 지급
     * @param {string} rewardStr - "gold:100" 형식
     */
    giveReward(rewardStr) {
        if (!rewardStr) return;
        
        const [type, value] = rewardStr.split(':');
        const amount = parseInt(value);
        
        switch (type) {
            case 'gold':
                this.gameState.addGold(amount);
                break;
            case 'exp':
                this.gameState.addExp(amount);
                break;
            case 'sp':
                this.gameState.player.statPoints += amount;
                break;
            case 'item':
                // TODO: 아이템 지급
                break;
        }
        
        gameLogger.debug(`Tutorial reward: ${type} ${amount}`);
    }

    /**
     * 튜토리얼 완료
     */
    complete() {
        this.gameState.tutorial.completed = true;
        this.gameState.tutorial.step = 99;
        
        gameEventBus.emit(GAME_EVENTS.TUTORIAL_COMPLETED);
        
        gameEventBus.emit(GAME_EVENTS.COMBAT_LOG, {
            message: '튜토리얼이 완료되었습니다!'
        });
        
        gameLogger.info('Tutorial completed');
    }

    /**
     * 현재 가이드 메시지 반환
     * @returns {string}
     */
    getCurrentGuide() {
        const tutorial = this.tutorialData.find(t => t.step === this.currentStep);
        return tutorial ? tutorial.guide_message : '';
    }

    /**
     * 튜토리얼 스킵
     */
    skip() {
        this.complete();
        gameLogger.info('Tutorial skipped');
    }

    /**
     * 정리
     */
    destroy() {
        // 이벤트 리스너 제거는 EventBus 에 위임
    }
}

export { TutorialSystem };
