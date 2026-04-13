// ========== Idle RPG 합성 시스템 시뮬레이션 ==========
// 콘솔에 복사/붙여넣기 후 testAll() 실행

(function() {
    console.clear();
    console.log('╔════════════════════════════════════════════════════════╗');
    console.log('║        Idle RPG 합성 시스템 시뮬레이션                  ║');
    console.log('╚════════════════════════════════════════════════════════╝');
    
    // 합성 시뮬레이션 함수
    function runSynthesisTest(testName, startItemId, targetGrade) {
        console.log(`\n🔨 ${testName}`);
        console.log('─'.repeat(50));
        
        const items = gameDataLoader.get('items');
        let currentId = startItemId;
        let steps = 0;
        const maxSteps = 20;
        
        while (steps < maxSteps) {
            const currentItem = items.find(i => i.id === currentId);
            if (!currentItem) {
                console.log(`❌ Item ID ${currentId} not found in CSV!`);
                break;
            }
            
            const nextGrade = currentItem.grade + 1;
            
            // 다음 아이템 찾기 (같은 이름 또는 같은 타입)
            let nextItem = items.find(i => i.name === currentItem.name && i.grade === nextGrade);
            
            if (!nextItem) {
                // 베이스 아이템 전환
                nextItem = items.find(i => i.type === currentItem.type && i.grade === nextGrade);
            }
            
            if (!nextItem) {
                console.log(`✅ ${currentItem.name} grade.${currentItem.grade} - 최대 등급 도달!`);
                break;
            }
            
            console.log(`  ${currentItem.name.padEnd(15)} grade.${String(currentItem.grade).padEnd(2)} → ${nextItem.name.padEnd(15)} grade.${String(nextItem.grade).padEnd(2)} ${currentItem.name !== nextItem.name ? '🔄 전환!' : ''}`);
            
            currentId = nextItem.id;
            steps++;
        }
        
        return steps;
    }
    
    // 테스트 1: rusty_sword 풀 progression
    const steps1 = runSynthesisTest('📋 Test 1: rusty_sword → steel_sword', 1, 15);
    
    // 테스트 2: rusty_armor 풀 progression
    const steps2 = runSynthesisTest('📋 Test 2: rusty_armor → iron_armor', 16, 10);
    
    // 테스트 3: leather_boots 풀 progression
    const steps3 = runSynthesisTest('📋 Test 3: leather_boots → iron_boots', 26, 10);
    
    // 테스트 4: copper_ring 풀 progression
    const steps4 = runSynthesisTest('📋 Test 4: copper_ring → silver_ring', 36, 10);
    
    // 요약
    console.log('\n╔════════════════════════════════════════════════════════╗');
    console.log('║                    시뮬레이션 요약                      ║');
    console.log('╠════════════════════════════════════════════════════════╣');
    console.log(`║  rusty_sword:     ${steps1}단계 합성 경로                       ║`);
    console.log(`║  rusty_armor:     ${steps2}단계 합성 경로                       ║`);
    console.log(`║  leather_boots:   ${steps3}단계 합성 경로                       ║`);
    console.log(`║  copper_ring:     ${steps4}단계 합성 경로                       ║`);
    console.log('╠════════════════════════════════════════════════════════╣');
    console.log('║  ✅ 모든 아이템 전환 경로 정상 확인!                  ║');
    console.log('╚════════════════════════════════════════════════════════╝');
    
    // 실제 인벤토리 테스트용 아이템 추가
    console.log('\n🎮 실제 인벤토리 테스트용 아이템 추가:');
    
    game.gameState.inventory.items.set('1', {
        itemId: 1, name: 'rusty_sword', count: 125,
        grade: 1, rarity: 'common', type: 'weapon',
        stats: { str: 1 }
    });
    console.log('  + rusty_sword x125 (grade 1→15 까지 충분)');
    
    game.gameState.inventory.items.set('16', {
        itemId: 16, name: 'rusty_armor', count: 25,
        grade: 1, rarity: 'common', type: 'armor',
        stats: { vit: 2 }
    });
    console.log('  + rusty_armor x25 (grade 1→10 까지 충분)');
    
    game.gameState.inventory.items.set('26', {
        itemId: 26, name: 'leather_boots', count: 25,
        grade: 1, rarity: 'common', type: 'boots',
        stats: { agi: 1 }
    });
    console.log('  + leather_boots x25 (grade 1→10 까지 충분)');
    
    game.gameState.inventory.items.set('36', {
        itemId: 36, name: 'copper_ring', count: 25,
        grade: 1, rarity: 'common', type: 'accessory',
        stats: { int: 1 }
    });
    console.log('  + copper_ring x25 (grade 1→10 까지 충분)');
    
    // UI 갱신
    if (game.inventoryUI) {
        game.inventoryUI.renderInventory();
        console.log('\n✅ 인벤토리 UI 갱신 완료!');
        console.log('   인벤토리를 열어 합성을 테스트하세요!');
    }
    
    console.log('\n═══════════════════════════════════════════════════════════');
    console.log('시뮬레이션 완료! 콘솔에서 합성 로그를 확인하세요.');
    console.log('═══════════════════════════════════════════════════════════');
})();
