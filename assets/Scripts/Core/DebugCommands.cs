using UnityEngine;
using UnityEngine.InputSystem;

public static class DebugCommands
{
    private static bool _initialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        var go = new GameObject("DebugCommands");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<DebugCommandBehaviour>();
    }

    private class DebugCommandBehaviour : MonoBehaviour
    {
        private void Update()
        {
            if (Keyboard.current.f1Key.wasPressedThisFrame)
            {
                AddExp(10000000);
                Debug.Log("[DEBUG] +10,000,000 EXP (F1)");
            }
            if (Keyboard.current.f2Key.wasPressedThisFrame)
            {
                var state = GameState.Instance;
                if (state != null)
                {
                    state.player.gold += 100000;
                    EventBus.Instance.Emit(GameEvents.GOLD_CHANGED);
                    Debug.Log("[DEBUG] +100,000 Gold (F2)");
                }
            }
            if (Keyboard.current.f3Key.wasPressedThisFrame)
            {
                var state = GameState.Instance;
                if (state != null)
                {
                    state.player.gems += 1000;
                    EventBus.Instance.Emit(GameEvents.GEM_CHANGED);
                    Debug.Log("[DEBUG] +1,000 Gems (F3)");
                }
            }
        }

        private static void AddExp(long amount)
        {
            var state = GameState.Instance;
            if (state == null) return;

            bool leveledUp = state.AddExperience(amount);
            if (leveledUp)
            {
                Debug.Log($"[DEBUG] Level up! Now Lv.{state.player.level}");
            }
            EventBus.Instance.Emit(GameEvents.PLAYER_STAT_CHANGED);
        }
    }
}
