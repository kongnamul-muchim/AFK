using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// 모달 창 관리 전담 클래스
/// UIManager에서 모달 관련 로직을 분리 (SRP 준수)
/// </summary>
public class ModalManager : MonoBehaviour
{
    public static ModalManager Instance { get; private set; }
    
    private VisualElement _root;
    private VisualElement _currentModal;
    
    // 모달 캐시
    private Dictionary<string, VisualElement> _modalCache = new Dictionary<string, VisualElement>();
    
    // 모달 열림/닫힘 이벤트
    public System.Action<string> OnModalOpened;
    public System.Action<string> OnModalClosed;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    /// <summary>
    /// 모달 시스템 초기화
    /// </summary>
    public void Initialize(VisualElement root)
    {
        _root = root;
        SetupModalCloseButtons();
    }
    
    /// <summary>
    /// 모달 표시
    /// </summary>
    public void ShowModal(VisualElement modal, string modalName = null)
    {
        HideAllModals();
        
        if (modal != null)
        {
            modal.style.display = DisplayStyle.Flex;
            _currentModal = modal;
            
            if (!string.IsNullOrEmpty(modalName))
                OnModalOpened?.Invoke(modalName);
        }
    }
    
    /// <summary>
    /// 모달 숨기기
    /// </summary>
    public void HideModal(VisualElement modal, string modalName = null)
    {
        if (modal != null)
        {
            modal.style.display = DisplayStyle.None;
            
            if (_currentModal == modal)
                _currentModal = null;
            
            if (!string.IsNullOrEmpty(modalName))
                OnModalClosed?.Invoke(modalName);
        }
    }
    
    /// <summary>
    /// 모든 모달 숨기기
    /// </summary>
    public void HideAllModals(params VisualElement[] excludeModals)
    {
        var allModals = new[]
        {
            _root.Q<VisualElement>("InventoryModal"),
            _root.Q<VisualElement>("SettingsModal"),
            _root.Q<VisualElement>("UpgradeModal"),
            _root.Q<VisualElement>("DailyMissionsModal"),
            _root.Q<VisualElement>("GemShopModal"),
            _root.Q<VisualElement>("OfflineRewardModal"),
            _root.Q<VisualElement>("StatisticsModal"),
            _root.Q<VisualElement>("TutorialOverlay")
        };
        
        foreach (var modal in allModals)
        {
            if (modal != null && !IsExcluded(modal, excludeModals))
            {
                modal.style.display = DisplayStyle.None;
            }
        }
        
        _currentModal = null;
    }
    
    private bool IsExcluded(VisualElement modal, VisualElement[] excludeModals)
    {
        if (excludeModals == null) return false;
        foreach (var excluded in excludeModals)
        {
            if (modal == excluded) return true;
        }
        return false;
    }
    
    /// <summary>
    /// 모달 닫기 버튼 설정
    /// </summary>
    private void SetupModalCloseButtons()
    {
        SetupCloseButton("CloseInventoryBtn", "InventoryModal");
        SetupCloseButton("CloseSettingsBtn", "SettingsModal");
        SetupCloseButton("CloseUpgradeBtn", "UpgradeModal");
        SetupCloseButton("CloseMissionsBtn", "DailyMissionsModal");
        SetupCloseButton("CloseGemShopBtn", "GemShopModal");
        SetupCloseButton("CloseOfflineBtn", "OfflineRewardModal");
        SetupCloseButton("CloseStatisticsBtn", "StatisticsModal");
    }
    
    private void SetupCloseButton(string buttonName, string modalName)
    {
        var closeBtn = _root.Q<Button>(buttonName);
        if (closeBtn != null)
        {
            var modal = _root.Q<VisualElement>(modalName);
            closeBtn.clicked += () => HideModal(modal, modalName);
        }
    }
    
    /// <summary>
    /// 튜토리얼 표시
    /// </summary>
    public void ShowTutorial()
    {
        var tutorialOverlay = _root.Q<VisualElement>("TutorialOverlay");
        if (tutorialOverlay != null)
            tutorialOverlay.style.display = DisplayStyle.Flex;
    }
    
    /// <summary>
    /// 튜토리얼 숨기기
    /// </summary>
    public void HideTutorial()
    {
        var tutorialOverlay = _root.Q<VisualElement>("TutorialOverlay");
        if (tutorialOverlay != null)
            tutorialOverlay.style.display = DisplayStyle.None;
    }
    
    /// <summary>
    /// 튜토리얼 메시지 업데이트
    /// </summary>
    public void UpdateTutorialMessage(string message)
    {
        var tutorialMessage = _root.Q<Label>("TutorialMessage");
        if (tutorialMessage != null)
            tutorialMessage.text = message;
    }
}
