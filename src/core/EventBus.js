/**
 * EventBus - 게임 전체 이벤트 시스템
 * Observer 패턴 구현, 느슨한 결합을 위한 핵심 인프라
 */
class EventBus {
    constructor() {
        this.events = new Map();
    }

    /**
     * 이벤트 리스너 등록
     * @param {string} event - 이벤트 이름
     * @param {Function} callback - 콜백 함수
     * @param {Object} context - this 바인딩용 컨텍스트
     */
    on(event, callback, context = null) {
        if (!this.events.has(event)) {
            this.events.set(event, []);
        }
        this.events.get(event).push({ callback, context });
        return this; // 체이닝 지원
    }

    /**
     * 일회성 이벤트 리스너 등록
     * @param {string} event - 이벤트 이름
     * @param {Function} callback - 콜백 함수
     * @param {Object} context - this 바인딩용 컨텍스트
     */
    once(event, callback, context = null) {
        const wrapper = (...args) => {
            this.off(event, wrapper);
            callback.apply(context, args);
        };
        return this.on(event, wrapper, context);
    }

    /**
     * 이벤트 리스너 제거
     * @param {string} event - 이벤트 이름
     * @param {Function} callback - 제거할 콜백
     */
    off(event, callback) {
        if (!this.events.has(event)) return this;
        
        if (!callback) {
            // 모든 리스너 제거
            this.events.delete(event);
        } else {
            // 특정 리스너 제거
            const listeners = this.events.get(event);
            const filtered = listeners.filter(l => l.callback !== callback);
            if (filtered.length === 0) {
                this.events.delete(event);
            } else {
                this.events.set(event, filtered);
            }
        }
        return this;
    }

    /**
     * 이벤트 발생
     * @param {string} event - 이벤트 이름
     * @param {*} data - 이벤트 데이터
     */
    emit(event, data = null) {
        if (!this.events.has(event)) return this;
        
        const listeners = this.events.get(event);
        // 복사본을 만들어 순회 (중간에 제거되어도 안전)
        [...listeners].forEach(({ callback, context }) => {
            try {
                callback.apply(context, data !== null ? [data] : []);
            } catch (error) {
                console.error(`[EventBus] Error in event "${event}":`, error);
            }
        });
        return this;
    }

    /**
     * 특정 이벤트의 리스너 수 반환
     * @param {string} event - 이벤트 이름
     * @returns {number} 리스너 수
     */
    listenerCount(event) {
        if (!this.events.has(event)) return 0;
        return this.events.get(event).length;
    }

    /**
     * 모든 이벤트 리스너 제거 (메모리 누수 방지)
     */
    removeAllListeners() {
        this.events.clear();
        return this;
    }

    /**
     * 등록된 모든 이벤트 이름 반환
     * @returns {string[]} 이벤트 이름 배열
     */
    eventNames() {
        return Array.from(this.events.keys());
    }
}

// 싱글톤 인스턴스
const gameEventBus = new EventBus();

// 게임 이벤트 상수
const GAME_EVENTS = {
    // 플레이어
    PLAYER_LEVELUP: 'player:levelup',
    PLAYER_STAT_CHANGED: 'player:stat_changed',
    PLAYER_HP_CHANGED: 'player:hp_changed',
    PLAYER_EXP_CHANGED: 'player:exp_changed',
    
    // 전투
    COMBAT_ATTACK: 'combat:attack',
    COMBAT_DAMAGE: 'combat:damage',
    COMBAT_MONSTER_KILLED: 'combat:monster_killed',
    COMBAT_PLAYER_DIED: 'combat:player_died',
    COMBAT_LOG: 'combat:log',
    COMBAT_PHASE_CHANGED: 'combat:phase_changed',
    COMBAT_ENCOUNTER: 'combat:encounter',
    COMBAT_VICTORY: 'combat:victory',
    
    // 스테이지
    STAGE_CHANGED: 'stage:changed',
    STAGE_BOSS_ENTER: 'stage:boss_enter',
    STAGE_BOSS_DEFEATED: 'stage:boss_defeated',
    STAGE_AUTO_REPEAT: 'stage:auto_repeat',
    
    // 인벤토리
    INVENTORY_ITEM_ADDED: 'inventory:item_added',
    INVENTORY_ITEM_REMOVED: 'inventory:item_removed',
    INVENTORY_GOLD_CHANGED: 'inventory:gold_changed',
    INVENTORY_SYNTHESIZE: 'inventory:synthesize',
    
    // 튜토리얼
    TUTORIAL_STEP: 'tutorial:step',
    TUTORIAL_COMPLETED: 'tutorial:completed',
    
    // 설정
    SETTINGS_CHANGED: 'settings:changed',
    
    // 게임 상태
    GAME_LOADED: 'game:loaded',
    GAME_SAVED: 'game:saved',
    GAME_PAUSED: 'game:paused',
    GAME_RESUMED: 'game:resumed',
    
    // 오프라인
    OFFLINE_REWARD: 'offline:reward',

    // 업그레이드
    UPGRADE_PURCHASED: 'upgrade:purchased',
    UPGRADE_INSUFFICIENT_GOLD: 'upgrade:insufficient_gold',
    UPGRADE_INSUFFICIENT_POINTS: 'upgrade:insufficient_points',
    UPGRADE_MAX_LEVEL: 'upgrade:max_level',

    // 일일 미션
    DAILY_MISSIONS_RESET: 'daily:missions_reset',
    DAILY_MISSION_COMPLETED: 'daily:mission_completed',
    DAILY_MISSION_CLAIMED: 'daily:mission_claimed',
    BUFF_ACTIVATED: 'buff:activated',

    // 환생
    REBIRTH_PERFORMED: 'rebirth:performed',
    REBIRTH_UPGRADE_PURCHASED: 'rebirth:upgrade_purchased'
};

export { EventBus, gameEventBus, GAME_EVENTS };
