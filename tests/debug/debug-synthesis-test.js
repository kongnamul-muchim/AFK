// ========== 합성 시스템 디버그 테스트 ==========
// 콘솔에 복사/붙여넣기 후 testAllSynthesis() 실행

(function testAllSynthesis() {
    console.clear();
    console.log('╔════════════════════════════════════════════════════════╗');
    console.log('║        합성 시스템 디버그 테스트                       ║');
    console.log('╚════════════════════════════════════════════════════════╝\n');
    
    // 테스트 데이터
    const testItems = [
        { id: '1', name: 'rusty_sword', grade: 1, type: 'weapon', count: 25 },
        { id: '16', name: 'rusty_armor', grade: 1, type: 'armor', count: 25 },
        { id: '26', name: 'leather_boots', grade: 1, type: 'boots', count: 25 },
        { id: '36', name: 'copper_ring', grade: 1, type: 'accessory', count: 25 }
    ];
    
    // 테스트 아이템 추가
    console.log('📦 테스트 아이템 추가:\n');
    testItems.forEach(item => {
        game.gameState.inventory.items.set(item.id, {
            itemId: parseInt(item.id),
            name: item.name,
            count: item.count,
            grade: item.grade,
            rarity: 'common',
            type: item.type,
            stats: {}
        });
        console.log(`  + ${item.name} x${item.count} (grade ${item.grade}, ${item.type})`);
    });
    
    // UI 갱신
    if (game.inventoryUI) {
        game.inventoryUI.renderInventory();
    }
    
    console.log('\n✅ 인벤토리 갱신 완료!\n');
    console.log('────────────────────────────────────────────────────────────');
    console.log('이제 인벤토리에서 각 아이템을 우클릭하여 합성을 테스트하세요.');
    console.log('콘솔에서 [findNextGradeItem] 로그를 확인하면 됩니다.');
    console.log('────────────────────────────────────────────────────────────\n');
    
    // 자동 합성 테스트 함수
    window.testSynthesis = function(itemId, maxSteps = 5) {
        console.log(`\n🔨 Testing synthesis for item ID ${itemId} (max ${maxSteps} steps)`);
        console.log('─'.repeat(60));
        
        let currentId = parseInt(itemId);
        
        for (let step = 0; step < maxSteps; step++) {
            const item = game.gameState.inventory.items.get(currentId.toString());
            if (!item) {
                console.log(`❌ Item ${currentId} not found in inventory!`);
                break;
            }
            
            if (item.count < 5) {
                console.log(`⚠️ Not enough items: ${item.name} x${item.count} (need 5)`);
                break;
            }
            
            console.log(`\nStep ${step + 1}: Synthesizing ${item.name} grade.${item.grade} x5...`);
            const result = game.inventorySystem.synthesize(currentId);
            
            if (result) {
                console.log(`  ✓ Success! → ${result.name} grade.${result.grade}`);
                currentId = result.id;
            } else {
                console.log(`  ❌ Failed!`);
                break;
            }
        }
    };
    
    console.log('\n💡 사용법:');
    console.log('  testSynthesis(1)   - rusty_sword 합성 테스트');
    console.log('  testSynthesis(16)  - rusty_armor 합성 테스트');
    console.log('  testSynthesis(26)  - leather_boots 합성 테스트');
    console.log('  testSynthesis(36)  - copper_ring 합성 테스트');
})();
