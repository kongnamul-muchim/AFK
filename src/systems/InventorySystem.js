/**
 * InventorySystem - 인벤토리 및 장비 합성 시스템
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameConfig } from '../config/GameConfig.js';
import { gameDataLoader } from '../data-parser/DataLoader.js';
import { gameLogger } from '../core/Logger.js';

class InventorySystem {
    /**
     * @param {GameState} gameState 
     */
    constructor(gameState) {
        this.gameState = gameState;
        this.synthesizeCount = gameConfig.inventory.synthesizeCount;
    }

    /**
     * 초기화
     */
    init() {
        gameLogger.debug('InventorySystem initialized');
    }

    /**
     * 아이템 추가 (중복 시 카운트 증가)
     * @param {Object} itemData 
     */
    addItem(itemData) {
        const { itemId, name, count = 1, grade, rarity, stats } = itemData;
        
        if (this.gameState.inventory.items.has(itemId)) {
            // 기존 아이템 - 카운트 증가
            const existing = this.gameState.inventory.items.get(itemId);
            existing.count += count;
            
            gameLogger.debug(`Item stack increased: ${name} x${existing.count}`);
        } else {
            // 새 아이템
            this.gameState.inventory.items.set(itemId, {
                itemId,
                name,
                count,
                grade,
                rarity,
                stats
            });
            
            gameLogger.debug(`New item added: ${name}`);
        }
        
        gameEventBus.emit(GAME_EVENTS.INVENTORY_ITEM_ADDED, {
            itemId,
            name,
            count,
            rarity
        });
    }

    /**
     * 아이템 제거
     * @param {number} itemId 
     * @param {number} count 
     * @returns {boolean}
     */
    removeItem(itemId, count = 1) {
        if (!this.gameState.inventory.items.has(itemId)) {
            return false;
        }
        
        const item = this.gameState.inventory.items.get(itemId);
        
        if (item.count > count) {
            item.count -= count;
        } else {
            this.gameState.inventory.items.delete(itemId);
        }
        
        gameEventBus.emit(GAME_EVENTS.INVENTORY_ITEM_REMOVED, {
            itemId,
            count
        });
        
        return true;
    }

    /**
     * 합성 가능 여부 확인
     * @param {number|string} itemId 
     * @returns {boolean}
     */
    canSynthesize(itemId) {
        const item = this.gameState.inventory.items.get(itemId.toString());
        if (!item) return false;
        
        return item.count >= this.synthesizeCount;
    }

    /**
     * 장비 합성 (5 개 → 다음 등급 1 개)
     * grade 1-10: 단순 강화 시스템
     * @param {number} itemId 
     * @returns {Object|null} 합성된 아이템 정보 또는 null
     */
    synthesize(itemId) {
        if (!this.canSynthesize(itemId)) {
            gameLogger.warn(`Cannot synthesize item ${itemId}: not enough count`);
            return null;
        }
        
        const item = this.gameState.inventory.items.get(itemId.toString());
        const nextGrade = item.grade + 1;
        
        // 최대 등급 확인 (10: silver_ring mythic 등)
        if (nextGrade > 10) {
            gameLogger.warn(`Item ${item.name} is already max grade`);
            return null;
        }
        
        // 다음 등급 아이템 찾기 (같은 이름 + 다음 grade)
        const items = gameDataLoader.get('items');
        const nextGradeItem = items.find(i => 
            i.name === item.name &&  // 같은 이름
            i.grade === nextGrade    // 다음 등급
        );
        
        if (!nextGradeItem) {
            gameLogger.warn(`No next grade item found for ${item.name} grade ${nextGrade}`);
            return null;
        }
        
        // 재료 아이템 5 개 제거
        this.removeItem(item.itemId || itemId, this.synthesizeCount);
        
        // 다음 등급 아이템 1 개 추가
        this.addItem({
            itemId: nextGradeItem.id,
            name: nextGradeItem.name,
            count: 1,
            grade: nextGradeItem.grade,
            rarity: nextGradeItem.rarity,
            stats: nextGradeItem.stats_min,
            type: nextGradeItem.type
        });
        
        // 이벤트
        gameEventBus.emit(GAME_EVENTS.INVENTORY_SYNTHESIZE, {
            materialId: itemId,
            resultId: nextGradeItem.id,
            resultName: nextGradeItem.name,
            resultGrade: nextGradeItem.grade
        });
        
        gameLogger.info(`Synthesized: ${item.name} grade.${item.grade} x5 → ${nextGradeItem.name} grade.${nextGradeItem.grade} x1`);
        
        return {
            id: nextGradeItem.id,
            name: nextGradeItem.name,
            grade: nextGradeItem.grade
        };
    }

    /**
     * 합성 가능한 아이템 목록 반환
     * @returns {Array}
     */
    getSynthesizableItems() {
        const result = [];
        
        this.gameState.inventory.items.forEach((item, itemId) => {
            if (item.count >= this.synthesizeCount) {
                result.push({
                    itemId,
                    name: item.name,
                    count: item.count,
                    grade: item.grade,
                    rarity: item.rarity
                });
            }
        });
        
        return result;
    }

    /**
     * 인벤토리 정리 (간단한 구현)
     */
    sortInventory() {
        // 등급순 정렬
        const sorted = new Map(
            [...this.gameState.inventory.items.entries()]
                .sort((a, b) => b[1].grade - a[1].grade)
        );
        this.gameState.inventory.items = sorted;
        gameLogger.debug('Inventory sorted');
    }

    /**
     * 정리
     */
    destroy() {
        // 정리할 작업 없음
    }
}

export { InventorySystem };
