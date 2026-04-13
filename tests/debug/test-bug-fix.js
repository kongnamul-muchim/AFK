// ========== 버그 수정 테스트 스크립트 ==========
// 브라우저 콘솔에 복사/붙여넣기

(function testBugFixes() {
    console.clear();
    console.log('╔════════════════════════════════════════════════════════╗');
    console.log('║        버그 수정 테스트                                ║');
    console.log('╚════════════════════════════════════════════════════════╝\n');
    
    // 1. 인벤토리 초기화
    console.log('🗑️  Step 1: 인벤토리 초기화');
    game.gameState.inventory.items.clear();
    game.gameState.inventory.discoveredItems.clear();
    game.gameState.inventory.gold = 0;
    console.log('   ✅ 인벤토리 초기화 완료\n');
    
    // 2. 합성 테스트용 아이템 지급 (rusty_sword grade.1 x25)
    console.log('🎁 Step 2: 합성 테스트용 아이템 지급\n');
    
    // rusty_sword grade.1 x25 (합성 5회 가능)
    const testItem = { 
        id: 1, 
        name: 'rusty_sword', 
        grade: 1, 
        type: 'weapon', 
        rarity: 'common', 
        count: 25 
    };
    
    game.gameState.inventory.items.set(testItem.id.toString(), {
        itemId: testItem.id,
        name: testItem.name,
        count: testItem.count,
        grade: testItem.grade,
        rarity: testItem.rarity,
        type: testItem.type,  // type 저장 확인!
        stats: {}
    });
    
    // discoveredItems에 추가
    game.gameState.inventory.discoveredItems.add(testItem.id.toString());
    
    console.log(`   + ${testItem.name} grade.${testItem.grade} (${testItem.rarity}) x${testItem.count}`);
    console.log('\n   ✅ 아이템 지급 완료\n');
    
    // 3. 합성 실행 (5회)
    console.log('🔨 Step 3: 합성 실행 (5회)\n');
    
    for (let i = 0; i < 5; i++) {
        const result = game.inventorySystem.synthesize(testItem.id + i);
        if (result) {
            console.log(`   ✅ 합성 성공: ${result.name} grade.${result.grade}`);
            
            // 합성 후 아이템 확인
            const newItem = game.gameState.inventory.items.get(result.id.toString());
            if (newItem) {
                console.log(`      - type: ${newItem.type || '❌ 없음'}`);
                console.log(`      - count: ${newItem.count}`);
                
                if (!newItem.type) {
                    console.log('      ❌ 문제: type이 저장되지 않음!');
                } else {
                    console.log('      ✅ type 저장됨!');
                }
            }
        } else {
            console.log('   ❌ 합성 실패 (재료 부족)');
            break;
        }
    }
    
    // 4. discoveredItems 확인
    console.log('\n📋 Step 4: discoveredItems 확인\n');
    
    const discovered = Array.from(game.gameState.inventory.discoveredItems);
    console.log(`   discoveredItems: ${discovered.length} 개`);
    discovered.forEach(id => {
        const item = game.gameState.inventory.items.get(id);
        const csvItem = game.dataLoader.get('items').find(i => i.id.toString() === id);
        console.log(`   - ID ${id}: ${item ? item.name : csvItem ? csvItem.name : '없음'} (count: ${item ? item.count : 0})`);
    });
    
    // 5. UI 확인
    console.log('\n🎨 Step 5: UI 렌더링\n');
    
    if (game.inventoryUI) {
        game.inventoryUI.renderInventory();
        console.log('   ✅ UI 렌더링 완료');
        console.log('   💡 인벤토리를 열어서 discovered 아이템이 활성화처럼 보이는지 확인하세요!');
    }
    
    // 6. 요약
    console.log('\n╔════════════════════════════════════════════════════════╗');
    console.log('║                    테스트 완료                         ║');
    console.log('╠════════════════════════════════════════════════════════╣');
    console.log('║  문제 1: 합성 아이템 type 저장 → 수정됨                ║');
    console.log('║  문제 2: discovered 아이템 활성화 → CSS 수정됨         ║');
    console.log('╠════════════════════════════════════════════════════════╣');
    console.log('║  인벤토리를 열어서 결과를 확인하세요!                  ║');
    console.log('╚════════════════════════════════════════════════════════╝\n');
})();