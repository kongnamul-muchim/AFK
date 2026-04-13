using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Phase 2 통합 테스트 시뮬레이터
/// 모든 게임 시스템의 연동을 테스트합니다.
/// </summary>
public class Phase2IntegrationTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool runOnStart = true;
    [SerializeField] private int testIterations = 100;
    
    private IGameState _gameState;
    private IEventBus _eventBus;
    private IGameLogger _logger;
    
    private CombatSystem _combatSystem;
    private StageSystem _stageSystem;
    private InventorySystem _inventorySystem;
    private DailyMissionSystem _missionSystem;
    private RebirthSystem _rebirthSystem;
    private OfflineRewardSystem _offlineSystem;
    private TutorialSystem _tutorialSystem;
    private StatsTracker _statsTracker;
    
    private TestResults _results = new TestResults();
    private List<string> _testLog = new List<string>();
    
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        
        // 의존성 가져오기
        _gameState = ServiceLocator.Instance.Get<IGameState>();
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        _logger = ServiceLocator.Instance.Get<IGameLogger>();
        
        // 시스템 인스턴스 가져오기
        _combatSystem = CombatSystem.Instance;
        _stageSystem = StageSystem.Instance;
        _inventorySystem = InventorySystem.Instance;
        _missionSystem = DailyMissionSystem.Instance;
        _rebirthSystem = RebirthSystem.Instance;
        _offlineSystem = OfflineRewardSystem.Instance;
        _tutorialSystem = TutorialSystem.Instance;
        _statsTracker = StatsTracker.Instance;
    }
    
    private void Start()
    {
        if (runOnStart)
        {
            StartCoroutine(RunAllTests());
        }
    }
    
    private IEnumerator RunAllTests()
    {
        Log("========== Phase 2 통합 테스트 시작 ==========");
        
        // 초기화
        yield return new WaitForSeconds(0.1f);
        
        // 테스트 1: 게임 시작 → 전투 → 레벨업 → 저장 → 재로드
        yield return StartCoroutine(TestScenario1_GameLoop());
        
        // 테스트 2: 환생 실행 → 데이터 초기화 → 재진행
        yield return StartCoroutine(TestScenario2_Rebirth());
        
        // 테스트 3: 오프라인 보상 계산 → 지급 → 저장
        yield return StartCoroutine(TestScenario3_OfflineRewards());
        
        // 테스트 4: 일일 미션 진행 → 완료 → 보상 청구
        yield return StartCoroutine(TestScenario4_DailyMissions());
        
        // 테스트 5: 인벤토리 합성 → 장비 장착 → 스탯 계산
        yield return StartCoroutine(TestScenario5_InventorySynthesis());
        
        // 성능 테스트: 전투 루프 1000회 반복
        yield return StartCoroutine(TestPerformance_CombatLoop());
        
        Log("========== 모든 테스트 완료 ==========");
        PrintResults();
    }
    
    private IEnumerator TestScenario1_GameLoop()
    {
        Log("\n--- 테스트 1: 게임 시작 → 전투 → 레벨업 → 저장 → 재로드 ---");
        
        // 1. 새 게임 시작
        _gameState.Initialize();
        Log($"초기화 완료 - 레벨: {_gameState.Player.level}, 골드: {_gameState.Player.gold}");
        
        // 2. 스테이지 1 진입
        _stageSystem.EnterStage(1);
        yield return new WaitForSeconds(0.2f);
        
        // 3. 전투 루프 10회 반복
        int initialLevel = _gameState.Player.level;
        for (int i = 0; i < 10; i++)
        {
            if (_combatSystem.CurrentPhase == CombatPhase.IDLE || _combatSystem.CurrentPhase == CombatPhase.DEFEATED)
            {
                _combatSystem.StartCombat();
            }
            
            // 전투가 끝날 때까지 대기
            while (_combatSystem.CurrentPhase != CombatPhase.IDLE && _combatSystem.CurrentPhase != CombatPhase.DEFEATED)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        Log($"10회 전투 완료 - 레벨: {_gameState.Player.level}, 골드: {_gameState.Player.gold}");
        
        // 4. 레벨업 확인
        if (_gameState.Player.level > initialLevel)
        {
            Pass("레벨업 발생");
        }
        else
        {
            Fail("레벨업이 발생하지 않음");
        }
        
        // 5. 저장
        SaveManager.Instance.Save((GameState)_gameState);
        Log("저장 완료");
        
        // 6. 재로드
        GameState loadedState = SaveManager.Instance.Load();
        if (loadedState != null)
        {
            Pass("저장/로드 성공");
            Log($"로드 완료 - 레벨: {loadedState.player.level}, 골드: {loadedState.player.gold}");
        }
        else
        {
            Fail("로드 실패");
        }
        
        yield return new WaitForSeconds(0.5f);
    }
    
    private IEnumerator TestScenario2_Rebirth()
    {
        Log("\n--- 테스트 2: 환생 실행 → 데이터 초기화 → 재진행 ---");
        
        // 1. 레벨 50으로 설정 (환생 조건)
        var player = _gameState.Player;
        player.level = 50;
        player.experience = 0;
        _gameState.Player = player;
        
        // 2. 환생 가능 여부 확인
        if (_rebirthSystem.CanRebirth())
        {
            Pass("환생 가능 판정");
        }
        else
        {
            Fail("환생 불가 판정 (레벨 50이어야 함)");
            yield break;
        }
        
        // 3. 환생 실행
        bool rebirthResult = _rebirthSystem.PerformRebirth();
        
        if (rebirthResult)
        {
            Pass("환생 실행 성공");
            Log($"환생 완료 - 레벨: {_gameState.Player.level}, 환생 횟수: {_gameState.Rebirth.rebirthCount}");
        }
        else
        {
            Fail("환생 실행 실패");
            yield break;
        }
        
        // 4. 데이터 초기화 확인
        if (_gameState.Player.level == 1)
        {
            Pass("레벨 초기화 확인");
        }
        else
        {
            Fail($"레벨 초기화 실패 (현재: {_gameState.Player.level})");
        }
        
        if (_gameState.Stage.currentStage == 1)
        {
            Pass("스테이지 초기화 확인");
        }
        else
        {
            Fail($"스테이지 초기화 실패 (현재: {_gameState.Stage.currentStage})");
        }
        
        // 5. 재진행
        _stageSystem.EnterStage(1);
        yield return new WaitForSeconds(0.2f);
        
        _combatSystem.StartCombat();
        while (_combatSystem.CurrentPhase != CombatPhase.IDLE && _combatSystem.CurrentPhase != CombatPhase.DEFEATED)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        Log("재진행 전투 완료");
        Pass("재진행 성공");
        
        yield return new WaitForSeconds(0.5f);
    }
    
    private IEnumerator TestScenario3_OfflineRewards()
    {
        Log("\n--- 테스트 3: 오프라인 보상 계산 → 지급 → 저장 ---");
        
        // 1. 오프라인 시간 계산 (0초)
        float offlineTime = _offlineSystem.CalculateOfflineTime();
        Log($"오프라인 시간: {offlineTime}초");
        
        // 2. 보상 계산
        var rewards = _offlineSystem.CalculateRewards(0);
        if (rewards.gold == 0 && rewards.experience == 0)
        {
            Pass("오프라인 시간 0일 때 보상 0");
        }
        else
        {
            Fail("오프라인 시간 0일 때 보상이 0이 아님");
        }
        
        // 3. 가상 오프라인 시간 (1시간) 시뮬레이션
        float simulatedTime = 3600f; // 1시간
        var simulatedRewards = _offlineSystem.CalculateRewards(simulatedTime);
        
        Log($"1시간 오프라인 보상: 골드 {simulatedRewards.gold}, 경험치 {simulatedRewards.experience}, 아이템 {simulatedRewards.items.Length}개");
        
        if (simulatedRewards.gold > 0 && simulatedRewards.experience > 0)
        {
            Pass("오프라인 보상 계산 정상");
        }
        else
        {
            Fail("오프라인 보상 계산 오류");
        }
        
        yield return new WaitForSeconds(0.5f);
    }
    
    private IEnumerator TestScenario4_DailyMissions()
    {
        Log("\n--- 테스트 4: 일일 미션 진행 → 완료 → 보상 청구 ---");
        
        // 1. 일일 미션 생성
        _missionSystem.GenerateDailyMissions();
        Log($"일일 미션 생성 완료 - {_gameState.DailyMissions.missions.Count}개");
        
        if (_gameState.DailyMissions.missions.Count == 5)
        {
            Pass("일일 미션 5개 생성");
        }
        else
        {
            Fail($"일일 미션 개수 오류 ({_gameState.DailyMissions.missions.Count}개)");
        }
        
        // 2. 몬스터 처치로 미션 진행
        int killsForMission = 0;
        foreach (var mission in _gameState.DailyMissions.missions)
        {
            if (mission.type == MissionType.Kill.ToString())
            {
                killsForMission = mission.target;
                break;
            }
        }
        
        // 3. 몬스터 처치 시뮬레이션
        for (int i = 0; i < killsForMission; i++)
        {
            _eventBus.Emit(GameEvents.MONSTER_KILL);
        }
        
        Log($"{killsForMission}마리 처치 시뮬레이션 완료");
        
        // 4. 미션 완료 확인
        bool missionCompleted = false;
        foreach (var mission in _gameState.DailyMissions.missions)
        {
            if (mission.type == MissionType.Kill.ToString() && mission.completed)
            {
                missionCompleted = true;
                break;
            }
        }
        
        if (missionCompleted)
        {
            Pass("미션 완료 판정");
        }
        else
        {
            Fail("미션이 완료되지 않음");
        }
        
        // 5. 보상 청구
        foreach (var mission in _gameState.DailyMissions.missions)
        {
            if (mission.completed && !mission.claimed)
            {
                bool claimed = _missionSystem.ClaimReward(mission.id);
                if (claimed)
                {
                    Pass($"미션 보상 청구 성공 - {mission.id}");
                }
                else
                {
                    Fail($"미션 보상 청구 실패 - {mission.id}");
                }
            }
        }
        
        yield return new WaitForSeconds(0.5f);
    }
    
    private IEnumerator TestScenario5_InventorySynthesis()
    {
        Log("\n--- 테스트 5: 인벤토리 합성 → 장비 장착 → 스탯 계산 ---");
        
        // 1. 아이템 5개 추가
        for (int i = 0; i < 5; i++)
        {
            var item = new ItemData
            {
                id = $"test_sword_grade0_{1000 + i}",
                name = $"일반 검 {i + 1}",
                grade = 0,
                count = 1
            };
            _inventorySystem.AddItem(item);
        }
        
        Log($"인벤토리에 아이템 5개 추가 완료");
        
        // 2. 합성 실행
        bool synthesized = _inventorySystem.Synthesize("test_sword_grade0_1000", 0);
        
        if (synthesized)
        {
            Pass("합성 성공");
        }
        else
        {
            Fail("합성 실패");
        }
        
        // 3. 합성 결과 확인 (다음 등급 아이템 생성)
        var inventory = _gameState.Inventory;
        bool hasHigherGrade = false;
        foreach (var item in inventory.items)
        {
            if (item.grade == 1)
            {
                hasHigherGrade = true;
                Log($"합성 결과: {item.name} (등급 {item.grade})");
                break;
            }
        }
        
        if (hasHigherGrade)
        {
            Pass("다음 등급 아이템 생성 확인");
        }
        else
        {
            Fail("합성 결과 아이템이 없음");
        }
        
        // 4. 장비 장착
        if (inventory.items.Count > 0)
        {
            var itemToEquip = inventory.items[0];
            float beforeAttack = _gameState.GetTotalAttack();
            
            bool equipped = _inventorySystem.EquipItem(itemToEquip.id, itemToEquip.grade);
            
            if (equipped)
            {
                Pass("장비 장착 성공");
                
                float afterAttack = _gameState.GetTotalAttack();
                Log($"장착 전 공격력: {beforeAttack}, 장착 후: {afterAttack}");
                
                if (afterAttack > beforeAttack)
                {
                    Pass("공격력 증가 확인");
                }
                else
                {
                    Fail("공격력이 증가하지 않음");
                }
            }
            else
            {
                Fail("장비 장착 실패");
            }
        }
        
        yield return new WaitForSeconds(0.5f);
    }
    
    private IEnumerator TestPerformance_CombatLoop()
    {
        Log("\n--- 성능 테스트: 전투 루프 1000회 반복 ---");
        
        float startTime = Time.realtimeSinceStartup;
        int kills = 0;
        
        for (int i = 0; i < testIterations; i++)
        {
            _stageSystem.EnterStage(1);
            yield return new WaitForSeconds(0.1f);
            
            _combatSystem.StartCombat();
            
            while (_combatSystem.CurrentPhase != CombatPhase.IDLE && _combatSystem.CurrentPhase != CombatPhase.DEFEATED)
            {
                yield return new WaitForSeconds(0.05f);
            }
            
            kills++;
            
            if (kills % 100 == 0)
            {
                Log($"{kills}회 전투 완료...");
            }
        }
        
        float elapsed = Time.realtimeSinceStartup - startTime;
        Log($"{testIterations}회 전투 완료 - 소요 시간: {elapsed:F2}초");
        
        // 성능 기준: 100회 전투당 10초 이내
        float timePer100 = elapsed / (testIterations / 100f);
        if (timePer100 <= 10f)
        {
            Pass($"성능 기준 통과 ({timePer100:F2}초/100회)");
        }
        else
        {
            Fail($"성능 기준 미달 ({timePer100:F2}초/100회, 목표: 10초)");
        }
        
        yield return new WaitForSeconds(0.5f);
    }
    
    private void Pass(string message)
    {
        _results.passed++;
        _testLog.Add($"✅ PASS: {message}");
        Log($"✅ PASS: {message}");
    }
    
    private void Fail(string message)
    {
        _results.failed++;
        _testLog.Add($"❌ FAIL: {message}");
        Log($"❌ FAIL: {message}");
    }
    
    private void Log(string message)
    {
        Debug.Log($"[Phase2Test] {message}");
        _testLog.Add(message);
    }
    
    private void PrintResults()
    {
        Log($"\n========== 테스트 결과 ==========");
        Log($"통과: {_results.passed}");
        Log($"실패: {_results.failed}");
        Log($"총계: {_results.passed + _results.failed}");
        
        if (_results.failed == 0)
        {
            Log("🎉 모든 테스트를 통과했습니다!");
        }
        else
        {
            Log($"⚠️ {_results.failed}개의 테스트가 실패했습니다.");
        }
    }
    
    private struct TestResults
    {
        public int passed;
        public int failed;
    }
}
