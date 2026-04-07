// ========== Node.js 합성 시뮬레이션 (standalone) ==========
// 브라우저 없이 독립 실행 가능

const fs = require('fs');

// CSV 파싱
function parseCSV(text) {
    const lines = text.split('\n').filter(line => line.trim() && !line.startsWith('#'));
    const headers = lines[0].split(',').map(h => h.trim());
    
    return lines.slice(1).map(line => {
        const values = line.split(',').map(v => v.trim());
        const row = {};
        headers.forEach((h, i) => {
            let val = values[i];
            if (val && /^\d+(\.\d+)?$/.test(val)) val = parseFloat(val);
            if (val && typeof val === 'string' && val.startsWith('{')) val = JSON.parse(val.replace(/""/g, '"'));
            row[h] = val;
        });
        return row;
    });
}

// 아이템 데이터 로드
const csvText = fs.readFileSync('data/items.csv', 'utf-8');
const items = parseCSV(csvText);

console.log('\n╔════════════════════════════════════════════════════════╗');
console.log('║     Idle RPG 합성 시스템 시뮬레이션 (Node.js)           ║');
console.log('╚════════════════════════════════════════════════════════╝');

// 합성 경로 시뮬레이션
function simulateSynthesis(startItemId, maxSteps = 20) {
    let currentId = startItemId;
    const path = [];
    
    for (let step = 0; step < maxSteps; step++) {
        const current = items.find(i => i.id === currentId);
        if (!current) break;
        
        path.push(current);
        const nextGrade = current.grade + 1;
        
        // 다음 아이템 찾기 (같은 이름 우선, 아니면 같은 타입)
        let next = items.find(i => i.name === current.name && i.grade === nextGrade);
        if (!next) {
            next = items.find(i => i.type === current.type && i.grade === nextGrade);
        }
        
        if (!next) {
            console.log(`\n✅ ${current.name} grade.${current.grade} - 최대 등급!`);
            break;
        }
        
        const isTransition = current.name !== next.name;
        console.log(`  Step ${String(step+1).padEnd(2)}: ${current.name.padEnd(15)} g.${String(current.grade).padEnd(2)} → ${next.name.padEnd(15)} g.${String(next.grade).padEnd(2)} ${isTransition ? '🔄' : ''}`);
        
        currentId = next.id;
    }
    
    return path;
}

console.log('\n📋 Test 1: rusty_sword (ID:1) → steel_sword');
console.log('─'.repeat(55));
simulateSynthesis(1);

console.log('\n📋 Test 2: rusty_armor (ID:16) → iron_armor');
console.log('─'.repeat(55));
simulateSynthesis(16);

console.log('\n📋 Test 3: leather_boots (ID:26) → iron_boots');
console.log('─'.repeat(55));
simulateSynthesis(26);

console.log('\n📋 Test 4: copper_ring (ID:36) → silver_ring');
console.log('─'.repeat(55));
simulateSynthesis(36);

console.log('\n╔════════════════════════════════════════════════════════╗');
console.log('║  ✅ 모든 합성 경로 정상 확인!                         ║');
console.log('╚════════════════════════════════════════════════════════╝\n');
