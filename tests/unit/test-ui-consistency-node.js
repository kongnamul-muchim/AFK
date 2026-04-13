// ========== UI-코드 일치성 테스트 (Node.js) ==========

const fs = require('fs');

console.log('╔════════════════════════════════════════════════════════╗');
console.log('║        UI-코드 일치성 테스트                           ║');
console.log('╚════════════════════════════════════════════════════════╝\n');

const results = { passed: 0, failed: 0, issues: [] };

// 파일 존재 확인
const files = {
    'InventoryUI.js': 'src/ui/InventoryUI.js',
    'InventorySystem.js': 'src/systems/InventorySystem.js',
    'GameState.js': 'src/core/GameState.js'
};

console.log('📋 Test 1: 파일 존재 확인');
Object.entries(files).forEach(([name, path]) => {
    if (fs.existsSync(path)) {
        console.log(`  ✅ ${name} exists`);
        results.passed++;
    } else {
        console.log(`  ❌ ${name} NOT found`);
        results.failed++;
        results.issues.push(`${name} 파일 없음`);
    }
});

// 코드 내용 확인
console.log('\n📋 Test 2: handleSynthesize 메서드 확인');
const uiCode = fs.readFileSync('src/ui/InventoryUI.js', 'utf-8');
if (uiCode.includes('handleSynthesize(itemId)')) {
    console.log('  ✅ handleSynthesize exists in UI');
    results.passed++;
} else {
    console.log('  ❌ handleSynthesize NOT found in UI');
    results.failed++;
    results.issues.push('UI 에 handleSynthesize 없음');
}

console.log('\n📋 Test 3: InventorySystem.synthesize 확인');
const systemCode = fs.readFileSync('src/systems/InventorySystem.js', 'utf-8');
if (systemCode.includes('synthesize(itemId)')) {
    console.log('  ✅ synthesize exists in InventorySystem');
    results.passed++;
} else {
    console.log('  ❌ synthesize NOT found in InventorySystem');
    results.failed++;
    results.issues.push('InventorySystem 에 synthesize 없음');
}

console.log('\n📋 Test 4: discoveredItems Set 확인');
const gameStateCode = fs.readFileSync('src/core/GameState.js', 'utf-8');
if (gameStateCode.includes('discoveredItems: new Set()')) {
    console.log('  ✅ discoveredItems Set exists in GameState');
    results.passed++;
} else {
    console.log('  ❌ discoveredItems NOT found in GameState');
    results.failed++;
    results.issues.push('GameState 에 discoveredItems 없음');
}

console.log('\n📋 Test 5: toJSON/discoveredItems 직렬화 확인');
if (gameStateCode.includes('discoveredItems: Array.from(this.inventory.discoveredItems)')) {
    console.log('  ✅ discoveredItems serialization exists in toJSON');
    results.passed++;
} else {
    console.log('  ❌ discoveredItems serialization NOT found');
    results.failed++;
    results.issues.push('toJSON 에 discoveredItems 직렬화 없음');
}

console.log('\n📋 Test 6: fromJSON/discoveredItems 복원 확인');
if (gameStateCode.includes('this.inventory.discoveredItems = new Set(data.inventory.discoveredItems)')) {
    console.log('  ✅ discoveredItems deserialization exists in fromJSON');
    results.passed++;
} else {
    console.log('  ❌ discoveredItems deserialization NOT found');
    results.failed++;
    results.issues.push('fromJSON 에 discoveredItems 복원 없음');
}

console.log('\n📋 Test 7: renderInventory 호출 확인');
if (uiCode.includes('this.renderInventory()') && uiCode.includes('handleSynthesize')) {
    const synthesizeSection = uiCode.substring(
        uiCode.indexOf('handleSynthesize(itemId)'),
        uiCode.indexOf('handleSynthesize(itemId)') + 500
    );
    if (synthesizeSection.includes('renderInventory()')) {
        console.log('  ✅ renderInventory called after synthesize');
        results.passed++;
    } else {
        console.log('  ❌ renderInventory NOT called after synthesize');
        results.failed++;
        results.issues.push('합성 후 renderInventory 호출 안 함');
    }
} else {
    console.log('  ⚠️ Cannot verify renderInventory call');
    results.issues.push('renderInventory 호출 확인 불가');
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

process.exit(results.failed > 0 ? 1 : 0);
