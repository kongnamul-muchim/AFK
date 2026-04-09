/**
 * GemShopUI - 보석 상점 UI
 * 보석으로 버프 구매
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameLogger } from '../core/Logger.js';

class GemShopUI {
    constructor(gameState, dailyMissionSystem) {
        this.gameState = gameState;
        this.dailyMissionSystem = dailyMissionSystem;
        
        // 상점 아이템 정의
        this.shopItems = [
            {
                id: 'attack_double',
                name: '공격력 2배',
                description: '30분간 공격력 2배',
                cost: 3,
                duration: 30,
                buffType: 'attackDouble',
                icon: '⚔️'
            },
            {
                id: 'hp_double',
                name: '체력 2배',
                description: '30분간 최대 체력 2배',
                cost: 3,
                duration: 30,
                buffType: 'hpDouble',
                icon: '❤️'
            },
            {
                id: 'gold_double',
                name: '골드 2배 드롭',
                description: '30분간 골드 드롭 2배',
                cost: 5,
                duration: 30,
                buffType: 'goldDouble',
                icon: '💰'
            },
            {
                id: 'exp_double',
                name: '경험치 2배',
                description: '30분간 경험치 2배',
                cost: 5,
                duration: 30,
                buffType: 'expDouble',
                icon: '⭐'
            }
        ];
    }

    /**
     * 초기화
     */
    init() {
        this.setupModal();
        this.renderShop();
        this.startBuffTimer();
    }

    /**
     * 모달 설정
     */
    setupModal() {
        const modal = document.getElementById('gem-shop-modal');
        const btn = document.getElementById('btn-gem-shop');
        const closeBtn = document.getElementById('btn-close-gem-shop');

        if (btn) {
            btn.addEventListener('click', () => {
                modal.style.display = 'flex';
                this.updateDisplay();
                this.renderShop();
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
        gameEventBus.on(GAME_EVENTS.BUFF_ACTIVATED, () => {
            this.renderShop();
            this.renderBuffs();
        });

        gameEventBus.on(GAME_EVENTS.INVENTORY_GOLD_CHANGED, () => {
            this.updateDisplay();
        });
    }

    /**
     * 버프 타이머 시작
     */
    startBuffTimer() {
        this.renderBuffs();
        setInterval(() => {
            this.renderBuffs();
        }, 1000);
    }

    /**
     * 버프 렌더링 (메인 화면)
     */
    renderBuffs() {
        const container = document.getElementById('buff-display');
        if (!container) return;

        container.innerHTML = '';

        const buffs = [
            { type: 'attackDouble', icon: '⚔️', name: '공격력 2배' },
            { type: 'hpDouble', icon: '❤️', name: '체력 2배' },
            { type: 'goldDouble', icon: '💰', name: '골드 2배' },
            { type: 'expDouble', icon: '⭐', name: '경험치 2배' }
        ];

        buffs.forEach(buff => {
            if (this.dailyMissionSystem.hasActiveBuff(buff.type)) {
                const el = this.createBuffElement(buff);
                container.appendChild(el);
            }
        });
    }

    /**
     * 버프 요소 생성
     */
    createBuffElement(buff) {
        const el = document.createElement('div');
        el.className = 'buff-item';

        const remaining = this.getRemainingTime(buff.type);
        const minutes = Math.floor(remaining / 60);
        const seconds = remaining % 60;

        el.innerHTML = `
            <div class="buff-item-icon">${buff.icon}</div>
            <div class="buff-item-timer">${minutes}:${seconds.toString().padStart(2, '0')}</div>
            <div class="buff-item-name">${buff.name}</div>
        `;

        return el;
    }

    /**
     * 남은 시간 계산 (초)
     */
    getRemainingTime(buffType) {
        const endTime = this.dailyMissionSystem.gameState.dailyMissions.buffs[buffType];
        const remaining = Math.max(0, Math.floor((endTime - Date.now()) / 1000));
        return remaining;
    }

    /**
     * 상점 렌더링
     */
    renderShop() {
        const grid = document.getElementById('gem-shop-grid');
        if (!grid) return;

        grid.innerHTML = '';

        this.shopItems.forEach(item => {
            const el = this.createShopItem(item);
            grid.appendChild(el);
        });
    }

    /**
     * 상점 아이템 생성
     */
    createShopItem(item) {
        const el = document.createElement('div');
        el.className = 'gem-shop-item';

        const gems = this.gameState.inventory.gems;
        const hasActiveBuff = this.dailyMissionSystem.hasActiveBuff(item.buffType);
        const canAfford = gems >= item.cost;

        el.innerHTML = `
            <div class="shop-item-icon">${item.icon}</div>
            <div class="shop-item-info">
                <div class="shop-item-name">${item.name}</div>
                <div class="shop-item-desc">${item.description}</div>
                ${hasActiveBuff ? '<div class="shop-item-status active">활성화됨</div>' : ''}
            </div>
            <button class="gem-buy-btn ${!canAfford || hasActiveBuff ? 'disabled' : ''}"
                 data-item-id="${item.id}"
                 ${canAfford && !hasActiveBuff ? '' : 'disabled'}>
                ${item.cost} 💎
            </button>
        `;

        // 버튼 이벤트
        const btn = el.querySelector('.gem-buy-btn');
        if (canAfford && !hasActiveBuff) {
            btn.addEventListener('click', () => {
                this.purchaseBuff(item);
            });
        }

        return el;
    }

    /**
     * 버프 구매
     */
    purchaseBuff(item) {
        if (this.gameState.inventory.gems < item.cost) {
            gameLogger.warn('Not enough gems');
            return;
        }

        this.gameState.inventory.gems -= item.cost;
        this.dailyMissionSystem.activateBuff(item.buffType, item.duration);

        gameLogger.info(`Purchased buff: ${item.name}`);

        this.updateDisplay();
        this.renderShop();
    }

    /**
     * 디스플레이 업데이트
     */
    updateDisplay() {
        const gemsEl = document.getElementById('gem-shop-gems');
        if (gemsEl) {
            gemsEl.textContent = this.gameState.inventory.gems;
        }
    }
}

export { GemShopUI };
