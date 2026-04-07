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

## 1️⃣ 플레이어 스프라이트 시트

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

## 2️⃣ 몬스터 스프라이트 시트 (슬라임)

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

## 3️⃣ 일반 층 배경

**파일명:** `background_normal.png`  
**크기:** 400x300 픽셀  
**저장 경로:** `assets/images/backgrounds/`

### 프롬프트 (영어)
```
Pixel art background for tower dungeon interior, floor level 1-9, 
size 400x300 pixels, side view.

Elements: Stone brick walls, wooden floor, torches on walls providing 
warm orange light, dark shadows between torches, dungeon atmosphere.

Style: Pixel art, moody lighting, warm torch light contrasting with 
cool dark stone, game background for idle RPG.
Color palette: Dark blues/grays for walls, orange/yellow for torch light.
```

### 분위기
- 어두운 던전이지만 횃불이 밝히는 공간
- 벽돌 벽, 나무 바닥
- 횃불 사이의 그림자

---

## 4️⃣ 보스 층 배경

**파일명:** `background_boss.png`  
**크기:** 400x300 픽셀  
**저장 경로:** `assets/images/backgrounds/`

### 프롬프트 (영어)
```
Pixel art background for tower boss room interior, floor level 10, 20, 30..., 
size 400x300 pixels, side view.

Elements: Ornate stone architecture, golden decorations, large throne or 
altar in background, multiple torches/candelabras, more spacious and 
impressive than normal floors.

Style: Pixel art, grand and imposing atmosphere, brighter than normal 
floors but still dungeon-like, game background for idle RPG boss battle.
Color palette: Dark purples/golds for walls, bright orange/yellow for lighting.
```

### 분위기
- 일반 층보다 고급지고 웅장함
- 금색 장식, 왕좌 또는 제단
- 더 많은 조명

---

## 5️⃣ 레벨업 이펙트 (선택)

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
