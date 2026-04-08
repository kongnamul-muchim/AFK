/**
 * CombatSystem - 이동/조우/전투 루프 시스템
 * Player이동 → 적조우 → 전투 → 처치 → 이동 반복
 * 공격시 체력 소모, 체력 0시 패배, 자동반복 모드 지원
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameConfig } from '../config/GameConfig.js';
import { gameDataLoader } from '../data-parser/DataLoader.js';
import { gameLogger } from '../core/Logger.js';

class CombatSystem {
    /**
     * @param {GameState} gameState 
     */
    constructor(gameState) {
        this.gameState = gameState;
        this.isRunning = false;
        this.currentMonster = null;
        
        // 타이밍 설정
        this.moveDuration = 1500;      // 이동 애니메이션 시간 (ms)
        this.encounterDuration = 500;  // 조우 대기 시간 (ms)
        this.victoryDuration = 1000;   // 처치 후 대기 시간 (ms)
        this.attackCooldown = 800;     // 공격 쿨타임 (ms)
        this.hitDuration = 300;        // 피격 애니메이션 시간 (ms)
        this.defeatDuration = 2000;    // 패배 대기 시간 (ms)
        
        // 몬스터 등장 타이밍
        this.monsterAppearProgress = 0.5; // 이동 50% 지점에서 몬스터 등장 시작
        
        // DailyMissionSystem 참조용 (lazy load)
        this._dailyMissionSystem = null;
    }

    /**
     * DailyMissionSystem 설정 (lazy load)
     */
    setDailyMissionSystem(dailyMissionSystem) {
        this._dailyMissionSystem = dailyMissionSystem;
    }

    /**
     * 버프 배율 가져오기
     */
    getBuffMultiplier(buffType) {
        if (this._dailyMissionSystem) {
            return this._dailyMissionSystem.getBuffMultiplier(buffType);
        }
        return 1.0;
    }

    /**
     * 초기화
     */
    init() {
        gameEventBus.on(GAME_EVENTS.GAME_LOADED, () => {
            // DailyMissionSystem 참조 설정
            if (window.game && window.game.dailyMissionSystem) {
                this.setDailyMissionSystem(window.game.dailyMissionSystem);
            }
        });
        
        // 설정에서 타이밍 값 로드
        this.attackCooldown = gameConfig.combat.attackInterval || 800;
        
        // 저장된 autoRepeat 상태 복원
        if (this.gameState.stage.autoRepeat !== undefined) {
            this.gameState.combatPhase.autoRepeat = this.gameState.stage.autoRepeat;
        }
        
        gameLogger.debug('CombatSystem initialized (new loop system)');
    }

    /**
     * 전투 시스템 시작
     */
    startCombat() {
        if (this.isRunning) return;
        
        this.isRunning = true;
        this.gameState.resetCombatPhase();
        this.startMoving();
        
        gameLogger.info('Combat loop started');
    }

    /**
     * 전투 시스템 중지
     */
    stopCombat() {
        this.isRunning = false;
        this.currentMonster = null;
        this.gameState.resetCombatPhase();
    }

    /**
     * 자동반복 모드 토글
     */
    toggleAutoRepeat() {
        this.gameState.combatPhase.autoRepeat = !this.gameState.combatPhase.autoRepeat;
        this.gameState.stage.autoRepeat = this.gameState.combatPhase.autoRepeat;
        
        gameEventBus.emit(GAME_EVENTS.COMBAT_LOG, {
            message: `자동반복 모드: ${this.gameState.combatPhase.autoRepeat ? 'ON' : 'OFF'}`
        });
        
        gameLogger.info(`Auto-repeat mode: ${this.gameState.combatPhase.autoRepeat}`);
    }

    /**
     * 메인 업데이트 (gameLoop에서 매 프레임 호출)
     * @param {number} dt - 델타 타임 (ms)
     */
    update(dt) {
        if (!this.isRunning) return;
        
        const phase = this.gameState.combatPhase.phase;
        const now = performance.now();
        
        switch (phase) {
            case 'MOVING':
                this.updateMoving(dt, now);
                break;
            case 'ENCOUNTERING':
                this.updateEncountering(dt, now);
                break;
            case 'COMBAT':
                this.updateCombat(dt, now);
                break;
            case 'VICTORY':
                this.updateVictory(dt, now);
                break;
            case 'DEFEATED':
                this.updateDefeat(dt, now);
                break;
            case 'IDLE':
            default:
                this.startMoving();
                break;
        }
    }

    /**
     * 이동 페이즈 업데이트
     */
    updateMoving(dt, now) {
        const cp = this.gameState.combatPhase;
        cp.phaseTimer += dt;
        cp.moveProgress = Math.min(1, cp.phaseTimer / this.moveDuration);
        
        // 몬스터 등장 체크 (이동 50% 지점)
        if (cp.moveProgress >= this.monsterAppearProgress && !this.currentMonster) {
            this.spawnMonster();
            cp.monsterState = 'appearing'; // 등장 전 상태 (프레임 0)
        }
        
        // 몬스터 돌진 타이밍 (이동 65% 지점) - 5번 프레임으로 전환
        if (cp.moveProgress >= 0.65 && cp.monsterState === 'appearing') {
            cp.monsterState = 'charging'; // 돌진 상태 (프레임 5)
        }
        
        // 이동 완료
        if (cp.moveProgress >= 1) {
            this.startEncounter();
        }
    }

    /**
     * 조우 페이즈 업데이트
     */
    updateEncountering(dt, now) {
        const cp = this.gameState.combatPhase;
        cp.phaseTimer += dt;
        
        // 몬스터 돌진 → Idle 전환 (조우 시작 후 200ms)
        if (cp.monsterState === 'charging' && cp.phaseTimer >= 200) {
            cp.monsterState = 'idle';
        }
        
        // 조우 대기 시간 경과 시 전투 시작
        if (cp.phaseTimer >= this.encounterDuration) {
            this.startCombatPhase();
        }
    }

    /**
     * 전투 페이즈 업데이트
     */
    updateCombat(dt, now) {
        const cp = this.gameState.combatPhase;
        cp.phaseTimer += dt;
        cp.combatTimer += dt;
        
        // 공격 애니메이션 프레임 계산 (attackSpeed에 비례)
        // attackSpeed 100 = 기본, 200 = 2배 빠름
        const attackSpeed = this.gameState.player.derivedStats.attackSpeed || 100;
        const framesPerAttack = 3; // 1→2→3 (3프레임)
        const baseFrameDuration = 200; // 기본 프레임당 200ms
        const adjustedFrameDuration = baseFrameDuration / (attackSpeed / 100);
        const attackAnimDuration = framesPerAttack * adjustedFrameDuration;
        
        // 플레이어 공격 애니메이션 상태 확인
        if (cp.playerState === 'attacking') {
            const timeSinceAttackStart = now - cp.attackAnimStartTime;
            const currentFrame = Math.floor(timeSinceAttackStart / adjustedFrameDuration);
            
            // 현재 프레임을 GameState에 저장 (렌더러에서 사용)
            cp.attackCurrentFrame = Math.min(currentFrame, framesPerAttack - 1);
            
            // 애니메이션 완료 확인 (3프레임 끝)
            if (currentFrame >= framesPerAttack) {
                cp.playerState = 'idle';
                cp.attackCurrentFrame = 0;
            } else {
                // 3번 프레임(인덱스 2)에서 데미지 판정과 체력 소모 (한 번만)
                if (currentFrame === 2 && !cp.damageDealt) {
                    // 1. 몬스터에게 데미지
                    this.dealDamageToMonster();
                    cp.damageDealt = true;
                    
                    // 2. 플레이어 체력 소모 (데미지 판정 직후)
                    this.consumePlayerHP();
                    cp.recoilDealt = true;
                }
            }
        }
        
        // 몬스터 피격 상태 해제 확인
        if (cp.monsterState === 'hit') {
            const timeSinceHitAnim = now - cp.hitAnimStartTime;
            if (timeSinceHitAnim >= cp.hitDuration) {
                cp.monsterState = 'idle';
            }
        }
        
        // 공격 쿨타임 확인 (마지막 공격 시간 기준)
        const timeSinceLastAttack = now - cp.lastAttackTime;
        // 쿨타임 = 애니메이션 시간 + 추가 대기 (최소 200ms)
        const effectiveCooldown = Math.max(attackAnimDuration + 200, this.attackCooldown);
        
        if (timeSinceLastAttack >= effectiveCooldown) {
            // 몬스터가 이미 죽었으면 공격 안 함
            if (cp.monsterState !== 'dead') {
                // 현재 공격 애니메이션 중이 아니면 새 공격 시작
                if (cp.playerState !== 'attacking') {
                    this.startPlayerAttack();
                }
            }
        }
    }

    /**
     * 승리 페이즈 업데이트
     */
    updateVictory(dt, now) {
        const cp = this.gameState.combatPhase;
        cp.phaseTimer += dt;
        cp.victoryTimer += dt;
        
        // 승리 대기 시간 경과 시 이동으로 복귀
        if (cp.phaseTimer >= this.victoryDuration) {
            if (cp.autoRepeat) {
                // 자동반복 모드: 같은 스테이지에서 계속
                this.gameState.combatPhase.moveProgress = 0;
                this.startMoving();
            } else {
                // 일반 모드: 다음 스테이지로
                this.startMoving();
            }
        }
    }

    /**
     * 패배 페이즈 업데이트
     */
    updateDefeat(dt, now) {
        const cp = this.gameState.combatPhase;
        cp.phaseTimer += dt;
        cp.defeatTimer += dt;
        
        // 패배 대기 시간 경과 시 부활
        if (cp.phaseTimer >= this.defeatDuration) {
            this.revivePlayer();
        }
    }

    /**
     * 이동 시작
     */
    startMoving() {
        this.gameState.startMoving();
        this.currentMonster = null; // 이동 시작시 몬스터 초기화
        gameEventBus.emit(GAME_EVENTS.COMBAT_PHASE_CHANGED, { phase: 'MOVING' });
        gameLogger.debug('Player started moving');
    }

    /**
     * 조우 시작
     */
    startEncounter() {
        this.gameState.startEncounter();
        
        // 몬스터가 이미 스폰되어있으면 이벤트 발행
        if (this.currentMonster) {
            gameEventBus.emit(GAME_EVENTS.COMBAT_LOG, { 
                message: `${this.currentMonster.name}이(가) 등장했습니다!` 
            });
            
            gameEventBus.emit(GAME_EVENTS.COMBAT_ENCOUNTER, {
                monster: this.currentMonster
            });
            
            gameEventBus.emit(GAME_EVENTS.COMBAT_PHASE_CHANGED, { phase: 'ENCOUNTERING' });
            
            if (this.currentMonster.isBoss) {
                gameEventBus.emit(GAME_EVENTS.STAGE_BOSS_ENTER);
            }
        }
        
        gameLogger.debug('Encounter started');
    }

    /**
     * 전투 시작 (실제 공격 가능 상태)
     */
    startCombatPhase() {
        this.gameState.startCombat();
        gameEventBus.emit(GAME_EVENTS.COMBAT_PHASE_CHANGED, { phase: 'COMBAT' });
        gameLogger.debug('Combat phase started');
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
        const scalingMultiplier = gameConfig.combat.monsterScalingMultiplier;
        const stageMultiplier = Math.pow(scalingMultiplier, stage - 1);
        
        this.currentMonster = {
            id: monsterData.id,
            name: monsterData.name,
            maxHp: Math.floor(monsterData.hp_base * stageMultiplier),
            currentHp: Math.floor(monsterData.hp_base * stageMultiplier),
            attack: Math.floor(monsterData.atk_base * stageMultiplier),
            expReward: Math.floor(monsterData.expReward * stageMultiplier),
            goldReward: Math.floor(monsterData.goldReward * stageMultiplier),
            isBoss: monsterData.isBoss
        };
    }

    /**
     * 플레이어 공격 시작 (애니메이션)
     */
    startPlayerAttack() {
        if (!this.currentMonster || this.gameState.combatPhase.phase !== 'COMBAT') return;
        
        const cp = this.gameState.combatPhase;
        
        // 공격 애니메이션 시작
        cp.playerState = 'attacking';
        cp.attackAnimStartTime = performance.now();
        cp.damageDealt = false; // 데미지 판정 초기화
        cp.recoilDealt = false; // 체력 소모 초기화
        
        // 마지막 공격 시간 업데이트
        cp.lastAttackTime = performance.now();
    }

    /**
     * 몬스터에게 데미지 판정 (공격 애니메이션 중 3번 프레임에서 발생)
     */
    dealDamageToMonster() {
        if (!this.currentMonster) return;
        
        const player = this.gameState.player;
        const monster = this.currentMonster;
        const stage = this.gameState.stage.current;
        
        // 버프 확인 (공격력 2배)
        const attackBuff = this.getBuffMultiplier('attackDouble');
        
        // 크리티컬 판정
        const isCrit = Math.random() < player.derivedStats.critChance;
        const critMultiplier = isCrit ? player.derivedStats.critDamage : 1;
        
        // 데미지 계산: (player.attack - 5) * critMultiplier * buffMultiplier * stageMultiplier
        const minDamage = gameConfig.combat.minDamage;
        const stageMultiplier = 1 + (stage - 1) * 0.1;
        let damage = Math.max(minDamage, player.derivedStats.attack - 5);
        damage = Math.floor(damage * critMultiplier * attackBuff * stageMultiplier);
        
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
        
        // 몬스터 피격 상태 설정 (애니메이션용)
        this.gameState.monsterHit();
        
        // 몬스터 처치 확인
        if (monster.currentHp <= 0) {
            this.killMonster();
        }
    }

    /**
     * 플레이어 체력 소모 (공격 반동)
     */
    consumePlayerHP() {
        const player = this.gameState.player;
        const stage = this.gameState.stage.current;
        
        // 플레이어 체력 소모 (스테이지 비례 고정 수치 - 방어력)
        // 공식: (stage * 5) - playerDefense, 최소 1
        const baseRecoil = stage * 5; // 스테이지당 5 데미지
        const recoilDamage = Math.max(1, baseRecoil - player.derivedStats.defense);
        
        player.currentHp = Math.max(0, player.currentHp - recoilDamage);
        
        gameEventBus.emit(GAME_EVENTS.PLAYER_HP_CHANGED, {
            currentHp: player.currentHp,
            maxHp: player.derivedStats.maxHp
        });
        
        // 플레이어 사망 확인
        if (player.currentHp <= 0) {
            this.playerDefeated();
        }
    }

    /**
     * 플레이어 사망 (체력 0)
     */
    playerDefeated() {
        this.gameState.startDefeat();
        
        gameEventBus.emit(GAME_EVENTS.COMBAT_LOG, {
            message: '플레이어가 쓰러졌습니다...'
        });
        
        gameEventBus.emit(GAME_EVENTS.COMBAT_PHASE_CHANGED, { phase: 'DEFEATED' });
        
        gameLogger.info('Player defeated');
    }

    /**
     * 플레이어 부활
     */
    revivePlayer() {
        const player = this.gameState.player;
        
        // 이전 스테이지로 이동 (최소 1)
        const prevStage = Math.max(1, this.gameState.stage.current - 1);
        this.gameState.stage.current = prevStage;
        
        // 자동반복 모드 활성화
        this.gameState.combatPhase.autoRepeat = true;
        this.gameState.stage.autoRepeat = true;
        
        // 새 스테이지 시작시 HP 완전 회복
        player.currentHp = player.derivedStats.maxHp;
        
        // 전투 초기화
        this.currentMonster = null;
        this.gameState.combatPhase.phase = 'IDLE';
        
        gameEventBus.emit(GAME_EVENTS.COMBAT_LOG, {
            message: `부활했습니다! Stage ${prevStage}에서 재도전 (자동반복 모드)`
        });
        
        gameEventBus.emit(GAME_EVENTS.PLAYER_HP_CHANGED, {
            currentHp: player.currentHp,
            maxHp: player.derivedStats.maxHp
        });
        
        gameEventBus.emit(GAME_EVENTS.STAGE_CHANGED, {
            stage: prevStage,
            isBoss: prevStage % 10 === 0
        });
        
        // 다시 이동 시작
        this.startMoving();
        
        gameLogger.info(`Player revived at stage ${prevStage} with auto-repeat mode`);
    }

    /**
     * 몬스터 처치
     */
    killMonster() {
        const monster = this.currentMonster;
        const player = this.gameState.player;
        
        // 버프 확인
        const goldBuff = this.getBuffMultiplier('goldDouble');
        const expBuff = this.getBuffMultiplier('expDouble');
        
        // 보상 지급 (버프 적용)
        const expReward = Math.floor(monster.expReward * expBuff);
        const goldReward = Math.floor(monster.goldReward * goldBuff);
        
        this.gameState.addExp(expReward);
        this.gameState.addGold(goldReward);
        
        // 아이템 드롭
        this.rollItemDrop();
        
        // 이전 스테이지 저장 (스테이지 변경 감지용)
        const prevStage = this.gameState.stage.current;
        
        // 스테이지 진행 확인 (자동반복 모드가 아닐 때만)
        if (!this.gameState.combatPhase.autoRepeat) {
            this.gameState.killMonster();
        }
        
        // 스테이지가 변경되었으면 HP 회복
        if (this.gameState.stage.current !== prevStage) {
            player.currentHp = player.derivedStats.maxHp;
            gameEventBus.emit(GAME_EVENTS.PLAYER_HP_CHANGED, {
                currentHp: player.currentHp,
                maxHp: player.derivedStats.maxHp
            });
            gameEventBus.emit(GAME_EVENTS.COMBAT_LOG, {
                message: `Stage ${this.gameState.stage.current} 시작! 체력이 회복되었습니다.`
            });
        }
        
        // 이벤트
        gameEventBus.emit(GAME_EVENTS.COMBAT_MONSTER_KILLED, {
            monsterId: monster.id,
            exp: expReward,
            gold: goldReward
        });
        
        // 로그
        if (goldBuff > 1 || expBuff > 1) {
            gameLogger.info(`Monster killed: exp=${expReward} (x${expBuff}), gold=${goldReward} (x${goldBuff})`);
        }
        
        // 보스 처치
        if (monster.isBoss) {
            gameEventBus.emit(GAME_EVENTS.STAGE_BOSS_DEFEATED);
        }
        
        // 승리 페이즈로 전환
        this.gameState.startVictory();
        
        gameEventBus.emit(GAME_EVENTS.COMBAT_VICTORY, {
            monsterId: monster.id,
            exp: expReward,
            gold: goldReward
        });
        
        gameEventBus.emit(GAME_EVENTS.COMBAT_PHASE_CHANGED, { phase: 'VICTORY' });
        
        gameLogger.debug('Monster killed, entering victory phase');
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
                stats: randomItem.stats
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
