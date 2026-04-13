/**
 * WebRenderer - 웹 (Canvas) 렌더러
 * 상태별 애니메이션 지원
 */
import { gameEventBus, GAME_EVENTS } from '../core/EventBus.js';
import { gameLogger } from '../core/Logger.js';
import { gameImageLoader } from '../core/ImageLoader.js';

class GameRenderer {
    constructor(gameState, combatSystem) {
        this.gameState = gameState;
        this.combatSystem = combatSystem;
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
        
        // 애니메이션 상태
        this.playerAnimState = 'idle'; // 'idle', 'moving', 'attacking', 'hit'
        this.monsterAnimState = 'idle'; // 'idle', 'hit', 'dead'
        
        // 프레임 매핑 (상태별 사용 프레임)
        // Player: 0-1 Idle, 2-3 Attack, 4-5 Hit(미사용), 6-7 Dead
        this.playerFrames = {
            idle: [0, 1],        // 2프레임 Idle
            attacking: [2, 3],   // 2프레임 Attack
            dead: [6, 7]         // 2프레임 Dead (쓰러짐)
        };
        
        // Monster: 0(등장전), 4/6(Idle), 5(돌진), 2→3(Dead), 1/7(미사용)
        this.monsterFrames = {
            appearing: [0],      // 1프레임 - 등장 전 (이동 페이즈)
            charging: [5],       // 1프레임 - 돌진 (조우 페이즈)
            idle: [4, 6],        // 2프레임 Idle (전투 중)
            dead: [2, 3]         // 2프레임 Dead (2→3 순서)
        };
        
        // 플레이어 이동 위치 (보간용)
        this.playerX = 0;
        this.playerTargetX = 0;
        this.monsterX = 0;
        this.monsterTargetX = 0;
        this.bgOffsetX = 0;
        
        this.lastPhase = null;
        
        // 데미지 텍스트 이펙트
        this.damageTexts = [];
        
        // 이미지 로더 참조
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
        
        // 초기 위치 설정
        this.playerX = this.width * 0.2;
        this.playerTargetX = this.width * 0.35;
        this.monsterX = this.width * 0.7;
        this.monsterTargetX = this.width * 0.65;
        
        // 배경 최대 스크롤량 (플레이어 이동 거리의 50%)
        this.maxBgScroll = (this.playerTargetX - this.width * 0.2) * 0.5;
        
        // 배경 너비 (무한 스크롤용) - resize()에서 실제 값으로 업데이트됨
        this.bgWidth = this.width;
        
        // 리사이즈 이벤트
        window.addEventListener('resize', () => this.resize());
        
        // 데미지 텍스트 이벤트 리스너
        gameEventBus.on(GAME_EVENTS.COMBAT_DAMAGE, (data) => {
            if (data.target === 'monster') {
                this.showDamageText(data.damage, data.isCrit);
            }
        });
        
        gameLogger.debug('GameRenderer initialized');
    }

    /**
     * 데미지 텍스트 표시 (몬스터 위에 랜덤 위치)
     */
    showDamageText(damage, isCrit) {
        // 몬스터의 화면상 위치 계산
        const monsterScreenX = this.monsterX;
        const monsterScreenY = this.height - 80; // 몬스터 발 위치
        
        // 랜덤 오프셋 (-50 ~ +50)
        const randomOffset = (Math.random() - 0.5) * 100;
        
        // 데미지 텍스트 생성
        this.damageTexts.push({
            x: monsterScreenX + randomOffset,
            y: monsterScreenY - 40, // 몬스터 머리 위
            text: damage.toString(),
            isCrit: isCrit,
            createdAt: performance.now(),
            duration: 1000, // 1초 동안 표시
            startY: monsterScreenY - 40
        });
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
        
        // Transform 초기화 후 DPR 스케일 적용
        this.ctx.resetTransform();
        this.ctx.scale(dpr, dpr);
        this.width = rect.width;
        this.height = rect.height;
        
        // 위치 재계산
        this.playerTargetX = this.width * 0.35;
        this.monsterTargetX = this.width * 0.65;
        
        // 배경 최대 스크롤량 재계산
        this.maxBgScroll = (this.playerTargetX - this.width * 0.2) * 0.5;
        
        // 배경 너비 업데이트 (캔버스 기준, 실제 이미지는 renderBackground에서 재계산)
        this.bgWidth = this.width;
    }

    /**
     * 렌더링
     * @param {number} dt - 델타 타임 (ms)
     */
    render(dt) {
        if (!this.ctx || this.width === 0) return;
        
        // 애니메이션 상태 업데이트 (GameState에서 읽기)
        this.updateAnimStates();
        
        // 애니메이션 프레임 업데이트
        this.frameTimer += dt;
        if (this.frameTimer >= this.frameInterval) {
            this.frameTimer = 0;
            this.updateFrameIndices();
        }
        
        // 플레이어 위치 보간 (이동 애니메이션)
        this.updatePositions(dt);
        
        // 지우기
        this.ctx.clearRect(0, 0, this.width, this.height);
        
        // 배경 렌더링
        this.renderBackground();
        
        // 플레이어 렌더링
        this.renderPlayer();
        
        // 몬스터 렌더링
        this.renderMonster();
        
        // 데미지 텍스트 렌더링
        this.renderDamageTexts(dt);
    }

    /**
     * 데미지 텍스트 렌더링
     */
    renderDamageTexts(dt) {
        const now = performance.now();
        
        // 만료된 텍스트 제거
        this.damageTexts = this.damageTexts.filter(text => {
            return now - text.createdAt < text.duration;
        });
        
        // 텍스트 렌더링
        this.damageTexts.forEach(text => {
            const elapsed = now - text.createdAt;
            const progress = elapsed / text.duration;
            
            // 위로 이동 (부드러운 감속)
            const riseDistance = 60 * (1 - Math.pow(1 - progress, 2));
            const currentY = text.startY - riseDistance;
            
            // 투명도 (점점 희미해짐)
            const alpha = 1 - progress;
            
            // 폰트 설정
            const fontSize = text.isCrit ? 28 : 20;
            const fontWeight = text.isCrit ? 'bold' : 'normal';
            this.ctx.font = `${fontWeight} ${fontSize}px Arial`;
            this.ctx.textAlign = 'center';
            
            // 색상 (크리티컬이면 빨강, 아니면 흰색)
            const color = text.isCrit ? '#ff4444' : '#ffffff';
            
            // 그림자
            this.ctx.fillStyle = 'rgba(0,0,0,0.5)';
            this.ctx.fillText(text.text, text.x + 2, currentY + 2);
            
            // 본체
            this.ctx.globalAlpha = alpha;
            this.ctx.fillStyle = color;
            this.ctx.fillText(text.text, text.x, currentY);
            this.ctx.globalAlpha = 1.0;
        });
    }

    /**
     * 애니메이션 상태 업데이트 (GameState에서 동기화)
     */
    updateAnimStates() {
        const cp = this.gameState.combatPhase;
        
        // 플레이어 상태
        this.playerAnimState = cp.playerState;
        
        // 몬스터 상태
        this.monsterAnimState = cp.monsterState;
    }

    /**
     * 프레임 인덱스 업데이트
     */
    updateFrameIndices() {
        // 플레이어 프레임
        const playerFrameSet = this.playerFrames[this.playerAnimState] || this.playerFrames.idle;
        
        // Dead 애니메이션은 한 번만 (6→7 그리고 멈춤)
        if (this.playerAnimState === 'dead') {
            if (this.playerFrameIndex < playerFrameSet.length - 1) {
                this.playerFrameIndex++;
            }
            // 마지막 프레임에서 멈춤
        } else {
            // 다른 상태는 반복
            this.playerFrameIndex = (this.playerFrameIndex + 1) % (playerFrameSet.length * 8); // 8바퀴
        }
        
        // 몬스터 프레임
        const monsterFrameSet = this.monsterFrames[this.monsterAnimState] || this.monsterFrames.idle;
        
        // 몬스터 Dead도 한 번만
        if (this.monsterAnimState === 'dead') {
            if (this.monsterFrameIndex < monsterFrameSet.length - 1) {
                this.monsterFrameIndex++;
            }
        } else {
            this.monsterFrameIndex = (this.monsterFrameIndex + 1) % (monsterFrameSet.length * 8);
        }
    }

    /**
     * 현재 플레이어 프레임 키 얻기
     */
    getPlayerFrameKey() {
        const frameSet = this.playerFrames[this.playerAnimState] || this.playerFrames.idle;
        
        // Dead 애니메이션: 마지막 프레임에서 멈춤
        if (this.playerAnimState === 'dead') {
            const deadIndex = Math.min(this.playerFrameIndex, frameSet.length - 1);
            return `player_${frameSet[deadIndex]}`;
        }
        
        // Attack 애니메이션: GameState의 attackCurrentFrame을 사용 (0, 1, 2)
        if (this.playerAnimState === 'attacking') {
            const currentFrame = this.gameState.combatPhase.attackCurrentFrame || 0;
            return `player_${frameSet[currentFrame]}`;
        }
        
        // 기타 상태 (idle 등)
        // MOVING/VICTORY 페이즈에서는 2배 빠르게 (4프레임마다 변경)
        const phase = this.gameState.combatPhase.phase;
        const frameSkip = (phase === 'MOVING' || phase === 'VICTORY') ? 4 : 8;
        const index = (Math.floor(this.playerFrameIndex / frameSkip) % frameSet.length);
        return `player_${frameSet[index]}`;
    }

    /**
     * 현재 몬스터 프레임 키 얻기
     */
    getMonsterFrameKey() {
        const frameSet = this.monsterFrames[this.monsterAnimState] || this.monsterFrames.idle;
        
        // appearing/charging/dead는 단일/고정 프레임
        if (this.monsterAnimState === 'appearing') {
            return 'monster_0'; // 등장 전 - 항상 0번
        }
        if (this.monsterAnimState === 'charging') {
            return 'monster_5'; // 돌진 - 항상 5번
        }
        if (this.monsterAnimState === 'dead') {
            // Dead 애니메이션: 2→3 순서, 3번에서 멈춤
            const deadProgress = Math.floor(this.monsterFrameIndex / 4) % frameSet.length;
            return `monster_${frameSet[deadProgress]}`;
        }
        
        // idle은 반복 애니메이션
        const index = (Math.floor(this.monsterFrameIndex / 4) % frameSet.length);
        return `monster_${frameSet[index]}`;
    }

    /**
     * 플레이어 위치 업데이트 (보간)
     */
    updatePositions(dt) {
        const phase = this.gameState.combatPhase.phase;
        const moveProgress = this.gameState.combatPhase.moveProgress;
        const monsterAppearProgress = 0.5; // 몬스터 등장 시작 지점 (이동 50%)
        
        // 배경 스크롤 - MOVING과 VICTORY에서만 왼쪽으로 이동 (COMBAT에서는 정지)
        // 고정 속도: 5 픽셀/초 (이동속도와 무관)
        if (phase !== 'COMBAT') {
            const bgScrollSpeed = 50 / 60; // 픽셀/프레임 (60fps 기준)
            this.bgOffsetX -= bgScrollSpeed * (dt / 16);
        }
        
        this.lastPhase = phase;
        
        if (phase === 'MOVING') {
            // 플레이어는 제자리, 애니메이션만 재생
            this.playerX = this.playerTargetX;
            
            // 몬스터 이동 - 오른쪽에서 등장 (50% 지점부터)
            if (moveProgress >= monsterAppearProgress) {
                const monsterMoveProgress = (moveProgress - monsterAppearProgress) / (1 - monsterAppearProgress);
                const easedMonsterProgress = this.easeInOutCubic(monsterMoveProgress);
                this.monsterX = this.width + (this.monsterTargetX - this.width) * easedMonsterProgress;
            } else {
                // 등장 전 - 화면 밖에
                this.monsterX = this.width + 50;
            }
        } else if (phase === 'VICTORY') {
            // 승리 후 플레이어는 제자리, 걷는 모션만 유지
            this.playerX = this.playerTargetX;
            // 몬스터는 제자리 (죽은 상태)
            this.monsterX = this.monsterTargetX;
        } else {
            // 전투 중 - 플레이어와 몬스터는 제자리
            this.playerX = this.playerTargetX;
            this.monsterX = this.monsterTargetX;
        }
    }

    /**
     * Easing 함수 (EaseInOutCubic)
     */
    easeInOutCubic(t) {
        return t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2;
    }

    /**
     * 배경 렌더링
     * 2개의 배경 이미지를 교차 배치하여 무한 스크롤 효과
     * - 이동 시: 배경2가 왼쪽에서 오른쪽으로 스크롤되며 화면을 덮음
     * - 덮는 동안 배경1을 배경2 오른쪽으로 재배치하여 반복
     */
    renderBackground() {
        const stage = this.gameState.stage.current;
        const isBoss = stage % 10 === 0;
        
        // 배경 이미지 시도 (없으면 그라데이션)
        const bgKey = isBoss ? 'background_boss' : 'background_normal';
        const bgImage = gameImageLoader.get(bgKey);
        
        if (bgImage) {
            // 이미지 렌더링 (캔버스 너비에 맞춤, 높이는 비율 유지)
            const scale = this.width / bgImage.width;
            const bgWidth = bgImage.width * scale;
            const bgHeight = bgImage.height * scale;
            const y = (this.height - bgHeight) / 2;
            
            // bgOffsetX는 0에서 -maxBgScroll까지 변화 (왼쪽으로 이동)
            // 무한 스크롤: normalizedScroll은 0~bgWidth 범위에서 순환
            const scrollOffset = this.bgOffsetX;
            const normalizedScroll = ((-scrollOffset % bgWidth) + bgWidth) % bgWidth;
            
            // 배경1: 오른쪽에서 왼쪽으로 스크롤 (0 → -bgWidth)
            const x1 = -normalizedScroll;
            // 배경2: 배경1의 오른쪽에서 따라옴 (bgWidth → 0)
            const x2 = bgWidth - normalizedScroll;
            
            // 두 배경 이미지 렌더링 (교차하며 무한 스크롤)
            this.ctx.drawImage(bgImage, x1, y, bgWidth, bgHeight);
            this.ctx.drawImage(bgImage, x2, y, bgWidth, bgHeight);
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
        const x = this.playerX;
        const y = this.height - 80;
        
        // 현재 상태의 프레임 키 얻기
        let frameKey = this.getPlayerFrameKey();
        let sprite = gameImageLoader.get(frameKey);
        
        // attacking인데 sprite가 없으면 idle 프레임으로 폴백
        if (!sprite && this.playerAnimState === 'attacking') {
            frameKey = 'player_' + (this.gameState.combatPhase.attackCurrentFrame || 0);
            sprite = gameImageLoader.get(frameKey);
        }
        
        // HP 바 기준 (player_0 프레임 크기)
        const baseSprite = gameImageLoader.get('player_0');
        const baseScale = 0.7;
        const baseWidth = baseSprite ? baseSprite.width * baseScale : 50;
        const baseHeight = baseSprite ? baseSprite.height * baseScale : 70;
        
        if (sprite) {
            // 현재 프레임 렌더링
            const scale = 0.7;
            const displayWidth = sprite.width * scale;
            const displayHeight = sprite.height * scale;
            
            // 항상 globalAlpha 초기화
            this.ctx.globalAlpha = 1.0;
            
            // 피격 시 깜빡임 효과
            if (this.playerAnimState === 'hit') {
                const flashPhase = Math.floor(performance.now() / 50) % 2;
                if (flashPhase === 0) {
                    this.ctx.globalAlpha = 0.5;
                }
            }
            
            // 공격 시 전방 돌진 효과
            if (this.playerAnimState === 'attacking') {
                const attackOffset = Math.sin(performance.now() / 50) * 5;
                this.ctx.drawImage(
                    sprite,
                    x - displayWidth/2 + attackOffset, y - displayHeight, displayWidth, displayHeight
                );
            } else {
                this.ctx.drawImage(
                    sprite,
                    x - displayWidth/2, y - displayHeight, displayWidth, displayHeight
                );
            }
        }
        
        // HP 바
        const hpBarWidth = 60;
        const hpBarX = x - hpBarWidth / 2;
        const hpBarY = y - baseHeight - 12;
        const hpPercent = this.gameState.player.currentHp / this.gameState.player.derivedStats.maxHp;
        
        this.ctx.fillStyle = '#333';
        this.ctx.fillRect(hpBarX, hpBarY, hpBarWidth, 8);
        this.ctx.fillStyle = '#ef4444';
        this.ctx.fillRect(hpBarX, hpBarY, hpBarWidth * hpPercent, 8);
    }

    /**
     * 몬스터 렌더링
     */
    renderMonster() {
        const x = this.monsterX;
        const y = this.height - 80;
        
        // 현재 상태의 프레임 키 얻기
        const frameKey = this.getMonsterFrameKey();
        const sprite = gameImageLoader.get(frameKey);
        
        // HP 바 기준 (monster_0 프레임 크기)
        const baseSprite = gameImageLoader.get('monster_0');
        const baseScale = 0.5;
        const baseWidth = baseSprite ? baseSprite.width * baseScale : 50;
        const baseHeight = baseSprite ? baseSprite.height * baseScale : 70;
        
        if (sprite) {
            // 현재 프레임 렌더링 (왼쪽 바라봄)
            const scale = 0.5;
            const displayWidth = sprite.width * scale;
            const displayHeight = sprite.height * scale;
            
            // 피격 시 깜빡임 효과
            if (this.monsterAnimState === 'hit') {
                const flashPhase = Math.floor(performance.now() / 50) % 2;
                if (flashPhase === 0) {
                    this.ctx.globalAlpha = 0.5;
                }
            }
            
            // 죽음 애니메이션 (서서히 사라짐)
            if (this.monsterAnimState === 'dead') {
                const deathProgress = Math.min(1, this.gameState.combatPhase.victoryTimer / 500);
                this.ctx.globalAlpha = 1 - deathProgress;
                
                // 위로 떠오르는 효과
                const floatY = y - deathProgress * 30;
                
                this.ctx.save();
                this.ctx.translate(x, floatY - displayHeight);
                this.ctx.scale(-1, 1);
                this.ctx.drawImage(sprite, 0, 0, displayWidth, displayHeight);
                this.ctx.restore();
            } else {
                // 좌우 반전 렌더링
                this.ctx.save();
                this.ctx.translate(x, y - displayHeight);
                this.ctx.scale(-1, 1);
                this.ctx.drawImage(sprite, 0, 0, displayWidth, displayHeight);
                this.ctx.restore();
            }
            
            this.ctx.globalAlpha = 1.0;
        } else {
            // 스프라이트 없으면 사각형으로 대체
            this.ctx.fillStyle = '#d94a4a';
            this.ctx.fillRect(x - 25, y - 70, 50, 70);
        }
        
        // HP 바
        const hpBarWidth = 60;
        const hpBarX = (x - baseWidth / 2) - hpBarWidth / 2;
        const hpBarY = y - baseHeight - 12;
        const monster = this.combatSystem?.currentMonster;
        
        if (monster && monster.maxHp > 0) {
            const hpPercent = monster.currentHp / monster.maxHp;
            this.ctx.fillStyle = '#333';
            this.ctx.fillRect(hpBarX, hpBarY, hpBarWidth, 8);
            this.ctx.fillStyle = '#ef4444';
            this.ctx.fillRect(hpBarX, hpBarY, hpBarWidth * hpPercent, 8);
            
            // 몬스터 이름 표시
            this.ctx.fillStyle = '#fff';
            this.ctx.font = '12px Arial';
            this.ctx.textAlign = 'center';
            this.ctx.fillText(monster.name, x, hpBarY - 5);
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
