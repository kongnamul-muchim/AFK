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
        Debug.Log($"[TOOLTIP-MGR] Initialize 완료: _itemTooltip={(_itemTooltip != null ? "있음" : "null")}");
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
        
        // 툴팁을 화면 중앙 근처에 표시 (로컬 좌표를-world 좌표로 변환)
        // UIToolkit에서 position은 로컬 좌표이므로, 부모 기준 오프셋 추가
        var parent = _itemTooltip.parent;
        if (parent != null)
        {
            var parentRect = parent.worldBound;
            _itemTooltip.style.left = parentRect.x + position.x + 20; // 20px 오프셋
            _itemTooltip.style.top = parentRect.y + position.y + 20;
        }
        else
        {
            _itemTooltip.style.left = position.x + 20;
            _itemTooltip.style.top = position.y + 20;
        }
        
        _itemTooltip.style.display = DisplayStyle.Flex;
        // Debug.Log($"[Tooltip] 표시: {itemName}, 위치: ({position.x}, {position.y})");
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
    /// 비교 툴팁 표시 - 현재 장비와 비교
    /// </summary>
    public void ShowComparisonTooltip(string itemName, int rarity, string itemType,
        float newAttack, float newDefense, float newHealth,
        float currentAttack, float currentDefense, float currentHealth,
        Vector2 position)
    {
        Debug.Log($"[TOOLTIP-MGR] ShowComparisonTooltip 시작: itemName={itemName}");
        Debug.Log($"[TOOLTIP-MGR] _itemTooltip={( _itemTooltip != null ? "있음" : "null")}, _root={( _root != null ? "있음" : "null")}");
        
        if (_itemTooltip == null)
        {
            Debug.LogWarning("[TOOLTIP-MGR] _itemTooltip이 null! 초기화 확인 필요");
            return;
        }
        
        // 기본 정보 설정
        var tooltipName = _root.Q<Label>("TooltipName");
        var tooltipGrade = _root.Q<Label>("TooltipGrade");
        
        if (tooltipName != null)
            tooltipName.text = itemName + " [장착 비교]";
        
        if (tooltipGrade != null)
        {
            string[] rarityNames = GetGradeNames();
            tooltipGrade.text = rarityNames[Mathf.Clamp(rarity, 0, rarityNames.Length - 1)];
            tooltipGrade.style.color = GetGradeColor(rarity);
        }
        
        // Stats 정보 업데이트 (기존 Label 또는 VisualElement 활용)
        // 비교 정보를 툴팁에 표시하기 위해 특수 마크업 사용
        UpdateTooltipStats(newAttack, newDefense, newHealth, 
            newAttack - currentAttack, 
            newDefense - currentDefense, 
            newHealth - currentHealth);
        
        // 위치 설정
        var parent = _itemTooltip.parent;
        if (parent != null)
        {
            var parentRect = parent.worldBound;
            _itemTooltip.style.left = parentRect.x + position.x + 20;
            _itemTooltip.style.top = parentRect.y + position.y + 20;
        }
        else
        {
            _itemTooltip.style.left = position.x + 20;
            _itemTooltip.style.top = position.y + 20;
        }
        
        _itemTooltip.style.display = DisplayStyle.Flex;
        Debug.Log($"[TOOLTIP-MGR] 툴팁 표시 완료!");
    }
    
    /// <summary>
    /// 툴팁 스탯 정보 업데이트 (비교 표시)
    /// </summary>
    private void UpdateTooltipStats(float atk, float def, float hp, float atkDelta, float defDelta, float hpDelta)
    {
        // TooltipStats라는 VisualElement가 있다고 가정하고 업데이트
        var statsContainer = _root.Q<VisualElement>("TooltipStats");
        if (statsContainer != null)
        {
            statsContainer.Clear();
            
            // 공격력
            string atkDeltaStr = atkDelta != 0 ? $"({(atkDelta > 0 ? "+" : "")}{atkDelta:F0})" : "";
            var atkRow = new Label($"⚔️ 공격력: {atk:F0} {atkDeltaStr}");
            atkRow.style.color = atkDelta > 0 ? Color.green : (atkDelta < 0 ? Color.red : Color.white);
            statsContainer.Add(atkRow);
            
            // 방어력
            string defDeltaStr = defDelta != 0 ? $"({(defDelta > 0 ? "+" : "")}{defDelta:F0})" : "";
            var defRow = new Label($"🛡️ 방어력: {def:F0} {defDeltaStr}");
            defRow.style.color = defDelta > 0 ? Color.green : (defDelta < 0 ? Color.red : Color.white);
            statsContainer.Add(defRow);
            
            // 체력
            string hpDeltaStr = hpDelta != 0 ? $"({(hpDelta > 0 ? "+" : "")}{hpDelta:F0})" : "";
            var hpRow = new Label($"❤️ 체력: {hp:F0} {hpDeltaStr}");
            hpRow.style.color = hpDelta > 0 ? Color.green : (hpDelta < 0 ? Color.red : Color.white);
            statsContainer.Add(hpRow);
        }
    }
    
    /// <summary>
    /// 자동 숨김 예약
    /// </summary>
    public void HideItemTooltipDelayed(float delay)
    {
        CancelAllInvokes();
        Invoke(nameof(HideItemTooltip), delay);
    }
    
    /// <summary>
    /// 예약된 Invoke 취소
    /// </summary>
    private void CancelAllInvokes()
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
