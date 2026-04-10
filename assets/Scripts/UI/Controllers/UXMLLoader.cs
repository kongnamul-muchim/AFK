using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// UXML 파일의 지연 로딩과 캐싱을 담당하는 클래스 (SRP 준수)
/// Resources/UXML/modals/ 폴더의 UXML 파일을 동적으로 로드하고 캐시합니다.
/// </summary>
public static class UXMLLoader
{
    // UXML 캐시: 파일명 -> VisualElement
    private static readonly Dictionary<string, VisualElement> _cache = new Dictionary<string, VisualElement>();
    
    // UXML 폴더 경로 (Resources 기준)
    private const string UXML_FOLDER = "UXML/modals";
    
    /// <summary>
    /// 모달 UXML을 로드합니다. 캐시되어 있으면 캐시된 것을 반환합니다.
    /// </summary>
    /// <param name="modalName">모달 이름 (파일명 without .uxml)</param>
    /// <param name="parent">부모 VisualElement</param>
    /// <returns>로드된 VisualElement, 실패 시 null</returns>
    public static VisualElement LoadModal(string modalName, VisualElement parent)
    {
        // 캐시 확인
        if (_cache.TryGetValue(modalName, out VisualElement cachedElement))
        {
            // 캐시된 요소 재사용
            Debug.Log($"[UXMLLoader] 캐시에서 로드: {modalName}");
            return cachedElement;
        }
        
        // UXML 로드
        string path = $"{UXML_FOLDER}/{modalName}";
        var asset = Resources.Load<VisualTreeAsset>(path);
        
        if (asset == null)
        {
            Debug.LogError($"[UXMLLoader] UXML을 찾을 수 없음: {path}");
            return null;
        }
        
        // 인스턴스화
        VisualElement element = asset.Instantiate();
        
        // 캐시에 저장
        _cache[modalName] = element;
        
        // 부모에 추가
        if (parent != null)
        {
            parent.Add(element);
        }
        
        Debug.Log($"[UXMLLoader] 새 UXML 로드: {modalName}");
        return element;
    }
    
    /// <summary>
    /// 특정 모달의 캐시를 제거합니다.
    /// </summary>
    public static void UnloadModal(string modalName)
    {
        if (_cache.TryGetValue(modalName, out VisualElement element))
        {
            element.RemoveFromHierarchy();
            _cache.Remove(modalName);
            Debug.Log($"[UXMLLoader] 언로드: {modalName}");
        }
    }
    
    /// <summary>
    /// 모든 모달 캐시를 제거합니다.
    /// </summary>
    public static void UnloadAll()
    {
        foreach (var kvp in _cache)
        {
            kvp.Value.RemoveFromHierarchy();
        }
        _cache.Clear();
        Debug.Log("[UXMLLoader] 모든 캐시 제거");
    }
    
    /// <summary>
    /// 모달이 캐시되어 있는지 확인합니다.
    /// </summary>
    public static bool IsLoaded(string modalName)
    {
        return _cache.ContainsKey(modalName);
    }
    
    /// <summary>
    /// 캐시된 모달 가져오기 (부모에 추가하지 않음)
    /// </summary>
    public static VisualElement GetCached(string modalName)
    {
        _cache.TryGetValue(modalName, out VisualElement element);
        return element;
    }
    
    /// <summary>
    /// 캐시 정보 로그 출력
    /// </summary>
    public static void LogCacheStatus()
    {
        Debug.Log($"[UXMLLoader] 캐시 상태: {_cache.Count}개 모달 로딩됨");
        foreach (var key in _cache.Keys)
        {
            Debug.Log($"  - {key}");
        }
    }
}
