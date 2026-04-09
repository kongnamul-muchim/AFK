/**
 * Idle RPG - Tower Climber
 * 메인 진입점
 */
import { gameEventBus, GAME_EVENTS } from './core/EventBus.js';
import { GameState } from './core/GameState.js';
import { StorageManager } from './core/StorageManager.js';
import { gameLogger } from './core/Logger.js';
import { gameDataLoader } from './data-parser/DataLoader.js';
import { gameImageLoader } from './core/ImageLoader.js';
import { gameConfig } from './config/GameConfig.js';
import { LoadingScreen } from './ui/LoadingScreen.js';
import { UIManager } from './ui/UIManager.js';
import { InventoryUI } from './ui/InventoryUI.js';
import { AudioManager } from './audio/AudioManager.js';
import { GameRenderer } from './adapters/WebRenderer.js';
import { CombatSystem } from './systems/CombatSystem.js';
import { StageSystem } from './systems/StageSystem.js';
import { InventorySystem } from './systems/InventorySystem.js';
import { OfflineRewards } from './systems/OfflineRewards.js';
import { TutorialSystem } from './systems/TutorialSystem.js';
import { StatsTracker } from './systems/StatsTracker.js';
import { DailyMissionSystem } from './systems/DailyMissionSystem.js';
import { RebirthSystem } from './systems/RebirthSystem.js';
import { UpgradeUI } from './ui/UpgradeUI.js';
import { DailyMissionUI } from './ui/DailyMissionUI.js';
import { GemShopUI } from './ui/GemShopUI.js';

class Game {
    constructor() {
        this.gameState = null;
        this.storageManager = null;
        this.loadingScreen = null;
        this.uiManager = null;
        this.audioManager = null;
        this.renderer = null;
        
        // Systems
        this.combatSystem = null;
        this.stageSystem = null;
        this.inventorySystem = null;
        this.offlineRewards = null;
        this.tutorialSystem = null;
        this.statsTracker = null;
        this.dailyMissionSystem = null;
        this.rebirthSystem = null;
        
        this.isRunning = false;
        this.lastFrameTime = 0;
        this.updateAccumulator = 0;
        this.UPDATE_INTERVAL = 100; // 100ms
    }

    /**
     * 게임 초기화
     */
    async init() {
        gameLogger.info('Initializing game...');
        
        try {
            // 로딩 화면 표시
            this.loadingScreen = new LoadingScreen();
            this.loadingScreen.show();
            this.updateLoadingProgress(10, '시스템 초기화...');

            // 스토리지 매니저 초기화
            this.storageManager = new StorageManager();
            // gameState 는 나중에 생성되므로 일단 null
            this.updateLoadingProgress(20, '데이터 로드 중...');

            // CSV 데이터 로드
            await gameDataLoader.loadAll();
            this.updateLoadingProgress(50, '이미지 로드 중...');

            // GameConfig 초기화 (CSV 기반 설정)
            gameConfig.init();

            // 이미지 로드 (분할된 스프라이트 + 배경)
            try {
                await gameImageLoader.loadAll({
                    // 플레이어 프레임 (8 개)
                    player_0: 'Assets/images/characters/player_spritesheet_0.png',
                    player_1: 'Assets/images/characters/player_spritesheet_1.png',
                    player_2: 'Assets/images/characters/player_spritesheet_2.png',
                    player_3: 'Assets/images/characters/player_spritesheet_3.png',
                    player_4: 'Assets/images/characters/player_spritesheet_4.png',
                    player_5: 'Assets/images/characters/player_spritesheet_5.png',
                    player_6: 'Assets/images/characters/player_spritesheet_6.png',
                    player_7: 'Assets/images/characters/player_spritesheet_7.png',
                    // 몬스터 프레임 (8 개 - 순차적)
                    monster_0: 'Assets/images/monsters/slime_spritesheet_0.png',
                    monster_1: 'Assets/images/monsters/slime_spritesheet_1.png',
                    monster_2: 'Assets/images/monsters/slime_spritesheet_2.png',
                    monster_3: 'Assets/images/monsters/slime_spritesheet_3.png',
                    monster_4: 'Assets/images/monsters/slime_spritesheet_4.png',
                    monster_5: 'Assets/images/monsters/slime_spritesheet_5.png',
                    monster_6: 'Assets/images/monsters/slime_spritesheet_6.png',
                    monster_7: 'Assets/images/monsters/slime_spritesheet_7.png',
                    // 배경
                    background_normal: 'Assets/images/backgrounds/background_normal.png',
                    background_boss: 'Assets/images/backgrounds/background_boss.png'
                });
                gameLogger.info('Images loaded successfully');
            } catch (error) {
                gameLogger.warn('Some images failed to load (using fallback graphics):', error.message);
            }
            
            // 로딩 완료 (이미지 실패해도 진행)
            this.updateLoadingProgress(70, 'UI 초기화...');

            // 게임 상태 로드 또는 신규 생성
            const savedData = this.storageManager.load();
            let offlineRewardData = null; // 오프라인 보상 데이터 저장용
            
            if (savedData) {
                this.gameState = new GameState();
                this.gameState.fromJSON(savedData);
                
                // items.csv ID 체계 리팩토링(v2)으로 인한 세이브 데이터 하드리셋
                // 기존 세이브 데이터의 아이템 ID가 새 체계와 호환되지 않음
                // 단, 한 번만 실행 (플래그로 관리)
                if (!this.gameState.inventoryResetDone) {
                    gameLogger.info('Performing hard reset of inventory due to ID system refactoring (v2)');
                    this.gameState.hardResetInventory();
                    this.gameState.inventoryResetDone = true;
                    this.storageManager.save(); // 플래그 저장
                }
                
                // 오프라인 보상 계산 (UI 표시는 나중에)
                const offlineSeconds = (Date.now() - savedData.lastSaveTime) / 1000;
                gameLogger.info(`Offline seconds: ${offlineSeconds}, condition: ${offlineSeconds > 60}`);
                if (offlineSeconds > 60) {
                    // 1. 오프라인 시간 계산
                    const hours = Math.min(offlineSeconds / 3600, gameConfig.offline.maxHours || 24);
                    
                    // 2. 최대 스테이지 가져오기 (환생 시 초기화됨)
                    const maxStage = this.gameState.stage.maxStage || 1;
                    
                    // 3. 처치 수 계산 (최대 스테이지 몬스터 기준)
                    const attackSpeedMs = 100; // 기본 공격 속도
                    const baseInterval = 100; // combat.attackInterval
                    const actualIntervalMs = baseInterval / (attackSpeedMs / 100);
                    const attacksPerSecond = 1000 / actualIntervalMs;
                    
                    // 몬스터 HP/골드/경험치 (스테이지 기반)
                    // monsters.csv에서 최대 스테이지 몬스터 정보 가져오기
                    const monsters = gameDataLoader.get('monsters');
                    let monsterData = null;
                    if (monsters && monsters.length > 0) {
                        // maxStage에 해당하는 몬스터 찾기 (없으면 첫 번째 몬스터)
                        monsterData = monsters.find(m => m.stage === maxStage) || monsters[0];
                    }
                    
                    const monsterHpBase = monsterData ? monsterData.hp_base : 50;
                    const monsterHpScale = monsterData ? monsterData.hp_scale : 15;
                    const monsterHp = monsterHpBase + (maxStage - 1) * monsterHpScale;
                    
                    const goldPerKillBase = monsterData ? monsterData.gold_reward : 5;
                    const goldPerKillScale = monsterData ? monsterData.gold_scale : 1;
                    const goldPerKill = goldPerKillBase + (maxStage - 1) * goldPerKillScale;
                    
                    const expPerKillBase = monsterData ? monsterData.exp_reward : 10;
                    const expPerKillScale = monsterData ? monsterData.exp_scale : 2;
                    const expPerKill = expPerKillBase + (maxStage - 1) * expPerKillScale;
                    
                    const playerDamage = this.gameState.player.derivedStats.attack || 10;
                    const attacksPerKill = Math.ceil(Math.max(1, monsterHp / playerDamage));
                    const killsPerSecond = attacksPerSecond / attacksPerKill;
                    const kills = Math.floor(killsPerSecond * offlineSeconds);
                    
                    // 4. 골드/경험치 계산
                    const totalGold = Math.floor(kills * goldPerKill);
                    const totalExp = Math.floor(kills * expPerKill);
                    
                    // 5. 장비 드롭 계산 (최대 스테이지에 해당하는 등급의 장비)
                    const maxDropsPerHour = 12.5;
                    const maxDrops = Math.floor(maxDropsPerHour * hours);
                    const variance = 0.8 + Math.random() * 0.4;
                    const actualDrops = Math.floor(maxDrops * variance);
                    
                    // 6. 골드/경험치 1/10 배율 적용 (온라인의 10%)
                    // 장비 드롭은 그대로 유지
                    let rewardScale = 0.1;
                    
                    // 보석 업그레이드: 오프라인 보상 증가 (2%/레벨)
                    const offlineBonus = this.gameState.gemUpgrades.offlineBonus || 0;
                    rewardScale *= (1 + offlineBonus * 0.02);
                    
                    const scaledKills = Math.max(1, Math.floor(kills * rewardScale));
                    const scaledGold = Math.max(1, Math.floor(totalGold * rewardScale));
                    const scaledExp = Math.max(1, Math.floor(totalExp * rewardScale));
                    
                    const equipmentDrops = [];
                    const items = gameDataLoader.get('items');
                    if (items) {
                        // 보석 업그레이드: 드롭 확률 업 (등급별 차등 적용)
                        const dropRateLevel = this.gameState.gemUpgrades.dropRate || 0;
                        const dropRates = {
                            mythic: 0.005 + (dropRateLevel * 0.001),      // 전설: 0.5% → 2.5%
                            legendary: 0.025 + (dropRateLevel * 0.004),    // 영웅: 2.5% → 10.5%
                            epic: 0.07 + (dropRateLevel * 0.004),          // 희귀: 7% → 15%
                            rare: 0.20 + (dropRateLevel * 0.002),          // 고급: 20% → 24%
                            common: 0.50 + (dropRateLevel * -0.011)        // 일반: 50% → 28%
                        };
                        
                        for (let i = 0; i < actualDrops; i++) {
                            const rarityRoll = Math.random();
                            let rarity;
                            if (rarityRoll < dropRates.mythic) rarity = 'mythic';
                            else if (rarityRoll < dropRates.mythic + dropRates.legendary) rarity = 'legendary';
                            else if (rarityRoll < dropRates.mythic + dropRates.legendary + dropRates.epic) rarity = 'epic';
                            else if (rarityRoll < dropRates.mythic + dropRates.legendary + dropRates.epic + dropRates.rare) rarity = 'rare';
                            else rarity = 'common';
                            
                            // 최대 스테이지에 해당하는 등급의 장비 (maxStage에 맞는 grade)
                            // grade 1-5는 stage 1-5, grade 6-10은 stage 6-10, ...
                            const targetGrade = Math.min(maxStage, 50); // 최대 50
                            const gradeRange = 5; // ±5 범위
                            const minGrade = Math.max(1, targetGrade - gradeRange);
                            const maxGrade = Math.min(50, targetGrade + gradeRange);
                            
                            const candidates = items.filter(item => 
                                (item.type === 'weapon' || item.type === 'armor' || 
                                 item.type === 'boots' || item.type === 'accessory') &&
                                item.grade >= minGrade && item.grade <= maxGrade &&
                                item.rarity === rarity
                            );
                            
                            if (candidates.length > 0) {
                                const selected = candidates[Math.floor(Math.random() * candidates.length)];
                                equipmentDrops.push({
                                    itemId: selected.id,
                                    name: selected.name,
                                    grade: selected.grade,
                                    rarity: selected.rarity,
                                    type: selected.type,
                                    stats: selected.stats
                                });
                            }
                        }
                    }
                    
                    // 오프라인 보상 데이터 저장 (UI 표시는 나중에)
                    offlineRewardData = {
                        hours,
                        kills: scaledKills,
                        gold: scaledGold,
                        exp: scaledExp,
                        equipment: equipmentDrops
                    };
                    
                    gameLogger.info(`Offline reward calculated: ${hours.toFixed(1)}h, ${kills} kills, ${totalGold} gold, ${totalExp} exp, ${equipmentDrops.length} equipment`);
                }
                
                gameLogger.info('Game loaded from save (inventory reset)');
            } else {
                this.gameState = new GameState();
                gameLogger.info('New game created');
            }
            
            // StorageManager 에 gameState 연동
            this.storageManager.init(this.gameState);
            
            this.updateLoadingProgress(70, 'UI 초기화...');

            // UI 매니저 초기화
            this.uiManager = new UIManager(this.gameState);
            this.uiManager.game = this; // game 참조 추가
            this.uiManager.init();

            // 오디오 매니저 초기화
            this.audioManager = new AudioManager(this.gameState);
            this.audioManager.init();

            // 시스템 초기화
            this.stageSystem = new StageSystem(this.gameState);
            this.stageSystem.init();
            
            this.inventorySystem = new InventorySystem(this.gameState);
            this.inventorySystem.init();
            
            // 인벤토리 UI 초기화
            this.inventoryUI = new InventoryUI(this.gameState, this.inventorySystem);
            this.inventoryUI.init();
            
            this.offlineRewards = new OfflineRewards(this.gameState);
            this.offlineRewards.init();
            
            this.tutorialSystem = new TutorialSystem(this.gameState);
            this.tutorialSystem.init();
            
            this.statsTracker = new StatsTracker(this.gameState);
            this.statsTracker.init();
            
            // 일일 미션 시스템
            this.dailyMissionSystem = new DailyMissionSystem(this.gameState);
            this.dailyMissionSystem.init();
            
            this.combatSystem = new CombatSystem(this.gameState);
            this.combatSystem.init();

            // 렌더러 초기화 (combatSystem 전달)
            this.renderer = new GameRenderer(this.gameState, this.combatSystem);
            this.renderer.init();
            
            // UI 초기화
            this.dailyMissionUI = new DailyMissionUI(this.gameState, this.dailyMissionSystem);
            this.dailyMissionUI.init();
            
            this.gemShopUI = new GemShopUI(this.gameState, this.dailyMissionSystem);
            this.gemShopUI.init();
            
            // 환생 시스템
            this.rebirthSystem = new RebirthSystem(this.gameState);
            this.rebirthSystem.init();
            
            // 업그레이드 UI 초기화 (rebirthSystem 필요)
            this.upgradeUI = new UpgradeUI(this.gameState, this.rebirthSystem);
            this.upgradeUI.init();
            
            // UI 초기화
            this.dailyMissionUI = new DailyMissionUI(this.gameState, this.dailyMissionSystem);
            this.dailyMissionUI.init();
            
            this.gemShopUI = new GemShopUI(this.gameState, this.dailyMissionSystem);
            this.gemShopUI.init();
            
            this.updateLoadingProgress(90, '마지막 준비...');

            // 이벤트 리스너 등록
            this.setupEventListeners();

            // 로딩 화면 숨김
            this.loadingScreen.hide();
            document.getElementById('game-container').style.display = 'flex';
            
            this.updateLoadingProgress(100, '완료!');
            
            gameLogger.info('Game initialized successfully');
            gameEventBus.emit(GAME_EVENTS.GAME_LOADED);
            
            this.isRunning = true;
            this.lastFrameTime = performance.now();
            this.gameLoop();
            
            // 오프라인 보상이 있으면 UI 표시 (로딩 완료 후)
            if (offlineRewardData && this.uiManager) {
                // 약간의 지연 후 표시 (UI 안정화)
                setTimeout(() => {
                    this.uiManager.showOfflineRewardDetailed(offlineRewardData);
                    
                    // 보상 지급 (UI 표시 후)
                    this.gameState.addGold(offlineRewardData.gold);
                    this.gameState.addExp(offlineRewardData.exp);
                    
                    offlineRewardData.equipment.forEach(equip => {
                        this.gameState.inventory.addItem(equip);
                    });
                    
                    // 저장 시간 업데이트 (오프라인 보상 지급 후)
                    this.gameState.lastSaveTime = Date.now();
                    this.storageManager.save(); // 즉시 저장
                    
                    gameLogger.info('Offline reward UI displayed and rewards granted');
                }, 500);
            }
            
        } catch (error) {
            gameLogger.error('Failed to initialize game:', error);
            this.loadingScreen.showError(error.message);
        }
    }

    /**
     * 로딩 진행률 업데이트
     * @param {number} percent 
     * @param {string} tip 
     */
    updateLoadingProgress(percent, tip) {
        if (this.loadingScreen) {
            this.loadingScreen.updateProgress(percent, tip);
        }
    }

    /**
     * 오프라인 보상 계산
     * @param {number} seconds 
     */
    calculateOfflineReward(seconds) {
        const hours = Math.min(seconds / 3600, 24); // 최대 24 시간
        const expPerHour = gameDataLoader.getConfigNumber('offline', 'expPerHour', 100);
        const goldPerHour = gameDataLoader.getConfigNumber('offline', 'goldPerHour', 50);
        
        const expReward = Math.floor(hours * expPerHour);
        const goldReward = Math.floor(hours * goldPerHour);
        
        this.gameState.addExp(expReward);
        this.gameState.addGold(goldReward);
        
        // 오프라인 보상 모달 표시
        if (this.uiManager) {
            this.uiManager.showOfflineReward(hours, expReward, goldReward);
        }
        
        gameLogger.info(`Offline reward: ${hours.toFixed(1)}h, ${expReward} exp, ${goldReward} gold`);
    }

    /**
     * 이벤트 리스너 설정
     */
    setupEventListeners() {
        // 자동 저장
        gameEventBus.on('game:state_changed', () => {
            this.storageManager.debouncedSave();
        });

        // 게임 저장
        gameEventBus.on(GAME_EVENTS.GAME_SAVED, () => {
            gameLogger.debug('Game saved');
        });

        // 상태창 업데이트
        gameEventBus.on(GAME_EVENTS.PLAYER_LEVELUP, (data) => {
            this.uiManager.updateHUD();
            this.uiManager.showToast(`레벨업! Lv.${data.level}`);
        });

        gameEventBus.on(GAME_EVENTS.PLAYER_HP_CHANGED, () => {
            this.uiManager.updateHUD();
        });

        gameEventBus.on(GAME_EVENTS.PLAYER_EXP_CHANGED, () => {
            this.uiManager.updateHUD();
        });

        gameEventBus.on(GAME_EVENTS.PLAYER_STAT_CHANGED, () => {
            this.uiManager.updateStatsPanel();
        });

        gameEventBus.on(GAME_EVENTS.INVENTORY_GOLD_CHANGED, () => {
            this.uiManager.updateHUD();
        });

        gameEventBus.on(GAME_EVENTS.STAGE_CHANGED, (data) => {
            this.uiManager.updateHUD();
            if (data.isBoss) {
                this.uiManager.showToast('보스가 등장합니다!');
            }
        });

        gameEventBus.on(GAME_EVENTS.COMBAT_LOG, (data) => {
            this.uiManager.addCombatLog(data.message);
        });
    }

    /**
     * 게임 루프
     * @param {number} currentTime 
     */
    gameLoop(currentTime = 0) {
        if (!this.isRunning) return;

        const deltaTime = currentTime - this.lastFrameTime;
        this.lastFrameTime = currentTime;
        
        // 업데이트 (고정 시간 간격)
        this.updateAccumulator += deltaTime;
        while (this.updateAccumulator >= this.UPDATE_INTERVAL) {
            this.update(this.UPDATE_INTERVAL);
            this.updateAccumulator -= this.UPDATE_INTERVAL;
        }

        // 렌더링
        this.render(deltaTime);

        // 다음 프레임
        requestAnimationFrame((t) => this.gameLoop(t));
    }

    /**
     * 게임 업데이트
     * @param {number} dt 
     */
    update(dt) {
        // 플레이 시간 기록
        this.statsTracker.updatePlayTime(dt);
        
        // UI 업데이트
        if (this.uiManager) {
            this.uiManager.updateGameView();
        }
        
        // 전투 시스템 업데이트 (새로운 루프 시스템)
        if (!this.combatSystem.isRunning) {
            this.combatSystem.startCombat();
        } else {
            this.combatSystem.update(dt);
        }
    }

    /**
     * 렌더링
     * @param {number} dt 
     */
    render(dt) {
        if (this.renderer) {
            this.renderer.render(dt);
        }
    }

    /**
     * 게임 시작
     */
    start() {
        if (!this.isRunning) {
            this.isRunning = true;
            this.lastFrameTime = performance.now();
            gameEventBus.emit(GAME_EVENTS.GAME_RESUMED);
            gameLogger.info('Game started');
        }
    }

    /**
     * 게임 일시정지
     */
    pause() {
        if (this.isRunning) {
            this.isRunning = false;
            gameEventBus.emit(GAME_EVENTS.GAME_PAUSED);
            gameLogger.info('Game paused');
        }
    }

    /**
     * 게임 재개
     */
    resume() {
        this.start();
    }

    /**
     * 게임 정리
     */
    destroy() {
        this.isRunning = false;
        
        if (this.storageManager) {
            this.storageManager.save(this.gameState.toJSON());
            this.storageManager.destroy();
        }
        
        if (this.renderer) {
            this.renderer.destroy();
        }
        
        gameEventBus.removeAllListeners();
        gameLogger.info('Game destroyed');
    }
}

// 게임 인스턴스 생성 및 시작
const game = new Game();

// DOMContentLoaded 후 초기화
document.addEventListener('DOMContentLoaded', () => {
    game.init();
});

// 페이지 이탈 시 저장
window.addEventListener('beforeunload', () => {
    if (game.gameState) {
        game.storageManager.save();
    }
});

// 전역 게임 객체 (디버깅용)
window.game = game;

export { Game };
