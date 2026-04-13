/**
 * DailyMissionUI - 일일 미션 UI
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameLogger } from '../core/Logger.js';

class DailyMissionUI {
    constructor(gameState, dailyMissionSystem) {
        this.gameState = gameState;
        this.dailyMissionSystem = dailyMissionSystem;
        this.timerInterval = null;
        this.currentMissionTab = 'daily'; // 'daily' or 'weekly'
    }

    /**
     * 초기화
     */
    init() {
        this.setupModal();
        this.setupTabs();
        this.startTimer();
    }

    /**
     * 모달 설정
     */
    setupModal() {
        const modal = document.getElementById('daily-missions-modal');
        const btn = document.getElementById('btn-daily-missions');
        const closeBtn = document.getElementById('btn-close-daily-missions');

        if (btn) {
            btn.addEventListener('click', () => {
                modal.style.display = 'flex';
                this.updateDisplay();
                this.renderMissions();
            });
        }

        if (closeBtn) {
            closeBtn.addEventListener('click', () => {
                modal.style.display = 'none';
            });
        }

        if (modal) {
            modal.addEventListener('click', (e) => {
                if (e.target === modal) {
                    modal.style.display = 'none';
                }
            });
        }

        // 이벤트 리스너 - 일일 미션
        gameEventBus.on(GAME_EVENTS.DAILY_MISSION_COMPLETED, () => {
            if (this.currentMissionTab === 'daily') this.renderMissions();
        });

        gameEventBus.on(GAME_EVENTS.DAILY_MISSION_CLAIMED, () => {
            if (this.currentMissionTab === 'daily') {
                this.renderMissions();
                this.updateDisplay();
            }
        });

        gameEventBus.on(GAME_EVENTS.DAILY_MISSIONS_RESET, () => {
            if (this.currentMissionTab === 'daily') this.renderMissions();
        });

        // 이벤트 리스너 - 주간 미션
        gameEventBus.on(GAME_EVENTS.WEEKLY_MISSION_COMPLETED, () => {
            if (this.currentMissionTab === 'weekly') this.renderMissions();
        });

        gameEventBus.on(GAME_EVENTS.WEEKLY_MISSION_CLAIMED, () => {
            if (this.currentMissionTab === 'weekly') {
                this.renderMissions();
                this.updateDisplay();
            }
        });

        gameEventBus.on(GAME_EVENTS.WEEKLY_MISSIONS_RESET, () => {
            if (this.currentMissionTab === 'weekly') this.renderMissions();
        });
    }

    /**
     * 탭 설정
     */
    setupTabs() {
        document.querySelectorAll('.missions-tabs .tab-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                document.querySelectorAll('.missions-tabs .tab-btn').forEach(b => {
                    b.classList.remove('active');
                });
                btn.classList.add('active');
                this.currentMissionTab = btn.dataset.missionTab;
                this.updateTimer();
                this.renderMissions();
            });
        });
    }

    /**
     * 타이머 시작
     */
    startTimer() {
        this.updateTimer();
        this.timerInterval = setInterval(() => {
            this.updateTimer();
        }, 1000);
    }

    /**
     * 타이머 업데이트
     */
    updateTimer() {
        const timerEl = document.getElementById('missions-reset-timer');
        if (!timerEl) return;

        let ms;
        if (this.currentMissionTab === 'weekly') {
            ms = this.dailyMissionSystem.getTimeUntilWeeklyReset();
        } else {
            ms = this.dailyMissionSystem.getTimeUntilReset();
        }

        const hours = Math.floor(ms / (1000 * 60 * 60));
        const minutes = Math.floor((ms % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((ms % (1000 * 60)) / 1000);

        const tabText = this.currentMissionTab === 'weekly' ? '주간 갱신' : '일일 갱신';
        timerEl.textContent = `${tabText}까지: ${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
    }

    /**
     * 미션 렌더링
     */
    renderMissions() {
        const grid = document.getElementById('missions-grid');
        if (!grid) return;

        grid.innerHTML = '';

        const missions = this.dailyMissionSystem.getMissions();
        const missionList = this.currentMissionTab === 'weekly' ? missions.weekly : missions.daily;

        if (!missionList || missionList.length === 0) {
            grid.innerHTML = '<div style="text-align: center; color: #666; padding: 2rem;">미션이 없습니다.</div>';
            return;
        }

        missionList.forEach(mission => {
            const el = this.createMissionItem(mission);
            grid.appendChild(el);
        });
    }

    /**
     * 미션 아이템 생성
     */
    createMissionItem(mission) {
        const el = document.createElement('div');
        el.className = `mission-item ${mission.completed ? 'completed' : ''} ${mission.claimed ? 'claimed' : ''}`;

        const progressPercent = Math.min((mission.progress / mission.target) * 100, 100);

        el.innerHTML = `
            <div class="mission-header-row">
                <span class="mission-name">${mission.name}</span>
                <span class="mission-progress-text">${mission.progress} / ${mission.target}</span>
            </div>
            <div class="mission-desc">${mission.description}</div>
            <div class="mission-progress-bar">
                <div class="mission-progress-fill" style="width: ${progressPercent}%"></div>
            </div>
            <div class="mission-reward">
                <span class="mission-reward-text">
                    보상: ${mission.reward.statPoints > 0 ? `⭐${mission.reward.statPoints}pt` : ''}
                    ${mission.reward.gems > 0 ? ` 💎${mission.reward.gems}` : ''}
                </span>
                <button class="claim-btn ${mission.claimed ? 'claimed' : ''} ${!mission.completed || mission.claimed ? 'disabled' : ''}"
                     data-mission-id="${mission.id}"
                     ${!mission.completed || mission.claimed ? 'disabled' : ''}>
                    ${mission.claimed ? '완료' : '보상 청구'}
                </button>
            </div>
        `;

        // 버튼 이벤트
        const btn = el.querySelector('.claim-btn');
        if (mission.completed && !mission.claimed) {
            btn.addEventListener('click', () => {
                this.claimReward(mission.id);
            });
        }

        return el;
    }

    /**
     * 보상 청구
     */
    claimReward(missionId) {
        const success = this.dailyMissionSystem.claimReward(missionId);
        if (success) {
            gameLogger.info('Mission reward claimed');
        }
    }

    /**
     * 디스플레이 업데이트
     */
    updateDisplay() {
        const gemsEl = document.getElementById('missions-gems');
        if (gemsEl) {
            gemsEl.textContent = this.gameState.inventory.gems;
        }
    }

    /**
     * 정리
     */
    destroy() {
        if (this.timerInterval) {
            clearInterval(this.timerInterval);
        }
    }
}

export { DailyMissionUI };
