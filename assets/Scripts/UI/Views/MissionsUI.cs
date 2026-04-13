using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// 미션 UI 전담 클래스 (Web 버전과 동일한 카드 리스트 레이아웃)
/// </summary>
public class MissionsUIClass : MonoBehaviour
{
    private IGameState _gameState;
    
    private string _currentTab = "daily";
    
    private VisualElement _root;
    private ScrollView _scrollView;
    private VisualElement _missionContainer;
    
    // 탭 버튼들
    private Button _missionsTabDaily;
    private Button _missionsTabWeekly;
    
    // 헤더 표시 요소들
    private Label _missionsGems;
    private Label _missionsResetTimer;
    
    private System.Timers.Timer _updateTimer;
    
    private void Awake()
    {
        try
        {
            InjectDependencies();
            Debug.Log("MissionUIClass.Awake() - DI 성공");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MissionUIClass.Awake() - DI 실패: {e.Message}");
        }
    }
    
    private void InjectDependencies()
    {
        var serviceLocator = ServiceLocator.Instance;
        _gameState = serviceLocator.Get<IGameState>();
    }
    
    public void Initialize(VisualElement root)
    {
        _root = root;
        
        _missionsTabDaily = _root.Q<Button>("MissionsTabDaily");
        _missionsTabWeekly = _root.Q<Button>("MissionsTabWeekly");
        
        _scrollView = _root.Q<ScrollView>("MissionsGrid");
        _missionContainer = _scrollView;
        
        // ScrollView 설정
        if (_scrollView != null)
        {
            _scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        }
        
        // 헤더 표시 요소들
        _missionsGems = _root.Q<Label>("MissionsGems");
        _missionsResetTimer = _root.Q<Label>("MissionsResetTimer");
        
        SetupTabs();
        StartTimer();
    }
    
    private void SetupTabs()
    {
        if (_missionsTabDaily != null)
            _missionsTabDaily.clicked += () => OnTabClicked("daily", _missionsTabDaily);
        if (_missionsTabWeekly != null)
            _missionsTabWeekly.clicked += () => OnTabClicked("weekly", _missionsTabWeekly);
    }
    
    private void OnTabClicked(string tabType, Button clickedTab)
    {
        _currentTab = tabType;
        ResetTabButtons();
        if (clickedTab != null)
            clickedTab.AddToClassList("active");
        UpdateDisplay();
        RefreshMissionsGrid();
    }
    
    private void ResetTabButtons()
    {
        if (_missionsTabDaily != null) _missionsTabDaily.RemoveFromClassList("active");
        if (_missionsTabWeekly != null) _missionsTabWeekly.RemoveFromClassList("active");
    }
    
    /// <summary>
    /// 타이머 시작 (1초마다 업데이트)
    /// </summary>
    private void StartTimer()
    {
        UpdateTimer();
        _updateTimer = new System.Timers.Timer(1000);
        _updateTimer.Elapsed += (s, e) => {
            if (_root != null && _root.panel != null)
            {
                _root.schedule.Execute(() => UpdateTimer());
            }
        };
        _updateTimer.AutoReset = true;
        _updateTimer.Enabled = true;
    }
    
    /// <summary>
    /// 타이머 업데이트 (갱신까지 남은 시간 표시)
    /// </summary>
    private void UpdateTimer()
    {
        if (_missionsResetTimer == null) return;
        
        TimeSpan timeUntilReset;
        string tabText;
        
        if (_currentTab == "weekly")
        {
            // 주간 미션: 다음 주 월요일 0시까지
            var now = DateTime.Now;
            var daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0 && now.Hour >= 0) daysUntilMonday = 7;
            var nextReset = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(daysUntilMonday);
            timeUntilReset = nextReset - now;
            tabText = "주간 갱신";
        }
        else
        {
            // 일일 미션: 다음 날 0시까지
            var now = DateTime.Now;
            var nextReset = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(1);
            timeUntilReset = nextReset - now;
            tabText = "일일 갱신";
        }
        
        _missionsResetTimer.text = $"{tabText}까지: {timeUntilReset.Hours:D2}:{timeUntilReset.Minutes:D2}:{timeUntilReset.Seconds:D2}";
    }
    
    /// <summary>
    /// 디스플레이 업데이트 (보석, 타이머 등)
    /// </summary>
    public void UpdateDisplay()
    {
        if (_missionsGems != null)
        {
            var gems = _gameState?.Player?.gems ?? 0;
            _missionsGems.text = $"💎 {gems:N0}";
        }
    }
    
    /// <summary>
    /// 미션 그리드 새로고침
    /// </summary>
    public void RefreshMissionsGrid()
    {
        if (_missionContainer == null || _gameState == null) return;
        
        // 컨테이너 비우기
        _missionContainer.Clear();
        
        var missions = GetMissions();
        
        if (missions == null || missions.Count == 0)
        {
            var noMissionContainer = new VisualElement();
            noMissionContainer.style.width = Length.Percent(100);
            noMissionContainer.style.alignItems = Align.Center;
            noMissionContainer.style.justifyContent = Justify.Center;
            noMissionContainer.style.marginTop = 30;
            
            var noMissionLabel = new Label("미션이 없습니다.");
            noMissionLabel.style.fontSize = 28;
            noMissionLabel.style.color = new Color(0.4f, 0.4f, 0.4f);
            noMissionContainer.Add(noMissionLabel);
            
            _missionContainer.Add(noMissionContainer);
            return;
        }
        
        foreach (var mission in missions)
        {
            var item = CreateMissionCard(mission);
            _missionContainer.Add(item);
        }
        
        Debug.Log($"미션 그리드 업데이트: {missions.Count}개 ({_currentTab})");
    }
    
    /// <summary>
    /// 현재 탭의 미션 목록 가져오기
    /// </summary>
    private List<MissionData> GetMissions()
    {
        var missions = new List<MissionData>();
        
        if (_currentTab == "daily")
        {
            // 일일 미션 (하드코딩 - 실제 시스템 연동 시 변경)
            missions.Add(new MissionData
            {
                id = "daily_1",
                name = "몬스터 10마리 처치",
                description = "스테이지에서 몬스터를 10마리 처치하세요",
                progress = _gameState.Stats.totalKills,
                target = 10,
                completed = _gameState.Stats.totalKills >= 10,
                claimed = false,
                reward = new MissionReward { statPoints = 5, gems = 2 }
            });
            
            missions.Add(new MissionData
            {
                id = "daily_2",
                name = "골드 1000 획득",
                description = "골드를 1000개 획득하세요",
                progress = 0, // TODO: 총 획득 골드 추적
                target = 1000,
                completed = false,
                claimed = false,
                reward = new MissionReward { statPoints = 3, gems = 1 }
            });
            
            missions.Add(new MissionData
            {
                id = "daily_3",
                name = "스테이지 5클리어",
                description = "스테이지를 5개 클리어하세요",
                progress = _gameState.Stage.currentStage - 1,
                target = 5,
                completed = (_gameState.Stage.currentStage - 1) >= 5,
                claimed = false,
                reward = new MissionReward { statPoints = 10, gems = 5 }
            });
            
            missions.Add(new MissionData
            {
                id = "daily_4",
                name = "인벤토리 확장",
                description = "인벤토리 슬롯을 1개 확장하세요",
                progress = 0,
                target = 1,
                completed = false,
                claimed = false,
                reward = new MissionReward { statPoints = 2, gems = 3 }
            });
        }
        else
        {
            // 주간 미션
            missions.Add(new MissionData
            {
                id = "weekly_1",
                name = "몬스터 100마리 처치",
                description = "일주일 동안 몬스터를 100마리 처치하세요",
                progress = _gameState.Stats.totalKills,
                target = 100,
                completed = _gameState.Stats.totalKills >= 100,
                claimed = false,
                reward = new MissionReward { statPoints = 50, gems = 20 }
            });
            
            missions.Add(new MissionData
            {
                id = "weekly_2",
                name = "보스 5마리 처치",
                description = "일주일 동안 보스를 5마리 처치하세요",
                progress = _gameState.Stats.totalBossKills,
                target = 5,
                completed = _gameState.Stats.totalBossKills >= 5,
                claimed = false,
                reward = new MissionReward { statPoints = 30, gems = 15 }
            });
            
            missions.Add(new MissionData
            {
                id = "weekly_3",
                name = "레벨 10업",
                description = "일주일 동안 레벨을 10개 올리세요",
                progress = _gameState.Player.level - 1,
                target = 10,
                completed = (_gameState.Player.level - 1) >= 10,
                claimed = false,
                reward = new MissionReward { statPoints = 20, gems = 10 }
            });
        }
        
        return missions;
    }
    
    /// <summary>
    /// 미션 카드 생성
    /// </summary>
    private VisualElement CreateMissionCard(MissionData mission)
    {
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        container.style.paddingLeft = 15;
        container.style.paddingRight = 15;
        container.style.paddingTop = 15;
        container.style.paddingBottom = 15;
        container.style.backgroundColor = new Color(0.14f, 0.14f, 0.26f);
        container.style.borderTopLeftRadius = 12;
        container.style.borderTopRightRadius = 12;
        container.style.borderBottomLeftRadius = 12;
        container.style.borderBottomRightRadius = 12;
        container.style.marginBottom = 10;
        
        if (mission.completed && !mission.claimed)
        {
            container.style.borderLeftWidth = 3;
            container.style.borderLeftColor = new Color(0.29f, 0.93f, 0.5f);
        }
        else if (mission.claimed)
        {
            container.style.opacity = 0.6f;
        }
        
        // 상단 행: 미션 이름 + 진행도 텍스트
        var headerRow = new VisualElement();
        headerRow.style.flexDirection = FlexDirection.Row;
        headerRow.style.justifyContent = Justify.SpaceBetween;
        headerRow.style.alignItems = Align.Center;
        
        var nameLabel = new Label(mission.name);
        nameLabel.style.fontSize = 26;
        nameLabel.style.color = Color.white;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLabel.style.flexGrow = 1;
        headerRow.Add(nameLabel);
        
        var progressLabel = new Label($"{mission.progress} / {mission.target}");
        progressLabel.style.fontSize = 20;
        progressLabel.style.color = mission.completed ? new Color(0.29f, 0.93f, 0.5f) : new Color(0.69f, 0.69f, 0.69f);
        headerRow.Add(progressLabel);
        
        container.Add(headerRow);
        
        // 설명
        var descLabel = new Label(mission.description);
        descLabel.style.fontSize = 18;
        descLabel.style.color = new Color(0.53f, 0.53f, 0.53f);
        descLabel.style.marginTop = 5;
        container.Add(descLabel);
        
        // 진행바
        var progressPercent = Math.Min(100, (mission.progress * 100.0f / mission.target));
        
        var progressBarBg = new VisualElement();
        progressBarBg.style.width = Length.Percent(100);
        progressBarBg.style.height = 8;
        progressBarBg.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        progressBarBg.style.borderTopLeftRadius = 4;
        progressBarBg.style.borderTopRightRadius = 4;
        progressBarBg.style.borderBottomLeftRadius = 4;
        progressBarBg.style.borderBottomRightRadius = 4;
        progressBarBg.style.marginTop = 10;
        
        var progressBarFill = new VisualElement();
        progressBarFill.style.width = Length.Percent(progressPercent);
        progressBarFill.style.height = 8;
        progressBarFill.style.backgroundColor = mission.completed ? 
            new Color(0.29f, 0.93f, 0.5f) : new Color(0.29f, 0.62f, 1);
        progressBarFill.style.borderTopLeftRadius = 4;
        progressBarFill.style.borderTopRightRadius = 4;
        progressBarFill.style.borderBottomLeftRadius = 4;
        progressBarFill.style.borderBottomRightRadius = 4;
        progressBarBg.Add(progressBarFill);
        
        container.Add(progressBarBg);
        
        // 하단 행: 보상 + 청구 버튼
        var bottomRow = new VisualElement();
        bottomRow.style.flexDirection = FlexDirection.Row;
        bottomRow.style.justifyContent = Justify.SpaceBetween;
        bottomRow.style.alignItems = Align.Center;
        bottomRow.style.marginTop = 10;
        
        var rewardText = "";
        if (mission.reward.statPoints > 0) rewardText += $"⭐{mission.reward.statPoints}pt";
        if (mission.reward.gems > 0) rewardText += (rewardText.Length > 0 ? " " : "") + $"💎{mission.reward.gems}";
        
        var rewardLabel = new Label($"보상: {rewardText}");
        rewardLabel.style.fontSize = 20;
        rewardLabel.style.color = new Color(1, 0.84f, 0);
        bottomRow.Add(rewardLabel);
        
        var claimBtn = new Button(() => ClaimReward(mission.id));
        if (mission.claimed)
        {
            claimBtn.text = "완료";
            claimBtn.style.backgroundColor = new Color(0.29f, 0.93f, 0.5f);
            claimBtn.style.color = Color.black;
        }
        else if (mission.completed)
        {
            claimBtn.text = "보상 청구";
            claimBtn.style.backgroundColor = new Color(1, 0.84f, 0);
            claimBtn.style.color = Color.black;
        }
        else
        {
            claimBtn.text = "보상 청구";
            claimBtn.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            claimBtn.style.color = Color.gray;
            claimBtn.SetEnabled(false);
        }
        
        claimBtn.style.fontSize = 20;
        claimBtn.style.paddingLeft = 15;
        claimBtn.style.paddingRight = 15;
        claimBtn.style.paddingTop = 8;
        claimBtn.style.paddingBottom = 8;
        claimBtn.style.borderTopLeftRadius = 8;
        claimBtn.style.borderTopRightRadius = 8;
        claimBtn.style.borderBottomLeftRadius = 8;
        claimBtn.style.borderBottomRightRadius = 8;
        bottomRow.Add(claimBtn);
        
        container.Add(bottomRow);
        
        return container;
    }
    
    /// <summary>
    /// 미션 보상 청구
    /// </summary>
    private void ClaimReward(string missionId)
    {
        var missions = GetMissions();
        var mission = missions.Find(m => m.id == missionId);
        
        if (mission == null || !mission.completed || mission.claimed) return;
        
        // 보상 지급
        if (_gameState.Player != null)
        {
            _gameState.Player.statPoints += mission.reward.statPoints;
            _gameState.Player.gems += mission.reward.gems;
        }
        
        mission.claimed = true;
        
        Debug.Log($"미션 보상 청구: {mission.name} - ⭐{mission.reward.statPoints}pt, 💎{mission.reward.gems}");
        
        UpdateDisplay();
        RefreshMissionsGrid();
    }
    
    private void OnDestroy()
    {
        if (_updateTimer != null)
        {
            _updateTimer.Stop();
            _updateTimer.Dispose();
        }
    }
    
    // ==================== 데이터 클래스 ====================
    
    private class MissionData
    {
        public string id;
        public string name;
        public string description;
        public int progress;
        public int target;
        public bool completed;
        public bool claimed;
        public MissionReward reward;
    }
    
    private class MissionReward
    {
        public int statPoints;
        public int gems;
    }
}
