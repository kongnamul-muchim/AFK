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
        
        // Auto-repeat button
        this.btnAutoRepeat = document.getElementById('btn-auto-repeat');
        
        // Combat log
        this.combatLog = document.getElementById('combat-log');
        
        // 아이템 드롭 이펙트 컨테이너
        this.itemDropEffects = document.getElementById('item-drop-effects');
        
        // 아이템 타입별 아이콘
        this.itemIcons = {
            'weapon': '⚔️',
            'armor': '🛡️',
            'boots': '👟',
            'accessory': '💍'
        };
        
        // Modals
        this.inventoryModal = document.getElementById('inventory-modal');
        this.settingsModal = document.getElementById('settings-modal');
        this.statsModal = document.getElementById('stats-modal');
        this.offlineRewardModal = document.getElementById('offline-reward-modal');
        this.statisticsModal = document.getElementById('statistics-modal');
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
        this.setupItemDropEffects();
        this.updateHUD();
        this.updateStatsPanel();
        gameLogger.debug('UIManager initialized');
    }

    /**
     * 아이템 드롭 이펙트 이벤트 설정
     */
    setupItemDropEffects() {
        gameEventBus.on(GAME_EVENTS.INVENTORY_ITEM_ADDED, (data) => {
            this.showItemDropEffect(data);
        });
    }

    /**
     * 아이템 드롭 이펙트 표시 (플레이어 머리 위에서 위로 슬라이드)
     */
    showItemDropEffect(data) {
        if (!this.itemDropEffects) return;
        
        const { itemId, name, rarity } = data;
        
        // 아이템 타입 추출 (name에서 weapon/armor/boots/accessory 판별)
        let itemType = 'weapon';
        const nameLower = name.toLowerCase();
        if (nameLower.includes('armor')) itemType = 'armor';
        else if (nameLower.includes('boot')) itemType = 'boots';
        else if (nameLower.includes('ring') || nameLower.includes('accessory')) itemType = 'accessory';
        
        const icon = this.itemIcons[itemType] || '⚔️';
        
        // canvas 위치 기준으로 플레이어 머리 위 계산
        const canvas = document.getElementById('game-canvas');
        if (canvas) {
            const canvasRect = canvas.getBoundingClientRect();
            const containerRect = document.getElementById('game-container').getBoundingClientRect();
            
            // 플레이어 머리 위 위치 (canvas 상단에서 약 30% 지점)
            const playerHeadY = canvasRect.top - containerRect.top + canvasRect.height * 0.3;
            
            this.itemDropEffects.style.top = `${playerHeadY}px`;
        }
        
        // 이펙트 엘리먼트 생성
        const effect = document.createElement('div');
        effect.className = `item-drop-effect ${rarity}`;
        effect.innerHTML = `
            <span class="item-icon">${icon}</span>
            <span class="item-name">${name}</span>
        `;
        
        // 컨테이너에 추가
        this.itemDropEffects.appendChild(effect);
        
        // 애니메이션 완료 후 제거 (1.5초)
        setTimeout(() => {
            if (effect.parentNode) {
                effect.parentNode.removeChild(effect);
            }
        }, 1500);
    }

    /**
     * 메뉴 버튼 설정
     */
    setupMenuButtons() {
        document.getElementById('btn-inventory')?.addEventListener('click', () => {
            this.showModal(this.inventoryModal);
            // InventoryUI 에게 위임
            if (this.game.inventoryUI) {
                this.game.inventoryUI.renderInventory();
            } else {
                this.renderInventory();
            }
        });
        
        document.getElementById('btn-stats')?.addEventListener('click', () => {
            this.showModal(this.statsModal);
        });
        
        document.getElementById('btn-statistics')?.addEventListener('click', () => {
            this.showStatisticsModal();
        });
        
        document.getElementById('btn-settings')?.addEventListener('click', () => {
            this.showModal(this.settingsModal);
        });
        
        // 자동반복 모드 버튼
        this.btnAutoRepeat?.addEventListener('click', () => {
            if (this.game && this.game.combatSystem) {
                this.game.combatSystem.toggleAutoRepeat();
                this.updateAutoRepeatButton();
            }
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
        
        document.getElementById('btn-close-offline')?.addEventListener('click', () => {
            this.hideModal(this.offlineRewardModal);
        });
        
        document.getElementById('btn-close-statistics')?.addEventListener('click', () => {
            this.hideModal(this.statisticsModal);
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
                const success = this.gameState.increaseStat(statType);
                
                if (success) {
                    // 성공 시 피드백
                    this.showToast(`${statType.toUpperCase()} +1!`);
                    this.updateStatsPanel();
                    this.updateHUD();
                } else {
                    // 스탯포인트 부족
                    this.showToast('스타츠 포인트가 부족합니다!');
                }
            });
        });
    }

    /**
     * 상태창 업데이트 (게임 루프에서 호출)
     */
    updateGameView() {
        // 플레이어 상태 업데이트
        this.updateHUD();
        
        // 스탯 패널 업데이트 (모달이 열려있을 때)
        const statsModal = document.getElementById('stats-modal');
        if (statsModal && statsModal.style.display !== 'none') {
            this.updateStatsPanel();
        }
    }

    /**
     * 토스트 메시지 표시
     * @param {string} message 
     */
    showToast(message) {
        // 기존 토스트 제거
        const existing = document.querySelector('.toast-message');
        if (existing) {
            existing.remove();
        }
        
        // 새 토스트 생성
        const toast = document.createElement('div');
        toast.className = 'toast-message';
        toast.textContent = message;
        toast.style.cssText = `
            position: fixed;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            background: rgba(0, 0, 0, 0.8);
            color: white;
            padding: 1rem 2rem;
            border-radius: 8px;
            font-size: 1.2rem;
            font-weight: bold;
            z-index: 9999;
            animation: toastFade 1.5s ease-out forwards;
        `;
        
        document.body.appendChild(toast);
        
        // 애니메이션 완료 후 제거
        setTimeout(() => {
            toast.remove();
        }, 1500);
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
        
        // 데이터 초기화 - 모든 게임 상태 완전 삭제
        document.getElementById('btn-reset-data')?.addEventListener('click', () => {
            // 1단계 경고
            if (!confirm('⚠️ 경고\n\n정말로 모든 데이터를 초기화하시겠습니까?\n\n초기화되면 다음과 같은 데이터가 모두 삭제됩니다:\n• 레벨, 스탯, 골드\n• 아이템, 업그레이드\n• 진행도, 업적\n• 설정, 세이브 데이터\n\n이 작업은 되돌릴 수 없습니다.')) {
                return;
            }
            
            // 2단계 최종 확인
            if (!confirm('정말로 초기화하시겠습니까?')) {
                return;
            }
            
            try {
                // localStorage 완전 삭제
                localStorage.clear();
                
                // 세션 스토리지도 삭제
                sessionStorage.clear();
                
                // 인덱스DB(있는 경우) 삭제
                if (indexedDB.deleteDatabase) {
                    // 모든 데이터베이스 삭제 시도
                    indexedDB.databases()?.then(dbs => {
                        dbs.forEach(db => indexedDB.deleteDatabase(db.name));
                    }).catch(() => {});
                }
                
                // 쿠키 삭제
                document.cookie.split(';').forEach(cookie => {
                    document.cookie = cookie.replace(/=.*/, '=;expires=' + new Date(0).toUTCString() + ';path=/');
                });
                
                gameLogger.info('모든 게임 데이터가 초기화되었습니다.');
                
                // 페이지 강제 새로고침 (캐시 무시)
                location.replace(location.href);
            } catch (error) {
                gameLogger.error('데이터 초기화 중 오류 발생:', error);
                alert('데이터 초기화 중 오류가 발생했습니다.');
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
            const maxHp = player.derivedStats.maxHp || player.maxHp || 1;
            const hpPercent = (player.currentHp / maxHp) * 100;
            this.hudHpFill.style.width = `${hpPercent}%`;
        }
        
        if (this.hudHpText) {
            const maxHp = player.derivedStats.maxHp || player.maxHp || 1;
            this.hudHpText.textContent = `${player.currentHp}/${maxHp}`;
        }
        
        if (this.hudStage) {
            this.hudStage.textContent = `Stage ${stage.current}`;
            // 자동반복 모드 표시
            if (stage.autoRepeat) {
                this.hudStage.textContent += ' (자동반복)';
            }
        }
        
        // 자동반복 버튼 상태 업데이트
        this.updateAutoRepeatButton();
        
        if (this.hudGold) {
            this.hudGold.textContent = this.formatNumber(inventory.gold);
        }
        
        if (this.hudExpFill) {
            const exp = player.exp || 0;
            const maxExp = player.maxExp || 1;
            const expPercent = (exp / maxExp) * 100;
            this.hudExpFill.style.width = `${expPercent}%`;
        }
        
        if (this.hudExpText) {
            const exp = player.exp || 0;
            const maxExp = player.maxExp || 1;
            this.hudExpText.textContent = `Exp: ${this.formatNumber(exp)}/${this.formatNumber(maxExp)}`;
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
        
        // 기존 stats-panel 요소 업데이트 (null 체크)
        const levelEl = document.getElementById('stats-level');
        if (levelEl) {
            levelEl.textContent = player.level;
        }
        const expEl = document.getElementById('stats-exp');
        if (expEl) {
            expEl.textContent = `${player.exp}/${player.maxExp}`;
        }
        const pointsEl = document.getElementById('stats-points');
        if (pointsEl) {
            pointsEl.textContent = player.statPoints;
        }
        const strEl = document.getElementById('stat-str');
        if (strEl) {
            strEl.textContent = player.stats.str;
        }
        const agiEl = document.getElementById('stat-agi');
        if (agiEl) {
            agiEl.textContent = player.stats.agi;
        }
        const intEl = document.getElementById('stat-int');
        if (intEl) {
            intEl.textContent = player.stats.int;
        }
        const vitEl = document.getElementById('stat-vit');
        if (vitEl) {
            vitEl.textContent = player.stats.vit;
        }
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
     * 오프라인 보상 표시 (기존 - 레거시)
     * @param {number} hours 
     * @param {number} exp 
     * @param {number} gold 
     */
    showOfflineReward(hours, exp, gold) {
        const durationEl = document.getElementById('offline-duration');
        if (durationEl) {
            durationEl.textContent = `${hours.toFixed(1)}시간`;
        }
        const expEl = document.getElementById('offline-exp');
        if (expEl) {
            expEl.textContent = this.formatNumber(exp);
        }
        const goldEl = document.getElementById('offline-gold');
        if (goldEl) {
            goldEl.textContent = this.formatNumber(gold);
        }
        
        document.getElementById('btn-claim-reward')?.addEventListener('click', () => {
            this.hideModal(this.offlineRewardModal);
        });
        
        this.showModal(this.offlineRewardModal);
    }

    /**
     * 오프라인 보상 상세 표시 (새 버전)
     */
    showOfflineRewardDetailed(data) {
        const { hours, kills, gold, exp, equipment } = data;
        
        // 오프라인 시간
        const durationEl = document.getElementById('offline-duration');
        if (durationEl) {
            durationEl.textContent = `${hours.toFixed(1)}시간`;
        }
        
        // 처치 수
        const killsEl = document.getElementById('offline-kills');
        if (killsEl) {
            killsEl.textContent = `${kills.toLocaleString()}마리`;
        }
        
        // 골드
        const goldEl = document.getElementById('offline-gold');
        if (goldEl) {
            goldEl.textContent = this.formatNumber(gold);
        }
        
        // 경험치
        const expEl = document.getElementById('offline-exp');
        if (expEl) {
            expEl.textContent = this.formatNumber(exp);
        }
        
        // 장비 드롭 목록
        const equipmentListEl = document.getElementById('offline-equipment-list');
        if (equipmentListEl) {
            equipmentListEl.innerHTML = '';
            
            if (equipment && equipment.length > 0) {
                const equipmentSection = document.getElementById('offline-equipment-section');
                if (equipmentSection) {
                    equipmentSection.style.display = 'block';
                }
                
                const equipmentHeader = document.createElement('p');
                equipmentHeader.textContent = `드롭된 장비 (${equipment.length}개):`;
                equipmentListEl.appendChild(equipmentHeader);
                
                // 최대 10개만 표시
                const displayCount = Math.min(equipment.length, 10);
                for (let i = 0; i < displayCount; i++) {
                    const equip = equipment[i];
                    const itemDiv = document.createElement('div');
                    itemDiv.className = `offline-item ${equip.rarity}`;
                    itemDiv.innerHTML = `
                        <span class="item-icon">${this.getItemIcon(equip.type)}</span>
                        <span class="item-name">${equip.name}</span>
                        <span class="item-rarity">${this.getRarityName(equip.rarity)}</span>
                    `;
                    equipmentListEl.appendChild(itemDiv);
                }
                
                if (equipment.length > 10) {
                    const moreDiv = document.createElement('div');
                    moreDiv.className = 'offline-item-more';
                    moreDiv.textContent = `...외 ${equipment.length - 10}개`;
                    equipmentListEl.appendChild(moreDiv);
                }
            }
        }
        
        // 확인 버튼 이벤트
        const claimBtn = document.getElementById('btn-claim-reward');
        if (claimBtn) {
            claimBtn.onclick = () => {
                this.hideModal(this.offlineRewardModal);
            };
        }
        
        // 모달 표시
        this.showModal(this.offlineRewardModal);
    }

    getItemIcon(type) {
        const icons = {
            'weapon': '⚔️',
            'armor': '🛡️',
            'boots': '👟',
            'accessory': '💍'
        };
        return icons[type] || '⚔️';
    }

    getRarityName(rarity) {
        const names = {
            'common': '일반',
            'rare': '희귀',
            'epic': '영웅',
            'legendary': '전설',
            'mythic': '신화'
        };
        return names[rarity] || rarity;
    }

    /**
     * 튜토리얼 가이드 표시
     * @param {Object} data - { step, message, reward }
     */
    showTutorialGuide(data) {
        const overlay = document.getElementById('tutorial-overlay');
        const message = document.getElementById('tutorial-message');
        const nextBtn = document.getElementById('tutorial-next');
        
        if (!overlay || !message) return;
        
        message.textContent = data.message;
        overlay.style.display = 'flex';
        
        // 다음 버튼
        const handleClick = () => {
            this.hideTutorialGuide();
            nextBtn?.removeEventListener('click', handleClick);
        };
        nextBtn?.addEventListener('click', handleClick);
        
        gameLogger.debug(`Tutorial step ${data.step}: ${data.message}`);
    }

    /**
     * 튜토리얼 가이드 숨김
     */
    hideTutorialGuide() {
        const overlay = document.getElementById('tutorial-overlay');
        if (overlay) {
            overlay.style.display = 'none';
        }
    }

    /**
     * 통계 화면 렌더링
     */
    renderStats() {
        const stats = this.gameState.stats;
        
        const playTimeSeconds = Math.floor(stats.playTime / 1000);
        const hours = Math.floor(playTimeSeconds / 3600);
        const minutes = Math.floor((playTimeSeconds % 3600) / 60);
        const seconds = playTimeSeconds % 60;
        
        const playtimeEl = document.getElementById('stats-playtime');
        if (playtimeEl) {
            playtimeEl.textContent = `${hours}시간 ${minutes}분 ${seconds}초`;
        }
        const killsEl = document.getElementById('stats-kills');
        if (killsEl) {
            killsEl.textContent = stats.totalKills;
        }
        const maxStageEl = document.getElementById('stats-max-stage');
        if (maxStageEl) {
            maxStageEl.textContent = stats.maxStage;
        }
        const levelupsEl = document.getElementById('stats-levelups');
        if (levelupsEl) {
            levelupsEl.textContent = stats.totalLevelups;
        }
        const goldEl = document.getElementById('stats-gold');
        if (goldEl) {
            goldEl.textContent = this.formatNumber(stats.totalGold);
        }
    }

    /**
     * 데미지 텍스트 표시
     * @param {number} damage 
     * @param {Object} position - { x, y }
     */
    showDamageText(damage, position) {
        const gameView = document.querySelector('.game-view');
        if (!gameView) return;
        
        const text = document.createElement('div');
        text.className = 'damage-text';
        text.textContent = damage;
        text.style.left = `${position.x}px`;
        text.style.top = `${position.y}px`;
        
        // 크리티컬이면 크게
        if (damage > 50) {
            text.style.fontSize = '2rem';
            text.style.color = '#fbbf24';
        }
        
        gameView.appendChild(text);
        
        // 애니메이션 완료 후 제거
        setTimeout(() => {
            text.remove();
        }, 1000);
    }

    /**
     * 레벨업 이펙트 표시
     */
    showLevelUpEffect() {
        const glow = document.createElement('div');
        glow.className = 'levelup-glow';
        document.body.appendChild(glow);
        
        setTimeout(() => {
            glow.remove();
        }, 2000);
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
     * 튜토리얼 가이드 표시
     * @param {Object} data - { step, message, reward }
     */
    showTutorialGuide(data) {
        const overlay = document.getElementById('tutorial-overlay');
        const message = document.getElementById('tutorial-message');
        const nextBtn = document.getElementById('tutorial-next');
        
        if (!overlay || !message) return;
        
        message.textContent = data.message;
        overlay.style.display = 'flex';
        
        // 다음 버튼
        nextBtn?.addEventListener('click', () => {
            this.hideTutorialGuide();
        });
        
        gameLogger.debug(`Tutorial step ${data.step}: ${data.message}`);
    }

    /**
     * 튜토리얼 가이드 숨김
     */
    hideTutorialGuide() {
        const overlay = document.getElementById('tutorial-overlay');
        if (overlay) {
            overlay.style.display = 'none';
        }
    }

    /**
     * 통계 화면 렌더링
     */
    renderStats() {
        const stats = this.gameState.stats;
        
        const playTimeSeconds = Math.floor(stats.playTime / 1000);
        const hours = Math.floor(playTimeSeconds / 3600);
        const minutes = Math.floor((playTimeSeconds % 3600) / 60);
        const seconds = playTimeSeconds % 60;
        
        const playtimeEl = document.getElementById('stats-playtime');
        if (playtimeEl) {
            playtimeEl.textContent = `${hours}시간 ${minutes}분 ${seconds}초`;
        }
        const killsEl = document.getElementById('stats-kills');
        if (killsEl) {
            killsEl.textContent = stats.totalKills;
        }
        const maxStageEl = document.getElementById('stats-max-stage');
        if (maxStageEl) {
            maxStageEl.textContent = stats.maxStage;
        }
        const levelupsEl = document.getElementById('stats-levelups');
        if (levelupsEl) {
            levelupsEl.textContent = stats.totalLevelups;
        }
        const goldEl = document.getElementById('stats-gold');
        if (goldEl) {
            goldEl.textContent = this.formatNumber(stats.totalGold);
        }
    }

    /**
     * 숫자 포맷팅
     * @param {number} num 
     * @returns {string}
     */
    formatNumber(num) {
        if (num === null || num === undefined || isNaN(num)) {
            return '0';
        }
        if (num >= 1000000) {
            return (num / 1000000).toFixed(1) + 'M';
        }
        if (num >= 1000) {
            return (num / 1000).toFixed(1) + 'K';
        }
        return num.toString();
    }

    /**
     * 자동반복 버튼 상태 업데이트
     */
    updateAutoRepeatButton() {
        if (!this.btnAutoRepeat) return;
        
        const isAutoRepeat = this.gameState.stage.autoRepeat || false;
        if (isAutoRepeat) {
            this.btnAutoRepeat.classList.add('active');
        } else {
            this.btnAutoRepeat.classList.remove('active');
        }
    }

    /**
     * 통계 모달 표시 (상세 스탯)
     */
    showStatisticsModal() {
        // 플레이어 스탯
        const player = this.gameState.player;
        const derivedStats = player.derivedStats;
        
        document.getElementById('stats-level').textContent = player.level;
        document.getElementById('stats-atk').textContent = Math.floor(derivedStats.attack || 0);
        document.getElementById('stats-def').textContent = Math.floor(derivedStats.defense || 0);
        document.getElementById('stats-hp').textContent = Math.floor(derivedStats.maxHp || 0);
        document.getElementById('stats-movespeed').textContent = Math.floor(derivedStats.moveSpeed || 0);
        document.getElementById('stats-critrate').textContent = ((derivedStats.critChance || 0) * 100).toFixed(1) + '%';
        document.getElementById('stats-critdmg').textContent = (derivedStats.critDamage * 100).toFixed(0) + '%';
        
        // 진행 상황
        const stage = this.gameState.stage;
        const stats = this.gameState.stats;
        document.getElementById('stats-current-stage').textContent = stage.current || 1;
        document.getElementById('stats-max-stage').textContent = stats.maxStage || stage.max || 1;
        document.getElementById('stats-stage-clears').textContent = stats.totalClears || 0;
        document.getElementById('stats-kills').textContent = stats.totalKills;
        document.getElementById('stats-boss-kills').textContent = stats.bossKills || 0;
        
        // 재화
        const inventory = this.gameState.inventory;
        document.getElementById('stats-gold').textContent = this.formatNumber(inventory.gold);
        document.getElementById('stats-gems').textContent = this.formatNumber(inventory.gems || 0);
        document.getElementById('stats-total-gold').textContent = this.formatNumber(this.gameState.stats.totalGold || 0);
        
        // 기타 통계 (playTime은 밀리초 단위)
        const playtimeMs = stats.playTime || 0;
        const playtimeSeconds = Math.floor(playtimeMs / 1000);
        const hours = Math.floor(playtimeSeconds / 3600);
        const minutes = Math.floor((playtimeSeconds % 3600) / 60);
        const seconds = playtimeSeconds % 60;
        document.getElementById('stats-playtime').textContent = `${hours}시간 ${minutes}분 ${seconds}초`;
        document.getElementById('stats-levelups').textContent = stats.totalLevelups || 0;
        document.getElementById('stats-rebirth').textContent = this.gameState.rebirth.count || 0;
        document.getElementById('stats-inventory').textContent = this.gameState.inventory.items.size;
        
        this.showModal(this.statisticsModal);
    }
}

export { UIManager };
