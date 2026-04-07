/**
 * WebRenderer - 웹 (Canvas) 렌더러
 * IRenderer 인터페이스 구현
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameLogger } from '../core/Logger.js';
import { gameImageLoader } from '../core/ImageLoader.js';

class GameRenderer {
    constructor(gameState) {
        this.gameState = gameState;
        this.canvas = null;
        this.ctx = null;
        this.width = 0;
        this.height = 0;
        
        // 스프라이트 정보
        this.playerSprite = null;
        this.monsterSprite = null;
        this.playerFrameIndex = 0;
        this.monsterFrameIndex = 0;
        this.frameTimer = 0;
        this.frameInterval = 150; // ms per frame
        
        // 이미지 로더 참조 (콘솔에서 접근 가능하도록)
        this.gameImageLoader = gameImageLoader;
    }

    /**
     * 렌더러 초기화
     */
    init() {
        this.canvas = document.getElementById('game-canvas');
        if (!this.canvas) {
            gameLogger.error('Canvas not found');
            return;
        }
        
        this.ctx = this.canvas.getContext('2d');
        this.resize();
        
        // 리사이즈 이벤트
        window.addEventListener('resize', () => this.resize());
        
        gameLogger.debug('GameRenderer initialized');
    }

    /**
     * 캔버스 리사이즈
     */
    resize() {
        const container = this.canvas.parentElement;
        if (!container) return;
        
        const dpr = window.devicePixelRatio || 1;
        const rect = container.getBoundingClientRect();
        
        this.canvas.width = rect.width * dpr;
        this.canvas.height = rect.height * dpr;
        this.canvas.style.width = `${rect.width}px`;
        this.canvas.style.height = `${rect.height}px`;
        
        this.ctx.scale(dpr, dpr);
        this.width = rect.width;
        this.height = rect.height;
    }

    /**
     * 렌더링
     * @param {number} dt - 델타 타임 (ms)
     */
    render(dt) {
        if (!this.ctx) return;
        
        // 애니메이션 업데이트
        this.frameTimer += dt;
        if (this.frameTimer >= this.frameInterval) {
            this.frameTimer = 0;
            this.playerFrameIndex = (this.playerFrameIndex + 1) % 4; // 4 프레임 대기/공격
            this.monsterFrameIndex = (this.monsterFrameIndex + 1) % 4;
        }
        
        // 지우기
        this.ctx.clearRect(0, 0, this.width, this.height);
        
        // 배경 렌더링 (이미지 또는 그라데이션)
        this.renderBackground();
        
        // 플레이어 렌더링 (스프라이트 또는 사각형)
        this.renderPlayer();
        
        // 몬스터 렌더링 (스프라이트 또는 사각형)
        this.renderMonster();
    }

    /**
     * 배경 렌더링
     */
    renderBackground() {
        const stage = this.gameState.stage.current;
        const isBoss = stage % 10 === 0;
        
        // 배경 이미지 시도 (없으면 그라데이션)
        const bgKey = isBoss ? 'background_boss' : 'background_normal';
        const bgImage = gameImageLoader.get(bgKey);
        
        if (bgImage) {
            // 이미지 렌더링 (캔버스에 맞게 스케일)
            const scale = Math.max(this.width / bgImage.width, this.height / bgImage.height);
            const x = (this.width - bgImage.width * scale) / 2;
            const y = (this.height - bgImage.height * scale) / 2;
            this.ctx.drawImage(bgImage, x, y, bgImage.width * scale, bgImage.height * scale);
        } else {
            // 그라데이션 폴백
            const gradient = this.ctx.createLinearGradient(0, 0, 0, this.height);
            
            if (isBoss) {
                gradient.addColorStop(0, '#2d1b1b');
                gradient.addColorStop(1, '#1a0f0f');
            } else {
                const hue = (stage * 10) % 60;
                gradient.addColorStop(0, `hsl(${240 + hue}, 30%, 20%)`);
                gradient.addColorStop(1, `hsl(${240 + hue}, 30%, 10%)`);
            }
            
            this.ctx.fillStyle = gradient;
            this.ctx.fillRect(0, 0, this.width, this.height);
        }
        
        // 바닥
        this.ctx.fillStyle = '#3d3d5c';
        this.ctx.fillRect(0, this.height - 50, this.width, 50);
    }

    /**
     * 플레이어 렌더링
     */
    renderPlayer() {
        const x = this.width * 0.3;
        const y = this.height - 100;
        const spriteSize = 48; // 약간 작게 조정
        
        // 스프라이트 시도
        const sprite = gameImageLoader.get('player');
        if (sprite) {
            const frameWidth = 32;
            const frameHeight = 32;
            const frameX = this.playerFrameIndex % 4;
            const frameY = 0; // 대기/공격 프레임 (0 행)
            
            this.ctx.drawImage(
                sprite,
                frameX * frameWidth, frameY * frameHeight, frameWidth, frameHeight,
                x - spriteSize/2, y - spriteSize, spriteSize, spriteSize
            );
        } else {
            // 폴백: 사각형
            this.ctx.fillStyle = '#4a9eff';
            this.ctx.fillRect(x - spriteSize/2, y - spriteSize, spriteSize, spriteSize);
            gameLogger.debug('Player sprite not loaded, using fallback');
        }
        
        // HP 바
        const hpPercent = this.gameState.player.currentHp / this.gameState.player.maxHp;
        this.ctx.fillStyle = '#333';
        this.ctx.fillRect(x - spriteSize/2, y - spriteSize - 10, spriteSize, 6);
        this.ctx.fillStyle = '#ef4444';
        this.ctx.fillRect(x - spriteSize/2, y - spriteSize - 10, spriteSize * hpPercent, 6);
    }

    /**
     * 몬스터 렌더링
     */
    renderMonster() {
        const x = this.width * 0.7;
        const y = this.height - 100;
        const spriteSize = 48;
        
        // 스프라이트 시도
        const sprite = gameImageLoader.get('monster');
        if (sprite) {
            const frameWidth = 32;
            const frameHeight = 32;
            const frameX = this.monsterFrameIndex % 4;
            const frameY = 0;
            
            this.ctx.drawImage(
                sprite,
                frameX * frameWidth, frameY * frameHeight, frameWidth, frameHeight,
                x - spriteSize/2, y - spriteSize, spriteSize, spriteSize
            );
        } else {
            // 폴백: 사각형
            this.ctx.fillStyle = '#ef4444';
            this.ctx.fillRect(x - spriteSize/2, y - spriteSize, spriteSize, spriteSize);
            gameLogger.debug('Monster sprite not loaded, using fallback');
        }
        
        // 몬스터 HP 바
        const monster = this.combatSystem?.currentMonster;
        if (monster && monster.maxHp > 0) {
            const hpPercent = monster.currentHp / monster.maxHp;
            this.ctx.fillStyle = '#333';
            this.ctx.fillRect(x - spriteSize/2, y - spriteSize - 10, spriteSize, 6);
            this.ctx.fillStyle = '#ef4444';
            this.ctx.fillRect(x - spriteSize/2, y - spriteSize - 10, spriteSize * hpPercent, 6);
        }
    }

    /**
     * 데미지 표시
     * @param {number} damage 
     * @param {Object} position 
     */
    showDamage(damage, position) {
        // TODO: Implement floating damage text
        gameLogger.debug(`Damage: ${damage}`);
    }

    /**
     * 레벨업 이펙트
     */
    showLevelUp() {
        // TODO: Implement level up effect
        gameLogger.debug('Level Up!');
    }

    /**
     * 정리
     */
    destroy() {
        this.canvas = null;
        this.ctx = null;
    }
}

export { GameRenderer };
