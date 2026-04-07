// ========== 실전 합성 테스트 ==========
// 이 코드를 브라우저 콘솔에 붙여넣고 결과 확인

(function realSynthesisTest() {
    console.clear();
    console.log('╔════════════════════════════════════════════════════════╗');
    console.log('║        실전 합성 테스트                                ║');
    console.log('╚════════════════════════════════════════════════════════╝\n');
    
    // 1. rusty_sword 5 개 추가
    console.log('📦 Step 1: rusty_sword x5 추가');
    game.gameState.inventory.items.set('1', {
        itemId: 1, name: 'rusty_sword', count: 5,
        grade: 1, rarity: 'common', type: 'weapon',
        stats: { str: 1 }
    });
    
    const item1 = game.gameState.inventory.items.get('1');
    console.log(`   ${item1.name} x${item1.count} grade.${item1.grade}\n`);
    
    // 2. 합성 실행
    console.log('🔨 Step 2: 합성 실행 (synthesize(1))');
    const result = game.inventorySystem.synthesize(1);
    
    if (result) {
        console.log(`   ✅ 성공! → ${result.name} grade.${result.grade}\n`);
    } else {
        console.log(`   ❌ 실패!\n`);
    }
    
    // 3. 결과 확인
    console.log('📦 Step 3: 인벤토리 확인');
    const after1 = game.gameState.inventory.items.get('1');
    const after2 = game.gameState.inventory.items.get('2');
    
    console.log(`   rusty_sword grade.1: x${after1?.count || 0}`);
    console.log(`   rusty_sword grade.2: x${after2?.count || 0}\n`);
    
    // 4. UI 갱신
    console.log('🎨 Step 4: UI 갱신');
    if (game.inventoryUI) {
        game.inventoryUI.renderInventory();
        console.log('   ✅ renderInventory() 실행 완료\n');
    } else {
        console.log('   ❌ inventoryUI 없음\n');
    }
    
    // 5. discoveredItems 확인
    console.log('🔓 Step 5: discoveredItems 확인');
    const discovered = game.gameState.inventory.discoveredItems;
    console.log(`   발견된 아이템: ${discovered.size} 개`);
    console.log(`   grade.1 포함: ${discovered.has('1')}`);
    console.log(`   grade.2 포함: ${discovered.has('2')}\n`);
    
    // 6. 추가 합성 테스트
    console.log('🔨 Step 6: grade.2 → grade.3 합성');
    if (after2 && after2.count >= 5) {
        // grade.2 를 4 개 더 추가 (총 5 개로)
        after2.count = 5;
        const result2 = game.inventorySystem.synthesize(2);
        if (result2) {
            console.log(`   ✅ 성공! → ${result2.name} grade.${result2.grade}`);
        } else {
            console.log(`   ❌ 실패!`);
        }
    } else {
        console.log(`   ⚠️ 아이템 부족 (x${after2?.count || 0})`);
    }
    
    console.log('\n╔════════════════════════════════════════════════════════╗');
    console.log('║  테스트 완료! 콘솔 로그를 확인하세요.                 ║');
    console.log('╚════════════════════════════════════════════════════════╝');
})();
