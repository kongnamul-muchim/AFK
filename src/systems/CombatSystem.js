/**
 * CombatSystem - 자동 전투 시스템
 * Unity 독립적인 순수 게임 로직
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameDataLoader } from '../data-parser/DataLoader.js';
import { gameLogger } from '../core/Logger.js';

class CombatSystem {
    /**
     * @param {GameState} gameState 
     */
    constructor(gameState) {
        this.gameState = gameState;
        this.attackTimer = null;
        this.isAttacking = false;
        this.currentMonster = null;
        this.attackInterval = 100; // ms
    }

    /**
     * 전투 시스템 초기화
     */
    init() {
        // 공격 인터벌 설정 (Config 에서 읽음)
        this.attackInterval = gameDataLoader.getConfigNumber('combat', 'attackInterval', 100);
        
        gameLogger.debug('CombatSystem initialized');
    }

    /**
     * 전투 시작
     */
    startCombat() {
        if (this.isAttacking) return;
        
        this.isAttacking = true;
        this.spawnMonster();
        this.startAttackLoop();
        
        gameLogger.info('Combat started');
    }

    /**
     * 전투 중지
     */
    stopCombat() {
        this.isAttacking = false;
        if (this.attackTimer) {
            clearInterval(this.attackTimer);
            this.attackTimer = null;
        }
        this.currentMonster = null;
    }

    /**
     * 몬스터 스폰
     */
    spawnMonster() {
        const stage = this.gameState.stage.current;
        const isBoss = stage % 10 === 0;
        
        // 몬스터 데이터 조회
        let monsterData;
        if (isBoss) {
            // 보스 스테이지 - 보스 몬스터
            monsterData = gameDataLoader.filter('monsters', m => m.stage === stage && m.isBoss)[0];
        }
        
        // 일반 몬스터 중 랜덤 선택
        if (!monsterData) {
            const stageMonsters = gameDataLoader.filter('monsters', m => 
                m.stage <= stage && !m.isBoss
            );
            if (stageMonsters.length > 0) {
                const randomIndex = Math.floor(Math.random() * stageMonsters.length);
                monsterData = stageMonsters[randomIndex];
            }
        }
        
        // 데이터가 없으면 기본 몬스터 생성
        if (!monsterData) {
            monsterData = {
                id: 1,
                name: 'Slime',
                stage: stage,
                hp_base: 50,
                hp_scale: 10,
                atk_base: 5,
                atk_scale: 1,
                exp_reward: 10,
                gold_reward: 5,
                isBoss: false
            };
        }
        
        // 몬스터 스탯 계산 (스테이지 기반 스케일링)
        const scalingMultiplier = gameDataLoader.getConfigNumber('combat', 'monsterScalingMultiplier', 1.1);
        const stageMultiplier = Math.pow(scalingMultiplier, stage - 1);
        
        this.currentMonster = {
            id: monsterData.id,
            name: monsterData.name,
            maxHp: Math.floor(monsterData.hp_base * stageMultiplier),
            currentHp: Math.floor(monsterData.hp_base * stageMultiplier),
            attack: Math.floor(monsterData.atk_base * stageMultiplier),
            expReward: Math.floor(monsterData.exp_reward * stageMultiplier),
            goldReward: Math.floor(monsterData.gold_reward * stageMultiplier),
            isBoss: monsterData.isBoss
        };
        
        // 새 몬스터 등장 로그 (처음만)
        gameEventBus.emit(GAME_EVENTS.COMBAT_LOG, { 
            message: `${this.currentMonster.name}이 (가) 등장했습니다!` 
        });
        
        if (this.currentMonster.isBoss) {
            gameEventBus.emit(GAME_EVENTS.STAGE_BOSS_ENTER);
        }
    }
    }

    /**
     * 공격 루프 시작
     */
    startAttackLoop() {
        this.attackTimer = setInterval(() => {
            if (!this.isAttacking || !this.currentMonster) return;
            
            // 플레이어 공격
            this.playerAttack();
            
        }, this.attackInterval);
    }

    /**
     * 플레이어 공격
     */
    playerAttack() {
        if (!this.currentMonster) return;
        
        const player = this.gameState.player;
        const monster = this.currentMonster;
        
        // 크리티컬 판정
        const isCrit = Math.random() < player.derivedStats.critChance;
        const critMultiplier = isCrit ? player.derivedStats.critDamage : 1;
        
        // 데미지 계산 (공격력 - 방어력, 최소 1)
        const minDamage = gameDataLoader.getConfigNumber('combat', 'minDamage', 1);
        let damage = Math.max(minDamage, player.derivedStats.attack - 5); // 몬스터 방어력 5 고정
        damage = Math.floor(damage * critMultiplier);
        
        // 몬스터 HP 감소
        monster.currentHp = Math.max(0, monster.currentHp - damage);
        
        // 이벤트 발행
        gameEventBus.emit(GAME_EVENTS.COMBAT_ATTACK, { 
            attacker: 'player', 
            damage, 
            isCrit 
        });
        
        gameEventBus.emit(GAME_EVENTS.COMBAT_DAMAGE, {
            target: 'monster',
            damage,
            currentHp: monster.currentHp,
            maxHp: monster.maxHp
        });
        
        // 로그 - 크리티컬Hit 만 표시 (로그 과부하 방지)
        if (isCrit) {
            gameEventBus.emit(GAME_EVENTS.COMBAT_LOG, {
                message: `[크리티컬!] ${damage} 데미지!`
            });
        }
        
        // 몬스터 처치 확인
        if (monster.currentHp <= 0) {
            this.killMonster();
        }
    }

    /**
     * 몬스터 처치
     */
    killMonster() {
        const monster = this.currentMonster;
        const player = this.gameState.player;
        
        // 보상 지급
        this.gameState.addExp(monster.expReward);
        this.gameState.addGold(monster.goldReward);
        
        // 아이템 드롭
        this.rollItemDrop();
        
        // 스테이지 진행
        this.gameState.killMonster();
        
        // 이벤트
        gameEventBus.emit(GAME_EVENTS.COMBAT_MONSTER_KILLED, {
            monsterId: monster.id,
            exp: monster.expReward,
            gold: monster.goldReward
        });
        
        gameEventBus.emit(GAME_EVENTS.COMBAT_LOG, {
            message: `${monster.name}을 (를) 처치했습니다! (+${monster.expReward} Exp, +${monster.goldReward} Gold)`
        });
        
        // 보스 처치
        if (monster.isBoss) {
            gameEventBus.emit(GAME_EVENTS.STAGE_BOSS_DEFEATED);
        }
        
        // 새 몬스터 스폰
        this.spawnMonster();
    }

    /**
     * 아이템 드롭 판정
     */
    rollItemDrop() {
        const dropRates = {
            common: gameDataLoader.getConfigNumber('inventory', 'commonDropRate', 0.6),
            rare: gameDataLoader.getConfigNumber('inventory', 'rareDropRate', 0.3),
            epic: gameDataLoader.getConfigNumber('inventory', 'epicDropRate', 0.09),
            legendary: gameDataLoader.getConfigNumber('inventory', 'legendaryDropRate', 0.01)
        };
        
        const roll = Math.random();
        let rarity = 'common';
        
        if (roll < dropRates.legendary) {
            rarity = 'legendary';
        } else if (roll < dropRates.epic) {
            rarity = 'epic';
        } else if (roll < dropRates.rare) {
            rarity = 'rare';
        }
        
        // 해당 희귀도의 아이템 중 현재 스테이지에 적합한 아이템 선택
        const items = gameDataLoader.filter('items', item => 
            item.rarity === rarity && item.grade <= Math.ceil(this.gameState.stage.current / 10) + 1
        );
        
        if (items.length > 0) {
            const randomItem = items[Math.floor(Math.random() * items.length)];
            this.gameState.inventory.items.set(randomItem.id, {
                itemId: randomItem.id,
                name: randomItem.name,
                count: 1,
                grade: randomItem.grade,
                rarity: rarity,
                stats: randomItem.stats_min
            });
            
            gameEventBus.emit(GAME_EVENTS.INVENTORY_ITEM_ADDED, {
                itemId: randomItem.id,
                name: randomItem.name,
                rarity: rarity
            });
            
            gameEventBus.emit(GAME_EVENTS.COMBAT_LOG, {
                message: `아이템 획득: ${randomItem.name} (${rarity})`
            });
        }
    }

    /**
     * 현재 몬스터 반환
     * @returns {Object|null}
     */
    getCurrentMonster() {
        return this.currentMonster;
    }

    /**
     * 정리
     */
    destroy() {
        this.stopCombat();
    }
}

export { CombatSystem };
