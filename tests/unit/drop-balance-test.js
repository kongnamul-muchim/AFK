// ========== 드롭율 밸런스 테스트 (Node.js standalone) ==========
// CombatSystem.js의 rollItemDrop 로직을 재현해서 통계 검증

function weightedRandomIndex(probabilities) {
    const roll = Math.random();
    let cumulative = 0;
    for (let i = 0; i < probabilities.length; i++) {
        cumulative += probabilities[i];
        if (roll < cumulative) return i;
    }
    return probabilities.length - 1;
}

function simulate(probabilities, iterations = 100000) {
    const counts = [0, 0, 0, 0, 0];
    const labels = ['일반', '희귀', '에픽', '전설', '신화'];
    
    for (let i = 0; i < iterations; i++) {
        const idx = weightedRandomIndex(probabilities);
        counts[idx]++;
    }
    
    console.log(`\n📊 ${iterations.toLocaleString()}회 시뮬레이션 결과:`);
    console.log('─'.repeat(45));
    let total = 0;
    counts.forEach((c, i) => {
        if (probabilities[i] > 0) {
            const pct = ((c / iterations) * 100).toFixed(2);
            const expected = (probabilities[i] * 100).toFixed(1);
            console.log(`  ${labels[i].padEnd(6)}: ${c.toString().padStart(7)}회 (${pct.padStart(5)}%)  [설정: ${expected}%]`);
            total += c;
        }
    });
    console.log(`  ${'합계'.padEnd(6)}: ${total.toString().padStart(7)}회 (100.00%)`);
}

console.log('╔══════════════════════════════════════════╗');
console.log('║     AFK Idle RPG 드롭율 밸런스 테스트    ║');
console.log('╚══════════════════════════════════════════╝');

// 일반 전투: [일반 50%, 희귀 30%, 에픽 15%, 전설 5%, 신화 0%]
console.log('\n🟢 일반 전투 드롭 (일반 50% / 희귀 30% / 에픽 15% / 전설 5%)');
simulate([0.50, 0.30, 0.15, 0.05, 0], 100000);

// 보스 전투: [일반 0%, 희귀 50%, 에픽 35%, 전설 15%, 신화 0%]
console.log('\n🔴 보스 전투 드롭 (희귀 50% / 에픽 35% / 전설 15%)');
simulate([0, 0.50, 0.35, 0.15, 0], 100000);

console.log('\n✅ 테스트 완료');
