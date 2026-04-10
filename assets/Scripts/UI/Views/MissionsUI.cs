using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// 미션 UI 전담 클래스
/// UIManager에서 미션 관련 로직을 분리 (SRP 준수)
/// </summary>
public class MissionsUIClass : MonoBehaviour
{
    [SerializeField] private VisualElement _missionsContainer;
    
    private IGameState _gameState;
    private ILogger _logger;
    
    private List<MissionItemData> _missionItemList = new List<MissionItemData>();
    private string _currentTab = "daily";
    
    private VisualElement _root;
    private ListView _listView;
    
    // 탭 버튼들
    private Button _missionsTabDaily;
    private Button _missionsTabWeekly;
    
    private void Awake()
    {
        InjectDependencies();
    }
    
    private void InjectDependencies()
    {
        var serviceLocator = ServiceLocator.Instance;
        _gameState = serviceLocator.Get<IGameState>();
        _logger = serviceLocator.Get<ILogger>();
    }
    
    public void Initialize(VisualElement root)
    {
        _root = root;
        
        _missionsTabDaily = _root.Q<Button>("MissionsTabDaily");
        _missionsTabWeekly = _root.Q<Button>("MissionsTabWeekly");
        
        _missionsContainer = _root.Q<VisualElement>("MissionsGrid");
        
        SetupTabs();
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
        RefreshMissionsGrid();
    }
    
    private void ResetTabButtons()
    {
        if (_missionsTabDaily != null) _missionsTabDaily.RemoveFromClassList("active");
        if (_missionsTabWeekly != null) _missionsTabWeekly.RemoveFromClassList("active");
    }
    
    public void RefreshMissionsGrid()
    {
        if (_missionsContainer == null || _gameState == null) return;
        
        var listView = _missionsContainer as ListView;
        if (listView == null) return;
        
        _missionItemList.Clear();
        
        var missionData = _gameState.DailyMissions;
        List<MissionData> missions = _currentTab == "daily" ? missionData.missions : missionData.weeklyMissions;
        
        foreach (var mission in missions)
        {
            string reward = string.IsNullOrEmpty(mission.reward) ? GetMissionReward(mission) : mission.reward;
            _missionItemList.Add(new MissionItemData
            {
                name = GetMissionName(mission),
                progress = mission.progress,
                target = mission.target,
                reward = reward
            });
        }
        
        listView.itemsSource = null;
        listView.makeItem = null;
        listView.bindItem = null;
        
        listView.itemsSource = _missionItemList;
        listView.makeItem = MakeMissionItem;
        listView.bindItem = BindMissionItem;
        listView.fixedItemHeight = 150;
        listView.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
        listView.showBoundCollectionSize = false;
        listView.reorderable = false;
        
        listView.Rebuild();
        
        _logger?.Debug($"미션 리스트 업데이트: {_missionItemList.Count}개 미션 ({_currentTab})");
    }
    
    private string GetMissionName(MissionData mission)
    {
        switch (mission.type)
        {
            case "kill":
                return $"몬스터 {mission.target}마리 처치";
            case "clearStage":
                return $"스테이지 {mission.target} 클리어";
            case "collectGold":
                return $"골드 {mission.target:N0} 획득";
            case "synthesize":
                return $"아이템 {mission.target}개 합성";
            case "rebirth":
                return $"환생 {mission.target}회";
            default:
                return !string.IsNullOrEmpty(mission.id) ? mission.id : "미션";
        }
    }
    
    private string GetMissionReward(MissionData mission)
    {
        switch (mission.type)
        {
            case "kill":
                return $"골드 {mission.target * 20:N0}";
            case "clearStage":
                return $"보석 {Mathf.CeilToInt(mission.target * 0.5f)}";
            case "collectGold":
                return $"보석 {Mathf.CeilToInt(mission.target / 10000f)}";
            case "synthesize":
                return $"보석 {mission.target}";
            case "rebirth":
                return $"전설 등급 아이템";
            default:
                return "골드 1000";
        }
    }
    
    private VisualElement MakeMissionItem()
    {
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        container.style.paddingLeft = 15;
        container.style.paddingRight = 15;
        container.style.paddingTop = 15;
        container.style.paddingBottom = 15;
        container.style.backgroundColor = new StyleColor(new Color(0.14f, 0.14f, 0.26f));
        container.style.borderTopLeftRadius = 12;
        container.style.borderTopRightRadius = 12;
        container.style.borderBottomLeftRadius = 12;
        container.style.borderBottomRightRadius = 12;
        container.style.marginBottom = 10;
        
        var nameLabel = new Label();
        nameLabel.style.fontSize = 26;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLabel.style.color = new StyleColor(Color.white);
        container.Add(nameLabel);
        
        var progressLabel = new Label();
        progressLabel.style.fontSize = 20;
        progressLabel.style.color = new StyleColor(Color.gray);
        container.Add(progressLabel);
        
        var progressBarBg = new VisualElement();
        progressBarBg.style.height = 20;
        progressBarBg.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.3f));
        progressBarBg.style.borderTopLeftRadius = 10;
        progressBarBg.style.borderTopRightRadius = 10;
        progressBarBg.style.borderBottomLeftRadius = 10;
        progressBarBg.style.borderBottomRightRadius = 10;
        progressBarBg.style.marginTop = 8;
        
        var progressBarFill = new VisualElement();
        progressBarFill.style.height = 20;
        progressBarFill.style.backgroundColor = new StyleColor(new Color(0.29f, 0.62f, 1f));
        progressBarFill.style.borderTopLeftRadius = 10;
        progressBarFill.style.borderTopRightRadius = 10;
        progressBarFill.style.borderBottomLeftRadius = 10;
        progressBarFill.style.borderBottomRightRadius = 10;
        progressBarFill.style.width = Length.Percent(50);
        progressBarBg.Add(progressBarFill);
        container.Add(progressBarBg);
        
        var rewardLabel = new Label();
        rewardLabel.style.fontSize = 22;
        rewardLabel.style.color = new StyleColor(Color.yellow);
        rewardLabel.style.marginTop = 8;
        container.Add(rewardLabel);
        
        var claimBtn = new Button();
        claimBtn.text = "보상 받기";
        claimBtn.style.fontSize = 20;
        claimBtn.style.marginTop = 8;
        container.Add(claimBtn);
        
        return container;
    }
    
    private void BindMissionItem(VisualElement element, int index)
    {
        if (index < 0 || index >= _missionItemList.Count) return;
        
        var item = _missionItemList[index];
        
        if (element.childCount >= 5)
        {
            var nameLabel = element[0] as Label;
            var progressLabel = element[1] as Label;
            var progressBarBg = element[2] as VisualElement;
            var rewardLabel = element[3] as Label;
            var claimBtn = element[4] as Button;
            
            if (nameLabel != null) nameLabel.text = item.name;
            if (progressLabel != null) progressLabel.text = $"{item.progress}/{item.target}";
            if (progressBarBg != null)
            {
                var progressBarFill = progressBarBg.childCount > 0 ? progressBarBg[0] as VisualElement : null;
                if (progressBarFill != null)
                {
                    float percent = item.target > 0 ? (float)item.progress / item.target * 100 : 0;
                    progressBarFill.style.width = Length.Percent(Mathf.Min(percent, 100));
                }
            }
            if (rewardLabel != null) rewardLabel.text = $"보상: {item.reward}";
            if (claimBtn != null)
            {
                bool isCompleted = item.progress >= item.target;
                claimBtn.SetEnabled(isCompleted);
                claimBtn.clicked += () => OnMissionClaim(item.name);
            }
        }
    }
    
    private void OnMissionClaim(string missionName)
    {
        _logger?.Debug($"미션 보상 청구: {missionName}");
        // 실제 보상 청구 로직 구현
    }
    
    private struct MissionItemData
    {
        public string name;
        public int progress;
        public int target;
        public string reward;
    }
}
