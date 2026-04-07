/**
 * ImageLoader - 이미지 리소스 로드 및 관리
 * 스프라이트 시트 로딩, 프레임 분할 기능 제공
 */
import { gameLogger } from '../core/Logger.js';

class ImageLoader {
    constructor() {
        this.images = new Map();
        this.loadedCount = 0;
        this.totalCount = 0;
    }

    /**
     * 이미지 로드 (프레이미스)
     * @param {string} key - 이미지에 대한 식별자
     * @param {string} src - 이미지 경로
     * @returns {Promise<HTMLImageElement>}
     */
    async load(key, src) {
        return new Promise((resolve, reject) => {
            const img = new Image();
            
            img.onload = () => {
                this.images.set(key, img);
                this.loadedCount++;
                gameLogger.debug(`Image loaded: ${key} (${img.width}x${img.height})`);
                resolve(img);
            };
            
            img.onerror = () => {
                gameLogger.error(`Failed to load image: ${key}`);
                reject(new Error(`Failed to load ${src}`));
            };
            
            img.src = src;
        });
    }

    /**
     * 여러 이미지 병렬 로드
     * @param {Object} imageMap - { key: src } 객체
     * @returns {Promise<Map>}
     */
    async loadAll(imageMap) {
        this.totalCount = Object.keys(imageMap).length;
        this.loadedCount = 0;
        
        const entries = Object.entries(imageMap);
        const results = await Promise.allSettled(
            entries.map(([key, src]) => this.load(key, src))
        );
        
        // 실패한 이미지 로깅
        results.forEach((result, index) => {
            if (result.status === 'rejected') {
                gameLogger.error(`Failed: ${entries[index][0]}`);
            }
        });
        
        return this.images;
    }

    /**
     * 이미지 가져오기
     * @param {string} key 
     * @returns {HTMLImageElement|null}
     */
    get(key) {
        return this.images.get(key) || null;
    }

    /**
     * 스프라이트 시트에서 프레임 추출
     * @param {string} key - 스프라이트 시트 키
     * @param {number} frameX - 프레임 X 위치 (0부터)
     * @param {number} frameY - 프레임 Y 위치 (0부터)
     * @param {number} frameWidth - 프레임 너비
     * @param {number} frameHeight - 프레임 높이
     * @returns {Object} 캔버스에 렌더링할 수 있는 프레임 정보
     */
    getSpriteFrame(key, frameX, frameY, frameWidth, frameHeight) {
        const image = this.images.get(key);
        if (!image) return null;
        
        return {
            image,
            sx: frameX * frameWidth,
            sy: frameY * frameHeight,
            sWidth: frameWidth,
            sHeight: frameHeight
        };
    }

    /**
     * 스프라이트 시트에서 모든 프레임 추출
     * @param {string} key - 스프라이트 시트 키
     * @param {number} frameWidth - 프레임 너비
     * @param {number} frameHeight - 프레임 높이
     * @param {number} columns - 가로 프레임 수
     * @param {number} rows - 세로 프레임 수
     * @returns {Array} 프레임 배열
     */
    getAllFrames(key, frameWidth, frameHeight, columns, rows) {
        const image = this.images.get(key);
        if (!image) return [];
        
        const frames = [];
        for (let y = 0; y < rows; y++) {
            for (let x = 0; x < columns; x++) {
                frames.push({
                    image,
                    sx: x * frameWidth,
                    sy: y * frameHeight,
                    sWidth: frameWidth,
                    sHeight: frameHeight
                });
            }
        }
        return frames;
    }

    /**
     * 진행률 (로딩 화면용)
     * @returns {number} 0-100
     */
    getProgress() {
        if (this.totalCount === 0) return 100;
        return Math.floor((this.loadedCount / this.totalCount) * 100);
    }

    /**
     * 캐시 비우기
     */
    clear() {
        this.images.clear();
        this.loadedCount = 0;
        this.totalCount = 0;
    }
}

// 싱글톤 인스턴스
const gameImageLoader = new ImageLoader();

export { ImageLoader, gameImageLoader };
