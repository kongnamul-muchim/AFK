/**
 * StorageManager - localStorage 관리
 * 버전 마이그레이션, JSON 내보내기/가져오기 지원
 */
import { gameEventBus, GAME_EVENTS } from './EventBus.js';
import { gameLogger } from './Logger.js';

const STORAGE_KEY = 'idle_rpg_save';
const CURRENT_VERSION = 1;

class StorageManager {
    constructor() {
        this.gameState = null;
        this.autoSaveInterval = null;
        this.debouncedSaveTimer = null;
        this.DEBOUNCE_DELAY = 1000; // 1 초
    }

    /**
     * 저장소 초기화
     * @param {GameState} gameState 
     */
    init(gameState) {
        this.gameState = gameState;
        this.startAutoSave();
        gameLogger.info('StorageManager initialized');
    }

    /**
     * 자동 저장 시작 (5 초마다)
     */
    startAutoSave() {
        if (this.autoSaveInterval) clearInterval(this.autoSaveInterval);
        this.autoSaveInterval = setInterval(() => {
            this.save();
        }, 5000);
        gameLogger.debug('Auto-save started (5s interval)');
    }

    /**
     * 자동 저장 중지
     */
    stopAutoSave() {
        if (this.autoSaveInterval) {
            clearInterval(this.autoSaveInterval);
            this.autoSaveInterval = null;
        }
    }

    /**
     * 지연 저장 (debouncing)
     */
    debouncedSave() {
        if (this.debouncedSaveTimer) clearTimeout(this.debouncedSaveTimer);
        this.debouncedSaveTimer = setTimeout(() => {
            this.save(this.gameState ? this.gameState.toJSON() : null);
        }, this.DEBOUNCE_DELAY);
    }

    /**
     * 게임 상태 저장
     * @param {Object} gameState - GameState.toJSON() 결과
     * @returns {boolean} 성공 여부
     */
    save(gameState) {
        if (!gameState) {
            // gameLogger.warn('Save called without gameState');
            return false;
        }
        try {
            gameState.lastSaveTime = Date.now();
            const data = {
                version: CURRENT_VERSION,
                timestamp: Date.now(),
                gameData: gameState
            };
            localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
            gameEventBus.emit(GAME_EVENTS.GAME_SAVED);
            // gameLogger.debug('Game saved');  // 로그 끄기
            return true;
        } catch (error) {
            gameLogger.error('Failed to save game:', error);
            return false;
        }
    }

    /**
     * 게임 상태 로드
     * @returns {Object|null} 저장된 데이터 또는 null
     */
    load() {
        try {
            const data = localStorage.getItem(STORAGE_KEY);
            if (!data) {
                gameLogger.info('No save data found');
                return null;
            }

            const parsed = JSON.parse(data);
            
            // 버전 확인 및 마이그레이션
            if (parsed.version !== CURRENT_VERSION) {
                gameLogger.info(`Migrating from version ${parsed.version} to ${CURRENT_VERSION}`);
                const migrated = this.migrate(parsed);
                return migrated.gameData;
            }

            gameLogger.info('Game loaded');
            return parsed.gameData;
        } catch (error) {
            gameLogger.error('Failed to load game:', error);
            return null;
        }
    }

    /**
     * 데이터 마이그레이션
     * @param {Object} oldData 
     * @returns {Object} 마이그레이션된 데이터
     */
    migrate(oldData) {
        const data = { ...oldData, version: CURRENT_VERSION };
        
        // v1 → v2 마이그레이션 준비
        // 예: 새로운 필드 추가, 구조 변경 등
        
        return data;
    }

    /**
     * 데이터 내보내기 (JSON 파일 다운로드)
     * @param {Object} gameState 
     */
    exportData(gameState) {
        try {
            const data = {
                version: CURRENT_VERSION,
                exportDate: new Date().toISOString(),
                gameData: gameState
            };
            const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `idle_rpg_save_${new Date().toISOString().split('T')[0]}.json`;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url);
            gameLogger.info('Data exported');
        } catch (error) {
            gameLogger.error('Failed to export data:', error);
            alert('데이터 내보내기 실패');
        }
    }

    /**
     * 데이터 가져오기 (JSON 파일 업로드)
     * @param {File} file 
     * @returns {Promise<Object>} 파싱된 데이터
     */
    importData(file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = (e) => {
                try {
                    const data = JSON.parse(e.target.result);
                    
                    // 유효성 검사
                    if (!data.version || !data.gameData) {
                        throw new Error('Invalid save file format');
                    }
                    
                    resolve(data.gameData);
                    gameLogger.info('Data imported');
                } catch (error) {
                    gameLogger.error('Failed to import data:', error);
                    reject(new Error('잘못된 파일 형식입니다'));
                }
            };
            reader.onerror = () => {
                reject(new Error('파일 읽기 실패'));
            };
            reader.readAsText(file);
        });
    }

    /**
     * 데이터 초기화
     */
    clearData() {
        try {
            localStorage.removeItem(STORAGE_KEY);
            gameLogger.info('Data cleared');
        } catch (error) {
            gameLogger.error('Failed to clear data:', error);
        }
    }

    /**
     * 저장 여부 확인
     * @returns {boolean}
     */
    hasSave() {
        return localStorage.getItem(STORAGE_KEY) !== null;
    }

    /**
     * 정리 (메모리 누수 방지)
     */
    destroy() {
        this.stopAutoSave();
        if (this.debouncedSaveTimer) clearTimeout(this.debouncedSaveTimer);
    }
}

export { StorageManager };
