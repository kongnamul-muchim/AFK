/**
 * AudioManager - 사운드 관리 (Unity 독립)
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameDataLoader } from '../data-parser/DataLoader.js';
import { gameLogger } from '../core/Logger.js';

class AudioManager {
    constructor(gameState) {
        this.gameState = gameState;
        this.audioContext = null;
        this.buffers = new Map();
        this.bgmSource = null;
        this.sfxVolume = 0.8;
        this.bgmVolume = 0.6;
        this.mute = false;
    }

    /**
     * 오디오 초기화
     */
    async init() {
        try {
            // Web Audio API 초기화
            this.audioContext = new (window.AudioContext || window.webkitAudioContext)();
            
            // 설정 로드
            this.sfxVolume = this.gameState.settings.soundVolume;
            this.bgmVolume = this.gameState.settings.musicVolume;
            this.mute = this.gameState.settings.mute;
            
            // 이벤트 리스너 등록
            gameEventBus.on(GAME_EVENTS.SETTINGS_CHANGED, (data) => {
                if (data.type === 'sfxVolume') this.sfxVolume = data.value;
                if (data.type === 'musicVolume') this.bgmVolume = data.value;
                if (data.type === 'mute') this.mute = data.value;
            });
            
            gameLogger.debug('AudioManager initialized');
        } catch (error) {
            gameLogger.warn('Web Audio API not available:', error);
        }
    }

    /**
     * 사운드 재생 (간소화된 구현)
     * @param {string} soundId 
     */
    playSFX(soundId) {
        if (this.mute || !this.audioContext) return;
        
        // TODO: 실제 사운드 파일 로드 및 재생
        // 현재는 로거만
        gameLogger.debug(`[SFX] ${soundId}`);
    }

    /**
     * BGM 재생
     * @param {string} trackId 
     */
    playBGM(trackId) {
        if (this.mute || !this.audioContext) return;
        
        // TODO: 실제 BGM 파일 로드 및 재생
        gameLogger.debug(`[BGM] ${trackId}`);
    }

    /**
     * BGM 정지
     */
    stopBGM() {
        if (this.bgmSource) {
            this.bgmSource.stop();
            this.bgmSource = null;
        }
    }

    /**
     * 음량 설정
     * @param {string} type - 'sfx' or 'bgm'
     * @param {number} value - 0-1
     */
    setVolume(type, value) {
        if (type === 'sfx') {
            this.sfxVolume = Math.max(0, Math.min(1, value));
        } else if (type === 'bgm') {
            this.bgmVolume = Math.max(0, Math.min(1, value));
        }
    }

    /**
     * 음소거 토글
     */
    toggleMute() {
        this.mute = !this.mute;
        gameEventBus.emit(GAME_EVENTS.SETTINGS_CHANGED, { type: 'mute', value: this.mute });
    }

    /**
     * 정리
     */
    destroy() {
        if (this.audioContext) {
            this.audioContext.close();
        }
        this.stopBGM();
    }
}

export { AudioManager };
