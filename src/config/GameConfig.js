/**
 * GameConfig - 게임 설정 관리 (CSV 기반)
 * DataLoader 를 통해 CSV 에서 값을 읽어옴
 */
import { gameDataLoader } from '../data-parser/DataLoader.js';
import { gameLogger } from './Logger.js';

class GameConfig {
    constructor() {
        this.configs = new Map();
    }

    /**
     * 설정 초기화 (CSV 에서 로드)
     */
    init() {
        const configData = gameDataLoader.get('game_config');
        if (!configData) {
            gameLogger.warn('Game config not loaded, using defaults');
            return;
        }

        // CSV 데이터를 계층적 구조로 변환
        configData.forEach(row => {
            const { category, key, value } = row;
            
            if (!this.configs.has(category)) {
                this.configs.set(category, new Map());
            }
            
            const categoryConfig = this.configs.get(category);
            categoryConfig.set(key, this.parseValue(value));
        });

        gameLogger.info('GameConfig initialized');
    }

    /**
     * 값 파싱 (자동 타입 변환)
     * @param {string} value 
     * @returns {number|string|boolean|Object}
     */
    parseValue(value) {
        if (value === null || value === undefined) return null;
        
        // 문자열
        if (typeof value === 'string') {
            // 숫자
            if (/^-?\d+(\.\d+)?$/.test(value)) {
                return parseFloat(value);
            }
            // 불리언
            if (value.toLowerCase() === 'true') return true;
            if (value.toLowerCase() === 'false') return false;
            // JSON 객체
            if (value.startsWith('{') || value.startsWith('[')) {
                try {
                    return JSON.parse(value);
                } catch (e) {
                    return value;
                }
            }
        }
        
        return value;
    }

    /**
     * 설정 값 조회
     * @param {string} category 
     * @param {string} key 
     * @param {*} defaultValue 
     * @returns {*}
     */
    get(category, key, defaultValue = null) {
        if (!this.configs.has(category)) {
            return defaultValue;
        }
        
        const categoryConfig = this.configs.get(category);
        return categoryConfig.get(key) ?? defaultValue;
    }

    /**
     * 숫자 설정 값 조회
     * @param {string} category 
     * @param {string} key 
     * @param {number} defaultValue 
     * @returns {number}
     */
    getNumber(category, key, defaultValue = 0) {
        const value = this.get(category, key, defaultValue);
        return typeof value === 'number' ? value : parseFloat(value) || defaultValue;
    }

    /**
     * 플레이어 설정
     */
    player = {
        get baseExp() {
            return gameConfig.getNumber('player', 'baseExp', 100);
        },
        get expMultiplier() {
            return gameConfig.getNumber('player', 'expMultiplier', 1.2);
        },
        get statPointsPerLevel() {
            return gameConfig.getNumber('player', 'statPointsPerLevel', 1);
        },
        get baseHp() {
            return gameConfig.getNumber('player', 'baseHp', 100);
        },
        get hpPerVit() {
            return gameConfig.getNumber('player', 'hpPerVit', 10);
        }
    };

    /**
     * 전투 설정
     */
    combat = {
        get attackInterval() {
            return gameConfig.getNumber('combat', 'attackInterval', 100);
        },
        get monsterScalingMultiplier() {
            return gameConfig.getNumber('combat', 'monsterScalingMultiplier', 1.1);
        },
        get minDamage() {
            return gameConfig.getNumber('combat', 'minDamage', 1);
        }
    };

    /**
     * 인벤토리 설정
     */
    inventory = {
        get synthesizeCount() {
            return gameConfig.getNumber('inventory', 'synthesizeCount', 5);
        }
    };

    /**
     * 오프라인 설정
     */
    offline = {
        get maxHours() {
            return gameConfig.getNumber('offline', 'maxHours', 24);
        },
        get expPerHour() {
            return gameConfig.getNumber('offline', 'expPerHour', 100);
        },
        get goldPerHour() {
            return gameConfig.getNumber('offline', 'goldPerHour', 50);
        }
    };
}

// 싱글톤 인스턴스
const gameConfig = new GameConfig();

export { GameConfig, gameConfig };
