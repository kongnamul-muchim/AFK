using UnityEngine;
using System.Collections;

/// <summary>
/// ?„íˆ¬ ?˜ì´ì¦??´ê±°??/// </summary>
public enum CombatPhase
{
    /// <summary>?€ê¸??íƒœ - ?¤ìŒ ?¤í…Œ?´ì?ë¡??´ë™ ì¤€ë¹?/summary>
    IDLE,
    
    /// <summary>?´ë™ ?íƒœ - ?Œë ˆ?´ì–´/ëª¬ìŠ¤???´ë™ ? ë‹ˆë©”ì´??/summary>
    MOVING,
    
    /// <summary>ì¡°ìš° ?íƒœ - ëª¬ìŠ¤???±ì¥ ? ë‹ˆë©”ì´??/summary>
    ENCOUNTERING,
    
    /// <summary>?„íˆ¬ ?íƒœ - ê³µê²©/?¼ê²© ë£¨í”„</summary>
    COMBAT,
    
    /// <summary>?¹ë¦¬ ?íƒœ - ëª¬ìŠ¤??ì²˜ì¹˜, ë³´ìƒ ì§€ê¸?/summary>
    VICTORY,
    
    /// <summary>?¨ë°° ?íƒœ - ?Œë ˆ?´ì–´ ?¬ë§, ë¶€???€ê¸?/summary>
    DEFEATED
}

/// <summary>
/// ê²Œì„ ?„íˆ¬ ?œìŠ¤?œì„ ê´€ë¦¬í•˜???´ë˜??/// ?Œë ˆ?´ì–´?€ ëª¬ìŠ¤?°ì˜ ?„íˆ¬ ë¡œì§??ì²˜ë¦¬?˜ë©°, ?˜ì´ì¦?ë¨¸ì‹  ê¸°ë°˜?¼ë¡œ ?™ì‘?©ë‹ˆ??
/// DIP ì¤€?? ServiceLocatorë¥??µí•œ ?˜ì¡´??ì£¼ì…
/// </summary>
public class CombatSystem : MonoBehaviour
{
    private static CombatSystem _instance;
    
    /// <summary>
    /// CombatSystem???±ê????¸ìŠ¤?´ìŠ¤
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

    // ========== ?˜ì¡´??ì£¼ì… ==========
    
    private IGameState _gameState;
    private IEventBus _eventBus;
    private IGameLogger _logger;
    private DailyMissionSystem _dailyMissionSystem;
    
    /// <summary>
    /// ServiceLocatorë¥??µí•œ ?˜ì¡´??ì£¼ì…
    /// </summary>
    private void InjectDependencies()
    {
        if (_gameState == null)
            _gameState = ServiceLocator.Instance.Get<IGameState>();
        if (_eventBus == null)
            _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        if (_logger == null)
            _logger = ServiceLocator.Instance.Get<IGameLogger>();
        
        // DailyMissionSystem ì°¸ì¡° ?¤ì • (ë²„í”„ ?œìŠ¤?œìš©)
        if (_dailyMissionSystem == null)
            _dailyMissionSystem = DailyMissionSystem.Instance;
    }

    // ========== ?„íˆ¬ ?¤ì • ==========
    
    /// <summary>?„íˆ¬ ?…ë°?´íŠ¸ ê°„ê²© (ì´?</summary>
    private const float COMBAT_TICK = 0.1f;
    
    /// <summary>?˜ì´ì¦??„í™˜ ì§€???œê°„ (ì´?</summary>
    private const float PHASE_DELAY = 0.5f;
    
    /// <summary>?ë™ ë°˜ë³µ ëª¨ë“œ ?¬ë?</summary>
    private bool _autoRepeatMode = false;
    
    /// <summary>HP ?¬ìƒ ?€?´ë¨¸ (ms)</summary>
    private float _hpRegenTimer = 0f;

    // ========== ?„ì¬ ?íƒœ ==========
    
    /// <summary>?„ì¬ ?„íˆ¬ ?˜ì´ì¦?/summary>
    private CombatPhase _currentPhase = CombatPhase.IDLE;
    
    /// <summary>?„ì¬ ?˜ì´ì¦?ê²½ê³¼ ?œê°„</summary>
    private float _phaseTimer = 0f;
    
    /// <summary>?„íˆ¬ ?€?´ë¨¸</summary>
    private float _combatTimer = 0f;
    
    /// <summary>ë§ˆì?ë§?ê³µê²© ?œê°„</summary>
    private float _lastAttackTime = 0f;
    
    /// <summary>?Œë ˆ?´ì–´ ê³µê²© ?ë„ (ì´ˆë‹¹ ê³µê²© ?Ÿìˆ˜)</summary>
    private float _playerAttackSpeed = 1f;
    
    /// <summary>ëª¬ìŠ¤??ê³µê²© ?ë„</summary>
    private float _monsterAttackSpeed = 1f;

    // ========== ì½”ë£¨??==========
    
    private Coroutine _combatLoopCoroutine;

    // ========== MonoBehaviour ?¼ì´?„ì‚¬?´í´ ==========
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // ?˜ì¡´??ì£¼ì…
        InjectDependencies();
    }

    private void OnDestroy()
    {
        StopCombatLoop();
    }

    private void OnEnable()
    {
        // ?˜ì¡´??ì£¼ì… ?•ì¸
        InjectDependencies();
        
        // ?´ë²¤??êµ¬ë…
        _eventBus.On(GameEvents.PLAYER_STAT_CHANGED, OnPlayerStatChanged);
    }

    private void OnDisable()
    {
        // ?´ë²¤???´ì œ
        _eventBus.Off(GameEvents.PLAYER_STAT_CHANGED, OnPlayerStatChanged);
    }

    // ========== ?˜ì´ì¦?ê´€ë¦?==========
    
    /// <summary>
    /// ?„ì¬ ?„íˆ¬ ?˜ì´ì¦?    /// </summary>
    public CombatPhase CurrentPhase => _currentPhase;

    /// <summary>
    /// ?˜ì´ì¦?ë³€ê²?    /// </summary>
    /// <param name="newPhase">???˜ì´ì¦?/param>
    public void ChangePhase(CombatPhase newPhase)
    {
        CombatPhase oldPhase = _currentPhase;
        _currentPhase = newPhase;
        _phaseTimer = 0f;
        
        Debug.Log($"?„íˆ¬ ?˜ì´ì¦?ë³€ê²? {oldPhase} ??{newPhase}");
        
        // ?˜ì´ì¦?ë³€ê²??´ë²¤??ë°œìƒ
        _eventBus.Emit(GameEvents.COMBAT_PHASE_CHANGED);
        
        // ?˜ì´ì¦?ì§„ì… ì²˜ë¦¬
        OnEnterPhase(newPhase);
    }

    private void OnEnterPhase(CombatPhase phase)
    {
        switch (phase)
        {
            case CombatPhase.IDLE:
                // ?€ê¸??íƒœ - ?„ë¬´ê²ƒë„ ????                break;
                
            case CombatPhase.MOVING:
                // ?´ë™ ? ë‹ˆë©”ì´???œì‘
                _phaseTimer = 0f;
                break;
                
            case CombatPhase.ENCOUNTERING:
                // ëª¬ìŠ¤???±ì¥
                SpawnMonster();
                _phaseTimer = 0f;
                break;
                
            case CombatPhase.COMBAT:
                // ?„íˆ¬ ?œì‘
                StartCombatLoop();
                _combatTimer = 0f;
                break;
                
            case CombatPhase.VICTORY:
                // ?¹ë¦¬ ì²˜ë¦¬
                StopCombatLoop();
                ProcessVictory();
                break;
                
            case CombatPhase.DEFEATED:
                // ?¨ë°° ì²˜ë¦¬
                StopCombatLoop();
                ProcessDefeat();
                break;
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        _phaseTimer += deltaTime;
        
        // HP ?¬ìƒ (ëª¨ë“  ?˜ì´ì¦ˆì—???ìš©)
        UpdateHpRegen(deltaTime);
        
        // ?˜ì´ì¦ˆë³„ ?œê°„ ê¸°ë°˜ ?„ì´
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
                if (_phaseTimer >= 2f) // ?¹ë¦¬ ??2ì´??€ê¸?(ëª¬ìŠ¤?°ê? ì£½ëŠ” ê±?ë³????ˆë„ë¡?
                {
                    // ?¤ìŒ ?¤í…Œ?´ì?ë¡??ë™ ì§„í–‰
                    Debug.Log($"VICTORY ?˜ì´ì¦??„ë£Œ, ?¤ìŒ ?¤í…Œ?´ì?ë¡??´ë™");
                    StageSystem.Instance.NextStage();
                }
                break;
                
            case CombatPhase.DEFEATED:
                if (_phaseTimer >= PHASE_DELAY * 2)
                {
                    if (_autoRepeatMode)
                    {
                        // ?ë™ ë°˜ë³µ ëª¨ë“œ - ì¦‰ì‹œ ?¬ì „??                        ChangePhase(CombatPhase.MOVING);
                    }
                    else
                    {
                        // ?˜ë™ ëª¨ë“œ - ?´ì „ ?¤í…Œ?´ì??ì„œ ë¶€??                        ChangePhase(CombatPhase.IDLE);
                    }
                }
                break;
        }
    }

    // ========== ?„íˆ¬ ?œì‘/ì¢…ë£Œ ==========
    
    /// <summary>
    /// ?„íˆ¬ ?œì‘ (IDLE ??MOVING ??ENCOUNTERING ??COMBAT)
    /// </summary>
    public void StartCombat()
    {
        Debug.Log($"[DEBUG] StartCombat ?¸ì¶œ??- ?„ì¬ ?˜ì´ì¦? {_currentPhase}");
        
        if (_currentPhase != CombatPhase.IDLE && _currentPhase != CombatPhase.DEFEATED)
        {
            _logger.Warn($"?„íˆ¬ ?œì‘ ë¶ˆê? - ?„ì¬ ?˜ì´ì¦? {_currentPhase}");
            return;
        }
        
        // ???„íˆ¬ ?°ì´??ì´ˆê¸°??(?´ì „ ?„íˆ¬???”ì—¬ ?°ì´???œê±°)
        var combatPhase = _gameState.CombatPhase;
        Debug.Log($"[DEBUG] StartCombat - ì´ˆê¸°????monsterState.currentHP: {combatPhase.monsterState.currentHP}");
        
        combatPhase.phase = 0;
        combatPhase.timer = 0;
        combatPhase.monsterState = new MonsterData(); // HP=0??ëª¬ìŠ¤??ì´ˆê¸°??        _gameState.CombatPhase = combatPhase;
        
        Debug.Log($"[DEBUG] StartCombat - ì´ˆê¸°????monsterState.currentHP: {_gameState.CombatPhase.monsterState.currentHP}");
        
        // ?Œë ˆ?´ì–´ HP ?Œë³µ (?¤í…Œ?´ì? ?œì‘ ??
        _gameState.Player.currentHP = _gameState.GetTotalHealth();
        
        // ?Œë ˆ?´ì–´ ê³µê²© ?ë„ ?¤ì •
        _playerAttackSpeed = 1f + (_gameState.Player.speed * 0.01f);
        
        // ?„íˆ¬ ?€?´ë¨¸ ì´ˆê¸°??        _combatTimer = 0f;
        _lastAttackTime = 0f;
        
        ChangePhase(CombatPhase.MOVING);
        
        // ?„íˆ¬ ì¡°ìš° ?´ë²¤??        _eventBus.Emit(GameEvents.COMBAT_ENCOUNTER);
        
        _logger.Info($"?„íˆ¬ ?œì‘ - ?¤í…Œ?´ì? {_gameState.Stage.currentStage}");
    }

    /// <summary>
    /// ?ë™ ë°˜ë³µ ëª¨ë“œ ? ê?
    /// </summary>
    /// <param name="enabled">?ë™ ë°˜ë³µ ?œì„±???¬ë?</param>
    public void SetAutoRepeatMode(bool enabled)
    {
        _autoRepeatMode = enabled;
        _logger.Info($"?ë™ ë°˜ë³µ ëª¨ë“œ: {enabled}");
    }

    /// <summary>
    /// ?ë™ ë°˜ë³µ ëª¨ë“œ ?¬ë?
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
            
            // ?Œë ˆ?´ì–´ ê³µê²©
            float playerAttackInterval = 1f / _playerAttackSpeed;
            if (_combatTimer - _lastAttackTime >= playerAttackInterval)
            {
                PlayerAttack();
                _lastAttackTime = _combatTimer;
            }
            
            // ëª¬ìŠ¤??ê³µê²©
            float monsterAttackInterval = 1f / _monsterAttackSpeed;
            if (_combatTimer >= monsterAttackInterval)
            {
                MonsterAttack();
            }
            
            // ?¹íŒ¨ ?ì •
            CheckCombatResult();
        }
    }

    // ========== ê³µê²©/?°ë?ì§€ ==========
    
    /// <summary>
    /// HP ?¬ìƒ ?…ë°?´íŠ¸ (ëª¨ë“  ?˜ì´ì¦ˆì—???¸ì¶œ)
    /// ?•í™•??1ì´ˆë§ˆ????ë²ˆì”© hpRegen ê°’ë§Œ???Œë³µ
    /// </summary>
    private void UpdateHpRegen(float deltaTime)
    {
        // hpRegen ê³„ì‚°: (goldUpgrades["hpRegen"] + statUpgrades["hpRegen"]) * baseValue
        // Web ë²„ì „: hpRegen baseValue = 1
        int hpRegenLevel = 0;
        if (_gameState.Player.goldUpgrades.ContainsKey("hpRegen"))
            hpRegenLevel += _gameState.Player.goldUpgrades["hpRegen"];
        if (_gameState.Player.statUpgrades.ContainsKey("hpRegen"))
            hpRegenLevel += _gameState.Player.statUpgrades["hpRegen"];
        
        // ?¨ìœ¨ ë°°ìœ¨ ê³„ì‚° (Web ë²„ì „ê³??™ì¼: 10?ˆë²¨ë§ˆë‹¤ ì¦ê?)
        float efficiencyMultiplier = 1f;
        if (hpRegenLevel >= 40) efficiencyMultiplier = 3f;
        else if (hpRegenLevel >= 30) efficiencyMultiplier = 2.5f;
        else if (hpRegenLevel >= 20) efficiencyMultiplier = 2f;
        else if (hpRegenLevel >= 10) efficiencyMultiplier = 1.5f;
        
        // hpRegen ê°?= ?„ì ê°?* baseValue(1)
        float hpRegen = 0;
        for (int i = 0; i < hpRegenLevel; i++)
        {
            hpRegen += efficiencyMultiplier;
        }
        
        if (hpRegen <= 0) return;
        
        float maxHp = _gameState.GetTotalHealth();
        var player = _gameState.Player;
        
        // 1ì´??€?´ë¨¸??deltaTime ?„ì  (deltaTime?€ ì´?
        _hpRegenTimer += deltaTime;
        
        // ?•í™•??1ì´ˆë§ˆ???Œë³µ
        if (_hpRegenTimer >= 1f)
        {
            if (player.currentHP < maxHp)
            {
                player.currentHP = Mathf.Min(maxHp, player.currentHP + hpRegen);
                _gameState.Player = player;
                
                _eventBus.Emit(GameEvents.PLAYER_STAT_CHANGED);
            }
            // ?€?´ë¨¸ ë¦¬ì…‹ (?¨ì? ?œê°„?€ ?¤ìŒ ì£¼ê¸°ë¡??´ì›”)
            _hpRegenTimer -= 1f;
        }
    }
    
    /// <summary>
    /// ?Œë ˆ?´ì–´ ê³µê²©
    /// </summary>
    private void PlayerAttack()
    {
        // ë²„í”„ ?•ì¸ (ê³µê²©??2ë°?
        float attackBuff = GetBuffMultiplier("attackDouble");
        
        // ë³´ì„ ?…ê·¸?ˆì´?? ?ë™ ?„íˆ¬ ê°•í™” (?ë™ ë°˜ë³µ ??2%/?ˆë²¨, ìµœë? 100%) - ?´ê¸ˆ ?„ìš”
        // TODO: StageData.autoRepeat?€ PlayerData.gemUpgrades êµ¬í˜„ ???œì„±??        float autoCombatBonus = 1f;
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
        
        // ?°ë?ì§€ ê³„ì‚°
        float damage = CalculateDamage(
            _gameState.GetTotalAttack(),
            _gameState.CombatPhase.monsterState.defense,
            _gameState.Player.critChance,
            _gameState.GetCritDamageMultiplier(),
            attackBuff,
            autoCombatBonus
        );
        
        // ëª¬ìŠ¤??HP ê°ì†Œ
        var monster = _gameState.CombatPhase.monsterState;
        monster.currentHP -= damage;
        var combatPhase = _gameState.CombatPhase;
        combatPhase.monsterState = monster;
        
        Debug.Log($"?Œë ˆ?´ì–´ ê³µê²© - ?°ë?ì§€: {damage:F1}, ëª¬ìŠ¤??HP: {monster.currentHP:F1}/{monster.maxHP:F1}");
        
        // ê³µê²© ? ë‹ˆë©”ì´???¸ë¦¬ê±?(GameRenderer)
        GameRenderer.Instance?.TriggerPlayerAttack();
        GameRenderer.Instance?.TriggerMonsterHit();
        
        // ëª¬ìŠ¤???¬ë§ ?•ì¸
        if (monster.currentHP <= 0)
        {
            monster.currentHP = 0;
            combatPhase.monsterState = monster;
            ChangePhase(CombatPhase.VICTORY);
            return; // ëª¬ìŠ¤?°ê? ì£½ì—ˆ?¼ë©´ ë°˜ë™ ?°ë?ì§€ ?†ìŒ
        }
        
        // ê³µê²© ë°˜ë™ ?°ë?ì§€: ?Œë ˆ?´ì–´ ì²´ë ¥ ?Œëª¨
        // ê³µì‹: (stage * 4) - playerDefense, ìµœì†Œ 1
        ConsumePlayerHP();
    }

    /// <summary>
    /// ë²„í”„ ë°°ìœ¨ ê°€?¸ì˜¤ê¸?    /// </summary>
    /// <param name="buffType">ë²„í”„ ?€??(attackDouble, hpDouble, goldDouble, expDouble)</param>
    /// <returns>ë°°ìœ¨ (?œì„±????2.0, ?„ë‹ˆë©?1.0)</returns>
    private float GetBuffMultiplier(string buffType)
    {
        if (_dailyMissionSystem != null)
        {
            return _dailyMissionSystem.GetBuffMultiplier(buffType);
        }
        return 1.0f;
    }

    /// <summary>
    /// ?Œë ˆ?´ì–´ ì²´ë ¥ ?Œëª¨ (ê³µê²© ë°˜ë™)
    /// Web ë²„ì „ê³??™ì¼??ë¡œì§: ?¤í…Œ?´ì???4 ?°ë?ì§€ - ë°©ì–´?? ìµœì†Œ 1
    /// </summary>
    private void ConsumePlayerHP()
    {
        int stage = _gameState.Stage.currentStage;
        float playerDefense = _gameState.GetTotalDefense();
        
        // ?Œë ˆ?´ì–´ ì²´ë ¥ ?Œëª¨ (?¤í…Œ?´ì? ë¹„ë? ê³ ì • ?˜ì¹˜ - ë°©ì–´??
        // ê³µì‹: (stage * 4) - playerDefense, ìµœì†Œ 1
        float baseRecoil = stage * 4f; // ?¤í…Œ?´ì???4 ?°ë?ì§€
        float recoilDamage = Mathf.Max(1f, baseRecoil - playerDefense);
        
        var player = _gameState.Player;
        player.currentHP = Mathf.Max(0f, player.currentHP - recoilDamage);
        _gameState.Player = player;
        
        Debug.Log($"ê³µê²© ë°˜ë™ ?°ë?ì§€: {recoilDamage:F1}, ?Œë ˆ?´ì–´ HP: {player.currentHP:F1}/{_gameState.GetTotalHealth():F1}");
        
        // ?Œë ˆ?´ì–´ ?¬ë§ ?•ì¸
        if (player.currentHP <= 0)
        {
            player.currentHP = 0;
            _gameState.Player = player;
            ChangePhase(CombatPhase.DEFEATED);
        }
    }

    /// <summary>
    /// ëª¬ìŠ¤??ê³µê²©
    /// </summary>
    private void MonsterAttack()
    {
        // ?Œë ˆ?´ì–´ ì´?ë°©ì–´??        float playerDefense = _gameState.GetTotalDefense();
        
        // ?°ë?ì§€ ê³„ì‚° (ëª¬ìŠ¤?????Œë ˆ?´ì–´)
        float damage = CalculateDamage(
            _gameState.CombatPhase.monsterState.attack,
            playerDefense,
            0f, // ëª¬ìŠ¤??ì¹˜ëª…?•ë¥ 
            GameConfig.MonsterCritDamage // ëª¬ìŠ¤??ì¹˜ëª…?¼í•´
        );
        
        // ?Œë ˆ?´ì–´ HP ê°ì†Œ
        var player = _gameState.Player;
        player.currentHP -= damage;
        _gameState.Player = player;
        
        Debug.Log($"ëª¬ìŠ¤??ê³µê²© - ?°ë?ì§€: {damage:F1}, ?Œë ˆ?´ì–´ HP: {player.currentHP:F1}/{_gameState.GetTotalHealth():F1}");
        
        // ëª¬ìŠ¤??ê³µê²© ? ë‹ˆë©”ì´???¸ë¦¬ê±?        GameRenderer.Instance?.TriggerMonsterAttack();
        GameRenderer.Instance?.TriggerPlayerHit();
        
        // ?Œë ˆ?´ì–´ ?¬ë§ ?•ì¸
        if (player.currentHP <= 0)
        {
            player.currentHP = 0;
            _gameState.Player = player;
            ChangePhase(CombatPhase.DEFEATED);
        }
    }

    /// <summary>
    /// ?°ë?ì§€ ê³„ì‚°
    /// </summary>
    /// <param name="attack">ê³µê²©??/param>
    /// <param name="defense">ë°©ì–´??/param>
    /// <param name="critChance">ì¹˜ëª…?•ë¥  (0-1)</param>
    /// <param name="critDamage">ì¹˜ëª…?¼í•´ ë°°ìœ¨</param>
    /// <param name="buffMultiplier">ë²„í”„ ë°°ìœ¨ (ê¸°ë³¸ 1)</param>
    /// <param name="autoCombatBonus">?ë™ ?„íˆ¬ ë³´ë„ˆ??(ê¸°ë³¸ 1)</param>
    /// <returns>ìµœì¢… ?°ë?ì§€</returns>
    public float CalculateDamage(float attack, float defense, float critChance, float critDamage, 
        float buffMultiplier = 1f, float autoCombatBonus = 1f)
    {
        // ê¸°ë³¸ ?°ë?ì§€ = ê³µê²©??- ë°©ì–´??(ìµœì†Œ 1)
        float baseDamage = Mathf.Max(1f, attack - defense);
        
        // ?°ë?ì§€ ë³€?™í­ (Â±10%)
        float variance = Random.Range(GameConfig.DamageVarianceMin, GameConfig.DamageVarianceMax);
        float damage = baseDamage * variance;
        
        // ì¹˜ëª…?€ ?ì •
        bool isCrit = Random.value < critChance;
        if (isCrit)
        {
            damage *= critDamage;
            Debug.Log("ì¹˜ëª…?€!");
        }
        
        // ë²„í”„ ë°??ë™ ?„íˆ¬ ë³´ë„ˆ???ìš©
        damage *= buffMultiplier * autoCombatBonus;
        
        return Mathf.Round(damage * 10f) / 10f;
    }

    /// <summary>
    /// ?¹íŒ¨ ?ì •
    /// </summary>
    private void CheckCombatResult()
    {
        float monsterHP = _gameState.CombatPhase.monsterState.currentHP;
        float playerHP = _gameState.Player.currentHP;
        
        Debug.Log($"[DEBUG] CheckCombatResult - monsterHP:{monsterHP}, playerHP:{playerHP}");
        
        // ëª¬ìŠ¤???¬ë§
        if (monsterHP <= 0)
        {
            Debug.Log($"[DEBUG] ëª¬ìŠ¤???¬ë§ ê°ì? - VICTORYë¡??„í™˜");
            ChangePhase(CombatPhase.VICTORY);
            return;
        }
        
        // ?Œë ˆ?´ì–´ ?¬ë§
        if (playerHP <= 0)
        {
            Debug.Log($"[DEBUG] ?Œë ˆ?´ì–´ ?¬ë§ ê°ì? - DEFEATEDë¡??„í™˜");
            ChangePhase(CombatPhase.DEFEATED);
        }
    }

    // ========== ëª¬ìŠ¤???œìŠ¤??==========
    
    // ë¶„ë¦¬??ì»´í¬?ŒíŠ¸
    private MonsterFactory _monsterFactory;
    private DropTable _dropTable;
    
    /// <summary>
    /// ëª¬ìŠ¤???ì„± (MonsterFactoryë¡??„ì„)
    /// </summary>
    private void SpawnMonster()
    {
        int stage = _gameState.Stage.currentStage;
        
        Debug.Log($"[DEBUG] SpawnMonster ?œì‘ - ?¤í…Œ?´ì? {stage}");
        Debug.Log($"[DEBUG] ?„ì¬ combatPhase.monsterState.currentHP: {_gameState.CombatPhase.monsterState.currentHP}");
        
        // MonsterFactoryë¥??¬ìš©?˜ì—¬ ëª¬ìŠ¤???ì„±
        if (_monsterFactory == null)
            _monsterFactory = new MonsterFactory();
        
        MonsterData monster = _monsterFactory.CreateMonster(stage);
        
        Debug.Log($"[DEBUG] MonsterFactory ?ì„± - name:{monster.name}, maxHP:{monster.maxHP}, currentHP:{monster.currentHP}");
        
        // ëª¬ìŠ¤??HPë¥?ìµœë? HPë¡?ëª…ì‹œ??ì´ˆê¸°??(ì£½ì? ?íƒœë¡??±ì¥?˜ëŠ” ë²„ê·¸ ë°©ì?)
        monster.currentHP = monster.maxHP;
        
        Debug.Log($"[DEBUG] HP ì´ˆê¸°????- currentHP:{monster.currentHP}");
        
        var combatPhase = _gameState.CombatPhase;
        combatPhase.monsterState = monster;
        _gameState.CombatPhase = combatPhase;
        
        Debug.Log($"[DEBUG] GameState??? ë‹¹ ???•ì¸ - currentHP:{_gameState.CombatPhase.monsterState.currentHP}");
        
        // ëª¬ìŠ¤??ê³µê²© ?ë„ ?¤ì •
        _monsterAttackSpeed = _monsterFactory.GetMonsterAttackSpeed(monster);
        
        _logger.Info($"ëª¬ìŠ¤???±ì¥ - {monster.name} (?¤í…Œ?´ì? {stage}, HP: {monster.currentHP}/{monster.maxHP}, {(monster.grade >= 3 ? "ë³´ìŠ¤" : "?¼ë°˜")})");
    }

    // ========== ?¹ë¦¬/?¨ë°° ì²˜ë¦¬ ==========
    
    /// <summary>
    /// ?¹ë¦¬ ì²˜ë¦¬
    /// </summary>
    private void ProcessVictory()
    {
        // ê²½í—˜ì¹?ì§€ê¸?        long expReward = CalculateExpReward();
        var player = _gameState.Player;
        player.experience += expReward;
        _gameState.Player = player;
        
        // ê³¨ë“œ ?œë¡­
        int goldReward = CalculateGoldDrop();
        player = _gameState.Player;
        player.gold += goldReward;
        _gameState.Player = player;
        
        // ?„ì´???œë¡­
        DropLoot();
        
        // ë³´ì„ ?œë¡­ (0.1% ?•ë¥ ë¡?1ê°? - ?ë™ ë°˜ë³µ ëª¨ë“œ?ì„œ???œë?˜ì? ?ŠìŒ
        if (!_autoRepeatMode)
        {
            RollGemDrop();
        }
        
        // ?µê³„ ?…ë°?´íŠ¸
        var stats = _gameState.Stats;
        stats.totalKills++;
        if (_gameState.CombatPhase.monsterState.grade >= 3)
        {
            stats.totalBossKills++;
        }
        _gameState.Stats = stats;
        
        // ?¤í…Œ?´ì? ?´ë¦¬??(?„ì¬ ?¤í…Œ?´ì?ê°€ ìµœë? ?¤í…Œ?´ì???ê²½ìš°)
        if (_gameState.Stage.currentStage >= _gameState.Stage.maxStage)
        {
            var stage = _gameState.Stage;
            stage.maxStage = stage.currentStage + 1;
            _gameState.Stage = stage;
            _eventBus.Emit(GameEvents.STAGE_RECORD_UPDATED);
        }
        
        // ?´ë¦¬???Œë˜ê·??¤ì •
        if (_gameState.Stage.clearedStages != null && _gameState.Stage.currentStage <= _gameState.Stage.clearedStages.Length)
        {
            var clearedStages = _gameState.Stage.clearedStages;
            clearedStages[_gameState.Stage.currentStage - 1] = true;
            var stage2 = _gameState.Stage;
            stage2.clearedStages = clearedStages;
            _gameState.Stage = stage2;
        }
        
        // ?´ë²¤??ë°œìƒ
        _eventBus.Emit(GameEvents.MONSTER_KILL);
        _eventBus.Emit(GameEvents.COMBAT_VICTORY);
        _eventBus.Emit(GameEvents.GOLD_CHANGED);
        _eventBus.Emit(GameEvents.STATS_CHANGED);
        
        // ë¯¸ì…˜ ì§„í–‰???…ë°?´íŠ¸
        _eventBus.Emit(GameEvents.DAILY_MISSION_PROGRESS);
        
        _logger.Info($"?¹ë¦¬! - ê²½í—˜ì¹?+{expReward}, ê³¨ë“œ +{goldReward}");
    }

    /// <summary>
    /// ?¨ë°° ì²˜ë¦¬
    /// </summary>
    private void ProcessDefeat()
    {
        // ?ë™ ë°˜ë³µ ëª¨ë“œ?¼ë©´ HPë§??Œë³µ
        if (_autoRepeatMode)
        {
            var player = _gameState.Player;
            player.currentHP = _gameState.GetTotalHealth();
            _gameState.Player = player;
            _logger.Info("?¨ë°° - ?ë™ ë°˜ë³µ ëª¨ë“œë¡??¬ì „??);
        }
        else
        {
            // ?˜ë™ ëª¨ë“œ - ?´ì „ ?¤í…Œ?´ì?ë¡??Œì•„ê°€ê¸?            var stage = _gameState.Stage;
            stage.currentStage = Mathf.Max(1, stage.currentStage - 1);
            _gameState.Stage = stage;
            
            var player = _gameState.Player;
            player.currentHP = _gameState.GetTotalHealth();
            _gameState.Player = player;
            
            _logger.Info($"?¨ë°° - ?¤í…Œ?´ì? {_gameState.Stage.currentStage}ë¡??„í‡´");
        }
        
        _eventBus.Emit(GameEvents.COMBAT_DEFEAT);
    }

    /// <summary>
    /// ê²½í—˜ì¹?ë³´ìƒ ê³„ì‚° (DropTableë¡??„ì„)
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
    /// ê³¨ë“œ ?œë¡­??ê³„ì‚° (DropTableë¡??„ì„)
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
    /// ?„ì´???œë¡­ (DropTableë¡??„ì„)
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
        
        // ?¸ë²¤? ë¦¬??ì¶”ê?
        var inventory = _gameState.Inventory;
        inventory.items.Add(item);
        _gameState.Inventory = inventory;
        
        // ë°œê²¬ ?„ì´???±ë¡
        if (!inventory.discoveredItems.Contains(item.id))
        {
            inventory.discoveredItems.Add(item.id);
            var stats = _gameState.Stats;
            stats.totalItemsDiscovered++;
            _gameState.Stats = stats;
            _eventBus.Emit(GameEvents.ITEM_DISCOVERED);
        }
        
        _eventBus.Emit(GameEvents.ITEM_ACQUIRED);
        
        _logger.Info($"?„ì´???œë¡­: {item.name} ({_dropTable.GetGradeName(item.grade)}?±ê¸‰)");
    }

    /// <summary>
    /// ë³´ì„ ?œë¡­ ?•ë¥  ë¡?(0.1% ?•ë¥ ë¡?1ê°?
    /// Web ë²„ì „ê³??™ì¼??ë¡œì§
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
            
            _logger.Info("ë³´ì„ ?œë¡­! ?’ +1");
        }
    }

    // ========== ? í‹¸ë¦¬í‹° ==========
    
    /// <summary>
    /// ?Œë ˆ?´ì–´ ?¤íƒ¯ ë³€ê²???ê³µê²© ?ë„ ?¬ê³„??    /// </summary>
    private void OnPlayerStatChanged()
    {
        _playerAttackSpeed = 1f + (_gameState.Player.speed * 0.01f);
    }

    /// <summary>
    /// ?„ì¬ ?„íˆ¬ ?•ë³´ ê°€?¸ì˜¤ê¸?    /// </summary>
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
/// ?„íˆ¬ ?•ë³´ êµ¬ì¡°ì²?/// </summary>
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
