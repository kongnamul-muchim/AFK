/**
 * DataLoader - CSV 데이터 로드 및 관리
 * 병렬 로드, 캐싱, 검증 기능 제공
 */
import { CSVParser } from './CSVParser.js';
import { gameLogger } from '../core/Logger.js';
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';

class DataLoader {
    constructor() {
        this.cache = new Map();
        this.dataPath = 'data/';
        this.requiredFiles = [
            'game_config',
            'items',
            'monsters',
            'stages'
        ];
        this.optionalFiles = [
            'achievements',
            'tutorial',
            'audio_definitions'
        ];
    }

    /**
     * 모든 데이터 로드
     * @returns {Promise<boolean>}
     */
    async loadAll() {
        gameLogger.info('Loading all data files...');
        
        const files = [...this.requiredFiles, ...this.optionalFiles];
        const results = await Promise.allSettled(
            files.map(name => this.load(name))
        );
        
        // 필수 파일 확인
        const failedRequired = [];
        results.forEach((result, index) => {
            const fileName = files[index];
            if (result.status === 'rejected') {
                if (this.requiredFiles.includes(fileName)) {
                    failedRequired.push(fileName);
                }
                gameLogger.error(`Failed to load ${fileName}:`, result.reason);
            } else {
                gameLogger.debug(`Loaded ${fileName}`);
            }
        });
        
        if (failedRequired.length > 0) {
            gameLogger.error('Missing required files:', failedRequired);
            throw new Error(`Missing required data files: ${failedRequired.join(', ')}`);
        }
        
        gameLogger.info('All data loaded successfully');
        return true;
    }

    /**
     *单个 파일 로드
     * @param {string} name - 파일 이름 (확장자 제외)
     * @returns {Promise<Object[]>}
     */
    async load(name) {
        // 캐시 확인
        if (this.cache.has(name)) {
            return this.cache.get(name);
        }
        
        const url = `${this.dataPath}${name}.csv`;
        const data = await CSVParser.parseFile(url);
        
        // 검증
        this.validate(name, data);
        
        // 캐싱
        this.cache.set(name, data);
        
        return data;
    }

    /**
     * 데이터 검증
     * @param {string} name 
     * @param {Object[]} data 
     */
    validate(name, data) {
        if (!Array.isArray(data)) {
            throw new Error(`${name}: Data must be an array`);
        }
        
        if (data.length === 0) {
            gameLogger.warn(`${name}: Empty data file`);
            return;
        }
        
        // ID 중복 검사
        const idFields = this.getIdField(name);
        if (idFields) {
            const ids = new Set();
            const duplicates = [];
            
            data.forEach((row, index) => {
                const id = row[idFields];
                if (ids.has(id)) {
                    duplicates.push({ id, index });
                }
                ids.add(id);
            });
            
            if (duplicates.length > 0) {
                gameLogger.warn(`${name}: Duplicate IDs found:`, duplicates);
            }
        }
    }

    /**
     * ID 필드명 반환
     * @param {string} name 
     * @returns {string|null}
     */
    getIdField(name) {
        const idMap = {
            'items': 'id',
            'monsters': 'id',
            'stages': 'stageNumber',
            'achievements': 'id',
            'tutorial': 'step',
            'audio_definitions': 'sound_id',
            'game_config': null // 복합 키 (category + key)
        };
        return idMap[name] || 'id';
    }

    /**
     * 데이터 조회
     * @param {string} name 
     * @returns {Object[]|null}
     */
    get(name) {
        return this.cache.get(name) || null;
    }

    /**
     * ID 로 항목 조회
     * @param {string} name 
     * @param {string|number} id 
     * @returns {Object|null}
     */
    getById(name, id) {
        const data = this.get(name);
        if (!data) return null;
        
        const idField = this.getIdField(name);
        return data.find(row => row[idField] == id) || null;
    }

    /**
     * 필터링
     * @param {string} name 
     * @param {Function} predicate 
     * @returns {Object[]}
     */
    filter(name, predicate) {
        const data = this.get(name);
        if (!data) return [];
        return data.filter(predicate);
    }

    /**
     * 설정 값 조회
     * @param {string} category 
     * @param {string} key 
     * @returns {*}
     */
    getConfig(category, key) {
        const config = this.get('game_config');
        if (!config) return null;
        
        const row = config.find(c => c.category === category && c.key === key);
        return row ? row.value : null;
    }

    /**
     * 설정 값 (숫자) 조회
     * @param {string} category 
     * @param {string} key 
     * @param {number} defaultValue 
     * @returns {number}
     */
    getConfigNumber(category, key, defaultValue = 0) {
        const value = this.getConfig(category, key);
        return value !== null ? Number(value) : defaultValue;
    }

    /**
     * 캐시 무효화
     * @param {string} name - 특정 파일 또는 null (전체)
     */
    invalidate(name = null) {
        if (name) {
            this.cache.delete(name);
            gameLogger.debug(`Invalidated cache for ${name}`);
        } else {
            this.cache.clear();
            gameLogger.debug('Invalidated all cache');
        }
    }

    /**
     * 로드 진행률 (로딩 화면용)
     * @returns {number} 0-100
     */
    getProgress() {
        const total = this.requiredFiles.length + this.optionalFiles.length;
        const loaded = this.cache.size;
        return Math.floor((loaded / total) * 100);
    }
}

// 싱글톤 인스턴스
const gameDataLoader = new DataLoader();

export { DataLoader, gameDataLoader };
