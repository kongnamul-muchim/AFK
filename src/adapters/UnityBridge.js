/**
 * UnityBridge - Unity 와 JavaScript 간 통신 브릿지
 * 
 * Unity(WebGL) 에서 JavaScript 게임 로직을 호출하기 위한 인터페이스
 * 웹 브라우저에서 Unity 연동을 테스트할 수 있음
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameLogger } from '../core/Logger.js';

class UnityBridge {
    constructor(game) {
        this.game = game;
        this.isUnityConnected = false;
        this.messageQueue = [];
        
        // Unity 에서 호출 가능한 함수들을 window 에 등록
        this.registerUnityFunctions();
        
        gameLogger.info('UnityBridge initialized');
    }

    /**
     * Unity 호출 가능 함수 등록 (window 객체)
     */
    registerUnityFunctions() {
        // Unity WebGL 에서 호출: UnityBridge.funcName(args)
        window.UnityBridge = {
            // 초기화
            init: () => this.handleUnityInit(),
            
            // 입력 처리
            onStatPointUsed: (statType) => this.handleStatPointUsed(statType),
            onItemSynthesized: (itemId) => this.handleItemSynthesized(itemId),
            onSettingsChanged: (settings) => this.handleSettingsChanged(settings),
            
            // 게임 제어
            pause: () => this.handlePause(),
            resume: () => this.handleResume(),
            save: () => this.handleSave(),
            load: () => this.handleLoad(),
            
            // Unity 상태 확인
            isConnected: () => this.isUnityConnected,
            
            // 메시지 전송 (Unity → JS)
            sendMessage: (message) => this.handleUnityMessage(message)
        };
        
        gameLogger.debug('Unity functions registered on window.UnityBridge');
    }

    /**
     * Unity 초기화 완료 핸들러
     */
    handleUnityInit() {
        this.isUnityConnected = true;
        gameLogger.info('Unity connected!');
        
        // 대기 중인 메시지 처리
        this.processMessageQueue();
        
        // Unity 에 게임 상태 전송
        this.sendGameStateToUnity();
    }

    /**
     * 스탯 포인트 사용 핸들러
     * @param {string} statType - 'str', 'agi', 'int', 'vit'
     */
    handleStatPointUsed(statType) {
        if (this.game.gameState) {
            const success = this.game.gameState.increaseStat(statType);
            this.sendToUnity('OnStatPointResult', JSON.stringify({
                statType,
                success,
                newValue: this.game.gameState.player.stats[statType],
                remainingPoints: this.game.gameState.player.statPoints
            }));
        }
    }

    /**
     * 아이템 합성 핸들러
     * @param {number} itemId 
     */
    handleItemSynthesized(itemId) {
        if (this.game.inventorySystem) {
            const result = this.game.inventorySystem.synthesize(itemId);
            this.sendToUnity('OnSynthesizeResult', JSON.stringify({
                success: result !== null,
                resultId: result?.id,
                resultName: result?.name,
                resultGrade: result?.grade
            }));
        }
    }

    /**
     * 설정 변경 핸들러
     * @param {Object} settings 
     */
    handleSettingsChanged(settings) {
        if (this.game.gameState) {
            Object.assign(this.game.gameState.settings, settings);
            gameEventBus.emit(GAME_EVENTS.SETTINGS_CHANGED, settings);
            this.sendToUnity('OnSettingsChanged', JSON.stringify({ success: true }));
        }
    }

    /**
     * 게임 일시정지
     */
    handlePause() {
        if (this.game) {
            this.game.pause();
            this.sendToUnity('OnGamePaused', JSON.stringify({ success: true }));
        }
    }

    /**
     * 게임 재개
     */
    handleResume() {
        if (this.game) {
            this.game.resume();
            this.sendToUnity('OnGameResumed', JSON.stringify({ success: true }));
        }
    }

    /**
     * 게임 저장
     */
    handleSave() {
        if (this.game.storageManager && this.game.gameState) {
            const success = this.game.storageManager.save(this.game.gameState.toJSON());
            this.sendToUnity('OnSaveComplete', JSON.stringify({ success }));
        }
    }

    /**
     * 게임 로드
     */
    handleLoad() {
        if (this.game.storageManager) {
            const data = this.game.storageManager.load();
            this.sendToUnity('OnLoadComplete', JSON.stringify({
                success: data !== null,
                data: data
            }));
        }
    }

    /**
     * Unity 로부터 메시지 수신
     * @param {string} message - JSON 문자열
     */
    handleUnityMessage(message) {
        try {
            const data = JSON.parse(message);
            gameLogger.debug('Message from Unity:', data);
            
            switch (data.type) {
                case 'input':
                    this.handleUnityInput(data.payload);
                    break;
                case 'render_complete':
                    this.handleRenderComplete(data.payload);
                    break;
                case 'audio_play':
                    this.handleAudioPlay(data.payload);
                    break;
            }
        } catch (error) {
            gameLogger.error('Failed to parse Unity message:', error);
        }
    }

    /**
     * Unity 입력 처리
     * @param {Object} input 
     */
    handleUnityInput(input) {
        switch (input.action) {
            case 'click_stat':
                this.handleStatPointUsed(input.statType);
                break;
            case 'click_synthesize':
                this.handleItemSynthesized(input.itemId);
                break;
            case 'click_settings':
                // 설정 화면 열기
                break;
        }
    }

    /**
     * Unity 렌더링 완료 처리
     * @param {Object} data 
     */
    handleRenderComplete(data) {
        // 다음 프레임 업데이트
        gameLogger.debug('Render complete, delta:', data.deltaTime);
    }

    /**
     * Unity 오디오 재생 요청
     * @param {Object} data 
     */
    handleAudioPlay(data) {
        if (this.game.audioManager) {
            if (data.type === 'sfx') {
                this.game.audioManager.playSFX(data.soundId);
            } else if (data.type === 'bgm') {
                this.game.audioManager.playBGM(data.trackId);
            }
        }
    }

    /**
     * Unity 에 데이터 전송
     * @param {string} functionName - Unity side function name
     * @param {string} data - JSON string
     */
    sendToUnity(functionName, data) {
        if (this.isUnityConnected) {
            // Unity WebGL 에서는 SendMessage 사용
            if (window.GameObject && window.GameObject.SendMessage) {
                window.GameObject.SendMessage(functionName, data);
            } else {
                // 테스트 모드: 콘솔 로그
                gameLogger.debug(`[To Unity] ${functionName}:`, data);
            }
        } else {
            // 대기 큐에 추가
            this.messageQueue.push({ functionName, data });
            gameLogger.debug(`[Queued] ${functionName}:`, data);
        }
    }

    /**
     * 대기 중인 메시지 처리
     */
    processMessageQueue() {
        this.messageQueue.forEach(({ functionName, data }) => {
            this.sendToUnity(functionName, data);
        });
        this.messageQueue = [];
    }

    /**
     * 게임 상태 Unity 에 전송
     */
    sendGameStateToUnity() {
        if (!this.game.gameState) return;
        
        const state = {
            player: {
                level: this.game.gameState.player.level,
                exp: this.game.gameState.player.exp,
                maxExp: this.game.gameState.player.maxExp,
                currentHp: this.game.gameState.player.currentHp,
                maxHp: this.game.gameState.player.maxHp,
                stats: this.game.gameState.player.stats,
                statPoints: this.game.gameState.player.statPoints
            },
            stage: {
                current: this.game.gameState.stage.current,
                max: this.game.gameState.stage.max,
                kills: this.game.gameState.stage.kills
            },
            inventory: {
                gold: this.game.gameState.inventory.gold,
                itemCount: this.game.gameState.inventory.items.size
            }
        };
        
        this.sendToUnity('OnGameStateUpdate', JSON.stringify(state));
    }

    /**
     * 이벤트 기반 상태 업데이트 구독
     */
    setupEventListeners() {
        // 플레이어 상태 변경
        gameEventBus.on(GAME_EVENTS.PLAYER_LEVELUP, (data) => {
            this.sendToUnity('OnPlayerLevelUp', JSON.stringify(data));
        });
        
        gameEventBus.on(GAME_EVENTS.PLAYER_HP_CHANGED, (data) => {
            this.sendToUnity('OnPlayerHpChanged', JSON.stringify(data));
        });
        
        gameEventBus.on(GAME_EVENTS.PLAYER_EXP_CHANGED, (data) => {
            this.sendToUnity('OnPlayerExpChanged', JSON.stringify(data));
        });
        
        // 인벤토리 변경
        gameEventBus.on(GAME_EVENTS.INVENTORY_GOLD_CHANGED, (data) => {
            this.sendToUnity('OnGoldChanged', JSON.stringify(data));
        });
        
        gameEventBus.on(GAME_EVENTS.INVENTORY_ITEM_ADDED, (data) => {
            this.sendToUnity('OnItemAdded', JSON.stringify(data));
        });
        
        // 스테이지 변경
        gameEventBus.on(GAME_EVENTS.STAGE_CHANGED, (data) => {
            this.sendToUnity('OnStageChanged', JSON.stringify(data));
        });
        
        // 전투 로그
        gameEventBus.on(GAME_EVENTS.COMBAT_LOG, (data) => {
            this.sendToUnity('OnCombatLog', JSON.stringify(data));
        });
        
        gameLogger.debug('UnityBridge event listeners setup complete');
    }

    /**
     * 연결 테스트 (웹 테스트용)
     */
    testConnection() {
        gameLogger.info('Testing Unity connection...');
        
        // 테스트 데이터 전송
        this.sendToUnity('OnTestConnection', JSON.stringify({
            timestamp: Date.now(),
            message: 'Hello from JavaScript!'
        }));
        
        return true;
    }

    /**
     * 정리
     */
    destroy() {
        this.isUnityConnected = false;
        this.messageQueue = [];
        gameLogger.debug('UnityBridge destroyed');
    }
}

export { UnityBridge };
