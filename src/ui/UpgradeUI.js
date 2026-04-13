/**
 * UpgradeUI - 업그레이드 UI 관리
 * 하나의 모달에 2개 탭 (골드/스탯) + 리스트 스크롤
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameLogger } from '../core/Logger.js';

class UpgradeUI {
    constructor(gameState, rebirthSystem) {
        this.gameState = gameState;
        this.rebirthSystem = rebirthSystem;
        this.currentTab = 'gold'; // gold or stat or rebirth
        
        // 효율 배율 계산 (10레벨마다 증가)
        // Lv 1-10: ×1.0, Lv 11-20: ×1.5, Lv 21-30: ×2.0, Lv 31-40: ×2.5, Lv 41+: ×3.0
        this.getEfficiencyMultiplier = (level) => {
            if (level < 10) return 1.0;
            if (level < 20) return 1.5;
            if (level < 30) return 2.0;
            if (level < 40) return 2.5;
            return 3.0;
        };
        
        // 스탯 정의 (리스트 순서)
        // calcUpgradeValue: GameState의 calcUpgradeValue와 동일한 누적 계산 함수
        const calcUpgradeValue = (lvl) => {
            if (lvl < 10) return lvl * 1.0;
            if (lvl < 20) return 10 * 1.0 + (lvl - 10) * 1.5;
            if (lvl < 30) return 10 * 1.0 + 10 * 1.5 + (lvl - 20) * 2.0;
            if (lvl < 40) return 10 * 1.0 + 10 * 1.5 + 10 * 2.0 + (lvl - 30) * 2.5;
            return 10 * 1.0 + 10 * 1.5 + 10 * 2.0 + 10 * 2.5 + (lvl - 40) * 3.0;
        };

        this.statDefinitions = {
            attack: {
                name: '공격력',
                maxLevel: null,
                goldCostBase: 100,
                statCost: 1,
                baseValue: 2,  // calcUpgradeValue 결과에 곱해지는 계수
                getValue: (level) => {
                    const value = calcUpgradeValue(level) * 2;
                    return `+${value}`;
                },
                tabs: ['gold', 'stat']
            },
            defense: {
                name: '방어력',
                maxLevel: null,
                goldCostBase: 80,
                statCost: 1,
                baseValue: 1,
                getValue: (level) => {
                    const value = calcUpgradeValue(level) * 1;
                    return `+${value}`;
                },
                tabs: ['gold', 'stat']
            },
            hp: {
                name: '체력',
                maxLevel: null,
                goldCostBase: 50,
                statCost: 1,
                baseValue: 10,
                getValue: (level) => {
                    const value = calcUpgradeValue(level) * 10;
                    return `+${value}`;
                },
                tabs: ['gold', 'stat']
            },
            hpRegen: {
                name: 'HP 회복',
                maxLevel: null,
                goldCostBase: 60,
                statCost: 1,
                baseValue: 1,
                getValue: (level) => {
                    const value = calcUpgradeValue(level) * 1;
                    return `+${value}/sec`;
                },
                tabs: ['gold', 'stat']
            },
            attackSpeed: {
                name: '공격속도',
                maxLevel: 50,
                goldCostBase: 150,
                statCost: 1,
                baseValue: 1,
                getValue: (level) => {
                    const value = calcUpgradeValue(level) * 1;
                    return `+${value}% (×${(100 + value) / 100})`;
                },
                tabs: ['gold', 'stat']
            },
            critChance: {
                name: '치명타 확률',
                maxLevel: 500,
                goldCostBase: 120,
                statCost: 1,
                baseValue: 0.2,
                getValue: (level) => {
                    const value = calcUpgradeValue(level) * 0.2;
                    return `+${value.toFixed(1)}%`;
                },
                tabs: ['gold', 'stat']
            },
            critDamage: {
                name: '치명타 데미지',
                maxLevel: null,
                goldCostBase: 100,
                statCost: 1,
                baseValue: 1,
                getValue: (level) => {
                    const value = calcUpgradeValue(level) * 1;
                    return `+${value}%`;
                },
                tabs: ['gold', 'stat']
            },
            decisiveChance: {
                name: '결정타 확률',
                maxLevel: 500,
                goldCostBase: 200,
                statCost: 1,
                baseValue: 0.2,
                getValue: (level) => {
                    const value = calcUpgradeValue(level) * 0.2;
                    return `+${value.toFixed(1)}%`;
                },
                tabs: ['gold'],
                unlockCondition: () => this.gameState.player.goldUpgrades.critChance >= 500,
                unlockMessage: '치명타 확률 100% 필요'
            },
            decisiveDamage: {
                name: '결정타 데미지',
                maxLevel: null,
                goldCostBase: 200,
                statCost: 1,
                baseValue: 1,
                getValue: (level) => {
                    const value = calcUpgradeValue(level) * 1;
                    return `+${value}%`;
                },
                tabs: ['gold'],
                unlockCondition: () => this.gameState.player.goldUpgrades.critChance >= 500,
                unlockMessage: '치명타 확률 100% 필요'
            },
            goldBonus: {
                name: '골드 획득량',
                maxLevel: 100,
                goldCostBase: 300,
                statCost: 1,
                baseValue: 1,
                getValue: (level) => {
                    const value = calcUpgradeValue(level) * 1;
                    return `+${value}%`;
                },
                tabs: ['gold']
            },
            expBonus: {
                name: '경험치 획득량',
                maxLevel: 100,
                goldCostBase: 300,
                statCost: 1,
                baseValue: 1,
                getValue: (level) => {
                    const value = calcUpgradeValue(level) * 1;
                    return `+${value}%`;
                },
                tabs: ['gold']
            }
        };
        
        // 보석 업그레이드 정의
        this.gemUpgradeDefinitions = {
            offlineBonus: {
                name: '오프라인 보상 증가',
                description: '오프라인 보상 2% 증가',
                maxLevel: null,
                gemCostBase: 10,
                getValue: (level) => `+${(level + 1) * 2}%`,
                effect: 'offline'
            },
            critDamage: {
                name: '치명타 피해 증가',
                description: '치명타 피해 2% 증가',
                maxLevel: null,
                gemCostBase: 15,
                getValue: (level) => `+${(level + 1) * 2}%`,
                effect: 'critDmg'
            },
            autoCombatDamage: {
                name: '자동 전투 강화',
                description: '자동 전투 시 데미지 2% 증가',
                maxLevel: 50,
                gemCostBase: 20,
                getValue: (level) => `+${Math.min(100, (level + 1) * 2)}%`,
                effect: 'autoDmg'
            },
            rebirthBonus: {
                name: '환생 보너스',
                description: '환생 시 보너스 포인트 1개 추가',
                maxLevel: 10,
                gemCostBase: 50,
                getValue: (level) => `+${level + 1} 포인트`,
                effect: 'rebirth'
            },
            dropRate: {
                name: '드롭 확률 업',
                description: '레어 아이템 드롭률 증가 (등급별 차등)',
                maxLevel: 20,
                gemCostBase: 25,
                getValue: (level) => {
                    const rates = this.getDropRateValues(level);
                    return `일반:${rates.common}%, 고급:${rates.rare}%, 희귀:${rates.epic}%`;
                },
                effect: 'drop'
            },
            baseStats: {
                name: '기본 스탯 증가',
                description: '공격력/방어력/체력 1% 증가',
                maxLevel: null,
                gemCostBase: 15,
                getValue: (level) => `+${(level + 1) * 1}%`,
                effect: 'stats'
            }
        };
    }
    
    /**
     * 드롭 확률업 레벨별 값 계산
     */
    getDropRateValues(level) {
        const baseRates = { common: 70, rare: 20, epic: 7, heroic: 2.5, legendary: 0.5 };
        const changesPerLevel = { common: -1.1, rare: 0.2, epic: 0.4, heroic: 0.4, legendary: 0.1 };
        
        return {
            common: Math.max(10, Math.floor((baseRates.common + changesPerLevel.common * level) * 10) / 10),
            rare: Math.floor((baseRates.rare + changesPerLevel.rare * level) * 10) / 10,
            epic: Math.floor((baseRates.epic + changesPerLevel.epic * level) * 10) / 10,
            heroic: Math.floor((baseRates.heroic + changesPerLevel.heroic * level) * 10) / 10,
            legendary: Math.floor((baseRates.legendary + changesPerLevel.legendary * level) * 10) / 10
        };
    }

    /**
     * 초기화
     */
    init() {
        this.setupModal();
        this.setupTabs();
        this.updateDisplay();
        this.renderUpgradeGrid();
    }

    /**
     * 모달 설정
     */
    setupModal() {
        const modal = document.getElementById('upgrade-modal');
        const btn = document.getElementById('btn-upgrade');
        const closeBtn = document.getElementById('btn-close-upgrade');

        if (btn) {
            btn.addEventListener('click', () => {
                modal.style.display = 'flex';
                this.updateDisplay();
                this.renderUpgradeGrid();
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

        // 이벤트 리스너
        gameEventBus.on(GAME_EVENTS.UPGRADE_PURCHASED, () => {
            this.renderUpgradeGrid();
            this.updateDisplay();
        });

        gameEventBus.on(GAME_EVENTS.REBIRTH_UPGRADE_PURCHASED, () => {
            this.renderUpgradeGrid();
            this.updateDisplay();
        });

        gameEventBus.on(GAME_EVENTS.INVENTORY_GOLD_CHANGED, () => {
            this.updateDisplay();
        });

        gameEventBus.on(GAME_EVENTS.PLAYER_STAT_CHANGED, () => {
            this.updateDisplay();
        });
    }

    /**
     * 탭 설정
     */
    setupTabs() {
        document.querySelectorAll('#upgrade-modal .upgrade-tabs .tab-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                document.querySelectorAll('#upgrade-modal .upgrade-tabs .tab-btn').forEach(b => {
                    b.classList.remove('active');
                });
                btn.classList.add('active');
                this.currentTab = btn.dataset.tab;
                this.updateDisplay();
                this.renderUpgradeGrid();
            });
        });
    }

    /**
     * 업그레이드 그리드 렌더링
     */
    renderUpgradeGrid() {
        const grid = document.getElementById('upgrade-grid');
        if (!grid) return;

        grid.innerHTML = '';

        // 환생 탭인 경우
        if (this.currentTab === 'rebirth') {
            this.renderRebirthGrid(grid);
            return;
        }

        // 보석 탭인 경우
        if (this.currentTab === 'gem') {
            this.renderGemUpgradeGrid(grid);
            return;
        }

        // 일반 탭 (골드/스탯)
        const stats = Object.entries(this.statDefinitions).filter(([key, def]) => {
            return def.tabs.includes(this.currentTab);
        });

        stats.forEach(([key, def]) => {
            const item = this.createUpgradeItem(key, def);
            grid.appendChild(item);
        });
    }

    /**
     * 보석 업그레이드 그리드 렌더링
     */
    renderGemUpgradeGrid(grid) {
        const gemUpgrades = this.gameState.gemUpgrades;
        
        Object.entries(this.gemUpgradeDefinitions).forEach(([key, def]) => {
            const upgradeData = gemUpgrades[key] || { unlocked: false, level: 0 };
            const item = this.createGemUpgradeItem(key, def, upgradeData);
            grid.appendChild(item);
        });
    }

    /**
     * 보석 업그레이드 아이템 생성
     */
    createGemUpgradeItem(key, def, upgradeData) {
        const container = document.createElement('div');
        container.className = 'upgrade-item';
        
        const { unlocked, level } = upgradeData;
        const maxLevel = def.maxLevel;
        const isMaxLevel = maxLevel !== null && level >= maxLevel;
        
        if (!unlocked) {
            // 해금 전: 해금 버튼 표시
            const unlockCost = def.gemCostBase; // 해금 비용은 기본 비용
            const gems = this.gameState.inventory.gems || 0;
            const canAfford = gems >= unlockCost;
            
            container.innerHTML = `
                <div class="upgrade-info">
                    <span class="upgrade-name">${def.name}</span>
                    <span class="upgrade-level locked">🔒 해금 필요</span>
                </div>
                <div class="upgrade-stats">
                    <span class="upgrade-current" style="color: #888;">${def.description}</span>
                    <span class="upgrade-next"></span>
                </div>
                <button class="upgrade-button unlock-btn ${!canAfford ? 'disabled' : ''}"
                        ${!canAfford ? 'disabled' : ''}>
                    💎 ${this.formatNumber(unlockCost)} (해금)
                </button>
            `;
            
            const unlockBtn = container.querySelector('.unlock-btn');
            if (canAfford) {
                unlockBtn.addEventListener('click', () => {
                    this.unlockGemUpgrade(key, def);
                });
            }
        } else {
            // 해금 후: 레벨 업그레이드 표시
            const upgradeCost = Math.floor(def.gemCostBase * Math.pow(1.15, level));
            const gems = this.gameState.inventory.gems || 0;
            const canAfford = gems >= upgradeCost && !isMaxLevel;
            
            const levelDisplay = maxLevel !== null 
                ? `Lv.${level}/${maxLevel}` 
                : `Lv.${level}`;
            
            // 현재 값과 다음 레벨 값 계산
            const currentValue = def.getValue(level);
            const nextLevel = isMaxLevel ? level : level + 1;
            const nextValue = isMaxLevel ? '' : def.getValue(nextLevel);
            
            container.innerHTML = `
                <div class="upgrade-info">
                    <span class="upgrade-name">${def.name}</span>
                    <span class="upgrade-level ${isMaxLevel ? 'max' : ''}">${levelDisplay}</span>
                </div>
                <div class="upgrade-stats">
                    <span class="upgrade-current">${currentValue}</span>
                    <span class="upgrade-next">${isMaxLevel ? '' : '→ ' + nextValue}</span>
                </div>
                <button class="upgrade-button ${!canAfford && !isMaxLevel ? 'disabled' : ''} ${isMaxLevel ? 'max-level' : ''}"
                        ${!canAfford && !isMaxLevel ? 'disabled' : ''}>
                    ${isMaxLevel ? '최대 레벨' : `💎 ${this.formatNumber(upgradeCost)}`}
                </button>
            `;
            
            const upgradeBtn = container.querySelector('.upgrade-button');
            if (!isMaxLevel && canAfford) {
                upgradeBtn.addEventListener('click', () => {
                    this.upgradeGemUpgrade(key, def, level);
                });
            }
        }
        
        return container;
    }

    /**
     * 보석 업그레이드 해금 (첫 구매)
     */
    unlockGemUpgrade(key, def) {
        const unlockCost = def.gemCostBase;
        const gems = this.gameState.inventory.gems || 0;
        
        if (gems < unlockCost) return;
        
        // 이미 해금되었으면 무시
        if (this.gameState.gemUpgrades[key].unlocked) return;
        
        // 보석 차감
        this.gameState.inventory.gems -= unlockCost;
        
        // 해금 처리 (레벨 1로 시작)
        this.gameState.gemUpgrades[key] = { unlocked: true, level: 1 };
        
        gameLogger.info(`Gem upgrade unlocked: ${key} → Lv.1`);
        
        // UI 업데이트
        this.updateDisplay();
        this.renderUpgradeGrid();
        
        // 파생 스탯 재계산 (효과 즉시 적용)
        this.gameState.recalculateDerivedStats();
        
        gameEventBus.emit(GAME_EVENTS.GEM_UPGRADE_PURCHASED, { key, level: 1, unlocked: true });
    }

    /**
     * 보석 업그레이드 레벨업 (해금 후 추가 업그레이드)
     */
    upgradeGemUpgrade(key, def, currentLevel) {
        const upgradeCost = Math.floor(def.gemCostBase * Math.pow(1.15, currentLevel));
        const gems = this.gameState.inventory.gems || 0;
        
        if (gems < upgradeCost) return;
        
        // 해금되지 않았으면 무시
        if (!this.gameState.gemUpgrades[key].unlocked) return;
        
        // 최대 레벨 확인
        const maxLevel = def.maxLevel;
        if (maxLevel !== null && currentLevel >= maxLevel) return;
        
        // 보석 차감
        this.gameState.inventory.gems -= upgradeCost;
        
        // 레벨 증가
        this.gameState.gemUpgrades[key].level = currentLevel + 1;
        
        gameLogger.info(`Gem upgrade upgraded: ${key} → Lv.${currentLevel + 1}`);
        
        // UI 업데이트
        this.updateDisplay();
        this.renderUpgradeGrid();
        
        // 파생 스탯 재계산
        this.gameState.recalculateDerivedStats();
        
        gameEventBus.emit(GAME_EVENTS.GEM_UPGRADE_PURCHASED, { key, level: currentLevel + 1, unlocked: false });
    }

    /**
     * 환생 업그레이드 그리드 렌더링
     */
    renderRebirthGrid(grid) {
        if (!this.rebirthSystem) return;

        // 환생 버튼 영역
        const rebirthSection = this.createRebirthSection();
        grid.appendChild(rebirthSection);

        // 구분선
        const divider = document.createElement('div');
        divider.style.cssText = 'width: 100%; height: 1px; background: rgba(255,255,255,0.1); margin: 1rem 0;';
        grid.appendChild(divider);

        // 환생 업그레이드 목록
        const upgrades = this.rebirthSystem.getAllUpgradeDefinitions();

        upgrades.forEach(upgrade => {
            const item = this.createRebirthUpgradeItem(upgrade);
            grid.appendChild(item);
        });
    }

    /**
     * 환생 섹션 생성 (환생 버튼 + 정보)
     */
    createRebirthSection() {
        const section = document.createElement('div');
        section.style.cssText = 'width: 100%; padding: 1rem; margin-bottom: 0.5rem;';

        const canRebirth = this.gameState.canRebirth();
        const rebirthCount = this.gameState.rebirth.count;
        const bonusPoints = this.gameState.rebirth.bonusPoints;
        const currentLevel = this.gameState.player.level;
        const minLevel = this.gameState.rebirth.minLevel;

        if (canRebirth) {
            const bonusCount = this.gameState.calculateRebirthBonus();
            section.innerHTML = `
                <div style="text-align: center; margin-bottom: 1rem;">
                    <div style="font-size: 1.2rem; font-weight: bold; color: #ffd700; margin-bottom: 0.5rem;">
                        🔄 환생 가능!
                    </div>
                    <div style="color: #aaa; margin-bottom: 0.25rem;">
                        현재 레벨: ${currentLevel} / 최소 레벨: ${minLevel}
                    </div>
                    <div style="color: #aaa; margin-bottom: 0.5rem;">
                        환생 횟수: ${rebirthCount}회
                    </div>
                    <div style="color: #4ade80; margin-bottom: 0.5rem;">
                        획득 보너스: 💎 ${bonusCount}개
                    </div>
                    <div style="color: #888; font-size: 0.85rem; margin-bottom: 1rem;">
                        * 환생 시 레벨 1로 초기화, 장비/인벤토리 유지, 보너스 포인트 획득
                    </div>
                    <button id="btn-perform-rebirth" style="
                        padding: 0.75rem 2rem;
                        font-size: 1.1rem;
                        font-weight: bold;
                        color: #fff;
                        background: linear-gradient(135deg, #ffd700, #ff8c00);
                        border: none;
                        border-radius: 8px;
                        cursor: pointer;
                        transition: transform 0.1s;
                    " onmouseover="this.style.transform='scale(1.05)'" onmouseout="this.style.transform='scale(1)'">
                        🔄 환생하기
                    </button>
                </div>
            `;

            // 환생 버튼 이벤트
            setTimeout(() => {
                const btn = document.getElementById('btn-perform-rebirth');
                if (btn) {
                    btn.addEventListener('click', () => {
                        this.performRebirth();
                    });
                }
            }, 0);
        } else {
            const progress = Math.min(100, (currentLevel / minLevel) * 100);
            section.innerHTML = `
                <div style="text-align: center; margin-bottom: 1rem;">
                    <div style="font-size: 1.2rem; font-weight: bold; color: #666; margin-bottom: 0.5rem;">
                        🔒 환생 잠김
                    </div>
                    <div style="color: #aaa; margin-bottom: 0.25rem;">
                        현재 레벨: ${currentLevel} / 최소 레벨: ${minLevel}
                    </div>
                    <div style="color: #aaa; margin-bottom: 0.5rem;">
                        환생 횟수: ${rebirthCount}회
                    </div>
                    <div style="width: 100%; max-width: 300px; height: 8px; background: #333; border-radius: 4px; margin: 0.5rem auto; overflow: hidden;">
                        <div style="width: ${progress}%; height: 100%; background: linear-gradient(90deg, #4ade80, #22c55e); transition: width 0.3s;"></div>
                    </div>
                    <div style="color: #888; font-size: 0.85rem; margin-top: 0.5rem;">
                        레벨 ${minLevel}이 되어야 환생할 수 있습니다
                    </div>
                </div>
            `;
        }

        return section;
    }

    /**
     * 환생 실행
     */
    performRebirth() {
        if (!this.gameState.canRebirth()) {
            gameLogger.warn('Cannot rebirth: level too low');
            return;
        }

        const bonusPoints = this.gameState.performRebirth();

        gameEventBus.emit(GAME_EVENTS.REBIRTH_PERFORMED, {
            rebirthCount: this.gameState.rebirth.count,
            bonusPoints
        });

        gameLogger.info(`Rebirth performed: +${bonusPoints} bonus points`);

        // UI 갱신
        this.updateDisplay();
        this.renderUpgradeGrid();

        // 알림
        gameEventBus.emit(GAME_EVENTS.COMBAT_LOG, {
            message: `환생 완료! ${bonusPoints}개의 보너스 포인트를 획득했습니다.`
        });
    }

    /**
     * 환생 업그레이드 아이템 생성
     */
    createRebirthUpgradeItem(upgrade) {
        const el = document.createElement('div');
        el.className = `upgrade-item ${!upgrade.isUnlocked ? 'locked' : ''}`;

        const canPurchase = this.rebirthSystem.canPurchaseUpgrade(upgrade.key);
        const isMaxLevel = upgrade.currentLevel >= upgrade.maxLevel;

        el.innerHTML = `
            <div class="upgrade-info">
                <span class="upgrade-name">${upgrade.isUnlocked ? upgrade.name : '???'}</span>
                <span class="upgrade-level">Lv.${upgrade.currentLevel}/${upgrade.maxLevel}</span>
            </div>
            <div class="upgrade-stats">
                <span class="upgrade-current">${upgrade.isUnlocked ? upgrade.description : '해금 조건: ' + this.getUnlockConditionText(upgrade.key)}</span>
                <span class="upgrade-next">${isMaxLevel ? '완성!' : upgrade.isUnlocked ? `다음: ${upgrade.effect(upgrade.currentLevel + 1)}` : ''}</span>
            </div>
            <button class="upgrade-button ${!canPurchase && !isMaxLevel ? 'disabled' : ''} ${isMaxLevel ? 'max-level' : ''}"
                 data-key="${upgrade.key}"
                 ${canPurchase && !isMaxLevel ? '' : 'disabled'}>
                ${isMaxLevel ? '완성' : !upgrade.isUnlocked ? '잠김' : `${upgrade.costPerLevel}pt`}
            </button>
        `;

        // 버튼 이벤트
        const btn = el.querySelector('.upgrade-button');
        if (canPurchase && !isMaxLevel) {
            btn.addEventListener('click', () => {
                this.rebirthSystem.purchaseUpgrade(upgrade.key);
            });
        }

        return el;
    }

    /**
     * 해금 조건 텍스트 생성
     */
    getUnlockConditionText(key) {
        const def = this.rebirthSystem.upgradeDefinitions[key];
        if (!def) return '';
        
        if (def.requires && def.requires.length === 0 && def.isHidden) {
            return '모든 업그레이드 완성 시 해금';
        }
        
        if (def.requires && def.requires.length > 0) {
            return def.requires.map(r => {
                const rDef = this.rebirthSystem.upgradeDefinitions[r];
                // 해금된 업그레이드면 이름 표시, 아니면 ???
                const isUnlocked = this.rebirthSystem.isUpgradeUnlocked(r);
                return isUnlocked ? (rDef ? rDef.name : r) : '???';
            }).join(' + ');
        }
        
        return '';
    }

    /**
     * 업그레이드 아이템 생성
     */
    createUpgradeItem(key, def) {
        const isGold = this.currentTab === 'gold';
        const level = isGold 
            ? this.gameState.player.goldUpgrades[key]
            : this.gameState.player.statUpgrades[key];
        const item = document.createElement('div');
        item.className = 'upgrade-item';

        const isLocked = def.unlockCondition && !def.unlockCondition();
        const isMaxLevel = def.maxLevel !== null && level >= def.maxLevel;
        
        const cost = isLocked || isMaxLevel ? null : (isGold ? this.calculateGoldCost(key, def) : def.statCost);
        const hasCurrency = isGold 
            ? this.gameState.inventory.gold >= cost 
            : this.gameState.player.statPoints >= def.statCost;

        // 현재 값과 다음 값 계산
        const currentValue = def.getValue(level);
        const nextLevel = isMaxLevel ? level : level + 1;
        const nextValue = isMaxLevel ? '' : def.getValue(nextLevel);

        item.innerHTML = `
            <div class="upgrade-info">
                <span class="upgrade-name">${def.name}</span>
                <span class="upgrade-level">Lv.${level}</span>
            </div>
            <div class="upgrade-stats">
                <span class="upgrade-current">${currentValue}</span>
                <span class="upgrade-next">${isMaxLevel ? '' : '→ ' + nextValue}</span>
            </div>
            <button class="upgrade-button ${isLocked ? 'locked' : ''} ${isMaxLevel ? 'max-level' : ''} ${!hasCurrency && !isMaxLevel && !isLocked ? 'disabled' : ''}"
                 data-key="${key}"
                 ${hasCurrency && !isMaxLevel && !isLocked ? '' : 'disabled'}>
                ${isLocked ? '🔒 해금 필요' : isMaxLevel ? '최대 레벨' : isGold ? `${cost}G` : `${cost}pt`}
            </button>
        `;

        // 버튼 이벤트
        const btn = item.querySelector('.upgrade-button');
        if (!isLocked && !isMaxLevel && hasCurrency) {
            btn.addEventListener('click', () => {
                if (isGold) {
                    this.purchaseGoldUpgrade(key, def);
                } else {
                    this.purchaseStatUpgrade(key, def);
                }
            });
        }

        return item;
    }

    /**
     * 골드 업그레이드 비용 계산
     * 매레벨 5% 증가, 10레벨마다 (10→11, 20→21...) 1.5배
     */
    calculateGoldCost(key, def) {
        const level = this.gameState.player.goldUpgrades[key];
        const baseCost = def.goldCostBase;
        
        // 레벨 0이면 기본비용
        if (level === 0) {
            return baseCost;
        }
        
        // 현재 비용 계산 (레벨 0부터 레벨 up까지 시뮬레이션)
        let currentCost = baseCost;
        
        for (let i = 1; i <= level; i++) {
            // 10레벨의 배수일 때 (10→11, 20→21, 30→32...)
            if (i % 10 === 0) {
                // 이전비용 × 1.5, 10의 자리 올림
                currentCost = Math.ceil(currentCost * 1.5);
                currentCost = Math.ceil(currentCost / 10) * 10;
            } else {
                // 1~9, 11~19, 21~29...: 이전비용 + (이전비용 × 5%), 10의 자리 올림
                const increase = currentCost * 0.05;
                currentCost = Math.ceil(currentCost + increase);
                currentCost = Math.ceil(currentCost / 10) * 10;
            }
        }
        
        return Math.max(currentCost, 10);
    }

    /**
     * 골드 업그레이드 구매
     */
    purchaseGoldUpgrade(key, def) {
        const cost = this.calculateGoldCost(key, def);

        if (this.gameState.inventory.gold < cost) {
            gameEventBus.emit(GAME_EVENTS.UPGRADE_INSUFFICIENT_GOLD, { required: cost, current: this.gameState.inventory.gold });
            return;
        }

        if (def.maxLevel !== null && this.gameState.player.goldUpgrades[key] >= def.maxLevel) {
            gameEventBus.emit(GAME_EVENTS.UPGRADE_MAX_LEVEL, { key });
            return;
        }

        this.gameState.spendGold(cost);
        this.gameState.player.goldUpgrades[key]++;
        this.gameState.recalculateDerivedStats();

        gameEventBus.emit(GAME_EVENTS.UPGRADE_PURCHASED, {
            key,
            level: this.gameState.player.goldUpgrades[key],
            cost,
            type: 'gold'
        });

        this.updateDisplay();
        this.renderUpgradeGrid();
    }

    /**
     * 스탯포인트 업그레이드 구매
     */
    purchaseStatUpgrade(key, def) {
        if (this.gameState.player.statPoints < def.statCost) {
            gameEventBus.emit(GAME_EVENTS.UPGRADE_INSUFFICIENT_POINTS, {
                required: def.statCost,
                current: this.gameState.player.statPoints
            });
            return;
        }

        if (def.maxLevel !== null && this.gameState.player.statUpgrades[key] >= def.maxLevel) {
            gameEventBus.emit(GAME_EVENTS.UPGRADE_MAX_LEVEL, { key });
            return;
        }

        this.gameState.player.statPoints -= def.statCost;
        this.gameState.player.statUpgrades[key]++;
        this.gameState.recalculateDerivedStats();

        gameEventBus.emit(GAME_EVENTS.UPGRADE_PURCHASED, {
            key,
            level: this.gameState.player.statUpgrades[key],
            cost: def.statCost,
            type: 'stat'
        });

        this.updateDisplay();
        this.renderUpgradeGrid();
    }

    /**
     * 디스플레이 업데이트
     */
    updateDisplay() {
        const goldEl = document.getElementById('upgrade-gold-display');
        const pointsEl = document.getElementById('upgrade-points-display');
        const gemsEl = document.getElementById('upgrade-gems-display');
        
        // 골드 표시
        if (goldEl) {
            if (this.currentTab === 'gold') {
                const gold = this.gameState.inventory.gold || 0;
                goldEl.textContent = `💰 ${gold.toLocaleString()}`;
                goldEl.style.display = 'flex';
            } else {
                goldEl.style.display = 'none';
            }
        }
        
        // 스탯 포인트/보너스 포인트 표시
        if (pointsEl) {
            if (this.currentTab === 'gold') {
                pointsEl.style.display = 'none';
            } else if (this.currentTab === 'stat') {
                pointsEl.textContent = `⭐ ${this.gameState.player.statPoints || 0}`;
                pointsEl.style.display = 'flex';
            } else if (this.currentTab === 'gem') {
                pointsEl.style.display = 'none';
            } else {
                pointsEl.textContent = `💎 ${this.gameState.rebirth.bonusPoints || 0}`;
                pointsEl.style.display = 'flex';
            }
        }
        
        // 보석 표시 (gem 탭일 때)
        if (gemsEl) {
            if (this.currentTab === 'gem') {
                const gems = this.gameState.inventory.gems || 0;
                gemsEl.textContent = `💎 ${gems.toLocaleString()}`;
                gemsEl.style.display = 'flex';
            } else {
                gemsEl.style.display = 'none';
            }
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
        if (num >= 1000000) return (num / 1000000).toFixed(1) + 'M';
        if (num >= 1000) return (num / 1000).toFixed(1) + 'K';
        return num.toString();
    }
}

export { UpgradeUI };