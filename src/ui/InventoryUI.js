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
        this.setupItemAddedListener();
        this.setupBatchSynthesizeButton();
        this.centerInventory();
    }

    /**
     * 아이템 추가 이벤트 리스너 설정 (인벤토리가 열려있을 때 자동 새로고침)
     */
    setupItemAddedListener() {
        gameEventBus.on(GAME_EVENTS.INVENTORY_ITEM_ADDED, () => {
            // 인벤토리 모달이 열려있을 때만 새로고침
            const modal = document.getElementById('inventory-modal');
            if (modal && modal.style.display !== 'none') {
                this.renderInventory();
            }
        });
    }

    /**
     * 일괄합성 버튼 설정
     */
    setupBatchSynthesizeButton() {
        const batchBtn = document.getElementById('batch-synthesize-btn');
        if (batchBtn) {
            batchBtn.addEventListener('click', () => {
                this.handleBatchSynthesize();
            });
        }
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
        const batchBtn = document.getElementById('batch-synthesize-btn');
        
        if (!container) return;
        
        // 런타임에 키 통일 (숫자/문자열 중복 방지)
        this.gameState.normalizeInventoryKeys();
        
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
        
        // 일괄합성 버튼 상태 업데이트
        if (batchBtn) {
            const synthesizable = this.inventorySystem.getSynthesizableItemsByType(this.currentTab);
            if (synthesizable.length > 0) {
                batchBtn.disabled = false;
                batchBtn.style.opacity = '1';
                batchBtn.style.cursor = 'pointer';
                
                // 합성 가능한 아이템 수 표시
                const totalPossible = synthesizable.reduce((sum, item) => sum + item.maxPossibleSyntheses, 0);
                batchBtn.title = `${synthesizable.length}종류 합성 가능 (총 ${totalPossible}회)`;
            } else {
                batchBtn.disabled = true;
                batchBtn.style.opacity = '0.5';
                batchBtn.style.cursor = 'not-allowed';
                batchBtn.title = '합성 가능한 아이템이 없습니다';
            }
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
        
        // 보유 확인 (현재 개수) - 숫자/문자열 키 모두 대응
        const itemId = item.id;
        const ownedByNumber = this.gameState.inventory.items.get(itemId);
        const ownedByString = this.gameState.inventory.items.get(itemId.toString());
        const owned = ownedByNumber || ownedByString;
        
        gameLogger.debug(`[createItemSlot] ${item.name} (id=${itemId}):`);
        gameLogger.debug(`  - get(${itemId}):`, ownedByNumber ? ownedByNumber.count : 'null');
        gameLogger.debug(`  - get("${itemId}"):`, ownedByString ? ownedByString.count : 'null');
        gameLogger.debug(`  - owned.count:`, owned ? owned.count : 0);
        
        // 발견 확인 (영구 해제) - discoveredItems 에 있으면 항상 활성화
        const discovered = this.gameState.inventory.discoveredItems.has(itemId.toString())
                        || this.gameState.inventory.discoveredItems.has(itemId);
        
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
            // 한 번 획득했지만 현재 개수 0 (도감 해제됨 - 항상 활성화, 장착 가능!)
            slot.classList.add('discovered', item.rarity);
            slot.dataset.count = 0;
            slot.dataset.itemName = item.name;
            slot.innerHTML = `
                <span class="item-name">${item.name}</span>
                <span class="item-count">x0</span>
            `;
            
            // 툴팁 + 장착 이벤트 (count=0이어도 장착 가능)
            slot.addEventListener('mouseenter', (e) => this.showTooltip(item, e));
            slot.addEventListener('mouseleave', () => this.hideTooltip());
            slot.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                this.handleEquip(item);
            });
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
        
        // 숫자/문자열 키 모두 대응
        const itemId = item.id;
        const owned = this.gameState.inventory.items.get(itemId) 
                   || this.gameState.inventory.items.get(itemId.toString());
        const discovered = this.gameState.inventory.discoveredItems.has(itemId.toString())
                        || this.gameState.inventory.discoveredItems.has(itemId);
        const count = owned ? owned.count : 0;
        
        document.getElementById('tooltip-name').textContent = item.name;
        
        // discovered 상태에 따라 표시
        let statusText;
        if (count > 0) {
            statusText = `x${count}`;
        } else if (discovered) {
            statusText = 'x0 (발견)';
        } else {
            statusText = '미획득';
        }
        
        document.getElementById('tooltip-grade').textContent = 
            `${this.getRarityName(item.rarity)} · ${statusText}`;
        
        // 스탯 정보
        const statsHtml = this.formatStats(item.stats);
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
        // 숫자/문자열 키 모두 대응
        const itemId = item.id;
        const owned = this.gameState.inventory.items.get(itemId) 
                   || this.gameState.inventory.items.get(itemId.toString());
        const discovered = this.gameState.inventory.discoveredItems.has(itemId.toString())
                        || this.gameState.inventory.discoveredItems.has(itemId);
        
        // 보유한 아이템이 있으면 장착 가능 (discoveredItems 없이도 OK)
        if (!owned && !discovered) {
            gameLogger.warn('Cannot equip: item not discovered');
            return;
        }
        
        // 장착
        this.gameState.player.equipment[item.type] = {
            itemId: item.id,
            name: item.name,
            grade: item.grade,
            stats: item.stats,
            rarity: item.rarity,
            type: item.type
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
     * 탭별 일괄 합성
     */
    handleBatchSynthesize() {
        const result = this.inventorySystem.synthesizeAllByType(this.currentTab);
        
        if (result.successCount === 0) {
            this.showToast('합성 가능한 아이템이 없습니다');
            return;
        }
        
        // UI 즉시 갱신
        this.renderInventory();
        this.updateEquipmentPanel();
        
        // 결과 표시
        this.showBatchSynthesizeResult(result);
    }

    /**
     * 일괄합성 결과 표시
     * @param {Object} result - 합성 결과 객체
     */
    showBatchSynthesizeResult(result) {
        // 결과 오버레이 생성
        const overlay = document.createElement('div');
        overlay.className = 'batch-synthesize-result';
        overlay.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: rgba(0, 0, 0, 0.7);
            display: flex;
            justify-content: center;
            align-items: center;
            z-index: 10000;
        `;
        
        const panel = document.createElement('div');
        panel.style.cssText = `
            background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
            border: 2px solid #ffd700;
            border-radius: 16px;
            padding: 2rem;
            min-width: 400px;
            max-width: 600px;
            max-height: 80vh;
            overflow-y: auto;
            box-shadow: 0 0 30px rgba(255, 215, 0, 0.3);
        `;
        
        // 제목
        const title = document.createElement('h2');
        title.textContent = `✨ 일괄 합성 결과 (${this.getTabName(this.currentTab)})`;
        title.style.cssText = `
            text-align: center;
            color: #ffd700;
            margin: 0 0 1.5rem 0;
            font-size: 1.5rem;
            text-shadow: 0 0 10px rgba(255, 215, 0, 0.5);
        `;
        panel.appendChild(title);
        
        // 요약 정보
        const summary = document.createElement('div');
        summary.style.cssText = `
            text-align: center;
            color: #aaa;
            margin-bottom: 1rem;
            font-size: 0.9rem;
        `;
        summary.innerHTML = `
            총 <span style="color: #ffd700;">${result.successCount}회</span> 합성 성공<br>
            소모된 재료: <span style="color: #ffd700;">${result.totalMaterialUsed}개</span>
        `;
        panel.appendChild(summary);
        
        // 구분선
        const divider = document.createElement('hr');
        divider.style.cssText = `
            border: none;
            border-top: 1px solid #333;
            margin: 1rem 0;
        `;
        panel.appendChild(divider);
        
        // 결과 목록
        const resultList = document.createElement('div');
        resultList.style.cssText = `
            display: flex;
            flex-direction: column;
            gap: 0.5rem;
        `;
        
        result.results.forEach(r => {
            const resultItem = document.createElement('div');
            resultItem.style.cssText = `
                display: flex;
                align-items: center;
                justify-content: space-between;
                padding: 0.75rem 1rem;
                background: rgba(255, 255, 255, 0.05);
                border-radius: 8px;
                border-left: 3px solid ${this.getRarityColor(r.result.rarity)};
            `;
            
            const rarityName = this.getRarityName(r.result.rarity);
            resultItem.innerHTML = `
                <div>
                    <span style="color: ${this.getRarityColor(r.result.rarity)}; font-weight: bold;">
                        ${r.result.name}
                    </span>
                    <span style="color: #888; font-size: 0.85rem;">
                        (Grade ${r.result.grade} · ${rarityName})
                    </span>
                </div>
                <div style="color: #ffd700; font-weight: bold; font-size: 1.1rem;">
                    x${r.count}
                </div>
            `;
            resultList.appendChild(resultItem);
        });
        
        panel.appendChild(resultList);
        
        // 닫기 버튼
        const closeBtn = document.createElement('button');
        closeBtn.textContent = '닫기';
        closeBtn.style.cssText = `
            display: block;
            margin: 1.5rem auto 0;
            padding: 0.75rem 2rem;
            background: linear-gradient(135deg, #ffd700 0%, #ff8c00 100%);
            border: none;
            border-radius: 8px;
            color: #1a1a2e;
            font-weight: bold;
            font-size: 1rem;
            cursor: pointer;
            transition: transform 0.2s;
        `;
        closeBtn.addEventListener('mouseenter', () => {
            closeBtn.style.transform = 'scale(1.05)';
        });
        closeBtn.addEventListener('mouseleave', () => {
            closeBtn.style.transform = 'scale(1)';
        });
        closeBtn.addEventListener('click', () => {
            overlay.remove();
        });
        panel.appendChild(closeBtn);
        
        overlay.appendChild(panel);
        
        // 오버레이 클릭 시 닫기
        overlay.addEventListener('click', (e) => {
            if (e.target === overlay) {
                overlay.remove();
            }
        });
        
        document.body.appendChild(overlay);
    }

    /**
     * 희귀도 색상 반환
     * @param {string} rarity 
     * @returns {string}
     */
    getRarityColor(rarity) {
        const colors = {
            common: '#a0a0a0',
            rare: '#4169e1',
            epic: '#a335ee',
            legendary: '#ff8c00',
            mythic: '#ff1493'
        };
        return colors[rarity] || '#ffffff';
    }

    /**
     * 탭 이름 반환
     * @param {string} tab 
     * @returns {string}
     */
    getTabName(tab) {
        const names = {
            weapon: '무기',
            armor: '방어구',
            boots: '신발',
            accessory: '액세서리'
        };
        return names[tab] || tab;
    }

    /**
     * 장착 패널 업데이트
     */
    updateEquipmentPanel() {
        const equipment = this.gameState.player.equipment;
        
        // 각 슬롯 업데이트
        ['weapon', 'armor', 'accessory', 'boots'].forEach(slot => {
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
        let bonusAtk = 0, bonusDef = 0, bonusHp = 0, bonusMoveSpeed = 0;
        
        Object.values(equipment).forEach(item => {
            if (item && item.stats) {
                const stats = item.stats;
                if (stats.attackBonus) bonusAtk += stats.attackBonus;
                if (stats.defenseBonus) bonusDef += stats.defenseBonus;
                if (stats.hpBonus) bonusHp += stats.hpBonus;
                if (stats.moveSpeed) bonusMoveSpeed += stats.moveSpeed;
            }
        });
        
        document.getElementById('bonus-atk').textContent = `+${Math.floor(bonusAtk)}%`;
        document.getElementById('bonus-def').textContent = `+${Math.floor(bonusDef)}%`;
        document.getElementById('bonus-hp').textContent = `+${Math.floor(bonusHp)}%`;
        
        // 이동속도 표시 (ID가 있다면)
        const moveSpeedEl = document.getElementById('bonus-movespeed');
        if (moveSpeedEl) {
            moveSpeedEl.textContent = `+${Math.floor(bonusMoveSpeed)}`;
        }
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
        if (stats.attackBonus) lines.push(`<div>공격력 +${stats.attackBonus}%</div>`);
        if (stats.defenseBonus) lines.push(`<div>방어력 +${stats.defenseBonus}%</div>`);
        if (stats.moveSpeed) lines.push(`<div>이동속도 +${stats.moveSpeed}</div>`);
        if (stats.hpBonus) lines.push(`<div>체력 +${stats.hpBonus}%</div>`);
        
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
