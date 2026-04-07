/**
 * Logger - 게임 로깅 시스템
 * 개발/배포 모드 분리, 오류 수집 준비
 */

const LOG_LEVELS = {
    DEBUG: 0,
    INFO: 1,
    WARN: 2,
    ERROR: 3
};

class Logger {
    constructor() {
        // 환경에 따라 자동 감지 (나중에 설정에서 조절 가능)
        this.isDevelopment = window.location.hostname === 'localhost' || 
                            window.location.hostname === '127.0.0.1' ||
                            window.DEBUG === true;
        this.minLevel = this.isDevelopment ? LOG_LEVELS.DEBUG : LOG_LEVELS.INFO;
        this.errorCollectionEnabled = false;
    }

    /**
     * 로그 레벨 설정
     * @param {number} level 
     */
    setLevel(level) {
        this.minLevel = level;
    }

    /**
     * 디버그 로그 (개발 모드만)
     */
    debug(msg, ...args) {
        if (this.minLevel <= LOG_LEVELS.DEBUG) {
            console.log(`[DEBUG] ${msg}`, ...args);
        }
    }

    /**
     * 정보 로그
     */
    info(msg, ...args) {
        if (this.minLevel <= LOG_LEVELS.INFO) {
            console.info(`[INFO] ${msg}`, ...args);
        }
    }

    /**
     * 경고 로그
     */
    warn(msg, ...args) {
        if (this.minLevel <= LOG_LEVELS.WARN) {
            console.warn(`[WARN] ${msg}`, ...args);
        }
    }

    /**
     * 오류 로그
     */
    error(msg, ...args) {
        if (this.minLevel <= LOG_LEVELS.ERROR) {
            console.error(`[ERROR] ${msg}`, ...args);
        }
        
        if (this.errorCollectionEnabled) {
            this.reportError(msg, args);
        }
    }

    /**
     * 오류 보고 (추후 Crashlytics 등 연동)
     * @param {string} msg 
     * @param {Array} args 
     */
    reportError(msg, args) {
        // TODO: Crashlytics, Sentry 등 오류 수집 서비스 연동
        console.error('[ErrorReport] Would send to error service:', msg, args);
    }

    /**
     * 오류 수집 활성화
     */
    enableErrorCollection() {
        this.errorCollectionEnabled = true;
    }

    /**
     * 전투 로그 (게임 내 표시용)
     * @param {string} message 
     */
    combat(message) {
        gameEventBus.emit(GAME_EVENTS.COMBAT_LOG, { message });
    }
}

// 싱글톤 인스턴스
const gameLogger = new Logger();

export { Logger, gameLogger, LOG_LEVELS };
