/**
 * InventoryUI - 인벤토리 UI 관리
 * 탭, 툴팁, 장착, 합성, 도감 시스템
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameDataLoader } from '../data-parser/DataLoader.js';
import { gameLogger } from '../core/Logger.js';

class InventoryUI {
    constructor(gameState, inventorySystem) {
        this.gameState = gameState;
        this.inventorySystem = inventorySystem;
        this.currentTab = 'weapon'; // 기본 탭: 무기
        this.selectedItemId = null;
        this.tooltip = null;
    }

    /**
     * 초기화
     */
    init() {
        this.tooltip = document.getElementById('item-tooltip');
        this.setupTabs();
        this.setupContextMenu();
        this.setupTooltip();
        this.centerInventory();
    }

    /**
     * 인벤토리 중앙 정렬
     */
    centerInventory() {
        const modal = document.querySelector('.inventory-modal-content');
        if (modal) {
            modal.style.margin = '0 auto';
        }
    }

    /**
     * 탭 설정
     */
    setupTabs() {
        document.querySelectorAll('.inventory-tabs .tab-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                // 탭 전환
                document.querySelectorAll('.inventory-tabs .tab-btn').forEach(b => {
                    b.classList.remove('active');
                });
                btn.classList.add('active');
                
                this.currentTab = btn.dataset.tab;
                this.renderInventory();
            });
        });
    }

    /**
     * 우클릭 메뉴 방지 (합성용)
     */
    setupContextMenu() {
        const grid = document.getElementById('inventory-items');
        if (grid) {
            grid.addEventListener('contextmenu', (e) => {
                e.preventDefault();
                // 우클릭 시 합성
                const itemSlot = e.target.closest('.item-slot');
                if (itemSlot && itemSlot.dataset.itemId) {
                    this.handleSynthesize(parseInt(itemSlot.dataset.itemId));
                }
            });
        }
    }

    /**
     * 툴팁 설정
     */
    setupTooltip() {
        // 마우스 이동 시 툴팁 위치 업데이트
        document.addEventListener('mousemove', (e) => {
            if (this.tooltip && this.tooltip.style.display !== 'none') {
                const x = e.clientX + 15;
                const y = e.clientY + 15;
                
                // 화면 밖으로 나가지 않게
                const rect = this.tooltip.getBoundingClientRect();
                const maxX = window.innerWidth - rect.width;
                const maxY = window.innerHeight - rect.height;
                
                this.tooltip.style.left = `${Math.min(x, maxX)}px`;
                this.tooltip.style.top = `${Math.min(y, maxY)}px`;
            }
        });
    }

    /**
     * 인벤토리 렌더링
     */
    renderInventory() {
        const container = document.getElementById('inventory-items');
        const goldDisplay = document.getElementById('inventory-gold');
        
        if (!container) return;
        
        container.innerHTML = '';
        container.className = 'inventory-items-wrapper';
        
        // 모든 아이템 데이터 (도감) - CSV 에서 로드
        const allItems = gameDataLoader.get('items') || [];
        
        // 현재 탭의 아이템만 필터링
        const tabItems = allItems.filter(item => item.type === this.currentTab);
        
        // 아이템을 베이스 이름별로 그룹화
        const groupedItems = this.groupItemsByBase(tabItems);
        
        // 그룹별로 행 생성 (1 행 = 5 희귀도)
        Object.keys(groupedItems).forEach(baseName => {
            const group = groupedItems[baseName];
            const row = this.createRarityRow(baseName, group);
            container.appendChild(row);
        });
        
        // 골드 업데이트
        if (goldDisplay) {
            goldDisplay.textContent = this.formatNumber(this.gameState.inventory.gold);
        }
        
        // 장착 아이템 업데이트
        this.updateEquipmentPanel();
    }

    /**
     * 아이템을 베이스 이름별로 그룹화
     * @param {Array} items 
     * @returns {Object}
     */
    groupItemsByBase(items) {
        const groups = {};
        
        items.forEach(item => {
            // name 으로 그룹화 (rusty_sword 는 그대로 사용)
            const baseName = item.name;
            
            if (!groups[baseName]) {
                groups[baseName] = [];
            }
            groups[baseName].push(item);
        });
        
        // 각 그룹을 희귀도 순서대로 정렬
        const rarityOrder = ['common', 'rare', 'epic', 'legendary', 'mythic'];
        Object.keys(groups).forEach(key => {
            groups[key].sort((a, b) => rarityOrder.indexOf(a.rarity) - rarityOrder.indexOf(b.rarity));
        });
        
        return groups;
    }

    /**
     * 희귀도 행 생성 (1 행 = 5 개)
     * @param {string} baseName 
     * @param {Array} items 
     * @returns {HTMLElement}
     */
    createRarityRow(baseName, items) {
        const row = document.createElement('div');
        row.className = 'rarity-row';
        row.style.cssText = 'display: flex; gap: 0.25rem; margin-bottom: 0.5rem;';
        
        // 5 희귀도 슬롯 생성
        items.forEach(item => {
            const slot = this.createItemSlot(item);
            slot.style.flex = '1';
            row.appendChild(slot);
        });
        
        return row;
    }

    /**
     * 아이템 슬롯 생성
     * @param {Object} item 
     * @returns {HTMLElement}
     */
    createItemSlot(item) {
        const slot = document.createElement('div');
        slot.className = 'item-slot';
        slot.dataset.itemId = item.id;
        
        // 보유 확인 (현재 개수)
        const owned = this.gameState.inventory.items.get(item.id.toString());
        
        // 발견 확인 (영구 해제) - discoveredItems 에 있으면 항상 활성화
        const discovered = this.gameState.inventory.discoveredItems.has(item.id.toString());
        
        gameLogger.debug(`[createItemSlot] ${item.name}: owned=${owned ? owned.count : 0}, discovered=${discovered}`);
        
        if (owned && owned.count > 0) {
            // 현재 보유한 아이템 - 활성화
            slot.classList.add('has-item', item.rarity);
            slot.dataset.count = owned.count;
            slot.dataset.itemName = item.name;
            slot.innerHTML = `
                <span class="item-name">${item.name}</span>
                <span class="item-count">x${owned.count}</span>
            `;
            
            // 이벤트
            slot.addEventListener('mouseenter', (e) => this.showTooltip(item, e));
            slot.addEventListener('mouseleave', () => this.hideTooltip());
            slot.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                this.handleEquip(item);
            });
            slot.addEventListener('contextmenu', (e) => {
                e.preventDefault();
                e.stopPropagation();
                this.handleSynthesize(item.id);
            });
        } else if (discovered) {
            // 한 번 획득했지만 현재 개수 0 (도감 해제됨 - 항상 활성화)
            slot.classList.add('discovered', item.rarity);
            slot.innerHTML = `
                <span class="item-name">${item.name}</span>
                <span class="item-count">x0</span>
            `;
            
            slot.addEventListener('mouseenter', (e) => this.showTooltip(item, e));
            slot.addEventListener('mouseleave', () => this.hideTooltip());
        } else {
            // 미획득 아이템 (도감) - 잠금
            slot.classList.add('locked');
            slot.innerHTML = `<span class="item-name">${item.name}</span>`;
        }
        
        return slot;
    }

    /**
     * 툴팁 표시
     * @param {Object} item 
     * @param {MouseEvent} e 
     */
    showTooltip(item, e) {
        if (!this.tooltip) return;
        
        const owned = this.gameState.inventory.items.get(item.id.toString());
        const count = owned ? owned.count : 0;
        
        document.getElementById('tooltip-name').textContent = item.name;
        document.getElementById('tooltip-grade').textContent = 
            `${this.getRarityName(item.rarity)} · ${(owned && owned.count > 0) ? `x${count}` : '미획득'}`;
        
        // 스탯 정보
        const statsHtml = this.formatStats(item.stats_min);
        document.getElementById('tooltip-stats').innerHTML = statsHtml;
        
        this.tooltip.style.display = 'block';
        this.tooltip.style.left = `${e.clientX + 15}px`;
        this.tooltip.style.top = `${e.clientY + 15}px`;
    }

    /**
     * 툴팁 숨김
     */
    hideTooltip() {
        if (this.tooltip) {
            this.tooltip.style.display = 'none';
        }
    }

    /**
     * 아이템 장착
     * @param {Object} item 
     */
    handleEquip(item) {
        const owned = this.gameState.inventory.items.get(item.id.toString());
        if (!owned || owned.count < 1) {
            gameLogger.warn('Cannot equip: item not owned');
            return;
        }
        
        // 현재 장착 아이템 확인
        const currentEquip = this.gameState.player.equipment[item.type];
        
        // 장착
        this.gameState.player.equipment[item.type] = {
            itemId: item.id,
            name: item.name,
            stats: item.stats_min,
            rarity: item.rarity
        };
        
        // 이벤트
        gameEventBus.emit(GAME_EVENTS.PLAYER_STAT_CHANGED);
        
        gameLogger.info(`Equipped: ${item.name}`);
        
        // UI 업데이트
        this.renderInventory();
        this.updateEquipmentPanel();
        this.showToast(`${item.name} 장착!`);
    }

    /**
     * 아이템 합성
     * @param {number} itemId 
     */
    handleSynthesize(itemId) {
        const result = this.inventorySystem.synthesize(itemId);
        
        if (result) {
            this.showToast(`합성 성공! ${result.name}`);
            // UI 즉시 갱신
            this.renderInventory();
            this.updateEquipmentPanel();
        } else {
            this.showToast('합성 실패: 재료가 부족합니다');
        }
    }

    /**
     * 장착 패널 업데이트
     */
    updateEquipmentPanel() {
        const equipment = this.gameState.player.equipment;
        
        // 각 슬롯 업데이트
        ['weapon', 'armor', 'boots', 'accessory'].forEach(slot => {
            const slotEl = document.getElementById(`equip-${slot}`);
            const item = equipment[slot];
            
            if (item) {
                slotEl.textContent = this.getSlotIcon(slot);
                slotEl.classList.add('has-item');
                slotEl.className = `slot-item has-item ${item.rarity}`;
            } else {
                slotEl.textContent = '';
                slotEl.className = 'slot-item';
            }
        });
        
        // 스탯 가중치 업데이트
        this.updateStatsBonus();
    }

    /**
     * 스탯 가중치 업데이트
     */
    updateStatsBonus() {
        const equipment = this.gameState.player.equipment;
        let bonusAtk = 0, bonusDef = 0, bonusHp = 0, bonusCrit = 0;
        
        Object.values(equipment).forEach(item => {
            if (item && item.stats) {
                const stats = item.stats;
                if (stats.str) bonusAtk += stats.str * 2;
                if (stats.vit) {
                    bonusDef += stats.vit * 0.5;
                    bonusHp += stats.vit * 10;
                }
                if (stats.agi) bonusCrit += stats.agi * 0.5;
            }
        });
        
        document.getElementById('bonus-atk').textContent = `+${Math.floor(bonusAtk)}`;
        document.getElementById('bonus-def').textContent = `+${Math.floor(bonusDef)}`;
        document.getElementById('bonus-hp').textContent = `+${Math.floor(bonusHp)}`;
        document.getElementById('bonus-crit').textContent = `+${bonusCrit.toFixed(1)}%`;
    }

    /**
     * 슬롯 아이콘
     * @param {string} slot 
     * @returns {string}
     */
    getSlotIcon(slot) {
        const icons = {
            weapon: '⚔️',
            armor: '🛡️',
            boots: '👢',
            accessory: '💍'
        };
        return icons[slot] || '❓';
    }

    /**
     * 희귀도 이름
     * @param {string} rarity 
     * @returns {string}
     */
    getRarityName(rarity) {
        const names = {
            common: '일반',
            rare: '희귀',
            epic: '영웅',
            legendary: '전설',
            mythic: '신화'
        };
        return names[rarity] || rarity;
    }

    /**
     * 스탯 포맷팅
     * @param {Object} stats 
     * @returns {string}
     */
    formatStats(stats) {
        if (!stats) return '<div>옵션 없음</div>';
        
        const lines = [];
        if (stats.str) lines.push(`<div>힘 +${stats.str}</div>`);
        if (stats.agi) lines.push(`<div>민첩 +${stats.agi}</div>`);
        if (stats.int) lines.push(`<div>지력 +${stats.int}</div>`);
        if (stats.vit) lines.push(`<div>체력 +${stats.vit}</div>`);
        
        return lines.join('') || '<div>옵션 없음</div>';
    }

    /**
     * 숫자 포맷팅
     * @param {number} num 
     * @returns {string}
     */
    formatNumber(num) {
        if (num >= 1000000) return (num / 1000000).toFixed(1) + 'M';
        if (num >= 1000) return (num / 1000).toFixed(1) + 'K';
        return num.toString();
    }

    /**
     * 토스트 메시지
     * @param {string} message 
     */
    showToast(message) {
        const toast = document.createElement('div');
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
        `;
        document.body.appendChild(toast);
        setTimeout(() => toast.remove(), 1500);
    }
}

export { InventoryUI };
