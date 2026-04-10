using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 아이템 드롭 이펙트 매니저 (SRP 준수)
/// 아이템 획득 시 시각적 이펙트를 표시합니다.
/// </summary>
public class ItemDropEffectManager : MonoBehaviour
{
    private static ItemDropEffectManager _instance;
    
    public static ItemDropEffectManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ItemDropEffectManager");
                _instance = go.AddComponent<ItemDropEffectManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    private VisualElement _root;
    private IEventBus _eventBus;
    
    // 아이템 타입별 아이콘
    private static readonly System.Collections.Generic.Dictionary<string, string> ItemIcons = new System.Collections.Generic.Dictionary<string, string>
    {
        { "weapon", "⚔️" },
        { "armor", "🛡️" },
        { "boots", "👟" },
        { "accessory", "💍" }
    };
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public void Initialize(VisualElement root, IEventBus eventBus)
    {
        _root = root;
        _eventBus = eventBus;
        
        // 이벤트 구독
        _eventBus.On(GameEvents.ITEM_ACQUIRED, OnItemAcquired);
    }
    
    private void OnItemAcquired()
    {
        // 간단한 이펙트만 표시 (실제로는 더 복잡한 애니메이션 가능)
        Debug.Log("[ItemDropEffect] 아이템 획득!");
    }
    
    /// <summary>
    /// 아이템 드롭 이펙트 표시
    /// </summary>
    public void ShowDropEffect(string itemName, string itemType, int grade)
    {
        if (_root == null) return;
        
        // 아이콘 결정
        string icon = "📦";
        foreach (var kvp in ItemIcons)
        {
            if (itemType.ToLower().Contains(kvp.Key))
            {
                icon = kvp.Value;
                break;
            }
        }
        
        // 이펙트 레이블 생성
        var label = new Label($"{icon} {itemName}");
        label.style.position = Position.Absolute;
        label.style.left = Length.Percent(50);
        label.style.top = new Length(200, LengthUnit.Pixel);
        label.style.fontSize = 24;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        
        // 희귀도별 색상
        label.style.color = GetGradeColor(grade);
        
        _root.Add(label);
        
        // 애니메이션 (간이 코루틴)
        StartCoroutine(AnimateDropEffect(label));
    }
    
    private System.Collections.IEnumerator AnimateDropEffect(Label label)
    {
        float duration = 1.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // 위로 이동 + 투명도 감소
            label.style.top = new Length(200 - (progress * 100), LengthUnit.Pixel);
            var color = label.resolvedStyle.color;
            color.a = 1f - progress;
            label.style.color = color;
            
            yield return null;
        }
        
        if (_root != null && label.panel != null)
        {
            _root.Remove(label);
        }
    }
    
    private Color GetGradeColor(int grade)
    {
        switch (grade)
        {
            case 0: return Color.white;           // 일반
            case 1: return new Color(0.3f, 0.7f, 1f);  // 고급
            case 2: return new Color(0.6f, 0.3f, 1f);  // 희귀
            case 3: return new Color(1f, 0.8f, 0.2f);  // 영웅
            case 4: return new Color(1f, 0.4f, 0.1f);  // 전설
            default: return Color.white;
        }
    }
}
