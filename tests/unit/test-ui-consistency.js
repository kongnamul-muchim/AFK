// ========== UI-코드 일치성 테스트 ==========
// 브라우저 콘솔에서 실행

(function testUICodeConsistency() {
    console.clear();
    console.log('╔════════════════════════════════════════════════════════╗');
    console.log('║        UI-코드 일치성 테스트                           ║');
    console.log('╚════════════════════════════════════════════════════════╝\n');
    
    const results = {
        passed: 0,
        failed: 0,
        issues: []
    };
    
    // 테스트 1: InventoryUI.handleSynthesize 존재 확인
    console.log('📋 Test 1: handleSynthesize 메서드 확인');
    if (typeof game.inventoryUI.handleSynthesize === 'function') {
        console.log('  ✅ handleSynthesize exists');
        results.passed++;
    } else {
        console.log('  ❌ handleSynthesize NOT found');
        results.failed++;
        results.issues.push('handleSynthesize 메서드 없음');
    }
    
    // 테스트 2: InventorySystem.synthesize 존재 확인
    console.log('\n📋 Test 2: InventorySystem.synthesize 확인');
    if (typeof game.inventorySystem.synthesize === 'function') {
        console.log('  ✅ synthesize exists');
        results.passed++;
    } else {
        console.log('  ❌ synthesize NOT found');
        results.failed++;
        results.issues.push('inventorySystem.synthesize 메서드 없음');
    }
    
    // 테스트 3: discoveredItems 존재 확인
    console.log('\n📋 Test 3: discoveredItems Set 확인');
    if (game.gameState.inventory.discoveredItems instanceof Set) {
        console.log(`  ✅ discoveredItems exists (${game.gameState.inventory.discoveredItems.size} items)`);
        results.passed++;
    } else {
        console.log('  ❌ discoveredItems NOT found or not a Set');
        results.failed++;
        results.issues.push('discoveredItems Set 없음');
    }
    
    // 테스트 4: 실제 합성 테스트 (rusty_sword)
    console.log('\n📋 Test 4: 실제 합성 테스트 (rusty_sword x5)');
    game.gameState.inventory.items.set('1', {
        itemId: 1, name: 'rusty_sword', count: 5,
        grade: 1, rarity: 'common', type: 'weapon',
        stats: { str: 1 }
    });
    
    const beforeItem = game.gameState.inventory.items.get('1');
    console.log(`  Before: ${beforeItem.name} x${beforeItem.count} grade.${beforeItem.grade}`);
    
    const result = game.inventorySystem.synthesize(1);
    
    const afterItem = game.gameState.inventory.items.get('1');
    const newItem = game.gameState.inventory.items.get('2');
    
    if (result && afterItem && afterItem.count === 0 && newItem && newItem.count === 1) {
        console.log(`  ✅ Synthesis successful!`);
        console.log(`     Before: rusty_sword x5 grade.1`);
        console.log(`     After:  rusty_sword x0 grade.1 + rusty_sword x1 grade.2`);
        results.passed++;
    } else {
        console.log(`  ❌ Synthesis failed!`);
        console.log(`     result:`, result);
        console.log(`     afterItem:`, afterItem);
        console.log(`     newItem:`, newItem);
        results.failed++;
        results.issues.push('실제 합성 실패');
    }
    
    // 테스트 5: UI 갱신 확인
    console.log('\n📋 Test 5: UI renderInventory 확인');
    if (typeof game.inventoryUI.renderInventory === 'function') {
        console.log('  ✅ renderInventory exists');
        results.passed++;
        
        // 실제 렌더링 시도
        try {
            game.inventoryUI.renderInventory();
            console.log('  ✅ renderInventory executed without error');
            results.passed++;
        } catch (e) {
            console.log('  ❌ renderInventory error:', e.message);
            results.failed++;
            results.issues.push(`renderInventory 에러: ${e.message}`);
        }
    } else {
        console.log('  ❌ renderInventory NOT found');
        results.failed++;
        results.issues.push('renderInventory 메서드 없음');
    }
    
    // 테스트 6:갑옷 합성 테스트
    console.log('\n📋 Test 6: 갑옷 합성 테스트 (rusty_armor x5)');
    game.gameState.inventory.items.set('16', {
        itemId: 16, name: 'rusty_armor', count: 5,
        grade: 1, rarity: 'common', type: 'armor',
        stats: { vit: 2 }
    });
    
    const armorResult = game.inventorySystem.synthesize(16);
    if (armorResult && armorResult.name === 'rusty_armor' && armorResult.grade === 2) {
        console.log('  ✅ Armor synthesis successful!');
        console.log(`     Result: ${armorResult.name} grade.${armorResult.grade}`);
        results.passed++;
    } else {
        console.log('  ❌ Armor synthesis failed!');
        console.log(`     result:`, armorResult);
        results.failed++;
        results.issues.push('갑옷 합성 실패');
    }
    
    // 테스트 7:아이템 개수 0 인지 확인
    console.log('\n📋 Test 7: 합성 후 count=0 확인');
    const swordAfter = game.gameState.inventory.items.get('1');
    if (swordAfter && swordAfter.count === 0) {
        console.log('  ✅ Item count correctly set to 0');
        results.passed++;
    } else {
        console.log('  ❌ Item count NOT 0:', swordAfter?.count);
        results.failed++;
        results.issues.push('합성 후 count 가 0 이 아님');
    }
    
    // 요약
    console.log('\n╔════════════════════════════════════════════════════════╗');
    console.log('║                    테스트 요약                          ║');
    console.log('╠════════════════════════════════════════════════════════╣');
    console.log(`║  Passed: ${results.passed}/ ${results.passed + results.failed}                                    ║`);
    console.log(`║  Failed: ${results.failed}/ ${results.passed + results.failed}                                    ║`);
    console.log('╠════════════════════════════════════════════════════════╣');
    
    if (results.issues.length > 0) {
        console.log('║  Issues:                                              ║');
        results.issues.forEach((issue, i) => {
            console.log(`║    ${i+1}. ${issue}`.padEnd(59) + '║');
        });
    } else {
        console.log('║  ✅ 모든 테스트 통과!                                 ║');
    }
    
    console.log('╚════════════════════════════════════════════════════════╝\n');
    
    return results;
})();
