/**
 * LoadingScreen - 로딩 화면 관리
 */
import { gameLogger } from '../core/Logger.js';

class LoadingScreen {
    constructor() {
        this.screen = document.getElementById('loading-screen');
        this.barFill = document.getElementById('loading-bar-fill');
        this.percentText = document.getElementById('loading-percent');
        this.tipText = document.getElementById('loading-tip');
        this.retryBtn = document.getElementById('loading-retry');
        
        this.tips = [
            '스탯포인트는 전략적으로 분배하세요!',
            '장비 5 개를 모으면 합성할 수 있습니다!',
            '오프라인 보상은 최대 24 시간까지!',
            '10 층마다 보스가 등장합니다!',
            '자동으로 전투가 진행됩니다!',
            '골드로 더 좋은 장비를 구입하세요!',
            '크리티컬 확률은 민첩으로 올라갑니다!',
            '방어력은 받는 데미지를 줄여줍니다!'
        ];
    }

    /**
     * 로딩 화면 표시
     */
    show() {
        if (this.screen) {
            this.screen.style.display = 'flex';
            this.updateProgress(0, '시작 중...');
            this.setRandomTip();
        }
    }

    /**
     * 로딩 화면 숨김
     */
    hide() {
        if (this.screen) {
            this.screen.style.opacity = '0';
            this.screen.style.transition = 'opacity 0.5s ease';
            setTimeout(() => {
                this.screen.style.display = 'none';
                this.screen.style.opacity = '1';
            }, 500);
        }
    }

    /**
     * 진행률 업데이트
     * @param {number} percent 
     * @param {string} tip 
     */
    updateProgress(percent, tip) {
        if (this.barFill) {
            this.barFill.style.width = `${percent}%`;
        }
        if (this.percentText) {
            this.percentText.textContent = `${Math.floor(percent)}%`;
        }
        if (this.tipText && tip) {
            this.tipText.textContent = tip;
        }
    }

    /**
     * 랜덤 팁 표시
     */
    setRandomTip() {
        const tip = this.tips[Math.floor(Math.random() * this.tips.length)];
        if (this.tipText) {
            this.tipText.textContent = tip;
        }
    }

    /**
     * 에러 표시
     * @param {string} message 
     */
    showError(message) {
        if (this.tipText) {
            this.tipText.textContent = `오류: ${message}`;
            this.tipText.style.color = '#f87171';
        }
        if (this.retryBtn) {
            this.retryBtn.style.display = 'inline-block';
            this.retryBtn.onclick = () => location.reload();
        }
        gameLogger.error('Loading error:', message);
    }
}

export { LoadingScreen };
