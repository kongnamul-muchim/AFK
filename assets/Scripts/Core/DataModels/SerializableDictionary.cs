using System;
using System.Collections.Generic;

/// <summary>
/// 직렬화 가능한 딕셔너리 (Unity JsonUtility 호환)
/// </summary>
[Serializable]
public class SerializableDictionary<TKey, TValue>
{
    [Serializable]
    public class KeyValuePair
    {
        public TKey key;
        public TValue value;
    }
    
    public KeyValuePair[] pairs;
    
    public SerializableDictionary()
    {
        pairs = new KeyValuePair[0];
    }
    
    public SerializableDictionary(Dictionary<TKey, TValue> dict)
    {
        pairs = new KeyValuePair[dict.Count];
        int i = 0;
        foreach (var kvp in dict)
        {
            pairs[i] = new KeyValuePair { key = kvp.Key, value = kvp.Value };
            i++;
        }
    }
    
    public Dictionary<TKey, TValue> ToDictionary()
    {
        var dict = new Dictionary<TKey, TValue>();
        if (pairs != null)
        {
            foreach (var kvp in pairs)
            {
                if (kvp != null && kvp.key != null)
                {
                    dict[kvp.key] = kvp.value;
                }
            }
        }
        return dict;
    }
    
    public void Add(TKey key, TValue value)
    {
        if (pairs == null) pairs = new KeyValuePair[0];
        
        foreach (var kvp in pairs)
        {
            if (kvp != null && kvp.key != null && kvp.key.Equals(key))
            {
                kvp.value = value;
                return;
            }
        }
        
        var newPairs = new KeyValuePair[pairs.Length + 1];
        Array.Copy(pairs, newPairs, pairs.Length);
        newPairs[pairs.Length] = new KeyValuePair { key = key, value = value };
        pairs = newPairs;
    }
    
    public bool ContainsKey(TKey key)
    {
        if (pairs == null) return false;
        foreach (var kvp in pairs)
        {
            if (kvp != null && kvp.key != null && kvp.key.Equals(key))
                return true;
        }
        return false;
    }
    
    public TValue this[TKey key]
    {
        get
        {
            if (pairs == null) throw new KeyNotFoundException();
            foreach (var kvp in pairs)
            {
                if (kvp != null && kvp.key != null && kvp.key.Equals(key))
                    return kvp.value;
            }
            throw new KeyNotFoundException();
        }
        set
        {
            Add(key, value);
        }
    }
}
