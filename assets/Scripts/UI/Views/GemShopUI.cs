using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

/// <summary>
/// 보석 상점 UI 전담 클래스 (Web 버전과 동일한 버프 상점)
/// </summary>
public class GemShopUIClass : MonoBehaviour
{
    private IGameState _gameState;
    
    private VisualElement _root;
    private ScrollView _scrollView;
    private VisualElement _shopContainer;
    
    // 헤더 표시 요소
    private Label _gemShopGems;
    
    // 상점 아이템 정의 (Web 버전과 동일)
    private List<ShopItemDefinition> _shopItems;
    
    private void Awake()
    {
        try
        {
            InjectDependencies();
            DefineShopItems();
            // Debug.Log("GemShopUIClass.Awake() - DI 성공");
        }
        catch (System.Exception e)
        {
            // Debug.LogError($"GemShopUIClass.Awake() - DI 실패: {e.Message}");
        }
    }
    
    private void InjectDependencies()
    {
        if (Bootstrap.Container == null) return;
        _gameState = Bootstrap.Container.Resolve<IGameState>();
    }
    
    private void DefineShopItems()
    {
        _shopItems = new List<ShopItemDefinition>
        {
            new ShopItemDefinition
            {
                id = "attack_double",
                name = "공격력 2배",
                description = "30분간 공격력 2배",
                cost = 3,
                duration = 30,
                buffType = "attackDouble",
                icon = "⚔️"
            },
            new ShopItemDefinition
            {
                id = "hp_double",
                name = "체력 2배",
                description = "30분간 최대 체력 2배",
                cost = 3,
                duration = 30,
                buffType = "hpDouble",
                icon = "❤️"
            },
            new ShopItemDefinition
            {
                id = "gold_double",
                name = "골드 2배 드롭",
                description = "30분간 골드 드롭 2배",
                cost = 5,
                duration = 30,
                buffType = "goldDouble",
                icon = "💰"
            },
            new ShopItemDefinition
            {
                id = "exp_double",
                name = "경험치 2배",
                description = "30분간 경험치 2배",
                cost = 5,
                duration = 30,
                buffType = "expDouble",
                icon = "⭐"
            }
        };
    }
    
    public void Initialize(VisualElement root)
    {
        _root = root;
        
        _gemShopGems = _root.Q<Label>("GemShopGems");
        
        _scrollView = _root.Q<ScrollView>("GemShopGrid");
        _shopContainer = _scrollView;
        
        // ScrollView 설정
        if (_scrollView != null)
        {
            _scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        }
        
        UpdateDisplay();
        RefreshShopGrid();
    }
    
    /// <summary>
    /// 디스플레이 업데이트 (보석 표시)
    /// </summary>
    public void UpdateDisplay()
    {
        if (_gemShopGems != null)
        {
            var gems = _gameState?.Player?.gems ?? 0;
            _gemShopGems.text = $"💎 {gems:N0}";
        }
    }
    
    /// <summary>
    /// 상점 그리드 새로고침
    /// </summary>
    public void RefreshShopGrid()
    {
        if (_shopContainer == null || _gameState == null) return;
        
        // 컨테이너 비우기
        _shopContainer.Clear();
        
        foreach (var item in _shopItems)
        {
            var card = CreateShopItem(item);
            _shopContainer.Add(card);
        }
        
        // Debug.Log($"상점 그리드 업데이트: {_shopItems.Count}개 아이템");
    }
    
    /// <summary>
    /// 상점 아이템 카드 생성 (Web 버전과 동일)
    /// </summary>
    private VisualElement CreateShopItem(ShopItemDefinition item)
    {
        var container = new VisualElement();
        container.AddToClassList("shop-card");
        container.style.flexDirection = FlexDirection.Row;
        container.style.alignItems = Align.Center;
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
        
        // 아이콘
        var iconLabel = new Label(item.icon);
        iconLabel.style.fontSize = 40;
        iconLabel.style.marginRight = 15;
        iconLabel.style.minWidth = 60;
        iconLabel.style.color = Color.white; // 이모지 색상 (검은색 방지)
        container.Add(iconLabel);
        
        // 정보 영역
        var infoArea = new VisualElement();
        infoArea.style.flexDirection = FlexDirection.Column;
        infoArea.style.flexGrow = 1;
        
        var nameLabel = new Label(item.name);
        nameLabel.style.fontSize = 26;
        nameLabel.style.color = Color.white;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        infoArea.Add(nameLabel);
        
        var descLabel = new Label(item.description);
        descLabel.style.fontSize = 18;
        descLabel.style.color = new Color(0.69f, 0.69f, 0.69f);
        descLabel.style.marginTop = 3;
        infoArea.Add(descLabel);
        
        // 활성화된 버프 표시
        if (HasActiveBuff(item.buffType))
        {
            var activeLabel = new Label("활성화됨");
            activeLabel.style.fontSize = 16;
            activeLabel.style.color = new Color(0.29f, 0.93f, 0.5f);
            activeLabel.style.marginTop = 3;
            infoArea.Add(activeLabel);
        }
        
        container.Add(infoArea);
        
        // 구매 버튼
        var gems = _gameState.Player.gems;
        var canAfford = gems >= item.cost;
        var hasActiveBuff = HasActiveBuff(item.buffType);
        
        var buyBtn = new Button(() => PurchaseBuff(item));
        buyBtn.text = $"{item.cost} 💎";
        buyBtn.style.fontSize = 32;
        buyBtn.style.paddingLeft = 30;
        buyBtn.style.paddingRight = 30;
        buyBtn.style.paddingTop = 12;
        buyBtn.style.paddingBottom = 12;
        
        if (canAfford && !hasActiveBuff)
        {
            buyBtn.style.backgroundColor = new Color(0.29f, 0.62f, 1);
            buyBtn.style.color = Color.white;
        }
        else
        {
            buyBtn.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            buyBtn.style.color = Color.gray;
            buyBtn.SetEnabled(false);
        }
        
        buyBtn.style.borderTopLeftRadius = 8;
        buyBtn.style.borderTopRightRadius = 8;
        buyBtn.style.borderBottomLeftRadius = 8;
        buyBtn.style.borderBottomRightRadius = 8;
        container.Add(buyBtn);
        
        return container;
    }
    
    /// <summary>
    /// 버프 활성화 확인
    /// </summary>
    private bool HasActiveBuff(string buffType)
    {
        // DailyMissionSystem은 싱글톤으로 직접 접근
        if (DailyMissionSystem.Instance != null)
        {
            return DailyMissionSystem.Instance.HasActiveBuff(buffType);
        }
        return false;
    }
    
    /// <summary>
    /// 버프 구매 (Web 버전과 동일)
    /// </summary>
    private void PurchaseBuff(ShopItemDefinition item)
    {
        if (_gameState.Player.gems < item.cost) return;
        
        _gameState.Player.gems -= item.cost;
        _gameState.Player = _gameState.Player; // 저장 트리거
        
        // 버프 활성화 (DailyMissionSystem 싱글톤 직접 사용)
        if (DailyMissionSystem.Instance != null)
        {
            DailyMissionSystem.Instance.ActivateBuff(item.buffType, item.duration);
        }
        
        // 보석 변경 이벤트 발생
        var eventBus = Bootstrap.Container?.Resolve<IEventBus>() ?? EventBus.Instance;
        eventBus?.Emit(GameEvents.GEM_CHANGED);
        
        UpdateDisplay();
        RefreshShopGrid();
    }
    
    // ==================== 데이터 클래스 ====================
    
    private class ShopItemDefinition
    {
        public string id;
        public string name;
        public string description;
        public int cost; // 보석 비용
        public int duration; // 분 단위
        public string buffType;
        public string icon;
    }
}
