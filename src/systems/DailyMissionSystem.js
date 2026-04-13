/**
 * DailyMissionSystem - 일일 미션 시스템
 * 매일 갱신되는 미션, 보상 (스탯포인트, 보석)
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameLogger } from '../core/Logger.js';

class DailyMissionSystem {
    constructor(gameState) {
        this.gameState = gameState;
        
        // 낮은 보상 미션 풀 (매일 2개 선택됨, 💎2 고정)
        this.easyMissionTemplates = [
            {
                id: 'kill_50',
                type: 'kill',
                name: '몬스터 사냥꾼',
                description: '몬스터 50마리 처치',
                target: 50,
                reward: { statPoints: 1, gems: 2 }
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
        
        // 높은 보상 미션 풀 (매일 1개 선택됨, 💎3 고정)
        this.hardMissionTemplates = [
            {
                id: 'kill_100',
                type: 'kill',
                name: '숙련된 사냥꾼',
                description: '몬스터 100마리 처치',
                target: 100,
                reward: { statPoints: 2, gems: 3 }
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
                id: 'gold_5000',
                type: 'gold',
                name: '골드 러시',
                description: '골드 5000 획득',
                target: 5000,
                reward: { statPoints: 2, gems: 3 }
            }
        ];
        
        // 통합 미션 템플릿 (하위 호환성용)
        this.missionTemplates = [...this.easyMissionTemplates, ...this.hardMissionTemplates];
        
        // 주간 미션 정의 (일일 미션의 5배 난이도, 7~8배 보상)
        // 낮은 보상 주간 미션 풀 (매주 2개 선택됨, 💎16 고정)
        this.easyWeeklyMissionTemplates = [
            {
                id: 'weekly_kill_250',
                type: 'kill',
                name: '주간 사냥꾼',
                description: '몬스터 250마리 처치',
                target: 250,
                reward: { statPoints: 10, gems: 16 }
            },
            {
                id: 'weekly_stage_25',
                type: 'stage',
                name: '주간 탐험가',
                description: '스테이지 25 클리어',
                target: 25,
                reward: { statPoints: 8, gems: 16 }
            },
            {
                id: 'weekly_synthesize_15',
                type: 'synthesize',
                name: '주간 연금술사',
                description: '아이템 15번 합성',
                target: 15,
                reward: { statPoints: 8, gems: 16 }
            },
            {
                id: 'weekly_upgrade_25',
                type: 'upgrade',
                name: '주간 강화 마스터',
                description: '업그레이드 25회 구매',
                target: 25,
                reward: { statPoints: 8, gems: 16 }
            }
        ];
        
        // 높은 보상 주간 미션 풀 (매주 1개 선택됨, 💎24 고정)
        this.hardWeeklyMissionTemplates = [
            {
                id: 'weekly_kill_500',
                type: 'kill',
                name: '주간 학살자',
                description: '몬스터 500마리 처치',
                target: 500,
                reward: { statPoints: 15, gems: 24 }
            },
            {
                id: 'weekly_stage_50',
                type: 'stage',
                name: '주간 원정대장',
                description: '스테이지 50 클리어',
                target: 50,
                reward: { statPoints: 15, gems: 24 }
            },
            {
                id: 'weekly_gold_50000',
                type: 'gold',
                name: '주간 골드 러시',
                description: '골드 50,000 획득',
                target: 50000,
                reward: { statPoints: 15, gems: 24 }
            }
        ];
        
        // 통합 주간 미션 템플릿 (하위 호환성용)
        this.weeklyMissionTemplates = [...this.easyWeeklyMissionTemplates, ...this.hardWeeklyMissionTemplates];
    }

    /**
     * 초기화
     */
    init() {
        this.checkDailyReset();
        this.checkWeeklyReset();
        this.generateDailyMissions();
        this.generateWeeklyMissions();
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
     * 이번 주의 월요일 자정 타임스탬프 반환
     */
    getThisWeekMonday() {
        const now = new Date();
        const dayOfWeek = now.getDay(); // 0=일요일, 1=월요일, ...
        const mondayOffset = dayOfWeek === 0 ? -6 : (1 - dayOfWeek); // 일요일이면 6일 전, 아니면 월요일까지
        const monday = new Date(now.getFullYear(), now.getMonth(), now.getDate() + mondayOffset);
        return monday.getTime();
    }

    /**
     * 다음 주 월요일 자정 타임스탬프 반환
     */
    getNextWeekMonday() {
        return this.getThisWeekMonday() + (7 * 24 * 60 * 60 * 1000);
    }

    /**
     * 주간 초기화 확인 (월요일 0시 기준)
     */
    checkWeeklyReset() {
        const thisWeekMonday = this.getThisWeekMonday();
        const lastReset = this.gameState.dailyMissions.weeklyLastReset;
        
        // 마지막 리셋이 이번 주 월요일 이전이면 리셋
        if (lastReset < thisWeekMonday) {
            this.resetWeeklyMissions();
        }
    }

    /**
     * 주간 미션 초기화 (낮은 보상 2개 + 높은 보상 1개 고정)
     */
    resetWeeklyMissions() {
        // 이번 주 월요일로 설정
        this.gameState.dailyMissions.weeklyLastReset = this.getThisWeekMonday();
        this.gameState.dailyMissions.weeklyMissions = [];
        
        // 낮은 보상 주간 미션 2개 무작위 선택
        const easyShuffled = [...this.easyWeeklyMissionTemplates].sort(() => Math.random() - 0.5);
        const easySelected = easyShuffled.slice(0, 2);
        
        // 높은 보상 주간 미션 1개 무작위 선택
        const hardShuffled = [...this.hardWeeklyMissionTemplates].sort(() => Math.random() - 0.5);
        const hardSelected = hardShuffled.slice(0, 1);
        
        // 합치기
        const selected = [...easySelected, ...hardSelected];
        
        selected.forEach(template => {
            this.gameState.dailyMissions.weeklyMissions.push({
                id: template.id,
                type: template.type,
                name: template.name,
                description: template.description,
                target: template.target,
                progress: 0,
                completed: false,
                claimed: false,
                reward: template.reward,
                isWeekly: true
            });
        });
        
        gameLogger.info('Weekly missions reset (2 easy + 1 hard)');
        gameEventBus.emit(GAME_EVENTS.WEEKLY_MISSIONS_RESET);
    }

    /**
     * 주간 미션 생성 (필요시)
     */
    generateWeeklyMissions() {
        if (!this.gameState.dailyMissions.weeklyMissions || this.gameState.dailyMissions.weeklyMissions.length === 0) {
            this.resetWeeklyMissions();
        }
    }

    /**
     * 미션 초기화 (낮은 보상 2개 + 높은 보상 1개 고정)
     */
    resetDailyMissions() {
        // 현실 시간 기준 오늘 자정으로 설정
        this.gameState.dailyMissions.lastReset = this.getTodayMidnight();
        this.gameState.dailyMissions.missions = [];
        
        // 낮은 보상 미션 2개 무작위 선택
        const easyShuffled = [...this.easyMissionTemplates].sort(() => Math.random() - 0.5);
        const easySelected = easyShuffled.slice(0, 2);
        
        // 높은 보상 미션 1개 무작위 선택
        const hardShuffled = [...this.hardMissionTemplates].sort(() => Math.random() - 0.5);
        const hardSelected = hardShuffled.slice(0, 1);
        
        // 합치기
        const selected = [...easySelected, ...hardSelected];
        
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
        
        gameLogger.info('Daily missions reset (2 easy + 1 hard)');
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
     * 미션 진행도 업데이트 (일일 + 주간)
     */
    updateProgress(type, amount) {
        // 일일 미션 업데이트
        const dailyMissions = this.gameState.dailyMissions.missions;
        dailyMissions.forEach(mission => {
            this.updateMissionProgress(mission, type, amount);
        });
        
        // 주간 미션 업데이트
        const weeklyMissions = this.gameState.dailyMissions.weeklyMissions || [];
        weeklyMissions.forEach(mission => {
            this.updateMissionProgress(mission, type, amount);
        });
    }

    /**
     * 개별 미션 진행도 업데이트
     */
    updateMissionProgress(mission, type, amount) {
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
                const eventType = mission.isWeekly ? GAME_EVENTS.WEEKLY_MISSION_COMPLETED : GAME_EVENTS.DAILY_MISSION_COMPLETED;
                gameEventBus.emit(eventType, { mission });
            }
        }
    }

    /**
     * 미션 보상 청구 (일일 또는 주간)
     */
    claimReward(missionId) {
        // 일일 미션에서 찾기
        let mission = this.gameState.dailyMissions.missions.find(m => m.id === missionId);
        let isWeekly = false;
        
        // 없으면 주간 미션에서 찾기
        if (!mission) {
            mission = (this.gameState.dailyMissions.weeklyMissions || []).find(m => m.id === missionId);
            isWeekly = !!mission;
        }
        
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
        
        const claimEvent = isWeekly ? GAME_EVENTS.WEEKLY_MISSION_CLAIMED : GAME_EVENTS.DAILY_MISSION_CLAIMED;
        gameEventBus.emit(claimEvent, { mission });
        
        gameLogger.info(`Claimed ${isWeekly ? 'weekly' : 'daily'} mission reward: ${mission.name}`);
        
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
     * 미션 목록 반환 (일일 + 주간)
     */
    getMissions() {
        return {
            daily: this.gameState.dailyMissions.missions,
            weekly: this.gameState.dailyMissions.weeklyMissions || []
        };
    }

    /**
     * 남은 주간 미션 시간 확인 (다음 주 월요일까지)
     */
    getTimeUntilWeeklyReset() {
        const now = Date.now();
        const nextMonday = this.getNextWeekMonday();
        
        return Math.max(0, nextMonday - now);
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
