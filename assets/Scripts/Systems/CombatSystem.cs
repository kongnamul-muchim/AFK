using UnityEngine;
using System.Collections;

/// <summary>
/// 전투 페이즈 열거형
/// </summary>
public enum CombatPhase
{
    /// <summary>대기 상태 - 다음 스테이지로 이동 준비</summary>
    IDLE,
    
    /// <summary>이동 상태 - 플레이어/몬스터 이동 애니메이션</summary>
    MOVING,
    
    /// <summary>조우 상태 - 몬스터 등장 애니메이션</summary>
    ENCOUNTERING,
    
    /// <summary>전투 상태 - 공격/피격 루프</summary>
    COMBAT,
    
    /// <summary>승리 상태 - 몬스터 처치, 보상 지급</summary>
    VICTORY,
    
    /// <summary>패배 상태 - 플레이어 사망, 부활 대기</summary>
    DEFEATED
}

/// <summary>
/// 게임 전투 시스템을 관리하는 클래스
/// 플레이어와 몬스터의 전투 로직을 처리하며, 페이즈 머신 기반으로 동작합니다.
/// DIP 준수: ServiceLocator를 통한 의존성 주입
/// </summary>
public class CombatSystem : MonoBehaviour
{
    private static CombatSystem _instance;
    
    /// <summary>
    /// CombatSystem의 싱글톤 인스턴스
    /// </summary>
    public static CombatSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("CombatSystem");
                _instance = go.AddComponent<CombatSystem>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // ========== 의존성 주입 ==========
    
    private IGameState _gameState;
    private IEventBus _eventBus;
    private ILogger _logger;
    
    /// <summary>
    /// ServiceLocator를 통한 의존성 주입
    /// </summary>
    private void InjectDependencies()
    {
        if (_gameState == null)
            _gameState = ServiceLocator.Instance.Get<IGameState>();
        if (_eventBus == null)
            _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        if (_logger == null)
            _logger = ServiceLocator.Instance.Get<ILogger>();
    }

    // ========== 전투 설정 ==========
    
    /// <summary>전투 업데이트 간격 (초)</summary>
    private const float COMBAT_TICK = 0.1f;
    
    /// <summary>페이즈 전환 지연 시간 (초)</summary>
    private const float PHASE_DELAY = 0.5f;
    
    /// <summary>자동 반복 모드 여부</summary>
    private bool _autoRepeatMode = false;

    // ========== 현재 상태 ==========
    
    /// <summary>현재 전투 페이즈</summary>
    private CombatPhase _currentPhase = CombatPhase.IDLE;
    
    /// <summary>현재 페이즈 경과 시간</summary>
    private float _phaseTimer = 0f;
    
    /// <summary>전투 타이머</summary>
    private float _combatTimer = 0f;
    
    /// <summary>마지막 공격 시간</summary>
    private float _lastAttackTime = 0f;
    
    /// <summary>플레이어 공격 속도 (초당 공격 횟수)</summary>
    private float _playerAttackSpeed = 1f;
    
    /// <summary>몬스터 공격 속도</summary>
    private float _monsterAttackSpeed = 1f;

    // ========== 코루틴 ==========
    
    private Coroutine _combatLoopCoroutine;

    // ========== MonoBehaviour 라이프사이클 ==========
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 의존성 주입
        InjectDependencies();
    }

    private void OnDestroy()
    {
        StopCombatLoop();
    }

    private void OnEnable()
    {
        // 의존성 주입 확인
        InjectDependencies();
        
        // 이벤트 구독
        _eventBus.On(GameEvents.PLAYER_STAT_CHANGED, OnPlayerStatChanged);
    }

    private void OnDisable()
    {
        // 이벤트 해제
        _eventBus.Off(GameEvents.PLAYER_STAT_CHANGED, OnPlayerStatChanged);
    }

    // ========== 페이즈 관리 ==========
    
    /// <summary>
    /// 현재 전투 페이즈
    /// </summary>
    public CombatPhase CurrentPhase => _currentPhase;

    /// <summary>
    /// 페이즈 변경
    /// </summary>
    /// <param name="newPhase">새 페이즈</param>
    public void ChangePhase(CombatPhase newPhase)
    {
        CombatPhase oldPhase = _currentPhase;
        _currentPhase = newPhase;
        _phaseTimer = 0f;
        
        _logger.Debug($"전투 페이즈 변경: {oldPhase} → {newPhase}");
        
        // 페이즈 변경 이벤트 발생
        _eventBus.Emit(GameEvents.COMBAT_PHASE_CHANGED);
        
        // 페이즈 진입 처리
        OnEnterPhase(newPhase);
    }

    private void OnEnterPhase(CombatPhase phase)
    {
        switch (phase)
        {
            case CombatPhase.IDLE:
                // 대기 상태 - 아무것도 안 함
                break;
                
            case CombatPhase.MOVING:
                // 이동 애니메이션 시작
                _phaseTimer = 0f;
                break;
                
            case CombatPhase.ENCOUNTERING:
                // 몬스터 등장
                SpawnMonster();
                _phaseTimer = 0f;
                break;
                
            case CombatPhase.COMBAT:
                // 전투 시작
                StartCombatLoop();
                _combatTimer = 0f;
                break;
                
            case CombatPhase.VICTORY:
                // 승리 처리
                StopCombatLoop();
                ProcessVictory();
                break;
                
            case CombatPhase.DEFEATED:
                // 패배 처리
                StopCombatLoop();
                ProcessDefeat();
                break;
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        _phaseTimer += deltaTime;
        
        // 페이즈별 시간 기반 전이
        switch (_currentPhase)
        {
            case CombatPhase.MOVING:
                if (_phaseTimer >= PHASE_DELAY)
                {
                    ChangePhase(CombatPhase.ENCOUNTERING);
                }
                break;
                
            case CombatPhase.ENCOUNTERING:
                if (_phaseTimer >= PHASE_DELAY)
                {
                    ChangePhase(CombatPhase.COMBAT);
                }
                break;
                
            case CombatPhase.VICTORY:
                if (_phaseTimer >= PHASE_DELAY)
                {
                    // 다음 스테이지로
                    if (_autoRepeatMode)
                    {
                        ChangePhase(CombatPhase.MOVING);
                    }
                    else
                    {
                        ChangePhase(CombatPhase.IDLE);
                    }
                }
                break;
                
            case CombatPhase.DEFEATED:
                if (_phaseTimer >= PHASE_DELAY * 2)
                {
                    if (_autoRepeatMode)
                    {
                        // 자동 반복 모드 - 즉시 재전투
                        ChangePhase(CombatPhase.MOVING);
                    }
                    else
                    {
                        // 수동 모드 - 이전 스테이지에서 부활
                        ChangePhase(CombatPhase.IDLE);
                    }
                }
                break;
        }
    }

    // ========== 전투 시작/종료 ==========
    
    /// <summary>
    /// 전투 시작 (IDLE → MOVING → ENCOUNTERING → COMBAT)
    /// </summary>
    public void StartCombat()
    {
        if (_currentPhase != CombatPhase.IDLE && _currentPhase != CombatPhase.DEFEATED)
        {
            _logger.Warn($"전투 시작 불가 - 현재 페이즈: {_currentPhase}");
            return;
        }
        
        // 플레이어 HP 회복 (스테이지 시작 시)
        _gameState.Player.currentHP = _gameState.GetTotalHealth();
        
        // 플레이어 공격 속도 설정
        _playerAttackSpeed = 1f + (_gameState.Player.speed * 0.01f);
        
        ChangePhase(CombatPhase.MOVING);
        
        // 전투 조우 이벤트
        _eventBus.Emit(GameEvents.COMBAT_ENCOUNTER);
        
        _logger.Info($"전투 시작 - 스테이지 {_gameState.Stage.currentStage}");
    }

    /// <summary>
    /// 자동 반복 모드 토글
    /// </summary>
    /// <param name="enabled">자동 반복 활성화 여부</param>
    public void SetAutoRepeatMode(bool enabled)
    {
        _autoRepeatMode = enabled;
        _logger.Info($"자동 반복 모드: {enabled}");
    }

    /// <summary>
    /// 자동 반복 모드 여부
    /// </summary>
    public bool IsAutoRepeatMode() => _autoRepeatMode;

    private void StartCombatLoop()
    {
        if (_combatLoopCoroutine != null)
        {
            StopCoroutine(_combatLoopCoroutine);
        }
        _combatLoopCoroutine = StartCoroutine(CombatLoopCoroutine());
    }

    private void StopCombatLoop()
    {
        if (_combatLoopCoroutine != null)
        {
            StopCoroutine(_combatLoopCoroutine);
            _combatLoopCoroutine = null;
        }
    }

    private IEnumerator CombatLoopCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(COMBAT_TICK);
        
        while (_currentPhase == CombatPhase.COMBAT)
        {
            yield return wait;
            
            _combatTimer += COMBAT_TICK;
            
            // 플레이어 공격
            float playerAttackInterval = 1f / _playerAttackSpeed;
            if (_combatTimer - _lastAttackTime >= playerAttackInterval)
            {
                PlayerAttack();
                _lastAttackTime = _combatTimer;
            }
            
            // 몬스터 공격
            float monsterAttackInterval = 1f / _monsterAttackSpeed;
            if (_combatTimer >= monsterAttackInterval)
            {
                MonsterAttack();
            }
            
            // 승패 판정
            CheckCombatResult();
        }
    }

    // ========== 공격/데미지 ==========
    
    /// <summary>
    /// 플레이어 공격
    /// </summary>
    private void PlayerAttack()
    {
        // 데미지 계산
        float damage = CalculateDamage(
            _gameState.GetTotalAttack(),
            _gameState.CombatPhase.monsterState.defense,
            _gameState.Player.critChance,
            _gameState.GetCritDamageMultiplier()
        );
        
        // 몬스터 HP 감소
        var monster = _gameState.CombatPhase.monsterState;
        monster.currentHP -= damage;
        var combatPhase = _gameState.CombatPhase;
        combatPhase.monsterState = monster;
        
        _logger.Debug($"플레이어 공격 - 데미지: {damage:F1}, 몬스터 HP: {monster.currentHP:F1}/{monster.maxHP:F1}");
        
        // 몬스터 사망 확인
        if (monster.currentHP <= 0)
        {
            monster.currentHP = 0;
            combatPhase.monsterState = monster;
            ChangePhase(CombatPhase.VICTORY);
        }
    }

    /// <summary>
    /// 몬스터 공격
    /// </summary>
    private void MonsterAttack()
    {
        // 플레이어 총 방어력
        float playerDefense = _gameState.GetTotalDefense();
        
        // 데미지 계산 (몬스터 → 플레이어)
        float damage = CalculateDamage(
            _gameState.CombatPhase.monsterState.attack,
            playerDefense,
            0f, // 몬스터 치명확률
            GameConfig.MonsterCritDamage // 몬스터 치명피해
        );
        
        // 플레이어 HP 감소
        var player = _gameState.Player;
        player.currentHP -= damage;
        _gameState.Player = player;
        
        _logger.Debug($"몬스터 공격 - 데미지: {damage:F1}, 플레이어 HP: {player.currentHP:F1}/{_gameState.GetTotalHealth():F1}");
        
        // 플레이어 사망 확인
        if (player.currentHP <= 0)
        {
            player.currentHP = 0;
            _gameState.Player = player;
            ChangePhase(CombatPhase.DEFEATED);
        }
    }

    /// <summary>
    /// 데미지 계산
    /// </summary>
    /// <param name="attack">공격력</param>
    /// <param name="defense">방어력</param>
    /// <param name="critChance">치명확률 (0-1)</param>
    /// <param name="critDamage">치명피해 배율</param>
    /// <returns>최종 데미지</returns>
    public float CalculateDamage(float attack, float defense, float critChance, float critDamage)
    {
        // 기본 데미지 = 공격력 - 방어력 (최소 1)
        float baseDamage = Mathf.Max(1f, attack - defense);
        
        // 데미지 변동폭 (±10%)
        float variance = Random.Range(GameConfig.DamageVarianceMin, GameConfig.DamageVarianceMax);
        float damage = baseDamage * variance;
        
        // 치명타 판정
        bool isCrit = Random.value < critChance;
        if (isCrit)
        {
            damage *= critDamage;
            _logger.Debug("치명타!");
        }
        
        return Mathf.Round(damage * 10f) / 10f;
    }

    /// <summary>
    /// 승패 판정
    /// </summary>
    private void CheckCombatResult()
    {
        // 몬스터 사망
        if (_gameState.CombatPhase.monsterState.currentHP <= 0)
        {
            ChangePhase(CombatPhase.VICTORY);
        }
        
        // 플레이어 사망
        if (_gameState.Player.currentHP <= 0)
        {
            ChangePhase(CombatPhase.DEFEATED);
        }
    }

    // ========== 몬스터 시스템 ==========
    
    /// <summary>
    /// 몬스터 생성
    /// </summary>
    private void SpawnMonster()
    {
        int stage = _gameState.Stage.currentStage;
        
        // 보스 여부 판정 (10스테이지마다)
        bool isBoss = (stage % 10 == 0);
        
        // 몬스터 스펙 계산
        MonsterData monster = CreateMonsterData(stage, isBoss);
        var combatPhase = _gameState.CombatPhase;
        combatPhase.monsterState = monster;
        
        // 몬스터 공격 속도 설정
        _monsterAttackSpeed = 1f; // 기본값, 몬스터 종류에 따라 변경 가능
        
        _logger.Info($"몬스터 등장 - {monster.name} (스테이지 {stage}, {(isBoss ? "보스" : "일반")})");
    }

    /// <summary>
    /// 몬스터 데이터 생성
    /// </summary>
    private MonsterData CreateMonsterData(int stage, bool isBoss)
    {
        // 최대 스테이지 기반 몬스터 스펙 계산
        int effectiveStage = Mathf.Max(stage, _gameState.Stage.maxStage);
        
        float baseHP = GameConfig.BaseMonsterHP;
        float baseAttack = GameConfig.BaseMonsterAttack;
        float baseDefense = GameConfig.BaseMonsterDefense;
        
        // 스테이지 비례 증가
        float hpMultiplier = 1f + (effectiveStage - 1) * GameConfig.MonsterStatPerStage;
        float attackMultiplier = 1f + (effectiveStage - 1) * GameConfig.MonsterStatPerStage * 0.8f;
        float defenseMultiplier = 1f + (effectiveStage - 1) * GameConfig.MonsterStatPerStage * 0.6f;
        
        float hp = baseHP * hpMultiplier;
        float attack = baseAttack * attackMultiplier;
        float defense = baseDefense * defenseMultiplier;
        
        // 보스 배율
        if (isBoss)
        {
            hp *= GameConfig.BossStatMultiplier;
            attack *= GameConfig.BossStatMultiplier;
            defense *= GameConfig.BossStatMultiplier;
        }
        
        // 몬스터 등급 (보스는 항상 영웅 이상)
        int grade = isBoss ? 3 : GetMonsterGrade(effectiveStage);
        
        // 등급별 스탯 보정
        float gradeMult = GameConfig.GradeStatMultipliers[Mathf.Min(grade, 4)];
        
        hp *= gradeMult;
        attack *= gradeMult;
        defense *= gradeMult;
        
        return new MonsterData
        {
            name = GetMonsterName(stage, isBoss),
            stage = stage,
            currentHP = hp,
            maxHP = hp,
            attack = attack,
            defense = defense,
            grade = grade
        };
    }

    /// <summary>
    /// 몬스터 등급 결정
    /// </summary>
    private int GetMonsterGrade(int stage)
    {
        // 스테이지가 높을수록 고등급 몬스터 등장 확률 증가
        float[] rates = _gameState.GetDropRates();
        
        float roll = Random.value;
        float cumulative = 0f;
        
        for (int i = 0; i < rates.Length; i++)
        {
            cumulative += rates[i];
            if (roll < cumulative)
            {
                return i;
            }
        }
        
        return 0; // 일반
    }

    /// <summary>
    /// 몬스터 이름 생성
    /// </summary>
    private string GetMonsterName(int stage, bool isBoss)
    {
        string[] prefixes = new string[] { "작은 ", "일반 ", "강한 ", "엘리트 ", "보스 " };
        string[] monsterTypes = new string[] { "슬라임", "고블린", "오크", "트롤", "드래곤" };
        
        int typeIndex = Mathf.Min(stage / 10, monsterTypes.Length - 1);
        string prefix = isBoss ? "보스 " : prefixes[Mathf.Min(stage / 5, prefixes.Length - 1)];
        
        return prefix + monsterTypes[typeIndex];
    }

    // ========== 승리/패배 처리 ==========
    
    /// <summary>
    /// 승리 처리
    /// </summary>
    private void ProcessVictory()
    {
        // 경험치 지급
        long expReward = CalculateExpReward();
        var player = _gameState.Player;
        player.experience += expReward;
        _gameState.Player = player;
        
        // 골드 드롭
        int goldReward = CalculateGoldDrop();
        player = _gameState.Player;
        player.gold += goldReward;
        _gameState.Player = player;
        
        // 아이템 드롭
        DropLoot();
        
        // 통계 업데이트
        var stats = _gameState.Stats;
        stats.totalKills++;
        if (_gameState.CombatPhase.monsterState.grade >= 3)
        {
            stats.totalBossKills++;
        }
        _gameState.Stats = stats;
        
        // 스테이지 클리어 (현재 스테이지가 최대 스테이지인 경우)
        if (_gameState.Stage.currentStage >= _gameState.Stage.maxStage)
        {
            var stage = _gameState.Stage;
            stage.maxStage = stage.currentStage + 1;
            _gameState.Stage = stage;
            _eventBus.Emit(GameEvents.STAGE_RECORD_UPDATED);
        }
        
        // 클리어 플래그 설정
        if (_gameState.Stage.clearedStages != null && _gameState.Stage.currentStage <= _gameState.Stage.clearedStages.Length)
        {
            var clearedStages = _gameState.Stage.clearedStages;
            clearedStages[_gameState.Stage.currentStage - 1] = true;
            var stage2 = _gameState.Stage;
            stage2.clearedStages = clearedStages;
            _gameState.Stage = stage2;
        }
        
        // 이벤트 발생
        _eventBus.Emit(GameEvents.MONSTER_KILL);
        _eventBus.Emit(GameEvents.COMBAT_VICTORY);
        _eventBus.Emit(GameEvents.GOLD_CHANGED);
        _eventBus.Emit(GameEvents.STATS_CHANGED);
        
        // 미션 진행도 업데이트
        _eventBus.Emit(GameEvents.DAILY_MISSION_PROGRESS);
        
        _logger.Info($"승리! - 경험치 +{expReward}, 골드 +{goldReward}");
    }

    /// <summary>
    /// 패배 처리
    /// </summary>
    private void ProcessDefeat()
    {
        // 자동 반복 모드라면 HP만 회복
        if (_autoRepeatMode)
        {
            var player = _gameState.Player;
            player.currentHP = _gameState.GetTotalHealth();
            _gameState.Player = player;
            _logger.Info("패배 - 자동 반복 모드로 재전투");
        }
        else
        {
            // 수동 모드 - 이전 스테이지로 돌아가기
            var stage = _gameState.Stage;
            stage.currentStage = Mathf.Max(1, stage.currentStage - 1);
            _gameState.Stage = stage;
            
            var player = _gameState.Player;
            player.currentHP = _gameState.GetTotalHealth();
            _gameState.Player = player;
            
            _logger.Info($"패배 - 스테이지 {_gameState.Stage.currentStage}로 후퇴");
        }
        
        _eventBus.Emit(GameEvents.COMBAT_DEFEAT);
    }

    /// <summary>
    /// 경험치 보상 계산
    /// </summary>
    private long CalculateExpReward()
    {
        int stage = _gameState.Stage.currentStage;
        bool isBoss = (stage % 10 == 0);
        
        long baseExp = 10 * stage;
        
        if (isBoss)
        {
            baseExp *= 5;
        }
        
        return baseExp;
    }

    /// <summary>
    /// 골드 드롭량 계산
    /// </summary>
    private int CalculateGoldDrop()
    {
        int stage = _gameState.Stage.currentStage;
        bool isBoss = (stage % 10 == 0);
        int monsterGrade = _gameState.CombatPhase.monsterState.grade;
        
        int baseGold = 5 * stage;
        
        // 등급 보정
        float gradeMult = GameConfig.GradeStatMultipliers[monsterGrade];
        
        // 보스 보정
        if (isBoss)
        {
            gradeMult *= 3;
        }
        
        // 변동폭
        float variance = Random.Range(GameConfig.GoldDropVarianceMin, GameConfig.GoldDropVarianceMax);
        
        return Mathf.RoundToInt(baseGold * gradeMult * variance);
    }

    /// <summary>
    /// 아이템 드롭
    /// </summary>
    private void DropLoot()
    {
        // 드롭 확률 확인
        if (Random.value > GameConfig.ItemDropRate)
        {
            return; // 드롭 없음
        }
        
        // 등급 결정
        float[] dropRates = _gameState.GetDropRates();
        int grade = 0;
        float roll = Random.value;
        float cumulative = 0f;
        
        for (int i = 0; i < dropRates.Length; i++)
        {
            cumulative += dropRates[i];
            if (roll < cumulative)
            {
                grade = i;
                break;
            }
        }
        
        // 아이템 생성
        string itemId = GenerateItemId(grade);
        string itemName = GenerateItemName(grade);
        
        ItemData item = new ItemData
        {
            id = itemId,
            name = itemName,
            grade = grade,
            quantity = 1
        };
        
        // 인벤토리에 추가
        var inventory = _gameState.Inventory;
        inventory.items.Add(item);
        _gameState.Inventory = inventory;
        
        // 발견 아이템 등록
        if (!inventory.discoveredItems.Contains(itemId))
        {
            inventory.discoveredItems.Add(itemId);
            var stats = _gameState.Stats;
            stats.totalItemsDiscovered++;
            _gameState.Stats = stats;
            _eventBus.Emit(GameEvents.ITEM_DISCOVERED);
        }
        
        _eventBus.Emit(GameEvents.ITEM_ACQUIRED);
        
        _logger.Info($"아이템 드롭: {itemName} ({GetGradeName(grade)}등급)");
    }

    private string GenerateItemId(int grade)
    {
        string[] types = new string[] { "sword", "armor", "boots", "accessory" };
        string type = types[Random.Range(0, types.Length)];
        return $"{type}_grade{grade}_{Random.Range(1000, 9999)}";
    }

    private string GenerateItemName(int grade)
    {
        string[] prefixes = new string[] { "일반 ", "고급 ", "희귀 ", "영웅 ", "전설 " };
        string[] types = new string[] { "검", "방어구", "신발", "장신구" };
        
        string prefix = prefixes[grade];
        string type = types[Random.Range(0, types.Length)];
        
        return prefix + type;
    }

    private string GetGradeName(int grade)
    {
        string[] names = new string[] { "일반", "고급", "희귀", "영웅", "전설" };
        return names[Mathf.Min(grade, names.Length - 1)];
    }

    // ========== 유틸리티 ==========
    
    /// <summary>
    /// 플레이어 스탯 변경 시 공격 속도 재계산
    /// </summary>
    private void OnPlayerStatChanged()
    {
        _playerAttackSpeed = 1f + (_gameState.Player.speed * 0.01f);
    }

    /// <summary>
    /// 현재 전투 정보 가져오기
    /// </summary>
    public CombatInfo GetCombatInfo()
    {
        return new CombatInfo
        {
            phase = _currentPhase,
            stage = _gameState.Stage.currentStage,
            playerHP = _gameState.Player.currentHP,
            playerMaxHP = _gameState.GetTotalHealth(),
            playerAttack = _gameState.GetTotalAttack(),
            playerDefense = _gameState.GetTotalDefense(),
            monsterHP = _gameState.CombatPhase.monsterState.currentHP,
            monsterMaxHP = _gameState.CombatPhase.monsterState.maxHP,
            monsterAttack = _gameState.CombatPhase.monsterState.attack,
            monsterDefense = _gameState.CombatPhase.monsterState.defense,
            monsterName = _gameState.CombatPhase.monsterState.name,
            combatTime = _combatTimer,
            isAutoRepeat = _autoRepeatMode
        };
    }
}

/// <summary>
/// 전투 정보 구조체
/// </summary>
[System.Serializable]
public struct CombatInfo
{
    public CombatPhase phase;
    public int stage;
    public float playerHP;
    public float playerMaxHP;
    public float playerAttack;
    public float playerDefense;
    public float monsterHP;
    public float monsterMaxHP;
    public float monsterAttack;
    public float monsterDefense;
    public string monsterName;
    public float combatTime;
    public bool isAutoRepeat;
}
