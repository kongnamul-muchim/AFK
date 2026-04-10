using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance { get; private set; }
    
    private Dictionary<string, VisualElement> _popupCache = new();
    private VisualElement _currentPopup;
    
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
    
    public void ShowPopup(string popupName)
    {
        HideCurrentPopup();
        
        if (!_popupCache.ContainsKey(popupName))
        {
            LoadPopup(popupName);
        }
        
        if (_popupCache.TryGetValue(popupName, out var popup))
        {
            _currentPopup = popup;
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                var uiDoc = canvas.GetComponent<UIDocument>();
                if (uiDoc != null)
                {
                    uiDoc.rootVisualElement.Add(_currentPopup);
                    _currentPopup.style.display = DisplayStyle.Flex;
                }
            }
        }
        else
        {
            Debug.LogError($"팝업 {popupName}을(를) 찾을 수 없습니다!");
        }
    }
    
    public void HideCurrentPopup()
    {
        if (_currentPopup != null)
        {
            _currentPopup.style.display = DisplayStyle.None;
            _currentPopup.RemoveFromHierarchy();
            _currentPopup = null;
        }
    }
    
    private void LoadPopup(string popupName)
    {
        var popupAsset = Resources.Load<VisualTreeAsset>($"UI/{popupName}");
        if (popupAsset != null)
        {
            var popup = popupAsset.CloneTree();
            popup.style.position = Position.Absolute;
            popup.style.left = 0;
            popup.style.top = 0;
            popup.style.right = 0;
            popup.style.bottom = 0;
            popup.style.alignItems = Align.Center;
            popup.style.justifyContent = Justify.Center;
            _popupCache[popupName] = popup;
        }
    }
}