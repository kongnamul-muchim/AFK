/**
 * DailyMissionSystem - 일일 미션 시스템
 * 매일 갱신되는 미션, 보상 (스탯포인트, 보석)
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameLogger } from '../core/Logger.js';

class DailyMissionSystem {
    constructor(gameState) {
        this.gameState = gameState;
        
        // 미션 정의 (5분 플레이 시 완료 가능한 수준)
        this.missionTemplates = [
            {
                id: 'kill_50',
                type: 'kill',
                name: '몬스터 사냥꾼',
                description: '몬스터 50마리 처치',
                target: 50,
                reward: { statPoints: 1, gems: 2 }
            },
            {
                id: 'kill_100',
                type: 'kill',
                name: '숙련된 사냥꾼',
                description: '몬스터 100마리 처치',
                target: 100,
                reward: { statPoints: 2, gems: 3 }
            },
            {
                id: 'stage_5',
                type: 'stage',
                name: '탐험가',
                description: '스테이지 5 클리어',
                target: 5,
                reward: { statPoints: 1, gems: 2 }
            },
            {
                id: 'stage_10',
                type: 'stage',
                name: '원정대장',
                description: '스테이지 10 클리어',
                target: 10,
                reward: { statPoints: 2, gems: 3 }
            },
            {
                id: 'gold_1000',
                type: 'gold',
                name: '골드 러시',
                description: '골드 1000 획득',
                target: 1000,
                reward: { statPoints: 1, gems: 1 }
            },
            {
                id: 'gold_5000',
                type: 'gold',
                name: '부자 되기',
                description: '골드 5000 획득',
                target: 5000,
                reward: { statPoints: 2, gems: 3 }
            },
            {
                id: 'synthesize_3',
                type: 'synthesize',
                name: '연금술사',
                description: '아이템 3번 합성',
                target: 3,
                reward: { statPoints: 1, gems: 2 }
            },
            {
                id: 'upgrade_5',
                type: 'upgrade',
                name: '강화 마스터',
                description: '업그레이드 5회 구매',
                target: 5,
                reward: { statPoints: 1, gems: 2 }
            }
        ];
    }

    /**
     * 초기화
     */
    init() {
        this.checkDailyReset();
        this.generateDailyMissions();
        this.setupEventListeners();
    }

    /**
     * 오늘 자정(00:00) 타임스탬프 반환
     */
    getTodayMidnight() {
        const now = new Date();
        return new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime();
    }

    /**
     * 내일 자정(00:00) 타임스탬프 반환
     */
    getTomorrowMidnight() {
        const now = new Date();
        return new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1).getTime();
    }

    /**
     * 일일 초기화 확인 (현실 시간 0시 기준)
     */
    checkDailyReset() {
        const todayMidnight = this.getTodayMidnight();
        const lastReset = this.gameState.dailyMissions.lastReset;
        
        // 마지막 리셋이 오늘 자정 이전이면 리셋
        if (lastReset < todayMidnight) {
            this.resetDailyMissions();
        }
    }

    /**
     * 미션 초기화
     */
    resetDailyMissions() {
        // 현실 시간 기준 오늘 자정으로 설정
        this.gameState.dailyMissions.lastReset = this.getTodayMidnight();
        this.gameState.dailyMissions.missions = [];
        
        // 3개 미션 무작위 선택
        const shuffled = [...this.missionTemplates].sort(() => Math.random() - 0.5);
        const selected = shuffled.slice(0, 3);
        
        selected.forEach(template => {
            this.gameState.dailyMissions.missions.push({
                id: template.id,
                type: template.type,
                name: template.name,
                description: template.description,
                target: template.target,
                progress: 0,
                completed: false,
                claimed: false,
                reward: template.reward
            });
        });
        
        gameLogger.info('Daily missions reset');
        gameEventBus.emit(GAME_EVENTS.DAILY_MISSIONS_RESET);
    }

    /**
     * 미션 생성 (필요시)
     */
    generateDailyMissions() {
        if (this.gameState.dailyMissions.missions.length === 0) {
            this.resetDailyMissions();
        }
    }

    /**
     * 이벤트 리스너 설정
     */
    setupEventListeners() {
        gameEventBus.on(GAME_EVENTS.COMBAT_MONSTER_KILLED, () => {
            this.updateProgress('kill', 1);
        });
        
        gameEventBus.on(GAME_EVENTS.STAGE_CHANGED, (data) => {
            this.updateProgress('stage', 1);
        });
        
        gameEventBus.on(GAME_EVENTS.INVENTORY_GOLD_CHANGED, () => {
            // 골드는 현재 골드량으로 체크
        });
        
        gameEventBus.on(GAME_EVENTS.INVENTORY_SYNTHESIZE, () => {
            this.updateProgress('synthesize', 1);
        });
        
        gameEventBus.on(GAME_EVENTS.UPGRADE_PURCHASED, () => {
            this.updateProgress('upgrade', 1);
        });
    }

    /**
     * 미션 진행도 업데이트
     */
    updateProgress(type, amount) {
        const missions = this.gameState.dailyMissions.missions;
        
        missions.forEach(mission => {
            if (mission.type === type && !mission.completed) {
                // 골드는 특별 처리 (현재 골드량 확인)
                if (type === 'gold') {
                    mission.progress = this.gameState.inventory.gold;
                } else {
                    mission.progress = Math.min(mission.progress + amount, mission.target);
                }
                
                // 완료 확인
                if (mission.progress >= mission.target) {
                    mission.completed = true;
                    gameEventBus.emit(GAME_EVENTS.DAILY_MISSION_COMPLETED, { mission });
                }
            }
        });
    }

    /**
     * 미션 보상 청구
     */
    claimReward(missionId) {
        const mission = this.gameState.dailyMissions.missions.find(m => m.id === missionId);
        
        if (!mission || !mission.completed || mission.claimed) {
            return false;
        }
        
        // 보상 지급
        if (mission.reward.statPoints > 0) {
            this.gameState.player.statPoints += mission.reward.statPoints;
        }
        if (mission.reward.gems > 0) {
            this.gameState.inventory.gems += mission.reward.gems;
        }
        
        mission.claimed = true;
        
        gameEventBus.emit(GAME_EVENTS.DAILY_MISSION_CLAIMED, { mission });
        
        gameLogger.info(`Claimed mission reward: ${mission.name}`);
        
        return true;
    }

    /**
     * 현재 활성화된 버프 확인
     */
    hasActiveBuff(buffType) {
        const buffTime = this.gameState.dailyMissions.buffs[buffType];
        return buffTime > Date.now();
    }

    /**
     * 버프 적용
     */
    activateBuff(buffType, durationMinutes) {
        const durationMs = durationMinutes * 60 * 1000;
        this.gameState.dailyMissions.buffs[buffType] = Date.now() + durationMs;
        
        gameLogger.info(`Buff activated: ${buffType} for ${durationMinutes} minutes`);
        gameEventBus.emit(GAME_EVENTS.BUFF_ACTIVATED, { buffType, duration: durationMinutes });
    }

    /**
     * 버프 효과 적용 (전투 시스템에서 사용)
     */
    getBuffMultiplier(buffType) {
        if (this.hasActiveBuff(buffType)) {
            return 2.0; // 2배
        }
        return 1.0;
    }

    /**
     * 남은 미션 시간 확인 (다음 자정까지)
     */
    getTimeUntilReset() {
        const now = Date.now();
        const tomorrowMidnight = this.getTomorrowMidnight();
        
        return Math.max(0, tomorrowMidnight - now);
    }

    /**
     * 미션 목록 반환
     */
    getMissions() {
        return this.gameState.dailyMissions.missions;
    }

    /**
     * 보석 수 반환
     */
    getGems() {
        return this.gameState.inventory.gems;
    }

    /**
     * [테스트용] 모든 미션 강제 완료
     */
    completeAllMissions() {
        const missions = this.gameState.dailyMissions.missions;
        
        missions.forEach(mission => {
            if (!mission.completed) {
                mission.progress = mission.target;
                mission.completed = true;
                gameEventBus.emit(GAME_EVENTS.DAILY_MISSION_COMPLETED, { mission });
            }
        });
        
        gameLogger.info('[TEST] All missions completed');
    }

    /**
     * [테스트용] 보석 추가
     */
    addGems(amount) {
        this.gameState.inventory.gems += amount;
        gameLogger.info(`[TEST] Added ${amount} gems`);
    }

    /**
     * [테스트용] 미션 초기화
     */
    forceReset() {
        this.resetDailyMissions();
    }

    /**
     * [테스트용] 모든 버프 활성화 (30분)
     */
    activateAllBuffs() {
        this.activateBuff('attackDouble', 30);
        this.activateBuff('hpDouble', 30);
        this.activateBuff('goldDouble', 30);
        this.activateBuff('expDouble', 30);
        gameLogger.info('[TEST] All buffs activated for 30 minutes');
    }

    /**
     * [테스트용] 현재 버프 상태 출력
     */
    printBuffStatus() {
        const buffs = ['attackDouble', 'hpDouble', 'goldDouble', 'expDouble'];
        const names = {
            attackDouble: '공격력 2배',
            hpDouble: '체력 2배',
            goldDouble: '골드 2배',
            expDouble: '경험치 2배'
        };
        
        console.log('=== Buff Status ===');
        buffs.forEach(type => {
            const active = this.hasActiveBuff(type);
            if (active) {
                const remaining = Math.floor((this.gameState.dailyMissions.buffs[type] - Date.now()) / 1000);
                const min = Math.floor(remaining / 60);
                const sec = remaining % 60;
                console.log(`${names[type]}: 활성 (${min}분 ${sec}초 남음)`);
            } else {
                console.log(`${names[type]}: 비활성`);
            }
        });
        console.log('===================');
    }
}

export { DailyMissionSystem };
