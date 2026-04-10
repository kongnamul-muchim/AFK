using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 아이템 툴팁 관리 전담 클래스
/// UIManager에서 툴팁 관련 로직을 분리 (SRP 준수)
/// </summary>
public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }
    
    private VisualElement _root;
    private VisualElement _itemTooltip;
    
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
    /// 툴팁 시스템 초기화
    /// </summary>
    public void Initialize(VisualElement root)
    {
        _root = root;
        _itemTooltip = _root.Q<VisualElement>("ItemTooltip");
    }
    
    /// <summary>
    /// 아이템 툴팁 표시
    /// </summary>
    public void ShowItemTooltip(string itemName, int grade, Vector2 position)
    {
        if (_itemTooltip == null) return;
        
        var tooltipName = _root.Q<Label>("TooltipName");
        var tooltipGrade = _root.Q<Label>("TooltipGrade");
        
        if (tooltipName != null)
            tooltipName.text = itemName;
        
        if (tooltipGrade != null)
        {
            string[] gradeNames = GetGradeNames();
            tooltipGrade.text = grade < gradeNames.Length ? gradeNames[grade] : "알 수 없음";
            tooltipGrade.style.color = GetGradeColor(grade);
        }
        
        // 툴팁 위치 설정
        _itemTooltip.style.left = position.x;
        _itemTooltip.style.top = position.y;
        _itemTooltip.style.display = DisplayStyle.Flex;
    }
    
    /// <summary>
    /// 아이템 툴팁 숨기기
    /// </summary>
    public void HideItemTooltip()
    {
        if (_itemTooltip != null)
            _itemTooltip.style.display = DisplayStyle.None;
    }
    
    /// <summary>
    /// 자동 숨김 예약
    /// </summary>
    public void HideItemTooltipDelayed(float delay)
    {
        CancelInvoke();
        Invoke(nameof(HideItemTooltip), delay);
    }
    
    private void CancelInvoke()
    {
        CancelInvoke(nameof(HideItemTooltip));
    }
    
    /// <summary>
    /// 등급 이름 배열 가져오기
    /// </summary>
    public string[] GetGradeNames()
    {
        return new[] { "일반", "고급", "희귀", "영웅", "전설" };
    }
    
    /// <summary>
    /// 등급 색상 가져오기
    /// </summary>
    public Color GetGradeColor(int grade)
    {
        switch (grade)
        {
            case 0: return new Color(0.8f, 0.8f, 0.8f); // 일반 - 회색
            case 1: return new Color(0.2f, 0.8f, 0.2f); // 고급 - 초록
            case 2: return new Color(0.2f, 0.6f, 1f);   // 희귀 - 파랑
            case 3: return new Color(1f, 0.6f, 0.2f);   // 영웅 - 주황
            case 4: return new Color(1f, 0.4f, 0.8f);   // 전설 - 분홍
            default: return Color.white;
        }
    }
}
