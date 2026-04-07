/**
 * AchievementSystem - 업적 시스템
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameDataLoader } from '../data-parser/DataLoader.js';
import { gameLogger } from '../core/Logger.js';

class AchievementSystem {
    /**
     * @param {GameState} gameState 
     */
    constructor(gameState) {
        this.gameState = gameState;
        this.achievementsData = [];
    }

    /**
     * 초기화
     */
    init() {
        // 업적 데이터 로드
        this.achievementsData = gameDataLoader.get('achievements') || [];
        
        // 이벤트 리스너 등록
        this.setupEventListeners();
        
        // 이미 달성한 업적 확인
        this.checkAllAchievements();
        
        gameLogger.debug('AchievementSystem initialized');
    }

    /**
     * 이벤트 리스너 설정
     */
    setupEventListeners() {
        // 몬스터 처치
        gameEventBus.on(GAME_EVENTS.COMBAT_MONSTER_KILLED, (data) => {
            this.checkCondition('kill_count', this.gameState.stats.totalKills);
        });
        
        // 레벨업
        gameEventBus.on(GAME_EVENTS.PLAYER_LEVELUP, (data) => {
            this.checkCondition('level', data.level);
        });
        
        // 스테이지 도달
        gameEventBus.on(GAME_EVENTS.STAGE_CHANGED, (data) => {
            this.checkCondition('stage_reach', data.stage);
        });
        
        // 골드 획득
        gameEventBus.on(GAME_EVENTS.INVENTORY_GOLD_CHANGED, (data) => {
            this.checkCondition('gold_total', data.gold);
        });
        
        // 합성
        gameEventBus.on(GAME_EVENTS.INVENTORY_SYNTHESIZE, () => {
            const count = this.gameState.achievements
                .filter(a => a.id.startsWith('synthesize'))
                .length;
            this.checkCondition('synthesize_count', count + 1);
        });
    }

    /**
     * 조건 확인
     * @param {string} conditionType 
     * @param {number} currentValue 
     */
    checkCondition(conditionType, currentValue) {
        this.achievementsData.forEach(achievement => {
            // 이미 해제된 업적 스킵
            if (this.isUnlocked(achievement.id)) return;
            
            // 조건 타입 확인
            if (achievement.condition_type !== conditionType) return;
            
            // 조건 값 확인
            const requiredValue = parseInt(achievement.condition_value);
            if (currentValue >= requiredValue) {
                this.unlock(achievement);
            }
        });
    }

    /**
     * 모든 업적 확인 (로드 시)
     */
    checkAllAchievements() {
        this.achievementsData.forEach(achievement => {
            if (this.isUnlocked(achievement.id)) return;
            
            let currentValue = 0;
            
            switch (achievement.condition_type) {
                case 'kill_count':
                    currentValue = this.gameState.stats.totalKills;
                    break;
                case 'level':
                    currentValue = this.gameState.player.level;
                    break;
                case 'stage_reach':
                    currentValue = this.gameState.stage.max;
                    break;
                case 'gold_total':
                    currentValue = this.gameState.inventory.gold;
                    break;
            }
            
            const requiredValue = parseInt(achievement.condition_value);
            if (currentValue >= requiredValue) {
                this.unlock(achievement);
            }
        });
    }

    /**
     * 업적 해제
     * @param {Object} achievement 
     */
    unlock(achievement) {
        // 업적 기록
        this.gameState.achievements.push({
            id: achievement.id,
            unlockedAt: Date.now()
        });
        
        // 보상 지급
        this.giveReward(achievement.reward_type, achievement.reward_value);
        
        // 이벤트
        gameEventBus.emit(GAME_EVENTS.ACHIEVEMENT_UNLOCKED, {
            id: achievement.id,
            name: achievement.name,
            reward_type: achievement.reward_type,
            reward_value: achievement.reward_value
        });
        
        gameEventBus.emit(GAME_EVENTS.COMBAT_LOG, {
            message: `업적 달성: ${achievement.name}!`
        });
        
        gameLogger.info(`Achievement unlocked: ${achievement.id}`);
    }

    /**
     * 보상 지급
     * @param {string} type 
     * @param {number} value 
     */
    giveReward(type, value) {
        const amount = parseInt(value);
        
        switch (type) {
            case 'gold':
                this.gameState.addGold(amount);
                break;
            case 'exp':
                this.gameState.addExp(amount);
                break;
            case 'item':
                // TODO: 아이템 지급
                break;
        }
    }

    /**
     * 업적 해제 여부 확인
     * @param {string} id 
     * @returns {boolean}
     */
    isUnlocked(id) {
        return this.gameState.achievements.some(a => a.id === id);
    }

    /**
     * 해제된 업적 목록 반환
     * @returns {Array}
     */
    getUnlockedAchievements() {
        return this.achievementsData
            .filter(a => this.isUnlocked(a.id))
            .map(a => ({
                ...a,
                unlocked: true
            }));
    }

    /**
     * 잠긴 업적 목록 반환
     * @returns {Array}
     */
    getLockedAchievements() {
        return this.achievementsData
            .filter(a => !this.isUnlocked(a.id))
            .map(a => ({
                ...a,
                unlocked: false
            }));
    }

    /**
     * 업적 진행률 반환
     * @returns {{unlocked: number, total: number, percent: number}}
     */
    getProgress() {
        const total = this.achievementsData.length;
        const unlocked = this.gameState.achievements.length;
        
        return {
            unlocked,
            total,
            percent: total > 0 ? (unlocked / total) * 100 : 0
        };
    }

    /**
     * 정리
     */
    destroy() {
        // 이벤트 리스너 제거는 EventBus 에 위임
    }
}

export { AchievementSystem };
