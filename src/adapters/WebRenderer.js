/**
 * WebRenderer - 웹 (Canvas) 렌더러
 * IRenderer 인터페이스 구현
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameLogger } from '../core/Logger.js';

class GameRenderer {
    constructor(gameState) {
        this.gameState = gameState;
        this.canvas = null;
        this.ctx = null;
        this.width = 0;
        this.height = 0;
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
        
        // 지우기
        this.ctx.clearRect(0, 0, this.width, this.height);
        
        // 배경 그라데이션 (스테이지에 따라 변경)
        this.renderBackground();
        
        // 플레이어 렌더링
        this.renderPlayer();
        
        // 몬스터 렌더링
        this.renderMonster();
        
        // 이펙트 렌더링
        // TODO: Implement effects
    }

    /**
     * 배경 렌더링
     */
    renderBackground() {
        const stage = this.gameState.stage.current;
        const isBoss = stage % 10 === 0;
        
        // 배경 그라데이션
        const gradient = this.ctx.createLinearGradient(0, 0, 0, this.height);
        
        if (isBoss) {
            // 보스 스테이지 - 붉은색
            gradient.addColorStop(0, '#2d1b1b');
            gradient.addColorStop(1, '#1a0f0f');
        } else {
            // 일반 스테이지 - 파란색 계열
            const hue = (stage * 10) % 60;
            gradient.addColorStop(0, `hsl(${240 + hue}, 30%, 20%)`);
            gradient.addColorStop(1, `hsl(${240 + hue}, 30%, 10%)`);
        }
        
        this.ctx.fillStyle = gradient;
        this.ctx.fillRect(0, 0, this.width, this.height);
        
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
        const size = 40;
        
        // 플레이어 (간단한 사각형)
        this.ctx.fillStyle = '#4a9eff';
        this.ctx.fillRect(x - size/2, y - size, size, size);
        
        // 눈
        this.ctx.fillStyle = 'white';
        this.ctx.fillRect(x + 5, y - size + 10, 8, 8);
        
        // HP 바
        const hpPercent = this.gameState.player.currentHp / this.gameState.player.maxHp;
        this.ctx.fillStyle = '#333';
        this.ctx.fillRect(x - size/2, y - size - 15, size, 6);
        this.ctx.fillStyle = '#ef4444';
        this.ctx.fillRect(x - size/2, y - size - 15, size * hpPercent, 6);
    }

    /**
     * 몬스터 렌더링
     */
    renderMonster() {
        const x = this.width * 0.7;
        const y = this.height - 100;
        const size = 45;
        
        // 몬스터 (빨간 사각형)
        this.ctx.fillStyle = '#ef4444';
        this.ctx.fillRect(x - size/2, y - size, size, size);
        
        // 눈
        this.ctx.fillStyle = 'yellow';
        this.ctx.fillRect(x - 15, y - size + 10, 10, 10);
        this.ctx.fillRect(x + 5, y - size + 10, 10, 10);
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
