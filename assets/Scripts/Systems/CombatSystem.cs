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
    private IGameLogger _logger;
    private DailyMissionSystem _dailyMissionSystem;
    
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
            _logger = ServiceLocator.Instance.Get<IGameLogger>();
        
        // DailyMissionSystem 참조 설정 (버프 시스템용)
        if (_dailyMissionSystem == null)
            _dailyMissionSystem = DailyMissionSystem.Instance;
    }

    // ========== 전투 설정 ==========
    
    /// <summary>전투 업데이트 간격 (초)</summary>
    private const float COMBAT_TICK = 0.1f;
    
    /// <summary>페이즈 전환 지연 시간 (초)</summary>
    private const float PHASE_DELAY = 0.5f;
    
    /// <summary>자동 반복 모드 여부</summary>
    private bool _autoRepeatMode = false;
    
    /// <summary>HP 재생 타이머 (ms)</summary>
    private float _hpRegenTimer = 0f;

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
        
        // HP 재생 (모든 페이즈에서 적용)
        UpdateHpRegen(deltaTime);
        
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
                if (_phaseTimer >= 2f) // 승리 후 2초 대기 (몬스터가 죽는 걸 볼 수 있도록)
                {
                    // 다음 스테이지로 자동 진행
                    _logger.Debug($"VICTORY 페이즈 완료, 다음 스테이지로 이동");
                    StageSystem.Instance.NextStage();
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
        _logger.Debug($"[DEBUG] StartCombat 호출됨 - 현재 페이즈: {_currentPhase}");
        
        if (_currentPhase != CombatPhase.IDLE && _currentPhase != CombatPhase.DEFEATED)
        {
            _logger.Warn($"전투 시작 불가 - 현재 페이즈: {_currentPhase}");
            return;
        }
        
        // ✅ 전투 데이터 초기화 (이전 전투의 잔여 데이터 제거)
        var combatPhase = _gameState.CombatPhase;
        _logger.Debug($"[DEBUG] StartCombat - 초기화 전 monsterState.currentHP: {combatPhase.monsterState.currentHP}");
        
        combatPhase.phase = 0;
        combatPhase.timer = 0;
        combatPhase.monsterState = new MonsterData(); // HP=0인 몬스터 초기화
        _gameState.CombatPhase = combatPhase;
        
        _logger.Debug($"[DEBUG] StartCombat - 초기화 후 monsterState.currentHP: {_gameState.CombatPhase.monsterState.currentHP}");
        
        // 플레이어 HP 회복 (스테이지 시작 시)
        _gameState.Player.currentHP = _gameState.GetTotalHealth();
        
        // 플레이어 공격 속도 설정
        _playerAttackSpeed = 1f + (_gameState.Player.speed * 0.01f);
        
        // 전투 타이머 초기화
        _combatTimer = 0f;
        _lastAttackTime = 0f;
        
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
    /// HP 재생 업데이트 (모든 페이즈에서 호출)
    /// 정확히 1초마다 한 번씩 hpRegen 값만큼 회복
    /// </summary>
    private void UpdateHpRegen(float deltaTime)
    {
        // hpRegen 계산: (goldUpgrades["hpRegen"] + statUpgrades["hpRegen"]) * baseValue
        // Web 버전: hpRegen baseValue = 1
        int hpRegenLevel = 0;
        if (_gameState.Player.goldUpgrades.ContainsKey("hpRegen"))
            hpRegenLevel += _gameState.Player.goldUpgrades["hpRegen"];
        if (_gameState.Player.statUpgrades.ContainsKey("hpRegen"))
            hpRegenLevel += _gameState.Player.statUpgrades["hpRegen"];
        
        // 효율 배율 계산 (Web 버전과 동일: 10레벨마다 증가)
        float efficiencyMultiplier = 1f;
        if (hpRegenLevel >= 40) efficiencyMultiplier = 3f;
        else if (hpRegenLevel >= 30) efficiencyMultiplier = 2.5f;
        else if (hpRegenLevel >= 20) efficiencyMultiplier = 2f;
        else if (hpRegenLevel >= 10) efficiencyMultiplier = 1.5f;
        
        // hpRegen 값 = 누적값 * baseValue(1)
        float hpRegen = 0;
        for (int i = 0; i < hpRegenLevel; i++)
        {
            hpRegen += efficiencyMultiplier;
        }
        
        if (hpRegen <= 0) return;
        
        float maxHp = _gameState.GetTotalHealth();
        var player = _gameState.Player;
        
        // 1초 타이머에 deltaTime 누적 (deltaTime은 초)
        _hpRegenTimer += deltaTime;
        
        // 정확히 1초마다 회복
        if (_hpRegenTimer >= 1f)
        {
            if (player.currentHP < maxHp)
            {
                player.currentHP = Mathf.Min(maxHp, player.currentHP + hpRegen);
                _gameState.Player = player;
                
                _eventBus.Emit(GameEvents.PLAYER_STAT_CHANGED);
            }
            // 타이머 리셋 (남은 시간은 다음 주기로 이월)
            _hpRegenTimer -= 1f;
        }
    }
    
    /// <summary>
    /// 플레이어 공격
    /// </summary>
    private void PlayerAttack()
    {
        // 버프 확인 (공격력 2배)
        float attackBuff = GetBuffMultiplier("attackDouble");
        
        // 보석 업그레이드: 자동 전투 강화 (자동 반복 시 2%/레벨, 최대 100%) - 해금 필요
        // TODO: StageData.autoRepeat와 PlayerData.gemUpgrades 구현 후 활성화
        float autoCombatBonus = 1f;
        /*
        if (_gameState.Stage.autoRepeat)
        {
            var gemUpgrades = _gameState.Player.gemUpgrades;
            if (gemUpgrades != null && gemUpgrades.ContainsKey("autoCombatDamage"))
            {
                int autoCombatLevel = gemUpgrades["autoCombatDamage"];
                autoCombatBonus = 1f + Mathf.Min(1f, autoCombatLevel * 0.02f);
            }
        }
        */
        
        // 데미지 계산
        float damage = CalculateDamage(
            _gameState.GetTotalAttack(),
            _gameState.CombatPhase.monsterState.defense,
            _gameState.Player.critChance,
            _gameState.GetCritDamageMultiplier(),
            attackBuff,
            autoCombatBonus
        );
        
        // 몬스터 HP 감소
        var monster = _gameState.CombatPhase.monsterState;
        monster.currentHP -= damage;
        var combatPhase = _gameState.CombatPhase;
        combatPhase.monsterState = monster;
        
        _logger.Debug($"플레이어 공격 - 데미지: {damage:F1}, 몬스터 HP: {monster.currentHP:F1}/{monster.maxHP:F1}");
        
        // 공격 애니메이션 트리거 (GameRenderer)
        GameRenderer.Instance?.TriggerPlayerAttack();
        GameRenderer.Instance?.TriggerMonsterHit();
        
        // 몬스터 사망 확인
        if (monster.currentHP <= 0)
        {
            monster.currentHP = 0;
            combatPhase.monsterState = monster;
            ChangePhase(CombatPhase.VICTORY);
            return; // 몬스터가 죽었으면 반동 데미지 없음
        }
        
        // 공격 반동 데미지: 플레이어 체력 소모
        // 공식: (stage * 4) - playerDefense, 최소 1
        ConsumePlayerHP();
    }

    /// <summary>
    /// 버프 배율 가져오기
    /// </summary>
    /// <param name="buffType">버프 타입 (attackDouble, hpDouble, goldDouble, expDouble)</param>
    /// <returns>배율 (활성화 시 2.0, 아니면 1.0)</returns>
    private float GetBuffMultiplier(string buffType)
    {
        if (_dailyMissionSystem != null)
        {
            return _dailyMissionSystem.GetBuffMultiplier(buffType);
        }
        return 1.0f;
    }

    /// <summary>
    /// 플레이어 체력 소모 (공격 반동)
    /// Web 버전과 동일한 로직: 스테이지당 4 데미지 - 방어력, 최소 1
    /// </summary>
    private void ConsumePlayerHP()
    {
        int stage = _gameState.Stage.currentStage;
        float playerDefense = _gameState.GetTotalDefense();
        
        // 플레이어 체력 소모 (스테이지 비례 고정 수치 - 방어력)
        // 공식: (stage * 4) - playerDefense, 최소 1
        float baseRecoil = stage * 4f; // 스테이지당 4 데미지
        float recoilDamage = Mathf.Max(1f, baseRecoil - playerDefense);
        
        var player = _gameState.Player;
        player.currentHP = Mathf.Max(0f, player.currentHP - recoilDamage);
        _gameState.Player = player;
        
        _logger.Debug($"공격 반동 데미지: {recoilDamage:F1}, 플레이어 HP: {player.currentHP:F1}/{_gameState.GetTotalHealth():F1}");
        
        // 플레이어 사망 확인
        if (player.currentHP <= 0)
        {
            player.currentHP = 0;
            _gameState.Player = player;
            ChangePhase(CombatPhase.DEFEATED);
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
        
        // 몬스터 공격 애니메이션 트리거
        GameRenderer.Instance?.TriggerMonsterAttack();
        GameRenderer.Instance?.TriggerPlayerHit();
        
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
    /// <param name="buffMultiplier">버프 배율 (기본 1)</param>
    /// <param name="autoCombatBonus">자동 전투 보너스 (기본 1)</param>
    /// <returns>최종 데미지</returns>
    public float CalculateDamage(float attack, float defense, float critChance, float critDamage, 
        float buffMultiplier = 1f, float autoCombatBonus = 1f)
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
        
        // 버프 및 자동 전투 보너스 적용
        damage *= buffMultiplier * autoCombatBonus;
        
        return Mathf.Round(damage * 10f) / 10f;
    }

    /// <summary>
    /// 승패 판정
    /// </summary>
    private void CheckCombatResult()
    {
        float monsterHP = _gameState.CombatPhase.monsterState.currentHP;
        float playerHP = _gameState.Player.currentHP;
        
        _logger.Debug($"[DEBUG] CheckCombatResult - monsterHP:{monsterHP}, playerHP:{playerHP}");
        
        // 몬스터 사망
        if (monsterHP <= 0)
        {
            _logger.Debug($"[DEBUG] 몬스터 사망 감지 - VICTORY로 전환");
            ChangePhase(CombatPhase.VICTORY);
            return;
        }
        
        // 플레이어 사망
        if (playerHP <= 0)
        {
            _logger.Debug($"[DEBUG] 플레이어 사망 감지 - DEFEATED로 전환");
            ChangePhase(CombatPhase.DEFEATED);
        }
    }

    // ========== 몬스터 시스템 ==========
    
    // 분리된 컴포넌트
    private MonsterFactory _monsterFactory;
    private DropTable _dropTable;
    
    /// <summary>
    /// 몬스터 생성 (MonsterFactory로 위임)
    /// </summary>
    private void SpawnMonster()
    {
        int stage = _gameState.Stage.currentStage;
        
        _logger.Debug($"[DEBUG] SpawnMonster 시작 - 스테이지 {stage}");
        _logger.Debug($"[DEBUG] 현재 combatPhase.monsterState.currentHP: {_gameState.CombatPhase.monsterState.currentHP}");
        
        // MonsterFactory를 사용하여 몬스터 생성
        if (_monsterFactory == null)
            _monsterFactory = new MonsterFactory();
        
        MonsterData monster = _monsterFactory.CreateMonster(stage);
        
        _logger.Debug($"[DEBUG] MonsterFactory 생성 - name:{monster.name}, maxHP:{monster.maxHP}, currentHP:{monster.currentHP}");
        
        // 몬스터 HP를 최대 HP로 명시적 초기화 (죽은 상태로 등장하는 버그 방지)
        monster.currentHP = monster.maxHP;
        
        _logger.Debug($"[DEBUG] HP 초기화 후 - currentHP:{monster.currentHP}");
        
        var combatPhase = _gameState.CombatPhase;
        combatPhase.monsterState = monster;
        _gameState.CombatPhase = combatPhase;
        
        _logger.Debug($"[DEBUG] GameState에 할당 후 확인 - currentHP:{_gameState.CombatPhase.monsterState.currentHP}");
        
        // 몬스터 공격 속도 설정
        _monsterAttackSpeed = _monsterFactory.GetMonsterAttackSpeed(monster);
        
        _logger.Info($"몬스터 등장 - {monster.name} (스테이지 {stage}, HP: {monster.currentHP}/{monster.maxHP}, {(monster.grade >= 3 ? "보스" : "일반")})");
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
        
        // 보석 드롭 (0.1% 확률로 1개) - 자동 반복 모드에서는 드랍되지 않음
        if (!_autoRepeatMode)
        {
            RollGemDrop();
        }
        
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
    /// 경험치 보상 계산 (DropTable로 위임)
    /// </summary>
    private long CalculateExpReward()
    {
        if (_dropTable == null)
            _dropTable = new DropTable();
        
        int stage = _gameState.Stage.currentStage;
        bool isBoss = (stage % 10 == 0);
        
        return _dropTable.GetExpReward(stage, isBoss);
    }

    /// <summary>
    /// 골드 드롭량 계산 (DropTable로 위임)
    /// </summary>
    private int CalculateGoldDrop()
    {
        if (_dropTable == null)
            _dropTable = new DropTable();
        
        int stage = _gameState.Stage.currentStage;
        bool isBoss = (stage % 10 == 0);
        int monsterGrade = _gameState.CombatPhase.monsterState.grade;
        
        return _dropTable.GetGoldDrop(monsterGrade, stage, isBoss);
    }

    /// <summary>
    /// 아이템 드롭 (DropTable로 위임)
    /// </summary>
    private void DropLoot()
    {
        if (_dropTable == null)
            _dropTable = new DropTable();
        
        int monsterGrade = _gameState.CombatPhase.monsterState.grade;
        int stage = _gameState.Stage.currentStage;
        
        ItemData? dropItem = _dropTable.GetDrop(monsterGrade, stage);
        
        if (dropItem == null)
            return;
        
        ItemData item = dropItem.Value;
        
        // 인벤토리에 추가
        var inventory = _gameState.Inventory;
        inventory.items.Add(item);
        _gameState.Inventory = inventory;
        
        // 발견 아이템 등록
        if (!inventory.discoveredItems.Contains(item.id))
        {
            inventory.discoveredItems.Add(item.id);
            var stats = _gameState.Stats;
            stats.totalItemsDiscovered++;
            _gameState.Stats = stats;
            _eventBus.Emit(GameEvents.ITEM_DISCOVERED);
        }
        
        _eventBus.Emit(GameEvents.ITEM_ACQUIRED);
        
        _logger.Info($"아이템 드롭: {item.name} ({_dropTable.GetGradeName(item.grade)}등급)");
    }

    /// <summary>
    /// 보석 드롭 확률 롤 (0.1% 확률로 1개)
    /// Web 버전과 동일한 로직
    /// </summary>
    private void RollGemDrop()
    {
        const float dropChance = 0.001f; // 0.1%
        
        if (Random.value < dropChance)
        {
            var player = _gameState.Player;
            player.gems += 1;
            _gameState.Player = player;
            
            _eventBus.Emit(GameEvents.GEM_CHANGED);
            
            _logger.Info("보석 드롭! 💎 +1");
        }
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
