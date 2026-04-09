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
        this.fixAllItemNames();  // 기존 아이템 이름 수정
    }

    /**
     * 모든 아이템 이름/타입 수정 (rusty_sword_2 → rusty_sword)
     */
    fixAllItemNames() {
        const items = gameDataLoader.get('items');
        if (!items) return;
        
        // itemId → item data 매핑 생성
        const itemDataMap = new Map();
        items.forEach(item => {
            itemDataMap.set(item.id.toString(), {
                name: item.name,
                type: item.type
            });
        });
        
        // 인벤토리 아이템 이름/타입 수정
        this.gameState.inventory.items.forEach((item, itemId) => {
            const correctData = itemDataMap.get(itemId);
            if (correctData) {
                if (item.name !== correctData.name) {
                    gameLogger.info(`Fixed item name: "${item.name}" → "${correctData.name}"`);
                    item.name = correctData.name;
                }
                if (item.type !== correctData.type) {
                    gameLogger.info(`Fixed item type: "${item.type}" → "${correctData.type}"`);
                    item.type = correctData.type;
                }
            }
        });
        
        gameLogger.debug('All item names/types fixed');
    }

    /**
     * 아이템 추가 (중복 시 카운트 증가)
     * @param {Object} itemData 
     */
    addItem(itemData) {
        const { itemId, name, count = 1, grade, rarity, stats, type } = itemData;
        const itemIdStr = itemId.toString();
        
        gameLogger.debug(`addItem: itemId=${itemIdStr}, name="${name}", grade=${grade}`);
        
        // 발견 아이템으로 등록 (영구 해제)
        this.gameState.inventory.discoveredItems.add(itemIdStr);
        
        if (this.gameState.inventory.items.has(itemIdStr)) {
            // 기존 아이템 - 카운트 증가
            const existing = this.gameState.inventory.items.get(itemIdStr);
            
            // 이름이 틀렸으면 수정 (rusty_sword_2 → rusty_sword)
            if (existing.name !== name) {
                gameLogger.debug(`Fixing item name: "${existing.name}" → "${name}"`);
                existing.name = name;
            }
            
            // type이 틀렸으면 수정
            if (type && existing.type !== type) {
                gameLogger.debug(`Fixing item type: "${existing.type}" → "${type}"`);
                existing.type = type;
            }
            
            existing.count += count;
            
            gameLogger.debug(`Item stack increased: ${name} x${existing.count}, stored.name="${existing.name}"`);
        } else {
            // 새 아이템
            const itemToStore = {
                itemId,
                name,
                count,
                grade,
                rarity,
                stats,
                type  // 합성 후 type 저장!
            };
            gameLogger.debug(`Storing new item:`, itemToStore);
            this.gameState.inventory.items.set(itemIdStr, itemToStore);
            
            gameLogger.debug(`New item added: ${name}`);
        }
        
        gameEventBus.emit(GAME_EVENTS.INVENTORY_ITEM_ADDED, {
            itemId: itemIdStr,
            name,
            count,
            rarity
        });
    }

    /**
     * 아이템 제거
     * @param {number|string} itemId 
     * @param {number} count 
     * @returns {boolean}
     */
    removeItem(itemId, count = 1) {
        const itemIdStr = itemId.toString();
        
        if (!this.gameState.inventory.items.has(itemIdStr)) {
            gameLogger.debug(`Item ${itemIdStr} not found for removal`);
            return false;
        }
        
        const item = this.gameState.inventory.items.get(itemIdStr);
        
        // 정확히 같은 수량일 때만 삭제 (count 가 0 이 되지 않도록)
        if (item.count === count) {
            this.gameState.inventory.items.delete(itemIdStr);
            gameLogger.debug(`Removed all x ${item.name}`);
        } else if (item.count > count) {
            item.count -= count;
            gameLogger.debug(`Removed ${count} x ${item.name}, remaining: ${item.count}`);
        } else {
            gameLogger.warn(`Not enough items: have ${item.count}, need ${count}`);
            return false;
        }
        
        gameEventBus.emit(GAME_EVENTS.INVENTORY_ITEM_REMOVED, {
            itemId: itemIdStr,
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
     * 타입별 최대 등급
     */
    getMaxGradeByType(type) {
        // CSV 데이터에서 실제 최대 grade 찾기
        const items = gameDataLoader.get('items');
        if (!items) return 10;
        
        // 해당 타입의 아이템들 중 최대 grade 반환
        const typeItems = items.filter(i => i.type === type);
        if (typeItems.length === 0) {
            gameLogger.warn(`[getMaxGradeByType] No items found for type: ${type}`);
            return 10; // 기본값
        }
        
        const maxGrade = Math.max(...typeItems.map(i => i.grade));
        gameLogger.debug(`[getMaxGradeByType] type=${type}, maxGrade=${maxGrade}`);
        return maxGrade;
    }

    /**
     * 장비 합성 (5 개 → 다음 등급 1 개)
     * grade 1-15: 단순 강화 시스템
     * 베이스 아이템 전환 지원 (rusty_sword → iron_sword → steel_sword)
     * @param {number|string} itemId 
     * @returns {Object|null} 합성된 아이템 정보 또는 null
     */
    synthesize(itemId) {
        // 문자열 키로 통일
        const itemIdStr = itemId.toString();
        
        if (!this.canSynthesize(itemIdStr)) {
            gameLogger.warn(`Cannot synthesize item ${itemId}: not enough count`);
            return null;
        }
        
        const item = this.gameState.inventory.items.get(itemIdStr);
        const nextGrade = item.grade + 1;
        
        // 타입별 최대 등급 확인
        const maxGrade = this.getMaxGradeByType(item.type);
        
        if (nextGrade > maxGrade) {
            gameLogger.warn(`${item.type} ${item.name} is already max grade (${maxGrade})`);
            return null;
        }
        
        // 다음 등급 아이템 찾기 (베이스 아이템 전환 지원)
        const nextGradeItem = this.findNextGradeItem(item.name, item.type, nextGrade);
        
        if (!nextGradeItem) {
            gameLogger.warn(`No next grade item found for "${item.name}" grade ${item.grade} → ${nextGrade}`);
            return null;
        }
        
        gameLogger.debug(`Found next grade: ${nextGradeItem.name} grade ${nextGradeItem.grade} (max: ${maxGrade})`);
        
        // 재료 아이템 5 개 제거 (문자열 키 사용!)
        const removed = this.removeItem(itemIdStr, this.synthesizeCount);
        gameLogger.debug(`Removed ${this.synthesizeCount} x ${item.name}, success: ${removed}`);
        
        // 다음 등급 아이템 1 개 추가
        const newItemData = {
            itemId: nextGradeItem.id,
            name: nextGradeItem.name,
            count: 1,
            grade: nextGradeItem.grade,
            rarity: nextGradeItem.rarity,
            stats: nextGradeItem.stats,
            type: nextGradeItem.type  // CSV 에서 읽은 정확한 타입
        };
        gameLogger.debug(`[addItem] id=${newItemData.itemId}, name="${newItemData.name}", type="${newItemData.type}", grade=${newItemData.grade}`);
        this.addItem(newItemData);
        gameLogger.debug(`Added new item: ${nextGradeItem.name} grade.${nextGradeItem.grade} type.${nextGradeItem.type}`);
        
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
     * 다음 등급 아이템 찾기 (베이스 아이템 전환 지원)
     * @param {string} currentName 
     * @param {string} type 
     * @param {number} nextGrade 
     * @returns {Object|null}
     */
    findNextGradeItem(currentName, type, nextGrade) {
        const items = gameDataLoader.get('items');
        
        // 디버깅 로그
        gameLogger.debug(`[findNextGradeItem] currentName="${currentName}", type="${type}", nextGrade=${nextGrade}`);
        gameLogger.debug(`[findNextGradeItem] Total items in CSV: ${items.length}`);
        
        // 1. 같은 이름 + 다음 등급 찾기
        const sameName = items.find(i => {
            const nameMatch = i.name === currentName;
            const gradeMatch = i.grade === nextGrade;
            if (nameMatch && gradeMatch) {
                gameLogger.debug(`[findNextGradeItem] Found same name: ${i.name} grade ${i.grade} (id:${i.id})`);
            }
            return nameMatch && gradeMatch;
        });
        
        if (sameName) {
            gameLogger.debug(`[findNextGradeItem] Returning same name item: ${sameName.name}`);
            return sameName;
        }
        
        gameLogger.debug(`[findNextGradeItem] No same name found, searching by type...`);
        
        // 2. 같은 타입 + 다음 등급 찾기 (베이스 아이템 전환)
        const byType = items.find(i => {
            const typeMatch = i.type === type;
            const gradeMatch = i.grade === nextGrade;
            if (typeMatch && gradeMatch) {
                gameLogger.debug(`[findNextGradeItem] Found type match: ${i.name} grade ${i.grade} (id:${i.id})`);
            }
            return typeMatch && gradeMatch;
        });
        
        if (!byType) {
            // 찾기 실패 - 자세한 정보 로그
            const availableGrades = items
                .filter(i => i.type === type)
                .map(i => `${i.name}(g.${i.grade})`)
                .join(', ');
            gameLogger.warn(`[findNextGradeItem] No item found! type="${type}", grade=${nextGrade}`);
            gameLogger.warn(`[findNextGradeItem] Available ${type} items: ${availableGrades}`);
        }
        
        return byType;
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
     * 탭별 일괄 합성
     * 해당 타입의 모든 합성 가능한 아이템을 자동으로 합성
     * @param {string} type - 'weapon', 'armor', 'boots', 'accessory'
     * @returns {Object} 합성 결과 { successCount, results: [{material, result, count}] }
     */
    synthesizeAllByType(type) {
        const results = [];
        let successCount = 0;
        let totalMaterialUsed = 0;
        
        // 현재 타입의 모든 아이템 중 합성 가능한 것 찾기
        const synthesizableItems = [];
        
        this.gameState.inventory.items.forEach((item, itemId) => {
            // 타입이 일치하고 합성 가능 개수 이상인 아이템
            if (item.type === type && item.count >= this.synthesizeCount) {
                // 최대 등급 확인
                const maxGrade = this.getMaxGradeByType(type);
                if (item.grade < maxGrade) {
                    synthesizableItems.push({
                        itemId,
                        item,
                        maxGrade
                    });
                }
            }
        });
        
        // 각 아이템에 대해 가능한 만큼 합성
        for (const { itemId, item, maxGrade } of synthesizableItems) {
            const itemIdStr = itemId.toString();
            
            // 다시 한번 확인 (다른 합성으로 인해 상태가 변했을 수 있음)
            const currentItem = this.gameState.inventory.items.get(itemIdStr);
            if (!currentItem || currentItem.count < this.synthesizeCount) continue;
            
            // 최대 등급 확인
            if (currentItem.grade >= maxGrade) continue;
            
            // 한 번에 할 수 있는 최대 합성 횟수 계산
            const possibleSyntheses = Math.floor(currentItem.count / this.synthesizeCount);
            
            // 연속 합성 수행
            for (let i = 0; i < possibleSyntheses; i++) {
                const result = this.synthesize(itemIdStr);
                if (result) {
                    successCount++;
                    totalMaterialUsed += this.synthesizeCount;
                    
                    // 결과 기록
                    const existingResult = results.find(r => r.result.id === result.id);
                    if (existingResult) {
                        existingResult.count++;
                    } else {
                        results.push({
                            material: {
                                name: currentItem.name,
                                grade: currentItem.grade - 1 // 합성 전 등급 (synth에서 grade+1됨)
                            },
                            result,
                            count: 1
                        });
                    }
                } else {
                    // 더 이상 합성 불가 (최대 등급 도달 등)
                    break;
                }
            }
        }
        
        return {
            successCount,
            totalMaterialUsed,
            results
        };
    }

    /**
     * 특정 타입의 합성 가능한 아이템 목록 반환
     * @param {string} type - 'weapon', 'armor', 'boots', 'accessory'
     * @returns {Array}
     */
    getSynthesizableItemsByType(type) {
        const result = [];
        const maxGrade = this.getMaxGradeByType(type);
        
        this.gameState.inventory.items.forEach((item, itemId) => {
            if (item.type === type && item.count >= this.synthesizeCount && item.grade < maxGrade) {
                result.push({
                    itemId,
                    name: item.name,
                    count: item.count,
                    grade: item.grade,
                    rarity: item.rarity,
                    maxPossibleSyntheses: Math.floor(item.count / this.synthesizeCount)
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
