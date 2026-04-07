/**
 * UIManager - 게임 UI 관리
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameLogger } from '../core/Logger.js';

class UIManager {
    constructor(gameState) {
        this.gameState = gameState;
        
        // HUD Elements
        this.hudLevel = document.getElementById('hud-level');
        this.hudHpFill = document.getElementById('hud-hp-fill');
        this.hudHpText = document.getElementById('hud-hp-text');
        this.hudStage = document.getElementById('hud-stage');
        this.hudGold = document.getElementById('hud-gold');
        this.hudExpFill = document.getElementById('hud-exp-fill');
        this.hudExpText = document.getElementById('hud-exp-text');
        this.hudStatPoints = document.getElementById('hud-stat-points');
        
        // Combat log
        this.combatLog = document.getElementById('combat-log');
        
        // Modals
        this.inventoryModal = document.getElementById('inventory-modal');
        this.settingsModal = document.getElementById('settings-modal');
        this.statsModal = document.getElementById('stats-modal');
        this.offlineRewardModal = document.getElementById('offline-reward-modal');
        this.tutorialOverlay = document.getElementById('tutorial-overlay');
        
        // Settings elements
        this.sfxVolume = document.getElementById('sfx-volume');
        this.bgmVolume = document.getElementById('bgm-volume');
        this.vibration = document.getElementById('vibration');
        this.notifications = document.getElementById('notifications');
        this.fileImport = document.getElementById('file-import');
        
        // Stats elements
        this.statPlusButtons = document.querySelectorAll('.stat-plus');
    }

    /**
     * UI 초기화
     */
    init() {
        this.setupMenuButtons();
        this.setupModalCloseButtons();
        this.setupSettingsHandlers();
        this.setupStatAllocation();
        this.setupDataManagement();
        this.updateHUD();
        this.updateStatsPanel();
        gameLogger.debug('UIManager initialized');
    }

    /**
     * 메뉴 버튼 설정
     */
    setupMenuButtons() {
        document.getElementById('btn-inventory')?.addEventListener('click', () => {
            this.showModal(this.inventoryModal);
            this.renderInventory();
        });
        
        document.getElementById('btn-stats')?.addEventListener('click', () => {
            this.showModal(this.statsModal);
        });
        
        document.getElementById('btn-settings')?.addEventListener('click', () => {
            this.showModal(this.settingsModal);
        });
    }

    /**
     * 모달 닫기 버튼 설정
     */
    setupModalCloseButtons() {
        document.getElementById('btn-close-inventory')?.addEventListener('click', () => {
            this.hideModal(this.inventoryModal);
        });
        
        document.getElementById('btn-close-settings')?.addEventListener('click', () => {
            this.hideModal(this.settingsModal);
        });
        
        document.getElementById('btn-close-stats')?.addEventListener('click', () => {
            this.hideModal(this.statsModal);
        });
    }

    /**
     * 설정 핸들러 설정
     */
    setupSettingsHandlers() {
        // SFX Volume
        this.sfxVolume?.addEventListener('input', (e) => {
            const value = e.target.value / 100;
            document.getElementById('sfx-value').textContent = `${e.target.value}%`;
            gameEventBus.emit(GAME_EVENTS.SETTINGS_CHANGED, { type: 'sfxVolume', value });
        });
        
        // BGM Volume
        this.bgmVolume?.addEventListener('input', (e) => {
            const value = e.target.value / 100;
            document.getElementById('bgm-value').textContent = `${e.target.value}%`;
            gameEventBus.emit(GAME_EVENTS.SETTINGS_CHANGED, { type: 'musicVolume', value });
        });
        
        // Vibration
        this.vibration?.addEventListener('change', (e) => {
            gameEventBus.emit(GAME_EVENTS.SETTINGS_CHANGED, { type: 'vibration', value: e.target.checked });
        });
        
        // Notifications
        this.notifications?.addEventListener('change', (e) => {
            gameEventBus.emit(GAME_EVENTS.SETTINGS_CHANGED, { type: 'notifications', value: e.target.checked });
        });
    }

    /**
     * 스탯 분배 설정
     */
    setupStatAllocation() {
        this.statPlusButtons.forEach(btn => {
            btn.addEventListener('click', () => {
                const statType = btn.dataset.stat;
                this.gameState.increaseStat(statType);
            });
        });
    }

    /**
     * 데이터 관리 설정
     */
    setupDataManagement() {
        // 내보내기
        document.getElementById('btn-export-data')?.addEventListener('click', () => {
            // TODO: Implement with StorageManager
            gameLogger.info('Export data clicked');
        });
        
        // 가져오기
        document.getElementById('btn-import-data')?.addEventListener('click', () => {
            this.fileImport?.click();
        });
        
        this.fileImport?.addEventListener('change', (e) => {
            const file = e.target.files[0];
            if (file) {
                // TODO: Implement import
                gameLogger.info('Import file:', file.name);
            }
        });
        
        // 초기화
        document.getElementById('btn-reset-data')?.addEventListener('click', () => {
            if (confirm('정말로 모든 데이터를 초기화하시겠습니까?')) {
                localStorage.clear();
                location.reload();
            }
        });
    }

    /**
     * 모달 표시
     * @param {HTMLElement} modal 
     */
    showModal(modal) {
        if (modal) {
            modal.style.display = 'flex';
        }
    }

    /**
     * 모달 숨김
     * @param {HTMLElement} modal 
     */
    hideModal(modal) {
        if (modal) {
            modal.style.display = 'none';
        }
    }

    /**
     * HUD 업데이트
     */
    updateHUD() {
        const player = this.gameState.player;
        const stage = this.gameState.stage;
        const inventory = this.gameState.inventory;
        
        if (this.hudLevel) {
            this.hudLevel.textContent = `Lv.${player.level}`;
        }
        
        if (this.hudHpFill) {
            const hpPercent = (player.currentHp / player.maxHp) * 100;
            this.hudHpFill.style.width = `${hpPercent}%`;
        }
        
        if (this.hudHpText) {
            this.hudHpText.textContent = `${player.currentHp}/${player.maxHp}`;
        }
        
        if (this.hudStage) {
            this.hudStage.textContent = `Stage ${stage.current}`;
        }
        
        if (this.hudGold) {
            this.hudGold.textContent = this.formatNumber(inventory.gold);
        }
        
        if (this.hudExpFill) {
            const expPercent = (player.exp / player.maxExp) * 100;
            this.hudExpFill.style.width = `${expPercent}%`;
        }
        
        if (this.hudExpText) {
            this.hudExpText.textContent = `Exp: ${player.exp}/${player.maxExp}`;
        }
        
        if (this.hudStatPoints) {
            this.hudStatPoints.textContent = `SP: ${player.statPoints}`;
            this.hudStatPoints.style.color = player.statPoints > 0 ? '#4a9eff' : '#b0b0c0';
        }
        
        // 스탯 분배 버튼 활성화/비활성화
        this.statPlusButtons.forEach(btn => {
            if (btn) {
                btn.disabled = player.statPoints <= 0;
            }
        });
    }

    /**
     * 스탯 패널 업데이트
     */
    updateStatsPanel() {
        const player = this.gameState.player;
        
        document.getElementById('stats-level').textContent = player.level;
        document.getElementById('stats-exp').textContent = `${player.exp}/${player.maxExp}`;
        document.getElementById('stats-points').textContent = player.statPoints;
        document.getElementById('stat-str').textContent = player.stats.str;
        document.getElementById('stat-agi').textContent = player.stats.agi;
        document.getElementById('stat-int').textContent = player.stats.int;
        document.getElementById('stat-vit').textContent = player.stats.vit;
    }

    /**
     * 인벤토리 렌더링
     */
    renderInventory() {
        const container = document.getElementById('inventory-items');
        const goldDisplay = document.getElementById('inventory-gold');
        
        if (!container) return;
        
        container.innerHTML = '';
        
        // 아이템 렌더링
        this.gameState.inventory.items.forEach((itemData, itemId) => {
            const slot = document.createElement('div');
            slot.className = `item-slot ${itemData.rarity || 'common'}`;
            slot.innerHTML = `
                <span class="item-count">x${itemData.count}</span>
                <span class="item-name">${itemData.name}</span>
            `;
            container.appendChild(slot);
        });
        
        if (goldDisplay) {
            goldDisplay.textContent = this.formatNumber(this.gameState.inventory.gold);
        }
    }

    /**
     * 오프라인 보상 표시
     * @param {number} hours 
     * @param {number} exp 
     * @param {number} gold 
     */
    showOfflineReward(hours, exp, gold) {
        document.getElementById('offline-duration').textContent = `${hours.toFixed(1)}시간`;
        document.getElementById('offline-exp').textContent = this.formatNumber(exp);
        document.getElementById('offline-gold').textContent = this.formatNumber(gold);
        
        document.getElementById('btn-claim-reward')?.addEventListener('click', () => {
            this.hideModal(this.offlineRewardModal);
        });
        
        this.showModal(this.offlineRewardModal);
    }

    /**
     * 전투 로그 추가
     * @param {string} message 
     */
    addCombatLog(message) {
        if (!this.combatLog) return;
        
        const entry = document.createElement('div');
        entry.className = 'combat-log-entry';
        entry.textContent = message;
        
        this.combatLog.insertBefore(entry, this.combatLog.firstChild);
        
        // 오래된 로그 제거
        while (this.combatLog.children.length > 20) {
            this.combatLog.removeChild(this.combatLog.lastChild);
        }
    }

    /**
     * 토스트 메시지 표시
     * @param {string} message 
     */
    showToast(message) {
        // 간단한 구현 - 추후 개선
        gameLogger.info('Toast:', message);
    }

    /**
     * 숫자 포맷팅
     * @param {number} num 
     * @returns {string}
     */
    formatNumber(num) {
        if (num >= 1000000) {
            return (num / 1000000).toFixed(1) + 'M';
        }
        if (num >= 1000) {
            return (num / 1000).toFixed(1) + 'K';
        }
        return num.toString();
    }
}

export { UIManager };
