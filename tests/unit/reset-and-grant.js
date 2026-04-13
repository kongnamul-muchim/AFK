// ========== 테스트용 초기화 & 아이템 지급 스크립트 ==========
// 브라우저 콘솔에 복사/붙여넣기

(function resetAndGrantItems() {
    console.clear();
    console.log('╔════════════════════════════════════════════════════════╗');
    console.log('║        테스트용 초기화 & 아이템 지급                   ║');
    console.log('╚════════════════════════════════════════════════════════╝\n');
    
    // 1. 인벤토리 초기화
    console.log('🗑️  Step 1: 인벤토리 초기화');
    game.gameState.inventory.items.clear();
    game.gameState.inventory.discoveredItems.clear();
    game.gameState.inventory.gold = 0;
    console.log('   ✅ 인벤토리 초기화 완료\n');
    
    // 2. 테스트용 아이템 지급 (각 아이템별 25 개)
    console.log('🎁 Step 2: 테스트용 아이템 지급 (각 25 개)\n');
    
    const testItems = [
        // 무기 (rusty_sword 1-5)
        { id: 1, name: 'rusty_sword', grade: 1, type: 'weapon', rarity: 'common', count: 25 },
        { id: 2, name: 'rusty_sword', grade: 2, type: 'weapon', rarity: 'rare', count: 25 },
        { id: 3, name: 'rusty_sword', grade: 3, type: 'weapon', rarity: 'epic', count: 25 },
        { id: 4, name: 'rusty_sword', grade: 4, type: 'weapon', rarity: 'legendary', count: 25 },
        { id: 5, name: 'rusty_sword', grade: 5, type: 'weapon', rarity: 'mythic', count: 25 },
        
        // 갑옷 (rusty_armor 1-5)
        { id: 16, name: 'rusty_armor', grade: 1, type: 'armor', rarity: 'common', count: 25 },
        { id: 17, name: 'rusty_armor', grade: 2, type: 'armor', rarity: 'rare', count: 25 },
        { id: 18, name: 'rusty_armor', grade: 3, type: 'armor', rarity: 'epic', count: 25 },
        { id: 19, name: 'rusty_armor', grade: 4, type: 'armor', rarity: 'legendary', count: 25 },
        { id: 20, name: 'rusty_armor', grade: 5, type: 'armor', rarity: 'mythic', count: 25 },
        
        // 신발 (leather_boots 1-5)
        { id: 26, name: 'leather_boots', grade: 1, type: 'boots', rarity: 'common', count: 25 },
        { id: 27, name: 'leather_boots', grade: 2, type: 'boots', rarity: 'rare', count: 25 },
        { id: 28, name: 'leather_boots', grade: 3, type: 'boots', rarity: 'epic', count: 25 },
        { id: 29, name: 'leather_boots', grade: 4, type: 'boots', rarity: 'legendary', count: 25 },
        { id: 30, name: 'leather_boots', grade: 5, type: 'boots', rarity: 'mythic', count: 25 },
        
        // 장신구 (copper_ring 1-5)
        { id: 36, name: 'copper_ring', grade: 1, type: 'accessory', rarity: 'common', count: 25 },
        { id: 37, name: 'copper_ring', grade: 2, type: 'accessory', rarity: 'rare', count: 25 },
        { id: 38, name: 'copper_ring', grade: 3, type: 'accessory', rarity: 'epic', count: 25 },
        { id: 39, name: 'copper_ring', grade: 4, type: 'accessory', rarity: 'legendary', count: 25 },
        { id: 40, name: 'copper_ring', grade: 5, type: 'accessory', rarity: 'mythic', count: 25 }
    ];
    
    testItems.forEach(item => {
        game.gameState.inventory.items.set(item.id.toString(), {
            itemId: item.id,
            name: item.name,
            count: item.count,
            grade: item.grade,
            rarity: item.rarity,
            type: item.type,  // 정확한 타입 저장!
            stats: {}
        });
        
        // discoveredItems 에 추가 (영구 해제)
        game.gameState.inventory.discoveredItems.add(item.id.toString());
        
        console.log(`   + ${item.name} grade.${item.grade} (${item.rarity}) x${item.count}`);
    });
    
    console.log('\n   ✅ 총 20 개 아이템 지급 완료 (각 25 개)\n');
    
    // 3. 골드 지급
    console.log('💰 Step 3: 골드 지급');
    game.gameState.inventory.gold = 10000;
    console.log(`   ✅ 골드 10000 지급\n`);
    
    // 4. UI 갱신
    console.log('🎨 Step 4: UI 갱신');
    if (game.inventoryUI) {
        game.inventoryUI.renderInventory();
        console.log('   ✅ renderInventory() 실행 완료\n');
    }
    
    // 5. 요약
    console.log('╔════════════════════════════════════════════════════════╗');
    console.log('║                    초기화 완료                         ║');
    console.log('╠════════════════════════════════════════════════════════╣');
    console.log('║  인벤토리: 초기화 ✅                                   ║');
    console.log('║  아이템: 20 종 × 25 개                                 ║');
    console.log('║  골드: 10000                                           ║');
    console.log('║  발견: 모든 아이템 해제 ✅                             ║');
    console.log('╠════════════════════════════════════════════════════════╣');
    console.log('║  이제 인벤토리를 열어 합성을 테스트하세요!             ║');
    console.log('╚════════════════════════════════════════════════════════╝\n');
    
    console.log('💡 팁: 인벤토리에서 아이템을 우클릭하면 합성됩니다!');
    console.log('   rusty_sword grade.1 x5 → grade.2\n');
})();
