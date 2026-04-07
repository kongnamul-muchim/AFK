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
import { LoadingScreen } from './ui/LoadingScreen.js';
import { UIManager } from './ui/UIManager.js';
import { AudioManager } from './audio/AudioManager.js';
import { GameRenderer } from './adapters/WebRenderer.js';
import { CombatSystem } from './systems/CombatSystem.js';
import { StageSystem } from './systems/StageSystem.js';
import { InventorySystem } from './systems/InventorySystem.js';
import { OfflineRewards } from './systems/OfflineRewards.js';
import { TutorialSystem } from './systems/TutorialSystem.js';
import { AchievementSystem } from './systems/AchievementSystem.js';
import { StatsTracker } from './systems/StatsTracker.js';

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
        this.achievementSystem = null;
        this.statsTracker = null;
        
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

            // 이미지 로드 (분할된 스프라이트 + 배경)
            try {
                await gameImageLoader.loadAll({
                    // 플레이어 프레임 (8 개)
                    player_0: 'assets/images/characters/player_spritesheet_0.png',
                    player_1: 'assets/images/characters/player_spritesheet_1.png',
                    player_2: 'assets/images/characters/player_spritesheet_2.png',
                    player_3: 'assets/images/characters/player_spritesheet_3.png',
                    player_4: 'assets/images/characters/player_spritesheet_4.png',
                    player_5: 'assets/images/characters/player_spritesheet_5.png',
                    player_6: 'assets/images/characters/player_spritesheet_6.png',
                    player_7: 'assets/images/characters/player_spritesheet_7.png',
                    // 몬스터 프레임 (8 개 - 순차적)
                    monster_0: 'assets/images/monsters/slime_spritesheet_0.png',
                    monster_1: 'assets/images/monsters/slime_spritesheet_1.png',
                    monster_2: 'assets/images/monsters/slime_spritesheet_2.png',
                    monster_3: 'assets/images/monsters/slime_spritesheet_3.png',
                    monster_4: 'assets/images/monsters/slime_spritesheet_4.png',
                    monster_5: 'assets/images/monsters/slime_spritesheet_5.png',
                    monster_6: 'assets/images/monsters/slime_spritesheet_6.png',
                    monster_7: 'assets/images/monsters/slime_spritesheet_7.png',
                    // 배경
                    background_normal: 'assets/images/backgrounds/background_normal.png',
                    background_boss: 'assets/images/backgrounds/background_boss.png'
                });
                gameLogger.info('Images loaded successfully');
            } catch (error) {
                gameLogger.warn('Some images failed to load (using fallback graphics):', error.message);
            }
            
            // 로딩 완료 (이미지 실패해도 진행)
            this.updateLoadingProgress(70, 'UI 초기화...');

            // 게임 상태 로드 또는 신규 생성
            const savedData = this.storageManager.load();
            if (savedData) {
                this.gameState = new GameState();
                this.gameState.fromJSON(savedData);
                
                // 오프라인 보상 계산
                const offlineSeconds = (Date.now() - savedData.lastSaveTime) / 1000;
                if (offlineSeconds > 60) {
                    this.calculateOfflineReward(offlineSeconds);
                }
                
                gameLogger.info('Game loaded from save');
            } else {
                this.gameState = new GameState();
                gameLogger.info('New game created');
            }
            
            // StorageManager 에 gameState 연동
            this.storageManager.init(this.gameState);
            
            this.updateLoadingProgress(70, 'UI 초기화...');

            // UI 매니저 초기화
            this.uiManager = new UIManager(this.gameState);
            this.uiManager.init();

            // 오디오 매니저 초기화
            this.audioManager = new AudioManager(this.gameState);
            this.audioManager.init();

            // 시스템 초기화
            this.stageSystem = new StageSystem(this.gameState);
            this.stageSystem.init();
            
            this.inventorySystem = new InventorySystem(this.gameState);
            this.inventorySystem.init();
            
            this.offlineRewards = new OfflineRewards(this.gameState);
            this.offlineRewards.init();
            
            this.tutorialSystem = new TutorialSystem(this.gameState);
            this.tutorialSystem.init();
            
            this.achievementSystem = new AchievementSystem(this.gameState);
            this.achievementSystem.init();
            
            this.statsTracker = new StatsTracker(this.gameState);
            this.statsTracker.init();
            
            this.combatSystem = new CombatSystem(this.gameState);
            this.combatSystem.init();

            // 렌더러 초기화
            this.renderer = new GameRenderer(this.gameState);
            this.renderer.init();
            
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
        
        // 전투 시스템 업데이트 (자동 공격)
        if (!this.combatSystem.isAttacking) {
            this.combatSystem.startCombat();
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
        game.storageManager.save(game.gameState.toJSON());
    }
});

// 전역 게임 객체 (디버깅용)
window.game = game;

export { Game };
