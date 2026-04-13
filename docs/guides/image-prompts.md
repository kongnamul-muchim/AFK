# 이미지 생성 프롬프트

Google FX Labs Flow 에서 사용할 스프라이트/배경 이미지 프롬프트입니다.

---

## 📁 저장 경로

생성된 이미지는 다음 경로로 저장하세요:

```
assets/images/
├── characters/
│   └── player_spritesheet.png
├── monsters/
│   └── slime_spritesheet.png
├── backgrounds/
│   ├── background_normal.png
│   └── background_boss.png
└── effects/
    └── levelup_effect.png (선택)
```

---

## 1. 플레이어 스프라이트 시트

**파일명:** `player_spritesheet.png`  
**크기:** 128x64 픽셀 (32x32 × 8 프레임)  
**저장 경로:** `assets/images/characters/`

### 프롬프트 (영어)
```
Pixel art sprite sheet for RPG game character, 8 frames arranged in 2 rows of 4, 
each frame 32x32 pixels, total size 128x64 pixels.

Animations:
- Row 1: Idle breathing (2 frames), Attack with sword (2 frames)
- Row 2: Hit/damage flash (2 frames), Death/fall (2 frames)

Character: Simple knight or warrior, fantasy style, side view facing right
Style: Clean pixel art, limited color palette (8-16 colors), white background
Game asset for mobile idle RPG
```

### 프레임 구성
```
┌────────────────────────────────────────────┐
│ 대기 1 │ 대기 2 │ 공격 1 │ 공격 2 │  (Row 0)
│ 피격 1 │ 피격 2 │ 죽음 1 │ 죽음 2 │  (Row 1)
└────────────────────────────────────────────┘
```

---

## 2. 몬스터 스프라이트 시트 (슬라임)

**파일명:** `slime_spritesheet.png`  
**크기:** 128x64 픽셀 (32x32 × 8 프레임)  
**저장 경로:** `assets/images/monsters/`

### 프롬프트 (영어)
```
Pixel art sprite sheet for RPG monster slime, 8 frames arranged in 2 rows of 4, 
each frame 32x32 pixels, total size 128x64 pixels.

Animations:
- Row 1: Idle bouncing (2 frames), Attack lunge (2 frames)
- Row 2: Hit/damage flash (2 frames), Death/disappear (2 frames)

Monster: Blue or green slime, simple fantasy creature, side view facing left
Style: Clean pixel art, limited color palette (8-16 colors), white background
Game asset for mobile idle RPG
```

### 프레임 구성
```
┌────────────────────────────────────────────┐
│ 대기 1 │ 대기 2 │ 공격 1 │ 공격 2 │  (Row 0)
│ 피격 1 │ 피격 2 │ 죽음 1 │ 죽음 2 │  (Row 1)
└────────────────────────────────────────────┘
```

---

## 3. 일반 층 배경 (복도)

**파일명:** `background_normal.png`  
**크기:** 400x300 픽셀 (또는 200x150 으로 작게 만들어 반복 사용)  
**저장 경로:** `assets/images/backgrounds/`

### 프롬프트 (영어)
```
Pixel art dungeon corridor background, seamless tileable pattern,
size 400x300 pixels, side view scrolling background.

Elements: Stone brick walls on left and right, wooden floor at bottom,
torches mounted on walls with warm orange glow, dark corridor extending
into the distance, one-point perspective.

Style: Pixel art, simple repeating pattern for side-scrolling game,
moody lighting with torch shadows.
Color palette: Dark gray/blue stones, orange torch light, brown wooden floor.

No characters, no foreground objects, just empty corridor for game background.
```

### 분위기
- 어두운 던전 복도
- 좌우 벽돌 벽, 중앙 바닥
- 횃불이 밝히는 어두운 공간
- **반복 패턴으로 무한 스크롤**

---

## 4. 보스 층 배경 (복도, 고급지게)

**파일명:** `background_boss.png`  
**크기:** 400x300 픽셀  
**저장 경로:** `assets/images/backgrounds/`

### 프롬프트 (영어)
```
Pixel art boss room corridor background, seamless tileable pattern,
size 400x300 pixels, side view scrolling background.

Elements: Ornate stone walls with golden decorations, marble floor,
multiple torches/candelabras on walls, grand corridor extending
into the distance, one-point perspective.

Style: Pixel art, simple repeating pattern for side-scrolling game,
impressive and spacious atmosphere.
Color palette: Dark purple/gold stones, bright torch light, polished floor.

No characters, no foreground objects, just empty boss corridor.
```

### 분위기
- 일반 층보다 고급지고 웅장함
- 금색 장식, 대리석 바닥
- 더 밝고 넓은 느낌
- **반복 패턴으로 무한 스크롤**

---

## 5. 레벨업 이펙트 (선택)

**파일명:** `levelup_effect.png`  
**크기:** 64x64 픽셀 (또는 256x64, 4 프레임)  
**저장 경로:** `assets/images/effects/`

### 프롬프트 (영어)
```
Pixel art effect animation for level up celebration, 4 frames, 
each frame 64x64 pixels, arranged in a row (total 256x64).

Effect: Sparkling stars, circular burst, ascending particles, 
golden/yellow color scheme, celebratory feel.

Style: Clean pixel art, transparent background, game VFX asset 
for mobile idle RPG level up notification.
```

---

## 💡 팁

### 스프라이트 시트 읽기 (JavaScript)
```javascript
// 32x32 프레임, 4 컬럼 × 2 rows
const frameWidth = 32;
const frameHeight = 32;
const frameX = frameIndex % 4;  // 0-3
const frameY = Math.floor(frameIndex / 4);  // 0 또는 1

ctx.drawImage(
    sprite,
    frameX * frameWidth, frameY * frameHeight, frameWidth, frameHeight,
    x, y, displaySize, displaySize
);
```

### 이미지가 없으면?
현재 코드는 **이미지가 없으면 자동으로 단순 사각형으로 폴백**되므로,  
이미지 생성 전에도 게임 실행은 가능합니다.

---

*마지막 업데이트: 2025-04-07*
